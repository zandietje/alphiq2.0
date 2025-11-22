# CLAUDE.md

Guidance for Claude Code when working inside this repository.

---

# 📌 Project Overview

This repository contains **Alphiq 2.0**, the next-generation architecture of the Alphiq algorithmic trading platform.

Two folders exist:

- `alphiq/` → **Alphiq v1** (reference only)
- `alphiq2.0/` → **Alphiq v2** (all new development happens here)

Alphiq 2.0 is designed to be:

- Faster
- Unified
- Extendable
- SaaS-ready
- Easier to deploy via .NET Aspire
- Built around a single, correct trading engine used in all modes

---

# PART 1: ALPHIQ V1 REFERENCE ARCHITECTURE

This section documents the existing Alphiq v1 system for reference. Claude Code should understand this architecture to inform v2 design decisions and avoid repeating v1's limitations.

---

## V1 System Overview

Alphiq v1 is a **microservices-based forex trading platform** that integrates with CTrader. It consists of multiple independent services built with .NET 8/9, Python, and React.

### V1 Service Architecture

**Core Trading Services (.NET 8/9):**
- `CTrader.MarketData.Live` (Port 5000) - Real-time market data streaming via SignalR
- `CTrader.MarketData.Historical` (Port 5001) - Historical candle retrieval
- `CTrader.MarketData.Backtest.Data` (Port 5004) - Backtest data service
- `CTrader.Orders` (Port 5002) - Order execution to CTrader
- `Alphiq.TradeGate` (Port 5003) - Pre-trade validation gateway
- `Alphiq.StrategyEngine` - Main strategy execution engine
- `Alphiq.StrategyEngine.Optimizer` - Genetic algorithm optimizer
- `Alphiq.StrategyEngine.Backtest` - Backtesting engine
- `Alphiq.StrategyEngine.Replay` - Trade replay engine

**Data Sync Services (Python):**
- `AccountInfoSync` - Syncs account balance to Supabase every 10 seconds
- `SymbolSync` - Syncs trading symbols to Supabase daily
- `CandlesGapFiller` - Repairs missing candle data

**Frontend:**
- `alphiq-candles-ui` (Port 8080) - React + Vite UI with lightweight-charts

**Data Store:**
- Supabase (PostgreSQL) - Stores strategies, symbols, candles, trades, account info

---

## V1 Communication Architecture

### SignalR for Real-Time Market Data

**BarsHub Pattern:**
```csharp
// Server: CTrader.MarketData.Live/Hubs/BarsHub.cs
public class BarsHub : Hub
{
    public async Task JoinGroup(string symbolId, string timeframe)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"{symbolId}:{timeframe}");
    }

    // Broadcast when bar closes
    public async Task BroadcastBarClosed(string symbolId, string timeframe, Bar bar)
    {
        await Clients.Group($"{symbolId}:{timeframe}")
            .SendAsync("BarClosed", symbolId, timeframe, bar);
    }
}
```

**Client Subscription:**
```csharp
// StrategyEngine connects and subscribes to required (symbol, timeframe) pairs
var connection = new HubConnectionBuilder()
    .WithUrl("http://marketdata_live:8080/hubs/bars")
    .Build();

connection.On<string, string, Bar>("BarClosed", async (symbolId, timeframe, bar) =>
{
    await OnBarClosedAsync(symbolId, timeframe, bar);
});

await connection.StartAsync();
await connection.InvokeAsync("JoinGroup", "12345", "M5");
```

### HTTP/REST for Commands & Queries

**Service Communication Map:**
```
CTrader API
    ↓
MarketData.Live (Port 5000) ──SignalR──→ StrategyEngine
    ↓ HTTP                                    ↓ HTTP
Supabase (candles table)              TradeGate (Port 5003)
                                              ↓
                                         Orders API (Port 5002)
                                              ↓
                                         CTrader Orders API
```

**Key API Endpoints:**
- `GET /api/Bars/history` - Fetch historical candles
- `POST /api/Orders` - Place market/limit orders
- `POST /api/analyze` - TradeGate pre-trade validation
- `GET /api/account/balance` - Current account balance

---

## V1 Strategy Execution Architecture

### Attribute-Based Plugin Discovery

**Strategy Registration:**
```csharp
[Strategy("MR_M5")]  // Auto-discovered via reflection
public class MR_M5 : FlexibleMeanReversionStrategy
{
    public MR_M5(StrategyDefinition cfg,
                 IStopLossFactory slFactory,
                 ITakeProfitFactory tpFactory,
                 IPositionSizingFactory psFactory)
        : base(cfg, slFactory, tpFactory, psFactory) { }
}

[StopLoss("ATR")]
public class ATRStopLoss : IStopLossStrategy { }

[TakeProfit("Multiplier")]
public class MultiplierTakeProfit : ITakeProfitStrategy { }

[PositionSizing("RiskPercent")]
public class RiskPercentSizer : IPositionSizingStrategy { }
```

