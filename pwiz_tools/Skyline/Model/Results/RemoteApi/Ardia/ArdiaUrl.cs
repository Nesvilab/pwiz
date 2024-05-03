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
using System.Net.Http;
using System.Net;
using Newtonsoft.Json.Linq;
using pwiz.Common.Collections;
using pwiz.ProteowizardWrapper;
using pwiz.Skyline.Properties;
using pwiz.Skyline.Util;

namespace pwiz.Skyline.Model.Results.RemoteApi.Ardia
{
    public class ArdiaUrl : RemoteUrl
    {
        public static readonly ArdiaUrl Empty = new ArdiaUrl(UrlPrefix);
        public static string UrlPrefix { get { return RemoteAccountType.ARDIA.Name + @":"; } }
        public string ServerApiUrl => ServerUrl.Replace(@"https://", @"https://api.");
        public string NavigationBaseUrl => $@"{ServerApiUrl}/session-management/bff/navigation/api/v1/navigation";
        public string SequenceBaseUrl => $@"{ServerApiUrl}/session-management/bff/standard-sequence/api/v1/";
        private string RawDataUrl => $@"{ServerApiUrl}/session-management/bff/raw-data/api/v1/rawdata/";

        public ArdiaUrl(string ardiaUrl) : base(ardiaUrl)
        {
        }

        protected override void Init(NameValueParameters nameValueParameters)
        {
            base.Init(nameValueParameters);
            Id = nameValueParameters.GetValue(@"id");
            SequenceKey = nameValueParameters.GetValue(@"resourceKey");
            StorageId = nameValueParameters.GetValue(@"storageId");
            RawName = nameValueParameters.GetValue(@"rawName");
        }

        public string Id { get; private set; }
        public string SequenceKey { get; private set; }
        public string StorageId { get; private set; }
        public string RawName { get; private set; }

        public ArdiaUrl ChangeId(string id)
        {
            return ChangeProp(ImClone(this), im => im.Id = id);
        }

        public ArdiaUrl ChangeSequenceKey(string key)
        {
            return ChangeProp(ImClone(this), im => im.SequenceKey = key);
        }

        public ArdiaUrl ChangeStorageId(string key)
        {
            return ChangeProp(ImClone(this), im => im.StorageId = key);
        }

        public ArdiaUrl ChangeRawName(string key)
        {
            return ChangeProp(ImClone(this), im => im.RawName = key);
        }

        public override bool IsWatersLockmassCorrectionCandidate()
        {
            return false;
        }

        public override RemoteAccountType AccountType
        {
            get { return RemoteAccountType.ARDIA; }
        }

        protected override NameValueParameters GetParameters()
        {
            var result = base.GetParameters();
            result.SetValue(@"id", Id);
            result.SetValue(@"resourceKey", SequenceKey);
            result.SetValue(@"storageId", StorageId);
            result.SetValue(@"rawName", RawName);
            return result;
        }

        // Retrieves a presigned download URL for raw file from the Ardia platform.
        private string GetPresignedUrl(HttpClient client, string storageId)
        {
            // Encode the storageId to be used in the URL
            var encodedStorageId = WebUtility.UrlEncode(storageId);
            var url = new Uri(RawDataUrl + encodedStorageId);

            var response = client.GetAsync(url).Result;
            response.EnsureSuccessStatusCode();
            var responseString = response.Content.ReadAsStringAsync().Result;
            var presignedUrlJson = JObject.Parse(responseString);
            return presignedUrlJson[@"presignedUrl"].ToString();
        }

        public override MsDataFileImpl OpenMsDataFile(OpenMsDataFileParams openMsDataFileParams)
        {
            var rawFilepath = Path.Combine(openMsDataFileParams.DownloadPath, RawName);
            if (File.Exists(rawFilepath))
                return openMsDataFileParams.OpenLocalFile(rawFilepath, 0, LockMassParameters);

            var account = FindMatchingAccount(Settings.Default.RemoteAccountList) as ArdiaAccount;
            if (account == null)
            {
                throw new RemoteServerException(string.Format(ArdiaResources.UnifiUrl_OpenMsDataFile_Cannot_find_account_for_username__0__and_server__1__, 
                    Username, ServerUrl));
            }

            if (StorageId.IsNullOrEmpty())
                throw new InvalidDataException(ArdiaResources.ArdiaUrl_OpenMsDataFile_cannot_open_an_ArdiaUrl_because_it_is_not_a_RAW_file_URL_with_a_StorageId);

            using var client = account.GetAuthenticatedHttpClient();
            string presignedUrl = GetPresignedUrl(client, StorageId);

            var response = client.GetAsync(presignedUrl, HttpCompletionOption.ResponseHeadersRead, openMsDataFileParams.CancellationToken).Result;
            response.EnsureSuccessStatusCode();
            var responseStream = response.Content.ReadAsStreamAsync().Result;
            using (var fileStream = new FileStream(rawFilepath, FileMode.CreateNew))
            {
                responseStream.CopyTo(fileStream);
            }

            return openMsDataFileParams.OpenLocalFile(rawFilepath, 0, LockMassParameters);
        }
    }
}
