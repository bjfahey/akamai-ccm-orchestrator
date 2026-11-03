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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Keyfactor.Extensions.Utilities.HttpInterface;
using Microsoft.Extensions.Logging;

namespace Keyfactor.Orchestrator.Extensions.AkamaiCcmOrchestrator.Models
{
    /// <summary>
    /// Client for Akamai Cloud Certificate Manager (CCM) API v2
    /// Base path: /ccm/v2
    /// Required query params: contractId, groupId (+ optional accountSwitchKey)
    /// </summary>
    public class CcmClient : ICcmClient
    {
        private readonly IHttpInterface _httpClient;
        private readonly ILogger _logger;
        private readonly string _contractId;
        private readonly string _groupId;
        private readonly string? _accountSwitchKey;

        private const string API_BASE = "/ccm/v2";

        public CcmClient(ILogger logger, Dictionary<string, string> storeProperties)
        {
            _logger = logger;
            _httpClient = new HttpInterface(_logger, storeProperties);

            _contractId = storeProperties.ContainsKey("ContractId") ? storeProperties["ContractId"] : string.Empty;
            _groupId = storeProperties.ContainsKey("GroupId") ? storeProperties["GroupId"] : string.Empty;
            _accountSwitchKey = storeProperties.ContainsKey("AccountSwitchKey") ? storeProperties["AccountSwitchKey"] : null;

            if (string.IsNullOrEmpty(_contractId) || string.IsNullOrEmpty(_groupId))
            {
                throw new ArgumentException("ContractId and GroupId are required for CCM API");
            }
        }

        private string BuildPath(string subPath, Dictionary<string, string>? extraParams = null)
        {
            var queryParams = new Dictionary<string, string>
            {
                { "contractId", _contractId },
                { "groupId", _groupId }
            };

            if (!string.IsNullOrEmpty(_accountSwitchKey))
            {
                queryParams["accountSwitchKey"] = _accountSwitchKey;
            }

            if (extraParams != null)
            {
                foreach (var kvp in extraParams)
                {
                    queryParams[kvp.Key] = kvp.Value;
                }
            }

            var queryString = string.Join("&", queryParams.Select(kvp =>
                $"{HttpUtility.UrlEncode(kvp.Key)}={HttpUtility.UrlEncode(kvp.Value)}"));

            return $"{API_BASE}{subPath}?{queryString}";
        }

        public async Task<CcmCertificate[]> ListCertificatesAsync()
        {
            _logger.LogDebug("Listing CCM certificates");
            var path = BuildPath("/certificates");
            var response = await _httpClient.GetAsync<CcmCertificateListResponse>(path);
            return response.Certificates ?? Array.Empty<CcmCertificate>();
        }

        public async Task<CcmCertificate> GetCertificateAsync(string certificateId)
        {
            _logger.LogDebug($"Getting CCM certificate {certificateId}");
            var path = BuildPath($"/certificates/{HttpUtility.UrlEncode(certificateId)}");
            return await _httpClient.GetAsync<CcmCertificate>(path);
        }

        public async Task<CcmCertificate> CreateCertificateAsync(CcmCreateCertificateRequest request)
        {
            _logger.LogDebug($"Creating CCM certificate: {request.CertificateName}");
            var path = BuildPath("/certificates");
            return await _httpClient.PostAsync<CcmCertificate>(path, request);
        }

        public async Task<CcmCertificate> UploadSignedCertificateAsync(string certificateId, string signedCertificatePem, string trustChainPem)
        {
            _logger.LogDebug($"Uploading signed certificate to CCM certificate {certificateId}");
            var path = BuildPath($"/certificates/{HttpUtility.UrlEncode(certificateId)}");

            // RFC 6902 JSON Patch format
            var patchOperations = new[]
            {
                new CcmPatchOperation
                {
                    Op = "add",
                    Path = "/signedCertificatePem",
                    Value = signedCertificatePem
                },
                new CcmPatchOperation
                {
                    Op = "add",
                    Path = "/trustChainPem",
                    Value = trustChainPem
                }
            };

            return await _httpClient.PatchAsync<CcmCertificate>(path, patchOperations, "application/json-patch+json");
        }

        public async Task DeleteCertificateAsync(string certificateId)
        {
            _logger.LogDebug($"Deleting CCM certificate {certificateId}");
            var path = BuildPath($"/certificates/{HttpUtility.UrlEncode(certificateId)}");
            await _httpClient.DeleteAsync<object>(path);
        }
    }
}
