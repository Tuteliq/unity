using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Tuteliq.Tests
{
    public class ErrorEnvelopeTests
    {
        private static TuteliqException Fail(int status, string body, params (string, string)[] headers)
        {
            var transport = new FakeTransport().Respond(status, body, headers);
            var client = TestClient.Create(transport, maxRetries: 0);
            return Assert.Catch<TuteliqException>(() => TestClient.Run(client.DetectUnsafeAsync("text")));
        }

        [Test]
        public void Error_ReadsCodeAndMessageFromTheEnvelope()
        {
            var error = Fail(400, "{\"error\":{\"code\":\"VAL_4001\",\"message\":\"text is required\",\"details\":{\"field\":\"text\"}}}");

            Assert.IsInstanceOf<ValidationException>(error);
            Assert.AreEqual("VAL_4001", error.Code);
            Assert.AreEqual("text is required", error.Message);
            Assert.AreEqual(400, error.HttpStatus);
            Assert.IsNotNull(error.Details);
        }

        [Test]
        public void Error_429_CarriesRetryAfter()
        {
            var error = Fail(429, "{\"error\":{\"code\":\"RATE_2001\",\"message\":\"Rate limit exceeded\"}}", ("Retry-After", "7"));

            Assert.IsInstanceOf<RateLimitException>(error);
            Assert.AreEqual(7, ((RateLimitException)error).RetryAfterSeconds);
        }

        [TestCase("RATE_2002")]
        [TestCase("RATE_2003")]
        [TestCase("SUB_7004")]
        [TestCase("SUB_7005")]
        public void Error_429_WithUsedUpAllowance_IsQuotaExceeded(string code)
        {
            var error = Fail(429, $"{{\"error\":{{\"code\":\"{code}\",\"message\":\"used up\"}}}}");

            Assert.IsInstanceOf<QuotaExceededException>(error);
        }

        [Test]
        public void Error_WithoutEnvelope_StillHasAMessage()
        {
            var error = Fail(502, "<html>Bad gateway</html>");

            Assert.IsInstanceOf<ServerException>(error);
            StringAssert.Contains("502", error.Message);
        }

        [Test]
        public void ParseRetryAfter_IgnoresHttpDates()
        {
            Assert.IsNull(ApiErrors.ParseRetryAfter("Wed, 21 Oct 2026 07:28:00 GMT"));
            Assert.AreEqual(12, ApiErrors.ParseRetryAfter(" 12 "));
        }
    }

    public class RetryTests
    {
        private const string RateLimited = "{\"error\":{\"code\":\"RATE_2001\",\"message\":\"Rate limit exceeded\"}}";
        private const string Ok = "{\"unsafe\":false,\"recommended_action\":\"none\"}";

        [Test]
        public void Retry_429_WaitsForRetryAfter()
        {
            var waits = new List<TimeSpan>();
            var transport = new FakeTransport()
                .Respond(429, RateLimited, ("Retry-After", "7"))
                .Respond(200, Ok);

            TestClient.Run(TestClient.Create(transport, waits).DetectUnsafeAsync("text"));

            Assert.AreEqual(2, transport.Requests.Count);
            CollectionAssert.AreEqual(new[] { TimeSpan.FromSeconds(7) }, waits);
        }

        [Test]
        public void Retry_RetryAfter_IsCapped()
        {
            var waits = new List<TimeSpan>();
            var transport = new FakeTransport()
                .Respond(429, RateLimited, ("Retry-After", "3600"))
                .Respond(200, Ok);

            TestClient.Run(TestClient.Create(transport, waits).DetectUnsafeAsync("text"));

            CollectionAssert.AreEqual(new[] { RetryPolicy.MaxRetryAfter }, waits);
        }

        [Test]
        public void Retry_WithoutRetryAfter_BacksOffExponentially()
        {
            var waits = new List<TimeSpan>();
            var transport = new FakeTransport()
                .Respond(503, "{}")
                .Respond(500, "{}")
                .Respond(200, Ok);

            TestClient.Run(TestClient.Create(transport, waits).DetectUnsafeAsync("text"));

            CollectionAssert.AreEqual(new[] { TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2) }, waits);
        }

        [Test]
        public void Retry_NetworkError_IsRetried()
        {
            var transport = new FakeTransport().FailWithNetworkError().Respond(200, Ok);

            TestClient.Run(TestClient.Create(transport).DetectUnsafeAsync("text"));

            Assert.AreEqual(2, transport.Requests.Count);
        }

        [TestCase(400)]
        [TestCase(401)]
        [TestCase(402)]
        [TestCase(403)]
        [TestCase(404)]
        [TestCase(409)]
        [TestCase(422)]
        public void Retry_AnsweredRequests_AreNotRepeated(int status)
        {
            var transport = new FakeTransport().Respond(status, "{\"error\":{\"message\":\"no\"}}").Respond(200, Ok);

            Assert.Catch<TuteliqException>(() => TestClient.Run(TestClient.Create(transport).DetectUnsafeAsync("text")));
            Assert.AreEqual(1, transport.Requests.Count);
        }

        [Test]
        public void Retry_GivesUpAfterMaxRetries()
        {
            var transport = new FakeTransport();
            for (int i = 0; i < 5; i++) transport.Respond(503, "{}");

            Assert.Throws<ServerException>(() => TestClient.Run(TestClient.Create(transport, maxRetries: 2).DetectUnsafeAsync("text")));
            Assert.AreEqual(3, transport.Requests.Count);
        }

        [Test]
        public void Retry_ZeroRetries_StillSendsOnce()
        {
            var transport = new FakeTransport().Respond(200, Ok);

            TestClient.Run(TestClient.Create(transport, maxRetries: 0).DetectUnsafeAsync("text"));

            Assert.AreEqual(1, transport.Requests.Count);
        }

        [Test]
        public void Retry_CreateVerificationSession_IsNeverRepeated()
        {
            var transport = new FakeTransport().Respond(503, "{}").Respond(200, "{\"session_id\":\"s1\"}");

            Assert.Throws<ServerException>(() => TestClient.Run(TestClient.Create(transport)
                .CreateVerificationSessionAsync(new CreateVerificationSessionInput())));
            Assert.AreEqual(1, transport.Requests.Count);
        }
    }
}

