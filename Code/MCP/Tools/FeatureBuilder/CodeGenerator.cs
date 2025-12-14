using System;
using System.Linq;
using System.Text;

namespace RPGGame.MCP.Tools.FeatureBuilder
{
    /// <summary>
    /// Generates code templates for classes, systems, and API endpoints
    /// </summary>
    public static class CodeGenerator
    {
        /// <summary>
        /// Generates class code from name and properties
        /// </summary>
        public static string GenerateClassCode(string name, string properties)
        {
            var output = new StringBuilder();

            output.AppendLine("GENERATED CLASS CODE:\n");
            output.AppendLine($"File: Code/{name}/{name}.cs\n");

            output.AppendLine($"using System;");
            output.AppendLine($"using System.Collections.Generic;");
            output.AppendLine($"");
            output.AppendLine($"namespace RPGGame.{name}");
            output.AppendLine($"{{");
            output.AppendLine($"    /// <summary>");
            output.AppendLine($"    /// {name} class - {name} functionality");
            output.AppendLine($"    /// </summary>");
            output.AppendLine($"    public class {name}");
            output.AppendLine($"    {{");

            // Parse properties
            var props = properties.Split(',').Select(p => p.Trim()).ToList();
            foreach (var prop in props)
            {
                var parts = prop.Split(' ');
                if (parts.Length >= 2)
                {
                    var type = parts[0];
                    var propName = parts[1];
                    output.AppendLine($"        public {type} {propName} {{ get; set; }}");
                }
            }

            output.AppendLine($"");
            output.AppendLine($"        public {name}()");
            output.AppendLine($"        {{");
            output.AppendLine($"            // Initialize default values");
            output.AppendLine($"        }}");
            output.AppendLine($"");
            output.AppendLine($"        public void Initialize()");
            output.AppendLine($"        {{");
            output.AppendLine($"            // Initialization logic");
            output.AppendLine($"        }}");
            output.AppendLine($"");
            output.AppendLine($"        public void Update()");
            output.AppendLine($"        {{");
            output.AppendLine($"            // Update logic");
            output.AppendLine($"        }}");
            output.AppendLine($"");
            output.AppendLine($"        public void Execute()");
            output.AppendLine($"        {{");
            output.AppendLine($"            // Execution logic");
            output.AppendLine($"        }}");
            output.AppendLine($"    }}");
            output.AppendLine($"}}");
            output.AppendLine();

            output.AppendLine("GENERATED TEST TEMPLATE:\n");
            output.AppendLine($"File: Code/Tests/{name}Tests.cs\n");

            output.AppendLine($"using Xunit;");
            output.AppendLine($"using RPGGame.{name};");
            output.AppendLine($"");
            output.AppendLine($"namespace RPGGame.Tests");
            output.AppendLine($"{{");
            output.AppendLine($"    public class {name}Tests");
            output.AppendLine($"    {{");
            output.AppendLine($"        [Fact]");
            output.AppendLine($"        public void {name}_Initialize_Succeeds()");
            output.AppendLine($"        {{");
            output.AppendLine($"            var {name.ToLower()} = new {name}();");
            output.AppendLine($"            {name.ToLower()}.Initialize();");
            output.AppendLine($"            Assert.NotNull({name.ToLower()});");
            output.AppendLine($"        }}");
            output.AppendLine($"    }}");
            output.AppendLine($"}}");
            output.AppendLine();

            output.AppendLine("NEXT STEPS:\n");
            output.AppendLine($"1. Create {name}.cs from generated code above");
            output.AppendLine($"2. Create {name}Tests.cs from test template");
            output.AppendLine($"3. Implement methods with actual logic");
            output.AppendLine($"4. Run tests to verify");
            output.AppendLine($"5. Add XML documentation comments");

            return output.ToString();
        }
        
