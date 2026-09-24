using System.Text.Json;
using AwesomeAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Monica.Core.JsonSerialization.Models;
using Monica.Core.JsonSerialization.Services;
using Monica.Core.Results;
using Monica.Framework.ChainTracing.Providers.AspNetCore;
using Monica.Framework.ChainTracing.Services;
using Monica.Framework.ChainTracing.Services.Support;
using Monica.Framework.ChainTracing.Models;
using Monica.Modules;
using Xunit;

namespace Test.Monica.Framework.ChainTracing;

public sealed class ChainTracingResultMetadataFilterTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OnActionExecuted_ShouldAttachChainOnlyForDiagnosticHosts(bool exposeDiagnostics)
    {
        var tracing = CreateTracingWithDatabaseNode();
        var filter = new ChainTracingResultMetadataActionFilter(tracing,
            new ChainResultMetadataAttacher(Options.Create(
                new ModuleResultEnvelopeOption { ExposeDiagnosticDetails = exposeDiagnostics })));
        var response = Res.Fail("Safe failure");
        var context = ExecutedContext(response);

        filter.OnActionExecuted(context);

        var json = JsonSerializer.Serialize(response);
        json.Should().Contain("traceId");
        if (exposeDiagnostics)
        {
            json.Should().Contain("chain");
            json.Should().Contain("SELECT 1");
        }
        else
        {
            json.Should().NotContain("chain");
            json.Should().NotContain("SELECT 1");
        }
    }

    private static AsyncLocalChainTracingService CreateTracingWithDatabaseNode()
    {
        var tracing = new AsyncLocalChainTracingService(
            Options.Create(new ModuleChainTracingOption()),
            NullLogger<AsyncLocalChainTracingService>.Instance,
            new JsonSerializerOptionsProvider(new JsonSerializerOptions(), DateTimeWireFormat.Iso8601WallClock));
        tracing.BeginTrace("SELECT 1", null, null, EChainTracingType.Database);
        return tracing;
    }

    private static ActionExecutedContext ExecutedContext(Res response)
    {
        var httpContext = new DefaultHttpContext();
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        return new ActionExecutedContext(actionContext, [], new object())
        {
            Result = new ObjectResult(response)
        };
    }
}
