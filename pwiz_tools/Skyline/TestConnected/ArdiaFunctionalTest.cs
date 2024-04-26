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
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using pwiz.Common.Collections;
using pwiz.Skyline;
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
        private string _saveCookie;
        private ArdiaAccount _account;

        [TestMethod]
        public void TestArdiaSingleRole()
        {
            if (!ArdiaTestUtil.EnableArdiaTests)
            {
                Console.Error.WriteLine("NOTE: skipping Ardia test because username/password for Ardia is not configured in environment variables");
                return;
            }

            TestFilesZip = @"TestConnected\ArdiaFunctionalTest.zip";

            _account = ArdiaTestUtil.GetTestAccount(ArdiaTestUtil.AccountType.SingleRole);
            // preserve login cookie (RunFunctionalTest will reset settings to all defaults)
            if (!Program.UseOriginalURLs)
                Settings.Default.LastArdiaLoginCookieByUsername.TryGetValue(_account.Username, out _saveCookie);
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

            // preserve login cookie (RunFunctionalTest will reset settings to all defaults)
            if (!Program.UseOriginalURLs)
                Settings.Default.LastArdiaLoginCookieByUsername.TryGetValue(_account.Username, out _saveCookie);
            RunFunctionalTest();
        }

        protected override void Cleanup()
        {
            // restore cookie after post-test settings reset
            Settings.Default.LastArdiaLoginCookieByUsername[_account.Username] = _saveCookie;
            Settings.Default.Save();
        }

        /*[TestMethod]
        public void TestRemoteAccountList()
        {
            var ArdiaAccount = new ArdiaAccount("https://Ardiaserver.xxx", "Ardia_username", "Ardia_password");
            var remoteAccountList = new RemoteAccountList();
            remoteAccountList.Add(ArdiaAccount);
            StringWriter stringWriter = new StringWriter();
            var xmlSerializer = new XmlSerializer(typeof(RemoteAccountList));
            xmlSerializer.Serialize(stringWriter, remoteAccountList);
            var serializedAccountList = stringWriter.ToString();
            Assert.AreEqual(-1, serializedAccountList.IndexOf(ArdiaAccount.Password, StringComparison.Ordinal));
            var roundTrip = (RemoteAccountList) xmlSerializer.Deserialize(new StringReader(serializedAccountList));
            Assert.AreEqual(remoteAccountList.Count, roundTrip.Count);
            Assert.AreEqual(ArdiaAccount, roundTrip[0]);
        }*/

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


            try
            {
                OkDialog(editAccountDlg, editAccountDlg.OkDialog);

                // short circuit single role test to reduce test time
                if (_account.Role.IsNullOrEmpty())
                {
                    RunUI(openDataSourceDialog.CancelDialog);
                    RunUI(importResultsDlg.CancelDialog);
                    return;
                }

                OpenFile(openDataSourceDialog, "Skyline");
                OpenFile(openDataSourceDialog, "Small Files");
                OpenFile(openDataSourceDialog, "Reserpine_10 pg_µL_2_08", "Uracil_Caffeine(Water)_Inj_Det_2_04");
                WaitForDocumentLoaded();
                WaitForClosedAllChromatogramsGraph();

                RunUI(() => SkylineWindow.SaveDocument());
                RunUI(() => SkylineWindow.OpenFile(TestFilesDir.GetTestPath("small.sky")));

                // delete local RAW file to test that it gets redownloaded when clicking on the chromatogram to view a spectrum
                string rawFilepath = TestFilesDir.GetTestPath("Reserpine_10 pg_µL_2_08.raw");
                File.Delete(rawFilepath);

                WaitForDocumentLoaded();
                RunUI(() => SkylineWindow.SelectElement(ElementRefs.FromObjectReference(ElementLocator.Parse("Molecule:/Molecules/Reserpine"))));

                ClickChromatogram(0.5, 33000);
                GraphFullScan graphFullScan = FindOpenForm<GraphFullScan>();
                Assert.IsNotNull(graphFullScan);
                RunUI(() => graphFullScan.Close());
            }
            finally
            {
                // always save cookie if login was successful
                _saveCookie = Settings.Default.LastArdiaLoginCookieByUsername[_account.Username];
            }
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
