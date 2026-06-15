using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using NinaHA.Client.Dto;

namespace NinaHA.Client {

    /// <summary>
    /// Minimal REST client for the Home Assistant HTTP API. A long-lived access token is sent as a bearer
    /// token on every request. Instances are cheap: they share a single process-wide <see cref="HttpClient"/>
    /// (unless one is injected for testing), so they can be created per use without exhausting sockets.
    /// </summary>
    public sealed class HomeAssistantRestClient : IDisposable {

        private static readonly HttpClient SharedClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };

        private readonly HttpClient httpClient;
        private readonly Uri baseUri;
        private readonly string token;

        /// <param name="baseUrl">e.g. <c>http://homeassistant.local:8123</c> (with or without trailing slash).</param>
        /// <param name="token">Long-lived access token.</param>
        /// <param name="httpClient">Optional injected client (e.g. for tests). When null, a shared client is used.</param>
        public HomeAssistantRestClient(string baseUrl, string token, HttpClient? httpClient = null) {
            if (string.IsNullOrWhiteSpace(baseUrl)) {
                throw new ArgumentException("Base URL is required.", nameof(baseUrl));
            }
            this.token = token;
            this.httpClient = httpClient ?? SharedClient;
            baseUri = new Uri(baseUrl.Trim().TrimEnd('/') + "/");
        }

        private HttpRequestMessage Request(HttpMethod method, string relativePath) {
            var req = new HttpRequestMessage(method, new Uri(baseUri, relativePath));
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return req;
        }

        /// <summary>Pings <c>GET /api/</c> to validate the URL and token. Returns true on HTTP 200.</summary>
        public async Task<bool> PingAsync(CancellationToken ct = default) {
            try {
                using var req = Request(HttpMethod.Get, "api/");
                using var resp = await httpClient.SendAsync(req, ct).ConfigureAwait(false);
                return resp.IsSuccessStatusCode;
            } catch (HttpRequestException) {
                return false;
            } catch (TaskCanceledException) {
                return false;
            }
        }

        /// <summary>Reads all entity states via <c>GET /api/states</c>.</summary>
        public async Task<IReadOnlyList<HaState>> GetStatesAsync(CancellationToken ct = default) {
            using var req = Request(HttpMethod.Get, "api/states");
            using var resp = await httpClient.SendAsync(req, ct).ConfigureAwait(false);
            resp.EnsureSuccessStatusCode();
            var states = await resp.Content.ReadFromJsonAsync<List<HaState>>(cancellationToken: ct).ConfigureAwait(false);
            return states ?? new List<HaState>();
        }

        /// <summary>Reads a single entity state via <c>GET /api/states/&lt;entity_id&gt;</c>. Returns null on 404.</summary>
        public async Task<HaState?> GetStateAsync(string entityId, CancellationToken ct = default) {
            using var req = Request(HttpMethod.Get, "api/states/" + Uri.EscapeDataString(entityId));
            using var resp = await httpClient.SendAsync(req, ct).ConfigureAwait(false);
            if (resp.StatusCode == System.Net.HttpStatusCode.NotFound) {
                return null;
            }
            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadFromJsonAsync<HaState>(cancellationToken: ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Reads the available services via <c>GET /api/services</c> and returns them as a flat,
        /// sorted list of <c>domain.service</c> identifiers (e.g. <c>light.turn_on</c>).
        /// </summary>
        public async Task<IReadOnlyList<string>> GetServicesAsync(CancellationToken ct = default) {
            using var req = Request(HttpMethod.Get, "api/services");
            using var resp = await httpClient.SendAsync(req, ct).ConfigureAwait(false);
            resp.EnsureSuccessStatusCode();
            var json = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            return ParseServices(json);
        }

        public static IReadOnlyList<string> ParseServices(string json) {
            var result = new List<string>();
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != System.Text.Json.JsonValueKind.Array) {
                return result;
            }
            foreach (var domainEntry in doc.RootElement.EnumerateArray()) {
                if (!domainEntry.TryGetProperty("domain", out var domainEl) ||
                    !domainEntry.TryGetProperty("services", out var servicesEl) ||
                    servicesEl.ValueKind != System.Text.Json.JsonValueKind.Object) {
                    continue;
                }
                var domain = domainEl.GetString();
                if (string.IsNullOrEmpty(domain)) {
                    continue;
                }
                foreach (var svc in servicesEl.EnumerateObject()) {
                    result.Add($"{domain}.{svc.Name}");
                }
            }
            result.Sort(StringComparer.OrdinalIgnoreCase);
            return result;
        }

        /// <summary>Calls a service via <c>POST /api/services/&lt;domain&gt;/&lt;service&gt;</c>.</summary>
        public async Task CallServiceAsync(string domain, string service, IReadOnlyDictionary<string, object?> data, CancellationToken ct = default) {
            using var req = Request(HttpMethod.Post, $"api/services/{Uri.EscapeDataString(domain)}/{Uri.EscapeDataString(service)}");
            req.Content = JsonContent.Create(data);
            using var resp = await httpClient.SendAsync(req, ct).ConfigureAwait(false);
            resp.EnsureSuccessStatusCode();
        }

        public void Dispose() {
            // The shared client is process-wide; injected clients are owned by the caller. Nothing to dispose.
        }
    }
}
