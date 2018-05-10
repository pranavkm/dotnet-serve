// Copyright (c) Nate McMaster.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;

namespace McMaster.DotNet.Serve
{
    class CertificateLoader
    {
        // see https://github.com/aspnet/Common/blob/61320f4ecc1a7b60e76ca8fe05cd86c98778f92c/shared/Microsoft.AspNetCore.Certificates.Generation.Sources/CertificateManager.cs#L19-L20
        // This is the unique OID for the developer cert generated by VS and the .NET Core CLI
        private const string AspNetHttpsOid = "1.3.6.1.4.1.311.84.1.1";
        private const string AspNetHttpsOidFriendlyName = "ASP.NET Core HTTPS development certificate";

        public const string DefaultCertPfxFileName = "cert.pfx";

        public static X509Certificate2 LoadCertificate(CommandLineOptions options, string currentDirectory)
        {
            if (!options.UseTls)
            {
                return null;
            }

            var retVal = FindCertificate(options, currentDirectory);

            if (retVal == null)
            {
                throw new InvalidOperationException("Could not find a certificate to use for HTTPS connections");
            }

            return retVal;
        }

        private static X509Certificate2 FindCertificate(CommandLineOptions options, string currentDirectory)
        {
            if (!string.IsNullOrEmpty(options.CertPfxPath))
            {
                options.ExcludedFiles.Add(options.CertPfxPath);
                return LoadFromPfxFile(options.CertPfxPath, options.CertificatePassword);
            }

            var defaultPfxFile = Path.Combine(currentDirectory, DefaultCertPfxFileName);
            if (File.Exists(defaultPfxFile))
            {
                options.ExcludedFiles.Add(defaultPfxFile);
                return LoadFromPfxFile(defaultPfxFile, options.CertificatePassword);
            }

            if (options.ShouldUseLocalhost())
            {
                return LoadDeveloperCertificate();
            }

            return null;
        }

        private static X509Certificate2 LoadFromPfxFile(string filepath, string password)
        {
            try
            {
                return new X509Certificate2(filepath, password);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to load certificate file from '{filepath}'", ex);
            }
        }

        private static X509Certificate2 LoadDeveloperCertificate()
        {
            using (var store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
            {
                store.Open(OpenFlags.ReadOnly);
                var certs = store.Certificates.Find(X509FindType.FindByExtension, AspNetHttpsOid, validOnly: false);
                if (certs.Count == 1)
                {
                    return certs[0];
                }

                if (certs.Count > 1)
                {
                    throw new InvalidOperationException($"Ambiguous certficiate match. Multiple certificates found with extension '{AspNetHttpsOid}' ({AspNetHttpsOidFriendlyName}).");
                }

                return null;
            }
        }
    }
}