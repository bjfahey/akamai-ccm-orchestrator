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

using Newtonsoft.Json;
using System;

namespace Keyfactor.Orchestrator.Extensions.AkamaiCcmOrchestrator.Models
{
    /// <summary>
    /// Represents a certificate in Akamai Cloud Certificate Manager (CCM)
    /// </summary>
    public class CcmCertificate
    {
        [JsonProperty("certificateId")]
        public string CertificateId { get; set; } = string.Empty;

        [JsonProperty("certificateName")]
        public string CertificateName { get; set; } = string.Empty;

        [JsonProperty("certificateType")]
        public string CertificateType { get; set; } = string.Empty;

        [JsonProperty("status")]
        public string Status { get; set; } = string.Empty;

        [JsonProperty("keyType")]
        public string KeyType { get; set; } = string.Empty;

        [JsonProperty("keySize")]
        public string KeySize { get; set; } = string.Empty;

        [JsonProperty("secureNetwork")]
        public string SecureNetwork { get; set; } = string.Empty;

        [JsonProperty("sans")]
        public string[]? Sans { get; set; }

        [JsonProperty("subject")]
        public CertificateSubject? Subject { get; set; }

        [JsonProperty("csrPem")]
        public string? CsrPem { get; set; }

        [JsonProperty("signedCertificatePem")]
        public string? SignedCertificatePem { get; set; }

        [JsonProperty("trustChainPem")]
        public string? TrustChainPem { get; set; }

        [JsonProperty("signedCertificateNotValidAfterDate")]
        public DateTime? NotValidAfter { get; set; }

        [JsonProperty("createdBy")]
        public string? CreatedBy { get; set; }

        [JsonProperty("createdDate")]
        public DateTime? CreatedDate { get; set; }
    }

    public class CertificateSubject
    {
        [JsonProperty("commonName")]
        public string CommonName { get; set; } = string.Empty;

        [JsonProperty("organization")]
        public string? Organization { get; set; }

        [JsonProperty("organizationUnit")]
        public string? OrganizationUnit { get; set; }

        [JsonProperty("locality")]
        public string? Locality { get; set; }

        [JsonProperty("state")]
        public string? State { get; set; }

        [JsonProperty("country")]
        public string? Country { get; set; }
    }
}
