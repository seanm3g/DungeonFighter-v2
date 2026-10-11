using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using RPGGame.Config;
using RPGGame.Utils;

namespace RPGGame.Services.Llm
{
    /// <summary>
    /// OpenAI Chat Completions client (also works with OpenAI-compatible base URLs).
    /// Uses the player's local key from <see cref="OpenAiPreferences"/>.
    /// API failures return null quietly (file debug log only) so optional features can fall back
    /// without spamming the combat log via <see cref="ErrorHandler.LogError"/>.
    /// </summary>
    public sealed class OpenAiChatClient : ILlmTextClient, IDisposable
    {
        private readonly HttpClient _http;
        private readonly OpenAiPreferences _prefs;
        private readonly bool _ownsHttp;

        public OpenAiChatClient(OpenAiPreferences prefs, HttpClient? httpClient = null)
        {
            _prefs = prefs ?? throw new ArgumentNullException(nameof(prefs));
            if (httpClient != null)
            {
                _http = httpClient;
                _ownsHttp = false;
            }
            else
            {
                _http = new HttpClient
                {
                    Timeout = TimeSpan.FromSeconds(Math.Clamp(prefs.TimeoutSeconds, 3, 120))
                };
                _ownsHttp = true;
            }
        }

        public async Task<string?> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken = default)
        {
            string apiKey = _prefs.ResolveApiKey();
            if (string.IsNullOrWhiteSpace(apiKey))
                return null;

            string baseUrl = string.IsNullOrWhiteSpace(_prefs.BaseUrl)
                ? OpenAiPreferences.DefaultBaseUrl
                : _prefs.BaseUrl.Trim().TrimEnd('/');
            string model = string.IsNullOrWhiteSpace(_prefs.Model)
                ? OpenAiPreferences.DefaultModel
                : _prefs.Model.Trim();

            var body = new
            {
                model,
                temperature = 0.85,
                max_tokens = 180,
                messages = new object[]
                {
                    new { role = "system", content = systemPrompt ?? string.Empty },
                    new { role = "user", content = userPrompt ?? string.Empty }
                }
            };

            string json = JsonSerializer.Serialize(body);
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/chat/completions");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
            string responseText = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                // Optional AI path: never WriteSystemLine — quota/auth/rate-limit are expected player-side conditions.
                ScrollDebugLogger.LogAlways(
                    $"OpenAiChatClient.CompleteAsync: HTTP {(int)response.StatusCode} — {SummarizeApiError(responseText)}");
                return null;
            }

            return ExtractAssistantContent(responseText);
        }

        /// <summary>Parses assistant text from a Chat Completions JSON body (unit-testable).</summary>
        public static string? ExtractAssistantContent(string responseJson)
        {
            if (string.IsNullOrWhiteSpace(responseJson))
                return null;

            try
            {
                using var doc = JsonDocument.Parse(responseJson);
                if (!doc.RootElement.TryGetProperty("choices", out var choices) ||
                    choices.ValueKind != JsonValueKind.Array ||
                    choices.GetArrayLength() == 0)
                {
                    return null;
                }

                var first = choices[0];
                if (!first.TryGetProperty("message", out var message))
                    return null;
                if (!message.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.String)
                    return null;

                string? text = content.GetString();
                return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
            }
            catch (JsonException)
            {
                return null;
            }
        }

        /// <summary>
        /// Pulls a short type/message from an OpenAI-style error JSON body (unit-testable).
        /// </summary>
        public static string SummarizeApiError(string? responseBody)
        {
            if (string.IsNullOrWhiteSpace(responseBody))
                return "empty error body";

            try
            {
                using var doc = JsonDocument.Parse(responseBody);
                if (doc.RootElement.TryGetProperty("error", out var error) &&
                    error.ValueKind == JsonValueKind.Object)
                {
                    string? type = error.TryGetProperty("type", out var typeEl) && typeEl.ValueKind == JsonValueKind.String
                        ? typeEl.GetString()
                        : null;
                    string? message = error.TryGetProperty("message", out var msgEl) && msgEl.ValueKind == JsonValueKind.String
                        ? msgEl.GetString()
                        : null;

                    if (!string.IsNullOrWhiteSpace(type) && !string.IsNullOrWhiteSpace(message))
                        return $"{type}: {Truncate(message!, 160)}";
                    if (!string.IsNullOrWhiteSpace(message))
                        return Truncate(message!, 160);
                    if (!string.IsNullOrWhiteSpace(type))
                        return type!;
                }
            }
            catch (JsonException)
            {
                // fall through to truncate raw body
            }

            return Truncate(responseBody.Trim(), 160);
        }

        private static string Truncate(string value, int max)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= max)
                return value ?? string.Empty;
            return value.Substring(0, max) + "…";
        }

        public void Dispose()
        {
            if (_ownsHttp)
                _http.Dispose();
        }
    }
}
