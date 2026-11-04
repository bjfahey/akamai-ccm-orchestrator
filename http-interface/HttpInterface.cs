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
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Keyfactor.Extensions.Utilities.HttpInterface
{
    public interface IHttpInterface
    {
        Task<T> GetAsync<T>(string path);
        Task<T> PostAsync<T>(string path, object body);
        Task<T> PatchAsync<T>(string path, object body, string contentType = "application/json");
        Task<T> DeleteAsync<T>(string path);
    }

    public class HttpInterface : IHttpInterface
    {
        private readonly HttpClient _httpClient;
        private readonly AkamaiAuth _akamaiAuth;
        private readonly string _host;
        private readonly ILogger _logger;
        private const int DefaultTimeoutSeconds = 60;

        public HttpInterface(ILogger logger, Dictionary<string, string> jobProperties)
        {
            _logger = logger;
            _host = jobProperties["host"];

            // Validate host against Akamai API hostname patterns to prevent SSRF
            if (!IsValidAkamaiHost(_host))
            {
                throw new ArgumentException($"Invalid Akamai API host. Must match *.luna.akamaiapis.net or *.edgegrid.net pattern.");
            }

            _akamaiAuth = new AkamaiAuth(jobProperties);

            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(DefaultTimeoutSeconds),
                BaseAddress = new Uri($"https://{_host}")
            };
        }

        private bool IsValidAkamaiHost(string host)
        {
            if (string.IsNullOrWhiteSpace(host))
                return false;

            // Validate against known Akamai API hostname patterns
            return host.EndsWith(".luna.akamaiapis.net", StringComparison.OrdinalIgnoreCase) ||
                   host.EndsWith(".edgegrid.net", StringComparison.OrdinalIgnoreCase);
        }

        public async Task<T> GetAsync<T>(string path)
        {
            return await SendRequestAsync<T>(HttpMethod.Get, path, null);
        }

        public async Task<T> PostAsync<T>(string path, object body)
        {
            return await SendRequestAsync<T>(HttpMethod.Post, path, body);
        }

        public async Task<T> PatchAsync<T>(string path, object body, string contentType = "application/json")
        {
            return await SendRequestAsync<T>(HttpMethod.Patch, path, body, contentType);
        }

        public async Task<T> DeleteAsync<T>(string path)
        {
            return await SendRequestAsync<T>(HttpMethod.Delete, path, null);
        }

        private async Task<T> SendRequestAsync<T>(HttpMethod method, string path, object? body, string contentType = "application/json")
        {
            var request = new HttpRequestMessage(method, path);
            request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            string? bodyString = null;
            if (body != null)
            {
                bodyString = JsonConvert.SerializeObject(body);
                request.Content = new StringContent(bodyString, Encoding.UTF8, contentType);
            }

            var authHeader = _akamaiAuth.GenerateAuthHeader(method.Method, _host, path, bodyString);
            request.Headers.Authorization = authHeader;

            _logger.LogDebug($"[HTTP {method}] {SanitizePath(path)}");

            var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                // Log full details at trace level only to avoid exposing sensitive data in logs
                _logger.LogTrace($"[HTTP {method}] {SanitizePath(path)} failed with status {response.StatusCode}. Response: {responseContent}");
                _logger.LogError($"[HTTP {method}] {SanitizePath(path)} failed with status {response.StatusCode}");

                // Return sanitized error message without exposing API response details
                throw new HttpRequestException($"HTTP {method} request failed with status {response.StatusCode}");
            }

            _logger.LogDebug($"[HTTP {method}] {SanitizePath(path)} succeeded with status {response.StatusCode}");

            if (typeof(T) == typeof(string))
            {
                return (T)(object)responseContent;
            }

            return JsonConvert.DeserializeObject<T>(responseContent)!;
        }

        private string SanitizePath(string path)
        {
            // Remove sensitive query parameters from logs (accountSwitchKey, tokens, etc.)
            if (path.Contains("?"))
            {
                var parts = path.Split('?');
                return $"{parts[0]}?<query-params-redacted>";
            }
            return path;
        }
    }
}
