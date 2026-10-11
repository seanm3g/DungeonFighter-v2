using System;
using System.Collections.Generic;
using System.Globalization;

namespace RPGGame.Combat.Narrative
{
    /// <summary>
    /// Pure HTTP routing for the combat-scene live feed (unit-testable without a listener).
    /// </summary>
    public static class CombatSceneLiveFeedHttp
    {
        public readonly struct Response
        {
            public Response(int statusCode, string contentType, string body, bool noCache = true)
            {
                StatusCode = statusCode;
                ContentType = contentType;
                Body = body ?? "";
                NoCache = noCache;
            }

            public int StatusCode { get; }
            public string ContentType { get; }
            public string Body { get; }
            public bool NoCache { get; }
        }

        public static Response Handle(string? rawUrl, int port)
        {
            SplitUrl(rawUrl, out string path, out string query);
            path = NormalizePath(path);

            return path switch
            {
                "/" or "/health" => Health(port),
                "/combat-scene" => Json(200, CombatSceneLiveFeed.EnvelopeJson),
                "/combat-scene/latest" => Json(200, CombatSceneLiveFeed.LatestJson),
                "/combat-scene/scenes" => Json(200, CombatSceneLiveFeed.ScenesJson),
                "/combat-scene/wait" => Wait(ParseQuery(query)),
                _ => Json(404, "{\"error\":\"not_found\"}")
            };
        }

        private static Response Health(int port)
        {
            var snap = CombatSceneLiveFeed.Current;
            string body =
                $"{{\"ok\":true,\"service\":\"dungeonfighter-combat-scene\",\"revision\":{snap.Revision},\"port\":{port},\"status\":{JsonString(snap.Status)}}}";
            return Json(200, body);
        }

        private static Response Wait(IReadOnlyDictionary<string, string> query)
        {
            long since = ParseLong(Get(query, "since"), 0);
            int timeoutMs = (int)ParseLong(Get(query, "timeoutMs"), 25_000);
            if (timeoutMs < 0) timeoutMs = 0;
            if (timeoutMs > 120_000) timeoutMs = 120_000;

            _ = CombatSceneLiveFeed.WaitForRevision(since, timeoutMs);
            return Json(200, CombatSceneLiveFeed.EnvelopeJson);
        }

        private static Response Json(int status, string body)
            => new(status, "application/json; charset=utf-8", body);

        internal static void SplitUrl(string? rawUrl, out string path, out string query)
        {
            path = "/";
            query = "";
            if (string.IsNullOrWhiteSpace(rawUrl))
                return;

            string url = rawUrl.Trim();
            if (Uri.TryCreate(url, UriKind.Absolute, out var absolute))
            {
                path = absolute.AbsolutePath;
                query = absolute.Query.TrimStart('?');
                return;
            }

            int q = url.IndexOf('?', StringComparison.Ordinal);
            if (q < 0)
            {
                path = url;
                return;
            }

            path = url[..q];
            query = url[(q + 1)..];
        }

        internal static string NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return "/";
            path = path.Trim();
            if (!path.StartsWith('/'))
                path = "/" + path;
            while (path.Length > 1 && path.EndsWith('/'))
                path = path[..^1];
            return path.ToLowerInvariant();
        }

        internal static Dictionary<string, string> ParseQuery(string query)
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(query))
                return map;

            foreach (string part in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                int eq = part.IndexOf('=', StringComparison.Ordinal);
                if (eq <= 0)
                    continue;
                string key = Uri.UnescapeDataString(part[..eq].Replace('+', ' '));
                string value = Uri.UnescapeDataString(part[(eq + 1)..].Replace('+', ' '));
                if (!string.IsNullOrEmpty(key))
                    map[key] = value;
            }

            return map;
        }

        private static string? Get(IReadOnlyDictionary<string, string> query, string key)
            => query.TryGetValue(key, out string? value) ? value : null;

        private static long ParseLong(string? raw, long fallback)
        {
            if (long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out long value))
                return value;
            return fallback;
        }

        private static string JsonString(string? value)
        {
            if (value == null)
                return "null";
            return "\"" + value.Replace("\\", "\\\\", StringComparison.Ordinal)
                .Replace("\"", "\\\"", StringComparison.Ordinal) + "\"";
        }
    }
}
