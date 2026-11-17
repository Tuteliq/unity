using System;
using System.Threading.Tasks;

namespace Tuteliq
{
    /// <summary>
    /// Which failures are retried and how long to wait between attempts.
    /// Only failures that say nothing was done are retried: rate limits, server
    /// errors, network errors and timeouts. Anything else (a validation error,
    /// a conflict, an unknown status) is thrown at once, so a request is never
    /// repeated after the API has answered it.
    /// </summary>
    internal static class RetryPolicy
    {
        /// <summary>Longest wait honoured from a Retry-After header.</summary>
        public static readonly TimeSpan MaxRetryAfter = TimeSpan.FromSeconds(60);

        public static bool IsRetryable(Exception error)
        {
            return error is RateLimitException
                || error is ServerException
                || error is NetworkException
                || error is TimeoutException;
        }

        /// <summary>
        /// Wait before retry number <paramref name="attempt"/> (0-based): the API's
        /// Retry-After when it sent one, otherwise exponential backoff.
        /// </summary>
        public static TimeSpan DelayFor(Exception error, int attempt, float retryDelaySeconds)
        {
            var retryAfter = RetryAfterOf(error);
            if (retryAfter.HasValue)
            {
                var requested = TimeSpan.FromSeconds(retryAfter.Value);
                return requested > MaxRetryAfter ? MaxRetryAfter : requested;
            }
            return TimeSpan.FromSeconds(retryDelaySeconds * Math.Pow(2, attempt));
        }

        /// <summary>
        /// Runs <paramref name="send"/> once, then up to <paramref name="maxRetries"/> more
        /// times while it fails with a retryable error.
        /// </summary>
        public static async Task<T> ExecuteAsync<T>(
            Func<Task<T>> send,
            int maxRetries,
            float retryDelaySeconds,
            Func<TimeSpan, Task> delay)
        {
            for (int attempt = 0; ; attempt++)
            {
                try
                {
                    return await send();
                }
                catch (Exception e) when (attempt < maxRetries && IsRetryable(e))
                {
                    await delay(DelayFor(e, attempt, retryDelaySeconds));
                }
            }
        }

        private static int? RetryAfterOf(Exception error)
        {
            if (error is RateLimitException rateLimit) return rateLimit.RetryAfterSeconds;
            if (error is ServerException server) return server.RetryAfterSeconds;
            return null;
        }
    }
}
