using Microsoft.AspNetCore.Mvc;

namespace Monica.Core.ExceptionHandling.Services;

/// <summary>Prevents framework binding messages from echoing attempted request values.</summary>
internal static class RequestBindingMessages
{
    public static void Configure(MvcOptions options)
    {
        var messages = options.ModelBindingMessageProvider;
        messages.SetMissingBindRequiredValueAccessor(_ => "A value is required.");
        messages.SetMissingKeyOrValueAccessor(() => "A key and value are required.");
        messages.SetMissingRequestBodyRequiredValueAccessor(() => "A request body is required.");
        messages.SetValueMustNotBeNullAccessor(_ => "A value is required.");
        messages.SetAttemptedValueIsInvalidAccessor((_, _) => "Enter a valid value.");
        messages.SetNonPropertyAttemptedValueIsInvalidAccessor(_ => "Enter a valid value.");
        messages.SetUnknownValueIsInvalidAccessor(_ => "Enter a valid value.");
        messages.SetNonPropertyUnknownValueIsInvalidAccessor(() => "Enter a valid value.");
        messages.SetValueIsInvalidAccessor(_ => "Enter a valid value.");
        messages.SetValueMustBeANumberAccessor(_ => "Enter a number.");
        messages.SetNonPropertyValueMustBeANumberAccessor(() => "Enter a number.");
    }
}
