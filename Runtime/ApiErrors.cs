using System.Collections.Generic;
using System.Globalization;

namespace Tuteliq
{
    /// <summary>
    /// Turns an API error response into the matching typed exception.
    /// The API answers errors with <c>{"error": {"code", "message", "details"}}</c>
    /// and sends <c>Retry-After</c> (seconds) on 429 and 5xx responses.
    /// </summary>
    internal static class ApiErrors
    {
        // 429 codes that mean an allowance is used up: waiting seconds will not help.
        private static readonly HashSet<string> ExhaustedAllowanceCodes = new HashSet<string>
        {
            "RATE_2002", // daily limit
            "RATE_2003", // quota exceeded
            "SUB_7004",  // message limit reached
            "SUB_7005"   // credits depleted
        };

        public static TuteliqException FromResponse(int status, string body, string retryAfterHeader, string requestIdHeader)
        {
            var envelope = ReadEnvelope(body);
            var message = envelope.Message ?? $"Request failed with status {status}";
            var retryAfter = ParseRetryAfter(retryAfterHeader);

            var error = Create(status, envelope, message, retryAfter);
            error.Code = envelope.Code;
            error.RequestId = envelope.RequestId ?? requestIdHeader;
            error.HttpStatus = status;
            return error;
        }

        /// <summary>
        /// Reads a Retry-After value given in seconds. HTTP-date values and garbage return null.
        /// </summary>
        public static int? ParseRetryAfter(string header)
        {
            if (string.IsNullOrWhiteSpace(header)) return null;
            if (int.TryParse(header.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var seconds))
                return seconds;
            return null;
        }

        private static TuteliqException Create(int status, Envelope envelope, string message, int? retryAfter)
        {
            switch (status)
            {
                case 400: return new ValidationException(message, envelope.Details);
                case 401: return new AuthenticationException(message, envelope.Details);
                case 402: return new QuotaExceededException(message, envelope.Details);
                case 403: return new TierAccessException(message, envelope.Details);
                case 404: return new NotFoundException(message, envelope.Details);
                case 429:
                    if (envelope.Code != null && ExhaustedAllowanceCodes.Contains(envelope.Code))
                        return new QuotaExceededException(message, envelope.Details);
                    return new RateLimitException(message, envelope.Details, retryAfter);
            }
            if (status >= 500) return new ServerException(message, status, envelope.Details, retryAfter);
            return new TuteliqException(message, envelope.Details);
        }

        private static Envelope ReadEnvelope(string body)
        {
            var envelope = new Envelope();
            if (string.IsNullOrEmpty(body)) return envelope;

            object parsed;
            try { parsed = MiniJson.Deserialize(body); }
            catch { return envelope; }

            if (parsed is Dictionary<string, object> root &&
                root.TryGetValue("error", out var errorObj) &&
                errorObj is Dictionary<string, object> error)
            {
                envelope.Code = ReadString(error, "code");
                envelope.Message = ReadString(error, "message");
                envelope.RequestId = ReadString(error, "request_id");
                error.TryGetValue("details", out envelope.Details);
            }
            return envelope;
        }

        private static string ReadString(Dictionary<string, object> data, string key)
        {
            return data.TryGetValue(key, out var value) && value != null ? value.ToString() : null;
        }

        private sealed class Envelope
        {
            public string Code;
            public string Message;
            public string RequestId;
            public object Details;
        }
    }
}
