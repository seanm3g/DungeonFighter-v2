# Configuration Management System

## Overview
The Configuration Management System provides a unified, type-safe way to load, save, and manage all game configurations. Implemented in October 2025 as part of the refactoring initiative.

## Architecture

### ConfigurationManager
Central manager providing:
- **Type-safe loading/saving**: Generic methods ensure compile-time type safety
- **Automatic caching**: Loaded configurations are cached for performance
- **Path resolution**: Uses `JsonLoader` for consistent file path finding
- **Validation support**: Optional validation functions for configuration integrity
- **Hot-reload capability**: Clear cache and reload without restart
- **Singleton pattern**: Built-in singleton access for configurations

### File Location
- **Path**: `Code/Config/ConfigurationManager.cs`
- **Namespace**: `RPGGame`
- **Dependencies**: `JsonLoader`, `ErrorHandler`, `DebugLogger`

## Basic Usage

### Loading a Configuration
```csharp
// Load using default file name (TypeName.json)
var uiConfig = ConfigurationManager.Load<UIConfiguration>();

// Load with custom file name
var customConfig = ConfigurationManager.Load<UIConfiguration>("custom-ui.json");

// Load without caching
var freshConfig = ConfigurationManager.Load<UIConfiguration>(useCache: false);

// Load with validation
var validatedConfig = ConfigurationManager.Load<UIConfiguration>(
    validator: ConfigurationValidation.ValidateUIConfiguration
);
```

### Saving a Configuration
```csharp
// Save configuration
var config = new UIConfiguration();
config.EnableDelays = true;
bool success = ConfigurationManager.Save(config);

// Save with custom file name
bool success = ConfigurationManager.Save(config, "custom-ui.json");

// Save without updating cache
bool success = ConfigurationManager.Save(config, updateCache: false);
```

### Using Singleton Pattern
```csharp
// Access singleton instance (auto-loads on first access)
var uiConfig = ConfigurationManager.Singleton<UIConfiguration>.Instance;

// Reload singleton
ConfigurationManager.Singleton<UIConfiguration>.Reload();

// Clear singleton (forces reload on next access)
ConfigurationManager.Singleton<UIConfiguration>.ClearInstance();
```

## Advanced Usage

### Cache Management
```csharp
// Check if configuration is cached
bool isCached = ConfigurationManager.IsCached<UIConfiguration>();

// Clear specific configuration from cache
ConfigurationManager.ClearCache<UIConfiguration>();

// Clear all cached configurations
ConfigurationManager.ClearAllCaches();

// Reload configuration from disk (bypasses cache)
var freshConfig = ConfigurationManager.Reload<UIConfiguration>();
```

### Custom File Mappings
```csharp
// Register custom file name for a configuration type
ConfigurationManager.RegisterFileMapping<MyCustomConfig>("my-config.json");

// Now Load<MyCustomConfig>() will automatically use my-config.json
var config = ConfigurationManager.Load<MyCustomConfig>();
```

### Configuration Validation
```csharp
// Define a validation function
public static bool ValidateMyConfig(MyConfig config)
{
    if (config == null) return false;
    if (config.MaxValue < config.MinValue) return false;
    return true;
}

// Load with validation
var config = ConfigurationManager.Load<MyConfig>(
    validator: ValidateMyConfig
);
```

## Default File Mappings

### Pre-registered Configurations
| Configuration Type | Default File Name |
|-------------------|-------------------|
| `UIConfiguration` | `UIConfiguration.json` |
| `GameSettings` | `gamesettings.json` |

### Convention-based Naming
If no mapping is registered, the system uses the convention: `TypeName.json`

Example:
- `MyCustomConfig` → `MyCustomConfig.json`
- `PlayerSettings` → `PlayerSettings.json`

## Configuration Validation

### Built-in Validators

#### UIConfiguration Validation
```csharp
bool isValid = ConfigurationValidation.ValidateUIConfiguration(config);
```

Validates:
- Beat timing ranges (0-10,000ms for combat, 0-1,000ms for menu)
- White temperature intensity (0.0-5.0)
- Non-null references

#### GameSettings Validation
```csharp
bool isValid = ConfigurationValidation.ValidateGameSettings(settings);
```

### Creating Custom Validators
```csharp
public static class MyConfigValidation
{
    public static bool ValidateMyConfig(MyConfig config)
    {
        // Null check
        if (config == null) return false;
        
        // Range validation
        if (config.Value < 0 || config.Value > 100)
        {
            ErrorHandler.LogWarning("Value must be between 0 and 100");
            return false;
        }
        
        // Complex validation
        if (config.EnableFeature && string.IsNullOrEmpty(config.FeatureData))
        {
            ErrorHandler.LogWarning("FeatureData required when EnableFeature is true");
            return false;
        }
        
        return true;
    }
}
```

## Migration from Legacy Patterns

### Before: UIConfiguration
```csharp
// Old way
var config = UIConfiguration.LoadFromFile();
config.SaveToFile();
```

### After: ConfigurationManager
```csharp
// New way (backward compatible - old methods still work)
var config = ConfigurationManager.Load<UIConfiguration>();
ConfigurationManager.Save(config);

// Or use singleton pattern
var config = ConfigurationManager.Singleton<UIConfiguration>.Instance;
```

