using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using RPGGame.Config;
using RPGGame.Services.Llm;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Services
{
    /// <summary>Tests for OpenAI Chat Completions response parsing (no network).</summary>
    public static class OpenAiChatClientTests
    {
        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== OpenAiChatClient Tests ===\n");
            _testsRun = _testsPassed = _testsFailed = 0;

            TestExtractAssistantContentHappyPath();
            TestExtractAssistantContentMalformed();
            TestExtractAssistantContentEmptyChoices();
            TestSummarizeApiErrorInsufficientQuota();
            TestHttpFailureReturnsNullQuietly().GetAwaiter().GetResult();

            TestBase.PrintSummary("OpenAiChatClient Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestExtractAssistantContentHappyPath()
        {
            Console.WriteLine("--- ExtractAssistantContent happy path ---");
            const string json =
                "{\"choices\":[{\"message\":{\"role\":\"assistant\",\"content\":\"Line one\\nLine two\"}}]}";

            string? text = OpenAiChatClient.ExtractAssistantContent(json);
            TestBase.AssertEqual("Line one\nLine two", text, "assistant content extracted",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestExtractAssistantContentMalformed()
        {
            Console.WriteLine("--- ExtractAssistantContent malformed ---");
            TestBase.AssertTrue(OpenAiChatClient.ExtractAssistantContent("not-json") == null,
                "malformed JSON returns null",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(OpenAiChatClient.ExtractAssistantContent("") == null,
                "empty returns null",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(OpenAiChatClient.ExtractAssistantContent(null!) == null,
                "null returns null",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestExtractAssistantContentEmptyChoices()
        {
            Console.WriteLine("--- ExtractAssistantContent empty choices ---");
            TestBase.AssertTrue(OpenAiChatClient.ExtractAssistantContent("{\"choices\":[]}") == null,
                "empty choices returns null",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(
                OpenAiChatClient.ExtractAssistantContent("{\"choices\":[{\"message\":{\"role\":\"assistant\"}}]}") == null,
                "missing content returns null",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestSummarizeApiErrorInsufficientQuota()
        {
            Console.WriteLine("--- SummarizeApiError insufficient_quota ---");
            const string body =
                "{\"error\":{\"message\":\"You have no credits remaining. Add credits to continue.\",\"type\":\"insufficient_quota\",\"param\":null,\"code\":\"insufficient_quota\"}}";

            string summary = OpenAiChatClient.SummarizeApiError(body);
            TestBase.AssertTrue(summary.Contains("insufficient_quota", StringComparison.Ordinal),
                "summary includes error type",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(summary.Contains("no credits", StringComparison.OrdinalIgnoreCase),
                "summary includes short message",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(!summary.Contains("\"error\"", StringComparison.Ordinal),
                "summary is not raw JSON wrapper",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertEqual("empty error body", OpenAiChatClient.SummarizeApiError(null),
                "null body summarized",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static async Task TestHttpFailureReturnsNullQuietly()
        {
            Console.WriteLine("--- HTTP 429 returns null (no throw) ---");
            const string body =
                "{\"error\":{\"message\":\"You have no credits remaining.\",\"type\":\"insufficient_quota\"}}";
            var handler = new FixedStatusHandler(HttpStatusCode.TooManyRequests, body);
            using var http = new HttpClient(handler);
            var prefs = new OpenAiPreferences
            {
                ApiKey = "sk-test",
                BaseUrl = "https://example.invalid/v1",
                Model = "gpt-4o-mini",
                TimeoutSeconds = 10
            };

            using var client = new OpenAiChatClient(prefs, http);
            string? result = await client.CompleteAsync("sys", "user");
            TestBase.AssertTrue(result == null, "HTTP failure returns null for FlavorText fallback",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(1, handler.CallCount, "one HTTP attempt was made",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private sealed class FixedStatusHandler : HttpMessageHandler
        {
            private readonly HttpStatusCode _status;
            private readonly string _body;

            public int CallCount { get; private set; }

            public FixedStatusHandler(HttpStatusCode status, string body)
            {
                _status = status;
                _body = body;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                CallCount++;
                return Task.FromResult(new HttpResponseMessage(_status)
                {
                    Content = new StringContent(_body)
                });
            }
        }
    }
}