### Strategy Factory Pattern

**Discovery & Instantiation** (`IStrategyFactory.cs`):
1. **Assembly scanning** - Reflection finds all types with `[Strategy]` attribute
2. **Attribute extraction** - Builds `Dictionary<string, Type>` mapping strategy keys to types
3. **DI-based creation** - Uses `ActivatorUtilities.CreateInstance()` with optional config

**Factory Implementation:**
```csharp
public class StrategyFactory : IStrategyFactory
{
    private readonly Dictionary<string, Type> _registry;
    private readonly IServiceProvider _services;

    public StrategyFactory(IServiceProvider services)
    {
        _services = services;
        _registry = ScanForStrategies();
    }

    private Dictionary<string, Type> ScanForStrategies()
    {
        var types = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => t.GetCustomAttribute<StrategyAttribute>() != null);

        return types.ToDictionary(
            t => t.GetCustomAttribute<StrategyAttribute>().Name,
            t => t
        );
    }

    public bool TryCreate(StrategyDefinition def, out ISignalStrategy? strategy)
    {
        if (!_registry.TryGetValue(def.Name, out var type))
        {
            strategy = null;
            return false;
        }

        strategy = (ISignalStrategy)ActivatorUtilities.CreateInstance(_services, type, def);
        return true;
    }
}
```

### Configuration Loading from Supabase

**SupabaseStrategyConfigProvider.cs:**
```csharp
public async Task<IEnumerable<(StrategyDefinition, string[])>> LoadAsync(CancellationToken ct)
{
    // Fetch from strategy_configs table
    var configs = await _supabase
        .From("strategy_configs")
        .Where(x => x.Enabled == true)
        .OrderBy(x => x.Name)
        .ThenByDescending(x => x.Version)
        .Get<StrategyConfigRow>();

    // Take latest version per strategy name
    var latest = configs
        .GroupBy(c => c.Name)
        .Select(g => g.First());

    // Deserialize JSON config blob
    return latest.Select(row => (
        JsonSerializer.Deserialize<StrategyDefinition>(row.Config),
        row.SymbolList
    ));
}
```

**Database Schema:**
```sql
CREATE TABLE strategy_configs (
    id BIGSERIAL PRIMARY KEY,
    name TEXT NOT NULL,                  -- e.g., "MR_M5"
    version INT NOT NULL DEFAULT 1,
    enabled BOOLEAN NOT NULL DEFAULT true,
    main_timeframe TEXT NOT NULL,        -- e.g., "M5"
    config JSONB NOT NULL,               -- StrategyDefinition JSON blob
    symbol_list TEXT[] NOT NULL,         -- ["EURUSD", "GBPUSD"]
    created_at TIMESTAMPTZ DEFAULT NOW(),
    UNIQUE(name, version)
);
```

**StrategyDefinition JSON Example:**
```json
{
  "Name": "MR_M5",
  "MainTimeframe": "M5",
  "Version": 3,
  "Timeframes": { "M5": 300, "H1": 100 },
  "Parameters": {
    "UseBollinger": true,
    "BbPeriod": 20,
    "BbStdDev": 1.8,
    "UseRSI": true,
    "RsiPeriod": 14,
    "OversoldLevel": 35.0,
    "OverboughtLevel": 65.0,
    "SessionStartUTC": "03:00",
    "SessionEndUTC": "20:00"
  },
  "Risk": {
    "StopLoss": {
      "Type": "ATR",
      "Parameters": { "period": 14, "multiplier": 1.4, "minPips": 5 }
    },
    "TakeProfit": {
      "Type": "Multiplier",
      "Parameters": { "multiplier": 1.5 }
    },
    "PositionSizing": {
      "Type": "RiskPercent",
      "Parameters": { "riskPct": 1.0, "pipValuePerLot": 10 }
    }
  }
}
```

### Composable Risk Management Pattern

**Strategy Composition:**
```
ISignalStrategy (entry logic)
  ↓ uses
IStopLossStrategy (SL calculation)
  ↓
ITakeProfitStrategy (TP calculation, may depend on SL)
  ↓
IPositionSizingStrategy (lot sizing based on SL)
  ↓
OrderContext (complete order DTO)
```

