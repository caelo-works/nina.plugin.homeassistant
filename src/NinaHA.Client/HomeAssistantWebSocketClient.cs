using System;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NinaHA.Client.Dto;

namespace NinaHA.Client {

    /// <summary>
    /// Connects to the Home Assistant WebSocket API, authenticates with a long-lived token,
    /// subscribes to <c>state_changed</c> events and raises <see cref="StateChanged"/> for each one.
    /// Runs a background receive loop with automatic reconnection (exponential backoff).
    /// </summary>
    public sealed class HomeAssistantWebSocketClient : IDisposable {

        private readonly Uri socketUri;
        private readonly string token;
        private readonly Func<ClientWebSocket>? socketFactory;

        private CancellationTokenSource? lifetimeCts;
        private Task? runLoop;
        private int messageId;

        /// <summary>Raised with the new state for each <c>state_changed</c> event.</summary>
        public event Action<HaState>? StateChanged;

        /// <summary>Raised when the authenticated subscription becomes active or is lost.</summary>
        public event Action<bool>? ConnectionStateChanged;

        public bool IsConnected { get; private set; }

        public HomeAssistantWebSocketClient(string baseUrl, string token, Func<ClientWebSocket>? socketFactory = null) {
            if (string.IsNullOrWhiteSpace(baseUrl)) {
                throw new ArgumentException("Base URL is required.", nameof(baseUrl));
            }
            this.token = token;
            this.socketFactory = socketFactory;
            this.socketUri = BuildSocketUri(baseUrl);
        }

        internal static Uri BuildSocketUri(string baseUrl) {
            var b = new UriBuilder(baseUrl.Trim().TrimEnd('/'));
            b.Scheme = b.Scheme == "https" ? "wss" : "ws";
            b.Path = b.Path.TrimEnd('/') + "/api/websocket";
            return b.Uri;
        }

        /// <summary>Starts the background connection/receive loop.</summary>
        public void Start() {
            if (runLoop != null) {
                return;
            }
            lifetimeCts = new CancellationTokenSource();
            runLoop = Task.Run(() => RunAsync(lifetimeCts.Token));
        }

        private async Task RunAsync(CancellationToken ct) {
            var backoff = TimeSpan.FromSeconds(1);
            var maxBackoff = TimeSpan.FromSeconds(30);

            while (!ct.IsCancellationRequested) {
                ClientWebSocket? socket = null;
                try {
                    socket = socketFactory?.Invoke() ?? new ClientWebSocket();
                    await socket.ConnectAsync(socketUri, ct).ConfigureAwait(false);
                    await AuthenticateAndSubscribeAsync(socket, ct).ConfigureAwait(false);

                    backoff = TimeSpan.FromSeconds(1);
                    SetConnected(true);
                    await ReceiveLoopAsync(socket, ct).ConfigureAwait(false);
                } catch (OperationCanceledException) {
                    break;
                } catch (Exception) {
                    // Swallow and retry; REST polling is the fallback in the meantime.
                } finally {
                    SetConnected(false);
                    socket?.Dispose();
                }

                if (ct.IsCancellationRequested) {
                    break;
                }
                try {
                    await Task.Delay(backoff, ct).ConfigureAwait(false);
                } catch (OperationCanceledException) {
                    break;
                }
                backoff = TimeSpan.FromTicks(Math.Min(backoff.Ticks * 2, maxBackoff.Ticks));
            }
        }

        private async Task AuthenticateAndSubscribeAsync(ClientWebSocket socket, CancellationToken ct) {
            // Server first sends { "type": "auth_required" }
            var hello = await ReceiveJsonAsync(socket, ct).ConfigureAwait(false);
            if (GetType(hello) != "auth_required") {
                throw new InvalidOperationException("Unexpected handshake from Home Assistant.");
            }

            await SendAsync(socket, new { type = "auth", access_token = token }, ct).ConfigureAwait(false);

            var authResult = await ReceiveJsonAsync(socket, ct).ConfigureAwait(false);
            if (GetType(authResult) != "auth_ok") {
                throw new InvalidOperationException("Home Assistant rejected the access token.");
            }

            var id = Interlocked.Increment(ref messageId);
            await SendAsync(socket, new { id, type = "subscribe_events", event_type = "state_changed" }, ct).ConfigureAwait(false);
        }

        private async Task ReceiveLoopAsync(ClientWebSocket socket, CancellationToken ct) {
            while (socket.State == WebSocketState.Open && !ct.IsCancellationRequested) {
                using var doc = await ReceiveJsonAsync(socket, ct).ConfigureAwait(false);
                if (doc == null) {
                    break;
                }
                if (GetType(doc) != "event") {
                    continue;
                }
                if (!doc.RootElement.TryGetProperty("event", out var ev) ||
                    !ev.TryGetProperty("data", out var data) ||
                    !data.TryGetProperty("new_state", out var newState) ||
                    newState.ValueKind != JsonValueKind.Object) {
                    continue;
                }
                var state = newState.Deserialize<HaState>();
                if (state != null && !string.IsNullOrEmpty(state.EntityId)) {
                    StateChanged?.Invoke(state);
                }
            }
        }

        private static string GetType(JsonDocument? doc) =>
            doc != null && doc.RootElement.TryGetProperty("type", out var t) && t.ValueKind == JsonValueKind.String
                ? t.GetString() ?? string.Empty
                : string.Empty;

        private static async Task SendAsync(ClientWebSocket socket, object payload, CancellationToken ct) {
            var json = JsonSerializer.SerializeToUtf8Bytes(payload);
            await socket.SendAsync(json, WebSocketMessageType.Text, endOfMessage: true, ct).ConfigureAwait(false);
        }

        private static async Task<JsonDocument?> ReceiveJsonAsync(ClientWebSocket socket, CancellationToken ct) {
            var buffer = new byte[8192];
            using var ms = new System.IO.MemoryStream();
            WebSocketReceiveResult result;
            do {
                result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), ct).ConfigureAwait(false);
                if (result.MessageType == WebSocketMessageType.Close) {
                    return null;
                }
                ms.Write(buffer, 0, result.Count);
            } while (!result.EndOfMessage);

            if (ms.Length == 0) {
                return null;
            }
            ms.Position = 0;
            return await JsonDocument.ParseAsync(ms, cancellationToken: ct).ConfigureAwait(false);
        }

        private void SetConnected(bool connected) {
            if (IsConnected == connected) {
                return;
            }
            IsConnected = connected;
            ConnectionStateChanged?.Invoke(connected);
        }

        public void Dispose() {
            try {
                lifetimeCts?.Cancel();
            } catch (ObjectDisposedException) {
                // already disposed
            }
            try {
                runLoop?.Wait(TimeSpan.FromSeconds(2));
            } catch (Exception) {
                // best effort shutdown
            }
            lifetimeCts?.Dispose();
        }
    }
}
