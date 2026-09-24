namespace Monica.Core.Results.Abstractions;

/// <summary>Provides host-owned localized fallback text for public errors.</summary>
public interface IResultErrorMessageProvider
{
    /// <summary>Formats safe text using logical names. Never include request values or exception detail.</summary>
    string GetMessage(ResultError error);
}
