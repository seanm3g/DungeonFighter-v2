using System;
using System.Collections.Generic;
using System.IO;

namespace RPGGame.Utils
{
    /// <summary>
    /// Comprehensive input validation utility for common validation scenarios
    /// Provides consistent validation patterns across the codebase
    /// </summary>
    public static class InputValidator
    {
        /// <summary>
        /// Validates that a string is not null, empty, or whitespace
        /// </summary>
        /// <param name="value">The string to validate</param>
        /// <param name="parameterName">The name of the parameter for error messages</param>
        /// <param name="allowEmpty">Whether to allow empty strings (default: false)</param>
        /// <exception cref="ArgumentException">Thrown when validation fails</exception>
        public static void ValidateString(string? value, string parameterName, bool allowEmpty = false)
        {
            if (value == null)
            {
                throw new ArgumentNullException(parameterName, $"{parameterName} cannot be null");
            }
            
            if (!allowEmpty && string.IsNullOrEmpty(value))
            {
                throw new ArgumentException($"{parameterName} cannot be empty", parameterName);
            }
            
            if (!allowEmpty && string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException($"{parameterName} cannot be whitespace", parameterName);
            }
        }
        
        /// <summary>
        /// Validates that a numeric value is within a specified range
        /// </summary>
        /// <param name="value">The value to validate</param>
        /// <param name="parameterName">The name of the parameter for error messages</param>
        /// <param name="minValue">The minimum allowed value (inclusive)</param>
        /// <param name="maxValue">The maximum allowed value (inclusive)</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when validation fails</exception>
        public static void ValidateRange(int value, string parameterName, int minValue, int maxValue)
        {
            if (value < minValue || value > maxValue)
            {
                throw new ArgumentOutOfRangeException(parameterName, 
                    $"{parameterName} must be between {minValue} and {maxValue}, but was {value}");
            }
        }
        
        /// <summary>
        /// Validates that a numeric value is within a specified range
        /// </summary>
        /// <param name="value">The value to validate</param>
        /// <param name="parameterName">The name of the parameter for error messages</param>
        /// <param name="minValue">The minimum allowed value (inclusive)</param>
        /// <param name="maxValue">The maximum allowed value (inclusive)</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when validation fails</exception>
        public static void ValidateRange(double value, string parameterName, double minValue, double maxValue)
        {
            if (value < minValue || value > maxValue)
            {
                throw new ArgumentOutOfRangeException(parameterName, 
                    $"{parameterName} must be between {minValue} and {maxValue}, but was {value}");
            }
        }
        
