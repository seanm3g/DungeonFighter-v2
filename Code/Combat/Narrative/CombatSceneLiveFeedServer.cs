using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RPGGame.Utils;

namespace RPGGame.Combat.Narrative
{
    /// <summary>
    /// Localhost-only HTTP server exposing <see cref="CombatSceneLiveFeed"/> for ComfyUI nodes.
    /// Default: <c>http://127.0.0.1:7877/</c>. Disable with env <c>DF_COMBAT_SCENE_FEED=0</c>;
    /// override port with <c>DF_COMBAT_SCENE_FEED_PORT</c>.
    /// </summary>
    public static class CombatSceneLiveFeedServer
    {
        public const int DefaultPort = 7877;
        public const string EnabledEnvironmentVariable = "DF_COMBAT_SCENE_FEED";
        public const string PortEnvironmentVariable = "DF_COMBAT_SCENE_FEED_PORT";

        private static readonly object Gate = new();
        private static HttpListener? _listener;
        private static CancellationTokenSource? _cts;
        private static Task? _loop;
        private static int _port = DefaultPort;
        private static bool _running;

        public static bool IsRunning
        {
            get
            {
                lock (Gate)
                    return _running;
            }
        }

        public static int Port
        {
            get
            {
                lock (Gate)
                    return _port;
            }
        }

        public static string BaseUrl => $"http://127.0.0.1:{Port}/";

        public static bool TryStart()
        {
            if (!IsEnabledByEnvironment())
            {
                DebugLogger.Log("CombatSceneLiveFeed", "Feed disabled via DF_COMBAT_SCENE_FEED");
                return false;
            }

            int port = ResolvePort();
            lock (Gate)
            {
                if (_running)
                    return true;

                var listener = new HttpListener();
                string prefix = $"http://127.0.0.1:{port}/";
                listener.Prefixes.Add(prefix);

                try
                {
                    listener.Start();
                }
                catch (Exception ex)
                {
                    DebugLogger.Log("CombatSceneLiveFeed", $"Failed to start on {prefix}: {ex.Message}");
                    try { listener.Close(); } catch { /* ignore */ }
                    return false;
                }

                _listener = listener;
                _port = port;
                _cts = new CancellationTokenSource();
                _running = true;
                var token = _cts.Token;
                _loop = Task.Run(() => AcceptLoopAsync(listener, port, token), token);
                DebugLogger.Log("CombatSceneLiveFeed", $"Listening on {prefix}");
                return true;
            }
        }

        public static void Stop()
        {
            HttpListener? listener;
            CancellationTokenSource? cts;
            Task? loop;
            lock (Gate)
            {
                if (!_running)
                    return;
                _running = false;
                listener = _listener;
                cts = _cts;
                loop = _loop;
                _listener = null;
                _cts = null;
                _loop = null;
            }

            try { cts?.Cancel(); } catch { /* ignore */ }
            try { listener?.Stop(); } catch { /* ignore */ }
            try { listener?.Close(); } catch { /* ignore */ }
            try { cts?.Dispose(); } catch { /* ignore */ }

            if (loop != null)
            {
                try
                {
                    loop.Wait(500);
                }
                catch
                {
                    // Shutdown must stay best-effort.
                }
            }
        }

        internal static bool IsEnabledByEnvironment()
        {
            try
            {
                string? raw = global::System.Environment.GetEnvironmentVariable(EnabledEnvironmentVariable);
                if (string.IsNullOrWhiteSpace(raw))
                    return true;
                raw = raw.Trim();
                if (raw.Equals("0", StringComparison.OrdinalIgnoreCase) ||
                    raw.Equals("false", StringComparison.OrdinalIgnoreCase) ||
                    raw.Equals("off", StringComparison.OrdinalIgnoreCase) ||
                    raw.Equals("no", StringComparison.OrdinalIgnoreCase))
                    return false;
                return true;
            }
            catch
            {
                return true;
            }
        }

        internal static int ResolvePort()
        {
            try
            {
                string? raw = global::System.Environment.GetEnvironmentVariable(PortEnvironmentVariable);
                if (int.TryParse(raw, out int port) && port is >= 1 and <= 65535)
                    return port;
            }
            catch
            {
                // fall through
            }

            return DefaultPort;
        }

        private static async Task AcceptLoopAsync(HttpListener listener, int port, CancellationToken token)
        {
            while (!token.IsCancellationRequested && listener.IsListening)
            {
                HttpListenerContext? context = null;
                try
                {
                    context = await listener.GetContextAsync().ConfigureAwait(false);
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (HttpListenerException)
                {
                    if (token.IsCancellationRequested || !listener.IsListening)
                        break;
                    continue;
                }
                catch (InvalidOperationException)
                {
                    break;
                }
                catch
                {
                    if (token.IsCancellationRequested)
                        break;
                    continue;
                }

                _ = Task.Run(() => Serve(context, port), token);
            }
        }

        private static void Serve(HttpListenerContext context, int port)
        {
            try
            {
                if (!string.Equals(context.Request.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(context.Request.HttpMethod, "HEAD", StringComparison.OrdinalIgnoreCase))
                {
                    Write(context.Response, new CombatSceneLiveFeedHttp.Response(
                        405, "application/json; charset=utf-8", "{\"error\":\"method_not_allowed\"}"));
                    return;
                }

                var response = CombatSceneLiveFeedHttp.Handle(context.Request.RawUrl, port);
                if (string.Equals(context.Request.HttpMethod, "HEAD", StringComparison.OrdinalIgnoreCase))
                {
                    Write(context.Response, new CombatSceneLiveFeedHttp.Response(
                        response.StatusCode, response.ContentType, "", response.NoCache));
                    return;
                }

                Write(context.Response, response);
            }
            catch
            {
                try
                {
                    Write(context.Response, new CombatSceneLiveFeedHttp.Response(
                        500, "application/json; charset=utf-8", "{\"error\":\"internal\"}"));
                }
                catch
                {
                    // ignore
                }
            }
        }

        private static void Write(HttpListenerResponse response, CombatSceneLiveFeedHttp.Response payload)
        {
            try
            {
                response.StatusCode = payload.StatusCode;
                response.ContentType = payload.ContentType;
                response.AddHeader("Access-Control-Allow-Origin", "*");
                if (payload.NoCache)
                {
                    response.AddHeader("Cache-Control", "no-store, no-cache, must-revalidate");
                    response.AddHeader("Pragma", "no-cache");
                }

                byte[] bytes = Encoding.UTF8.GetBytes(payload.Body);
                response.ContentLength64 = bytes.Length;
                if (bytes.Length > 0)
                    response.OutputStream.Write(bytes, 0, bytes.Length);
            }
            finally
            {
                try { response.OutputStream.Close(); } catch { /* ignore */ }
                try { response.Close(); } catch { /* ignore */ }
            }
        }
    }
}
