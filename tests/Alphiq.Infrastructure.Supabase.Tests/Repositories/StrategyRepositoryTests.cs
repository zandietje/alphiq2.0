using System.Net;
using System.Text;
using Alphiq.Domain.ValueObjects;
using Alphiq.Infrastructure.Supabase.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Alphiq.Infrastructure.Supabase.Tests.Repositories;

public class StrategyRepositoryTests
{
    [Fact]
    public async Task GetAllEnabledAsync_ReturnsLatestVersionPerStrategy()
    {
        // Arrange
        var responseJson = """
        [
            {"id":1,"name":"MR_M5","version":3,"enabled":true,"main_timeframe":"M5","config":{"Timeframes":{"M5":300},"Parameters":{},"Risk":{"StopLoss":{"Type":"ATR","Parameters":{}},"TakeProfit":{"Type":"Multiplier","Parameters":{}},"PositionSizing":{"Type":"RiskPercent","Parameters":{}}}},"symbol_list":["1","2"],"created_at":"2024-01-01T00:00:00Z"},
            {"id":2,"name":"MR_M5","version":2,"enabled":true,"main_timeframe":"M5","config":{"Timeframes":{"M5":300},"Parameters":{},"Risk":{"StopLoss":{"Type":"ATR","Parameters":{}},"TakeProfit":{"Type":"Multiplier","Parameters":{}},"PositionSizing":{"Type":"RiskPercent","Parameters":{}}}},"symbol_list":["1"],"created_at":"2024-01-01T00:00:00Z"}
        ]
        """;

        var handler = new MockHttpMessageHandler(responseJson, HttpStatusCode.OK);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://test.supabase.co/") };
        var sut = new StrategyRepository(httpClient, NullLogger<StrategyRepository>.Instance);

        // Act
        var result = await sut.GetAllEnabledAsync();

        // Assert
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("MR_M5");
        result[0].Version.Should().Be(3); // Latest version
        result[0].Symbols.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllEnabledAsync_MultipleStrategies_ReturnsLatestVersionEach()
    {
        // Arrange
        var responseJson = """
        [
            {"id":1,"name":"MR_M5","version":2,"enabled":true,"main_timeframe":"M5","config":{"Timeframes":{"M5":300},"Parameters":{},"Risk":{"StopLoss":{"Type":"ATR","Parameters":{}},"TakeProfit":{"Type":"Multiplier","Parameters":{}},"PositionSizing":{"Type":"RiskPercent","Parameters":{}}}},"symbol_list":["1"],"created_at":"2024-01-01T00:00:00Z"},
            {"id":2,"name":"MR_M5","version":1,"enabled":true,"main_timeframe":"M5","config":{"Timeframes":{"M5":300},"Parameters":{},"Risk":{"StopLoss":{"Type":"ATR","Parameters":{}},"TakeProfit":{"Type":"Multiplier","Parameters":{}},"PositionSizing":{"Type":"RiskPercent","Parameters":{}}}},"symbol_list":["1"],"created_at":"2024-01-01T00:00:00Z"},
            {"id":3,"name":"BB_H1","version":3,"enabled":true,"main_timeframe":"H1","config":{"Timeframes":{"H1":100},"Parameters":{},"Risk":{"StopLoss":{"Type":"ATR","Parameters":{}},"TakeProfit":{"Type":"Multiplier","Parameters":{}},"PositionSizing":{"Type":"RiskPercent","Parameters":{}}}},"symbol_list":["1","2","3"],"created_at":"2024-01-01T00:00:00Z"},
            {"id":4,"name":"BB_H1","version":1,"enabled":true,"main_timeframe":"H1","config":{"Timeframes":{"H1":100},"Parameters":{},"Risk":{"StopLoss":{"Type":"ATR","Parameters":{}},"TakeProfit":{"Type":"Multiplier","Parameters":{}},"PositionSizing":{"Type":"RiskPercent","Parameters":{}}}},"symbol_list":["1"],"created_at":"2024-01-01T00:00:00Z"}
        ]
        """;

        var handler = new MockHttpMessageHandler(responseJson, HttpStatusCode.OK);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://test.supabase.co/") };
        var sut = new StrategyRepository(httpClient, NullLogger<StrategyRepository>.Instance);

        // Act
        var result = await sut.GetAllEnabledAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(s => s.Name == "MR_M5" && s.Version == 2);
        result.Should().Contain(s => s.Name == "BB_H1" && s.Version == 3);
    }

    [Fact]
    public async Task GetAllEnabledAsync_ParsesTimeframeCorrectly()
    {
        // Arrange
        var responseJson = """[{"id":1,"name":"Test","version":1,"enabled":true,"main_timeframe":"M5","config":{"Timeframes":{"M5":300,"H1":100},"Parameters":{},"Risk":{"StopLoss":{"Type":"ATR","Parameters":{}},"TakeProfit":{"Type":"Multiplier","Parameters":{}},"PositionSizing":{"Type":"RiskPercent","Parameters":{}}}},"symbol_list":["1"],"created_at":"2024-01-01T00:00:00Z"}]""";

        var handler = new MockHttpMessageHandler(responseJson, HttpStatusCode.OK);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://test.supabase.co/") };
        var sut = new StrategyRepository(httpClient, NullLogger<StrategyRepository>.Instance);

        // Act
        var result = await sut.GetAllEnabledAsync();

        // Assert
        result[0].MainTimeframe.Should().Be(Timeframe.M5);
        result[0].RequiredTimeframes.Should().ContainKey(Timeframe.M5);
        result[0].RequiredTimeframes.Should().ContainKey(Timeframe.H1);
        result[0].RequiredTimeframes[Timeframe.M5].Should().Be(300);
        result[0].RequiredTimeframes[Timeframe.H1].Should().Be(100);
    }

    [Fact]
    public async Task GetAllEnabledAsync_ParsesSymbolIdsCorrectly()
    {
        // Arrange
        var responseJson = """[{"id":1,"name":"Test","version":1,"enabled":true,"main_timeframe":"M5","config":{"Timeframes":{"M5":300},"Parameters":{},"Risk":{"StopLoss":{"Type":"ATR","Parameters":{}},"TakeProfit":{"Type":"Multiplier","Parameters":{}},"PositionSizing":{"Type":"RiskPercent","Parameters":{}}}},"symbol_list":["123","456","789"],"created_at":"2024-01-01T00:00:00Z"}]""";

        var handler = new MockHttpMessageHandler(responseJson, HttpStatusCode.OK);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://test.supabase.co/") };
        var sut = new StrategyRepository(httpClient, NullLogger<StrategyRepository>.Instance);

        // Act
        var result = await sut.GetAllEnabledAsync();

        // Assert
        result[0].Symbols.Should().HaveCount(3);
        result[0].Symbols[0].Value.Should().Be(123);
        result[0].Symbols[1].Value.Should().Be(456);
        result[0].Symbols[2].Value.Should().Be(789);
    }

    [Fact]
    public async Task GetAllEnabledAsync_ParsesRiskConfigCorrectly()
    {
        // Arrange
        var responseJson = """[{"id":1,"name":"Test","version":1,"enabled":true,"main_timeframe":"M5","config":{"Timeframes":{"M5":300},"Parameters":{},"Risk":{"StopLoss":{"Type":"ATR","Parameters":{"period":14,"multiplier":1.4}},"TakeProfit":{"Type":"Multiplier","Parameters":{"multiplier":1.5}},"PositionSizing":{"Type":"RiskPercent","Parameters":{"riskPct":1.0}}}},"symbol_list":["1"],"created_at":"2024-01-01T00:00:00Z"}]""";

        var handler = new MockHttpMessageHandler(responseJson, HttpStatusCode.OK);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://test.supabase.co/") };
        var sut = new StrategyRepository(httpClient, NullLogger<StrategyRepository>.Instance);

        // Act
        var result = await sut.GetAllEnabledAsync();

        // Assert
        result[0].Risk.StopLoss.Type.Should().Be("ATR");
        result[0].Risk.StopLoss.Parameters.Should().ContainKey("period");
        result[0].Risk.TakeProfit.Type.Should().Be("Multiplier");
        result[0].Risk.PositionSizing.Type.Should().Be("RiskPercent");
    }

    [Fact]
    public async Task GetAllEnabledAsync_ParsesParametersCorrectly()
    {
        // Arrange
        var responseJson = """[{"id":1,"name":"Test","version":1,"enabled":true,"main_timeframe":"M5","config":{"Timeframes":{"M5":300},"Parameters":{"UseBollinger":true,"BbPeriod":20,"BbStdDev":1.8},"Risk":{"StopLoss":{"Type":"ATR","Parameters":{}},"TakeProfit":{"Type":"Multiplier","Parameters":{}},"PositionSizing":{"Type":"RiskPercent","Parameters":{}}}},"symbol_list":["1"],"created_at":"2024-01-01T00:00:00Z"}]""";

        var handler = new MockHttpMessageHandler(responseJson, HttpStatusCode.OK);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://test.supabase.co/") };
        var sut = new StrategyRepository(httpClient, NullLogger<StrategyRepository>.Instance);

        // Act
        var result = await sut.GetAllEnabledAsync();

        // Assert
        result[0].Parameters.Should().ContainKey("UseBollinger");
        result[0].Parameters.Should().ContainKey("BbPeriod");
        result[0].Parameters.Should().ContainKey("BbStdDev");
    }

    [Fact]
    public async Task GetAllEnabledAsync_EmptyResponse_ReturnsEmptyList()
    {
        // Arrange
        var handler = new MockHttpMessageHandler("[]", HttpStatusCode.OK);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://test.supabase.co/") };
        var sut = new StrategyRepository(httpClient, NullLogger<StrategyRepository>.Instance);

        // Act
        var result = await sut.GetAllEnabledAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByNameAsync_ExistingStrategy_ReturnsStrategy()
    {
        // Arrange
        var responseJson = """[{"id":1,"name":"MR_M5","version":3,"enabled":true,"main_timeframe":"M5","config":{"Timeframes":{"M5":300},"Parameters":{},"Risk":{"StopLoss":{"Type":"ATR","Parameters":{}},"TakeProfit":{"Type":"Multiplier","Parameters":{}},"PositionSizing":{"Type":"RiskPercent","Parameters":{}}}},"symbol_list":["1"],"created_at":"2024-01-01T00:00:00Z"}]""";

        var handler = new MockHttpMessageHandler(responseJson, HttpStatusCode.OK);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://test.supabase.co/") };
        var sut = new StrategyRepository(httpClient, NullLogger<StrategyRepository>.Instance);

        // Act
        var result = await sut.GetByNameAsync("MR_M5");

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("MR_M5");
        result.Version.Should().Be(3);
    }

    [Fact]
    public async Task GetByNameAsync_NonExistingStrategy_ReturnsNull()
    {
        // Arrange
        var handler = new MockHttpMessageHandler("[]", HttpStatusCode.OK);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://test.supabase.co/") };
        var sut = new StrategyRepository(httpClient, NullLogger<StrategyRepository>.Instance);

        // Act
        var result = await sut.GetByNameAsync("NonExistent");

        // Assert
        result.Should().BeNull();
    }
}

/// <summary>
/// Mock HttpMessageHandler for testing HttpClient calls.
/// </summary>
public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly string _response;
    private readonly HttpStatusCode _statusCode;

    public MockHttpMessageHandler(string response, HttpStatusCode statusCode)
    {
        _response = response;
        _statusCode = statusCode;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(new HttpResponseMessage
        {
            StatusCode = _statusCode,
            Content = new StringContent(_response, Encoding.UTF8, "application/json")
        });
    }
}
