using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NinaHA.Client;
using Xunit;

namespace NinaHA.Client.Tests {

    public class HomeAssistantRestClientTests {

        private sealed class StubHandler : HttpMessageHandler {
            private readonly Func<HttpRequestMessage, HttpResponseMessage> responder;
            public HttpRequestMessage? LastRequest { get; private set; }
            public string? LastBody { get; private set; }

            public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) {
                this.responder = responder;
            }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
                LastRequest = request;
                if (request.Content != null) {
                    LastBody = await request.Content.ReadAsStringAsync(cancellationToken);
                }
                return responder(request);
            }
        }

        [Fact]
        public async Task GetStatesAsync_parses_entities() {
            var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) {
                Content = new StringContent("[{\"entity_id\":\"switch.a\",\"state\":\"on\",\"attributes\":{\"friendly_name\":\"A\"}}]")
            });
            using var client = new HomeAssistantRestClient("http://ha.local:8123", "tok", new HttpClient(handler));

            var states = await client.GetStatesAsync();

            Assert.Single(states);
            Assert.Equal("switch.a", states[0].EntityId);
            Assert.Equal("on", states[0].State);
            Assert.Equal("A", states[0].FriendlyName);
        }

        [Fact]
        public async Task CallServiceAsync_posts_to_correct_url_with_bearer() {
            var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) {
                Content = new StringContent("[]")
            });
            using var client = new HomeAssistantRestClient("http://ha.local:8123", "tok", new HttpClient(handler));

            await client.CallServiceAsync("switch", "turn_on", new Dictionary<string, object?> { ["entity_id"] = "switch.a" });

            Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
            Assert.Equal("http://ha.local:8123/api/services/switch/turn_on", handler.LastRequest!.RequestUri!.ToString());
            Assert.Equal("Bearer", handler.LastRequest!.Headers.Authorization!.Scheme);
            Assert.Equal("tok", handler.LastRequest!.Headers.Authorization!.Parameter);
            Assert.Contains("switch.a", handler.LastBody);
        }

        [Fact]
        public async Task GetStateAsync_returns_null_on_404() {
            var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
            using var client = new HomeAssistantRestClient("http://ha.local:8123", "tok", new HttpClient(handler));

            Assert.Null(await client.GetStateAsync("switch.missing"));
        }

        [Fact]
        public async Task PingAsync_true_on_success_false_on_error() {
            var ok = new HomeAssistantRestClient("http://ha.local:8123", "tok",
                new HttpClient(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK))));
            Assert.True(await ok.PingAsync());

            var bad = new HomeAssistantRestClient("http://ha.local:8123", "tok",
                new HttpClient(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized))));
            Assert.False(await bad.PingAsync());
        }
    }
}