**Evaluation Flow:**
```csharp
public class FlexibleMeanReversionStrategy : ISignalStrategy
{
    private readonly IStopLossStrategy _sl;
    private readonly ITakeProfitStrategy _tp;
    private readonly IPositionSizingStrategy _ps;

    public OrderContext Evaluate(OrderContext seed)
    {
        // 1. Check session time window
        if (!InSessionWindow(seed.MarketData)) return seed;

        // 2. Calculate indicators
        var closes = seed.MarketData["M5"].Select(b => b.Close).ToArray();
        double rsi = IndicatorUtils.RSI(closes, _rsiPeriod);
        var (lower, upper) = IndicatorUtils.BollingerBands(closes, _bbPeriod, _bbStdDev);

        // 3. Generate signal
        double currentPrice = closes.Last();
        if (_useRSI && rsi < _oversoldLevel && _useBollinger && currentPrice < lower)
            seed.Side = TradeSignal.Buy;
        else if (_useRSI && rsi > _overboughtLevel && _useBollinger && currentPrice > upper)
            seed.Side = TradeSignal.Sell;
        else
            return seed; // No signal

        // 4. Calculate risk parameters via composed strategies
        seed.StopLossPips = _sl.CalculateStopLossPips(seed);
        seed.TakeProfitPips = _tp.CalculateTakeProfitPips(seed, seed.StopLossPips);
        seed.VolumeLots = _ps.CalculateVolume(seed, seed.StopLossPips);

        return seed;
    }
}
```

### Multi-Timeframe Data Management

**In-Memory Cache Structure:**
```csharp
// Per-symbol, per-timeframe rolling window
private Dictionary<long, Dictionary<string, List<Bar>>> _barCache;
// symbolId -> { "M5" -> [Bar1, Bar2, ...], "H1" -> [...] }

// Per-symbol semaphores to prevent concurrent cache corruption
private Dictionary<long, SemaphoreSlim> _symbolGates;
```

**Cache Initialization:**
```csharp
private async Task InitializeCacheAsync(long currentOpenTs)
{
    foreach (var symbol in _allSymbols)
    {
        _barCache[symbol.Id] = new();
        _symbolGates[symbol.Id] = new SemaphoreSlim(1, 1);

        foreach (var tf in _requiredTimeframes)
        {
            var bars = await _marketData.GetHistoryAsync(
                symbol.Id, tf,
                fromTs: currentOpenTs - (maxBars * tfSeconds),
                toTs: currentOpenTs,
                count: maxBars
            );
            _barCache[symbol.Id][tf] = bars;
        }
    }
}
```

**Incremental Update on Bar Close:**
```csharp
private async Task OnBarClosedAsync(long symbolId, string timeframe, Bar newBar)
{
    var gate = _symbolGates[symbolId];
    await gate.WaitAsync();
    try
    {
        var series = _barCache[symbolId][timeframe];

        // Prevent duplicates (timestamp indexed by CLOSE time)
        if (newBar.Timestamp > series.Last().Timestamp)
        {
            series.Add(newBar);
            if (series.Count > _maxBarsPerTimeframe)
                series.RemoveAt(0); // FIFO eviction
        }
    }
    finally
    {
        gate.Release();
    }

    // If this is the strategy's main timeframe, evaluate strategy
    if (timeframe == strategy.MainTimeframe)
        await EvaluateStrategyAsync(symbolId, timeframe);
}
```

**Strategy Consumption:**
```csharp
private async Task EvaluateStrategyAsync(long symbolId, string mainTf)
{
    var marketData = new Dictionary<string, List<Bar>>();

    // Provide snapshot of all required timeframes
    foreach (var (tf, barCount) in strategy.RequiredTimeframes)
    {
        marketData[tf] = _barCache[symbolId][tf]
            .TakeLast(barCount)
            .ToList();
    }

    var context = new OrderContext
    {
        SymbolId = symbolId,
        Symbol = symbolName,
        MarketData = marketData,
        AccountBalance = await GetAccountBalanceAsync(),
        EntryTsSeconds = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds()
    };

    // Strategy evaluates and populates Side/SL/TP/Volume
    var result = strategy.Evaluate(context);

    if (result.Side != TradeSignal.None)
        await SendToTradeGateAsync(result);
}
```

---

## V1 Backtest vs Live Execution

### Live Trading Flow

**StrategyEngineService.cs:**
1. **Event-driven**: Listens to SignalR `BarClosed` events
2. **In-memory cache**: Rolling window of recent bars per (symbol, timeframe)
3. **Real-time execution**: Strategies evaluated immediately when main timeframe bar closes
4. **Async order placement**: HTTP POST to Orders API → CTrader
5. **TradeGate validation**: Position conflicts, cooldowns, risk checks
6. **Account balance**: Fetched from Supabase `account_info` table

**Live Execution Pipeline:**
```
SignalR BarClosed Event
  → Filter by MainTimeframe
  → Update Cache (per-symbol lock)
  → Fetch Multi-TF Snapshot
  → Evaluate Strategy
  → Calculate SL (IStopLossStrategy)
  → Calculate TP (ITakeProfitStrategy)
  → Calculate Volume (IPositionSizingStrategy)
  → HTTP POST to TradeGate /api/analyze
  → If approved: HTTP POST to Orders /api/Orders
  → CTrader Places Order
  → Persist Trade to Supabase
```

