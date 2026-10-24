using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using RPGGame.Entity.Services;

[assembly: InternalsVisibleTo("DF.Tests")]

namespace RPGGame.Utils.Testing
{
    /// <summary>
    /// Calls test suites in DF.Tests.dll by name. The game does not reference the test project, so
    /// test code stays out of DF.dll and its compile; building Code/Tests/Tests.csproj copies
    /// DF.Tests.dll next to DF.exe, and this loads it from there on first use.
    /// </summary>
    public static class TestAssembly
    {
        public const string FileName = "DF.Tests.dll";
        private const string RootNamespace = "RPGGame.Tests.";
        private static readonly object _lock = new object();
        private static Assembly? _assembly;

        public static string NotBuiltMessage =>
            $"{FileName} was not found next to DF.exe. Build the tests first: dotnet build Code/Tests/Tests.csproj";

        /// <summary>True when DF.Tests.dll is present and loads.</summary>
        public static bool IsAvailable => TryLoad() != null;

        /// <summary>Runs a public static method, e.g. <c>Run("Runners.ComprehensiveTestRunner", "RunAllTests")</c>.</summary>
        public static void Run(string typeName, string methodName, params object?[] args) =>
            Invoke(typeName, methodName, args);

        /// <summary>Runs a public static method and returns its result.</summary>
        public static T Invoke<T>(string typeName, string methodName, params object?[] args) =>
            (T)Invoke(typeName, methodName, args)!;

        private static object? Invoke(string typeName, string methodName, object?[] args)
        {
            var assembly = TryLoad() ?? throw new InvalidOperationException(NotBuiltMessage);
            var type = assembly.GetType(RootNamespace + typeName)
                ?? throw new MissingMemberException($"{FileName} has no type {RootNamespace}{typeName}. Rebuild the tests.");
            var method = type.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(m => m.Name == methodName && AcceptsArgCount(m, args.Length))
                ?? throw new MissingMethodException($"{type.FullName}.{methodName} with {args.Length} argument(s) was not found. Rebuild the tests.");
            // Trailing optional parameters the caller left out get their declared defaults.
            var parameters = method.GetParameters();
            var callArgs = parameters.Select((p, i) => i < args.Length ? args[i] : p.DefaultValue).ToArray();
            // Tests save throwaway heroes; keep them out of GameData so they never reach the load menu.
            string? previousSaveDir = CharacterFileManager.SaveDirectoryOverride;
            string testSaveDir = Path.Combine(Path.GetTempPath(), "DF.Tests", "Saves");
            Directory.CreateDirectory(testSaveDir);
            CharacterFileManager.SaveDirectoryOverride = testSaveDir;
            try
            {
                return method.Invoke(null, callArgs);
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                throw;
            }
            finally
            {
                CharacterFileManager.SaveDirectoryOverride = previousSaveDir;
            }
        }

        private static bool AcceptsArgCount(MethodInfo method, int count)
        {
            var parameters = method.GetParameters();
            return count <= parameters.Length && parameters.Skip(count).All(p => p.IsOptional);
        }

        private static Assembly? TryLoad()
        {
            lock (_lock)
            {
                if (_assembly != null)
                    return _assembly;
                string path = Path.Combine(AppContext.BaseDirectory, FileName);
                if (!File.Exists(path))
                    return null;
                _assembly = Assembly.LoadFrom(path);
                return _assembly;
            }
        }
    }
}
