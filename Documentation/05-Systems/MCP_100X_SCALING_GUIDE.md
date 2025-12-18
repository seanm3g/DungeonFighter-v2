# 100x Scaling Guide for MCP Server

This guide outlines the architectural changes and infrastructure needed to scale the DungeonFighter MCP server to handle **40,000-80,000 concurrent bot instances** (100x current capacity).

## Current State vs Target State

| Metric | Current (Optimized) | Target (100x) | Required Changes |
|--------|-------------------|---------------|------------------|
| **Instances per machine** | 400-800 | 40,000-80,000 | Distributed architecture |
| **Total instances** | Single machine | 100+ machines | Cloud/distributed deployment |
| **Save mechanism** | File-based (`character_save.json`) | Database | Replace file I/O with DB |
| **State management** | Per-process | Shared/stateless | State management layer |
| **Deployment** | Manual | Containerized | Docker/Kubernetes |
| **Monitoring** | None | Comprehensive | Observability stack |

## Architecture Changes Required

### 1. Distributed Architecture

#### Current: Single Process per Bot
```
Bot 1 → Process 1 → Game Instance 1
Bot 2 → Process 2 → Game Instance 2
...
Bot N → Process N → Game Instance N
```

#### Target: Distributed with Load Balancing
```
                    ┌─────────────┐
                    │ Load Balancer│
                    │  (MCP Proxy) │
                    └──────┬───────┘
                           │
        ┌──────────────────┼──────────────────┐
        │                  │                  │
   ┌────▼────┐        ┌────▼────┐       ┌────▼────┐
   │ Server │        │ Server │       │ Server │
   │  Pool │        │  Pool │       │  Pool │
   │ (1000) │        │ (1000) │       │ (1000) │
   └────────┘        └────────┘       └────────┘
        │                  │                  │
        └──────────────────┼──────────────────┘
                           │
                    ┌──────▼───────┐
                    │   Database   │
                    │  (PostgreSQL)│
                    └──────────────┘
```

### 2. Database Migration (Critical)

**Current Problem:**
- File-based saves: `character_save.json`
- Single file per bot (or shared file = conflicts)
- No concurrent access support
- File I/O bottleneck

**Solution: Database-Backed Saves**

#### Implementation Steps:

1. **Create Database Schema**
```sql
-- Character saves table
CREATE TABLE character_saves (
    id UUID PRIMARY KEY,
    bot_id VARCHAR(255) UNIQUE NOT NULL,
    character_data JSONB NOT NULL,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW(),
    version INTEGER DEFAULT 1
);

-- Index for fast lookups
CREATE INDEX idx_character_saves_bot_id ON character_saves(bot_id);
CREATE INDEX idx_character_saves_updated_at ON character_saves(updated_at);

-- Game state cache (optional, for performance)
CREATE TABLE game_state_cache (
    bot_id VARCHAR(255) PRIMARY KEY,
    state_data JSONB NOT NULL,
    expires_at TIMESTAMP NOT NULL
);
```

2. **Create Database Abstraction Layer**

```csharp
// Code/MCP/Data/ICharacterRepository.cs
public interface ICharacterRepository
{
    Task<Character?> LoadCharacterAsync(string botId, CancellationToken ct = default);
    Task SaveCharacterAsync(string botId, Character character, CancellationToken ct = default);
    Task<bool> CharacterExistsAsync(string botId, CancellationToken ct = default);
    Task DeleteCharacterAsync(string botId, CancellationToken ct = default);
}

// Code/MCP/Data/PostgresCharacterRepository.cs
public class PostgresCharacterRepository : ICharacterRepository
{
    private readonly NpgsqlConnection _connection;
    
    public async Task<Character?> LoadCharacterAsync(string botId, CancellationToken ct = default)
    {
        // Load from PostgreSQL, deserialize JSON
    }
    
    public async Task SaveCharacterAsync(string botId, Character character, CancellationToken ct = default)
    {
        // Serialize to JSON, save to PostgreSQL with optimistic locking
    }
}
```

3. **Update CharacterSaveManager**

```csharp
// Replace file-based saves with database calls
public static class CharacterSaveManager
{
    private static ICharacterRepository? _repository;
    
    public static void Initialize(ICharacterRepository repository)
    {
        _repository = repository;
    }
    
    public static async Task SaveCharacterAsync(Character character, string botId)
    {
        if (_repository == null)
            throw new InvalidOperationException("Repository not initialized");
            
        await _repository.SaveCharacterAsync(botId, character);
    }
    
    public static async Task<Character?> LoadCharacterAsync(string botId)
    {
        if (_repository == null)
            throw new InvalidOperationException("Repository not initialized");
            
        return await _repository.LoadCharacterAsync(botId);
    }
}
```

4. **Update MCP Tools to Use Bot ID**

