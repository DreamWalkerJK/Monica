using Monica.Authority.Localization;
using Monica.Core.Results;

namespace Monica.Authority.Authorization.Services.Support;

/// <summary>
/// Builds authorization failure results using the standard 401 status and typed reason codes.
/// </summary>
public static class ResultsAuthorization
{
    /// <summary>Builds the not-logged-in 401 result.</summary>
    public static Res NotLogin(AuthorityMessageLocalizer authorityLocalizer)
    {
        return new Res(authorityLocalizer.GetNotLoggedInMessage(), ResStatus.Unauthorized);
    }

    /// <summary>Builds the access-token-expired 401 result; callers refresh silently on this reason.</summary>
    public static Res AccessTokenExpired(AuthorityMessageLocalizer authorityLocalizer)
    {
        return new Res(authorityLocalizer.GetAccessTokenExpiredMessage(), ResStatus.Unauthorized)
            .SetError(new ResultError(ResultErrorCodes.AccessTokenExpired, ResultTraceId.Capture()));
    }

    /// <summary>Builds the refresh-token-expired 401 result; callers redirect to sign-in on this reason.</summary>
    public static Res RefreshTokenExpired(AuthorityMessageLocalizer authorityLocalizer)
    {
        return new Res(authorityLocalizer.GetRefreshTokenExpiredMessage(), ResStatus.Unauthorized)
            .SetError(new ResultError(ResultErrorCodes.RefreshTokenExpired, ResultTraceId.Capture()));
    }
}
