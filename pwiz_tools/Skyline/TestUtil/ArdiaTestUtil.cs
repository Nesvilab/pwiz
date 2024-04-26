﻿/*
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
using pwiz.Skyline.Model.Results.RemoteApi.Ardia;

namespace pwiz.SkylineTestUtil
{
    /// <summary>
    /// Helper methods for testing the Ardia server.
    /// In order for Ardia tests to be enabled, you must have an environment variable "ARDIA_PASSWORD".
    /// </summary>
    public static class ArdiaTestUtil
    {
        private static string _baseUrl = "https://hyperbridge.cmdtest.thermofisher.com";

        public enum AccountType
        {
            MultiRole,
            SingleRole
        }

        public static ArdiaAccount GetTestAccount(AccountType type = AccountType.MultiRole)
        {
            string envVarName = type == AccountType.MultiRole ? "ARDIA_PASSWORD" : "ARDIA_PASSWORD_1ROLE";
            //return (ArdiaAccount)ArdiaAccount.DEFAULT.ChangeServerUrl(_baseUrl).ChangeUsername("kajo.nagyeri@gmail.com").ChangePassword("Thermo@123");

            var password = Environment.GetEnvironmentVariable(envVarName);
            if (string.IsNullOrWhiteSpace(password))
                return null;

            switch (type)
            {
                case AccountType.MultiRole:
                    return (ArdiaAccount)ArdiaAccount.DEFAULT.ChangeRole("Tester")
                        .ChangeServerUrl(_baseUrl)
                        .ChangeUsername("matt.chambers42@gmail.com")
                        .ChangePassword(password);

                case AccountType.SingleRole:
                    return (ArdiaAccount)ArdiaAccount.DEFAULT.ChangeServerUrl(_baseUrl)
                        .ChangeUsername("kajo.nagyeri@gmail.com")
                        .ChangePassword(password);

                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        public static bool EnableArdiaTests
        {
            get
            {
                return GetTestAccount() != null;
            }
        }
    }
}