```csharp
[McpServerTool(Name = "start_new_game", Title = "Start New Game")]
[Description("Starts a new game instance. Returns the initial game state.")]
public static async Task<string> StartNewGame(
    [Description("Unique bot identifier")] string botId)
{
    // Use botId for save/load operations
    _gameWrapper.InitializeGame();
    // ...
}

[McpServerTool(Name = "save_game", Title = "Save Game")]
public static async Task<string> SaveGame(
    [Description("Unique bot identifier")] string botId)
{
    // Save using botId
    await _gameWrapper.SaveGameAsync(botId);
    // ...
}
```

### 3. Containerization (Docker)

#### Dockerfile

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Code/Code.csproj", "Code/"]
RUN dotnet restore "Code/Code.csproj"
COPY . .
WORKDIR "/src/Code"
RUN dotnet build "Code.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Code.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DF.dll", "MCP"]
```

#### Docker Compose (for local testing)

```yaml
version: '3.8'

services:
  postgres:
    image: postgres:15
    environment:
      POSTGRES_DB: dungeonfighter
      POSTGRES_USER: dfuser
      POSTGRES_PASSWORD: ${DB_PASSWORD}
    volumes:
      - postgres_data:/var/lib/postgresql/data
    ports:
      - "5432:5432"

  mcp-server-1:
    build: .
    environment:
      - DB_CONNECTION_STRING=Host=postgres;Database=dungeonfighter;Username=dfuser;Password=${DB_PASSWORD}
      - BOT_ID_PREFIX=bot-1-
    depends_on:
      - postgres

  mcp-server-2:
    build: .
    environment:
      - DB_CONNECTION_STRING=Host=postgres;Database=dungeonfighter;Username=dfuser;Password=${DB_PASSWORD}
      - BOT_ID_PREFIX=bot-2-
    depends_on:
      - postgres

  # ... scale as needed
```

### 4. Kubernetes Deployment

#### Deployment YAML

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: mcp-server
spec:
  replicas: 100  # 100 pods, each handling ~400-800 bots
  selector:
    matchLabels:
      app: mcp-server
  template:
    metadata:
      labels:
        app: mcp-server
    spec:
      containers:
      - name: mcp-server
        image: dungeonfighter-mcp:latest
        env:
        - name: DB_CONNECTION_STRING
          valueFrom:
            secretKeyRef:
              name: db-secret
              key: connection-string
        - name: POD_NAME
          valueFrom:
            fieldRef:
              fieldPath: metadata.name
        resources:
          requests:
            memory: "256Mi"
            cpu: "100m"
          limits:
            memory: "512Mi"
            cpu: "500m"
---
apiVersion: v1
kind: Service
metadata:
  name: mcp-server-service
spec:
  selector:
    app: mcp-server
  ports:
  - port: 80
    targetPort: 80
  type: LoadBalancer
```

### 5. Load Balancer / MCP Proxy

Since MCP uses stdio transport, you need a proxy that:
- Accepts MCP connections
- Routes to available server instances
- Handles connection pooling
- Provides health checks

#### MCP Proxy Implementation (Conceptual)

```csharp
// MCP Proxy that routes connections to backend servers
public class McpProxy
{
    private readonly List<McpServerInstance> _servers;
    private readonly ILoadBalancer _loadBalancer;
    
    public async Task HandleConnection(Stream clientStream)
    {
        // Get available server
        var server = _loadBalancer.GetNextServer();
        
        // Forward stdio streams
        await ForwardStreams(clientStream, server.Stream);
    }
}
```

**Alternative:** Use a message queue (RabbitMQ, Redis Streams) instead of direct stdio forwarding.

### 6. State Management

#### Option A: Stateless Design (Recommended)
- Each request includes full bot context
- No server-side state
- Scales horizontally easily
- Higher token usage (context in each request)

#### Option B: Shared State Cache
- Redis for game state caching
- Reduces database load
- Adds complexity
- Requires cache invalidation strategy

```csharp
// Redis cache for game state
public class GameStateCache
{
    private readonly IDatabase _redis;
    
    public async Task<GameStateSnapshot?> GetStateAsync(string botId)
    {
        var cached = await _redis.StringGetAsync($"state:{botId}");
        if (cached.HasValue)
            return JsonSerializer.Deserialize<GameStateSnapshot>(cached!);
        return null;
    }
    
    public async Task SetStateAsync(string botId, GameStateSnapshot state, TimeSpan ttl)
    {
        var json = JsonSerializer.Serialize(state);
        await _redis.StringSetAsync($"state:{botId}", json, ttl);
    }
}
```

### 7. Monitoring & Observability

#### Required Metrics:
- **Instance count** per server
- **Memory usage** per server
- **CPU usage** per server
- **Token usage** per bot/interaction
- **Database connection pool** usage
- **Request latency** (p50, p95, p99)
- **Error rates**

