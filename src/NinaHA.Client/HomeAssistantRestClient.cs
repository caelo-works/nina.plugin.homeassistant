using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NinaHA.Client.Dto;

namespace NinaHA.Client {

    /// <summary>
    /// Minimal REST client for the Home Assistant HTTP API.
    /// Uses a long-lived access token as a bearer token. Thread-safe for concurrent reads/writes.
    /// </summary>
    public sealed class HomeAssistantRestClient : IDisposable {

        private readonly HttpClient httpClient;
        private readonly bool ownsHttpClient;

        /// <summary>
        /// Creates a client for the given base URL and token.
        /// </summary>
        /// <param name="baseUrl">e.g. <c>http://homeassistant.local:8123</c> (with or without trailing slash).</param>
        /// <param name="token">Long-lived access token.</param>
        /// <param name="httpClient">Optional injected client (e.g. for tests). When null, one is created and owned.</param>
        public HomeAssistantRestClient(string baseUrl, string token, HttpClient? httpClient = null) {
            if (string.IsNullOrWhiteSpace(baseUrl)) {
                throw new ArgumentException("Base URL is required.", nameof(baseUrl));
            }

            ownsHttpClient = httpClient == null;
            this.httpClient = httpClient ?? new HttpClient();
            this.httpClient.BaseAddress = new Uri(NormalizeBase(baseUrl));
            this.httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        private static string NormalizeBase(string baseUrl) {
            var trimmed = baseUrl.Trim().TrimEnd('/');
            return trimmed + "/";
        }

        /// <summary>Pings <c>GET /api/</c> to validate the URL and token. Returns true on HTTP 200.</summary>
        public async Task<bool> PingAsync(CancellationToken ct = default) {
            try {
                using var resp = await httpClient.GetAsync("api/", ct).ConfigureAwait(false);
                return resp.IsSuccessStatusCode;
            } catch (HttpRequestException) {
                return false;
            } catch (TaskCanceledException) {
                return false;
            }
        }

        /// <summary>Reads all entity states via <c>GET /api/states</c>.</summary>
        public async Task<IReadOnlyList<HaState>> GetStatesAsync(CancellationToken ct = default) {
            using var resp = await httpClient.GetAsync("api/states", ct).ConfigureAwait(false);
            resp.EnsureSuccessStatusCode();
            var states = await resp.Content.ReadFromJsonAsync<List<HaState>>(cancellationToken: ct).ConfigureAwait(false);
            return states ?? new List<HaState>();
        }

        /// <summary>Reads a single entity state via <c>GET /api/states/&lt;entity_id&gt;</c>. Returns null on 404.</summary>
        public async Task<HaState?> GetStateAsync(string entityId, CancellationToken ct = default) {
            using var resp = await httpClient.GetAsync("api/states/" + Uri.EscapeDataString(entityId), ct).ConfigureAwait(false);
            if (resp.StatusCode == System.Net.HttpStatusCode.NotFound) {
                return null;
            }
            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadFromJsonAsync<HaState>(cancellationToken: ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Calls a service via <c>POST /api/services/&lt;domain&gt;/&lt;service&gt;</c>.
        /// </summary>
        /// <param name="data">Service payload (typically includes <c>entity_id</c> and parameters).</param>
        public async Task CallServiceAsync(string domain, string service, IReadOnlyDictionary<string, object?> data, CancellationToken ct = default) {
            var url = $"api/services/{Uri.EscapeDataString(domain)}/{Uri.EscapeDataString(service)}";
            using var content = JsonContent.Create(data);
            using var resp = await httpClient.PostAsync(url, content, ct).ConfigureAwait(false);
            resp.EnsureSuccessStatusCode();
        }

        public void Dispose() {
            if (ownsHttpClient) {
                httpClient.Dispose();
            }
        }
    }
}
