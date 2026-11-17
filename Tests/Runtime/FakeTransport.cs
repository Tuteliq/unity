using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Tuteliq.Tests
{
    /// <summary>
    /// Records requests and answers them from a queue, so no test touches the network.
    /// </summary>
    internal sealed class FakeTransport : ITransport
    {
        public readonly List<TransportRequest> Requests = new List<TransportRequest>();
        private readonly Queue<TransportResponse> _responses = new Queue<TransportResponse>();

        public FakeTransport Respond(int status, string body, params (string Name, string Value)[] headers)
        {
            var response = new TransportResponse { StatusCode = status, Body = body };
            foreach (var (name, value) in headers) response.Headers[name] = value;
            _responses.Enqueue(response);
            return this;
        }

        public FakeTransport FailWithNetworkError(string error = "Cannot resolve destination host")
        {
            _responses.Enqueue(new TransportResponse { TransportError = error });
            return this;
        }

        public Task<TransportResponse> SendAsync(TransportRequest request)
        {
            Requests.Add(request);
            var response = _responses.Count > 0 ? _responses.Dequeue() : new TransportResponse { StatusCode = 200, Body = "{}" };
            return Task.FromResult(response);
        }

        public TransportRequest Last => Requests[Requests.Count - 1];

        public Dictionary<string, object> LastBody => (Dictionary<string, object>)MiniJson.Deserialize(Last.JsonBody);
    }

    internal static class TestClient
    {
        public const string ApiKey = "test-api-key-12345";

        /// <summary>
        /// A client on a fake transport whose retry waits are recorded instead of slept.
        /// </summary>
        public static TuteliqClient Create(FakeTransport transport, List<TimeSpan> waits = null, int maxRetries = 3)
        {
            return new TuteliqClient(ApiKey, transport, wait =>
            {
                waits?.Add(wait);
                return Task.CompletedTask;
            }, maxRetries: maxRetries);
        }

        /// <summary>
        /// Unity's test runner has no async tests; the fake transport completes synchronously.
        /// </summary>
        public static T Run<T>(Task<T> task) => task.GetAwaiter().GetResult();
    }
}
