using System;

namespace SafeNest
{
    /// <summary>
    /// Base exception for SafeNest SDK errors.
    /// </summary>
    public class SafeNestException : Exception
    {
        public object Details { get; }

        public SafeNestException(string message, object details = null) : base(message)
        {
            Details = details;
        }
    }

    /// <summary>
    /// Thrown when API key is invalid or missing.
    /// </summary>
    public class AuthenticationException : SafeNestException
    {
        public AuthenticationException(string message, object details = null) : base(message, details) { }
    }

    /// <summary>
    /// Thrown when rate limit is exceeded.
    /// </summary>
    public class RateLimitException : SafeNestException
    {
        public RateLimitException(string message, object details = null) : base(message, details) { }
    }

    /// <summary>
    /// Thrown when request validation fails.
    /// </summary>
    public class ValidationException : SafeNestException
    {
        public ValidationException(string message, object details = null) : base(message, details) { }
    }

    /// <summary>
    /// Thrown when a resource is not found.
    /// </summary>
    public class NotFoundException : SafeNestException
    {
        public NotFoundException(string message, object details = null) : base(message, details) { }
    }

    /// <summary>
    /// Thrown when the server returns a 5xx error.
    /// </summary>
    public class ServerException : SafeNestException
    {
        public int StatusCode { get; }

        public ServerException(string message, int statusCode, object details = null) : base(message, details)
        {
            StatusCode = statusCode;
        }
    }

    /// <summary>
    /// Thrown when a request times out.
    /// </summary>
    public class TimeoutException : SafeNestException
    {
        public TimeoutException(string message, object details = null) : base(message, details) { }
    }

    /// <summary>
    /// Thrown when a network error occurs.
    /// </summary>
    public class NetworkException : SafeNestException
    {
        public NetworkException(string message, object details = null) : base(message, details) { }
    }
}
