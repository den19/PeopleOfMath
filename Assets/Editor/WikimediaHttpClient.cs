using System;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;

namespace PeopleOfMath.Editor
{
    public static class WikimediaHttpClient
    {
        const string UserAgent = "PeopleOfMath/1.0 (Unity Editor; educational app)";
        const int MinIntervalMs = 2000;
        const int MaxAttempts = 8;
        const int CircuitBreakerPauseMs = 90000;
        const int MaxBackoffMs = 120000;

        static long _lastRequestTicks;
        static bool _circuitBreakerUsed;
        static readonly System.Random Jitter = new();

        public static void ResetSession()
        {
            _lastRequestTicks = 0;
            _circuitBreakerUsed = false;
        }

        public static string GetText(string url) =>
            Execute(url, downloadBytes: false, out _);

        public static byte[] DownloadBytes(string url)
        {
            Execute(url, downloadBytes: true, out var bytes);
            return bytes;
        }

        static string Execute(string url, bool downloadBytes, out byte[] bytes)
        {
            bytes = null;
            for (var attempt = 0; attempt < MaxAttempts; attempt++)
            {
                WaitMinInterval();
                using var req = UnityWebRequest.Get(url);
                req.SetRequestHeader("User-Agent", UserAgent);
                var op = req.SendWebRequest();
                while (!op.isDone)
                    Thread.Sleep(20);

                _lastRequestTicks = DateTime.UtcNow.Ticks;

                if (req.result == UnityWebRequest.Result.Success)
                {
                    return downloadBytes
                        ? EncodingBytes(req, out bytes)
                        : req.downloadHandler.text;
                }

                if (!IsRateLimited(req))
                    throw new Exception($"HTTP {req.responseCode}: {req.error}");

                var waitMs = ComputeBackoffMs(req, attempt);
                Debug.LogWarning($"Wikimedia rate limit ({req.responseCode}), wait {waitMs / 1000}s (attempt {attempt + 1}/{MaxAttempts}): {url}");
                Thread.Sleep(waitMs);

                if (!_circuitBreakerUsed && attempt >= 2)
                {
                    _circuitBreakerUsed = true;
                    Debug.LogWarning($"Wikimedia circuit breaker: extra pause {CircuitBreakerPauseMs / 1000}s");
                    Thread.Sleep(CircuitBreakerPauseMs);
                }
            }

            throw new Exception($"Rate limit: failed after {MaxAttempts} attempts: {url}");
        }

        static string EncodingBytes(UnityWebRequest req, out byte[] bytes)
        {
            bytes = req.downloadHandler.data;
            if (bytes == null || bytes.Length == 0)
                throw new Exception("Empty response body");
            return null;
        }

        static void WaitMinInterval()
        {
            if (_lastRequestTicks == 0)
                return;

            var elapsed = (DateTime.UtcNow.Ticks - _lastRequestTicks) / TimeSpan.TicksPerMillisecond;
            var wait = MinIntervalMs - (int)elapsed;
            if (wait > 0)
                Thread.Sleep(wait);
        }

        static bool IsRateLimited(UnityWebRequest req)
        {
            if (req.responseCode == 429 || req.responseCode == 503)
                return true;

            var err = req.error ?? "";
            return err.Contains("429", StringComparison.OrdinalIgnoreCase) ||
                   err.Contains("503", StringComparison.OrdinalIgnoreCase) ||
                   err.Contains("Too Many Requests", StringComparison.OrdinalIgnoreCase);
        }

        static int ComputeBackoffMs(UnityWebRequest req, int attempt)
        {
            var retryAfterSec = ParseRetryAfterSeconds(req);
            if (retryAfterSec > 0)
                return Math.Min(MaxBackoffMs, retryAfterSec * 1000);

            var exp = (int)Math.Min(MaxBackoffMs, 15000 * Math.Pow(2, attempt));
            var jitter = Jitter.Next(0, 3000);
            return exp + jitter;
        }

        static int ParseRetryAfterSeconds(UnityWebRequest req)
        {
            var header = req.GetResponseHeader("Retry-After");
            if (string.IsNullOrEmpty(header))
                return 0;

            if (int.TryParse(header.Trim(), out var seconds) && seconds > 0)
                return Math.Min(seconds, 120);

            if (DateTime.TryParse(header, out var date))
            {
                var sec = (int)Math.Max(0, (date.ToUniversalTime() - DateTime.UtcNow).TotalSeconds);
                return Math.Min(sec, 120);
            }

            return 0;
        }
    }
}
