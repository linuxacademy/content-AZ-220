// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Provisioning.Client;
using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using Microsoft.Azure.Devices.Shared;
using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Microsoft.Azure.Devices.Provisioning.Client.Samples
{
    public static class Program
    {
        // The Provisioning Hub IDScope.

        // For this sample either:
        // - pass this value as a command-prompt argument
        // - set the DPS_IDSCOPE environment variable 
        // - create a launchSettings.json (see launchSettings.json.template) containing the variable
        private static string s_idScope = Environment.GetEnvironmentVariable("DPS_IDSCOPE");
        private const string GlobalDeviceEndpoint = "global.azure-devices-provisioning.net";
       
        //registration id for enrollment groups can be chosen arbitrarily and do not require any portal setup. 
        //The chosen value will become the provisioned device's device id.
        //
        //registration id for individual enrollments must be retrieved from the portal and will be unrelated to the provioned
        //device's device id
        //
        //This field is mandatory to provide for this sample
        private static string s_registrationID;

        // In your Device Provisioning Service please go to "Manage enrollments" and select "Individual Enrollments".
        // Select "Add individual enrollment" then fill in the following:
        // Mechanism: X.509
        // Certificate: 
        //    You can generate a self-signed certificate by running the GenerateTestCertificate.ps1 powershell script.
        //    Select the public key 'certificate.cer' file. ('certificate.pfx' contains the private key and is password protected.)
        //    For production code, it is advised that you install the certificate in the CurrentUser (My) store.
        // DeviceID: iothubx509device1

        // X.509 certificates may also be used for enrollment groups.
        // In your Device Provisioning Service please go to "Manage enrollments" and select "Enrollment Groups".
        // Select "Add enrollment group" then fill in the following:
        // Group name: <your  group name>
        // Attestation Type: Certificate
        // Certificate Type: 
        //    choose CA certificate then link primary and secondary certificates 
        //    OR choose Intermediate certificate and upload primary and secondary certificate files
        // You may also change other enrollemtn group parameters according to your needs
        private static string s_certificateFileName;

        public static int Main(string[] args)
        {
            if (string.IsNullOrWhiteSpace(s_idScope) && (args.Length > 0))
            {
                s_idScope = args[0];
            }

            if (string.IsNullOrWhiteSpace(s_idScope))
            {
                Console.WriteLine("ProvisioningDeviceClientSymmetricKey <IDScope> <registrationID>");
                return 1;
            }

            if (string.IsNullOrWhiteSpace(s_registrationID) && (args.Length > 1))
            {
                s_registrationID = args[1];               
                s_certificateFileName = s_registrationID + ".pfx";
            }
            if (string.IsNullOrWhiteSpace(s_registrationID))
            {
                Console.WriteLine("ProvisioningDeviceClientSymmetricKey <IDScope> <registrationID>");
                return 1;
            }

            //X509Certificate2 certificate = LoadProvisioningCertificate();

            var myCertificate = new X509Certificate2(s_certificateFileName, "1234");
            var myChain = new X509Certificate2Collection();

            //myChain.Import("azure-iot-test-only.chain.ca.cert.pem");

            using (var security = new SecurityProviderX509Certificate(myCertificate, myChain))

            // Select one of the available transports:
            // To optimize for size, reference only the protocols used by your application.
            using (var transport = new ProvisioningTransportHandlerAmqp(TransportFallbackType.TcpOnly))
            // using (var transport = new ProvisioningTransportHandlerHttp())
            // using (var transport = new ProvisioningTransportHandlerMqtt(TransportFallbackType.TcpOnly))
            // using (var transport = new ProvisioningTransportHandlerMqtt(TransportFallbackType.WebSocketOnly))
            {
                ProvisioningDeviceClient provClient =
                    ProvisioningDeviceClient.Create(GlobalDeviceEndpoint, s_idScope, security, transport);

                var sample = new ProvisioningDeviceClientSample(provClient, security);
                sample.RunSampleAsync().GetAwaiter().GetResult();
            }

            return 0;
        }

        private static X509Certificate2 LoadProvisioningCertificate()
        {
            string certificatePassword = ReadCertificatePassword();

            var certificateCollection = new X509Certificate2Collection();
            certificateCollection.Import(s_certificateFileName, certificatePassword, X509KeyStorageFlags.UserKeySet);

            X509Certificate2 certificate = null;

            foreach (X509Certificate2 element in certificateCollection)
            {
                Console.WriteLine($"Found certificate: {element?.Thumbprint} {element?.Subject}; PrivateKey: {element?.HasPrivateKey}");
                if (certificate == null && element.HasPrivateKey)
                {
                    certificate = element;
                }
                else
                {
                    element.Dispose();
                }
            }

            if (certificate == null)
            {
                throw new FileNotFoundException($"{s_certificateFileName} did not contain any certificate with a private key.");
            }
            else
            {
                Console.WriteLine($"Using certificate {certificate.Thumbprint} {certificate.Subject}");
            }

            return certificate;
        }

        private static string ReadCertificatePassword()
        {
            var password = new StringBuilder();
            Console.WriteLine($"Enter the PFX password for {s_certificateFileName}:");

            while(true)
            {
                ConsoleKeyInfo key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Backspace)
                {
                    if (password.Length > 0)
                    {
                        password.Remove(password.Length - 1, 1);
                        Console.Write("\b \b");
                    }
                }
                else if (key.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    break;
                }
                else
                {
                    Console.Write('*');
                    password.Append(key.KeyChar);
                }
            }

            return password.ToString();
        }
    }
}
