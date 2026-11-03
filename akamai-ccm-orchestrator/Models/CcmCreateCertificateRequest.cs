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

namespace Keyfactor.Orchestrator.Extensions.AkamaiCcmOrchestrator.Models
{
    /// <summary>
    /// Request body for creating a THIRD_PARTY certificate enrollment in CCM
    /// </summary>
    public class CcmCreateCertificateRequest
    {
        [JsonProperty("certificateName")]
        public string CertificateName { get; set; } = string.Empty;

        [JsonProperty("certificateType")]
        public string CertificateType { get; set; } = "THIRD_PARTY";

        [JsonProperty("sans")]
        public string[] Sans { get; set; } = Array.Empty<string>();

        [JsonProperty("subject")]
        public CertificateSubject Subject { get; set; } = new CertificateSubject();

        [JsonProperty("keyType")]
        public string KeyType { get; set; } = "RSA";

        [JsonProperty("keySize")]
        public string KeySize { get; set; } = "2048";

        [JsonProperty("secureNetwork")]
        public string SecureNetwork { get; set; } = "ENHANCED_TLS";
    }
}