### Benefits of Migration
1. **Consistency**: All configurations use the same loading pattern
2. **Caching**: Automatic caching improves performance
3. **Validation**: Built-in validation support
4. **Hot-reload**: Easy to reload without restart
5. **Type-safety**: Generic methods ensure compile-time safety

## Examples

### Example 1: Simple Loading
```csharp
// Load UI configuration
var uiConfig = ConfigurationManager.Load<UIConfiguration>();

// Use configuration
if (uiConfig.EnableDelays)
{
    int delay = uiConfig.GetEffectiveDelay(UIMessageType.Combat);
    Thread.Sleep(delay);
}
```

### Example 2: Validated Loading
```csharp
// Load with validation
var uiConfig = ConfigurationManager.Load<UIConfiguration>(
    validator: ConfigurationValidation.ValidateUIConfiguration
);

// If validation fails, default configuration is returned
Console.WriteLine($"Using delays: {uiConfig.EnableDelays}");
```

### Example 3: Hot-reload
```csharp
// Initial load
var config = ConfigurationManager.Load<UIConfiguration>();
Console.WriteLine($"Initial: {config.EnableDelays}");

// ... user modifies UIConfiguration.json ...

// Reload from disk
config = ConfigurationManager.Reload<UIConfiguration>();
Console.WriteLine($"Reloaded: {config.EnableDelays}");
```

### Example 4: Singleton Pattern
```csharp
// Access anywhere in codebase
public class MyGameSystem
{
    public void DoSomething()
    {
        var config = ConfigurationManager.Singleton<UIConfiguration>.Instance;
        if (config.EnableDelays)
        {
            // ... apply delays ...
        }
    }
}
```

### Example 5: Custom Configuration
```csharp
// Define custom configuration
public class PlayerPreferences
{
    public string PreferredWeapon { get; set; } = "Sword";
    public int Difficulty { get; set; } = 1;
    public bool ShowTutorials { get; set; } = true;
}

// Register file mapping
ConfigurationManager.RegisterFileMapping<PlayerPreferences>("player-prefs.json");

// Load and use
var prefs = ConfigurationManager.Load<PlayerPreferences>();
Console.WriteLine($"Preferred weapon: {prefs.PreferredWeapon}");

// Modify and save
prefs.PreferredWeapon = "Axe";
ConfigurationManager.Save(prefs);
```

## Performance Considerations

### Caching Strategy
- **First Load**: Reads from disk, parses JSON, caches result
- **Subsequent Loads**: Returns cached instance (no disk I/O)
- **Memory Impact**: Minimal - only loaded configurations are cached
- **Thread Safety**: Cache access is not currently thread-safe (single-threaded usage assumed)

### Best Practices
1. **Use Caching**: Enable caching for frequently accessed configurations
2. **Singleton for Globals**: Use singleton pattern for application-wide configurations
3. **Validate Once**: Validate on load, not on every access
4. **Clear Strategically**: Only clear cache when configuration changes
5. **Batch Saves**: Minimize disk writes by batching configuration updates

## Error Handling

### Load Failures
When loading fails:
1. Error is logged via `ErrorHandler`
2. Default configuration instance is returned
3. Application continues with safe defaults

### Save Failures
When saving fails:
1. Error is logged via `ErrorHandler`
2. Method returns `false`
3. Cache is not updated
4. Existing file is not corrupted

### Validation Failures
When validation fails:
1. Warning is logged
2. Default configuration is returned
3. Application continues with safe defaults

## Integration with Existing Systems

### GameConfiguration
`GameConfiguration` maintains its own loading logic for backward compatibility and because it aggregates multiple configuration domains.

```csharp
// GameConfiguration continues to work as before
var gameConfig = GameConfiguration.Instance;
var combatConfig = gameConfig.Combat;
```

### UIConfiguration
`UIConfiguration` maintains its old `LoadFromFile()` and `SaveToFile()` methods for backward compatibility.

```csharp
// Old way still works
var config1 = UIConfiguration.LoadFromFile();

// New way also works
var config2 = ConfigurationManager.Load<UIConfiguration>();
```

## Future Enhancements

### Planned Features
1. **Thread-safe caching**: Lock-based or concurrent dictionary
2. **Change notifications**: Events when configuration changes
3. **Configuration merging**: Merge multiple configuration sources
4. **Environment-specific configs**: Dev, staging, production configs
5. **Configuration encryption**: Encrypt sensitive configuration values
6. **Remote configuration**: Load configurations from remote sources
7. **Configuration versioning**: Handle configuration schema changes

### Potential Improvements
- **Auto-save on change**: Automatically save when configuration changes
- **Configuration diff**: Show differences between configurations
- **Configuration backup**: Automatic backup before saves
- **Configuration history**: Track configuration changes over time

## Related Documentation

- **`CODE_PATTERNS.md`**: Configuration patterns and best practices
- **`ARCHITECTURE.md`**: System architecture including configuration layer
- **`DATA_MANAGEMENT.md`**: Data loading and management patterns
- **`JsonLoader.cs`**: Underlying JSON loading utility

## Summary

The ConfigurationManager provides a modern, unified approach to configuration management that:
- ✅ Eliminates code duplication across configuration loaders
- ✅ Provides type-safe, validated configuration loading
- ✅ Supports caching for performance
- ✅ Enables hot-reload without restart
- ✅ Maintains backward compatibility
- ✅ Follows established patterns and conventions

---

*Last Updated: October 2025*
*Part of the Configuration System Consolidation refactoring initiative*

