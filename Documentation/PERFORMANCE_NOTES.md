# Performance Notes - DungeonFighter-v2

Performance considerations, optimizations, and monitoring for the DungeonFighter-v2 codebase.

## Performance Overview

### Current Performance Characteristics
- **Startup Time**: ~2-3 seconds (includes JSON loading and initialization)
- **Combat Execution**: ~100-500ms per combat (depending on complexity)
- **Memory Usage**: ~50-100MB typical, ~200MB peak
- **JSON Loading**: ~100-200ms for all game data
- **Balance Analysis**: ~1-5 seconds for comprehensive analysis

### Performance Targets
- **Combat Response**: <100ms for simple actions
- **Menu Navigation**: <50ms response time
- **Data Loading**: <500ms for all game data
- **Memory Usage**: <200MB peak usage
- **Startup Time**: <5 seconds total

## Performance Optimizations

### 1. Lazy Loading
**Purpose**: Load data only when needed to reduce startup time

**Implementation**:
```csharp
public class ActionLoader
{
    private static Dictionary<string, Action> _actionCache;
    
    public static Action GetAction(string actionName)
    {
        if (_actionCache == null)
        {
            LoadAllActions(); // Load only when first accessed
        }
        
        return _actionCache.TryGetValue(actionName, out var action) ? action : null;
    }
}
```

**Benefits**:
- Faster startup time
- Reduced memory usage
- On-demand resource loading

### 2. Object Pooling
**Purpose**: Reuse objects to reduce garbage collection pressure

**Implementation**:
```csharp
public class EnemyPool
{
    private readonly Queue<Enemy> _availableEnemies = new Queue<Enemy>();
    private readonly List<Enemy> _allEnemies = new List<Enemy>();
    
    public Enemy GetEnemy(string enemyType, int level)
    {
        Enemy enemy;
        
        if (_availableEnemies.Count > 0)
        {
            enemy = _availableEnemies.Dequeue();
            enemy.Reset(enemyType, level); // Reuse existing object
        }
        else
        {
            enemy = EnemyFactory.CreateEnemy(enemyType, level);
            _allEnemies.Add(enemy);
        }
        
        return enemy;
    }
}
```

**Benefits**:
- Reduced garbage collection
- Lower memory allocation
- Improved performance consistency

### 3. Caching
**Purpose**: Store frequently accessed data to avoid recalculation

**Implementation**:
```csharp
public class Character
{
    private int? _cachedTotalArmor;
    private List<ArmorItem> _lastArmorList;
    
    public int GetTotalArmor()
    {
        var currentArmor = GetEquippedArmor();
        
        // Check if cache is still valid
        if (_cachedTotalArmor.HasValue && 
            _lastArmorList != null && 
            _lastArmorList.SequenceEqual(currentArmor))
        {
            return _cachedTotalArmor.Value;
        }
        
        // Recalculate and cache
        _cachedTotalArmor = currentArmor.Sum(armor => armor.ArmorValue);
        _lastArmorList = new List<ArmorItem>(currentArmor);
        
        return _cachedTotalArmor.Value;
    }
}
```

**Benefits**:
- Faster repeated calculations
- Reduced CPU usage
- Improved response times

### 4. Efficient Data Structures
**Purpose**: Use appropriate data structures for fast lookups

**Implementation**:
```csharp
// Use Dictionary for O(1) lookups
private readonly Dictionary<string, Action> _actionCache = new Dictionary<string, Action>();

// Use List for ordered collections
private readonly List<CombatEvent> _combatHistory = new List<CombatEvent>();

// Use HashSet for unique collections
private readonly HashSet<string> _availableActions = new HashSet<string>();
```

**Benefits**:
- Fast lookups and searches
- Appropriate memory usage
- Optimized for specific use cases

## Performance Monitoring

### 1. Execution Time Monitoring
**Purpose**: Track how long operations take