namespace Tuteliq.Tests
{
    public class MalformedSuccessBodyTests
    {
        // A 2xx that is not a JSON object (proxy page, empty body) must not read as a clean result.
        [TestCase("")]
        [TestCase("<html>Sign in to the network</html>")]
        [TestCase("[]")]
        public void Detection_2xxWithoutAJsonObject_Throws(string body)
        {
            var transport = new FakeTransport().Respond(200, body);
            var client = TestClient.Create(transport);

            var error = Assert.Catch<TuteliqException>(() => TestClient.Run(client.DetectUnsafeAsync("text")));

            Assert.AreEqual(200, error.HttpStatus);
            Assert.AreEqual(1, transport.Requests.Count, "an answered request is not repeated");
        }

        [Test]
        public void Delete_204WithNoBody_Succeeds()
        {
            var transport = new FakeTransport().Respond(204, "");
            var client = TestClient.Create(transport);

            Assert.DoesNotThrow(() => TestClient.Run(client.DeleteWebhookAsync("wh_1")));
        }

        [Test]
        public void ConsentStatus_EscapesTheTypeInTheQuery()
        {
            var transport = new FakeTransport().Respond(200, "{\"consents\":[]}");
            var client = TestClient.Create(transport);

            TestClient.Run(client.GetConsentStatusAsync("a&b=c"));

            StringAssert.EndsWith("?type=a%26b%3Dc", transport.Last.Url);
        }
    }
}

namespace Tuteliq.Tests
{
    // Calls that create or change something are not repeated when the API may have
    // acted on them (5xx, network error, timeout). A 429 means nothing ran, so it is retried.
    public class StateChangingRetryTests
    {
        private static readonly Dictionary<string, Func<TuteliqClient, object>> Calls = new Dictionary<string, Func<TuteliqClient, object>>
        {
            { "create webhook", c => c.CreateWebhookAsync(new CreateWebhookInput { Name = "n", Url = "https://example.com/h", Events = new List<string> { "incident.created" } }) },
            { "test webhook", c => c.TestWebhookAsync("wh_1") },
            { "regenerate secret", c => c.RegenerateWebhookSecretAsync("wh_1") },
            { "record consent", c => c.RecordConsentAsync(new RecordConsentInput { ConsentType = "analytics", Version = "1" }) },
            { "log breach", c => c.LogBreachAsync(new LogBreachInput()) },
            { "review incident", c => c.ReviewIncidentAsync("inc_1", new ReviewIncidentInput { Action = ModeratorAction.Confirm, ModeratorExternalId = "mod_1" }) },
        };

        public static IEnumerable<string> Names => Calls.Keys;

        private static void Send(TuteliqClient client, string name) => ((System.Threading.Tasks.Task)Calls[name](client)).GetAwaiter().GetResult();

        [TestCaseSource(nameof(Names))]
        public void ServerError_IsNotRepeated(string name)
        {
            var transport = new FakeTransport().Respond(503, "{}").Respond(200, "{}");

            Assert.Throws<ServerException>(() => Send(TestClient.Create(transport), name));
            Assert.AreEqual(1, transport.Requests.Count);
        }

        [TestCaseSource(nameof(Names))]
        public void NetworkError_IsNotRepeated(string name)
        {
            var transport = new FakeTransport().FailWithNetworkError().Respond(200, "{}");

            Assert.Throws<NetworkException>(() => Send(TestClient.Create(transport), name));
            Assert.AreEqual(1, transport.Requests.Count);
        }

        [TestCaseSource(nameof(Names))]
        public void RateLimit_IsRetried(string name)
        {
            var transport = new FakeTransport().Respond(429, "{}", ("Retry-After", "1")).Respond(200, "{}");

            Send(TestClient.Create(transport), name);

            Assert.AreEqual(2, transport.Requests.Count);
        }

        [Test]
        public void ReadsAndDetections_StillRetryServerErrors()
        {
            var transport = new FakeTransport().Respond(503, "{}").Respond(200, "{\"webhooks\":[]}");

            TestClient.Run(TestClient.Create(transport).ListWebhooksAsync());

            Assert.AreEqual(2, transport.Requests.Count);
        }
    }
}
