# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Context

This is **Alphiq 2.0** - a fresh project for the next generation of the Alphiq algorithmic trading platform. The original Alphiq system (in the adjacent `alphiq` directory) is a microservices-based forex trading platform integrating with CTrader.

## Original System Architecture (Reference)

Understanding the v1 architecture is crucial for designing v2 improvements:

### Microservices Communication
- **SignalR** for real-time market data streaming (pub/sub pattern)
  - BarsHub broadcasts bar close events to subscribers
  - Clients join groups via pattern: `"{symbolId}:{timeframe}"`
- **HTTP/REST** for command-and-control (order placement, config queries)
- **Supabase** as shared data store (strategies, symbols, candles, trades)

### Strategy Execution Architecture

**Plugin System via Attribute-Based Discovery:**
```csharp
[Strategy("MR_M5")]  // Auto-registered via reflection
public class FlexibleMeanReversionStrategy : ISignalStrategy { }
```

**Composable Risk Management:**
```
ISignalStrategy (entry logic)
  → IStopLossStrategy (SL calculation)
  → ITakeProfitStrategy (TP calculation)
  → IPositionSizingStrategy (lot sizing)
  → OrderContext (DTO containing complete order)
```

**Configuration Hot-Reload from Supabase:**
- Strategy definitions stored in `strategy_configs` table (JSON blob)
- Supports versioning (takes latest per strategy name)
- Parameters, timeframes, symbol lists all database-driven
- No code deployment needed to tune strategies

**Multi-Timeframe Data Management:**
- In-memory cache of rolling window bars per (symbol, timeframe)
- Per-symbol semaphores prevent cache corruption
- Strategies can request multiple timeframes (e.g., M5 signal + H1 trend filter)

### Backtest vs Live Execution

**Live Trading (`StrategyEngine`):**
- Event-driven: SignalR `BarClosed` triggers strategy evaluation
- Real-time order placement via HTTP to Orders API → CTrader
- TradeGate validation layer (position conflicts, cooldowns, risk checks)

**Backtesting (`BacktestEngine`):**
- Batch data load from PostgreSQL (all bars for date range)
- Global timeline sorted across all (symbol, timeframe) pairs
- Simulated fills at T+1 (next bar OPEN + spread/slippage/commission)
- In-memory position tracking (no external APIs)

**Genetic Optimizer:**
- Population-based parameter search (crossover + mutation)
- Parallel backtest execution via `Task.WhenAll`
- Fitness function: `1000*CAGR + 20*PF - 200*DD + trade_bonus`
- Guardrails: min trades, profit factor, max drawdown

### Key Pain Points in V1 (Opportunities for V2)

1. **No unified solution structure** - Each microservice has separate .sln
2. **Tight coupling to Supabase** - Hard to swap data layer
3. **Limited testing** - No unit test projects found
4. **Manual Docker orchestration** - docker-compose only, no k8s
5. **Strategy backtest/live divergence** - Different code paths can cause drift
6. **No distributed backtesting** - Single-node optimizer only
7. **Configuration scattered** - appsettings + env vars + Supabase tables
8. **No API gateway** - Services expose ports directly

## Development Commands (V1 Reference)

### Building Individual Services
```bash
# .NET services (each has own .sln)
cd alphiq/Alphiq.StrategyEngine
dotnet build Alphiq.StrategyEngine.sln

cd alphiq/CTrader.MarketData.Live
dotnet build CTrader.MarketData.Live.sln

# React frontend
cd alphiq/alphiq-candles-ui
npm install
npm run dev        # Development server (Vite)
npm run build      # Production build
```

### Running Locally
```bash
# Run individual .NET service
cd alphiq/Alphiq.StrategyEngine/Alphiq.StrategyEngine
dotnet run

# Run backtest
cd alphiq/Alphiq.StrategyEngine/Alphiq.StrategyEngine.Backtest
dotnet run

# Run optimizer
cd alphiq/Alphiq.StrategyEngine/Alphiq.StrategyEngine.Optimizer
dotnet run
```

### Docker Deployment
```bash
# Build and start all services
cd alphiq
docker-compose up --build

# Start specific service
docker-compose up marketdata_live

# View logs
docker-compose logs -f strategy_engine

# Stop all
docker-compose down
```

### Service Ports (V1)
- `5000`: MarketData.Live (real-time streaming)
- `5001`: MarketData.Historical
- `5002`: CTrader.Orders
- `5003`: TradeGate (analyzer/gateway)
- `5004`: MarketData.Backtest.Data
- `8080`: React frontend (forex-chart-frontend)

### Configuration Files
- `appsettings.json` - Base service configuration
- `appsettings.StrategyEngine.json` - Strategy-specific settings
- `appsettings.Optimizer.json` - Optimization parameters
- `docker-compose.yml` - Container orchestration + environment variables

### CTrader Token Setup
```bash
# Tokens stored in:
alphiq/CTrader.Tokens/ctrader_tokens/

# Structure expected by services:
# - access_token
# - refresh_token
# - expiry timestamp
```

## Architectural Patterns to Preserve

1. **Strategy Pattern** - Clean separation of signal/SL/TP/sizing logic
2. **Factory Pattern** - Dynamic instantiation based on config strings
3. **Event-Driven** - Bar close events trigger evaluation
4. **Dependency Injection** - Constructor injection throughout
5. **Repository Pattern** - Abstract data layer
6. **Chain of Responsibility** - TradeGate rule evaluation
7. **Template Method** - Base strategy classes define skeleton
8. **Builder Pattern** - OrderContext progressively enriched

## V2 Design Considerations

When working in this repository (alphiq2.0), consider:

1. **Unified solution** - Single .sln for all services
2. **Testability** - Repository pattern for all external dependencies
3. **Configuration** - Centralized config service (eliminate scattered settings)
4. **Observability** - Structured logging, metrics, tracing from day 1
5. **Deployment** - Kubernetes manifests alongside Docker
6. **Strategy parity** - Shared evaluation engine for backtest/live/replay
7. **Distributed optimization** - Job queue for parallel backtest runs
8. **API gateway** - Single entry point with routing/auth
9. **Event sourcing** - Append-only trade audit log
10. **Database flexibility** - Interface-driven (support PostgreSQL/TimescaleDB/ClickHouse)

## File Organization (Proposed for V2)

```
alphiq2.0/
├── src/
│   ├── Core/                  # Domain models, interfaces
│   ├── Infrastructure/        # Data access, external APIs
│   ├── Services/
│   │   ├── MarketData/
│   │   ├── Strategy/
│   │   ├── Orders/
│   │   ├── Gateway/
│   ├── WebApi/                # API Gateway
│   └── Frontend/              # React UI
├── tests/
│   ├── Unit/
│   ├── Integration/
│   └── Backtest/
├── deploy/
│   ├── docker/
│   └── k8s/
├── docs/
└── Alphiq.sln                 # Unified solution
```

## Technology Stack Recommendations

- **Runtime**: .NET 9.0 (consistent with V1)
- **API**: ASP.NET Core Minimal APIs or gRPC
- **Real-time**: SignalR (proven) or gRPC streaming
- **Frontend**: React + TypeScript + Vite
- **Database**: PostgreSQL 16+ (or TimescaleDB for time-series)
- **Caching**: Redis for distributed cache
- **Messaging**: RabbitMQ or Kafka for event bus
- **Observability**: OpenTelemetry + Prometheus + Grafana
- **Testing**: xUnit + FluentAssertions + Testcontainers
- **Containerization**: Docker + Kubernetes
