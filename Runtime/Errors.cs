using System;

namespace Tuteliq
{
    /// <summary>
    /// Base exception for Tuteliq SDK errors.
    /// </summary>
    public class TuteliqException : Exception
    {
        public object Details { get; }

        /// <summary>
        /// Error code from the API error envelope (for example "RATE_2001"), when the API sent one.
        /// </summary>
        public string Code { get; internal set; }

        /// <summary>
        /// Request ID of the failed call, when known. Quote it when contacting support.
        /// </summary>
        public string RequestId { get; internal set; }

        /// <summary>
        /// HTTP status of the failed call, when the API answered.
        /// </summary>
        public int? HttpStatus { get; internal set; }

        public TuteliqException(string message, object details = null) : base(message)
        {
            Details = details;
        }
    }

    /// <summary>
    /// Thrown when API key is invalid or missing.
    /// </summary>
    public class AuthenticationException : TuteliqException
    {
        public AuthenticationException(string message, object details = null) : base(message, details) { }
    }

    /// <summary>
    /// Thrown when rate limit is exceeded.
    /// </summary>
    public class RateLimitException : TuteliqException
    {
        /// <summary>
        /// Seconds the API asked the client to wait before retrying (the Retry-After header), when sent.
        /// </summary>
        public int? RetryAfterSeconds { get; }

        public RateLimitException(string message, object details = null, int? retryAfterSeconds = null) : base(message, details)
        {
            RetryAfterSeconds = retryAfterSeconds;
        }
    }

    /// <summary>
    /// Thrown when request validation fails.
    /// </summary>
    public class ValidationException : TuteliqException
    {
        public ValidationException(string message, object details = null) : base(message, details) { }
    }

    /// <summary>
    /// Thrown when a resource is not found.
    /// </summary>
    public class NotFoundException : TuteliqException
    {
        public NotFoundException(string message, object details = null) : base(message, details) { }
    }

    /// <summary>
    /// Thrown when the server returns a 5xx error.
    /// </summary>
    public class ServerException : TuteliqException
    {
        public int StatusCode { get; }

        /// <summary>
        /// Seconds the API asked the client to wait before retrying (the Retry-After header), when sent.
        /// </summary>
        public int? RetryAfterSeconds { get; }

        public ServerException(string message, int statusCode, object details = null, int? retryAfterSeconds = null) : base(message, details)
        {
            StatusCode = statusCode;
            RetryAfterSeconds = retryAfterSeconds;
        }
    }

    /// <summary>
    /// Thrown when a request times out.
    /// </summary>
    public class TimeoutException : TuteliqException
    {
        public TimeoutException(string message, object details = null) : base(message, details) { }
    }

    /// <summary>
    /// Thrown when a network error occurs.
    /// </summary>
    public class NetworkException : TuteliqException
    {
        public NetworkException(string message, object details = null) : base(message, details) { }
    }

    /// <summary>
    /// Thrown when the usage quota has been exceeded.
    /// </summary>
    public class QuotaExceededException : TuteliqException
    {
        public QuotaExceededException(string message, object details = null) : base(message, details) { }
    }

    /// <summary>
    /// Thrown when the current tier does not have access to the requested feature.
    /// </summary>
    public class TierAccessException : TuteliqException
    {
        public TierAccessException(string message, object details = null) : base(message, details) { }
    }
}