#### Tools:
- **Prometheus** for metrics
- **Grafana** for dashboards
- **ELK Stack** (Elasticsearch, Logstash, Kibana) for logs
- **Jaeger** for distributed tracing

#### Example Prometheus Metrics

```csharp
// Add to MCP server
private static readonly Counter BotInteractions = Metrics
    .CreateCounter("mcp_bot_interactions_total", "Total bot interactions");

private static readonly Gauge ActiveBots = Metrics
    .CreateGauge("mcp_active_bots", "Number of active bots");

private static readonly Histogram ResponseTime = Metrics
    .CreateHistogram("mcp_response_time_seconds", "Response time in seconds");
```

### 8. Resource Optimization

#### Memory Optimization
- **Object pooling** for game instances (reuse Game objects)
- **String interning** for common strings
- **Lazy loading** for game data
- **Garbage collection tuning** (server GC mode)

#### CPU Optimization
- **Async/await** everywhere (already done)
- **Connection pooling** for database
- **Read replicas** for database (scale reads)
- **Caching** frequently accessed data

#### Network Optimization
- **Compression** for JSON responses (gzip)
- **Connection keep-alive**
- **Batch operations** where possible

### 9. Configuration Management

#### Environment Variables
```bash
# Database
DB_CONNECTION_STRING=Host=postgres;Database=df;...
DB_POOL_SIZE=100

# Redis (if using cache)
REDIS_CONNECTION_STRING=redis://redis:6379

# Scaling
MAX_BOTS_PER_INSTANCE=800
INSTANCE_ID=server-1

# Monitoring
PROMETHEUS_ENDPOINT=http://prometheus:9090
```

### 10. Testing Strategy

#### Load Testing
- **Locust** or **k6** for load testing
- Test with 1000, 5000, 10000 concurrent bots
- Measure:
  - Response times
  - Memory usage
  - Database performance
  - Token usage

#### Chaos Engineering
- Randomly kill instances
- Database connection failures
- Network partitions
- Verify system recovers gracefully

## Implementation Roadmap

### Phase 1: Database Migration (Week 1-2)
1. ✅ Create database schema
2. ✅ Implement `ICharacterRepository`
3. ✅ Update `CharacterSaveManager`
4. ✅ Add database connection pooling
5. ✅ Migrate existing saves (if any)

### Phase 2: Containerization (Week 2-3)
1. ✅ Create Dockerfile
2. ✅ Docker Compose for local testing
3. ✅ Test with multiple containers
4. ✅ Verify database connectivity

### Phase 3: Distributed Deployment (Week 3-4)
1. ✅ Kubernetes manifests
2. ✅ Load balancer/proxy
3. ✅ Health checks
4. ✅ Auto-scaling configuration

### Phase 4: Monitoring (Week 4-5)
1. ✅ Prometheus metrics
2. ✅ Grafana dashboards
3. ✅ Logging infrastructure
4. ✅ Alerting rules

### Phase 5: Optimization (Week 5-6)
1. ✅ Performance tuning
2. ✅ Load testing
3. ✅ Capacity planning
4. ✅ Documentation

## Cost Estimates (Cloud)

### AWS Example (100x scaling)

| Resource | Count | Cost/Month |
|----------|-------|------------|
| **EC2 Instances** (c5.xlarge) | 100 | ~$15,000 |
| **RDS PostgreSQL** (db.r5.2xlarge) | 1 | ~$500 |
| **ElastiCache Redis** (cache.r5.large) | 3 | ~$300 |
| **Load Balancer** | 1 | ~$20 |
| **Data Transfer** | - | ~$200 |
| **Total** | | **~$16,000/month** |

### Azure/GCP Similar pricing

**Note:** Costs scale with actual usage. Start smaller and scale up.

## Scaling Checklist

- [ ] Database migration complete
- [ ] Containerization (Docker) working
- [ ] Kubernetes deployment configured
- [ ] Load balancer/proxy implemented
- [ ] Monitoring and alerting set up
- [ ] Load testing completed
- [ ] Auto-scaling configured
- [ ] Disaster recovery plan
- [ ] Documentation updated
- [ ] Team trained on new architecture

## Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| **Database bottleneck** | High | Read replicas, connection pooling, caching |
| **Memory exhaustion** | High | Resource limits, auto-scaling, monitoring |
| **Token usage spikes** | Medium | Rate limiting, quotas per bot |
| **Network latency** | Medium | Regional deployment, CDN for static data |
| **Data loss** | Critical | Database backups, replication |
| **Single point of failure** | Critical | Multi-region deployment, failover |

## Next Steps

1. **Start with database migration** - This is the foundation
2. **Test with 10x scaling first** - Validate architecture
3. **Incremental scaling** - 10x → 50x → 100x
4. **Monitor everything** - Metrics are critical
5. **Iterate and optimize** - Learn from production

---

*This is a comprehensive guide. Start with Phase 1 and iterate. 100x scaling is achievable but requires careful planning and execution.*