### Backtest Flow

**BacktestEngine.cs:**
1. **Batch data load**: All historical bars from PostgreSQL via Npgsql
2. **Global timeline**: Sorted by timestamp across all (symbol, timeframe) pairs
3. **Simulated fills**: T+1 execution at next bar's OPEN price
4. **Cost modeling**: Spread + slippage + commission per symbol
5. **In-memory position tracking**: No external API calls
6. **Fast-forward simulation**: No sleep/delays, pure computation

**Backtest Execution Pipeline:**
```
Load All Bars from PostgreSQL (date range + symbols)
  → Build Global Timeline (sorted by timestamp)
  → For each timestamp:
      → Aggregate bars into timeframes (M5 → M15 → H1 → H4 → D1)
      → For each strategy:
          → Check if main timeframe bar closed
          → Evaluate strategy with multi-TF data
          → If signal: Create pending order
      → Process pending orders (fill at next bar OPEN + costs)
      → Update in-memory positions & portfolio
      → Emit trade events to result collector
  → Calculate metrics (PnL, Sharpe, Drawdown, Win Rate)
  → Return BacktestResult
```

**Key Differences:**

| Aspect | Live | Backtest |
|--------|------|----------|
| Data Source | SignalR stream | PostgreSQL batch load |
| Execution | Real-time async | Simulated sync loop |
| Fill Price | Market (next tick) | Next bar OPEN + spread/slippage/commission |
| Account State | Supabase query | In-memory balance tracker |
| Trade Validation | TradeGate HTTP call | Skipped (or in-memory rules) |
| Position Tracking | CTrader API | In-memory dictionary |
| Speed | Real-time (seconds) | Fast-forward (minutes for years) |

---

## V1 Genetic Optimizer

**GeneticSearch.cs Implementation:**

```csharp
public async Task<ReplayReport> OptimizeWindowAsync(
    string jobName,
    IEnumerable<string> symbols,
    DateTime startUtc,
    DateTime endUtc,
    CancellationToken ct)
{
    var rng = new Random(777);
    int populationSize = _settings.GeneticPopulation;      // e.g., 50
    int generations = _settings.GeneticGenerations;        // e.g., 20
    double mutationProb = _settings.GeneticMutationProb;   // e.g., 0.15

    // 1. Initialize population with random parameters
    var population = Enumerable.Range(0, populationSize)
        .Select(_ => _paramSpace.Sample(rng))
        .ToList();

    // 2. Evolution loop
    for (int gen = 0; gen < generations; gen++)
    {
        // Evaluate all candidates in parallel
        var evaluations = await Task.WhenAll(
            population.Select(paramSet =>
                _backtestRunner.RunAsync(jobName, _strategyTemplate, paramSet, symbols, startUtc, endUtc, ct)
            )
        );

        // 3. Selection: Keep top 50% that pass guardrails
        var survivors = evaluations
            .Where(r => PassesGuardrails(r, _settings))
            .OrderByDescending(r => FitnessScore(r))
            .Take(populationSize / 2)
            .ToList();

        if (survivors.Count == 0)
        {
            _logger.LogWarning("Generation {Gen}: No survivors passed guardrails", gen);
            break;
        }

        // 4. Crossover + Mutation
        var nextGeneration = new List<Dictionary<string, object>>();
        nextGeneration.AddRange(survivors.Select(r => r.Parameters)); // Elitism

        while (nextGeneration.Count < populationSize)
        {
            var parentA = survivors[rng.Next(survivors.Count)];
            var parentB = survivors[rng.Next(survivors.Count)];
            var child = Crossover(parentA.Parameters, parentB.Parameters, rng);

            if (rng.NextDouble() < mutationProb)
                child = _paramSpace.Mutate(child, rng);

            nextGeneration.Add(child);
        }

        population = nextGeneration;
    }

    // Return best candidate
    var finalEvals = await Task.WhenAll(
        population.Select(p => _backtestRunner.RunAsync(jobName, _strategyTemplate, p, symbols, startUtc, endUtc, ct))
    );

    return finalEvals
        .Where(r => PassesGuardrails(r, _settings))
        .OrderByDescending(r => FitnessScore(r))
        .FirstOrDefault();
}

// Fitness function (maximize CAGR/DD ratio)
private static double FitnessScore(ReplayReport report)
{
    double days = (report.EndDate - report.StartDate).TotalDays;
    double cagr = Math.Pow(report.FinalBalance / report.InitialBalance, 365.0 / days) - 1.0;
    double profitFactor = report.ProfitFactor;
    double ddPct = report.MaxDrawdownPct / 100.0;
    int trades = report.TotalTrades;

    double tradeBonusweight = (trades >= 100) ? 1.0 : 0.5;

    return (1000 * cagr) + (20 * profitFactor) - (200 * ddPct) + (tradeBonusweight * trades);
}

private bool PassesGuardrails(ReplayReport r, OptimizationSettings s)
{
    return r.TotalTrades >= s.MinTrades
        && r.ProfitFactor >= s.MinProfitFactor
        && r.MaxDrawdownPct <= s.MaxDrawdownPct;
}
```

