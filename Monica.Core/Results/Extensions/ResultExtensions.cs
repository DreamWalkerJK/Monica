using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Net;
using System.Text;
using Monica.Core.Results.Abstractions;

using Monica.Tool.Extensions;
// ReSharper disable once CheckNamespace
namespace Monica.Core.Results;

public static class ResultExtensions
{
    /// <summary>
    /// Get the HttpStatusCode corresponding to the response code
    /// </summary>
    /// <returns></returns>
    public static HttpStatusCode? ToHttpStatusCode(this IResultEnvelope? response)
    {
        if (response == null) return null;
        switch (response.Status)
        {
            case ResStatus.Ok:
                return HttpStatusCode.OK;

            case ResStatus.Created:
                return HttpStatusCode.Created;

            case ResStatus.NotFound:
                return HttpStatusCode.NotFound;

            case ResStatus.Conflict:
                return HttpStatusCode.Conflict;

            case ResStatus.Unauthorized:
            case ResStatus.RefreshTokenExpired:
            case ResStatus.AccessTokenExpired:
                return HttpStatusCode.Unauthorized;


            case ResStatus.Forbidden:
                return HttpStatusCode.Forbidden;


            case ResStatus.ValidateError:
            case ResStatus.ErrorWarning:
            case ResStatus.BadRequest:
                return HttpStatusCode.BadRequest;

            case ResStatus.PayloadTooLarge:
                return HttpStatusCode.RequestEntityTooLarge;

            case ResStatus.UnsupportedMediaType:
                return HttpStatusCode.UnsupportedMediaType;

            case ResStatus.TooManyRequests:
                return HttpStatusCode.TooManyRequests;

            case ResStatus.BadGateway:
                return HttpStatusCode.BadGateway;

            case ResStatus.ServiceUnavailable:
                return HttpStatusCode.ServiceUnavailable;

            case ResStatus.GatewayTimeout:
                return HttpStatusCode.GatewayTimeout;


            case ResStatus.InternalError:
                return HttpStatusCode.InternalServerError;


            case ResStatus.Unknown:
                return null;
            default:
                throw new ArgumentOutOfRangeException(response.ToString(), $"No HTTP status code mapping is defined for status {response.Status}.");
        }
    }

    /// <summary>
    ///  [200/201] indicates successful processing; success does not guarantee non-null Data.
    /// </summary>
    public static bool IsOk(this IResultEnvelope res) => res.Status is ResStatus.Ok or ResStatus.Created;

    /// <summary>
    /// Indicates an uninitialized envelope. Remote responses require the fuller wire-contract validation performed by the RPC boundary.
    /// </summary>
    public static bool IsMalformed(this IResultEnvelope res)
    {
        return res.Status == ResStatus.Unknown;
    }
    /// <summary>
    /// Additional information for interface settings (duplication will overwrite)
    /// </summary>
    /// <param name="res"></param>
    /// <param name="name"></param>
    /// <param name="info"></param>
    public static T SetMetadata<T>(this T res, string name, object? info = null) where T : IResultEnvelope
    {
        res.Metadata ??= new ExpandoObject();
        res.Metadata.Set(name, info);
        return res;
    }
    /// <summary>
    /// Add additional information to the interface (add a suffix if the Name is repeated)
    /// </summary>
    /// <param name="res"></param>
    /// <param name="name"></param>
    /// <param name="info"></param>
    public static T AppendMetadata<T>(this T res, string name, object? info = null) where T : IResultEnvelope
    {
        res.Metadata ??= new ExpandoObject();
        res.Metadata.Append(name, info);
        return res;
    }

    /// <summary>
    /// Adds in-process technical detail. HTTP and remote-call presentation remove this reserved member;
    /// record diagnostics through the host's logger when operators need to retain them.
    /// </summary>
    /// <param name="res">The result envelope to enrich.</param>
    /// <param name="detail">Technical detail for local consumers, never a public response contract.</param>
    public static T WithDetail<T>(this T res, object? detail) where T : IResultEnvelope
    {
        return detail is null ? res : res.SetMetadata("detail", detail);
    }

    /// <summary>
    /// Adds formatted in-process detail that is removed at HTTP and remote-call presentation boundaries.
    /// </summary>
    /// <param name="res">The result envelope to enrich.</param>
    /// <param name="format">Composite format string for technical detail.</param>
    /// <param name="args">Composite format arguments.</param>
    public static T WithDetail<T>(
        this T res,
        [StringSyntax("CompositeFormat")] string? format,
        params object?[] args) where T : IResultEnvelope
    {
        return format is null ? res : res.WithDetail((object)string.Format(format, args));
    }