**Implementation**:
```csharp
public class PerformanceMonitor
{
    public static T MeasureExecutionTime<T>(Func<T> operation, string operationName)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = operation();
        stopwatch.Stop();
        
        DebugLogger.Log($"Performance: {operationName} took {stopwatch.ElapsedMilliseconds}ms");
        return result;
    }
}

// Usage
var result = PerformanceMonitor.MeasureExecutionTime(
    () => CombatCalculator.CalculateDamage(player, enemy, action),
    "Damage Calculation"
);
```

### 2. Memory Usage Monitoring
**Purpose**: Track memory consumption and garbage collection

**Implementation**:
```csharp
public class MemoryMonitor
{
    public static void LogMemoryUsage(string context)
    {
        var memory = GC.GetTotalMemory(false);
        var memoryMB = memory / (1024.0 * 1024.0);
        
        DebugLogger.Log($"Memory: {context} - {memoryMB:F2}MB");
    }
    
    public static void ForceGarbageCollection()
    {
        var memoryBefore = GC.GetTotalMemory(false);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var memoryAfter = GC.GetTotalMemory(false);
        
        var freed = (memoryBefore - memoryAfter) / (1024.0 * 1024.0);
        DebugLogger.Log($"GC freed {freed:F2}MB");
    }
}
```

### 3. Performance Profiling
**Purpose**: Identify performance bottlenecks

**Implementation**:
```csharp
public class PerformanceProfiler
{
    private static readonly Dictionary<string, List<long>> _measurements = new Dictionary<string, List<long>>();
    
    public static void StartMeasurement(string operation)
    {
        // Implementation for starting measurement
    }
    
    public static void EndMeasurement(string operation)
    {
        // Implementation for ending measurement
    }
    
    public static void GenerateReport()
    {
        foreach (var kvp in _measurements)
        {
            var times = kvp.Value;
            var avg = times.Average();
            var min = times.Min();
            var max = times.Max();
            
            DebugLogger.Log($"Performance Report - {kvp.Key}:");
            DebugLogger.Log($"  Average: {avg:F2}ms");
            DebugLogger.Log($"  Min: {min}ms");
            DebugLogger.Log($"  Max: {max}ms");
        }
    }
}
```

## Performance Bottlenecks

### 1. JSON Loading
**Issue**: Loading large JSON files can be slow
**Impact**: Startup time, memory usage
**Solutions**:
- Lazy loading of JSON data
- Caching loaded data
- Optimizing JSON structure
- Using streaming for large files

### 2. Combat Calculations
**Issue**: Complex damage calculations in tight loops
**Impact**: Combat response time
**Solutions**:
- Caching calculation results
- Optimizing mathematical operations
- Reducing calculation complexity
- Using lookup tables where appropriate

### 3. String Operations
**Issue**: Frequent string concatenation and formatting
**Impact**: Memory allocation, garbage collection
**Solutions**:
- Using StringBuilder for concatenation
- Caching formatted strings
- Reducing string operations
- Using string interpolation efficiently

### 4. Collection Operations
**Issue**: Inefficient collection operations
**Impact**: CPU usage, memory allocation
**Solutions**:
- Using appropriate data structures
- Avoiding unnecessary iterations
- Using LINQ efficiently
- Pre-allocating collections when possible

## Performance Testing

### 1. Load Testing
**Purpose**: Test performance under various loads

**Implementation**:
```csharp
[Test]
public void TestCombatPerformance()
{
    var stopwatch = Stopwatch.StartNew();
    
    // Run 1000 combat simulations
    for (int i = 0; i < 1000; i++)
    {
        var player = new Character("TestPlayer");
        var enemy = new Enemy("TestEnemy", 1);
        var combat = new Combat(player, enemy);
        combat.ExecuteCombat();
    }
    
    stopwatch.Stop();
    
    Assert.IsTrue(stopwatch.ElapsedMilliseconds < 10000, 
                  "1000 combats should complete in under 10 seconds");
}
```