**Parameter Space Definition:**
```csharp
public class ParameterSpace
{
    private Dictionary<string, ParamDefinition> _params;

    public Dictionary<string, object> Sample(Random rng)
    {
        var result = new Dictionary<string, object>();
        foreach (var (name, def) in _params)
        {
            result[name] = def.Type switch
            {
                "int" => rng.Next(def.Min, def.Max + 1),
                "double" => def.Min + rng.NextDouble() * (def.Max - def.Min),
                "bool" => rng.Next(2) == 0,
                _ => def.Default
            };
        }
        return result;
    }

    public Dictionary<string, object> Mutate(Dictionary<string, object> original, Random rng)
    {
        var mutated = new Dictionary<string, object>(original);
        var keyToMutate = mutated.Keys.ElementAt(rng.Next(mutated.Count));
        var def = _params[keyToMutate];

        mutated[keyToMutate] = def.Type switch
        {
            "int" => Clamp(((int)original[keyToMutate]) + rng.Next(-2, 3), def.Min, def.Max),
            "double" => Clamp(((double)original[keyToMutate]) + (rng.NextDouble() - 0.5) * 0.2, def.Min, def.Max),
            "bool" => !(bool)original[keyToMutate],
            _ => original[keyToMutate]
        };

        return mutated;
    }
}
```

**Optimization Settings (appsettings.Optimizer.json):**
```json
{
  "Optimization": {
    "GeneticPopulation": 50,
    "GeneticGenerations": 20,
    "GeneticMutationProb": 0.15,
    "MinTrades": 50,
    "MinProfitFactor": 1.2,
    "MaxDrawdownPct": 30.0,
    "InitialBalance": 10000
  }
}
```

---

## V1 Configuration Management

### Multi-Layer Configuration Hierarchy

**1. appsettings.json (Per Service)**
```json
{
  "Supabase": {
    "Url": "https://xxx.supabase.co",
    "ApiKey": "..."
  },
  "MarketData": {
    "BaseUrl": "http://marketdata_live:8080"
  },
  "OrdersApi": {
    "BaseUrl": "http://orders:8080"
  },
  "TradeGate": {
    "BaseUrl": "http://analyzer_gateway:8080"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

**2. Environment Variables (Docker Compose)**
```yaml
environment:
  SUPABASE_URL: "https://mwbchdjcputissawsktk.supabase.co"
  SUPABASE_KEY: "eyJhbGci..."
  LOG_PATH: "/logs/strategyengine-.log"
  ASPNETCORE_ENVIRONMENT: Development
```

**3. Supabase Tables (Dynamic, Hot-Reload)**
- `strategy_configs` - Strategy definitions + parameters
- `symbols` - Symbol metadata (pip_position, digits, scale)
- `account_info` - Live account balance
- `candles` - Historical OHLCV data
- `orders` - Trade audit log

**Configuration Loading Pattern:**
```csharp
// Startup.cs / Program.cs
services.Configure<SupabaseConfig>(cfg.GetSection("Supabase"));
services.Configure<OrdersApiConfig>(cfg.GetSection("OrdersApi"));

// Load strategies from Supabase at runtime
var strategies = await _strategyProvider.LoadAsync(ct);

// Merge with local fallback
if (strategies.Count == 0)
    strategies = LoadFromAppSettingsJson();

// Instantiate via factory
foreach (var (def, symbols) in strategies)
{
    if (_strategyFactory.TryCreate(def, out var strategy))
        _strategies.Add((strategy, symbols));
}
```

---

## V1 Key Abstractions & Interfaces

### Core Strategy Interfaces

```csharp
// Signal generation (entry logic)
public interface ISignalStrategy
{
    string Name { get; }
    string MainTimeframe { get; }
    int Version { get; }
    TimeSpan Interval { get; }  // For scheduled strategies (TimeSpan.Zero = event-driven)
    IDictionary<string, int> RequiredTimeframes { get; }  // {"M5": 300, "H1": 100}

    OrderContext Evaluate(OrderContext seed);
}

// Stop loss calculation
public interface IStopLossStrategy
{
    double CalculateStopLossPips(OrderContext context);
}

// Take profit calculation
public interface ITakeProfitStrategy
{
    double CalculateTakeProfitPips(OrderContext context, double stopLossPips);
}

