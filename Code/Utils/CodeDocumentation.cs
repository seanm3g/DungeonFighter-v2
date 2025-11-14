using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace RPGGame.Utils
{
    /// <summary>
    /// Utility for generating and managing code documentation
    /// Provides automated documentation generation and validation
    /// </summary>
    public static class CodeDocumentation
    {
        /// <summary>
        /// Generates comprehensive documentation for a type
        /// </summary>
        /// <param name="type">The type to document</param>
        /// <returns>Formatted documentation string</returns>
        public static string GenerateTypeDocumentation(Type type)
        {
            var doc = new StringBuilder();
            
            // Type header
            doc.AppendLine($"# {type.Name}");
            doc.AppendLine();
            
            // Type description
            var typeSummary = GetTypeSummary(type);
            if (!string.IsNullOrEmpty(typeSummary))
            {
                doc.AppendLine($"**Description:** {typeSummary}");
                doc.AppendLine();
            }
            
            // Type information
            doc.AppendLine("## Type Information");
            doc.AppendLine($"- **Namespace:** {type.Namespace}");
            doc.AppendLine($"- **Assembly:** {type.Assembly.GetName().Name}");
            doc.AppendLine($"- **Is Class:** {type.IsClass}");
            doc.AppendLine($"- **Is Interface:** {type.IsInterface}");
            doc.AppendLine($"- **Is Abstract:** {type.IsAbstract}");
            doc.AppendLine($"- **Is Static:** {type.IsAbstract && type.IsSealed}");
            doc.AppendLine();
            
            // Properties
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
            if (properties.Length > 0)
            {
                doc.AppendLine("## Properties");
                foreach (var prop in properties)
                {
                    doc.AppendLine($"- **{prop.Name}** ({GetTypeName(prop.PropertyType)})");
                    var propSummary = GetPropertySummary(prop);
                    if (!string.IsNullOrEmpty(propSummary))
                    {
                        doc.AppendLine($"  - {propSummary}");
                    }
                }
                doc.AppendLine();
            }
            
            // Methods
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                .Where(m => !m.IsSpecialName && !m.Name.StartsWith("get_") && !m.Name.StartsWith("set_"))
                .ToArray();
            
            if (methods.Length > 0)
            {
                doc.AppendLine("## Methods");
                foreach (var method in methods)
                {
                    doc.AppendLine($"- **{method.Name}**");
                    var methodSummary = GetMethodSummary(method);
                    if (!string.IsNullOrEmpty(methodSummary))
                    {
                        doc.AppendLine($"  - {methodSummary}");
                    }
                    
                    var parameters = method.GetParameters();
                    if (parameters.Length > 0)
                    {
                        doc.AppendLine("  - **Parameters:**");
                        foreach (var param in parameters)
                        {
                            doc.AppendLine($"    - `{param.Name}` ({GetTypeName(param.ParameterType)})");
                        }
                    }
                    
                    if (method.ReturnType != typeof(void))
                    {
                        doc.AppendLine($"  - **Returns:** {GetTypeName(method.ReturnType)}");
                    }
                }
                doc.AppendLine();
            }
            
            // Fields
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
            if (fields.Length > 0)
            {
                doc.AppendLine("## Fields");
                foreach (var field in fields)
                {
                    doc.AppendLine($"- **{field.Name}** ({GetTypeName(field.FieldType)})");
                    var fieldSummary = GetFieldSummary(field);
                    if (!string.IsNullOrEmpty(fieldSummary))
                    {
                        doc.AppendLine($"  - {fieldSummary}");
                    }
                }
                doc.AppendLine();
            }
            
            return doc.ToString();
        }
        
        /// <summary>
        /// Generates documentation for all types in an assembly
        /// </summary>
        /// <param name="assembly">The assembly to document</param>
        /// <returns>Formatted documentation string</returns>
        public static string GenerateAssemblyDocumentation(Assembly assembly)
        {
            var doc = new StringBuilder();
            
            doc.AppendLine($"# Assembly Documentation: {assembly.GetName().Name}");
            doc.AppendLine();
            doc.AppendLine($"**Version:** {assembly.GetName().Version}");
            doc.AppendLine($"**Location:** {assembly.Location}");
            doc.AppendLine();
            
            var types = assembly.GetTypes()
                .Where(t => t.IsPublic && !t.IsNested)
                .OrderBy(t => t.Namespace)
                .ThenBy(t => t.Name)
                .ToArray();
            
            var groupedTypes = types.GroupBy(t => t.Namespace ?? "Global");
            
            foreach (var group in groupedTypes)
            {
                doc.AppendLine($"## Namespace: {group.Key}");
                doc.AppendLine();
                
                foreach (var type in group)
                {
                    doc.AppendLine($"### {type.Name}");
                    var summary = GetTypeSummary(type);
                    if (!string.IsNullOrEmpty(summary))
                    {
                        doc.AppendLine(summary);
                    }
                    doc.AppendLine();
                }
            }
            
            return doc.ToString();
        }
        
        /// <summary>
        /// Validates that all public members have documentation
        /// </summary>
        /// <param name="type">The type to validate</param>
        /// <returns>List of undocumented members</returns>
        public static List<UndocumentedMember> ValidateDocumentation(Type type)
        {
            var undocumented = new List<UndocumentedMember>();
            
            // Check type documentation
            if (string.IsNullOrEmpty(GetTypeSummary(type)))
            {
                undocumented.Add(new UndocumentedMember
                {
                    Type = MemberType.Type,
                    Name = type.Name,
                    FullName = type.FullName ?? type.Name
                });
            }
            
            // Check properties
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
            foreach (var prop in properties)
            {
                if (string.IsNullOrEmpty(GetPropertySummary(prop)))
                {
                    undocumented.Add(new UndocumentedMember
                    {
                        Type = MemberType.Property,
                        Name = prop.Name,
                        FullName = $"{type.FullName}.{prop.Name}"
                    });
                }
            }
            
            // Check methods
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                .Where(m => !m.IsSpecialName && !m.Name.StartsWith("get_") && !m.Name.StartsWith("set_"))
                .ToArray();
            
            foreach (var method in methods)
            {
                if (string.IsNullOrEmpty(GetMethodSummary(method)))
                {
                    undocumented.Add(new UndocumentedMember
                    {
                        Type = MemberType.Method,
                        Name = method.Name,
                        FullName = $"{type.FullName}.{method.Name}"
                    });
                }
            }
            
            // Check fields
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
            foreach (var field in fields)
            {
                if (string.IsNullOrEmpty(GetFieldSummary(field)))
                {
                    undocumented.Add(new UndocumentedMember
                    {
                        Type = MemberType.Field,
                        Name = field.Name,
                        FullName = $"{type.FullName}.{field.Name}"
                    });
                }
            }
            
            return undocumented;
        }
        
        /// <summary>
        /// Generates a code quality report for a type
        /// </summary>
        /// <param name="type">The type to analyze</param>
        /// <returns>Code quality report</returns>
        public static CodeQualityReport GenerateQualityReport(Type type)
        {
            var report = new CodeQualityReport
            {
                TypeName = type.Name,
                Namespace = type.Namespace ?? "Global"
            };
            
            // Count members
            report.PropertyCount = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static).Length;
            report.MethodCount = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                .Where(m => !m.IsSpecialName && !m.Name.StartsWith("get_") && !m.Name.StartsWith("set_"))
                .Count();
            report.FieldCount = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static).Length;
            
            // Check documentation coverage
            var undocumented = ValidateDocumentation(type);
            report.DocumentationCoverage = (double)(report.PropertyCount + report.MethodCount + report.FieldCount + 1 - undocumented.Count) / 
                                         (report.PropertyCount + report.MethodCount + report.FieldCount + 1) * 100;
            
            // Check for potential issues
            report.HasPublicFields = report.FieldCount > 0;
            report.IsLargeClass = report.PropertyCount + report.MethodCount > 20;
            report.IsStaticClass = type.IsAbstract && type.IsSealed;
            
            return report;
        }
        
        private static string GetTypeSummary(Type type)
        {
            // This is a simplified version - in a real implementation, you'd parse XML documentation
            var summaryAttribute = type.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>();
            return summaryAttribute?.Description ?? "";
        }
        
        private static string GetPropertySummary(PropertyInfo property)
        {
            var summaryAttribute = property.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>();
            return summaryAttribute?.Description ?? "";
        }
        
        private static string GetMethodSummary(MethodInfo method)
        {
            var summaryAttribute = method.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>();
            return summaryAttribute?.Description ?? "";
        }
        
        private static string GetFieldSummary(FieldInfo field)
        {
            var summaryAttribute = field.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>();
            return summaryAttribute?.Description ?? "";
        }
        
        private static string GetTypeName(Type type)
        {
            if (type.IsGenericType)
            {
                var genericArgs = type.GetGenericArguments();
                var baseName = type.Name.Split('`')[0];
                var argNames = string.Join(", ", genericArgs.Select(GetTypeName));
                return $"{baseName}<{argNames}>";
            }
            
            return type.Name;
        }
        
        /// <summary>
        /// Represents an undocumented member
        /// </summary>
        public class UndocumentedMember
        {
            public MemberType Type { get; set; }
            public string Name { get; set; } = "";
            public string FullName { get; set; } = "";
        }
        
        /// <summary>
        /// Represents a code quality report
        /// </summary>
        public class CodeQualityReport
        {
            public string TypeName { get; set; } = "";
            public string Namespace { get; set; } = "";
            public int PropertyCount { get; set; }
            public int MethodCount { get; set; }
            public int FieldCount { get; set; }
            public double DocumentationCoverage { get; set; }
            public bool HasPublicFields { get; set; }
            public bool IsLargeClass { get; set; }
            public bool IsStaticClass { get; set; }
        }
        
        /// <summary>
        /// Member types for documentation validation
        /// </summary>
        public enum MemberType
        {
            Type,
            Property,
            Method,
            Field
        }
    }
}
