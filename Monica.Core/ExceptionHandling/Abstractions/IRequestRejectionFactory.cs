using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Monica.Core.ExceptionHandling.Models;
using Monica.Core.Results;

namespace Monica.Core.ExceptionHandling.Abstractions;

/// <summary>Normalizes request failures without exposing formatter exception messages.</summary>
public interface IRequestRejectionFactory
{
    /// <summary>Creates bounded field errors while retaining the existing numeric status policy.</summary>
    RequestRejection FromModelState(ActionContext context, ResStatus status = ResStatus.ValidateError);
    /// <summary>Preserves HTTP 413/415 and extracts safe JSON-path information.</summary>
    RequestRejection FromBadRequest(HttpContext? context, BadHttpRequestException exception);
    /// <summary>Normalizes declared validation errors while retaining the existing validation status.</summary>
    RequestRejection FromValidationErrors(HttpContext? context, IEnumerable<ValidationResult> errors);
}