        /// <summary>
        /// Validates that a value is positive (greater than zero)
        /// </summary>
        /// <param name="value">The value to validate</param>
        /// <param name="parameterName">The name of the parameter for error messages</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when validation fails</exception>
        public static void ValidatePositive(int value, string parameterName)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(parameterName, 
                    $"{parameterName} must be positive, but was {value}");
            }
        }
        
        /// <summary>
        /// Validates that a value is positive (greater than zero)
        /// </summary>
        /// <param name="value">The value to validate</param>
        /// <param name="parameterName">The name of the parameter for error messages</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when validation fails</exception>
        public static void ValidatePositive(double value, string parameterName)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(parameterName, 
                    $"{parameterName} must be positive, but was {value}");
            }
        }
        
        /// <summary>
        /// Validates that a value is non-negative (greater than or equal to zero)
        /// </summary>
        /// <param name="value">The value to validate</param>
        /// <param name="parameterName">The name of the parameter for error messages</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when validation fails</exception>
        public static void ValidateNonNegative(int value, string parameterName)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(parameterName, 
                    $"{parameterName} must be non-negative, but was {value}");
            }
        }
        
        /// <summary>
        /// Validates that a value is non-negative (greater than or equal to zero)
        /// </summary>
        /// <param name="value">The value to validate</param>
        /// <param name="parameterName">The name of the parameter for error messages</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when validation fails</exception>
        public static void ValidateNonNegative(double value, string parameterName)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(parameterName, 
                    $"{parameterName} must be non-negative, but was {value}");
            }
        }
        
        /// <summary>
        /// Validates that a probability value is between 0 and 1 (inclusive)
        /// </summary>
        /// <param name="value">The probability value to validate</param>
        /// <param name="parameterName">The name of the parameter for error messages</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when validation fails</exception>
        public static void ValidateProbability(double value, string parameterName)
        {
            if (value < 0.0 || value > 1.0)
            {
                throw new ArgumentOutOfRangeException(parameterName, 
                    $"{parameterName} must be between 0.0 and 1.0, but was {value}");
            }
        }
        
        /// <summary>
        /// Validates that a file path exists
        /// </summary>
        /// <param name="filePath">The file path to validate</param>
        /// <param name="parameterName">The name of the parameter for error messages</param>
        /// <exception cref="ArgumentException">Thrown when validation fails</exception>
        public static void ValidateFileExists(string filePath, string parameterName)
        {
            ValidateString(filePath, parameterName);
            
            if (!File.Exists(filePath))
            {
                throw new ArgumentException($"File does not exist: {filePath}", parameterName);
            }
        }
        
        /// <summary>
        /// Validates that a directory path exists
        /// </summary>
        /// <param name="directoryPath">The directory path to validate</param>
        /// <param name="parameterName">The name of the parameter for error messages</param>
        /// <exception cref="ArgumentException">Thrown when validation fails</exception>
        public static void ValidateDirectoryExists(string directoryPath, string parameterName)
        {
            ValidateString(directoryPath, parameterName);
            
            if (!Directory.Exists(directoryPath))
            {
                throw new ArgumentException($"Directory does not exist: {directoryPath}", parameterName);
            }
        }
        
        /// <summary>
        /// Validates that a collection is not null or empty
        /// </summary>
        /// <typeparam name="T">The type of items in the collection</typeparam>
        /// <param name="collection">The collection to validate</param>
        /// <param name="parameterName">The name of the parameter for error messages</param>
        /// <exception cref="ArgumentException">Thrown when validation fails</exception>
        public static void ValidateCollection<T>(IEnumerable<T>? collection, string parameterName)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(parameterName, $"{parameterName} cannot be null");
            }
            
            if (!collection.Any())
            {
                throw new ArgumentException($"{parameterName} cannot be empty", parameterName);
            }
        }
        
        /// <summary>
        /// Validates that an object is not null
        /// </summary>
        /// <param name="value">The object to validate</param>
        /// <param name="parameterName">The name of the parameter for error messages</param>
        /// <exception cref="ArgumentNullException">Thrown when validation fails</exception>
        public static void ValidateNotNull(object? value, string parameterName)
        {
            if (value == null)
            {
                throw new ArgumentNullException(parameterName, $"{parameterName} cannot be null");
            }
        }
        
        /// <summary>
        /// Validates that a string matches a specific pattern (using simple contains check)
        /// </summary>
        /// <param name="value">The string to validate</param>
        /// <param name="parameterName">The name of the parameter for error messages</param>
        /// <param name="allowedValues">The allowed values</param>
        /// <exception cref="ArgumentException">Thrown when validation fails</exception>
        public static void ValidateEnumValue(string value, string parameterName, params string[] allowedValues)
        {
            ValidateString(value, parameterName);
            
            if (!allowedValues.Contains(value))
            {
                throw new ArgumentException($"{parameterName} must be one of: {string.Join(", ", allowedValues)}, but was '{value}'", parameterName);
            }
        }
        
        /// <summary>
        /// Validates that a string has a minimum length
        /// </summary>
        /// <param name="value">The string to validate</param>
        /// <param name="parameterName">The name of the parameter for error messages</param>
        /// <param name="minLength">The minimum required length</param>
        /// <exception cref="ArgumentException">Thrown when validation fails</exception>
        public static void ValidateMinLength(string value, string parameterName, int minLength)
        {
            ValidateString(value, parameterName);
            
            if (value.Length < minLength)
            {
                throw new ArgumentException($"{parameterName} must be at least {minLength} characters long, but was {value.Length}", parameterName);
            }
        }
        
        /// <summary>
        /// Validates that a string has a maximum length
        /// </summary>
        /// <param name="value">The string to validate</param>
        /// <param name="parameterName">The name of the parameter for error messages</param>
        /// <param name="maxLength">The maximum allowed length</param>
        /// <exception cref="ArgumentException">Thrown when validation fails</exception>
        public static void ValidateMaxLength(string value, string parameterName, int maxLength)
        {
            ValidateString(value, parameterName);
            
            if (value.Length > maxLength)
            {
                throw new ArgumentException($"{parameterName} must be at most {maxLength} characters long, but was {value.Length}", parameterName);
            }
        }
    }
}