### 2. Memory Testing
**Purpose**: Test memory usage and garbage collection

**Implementation**:
```csharp
[Test]
public void TestMemoryUsage()
{
    var memoryBefore = GC.GetTotalMemory(false);
    
    // Create many objects
    var characters = new List<Character>();
    for (int i = 0; i < 1000; i++)
    {
        characters.Add(new Character($"TestHero{i}"));
    }
    
    var memoryAfter = GC.GetTotalMemory(false);
    var memoryUsed = memoryAfter - memoryBefore;
    
    Assert.IsTrue(memoryUsed < 100 * 1024 * 1024, // 100MB
                  "Memory usage should be reasonable");
}
```

### 3. Stress Testing
**Purpose**: Test performance under extreme conditions

**Implementation**:
```csharp
[Test]
public void TestStressPerformance()
{
    var stopwatch = Stopwatch.StartNew();
    
    // Run intensive operations
    for (int i = 0; i < 10000; i++)
    {
        var actions = ActionLoader.LoadActions();
        var enemies = EnemyLoader.LoadEnemies();
        var items = ItemGenerator.GenerateItems(100);
    }
    
    stopwatch.Stop();
    
    Assert.IsTrue(stopwatch.ElapsedMilliseconds < 30000, 
                  "Stress test should complete in under 30 seconds");
}
```

## Performance Best Practices

### 1. Code Optimization
- Use appropriate data structures
- Avoid unnecessary object creation
- Cache expensive calculations
- Use lazy loading where appropriate
- Optimize loops and iterations

### 2. Memory Management
- Dispose of resources properly
- Use object pooling for frequently created objects
- Avoid memory leaks
- Monitor garbage collection
- Use weak references where appropriate

### 3. Algorithm Efficiency
- Choose efficient algorithms
- Avoid nested loops when possible
- Use lookup tables for repeated calculations
- Optimize mathematical operations
- Consider space-time tradeoffs

### 4. System Design
- Design for performance from the start
- Consider scalability requirements
- Use appropriate design patterns
- Minimize system complexity
- Plan for future optimization

## Performance Monitoring Tools

### Built-in Tools
- **DebugLogger**: Log performance metrics
- **PerformanceMonitor**: Measure execution times
- **MemoryMonitor**: Track memory usage
- **PerformanceProfiler**: Profile operations

### External Tools
- **Visual Studio Profiler**: Detailed performance analysis
- **dotMemory**: Memory profiling
- **PerfView**: Performance analysis
- **BenchmarkDotNet**: Benchmarking framework

## Performance Optimization Checklist

### Before Optimization
- [ ] Identify performance bottlenecks
- [ ] Measure current performance
- [ ] Set performance targets
- [ ] Plan optimization approach

### During Optimization
- [ ] Make incremental changes
- [ ] Test after each change
- [ ] Measure performance impact
- [ ] Verify functionality still works

### After Optimization
- [ ] Run performance tests
- [ ] Verify performance targets met
- [ ] Check for regressions
- [ ] Document optimization changes

## Performance Maintenance

### Regular Monitoring
- Monitor performance metrics
- Track memory usage trends
- Check for performance regressions
- Review optimization opportunities

### Performance Reviews
- Regular performance audits
- Code review for performance
- Architecture review for scalability
- Technology review for improvements

### Continuous Improvement
- Stay updated on performance best practices
- Experiment with new optimization techniques
- Share performance knowledge
- Learn from performance issues

## Related Documentation

- **`CODE_PATTERNS.md`**: Performance patterns and optimization techniques
- **`TESTING_STRATEGY.md`**: Performance testing approaches
- **`DEBUGGING_GUIDE.md`**: Performance debugging techniques
- **`DEVELOPMENT_WORKFLOW.md`**: Performance considerations in development
- **`QUICK_REFERENCE.md`**: Performance monitoring commands

---

*This document should be updated as performance characteristics change and new optimizations are implemented.*
