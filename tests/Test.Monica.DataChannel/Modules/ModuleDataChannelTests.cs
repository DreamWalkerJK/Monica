using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Monica.Core.JsonSerialization.Abstractions;
using Monica.Core.Results;
using Monica.Core.Results.Abstractions;
using Test.Monica.DataChannel.Support;
using Xunit;

namespace Test.Monica.DataChannel.Modules;

public sealed class ModuleDataChannelTests
{
    [Fact]
    public async Task AddDataChannel_WhenOnlySetupAndMinimalApiAreSelected_ShouldExecuteProductionResultProjection()
    {
        var scenario = new DataChannelScenario();
        await using var application = await new DataChannelTestApplicationFactory(scenario, enableMinimalApi: true)
            .CreateAsync(cancellationToken: TestContext.Current.CancellationToken);
        application.Services.GetRequiredService<IResultErrorMessageProvider>().Should().NotBeNull();
        using var client = ((TestServer)application.Services.GetRequiredService<IServer>()).CreateClient();

        using var response = await client.GetAsync("/channels", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var options = application.Services.GetRequiredService<IJsonSerializerOptionsProvider>().SerializerOptions;
        var envelope = JsonSerializer.Deserialize<Res<JsonElement>>(body, options)!;
        envelope.Status.Should().Be(ResStatus.Ok);
        envelope.Message.Should().BeNullOrEmpty();
        envelope.Data.ValueKind.Should().Be(JsonValueKind.Array);
        envelope.Data.GetArrayLength().Should().Be(1);
        var channel = envelope.Data[0];
        channel.GetProperty("id").GetString().Should().Be(scenario.Primary.Id);
        channel.GetProperty("groupId").GetString().Should().Be(scenario.Primary.GroupId);
        channel.GetProperty("isInitialized").GetBoolean().Should().BeTrue();
        channel.GetProperty("outerEndpoint").GetProperty("metadata").GetProperty("json").GetString().Should().NotBeNullOrEmpty();
    }
}