        /// <summary>
        /// Generates system scaffold structure
        /// </summary>
        public static string GenerateSystemScaffold(string systemName)
        {
            var output = new StringBuilder();

            output.AppendLine($"SYSTEM SCAFFOLD: {systemName}\n");

            output.AppendLine("RECOMMENDED FILE STRUCTURE:\n");
            output.AppendLine($"Code/{systemName}/");
            output.AppendLine($"├── {systemName}System.cs (main system class)");
            output.AppendLine($"├── {systemName}Manager.cs (management layer)");
            output.AppendLine($"├── {systemName}Factory.cs (object creation)");
            output.AppendLine($"├── Models/");
            output.AppendLine($"│   ├── {systemName}State.cs");
            output.AppendLine($"│   └── {systemName}Config.cs");
            output.AppendLine($"├── Handlers/");
            output.AppendLine($"│   ├── {systemName}Handler.cs");
            output.AppendLine($"│   └── {systemName}EventHandler.cs");
            output.AppendLine($"└── Utils/");
            output.AppendLine($"    └── {systemName}Utilities.cs\n");

            output.AppendLine("CORE FILES TO CREATE:\n");

            output.AppendLine($"1. {systemName}System.cs");
            output.AppendLine("   public class " + systemName + "System");
            output.AppendLine("   {");
            output.AppendLine("       public void Initialize() { }");
            output.AppendLine("       public void Update() { }");
            output.AppendLine("       public void Execute() { }");
            output.AppendLine("   }\n");

            output.AppendLine($"2. {systemName}Manager.cs");
            output.AppendLine("   public class " + systemName + "Manager");
            output.AppendLine("   {");
            output.AppendLine("       private " + systemName + "System system;");
            output.AppendLine("       public void Create() { }");
            output.AppendLine("       public void Update() { }");
            output.AppendLine("   }\n");

            output.AppendLine($"3. {systemName}Factory.cs");
            output.AppendLine("   public class " + systemName + "Factory");
            output.AppendLine("   {");
            output.AppendLine("       public " + systemName + " Create() { }");
            output.AppendLine("   }\n");

            output.AppendLine("INTEGRATION STEPS:\n");
            output.AppendLine("1. Add System instance to GameWrapper");
            output.AppendLine("2. Initialize in Game.Initialize()");
            output.AppendLine("3. Call Update in Game.Update()");
            output.AppendLine("4. Add configuration to TuningConfig.json");
            output.AppendLine("5. Create tests in Code/Tests/\n");

            output.AppendLine("ESTIMATED EFFORT: 120-180 minutes");

            return output.ToString();
        }
        
        /// <summary>
        /// Generates API endpoint code
        /// </summary>
        public static string GenerateEndpointCode(string path, string method)
        {
            var output = new StringBuilder();

            output.AppendLine("GENERATED API ENDPOINT:\n");
            output.AppendLine($"Path: {path}");
            output.AppendLine($"Method: {method}\n");

            output.AppendLine($"[Http{method}(\"{path}\")]");
            output.AppendLine($"public async Task<IActionResult> {GetMethodName(path, method)}()");
            output.AppendLine($"{{");
            output.AppendLine($"    try");
            output.AppendLine($"    {{");
            output.AppendLine($"        // Implementation");
            output.AppendLine($"        return Ok(result);");
            output.AppendLine($"    }}");
            output.AppendLine($"    catch (Exception ex)");
            output.AppendLine($"    {{");
            output.AppendLine($"        return BadRequest(new {{ error = ex.Message }});");
            output.AppendLine($"    }}");
            output.AppendLine($"}}\n");

            output.AppendLine("GENERATED TEST:\n");
            output.AppendLine($"[Fact]");
            output.AppendLine($"public async Task {GetMethodName(path, method)}_Returns200()");
            output.AppendLine($"{{");
            output.AppendLine($"    var result = await client.{method.ToLower()}(\"{path}\");");
            output.AppendLine($"    Assert.Equal(System.Net.HttpStatusCode.OK, result.StatusCode);");
            output.AppendLine($"}}\n");

            output.AppendLine("NEXT STEPS:\n");
            output.AppendLine("1. Add endpoint to Controller");
            output.AppendLine("2. Implement business logic");
            output.AppendLine("3. Add request/response models if needed");
            output.AppendLine("4. Create integration tests");
            output.AppendLine("5. Add API documentation\n");

            return output.ToString();
        }
        
        /// <summary>
        /// Gets a method name from a path and HTTP method
        /// </summary>
        public static string GetMethodName(string path, string method)
        {
            var name = path.Replace("/", "").Replace("{", "").Replace("}", "");
            return char.ToUpper(name[0]) + name.Substring(1) + method;
        }
    }
}

