// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Provisioning.Client;
using Microsoft.Azure.Devices.Shared;
using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Provisioning.Client.Samples
{
    public class ProvisioningDeviceClientSample
    {
        ProvisioningDeviceClient _provClient;
        SecurityProvider _security;

        public ProvisioningDeviceClientSample(ProvisioningDeviceClient provisioningDeviceClient, SecurityProvider security)
        {
            _provClient = provisioningDeviceClient;
            _security = security;
        }

        public async Task RunSampleAsync()
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine($"RegistrationID = {_security.GetRegistrationID()}");
            VerifyRegistrationIdFormat(_security.GetRegistrationID());

            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("ProvisioningClient RegisterAsync . . . ");
            DeviceRegistrationResult result = await _provClient.RegisterAsync().ConfigureAwait(false);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"{result.Status}");
            Console.WriteLine($"ProvisioningClient AssignedHub: {result.AssignedHub}; DeviceID: {result.DeviceId}");

            if (result.Status != ProvisioningRegistrationStatusType.Assigned) return;

            IAuthenticationMethod auth;
            if (_security is SecurityProviderTpm)
            {
                Console.WriteLine("Creating TPM DeviceClient authentication.");
                auth = new DeviceAuthenticationWithTpm(result.DeviceId, _security as SecurityProviderTpm);
            }
            else if (_security is SecurityProviderX509)
            {
                Console.WriteLine("Creating X509 DeviceClient authentication.");
                auth = new DeviceAuthenticationWithX509Certificate(result.DeviceId, (_security as SecurityProviderX509).GetAuthenticationCertificate());
            }
            else if (_security is SecurityProviderSymmetricKey)
            {
                Console.WriteLine("Creating Symmetric Key DeviceClient authenication");
                auth = new DeviceAuthenticationWithRegistrySymmetricKey(result.DeviceId, (_security as SecurityProviderSymmetricKey).GetPrimaryKey());
            }
            else
            {
                throw new NotSupportedException("Unknown authentication type.");
            }

            using (DeviceClient iotClient = DeviceClient.Create(result.AssignedHub, auth, TransportType.Amqp))
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("DeviceClient OpenAsync.");
                await iotClient.OpenAsync().ConfigureAwait(false);
                Console.WriteLine("DeviceClient SendEventAsync.");
                await iotClient.SendEventAsync(new Message(Encoding.UTF8.GetBytes("TestMessage"))).ConfigureAwait(false);
                Console.WriteLine("DeviceClient CloseAsync.");
                await iotClient.CloseAsync().ConfigureAwait(false);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("****************************************");
                Console.WriteLine();
                Console.WriteLine($"\tRegistration ID: {result.DeviceId} created successfully!");
                Console.WriteLine();
                Console.WriteLine("****************************************");
            }

        }

        private void VerifyRegistrationIdFormat(string v)
        {
            var r = new Regex("^[a-zA-Z0-9-]*$");
            if (!r.IsMatch(v))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                throw new FormatException("Invalid registrationId: The registration ID is alphanumeric, lowercase, and may contain hyphens");
            }
        }
    }
}
