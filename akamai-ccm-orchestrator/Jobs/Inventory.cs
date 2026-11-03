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
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Keyfactor.Orchestrator.Extensions.AkamaiCcmOrchestrator.Jobs
{
    public class Inventory : CcmJob, IInventoryJobExtension
    {
        public string ExtensionName => "AkamaiCcm";

        private readonly ILogger _logger;
        private readonly ICcmClientFactory _clientFactory;

        public Inventory()
        {
            _logger = LogHandler.GetClassLogger<Inventory>();
            _clientFactory = new CcmClientFactory();
        }

        public Inventory(ILogger logger, ICcmClientFactory clientFactory)
        {
            _logger = logger;
            _clientFactory = clientFactory;
        }

        public JobResult ProcessJob(InventoryJobConfiguration jobConfiguration, SubmitInventoryUpdate submitInventoryUpdate)
        {
            JobHistoryId = jobConfiguration.JobHistoryId;
            ICcmClient client;

            try
            {
                _logger.LogTrace("Reading store properties for Akamai CCM auth information.");
                var storeProps = JsonConvert.DeserializeObject<Dictionary<string, string>>(jobConfiguration.CertificateStoreDetails.Properties);

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

            var inventory = new List<CurrentInventoryItem>();
            CcmCertificate[] certificates;

            try
            {
                _logger.LogDebug("Fetching CCM certificates");
                certificates = client.ListCertificatesAsync().Result;
                _logger.LogDebug($"Found {certificates.Length} total CCM certificates.");
            }
            catch (Exception e)
            {
                _logger.LogError("Error occurred getting list of CCM certificates.");
                string errorMessage = FlattenException(e);
                _logger.LogError(errorMessage);
                return Failure(errorMessage);
            }

            try
            {
                foreach (var cert in certificates)
                {
                    _logger.LogDebug($"Processing certificate {cert.CertificateId} - {cert.CertificateName}");

                    // Only inventory certificates that have been signed
                    if (string.IsNullOrEmpty(cert.SignedCertificatePem))
                    {
                        _logger.LogTrace($"Certificate {cert.CertificateId} has no signed certificate yet, skipping");
                        continue;
                    }

                    var x509Cert = new X509Certificate2(Encoding.UTF8.GetBytes(cert.SignedCertificatePem));

                    var inventoryItem = new CurrentInventoryItem
                    {
                        Alias = cert.CertificateId,
                        Certificates = new[] { Convert.ToBase64String(x509Cert.RawData) },
                        ItemStatus = OrchestratorInventoryItemStatus.Unknown,
                        PrivateKeyEntry = false,
                        UseChainLevel = false
                    };

                    inventory.Add(inventoryItem);
                    _logger.LogTrace($"Added certificate {cert.CertificateId} to inventory");
                }

                _logger.LogInformation($"Submitting inventory with {inventory.Count} certificates");
                submitInventoryUpdate.Invoke(inventory);
                return Success();
            }
            catch (Exception e)
            {
                _logger.LogError("Error occurred while processing certificates.");
                string errorMessage = FlattenException(e);
                _logger.LogError(errorMessage);
                return Failure(errorMessage);
            }
        }
    }
}
