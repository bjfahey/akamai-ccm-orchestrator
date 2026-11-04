// Copyright 2026 Keyfactor
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Keyfactor.Logging;
using Keyfactor.Orchestrators.Extensions;
using Keyfactor.Orchestrator.Extensions.AkamaiCcmOrchestrator.Models;
using Keyfactor.Orchestrator.Extensions.AkamaiCcmOrchestrator.Factories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Org.BouncyCastle.Pkcs;

namespace Keyfactor.Orchestrator.Extensions.AkamaiCcmOrchestrator.Jobs
{
    /// <summary>
    /// Management job for Akamai CCM - handles certificate reenrollment
    /// Workflow:
    /// 1. Create THIRD_PARTY enrollment in CCM (generates CSR)
    /// 2. Extract CSR from CCM
    /// 3. Use certificate from Keyfactor Command (already signed)
    /// 4. Upload signed certificate + trust chain to CCM via PATCH
    /// </summary>
    public class Management : CcmJob, IManagementJobExtension
    {
        public string ExtensionName => "AkamaiCcm";

        private readonly ILogger _logger;
        private readonly ICcmClientFactory _clientFactory;

        public Management()
        {
            _logger = LogHandler.GetClassLogger<Management>();
            _clientFactory = new CcmClientFactory();
        }

        public Management(ILogger logger, ICcmClientFactory clientFactory)
        {
            _logger = logger;
            _clientFactory = clientFactory;
        }

        public JobResult ProcessJob(ManagementJobConfiguration jobConfiguration)
        {
            JobHistoryId = jobConfiguration.JobHistoryId;
            ICcmClient client;
            Dictionary<string, string> storeProps;

            try
            {
                _logger.LogTrace("Reading store properties for Akamai CCM auth information.");
                storeProps = JsonConvert.DeserializeObject<Dictionary<string, string>>(jobConfiguration.CertificateStoreDetails.Properties);

                if (storeProps == null)
                {
                    throw new Exception("Failed to deserialize store properties");
                }

                _logger.LogTrace("Creating Akamai CCM Client.");
                client = _clientFactory.Create(_logger, storeProps);
                _logger.LogDebug("Successfully created Akamai CCM client");
            }
            catch (Exception e)
            {
                _logger.LogError("Error occurred while setting up Akamai CCM Client.");
                string errorMessage = FlattenException(e);
                _logger.LogError(errorMessage);
                return Failure(errorMessage);
            }

            try
            {
                var jobCert = new X509Certificate2(Convert.FromBase64String(jobConfiguration.JobCertificate.Contents));
                _logger.LogInformation($"Processing certificate: {jobCert.Subject}");

                // Parse SANs from certificate
                var sans = new List<string>();
                var sanExtension = jobCert.Extensions.Cast<X509Extension>()
                    .FirstOrDefault(e => e.Oid?.Value == "2.5.29.17");

                if (sanExtension != null)
                {
                    var asnData = new System.Security.Cryptography.AsnEncodedData(sanExtension.Oid!, sanExtension.RawData);
                    var sanString = asnData.Format(false);
                    sans = sanString.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Replace("DNS Name=", "").Trim())
                        .ToList();
                }

                // Extract CN from subject
                var cn = jobCert.GetNameInfo(X509NameType.SimpleName, false);
                if (!sans.Contains(cn))
                {
                    sans.Insert(0, cn);
                }

                _logger.LogDebug($"Certificate CN: {cn}, SANs: {string.Join(", ", sans)}");

                // Check if we're updating an existing certificate or creating a new one
                string certificateId = jobConfiguration.JobCertificate.Alias;
                CcmCertificate ccmCert;

                if (string.IsNullOrEmpty(certificateId))
                {
                    // Create new THIRD_PARTY enrollment
                    _logger.LogInformation("Creating new CCM THIRD_PARTY certificate enrollment");

                    var createRequest = new CcmCreateCertificateRequest
                    {
                        CertificateName = $"{cn} - {DateTime.UtcNow:yyyy-MM-dd}",
                        CertificateType = "THIRD_PARTY",
                        Sans = sans.ToArray(),
                        Subject = new CertificateSubject
                        {
                            CommonName = cn
                        },
                        KeyType = jobCert.PublicKey.Oid.FriendlyName?.Contains("RSA") == true ? "RSA" : "EC",
                        KeySize = jobCert.PublicKey.Key.KeySize.ToString(),
                        SecureNetwork = "ENHANCED_TLS"
                    };

                    ccmCert = client.CreateCertificateAsync(createRequest).Result;
                    certificateId = ccmCert.CertificateId;
                    _logger.LogInformation($"Created CCM certificate {certificateId}");
                }
                else
                {
                    // Get existing certificate
                    _logger.LogInformation($"Updating existing CCM certificate {certificateId}");
                    ccmCert = client.GetCertificateAsync(certificateId).Result;
                }

                // Upload signed certificate + trust chain
                _logger.LogInformation($"Uploading signed certificate to CCM certificate {certificateId}");

                var signedCertPem = ConvertToPem(jobCert.RawData, "CERTIFICATE");
                var trustChainPem = BuildTrustChain(jobConfiguration.JobCertificate.Contents);

                var updatedCert = client.UploadSignedCertificateAsync(certificateId, signedCertPem, trustChainPem).Result;

                _logger.LogInformation($"Successfully uploaded certificate to CCM. Status: {updatedCert.Status}");
                return Success($"Certificate {certificateId} successfully enrolled in Akamai CCM");
            }
            catch (Exception e)
            {
                _logger.LogError("Error occurred during certificate management operation.");
                string errorMessage = FlattenException(e);
                _logger.LogError(errorMessage);
                return Failure(errorMessage);
            }
        }

        private string ConvertToPem(byte[] certData, string type)
        {
            var base64 = Convert.ToBase64String(certData);
            var sb = new StringBuilder();
            sb.AppendLine($"-----BEGIN {type}-----");

            for (int i = 0; i < base64.Length; i += 64)
            {
                sb.AppendLine(base64.Substring(i, Math.Min(64, base64.Length - i)));
            }

            sb.AppendLine($"-----END {type}-----");
            return sb.ToString();
        }

        private string BuildTrustChain(string certContents)
        {
            try
            {
                var certBytes = Convert.FromBase64String(certContents);
                var parser = new Pkcs12StoreBuilder().Build();
                parser.Load(new System.IO.MemoryStream(certBytes), Array.Empty<char>());

                var sb = new StringBuilder();
                foreach (string alias in parser.Aliases)
                {
                    var chain = parser.GetCertificateChain(alias);
                    if (chain != null && chain.Length > 1)
                    {
                        // Skip first cert (leaf), include the rest (chain)
                        for (int i = 1; i < chain.Length; i++)
                        {
                            var chainCert = chain[i].Certificate;
                            sb.Append(ConvertToPem(chainCert.GetEncoded(), "CERTIFICATE"));
                        }
                    }
                }

                var trustChain = sb.ToString();
                if (string.IsNullOrEmpty(trustChain))
                {
                    _logger.LogWarning("No trust chain found in certificate contents. Certificate may be uploaded without intermediate certificates.");
                }

                return trustChain;
            }
            catch (Exception ex)
            {
                // Log warning when trust chain extraction fails
                _logger.LogWarning($"Failed to extract trust chain from certificate contents: {ex.Message}. Uploading certificate without trust chain.");
                return string.Empty;
            }
        }
    }
}