    /// <summary>
    /// Indicates an unsuccessful status (anything other than 200 or 201).
    /// </summary>
    public static bool IsFailed<T>(this Res<T> res, [NotNullWhen(true)] out Res? error, [MaybeNullWhen(true)]out T data)
    {
        error = null;
        if (res.IsOk(out data)) return false;
        error = res.ToRes();
        return true;
    }
    /// <summary>
    /// Indicates an unsuccessful status (anything other than 200 or 201).
    /// </summary>
    public static bool IsFailed<T>(this Res<T> res, [NotNullWhen(true)] out Res? error)
    {
        error = null;
        if (res.IsOk()) return false;
        error = res.ToRes();
        return true;
    }
    /// <summary>
    /// Indicates an unsuccessful status (anything other than 200 or 201).
    /// </summary>
    public static bool IsFailed<T>(this Res<T?> res, [NotNullWhen(true)] out Res? error, out T? data) where T : struct
    {
        error = null;
        if (res.IsOk(out data)) return false;
        error = res.ToRes();
        return true;
    }
    /// <summary>
    /// Indicates an unsuccessful status (anything other than 200 or 201).
    /// </summary>
    public static bool IsFailed<T>(this Res<T?> res, [NotNullWhen(true)] out Res? error) where T : struct
    {
        error = null;
        if (res.IsOk()) return false;
        error = res.ToRes();
        return true;
    }
    /// <summary>
    /// Indicates an unsuccessful status (anything other than 200 or 201).
    /// </summary>
    public static bool IsFailed(this Res res, [NotNullWhen(true)] out Res? error)
    {
        error = null;
        if (res.IsOk()) return false;
        error = res;
        return true;
    }

    /// <summary>
    /// Indicates an unsuccessful status (anything other than 200 or 201).
    /// </summary>
    public static bool IsFailed<T>(this ResPaged<T> res, [NotNullWhen(true)] out Res? error, out ResPaged<T>.PageData data)
    {
        error = null;
        if (res.IsOk(out data)) return false;
        error = res.ToRes();
        return true;
    }

    /// <summary>
    /// Indicates an unsuccessful status (anything other than 200 or 201).
    /// </summary>
    public static bool IsFailed<T>(this ResPaged<T> res, [NotNullWhen(true)] out Res? error)
    {
        error = null;
        if (res.IsOk()) return false;
        error = res.ToRes();
        return true;
    }

    /// <summary>
    /// [200/201] indicates successful processing; success does not guarantee non-null Data.
    /// </summary>
    public static bool IsOk<T>(this ResPaged<T> res, out ResPaged<T>.PageData data)
    {
        data = res.Data;
        return res.IsOk();
    }
    /// <summary>
    /// [200/201] indicates successful processing; success does not guarantee non-null Data.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="res"></param>
    /// <param name="data"></param>
    /// <returns></returns>
    public static bool IsOk<T>(this Res<T?> res, out T? data) where T : struct
    {
        data = res.Data!;
        return res.IsOk();
    }

    /// <summary>
    /// [200/201] indicates successful processing; success does not guarantee non-null Data.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="res"></param>
    /// <param name="data"></param>
    /// <returns></returns>
    public static bool IsOk<T>(this Res<T> res,  [MaybeNullWhen(false)] out T data)
    {
        data = res.Data!;
        return res.IsOk();
    }
    
    /// <summary>
    /// Batch call results are converted to single call results.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="responses"></param>
    /// <returns></returns>
    public static Res<List<T>> ToBulkRes<T>(this IEnumerable<Res<T>> responses)
    {
        var list = new List<T?>();
        var result = new Res<List<T>>()
        {
            Status = ResStatus.Ok,
            Message = ""
        };
        var sb = new StringBuilder();
        foreach (var res in from response in responses select response)
        {
            if (res.Message is { } msg)
            {
                sb.AppendLine(msg);
            }

            if (res.Metadata is { } metadata)
            {
                result.Metadata ??= new ExpandoObject();
                result.Metadata.Append("bulk", metadata);
            }
            if (!res.IsOk(out var data) && result.Status == ResStatus.Ok)
            {
                result.Status = res.Status;
            }
            list.Add(data);
        }

        result.Message = sb.ToString().TrimEnd();
        result.Data = list.Cast<T>().ToList();
        return result;
    }
    
    /// <summary>
    /// Additional information
    /// </summary>
    /// <param name="self"></param>
    /// <param name="message"></param>
    public static T AppendMessage<T>(this T self, string? message)
        where T : IResultEnvelope
    {
        return Append(self, message);
    }
    /// <summary>
    /// Additional information
    /// </summary>
    /// <param name="self"></param>
    /// <param name="message"></param>
    private static T Append<T>(this T self, string? message)
        where T : IResultEnvelope
    {
        if (!string.IsNullOrWhiteSpace(message))
        {
            self.Message += $";{message}";
            self.Message = self.Message.TrimStart(';');
        }
        return self;
    }
    /// <summary>
    /// append error
    /// </summary>
    /// <param name="self"></param>
    /// <param name="message"></param>
    /// <param name="status"></param>
    public static T AppendFailure<T>(this T self, string? message, ResStatus status = ResStatus.BadRequest)
        where T : IResultEnvelope
    {
        self = Append(self, message);
        self.Status = status;
        return self;
    }
}
