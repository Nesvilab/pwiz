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
using System.Net.Http;
using System.Xml;
using System.Xml.Serialization;
using pwiz.Common.Collections;
using pwiz.Skyline.Util;

namespace pwiz.Skyline.Model.Results.RemoteApi.Ardia
{
    [XmlRoot("ardia_account")]
    public class ArdiaAccount : RemoteAccount
    {
        public static readonly ArdiaAccount DEFAULT = new ArdiaAccount(string.Empty, string.Empty, string.Empty);

        public ArdiaAccount(string serverUrl, string username, string password)
        {
            ServerUrl = serverUrl;
        }

        public string GetFolderContentsUrl(ArdiaUrl ardiaUrl)
        {
            if (ardiaUrl.SequenceKey != null)
                return GetRootArdiaUrl().SequenceBaseUrl + ardiaUrl.SequenceKey;
            else if (ardiaUrl.EncodedPath != null)
                return GetRootArdiaUrl().NavigationBaseUrl + $@"/path?itemPath=/{ardiaUrl.EncodedPath}";
            return GetRootArdiaUrl().NavigationBaseUrl;
        }

        public string GetFolderContentsUrl(string folder = "")
        {
            return GetRootArdiaUrl().NavigationBaseUrl + ((folder?.TrimStart('/')).IsNullOrEmpty() ? "" : $@"/path?itemPath={folder}");
        }

        public string GetPathFromFolderContentsUrl(string folderUrl)
        {
            var rootUrl = GetRootArdiaUrl();
            return folderUrl.Replace(rootUrl.NavigationBaseUrl, "").Replace(rootUrl.ServerUrl, "").Replace(@"/path?itemPath=", "").TrimEnd('/');
        }

        private Func<HttpClient> _authenticatedHttpClientFactory;

        public HttpClient GetAuthenticatedHttpClient()
        {
            if (_authenticatedHttpClientFactory == null)
            {
                // If RemoteAccountUserInteraction is null, some top level interface didn't set it when it should have
                Assume.IsNotNull(RemoteSession.RemoteAccountUserInteraction, @"RemoteSession.UserInteraction is not set");
                _authenticatedHttpClientFactory = RemoteSession.RemoteAccountUserInteraction.UserLogin(this);
            }
            return _authenticatedHttpClientFactory();
        }

        public override RemoteAccountType AccountType
        {
            get { return RemoteAccountType.ARDIA; }
        }

        public override RemoteSession CreateSession()
        {
            return new ArdiaSession(this);
        }

        public override RemoteAccount ChangeServerUrl(string serverUrl)
        {
            return ChangeProp(ImClone(this), im =>
            {
                im.ServerUrl = serverUrl;
            });
        }

        public override RemoteAccount ChangeUsername(string username)
        {
            return ChangeProp(ImClone(this), im =>
            {
                im.Username = username;
            });
        }

        public ArdiaUrl GetRootArdiaUrl()
        {
            return (ArdiaUrl) ArdiaUrl.Empty.ChangeServerUrl(ServerUrl).ChangeUsername(Username);
        }

        public override RemoteUrl GetRootUrl()
        {
            return GetRootArdiaUrl();
        }

        private ArdiaAccount()
        {
        }
        public static ArdiaAccount Deserialize(XmlReader reader)
        {
            return reader.Deserialize(new ArdiaAccount());
        }

        protected bool Equals(ArdiaAccount other)
        {
            return base.Equals(other);
        }
    }
}
