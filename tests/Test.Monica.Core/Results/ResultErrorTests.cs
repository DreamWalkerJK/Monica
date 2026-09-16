using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Monica.Core.Results;
using Monica.Core.Results.Services;
using Xunit;

namespace Test.Monica.Core.Results;

public sealed class ResultErrorTests
{
    [Theory]
    [InlineData(ResStatus.Ok)]
    [InlineData(ResStatus.Created)]
    public void Success_WhenDataIsNull_ShouldRemainSuccessWithoutPromisingPayload(ResStatus status)
    {
        var result = new Res<string?>("", status);
        Assert.True(result.IsOk(out var data));
        Assert.Null(data);
        Assert.False(result.IsFailed(out _));
    }

    [Theory]
    [InlineData(ResStatus.ValidateError, 400)]
    [InlineData(ResStatus.AccessTokenExpired, 401)]
    [InlineData(ResStatus.RefreshTokenExpired, 401)]
    [InlineData(ResStatus.ErrorWarning, 400)]
    public void Projection_WhenStatusIsLegacy_ShouldRetainHttpMapping(ResStatus status, int http)
    {
        Assert.Equal(http, ResultHttpProjection.GetStatusCode(new Res("", status)));
    }

    [Fact]
    public void Error_WhenTraceIsMissing_ShouldRejectInvalidConstruction()
    {
        Assert.Throws<ArgumentException>(() => new ResultError("test", ""));
        var context = new DefaultHttpContext { TraceIdentifier = "request-123" };
        Assert.Equal(ResultTraceId.Capture(context), ResultTraceId.Capture(context));
        Assert.False(string.IsNullOrWhiteSpace(ResultTraceId.Capture()));
    }

    [Fact]
    public void Presentation_WhenMetadataContainsDiagnostics_ShouldRetainOnlyPublicMembersAndTypedError()
    {
        var error = new ResultError("domain.conflict", "trace-123");
        var result = Res.Fail("Domain message").SetError(error)
            .SetMetadata("request", "secret").SetMetadata("exception2", "secret")
            .SetMetadata("chain_error", "secret").SetMetadata("detail", "secret")
            .SetMetadata("availableActions", new[] { "cancel" });
        result.PrepareForPresentation(new JsonSerializerOptions(), new DefaultResultErrorMessageProvider(), "new-trace");
        var json = JsonSerializer.Serialize(result);
        Assert.DoesNotContain("secret", json);
        Assert.Contains("availableActions", json);
        Assert.True(result.TryGetError(new JsonSerializerOptions(), out var actual));
        Assert.Same(error, actual);
    }

    [Fact]
    public void Presentation_WhenTypedJsonContainsUndeclaredDiagnostics_ShouldDiscardThem()
    {
        var json = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        using var document = JsonDocument.Parse("""{"code":"domain.failed","traceId":"origin","stackTrace":"secret"}""");
        var result = Res.Fail("Safe failure").SetMetadata("error", document.RootElement.Clone());
        result.PrepareForPresentation(json, new DefaultResultErrorMessageProvider(), "local");
        Assert.DoesNotContain("secret", JsonSerializer.Serialize(result, json));
        Assert.True(result.TryGetError(json, out var error));
        Assert.Equal("origin", error.TraceId);
    }

    [Fact]
    public void Presentation_WhenLegacyErrorProducerRemains_ShouldRejectMixedContract()
    {
        var result = Res.Fail("failure").SetMetadata("error", new { stackTrace = "secret" });
        Assert.Throws<InvalidOperationException>(() => result.PrepareForPresentation(
            new JsonSerializerOptions(), new DefaultResultErrorMessageProvider(), "trace"));
    }
}