// Position sizing
public interface IPositionSizingStrategy
{
    double CalculateVolume(OrderContext context, double stopLossPips);
}
```

### OrderContext DTO

```csharp
public class OrderContext
{
    // Input (from Strategy Engine)
    public long SymbolId { get; set; }
    public string Symbol { get; set; }
    public Dictionary<string, List<Bar>> MarketData { get; set; }
    public double AccountBalance { get; set; }
    public double? CurrentSpreadPips { get; set; }
    public int EntryTsSeconds { get; set; }

    // Output (from Strategy)
    public TradeSignal Side { get; set; }  // Buy/Sell/None
    public double VolumeLots { get; set; }
    public double StopLossPips { get; set; }
    public double TakeProfitPips { get; set; }
    public bool TrailingStop { get; set; }

    // Metadata
    public string StrategyName { get; set; }
    public int StrategyVersion { get; set; }
    public string MainTimeframe { get; set; }
    public string ClientOrderId { get; set; }
}
```

### Service Abstractions

```csharp
public interface IMarketDataService
{
    Task<List<Bar>> GetHistoryAsync(
        long symbolId, string period,
        long fromTs, long toTs, uint count,
        CancellationToken ct);
}

public interface IOrderService
{
    Task<OrderRequest> PlaceOrderAsync(OrderContext context, bool useInProcessBars);
}

public interface ITradeGateService
{
    Task<DecisionResponse> AnalyzeAsync(SignalDto signal, CancellationToken ct);
}
```

### Factory Pattern

```csharp
public interface IStrategyFactory
{
    bool TryCreate(StrategyDefinition def, out ISignalStrategy? strategy);
}

public interface IStopLossFactory
{
    IStopLossStrategy? Create(StopBlock? cfg);
}

public interface ITakeProfitFactory
{
    ITakeProfitStrategy? Create(TakeProfitBlock? cfg);
}

public interface IPositionSizingFactory
{
    IPositionSizingStrategy? Create(PositionSizingBlock? cfg);
}
```

---

## V1 Architectural Patterns

1. **Microservices** - Decoupled services communicating via HTTP + SignalR
2. **Event-Driven** - Bar close events trigger strategy evaluation (pub/sub via SignalR)
3. **Strategy Pattern** - Pluggable signal/SL/TP/sizing implementations via interfaces
4. **Factory Pattern** - Dynamic instantiation based on configuration strings
5. **Dependency Injection** - Constructor injection for services and strategies
6. **Repository Pattern** - Supabase client abstracts database operations
7. **Chain of Responsibility** - TradeGate rules evaluated sequentially
8. **Template Method** - Base strategy classes define evaluation skeleton
9. **Builder Pattern** - OrderContext progressively enriched through pipeline
10. **Attribute-based Discovery** - Reflection scans for `[Strategy]`, `[StopLoss]`, etc.

---

## V1 Pain Points & Limitations

These are the issues in v1 that Alphiq 2.0 must address:

1. **No unified solution** - Each microservice has separate `.sln` file
2. **Live/Backtest code divergence** - Different execution paths can cause strategy drift
3. **Tight coupling to Supabase** - Hard to swap data layer or test in isolation
4. **No unit tests** - Zero test projects found in codebase
5. **Manual Docker orchestration** - docker-compose only, no Kubernetes support
6. **No distributed backtesting** - Single-node optimizer, can't scale horizontally
7. **Configuration scattered** - appsettings + env vars + Supabase tables (hard to audit)
8. **No API gateway** - Services expose ports directly (security/routing concerns)
9. **No observability** - Limited structured logging, no distributed tracing
10. **SignalR complexity** - Managing connections/reconnections/subscriptions is error-prone

---

## V1 Development Commands (Reference Only)

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
npm run dev        # Vite dev server
npm run build      # Production build
```

### Running Locally
```bash
# Run individual service
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
cd alphiq
docker-compose up --build           # Build and start all
docker-compose up marketdata_live   # Start specific service
docker-compose logs -f strategy_engine
docker-compose down
```

---

# PART 2: ALPHIQ V2 ARCHITECTURE

This section defines the **new Alphiq 2.0 architecture**. Claude Code MUST follow these specifications when generating or modifying code in `alphiq2.0/`.

---

# 🚫 Hard Rules (Claude MUST Follow)

These are critical architectural constraints that Claude Code **MUST adhere to**.

---

## 1. Single Trading Engine (Critical)

There must be exactly one library:

**`Alphiq.TradingEngine`**

This library contains **all trading logic**:

- Strategy evaluation
- Risk management
- Portfolio accounting
- Order decision logic
- Multi-timeframe bar aggregation
- Event emission
- Runtime orchestration

All execution modes use this engine:

