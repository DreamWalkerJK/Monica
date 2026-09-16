using System.Text.Json;
using System.Reflection;
using System.Text.Json.Serialization.Metadata;
using Monica.Core.Results.Abstractions;

namespace Monica.Core.Results.Models.Internal;

internal sealed class ResultEnvelopeJsonTypeInfoResolver(
    ResultEnvelopeFieldNames fieldNames,
    IJsonTypeInfoResolver? innerResolver = null) : IJsonTypeInfoResolver
{
    private readonly ResultEnvelopeFieldNames _fieldNames = fieldNames ?? throw new ArgumentNullException(nameof(fieldNames));
    private readonly IJsonTypeInfoResolver _innerResolver = innerResolver ?? new DefaultJsonTypeInfoResolver();

    internal static IJsonTypeInfoResolver Create(
        ResultEnvelopeFieldNames fieldNames,
        IJsonTypeInfoResolver? innerResolver)
    {
        ArgumentNullException.ThrowIfNull(fieldNames);

        while (innerResolver is ResultEnvelopeJsonTypeInfoResolver resolver)
        {
            innerResolver = resolver._innerResolver;
        }

        return new ResultEnvelopeJsonTypeInfoResolver(fieldNames, innerResolver);
    }

    public JsonTypeInfo? GetTypeInfo(Type type, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(options);

        var typeInfo = _innerResolver.GetTypeInfo(type, options);
        if (typeInfo is null || typeInfo.Kind != JsonTypeInfoKind.Object || !typeof(IResultEnvelope).IsAssignableFrom(type))
        {
            return typeInfo;
        }

        foreach (var property in typeInfo.Properties)
        {
            var memberName = (property.AttributeProvider as MemberInfo)?.Name ?? property.Name;
            if (!_fieldNames.TryGetResolvedName(memberName, options, out var resolvedName))
            {
                continue;
            }

            property.Name = resolvedName;
        }

        return typeInfo;
    }

}
