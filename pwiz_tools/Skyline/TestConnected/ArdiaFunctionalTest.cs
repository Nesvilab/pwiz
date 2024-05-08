/*
 * Original author: Matt Chambers <matt.chambers42 .@. gmail.com>
 *
 * Copyright 2024 University of Washington - Seattle, WA
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using pwiz.Common.Collections;
using pwiz.Skyline.Alerts;
using pwiz.Skyline.Controls.Graphs;
using pwiz.Skyline.FileUI;
using pwiz.Skyline.Model.ElementLocators;
using pwiz.Skyline.Model.Results.RemoteApi;
using pwiz.Skyline.Model.Results.RemoteApi.Ardia;
using pwiz.Skyline.Properties;
using pwiz.Skyline.ToolsUI;
using pwiz.SkylineTestUtil;

namespace pwiz.SkylineTestConnected
{
    [TestClass]
    public class ArdiaTest : AbstractFunctionalTestEx
    {
        private ArdiaAccount _account;
        private List<string[]> _openPaths;

        private readonly List<string[]> SmallPaths = new List<string[]>
        {
            new[] { "Skyline" },
            new[] { "Small Files" },
            new[] { "Reserpine_10 pg_µL_2_08", "Uracil_Caffeine(Water)_Inj_Det_2_04" }
        };

        private readonly List<string[]> LargePaths = new List<string[]>
        {
            new[] { "Skyline" },
            new[] { "ExtraLargeFiles" },
            new[] { "Test" },
            new[] { "astral", "Q_2014_0523_12_0_amol_uL_20mz" }
        };

        [TestMethod]
        public void TestArdiaSingleRole()
        {
            if (!ArdiaTestUtil.EnableArdiaTests)
            {
                Console.Error.WriteLine("NOTE: skipping Ardia test because username/password for Ardia is not configured in environment variables");
                return;
            }

            TestFilesZip = @"TestConnected\ArdiaFunctionalTest.zip";

            _account = ArdiaTestUtil.GetTestAccount(ArdiaTestUtil.AccountType.SingleRole).ChangeDeleteRawAfterImport(true);
            _openPaths = SmallPaths;

            RunFunctionalTest();
        }

        [TestMethod]
        public void TestArdiaMultiRole()
        {
            if (!ArdiaTestUtil.EnableArdiaTests)
            {
                Console.Error.WriteLine("NOTE: skipping Ardia test because username/password for Ardia is not configured in environment variables");
                return;
            }

            TestFilesZip = @"TestConnected\ArdiaFunctionalTest.zip";

            _account = ArdiaTestUtil.GetTestAccount();
            _openPaths = SmallPaths;

            RunFunctionalTest();
        }

        //[TestMethod]
        public void TestArdiaLargeFile()
        {
            if (!ArdiaTestUtil.EnableArdiaTests)
            {
                Console.Error.WriteLine("NOTE: skipping Ardia test because username/password for Ardia is not configured in environment variables");
                return;
            }

            /*if (!RunPerfTests)
            {
                Console.Error.WriteLine("NOTE: skipping TestArdiaLargeFile because perftests are not enabled");
                return;
            }*/

            TestFilesZip = @"TestConnected\ArdiaFunctionalTest.zip";

            _account = ArdiaTestUtil.GetTestAccount(ArdiaTestUtil.AccountType.SingleRole);
            _openPaths = LargePaths;

            RunFunctionalTest();
        }

        protected override void DoTest()
        {
            RunUI(() => SkylineWindow.OpenFile(TestFilesDir.GetTestPath("small.sky")));
            var importResultsDlg = ShowDialog<ImportResultsDlg>(SkylineWindow.ImportResults);
            var openDataSourceDialog = ShowDialog<OpenDataSourceDialog>(importResultsDlg.OkDialog);
            var editAccountDlg = ShowDialog<EditRemoteAccountDlg>(() => openDataSourceDialog.CurrentDirectory = RemoteUrl.EMPTY);
            RunUI(() => editAccountDlg.SetRemoteAccount(_account));

            // Click test button
            var testSuccessfulDlg = ShowDialog<MessageDlg>(() => editAccountDlg.TestSettings());
            OkDialog(testSuccessfulDlg, testSuccessfulDlg.OkDialog);
            OkDialog(editAccountDlg, editAccountDlg.OkDialog);

            foreach(var paths in _openPaths)
                OpenFile(openDataSourceDialog, paths);
            WaitForDocumentLoaded();
            WaitForClosedAllChromatogramsGraph();

            // short circuit for large file test
            if (ReferenceEquals(_openPaths, LargePaths))
                return;

            string rawFilepath = TestFilesDir.GetTestPath(_openPaths.Last().First() + ".raw");

            // short circuit single role test to reduce test time
            if (_account.Role.IsNullOrEmpty())
            {
                AssertEx.FileNotExists(rawFilepath); // for single role test, file should have been deleted after importing
                return;
            }

            RunUI(() => SkylineWindow.SaveDocument());
            RunUI(() => SkylineWindow.OpenFile(TestFilesDir.GetTestPath("small.sky")));

            // delete local RAW file to test that it gets redownloaded when clicking on the chromatogram to view a spectrum
            AssertEx.FileExists(rawFilepath);
            File.Delete(rawFilepath);

            WaitForDocumentLoaded();
            RunUI(() => SkylineWindow.SelectElement(ElementRefs.FromObjectReference(ElementLocator.Parse("Molecule:/Molecules/Reserpine"))));

            ClickChromatogram(0.5, 33000);
            GraphFullScan graphFullScan = FindOpenForm<GraphFullScan>();
            Assert.IsNotNull(graphFullScan);
            RunUI(() => graphFullScan.Close());

            // delete results and reimport to test using saved cookie
            RemoveResultsAndReimport();

            // corrupt the cookie (simulate it being expired) and try reimporting again
            //Settings.Default.LastArdiaLoginCookieByUsername[_account.Username] = "foobar";
            _account = _account.ChangeBffHostCookie("foobar");

            // delete local files
            foreach (var rawName in _openPaths.Last())
                File.Delete(TestFilesDir.GetTestPath(rawName + ".raw"));

            _account.ResetAuthenticatedHttpClientFactory();
            Settings.Default.RemoteAccountList.Clear();
            Settings.Default.RemoteAccountList.Add(_account);
            RemoveResultsAndReimport();
        }

        private void RemoveResultsAndReimport()
        {
            RunDlg<ManageResultsDlg>(SkylineWindow.ManageResults, dlg =>
            {
                dlg.RemoveAllReplicates();
                dlg.OkDialog();
            });
            RunUI(() => SkylineWindow.SaveDocument());

            var importResultsDlg = ShowDialog<ImportResultsDlg>(SkylineWindow.ImportResults);
            var openDataSourceDialog = ShowDialog<OpenDataSourceDialog>(importResultsDlg.OkDialog);
            RunUI(() => openDataSourceDialog.CurrentDirectory = RemoteUrl.EMPTY);
            foreach (var paths in _openPaths)
                OpenFile(openDataSourceDialog, paths);
            WaitForDocumentLoaded();
            WaitForClosedAllChromatogramsGraph();
        }

        private void OpenFile(OpenDataSourceDialog openDataSourceDialog, params string[] names)
        {
            WaitForConditionUI(() => names.All(n => openDataSourceDialog.ListItemNames.Contains(n)));
            RunUI(() =>
            {
                foreach (string name in names)
                    openDataSourceDialog.SelectFile(name);
                openDataSourceDialog.Open();
            });
        }
    }
}