- Live trading
- Backtesting
- Replay
- Paper trading

These modes differ only by supplying different adapters:

- `IMarketDataFeed`
- `IOrderExecution`
- `IClock`
- `IEngineEventSink`

❗ **Claude must never duplicate trading logic anywhere else.**

---

## 2. Supabase-only Persistence

- No EF Core
- No custom ORMs
- All data access goes through Supabase/PostgREST

A repository layer exists under:

**`Alphiq.Infrastructure.Supabase`**

Repositories map DB DTOs ↔ domain models explicitly.

- Short-term: Supabase/PostgreSQL
- Long-term: Repositories must be interface-driven so other backends can be added later if explicitly requested.

---

## 3. Messaging = NATS (JetStream)

Use **NATS + JetStream** for:

- Event bus
- Job queues
- Live engine event publishing
- Backtest job distribution
- Optimization job distribution

Default topics:

- `alphiq.trade.events`
- `alphiq.backtest.jobs`
- `alphiq.backtest.results`
- `alphiq.optimizer.jobs`
- `alphiq.optimizer.results`

❗ Do **NOT** introduce RabbitMQ or Kafka unless explicitly ordered.

---

## 4. Orchestration = .NET Aspire

Use a **.NET Aspire AppHost** as the central definition of all:

- Alphiq2.0 services
- Infrastructure resources
- Environment wiring
- Observability
- Local dev orchestration
- Docker / K8s manifest generation

Claude must NOT scatter Docker/K8s configs randomly.
Aspire is the source of truth.

---

## 5. Respect v1 Directory Boundary

- `alphiq/` (v1) is reference-only
- Claude must NOT modify any file under `alphiq/` unless explicitly instructed
- All new code belongs in `alphiq2.0/`

---

## 6. Unified Solution

Alphiq 2.0 uses a **single `.sln` file** at the root of `alphiq2.0/`.

This solution contains:

- Core libraries
- Infrastructure libraries
- Services (TradeNode, BacktestWorker, ReplayWorker, Optimizer)
- API Gateway
- UI
- Tests
- AppHost (Aspire)

---

## 7. Testability from Day 1

Claude should generate tests using:

- xUnit
- FluentAssertions
- Testcontainers (optional for integration tests)

TradingEngine must be fully testable in isolation using mocked adapters.

---

# 🧠 Alphiq v2 Architecture Summary

Claude Code must use the architecture below when generating or modifying code.

---

# 1. Core Layer (Pure Logic)

### **Alphiq.Domain**

- Entities: `Instrument`, `Bar`, `Tick`, `Order`, `Trade`, `Position`, `Portfolio`, `StrategyInstance`
- Value Objects: `Money`, `Timeframe`, `SymbolId`, `Quantity`, `AccountId`
- Domain Services: signal helpers, risk helpers, cost models
- **Zero dependencies**

### **Alphiq.Brokers.Abstractions**

Interfaces for adapters:

- `IMarketDataFeed`
- `IOrderExecution`
- `IBrokerConnection`
- `IClock`
- `IEngineEventSink`

### **Alphiq.Configuration.Abstractions**

- Strategy configuration access
- Parameter sets & versions
- Tenant context
- Metadata access

### **Alphiq.TradingEngine**

**The single unified trading engine** used by all modes.

Contains:

- Strategy orchestration
- Multi-timeframe aggregation
- Risk & sizing
- Portfolio state machine
- Order generation lifecycle
- Event pipeline
- Clock-driven engine loop

Does *not* touch Supabase, cTrader, NATS, or UI.

---

# 2. Infrastructure Layer

### **Alphiq.Infrastructure.Supabase**

Supabase/PostgREST integration.

Repositories for:

- Strategy configurations
- Strategy versions
- Historical bars
- Orders
- Trades
- Portfolios
- Backtest results
- Optimization results

No EF Core.
DTO ↔ Domain mapping is explicit.

### **Alphiq.Infrastructure.BrokerAuth**

- Broker credential storage (Supabase, encrypted)
- cTrader OAuth refresh cycles
- Credential management per tenant

### **Alphiq.Infrastructure.Broker.CTrader**

Implements:

- `CTraderMarketDataFeed`
- `CTraderOrderExecution`
- `CTraderBrokerConnection`

Used in **live trading**.

### **Alphiq.Infrastructure.Broker.Simulated**

Implements:

- `BacktestMarketDataFeed`
- `SimulatedOrderExecution`
- `BacktestClock`

Used in **backtesting**, **replay**, and **simulation**.

### **Alphiq.Infrastructure.Messaging**

NATS + JetStream:

- Typed publishers/subscribers
- Event and job streams
- Durable consumers
- Streams for trade events, job queues, logs

### **Alphiq.Infrastructure.Caching**

- Redis metadata caches
- Distributed locks
- Strategy snapshots

