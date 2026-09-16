using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Monica.Core.ExceptionHandling.Abstractions;
using Monica.Core.Modularity.Models;
using Monica.Core.Results.Services;

namespace Monica.Core.ExceptionHandling.Services;

/// <summary>Projects opt-in Minimal API validation through ASP.NET Core's problem-details service.</summary>
internal sealed class MonicaValidationProblemDetailsWriter(IRequestRejectionFactory rejections) : IProblemDetailsWriter
{
    public bool CanWrite(ProblemDetailsContext context) =>
        context.HttpContext.GetEndpoint()?.Metadata.GetMetadata<MonicaEndpointMetadata>() is { Kind: MonicaEndpointKind.MinimalApi } &&
        context.ProblemDetails is HttpValidationProblemDetails;

    public async ValueTask WriteAsync(ProblemDetailsContext context)
    {
        var validation = (HttpValidationProblemDetails)context.ProblemDetails;
        var errors = validation.Errors.SelectMany(pair =>
            pair.Value.Select(message => new ValidationResult(message, [pair.Key])));
        var rejection = rejections.FromValidationErrors(context.HttpContext, errors).ToResult();
        // AddValidation already uses 400. Keep that existing numeric result for this endpoint family.
        rejection.Status = Core.Results.ResStatus.BadRequest;
        await ResultHttpProjection.ToMinimalApiResult(rejection).ExecuteAsync(context.HttpContext);
    }
}
