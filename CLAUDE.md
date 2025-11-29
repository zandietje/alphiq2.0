# CLAUDE.md

Guidance for Claude Code when working inside this repository.

## Core Development Philosophy

### KISS (Keep It Simple, Stupid)

Simplicity should be a key goal in design. Choose straightforward solutions over complex ones whenever possible. Simple solutions are easier to understand, maintain, and debug.

### YAGNI (You Aren't Gonna Need It)

Avoid building functionality on speculation. Implement features only when they are needed, not when you anticipate they might be useful in the future.

### Design Principles

- **Dependency Inversion**: High-level modules should not depend on low-level modules. Both should depend on abstractions.
- **Open/Closed Principle**: Software entities should be open for extension but closed for modification.
- **Single Responsibility**: Each function, class, and module should have one clear purpose.
- **Fail Fast**: Check for potential errors early and raise exceptions immediately when issues occur.

---

# 📌 Project Overview

This repository contains **Alphiq 2.0**, the next-generation architecture of the Alphiq algorithmic trading platform.

Alphiq 2.0 is designed to be:

- Faster
- Unified
- Extendable
- SaaS-ready
- Easier to deploy via .NET Aspire
- Built around a single, correct trading engine used in all modes

---

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
- `Alphiq.Infrastructure.Supabase` is allowed to use **Npgsql** for direct SQL access
  to the Supabase Postgres database (especially for bulk candle/trade queries).
- PostgREST / Supabase client can still be used where convenient (e.g. auth, metadata),
  but never directly from services – only via `Alphiq.Infrastructure.Supabase`.

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
- Services (TradeNode, BacktestWorker, Optimizer)
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
- StrategyDefinition
- Parameter sets & versions
- Tenant context
- Metadata access

### **Alphiq.TradingEngine**

**The single unified trading engine** used by all modes.

Contains:

- Strategy orchestration
- Strategies
- Multi-timeframe aggregation
- Risk & sizing
- Portfolio state machine
- Order generation lifecycle
- Event pipeline
- Clock-driven engine loop

Does *not* touch Supabase, cTrader, NATS, or UI.

### **Alphiq.Optimizer**

Pure logic for optimizing:

- Parameter search
- Random/grid/genetic search
- Walk-forward and robustness filters

### **Alphiq.Contracts**

All NATS messages use DTOs defined in Alphiq.Contracts.
Services must not invent their own incompatible message types.

examples:
    - BacktestJob
    - BacktestResult
    - OptimizerJob
    - OptimizerResult
    - TradeEvent
    - EngineStatusEvent

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

=> CTrader adapters must not implement OAuth themselves. They take a dependency on a generic IBrokerTokenProvider from Alphiq.Infrastructure.BrokerAuth and call it to obtain access tokens.

Used in **live trading**.

### **Alphiq.Infrastructure.Broker.Simulated**

Implements:

- `BacktestMarketDataFeed`
- `SimulatedOrderExecution`
- `BacktestClock`

Used in **backtesting** and **simulation**.

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

Distributed backtesting worker. IT replays he historical data into TradingEngine. The functionality is the same as the
legacy Alphiq.StrategyEngine.Replay

Flow:

1. Receive `BacktestJob` via NATS
2. Load bars from Supabase
3. Use simulated adapters
4. Run TradingEngine
5. Publish results to NATS + Supabase

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
│   │   ├── Alphiq.Optimizer/
│   │   ├── Alphiq.Contracts/
│   │   └── Alphiq.TradingEngine/
│   ├── Infrastructure/
│   │   ├── Alphiq.Infrastructure.Supabase/
│   │   ├── Alphiq.Infrastructure.BrokerAuth/
│   │   ├── Alphiq.Infrastructure.Broker.CTrader/
│   │   ├── Alphiq.Infrastructure.Broker.Simulated/
│   │   ├── Alphiq.Infrastructure.Messaging/
│   │   ├── Alphiq.Infrastructure.Caching/
│   │   └── Alphiq.Infrastructure.Observability/
│   ├── Services/
│   │   ├── Alphiq.TradeNode/
│   │   ├── Alphiq.Backtest.Worker/
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
- .NET 10
- Aspire
- Unified TradingEngine
- Interface-driven design

## ⚠️ Important Notes

- **NEVER ASSUME OR GUESS** - When in doubt, ask for clarification
- **Always verify file paths and module names** before use
- **Keep CLAUDE.md updated** when adding new patterns or dependencies
- **Test your code** - No feature is complete without tests, always create a buildable result
- **Document your decisions** - Future developers (including yourself) will thank you

---
# ✔️ End of CLAUDE.md