### **Alphiq.Infrastructure.Observability**

- OpenTelemetry tracing
- Metrics
- Structured logging

---

# 3. Services Layer (Executables)

### **Alphiq.TradeNode**

Live trading host.

- Wires TradingEngine with:
  - `CTraderMarketDataFeed`
  - `CTraderOrderExecution`
  - `SystemClock`
  - `NatsEventSink`
- Publishes all trading events to NATS
- Writes state to Supabase
- Provides gRPC control (start/stop strategy, query engine state)

### **Alphiq.Backtest.Worker**

Distributed backtesting worker.

Flow:

1. Receive `BacktestJob` via NATS
2. Load bars from Supabase
3. Use simulated adapters
4. Run TradingEngine
5. Publish results to NATS + Supabase

### **Alphiq.Replay.Worker**

Replay timed market events for UI or debugging.

### **Alphiq.Optimizer.Engine**

Pure logic:

- Parameter search
- Random/grid/genetic search
- Walk-forward and robustness filters

### **Alphiq.Optimizer.Service**

- Receives optimization requests
- Splits into many `BacktestJob`s
- Aggregates results
- Selects champion configs
- Stores final configurations to Supabase

---

# 4. API & UI

### **Alphiq.Api.Gateway**

ASP.NET Core Minimal APIs + gRPC + SignalR.

Responsibilities:

- Authentication & multi-tenancy
- Manage strategy configs & versions
- Trigger backtests and optimizations
- Query trade/order/portfolio data
- Stream live engine events via SignalR (via NATS subscriptions)

### **Alphiq.Ui.Web**

Blazor or React front-end.

Provides:

- Live dashboards
- Multi-timeframe charts
- Strategy configuration
- Backtest viewers
- Optimization viewers
- Broker connection flows

---

# 5. Orchestration (Aspire)

### **Alphiq.AppHost**

Uses .NET Aspire to define:

**Services:**

- `TradeNode`
- `BacktestWorker`
- `ReplayWorker`
- `Optimizer`
- `ApiGateway`
- `UiWeb`

**Resources:**

- Supabase/Postgres
- Redis
- NATS
- OpenTelemetry collector

Aspire generates Docker Compose / K8s manifests.

---

# 📁 Recommended Directory Layout

```
alphiq2.0/
├── src/
│   ├── Core/
│   │   ├── Alphiq.Domain/
│   │   ├── Alphiq.Brokers.Abstractions/
│   │   ├── Alphiq.Configuration.Abstractions/
│   │   └── Alphiq.TradingEngine/
│   ├── Infrastructure/
│   │   ├── Alphiq.Infrastructure.Supabase/
│   │   ├── Alphiq.Infrastructure.Broker.CTrader/
│   │   ├── Alphiq.Infrastructure.Broker.Simulated/
│   │   ├── Alphiq.Infrastructure.Messaging/
│   │   ├── Alphiq.Infrastructure.Caching/
│   │   └── Alphiq.Infrastructure.Observability/
│   ├── Services/
│   │   ├── Alphiq.TradeNode/
│   │   ├── Alphiq.Backtest.Worker/
│   │   ├── Alphiq.Replay.Worker/
│   │   └── Alphiq.Optimizer.Service/
│   ├── WebApi/
│   │   └── Alphiq.Api.Gateway/
│   └── Frontend/
│       └── Alphiq.Ui.Web/
├── tests/
│   ├── Unit/
│   ├── Integration/
│   └── Backtest/
├── apphost/
│   └── Alphiq.AppHost/
├── deploy/
│   ├── docker/
│   └── k8s/
└── Alphiq.sln
```

---

# 🧭 How Claude Should Work in This Repo

When performing refactors, creating files, or generating code:

- Work only inside `alphiq2.0/`
- Follow all hard rules
- Use the architecture described above
- Ask before adding new dependencies

Default choices:

- Supabase/Postgres
- NATS + JetStream
- .NET 8/9
- Aspire
- Unified TradingEngine
- Interface-driven design

---

# 🏁 Initial Task Plan for Claude

Claude may follow these steps when starting major refactor tasks:

1. Create the entire folder + project structure
2. Add the solution file and include all projects
3. Scaffold all empty projects (correct namespaces + dependencies)
4. Define the broker abstractions
5. Create the TradingEngine skeleton
6. Implement repository interfaces in `Alphiq.Infrastructure.Supabase`
7. Implement the CTrader and Simulated broker adapter skeletons
8. Implement the TradeNode `Program.cs` + DI wiring
9. Implement BacktestWorker + basic job handling via NATS
10. Implement a minimal ApiGateway with sample endpoints
11. Implement Aspire AppHost with resource definitions
12. Create basic xUnit test projects

---

# ✔️ End of CLAUDE.md
