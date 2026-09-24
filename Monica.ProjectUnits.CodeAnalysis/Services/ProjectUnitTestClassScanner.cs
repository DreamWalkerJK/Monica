using Microsoft.CodeAnalysis;
using Monica.ProjectUnits.CodeAnalysis.Models;

namespace Monica.ProjectUnits.CodeAnalysis.Services;

/// <summary>
/// Collects test classes from the declared types of one test project. A test class is a concrete,
/// non-generic class owning at least one Fact/Theory test method; traits declared on the class and on
/// those methods are merged, because the test framework applies class-level traits to every contained
/// test and method-level traits refine the class set.
/// </summary>
internal static class ProjectUnitTestClassScanner
{
    private const string TRAIT_ATTRIBUTE = "TraitAttribute";
    private const string TRAIT_INVALID_CODE = "ProjectUnit.Tests.Trait.Invalid";
    private static readonly HashSet<string> TestMethodAttributeNames = ["FactAttribute", "TheoryAttribute"];

    internal sealed record TestClassCandidate(
        INamedTypeSymbol Symbol,
        int TestMethodCount,
        IReadOnlyList<ProjectUnitSourceTestTrait> Traits,
        IReadOnlyList<ProjectUnitSourceDiagnostic> Diagnostics);

    internal static IEnumerable<TestClassCandidate> Scan(IEnumerable<INamedTypeSymbol> symbols)
    {
        foreach (var symbol in symbols)
        {
            if (symbol is not { TypeKind: TypeKind.Class, IsAbstract: false, Arity: 0 })
            {
                continue;
            }

            var testMethods = symbol.GetMembers()
                .OfType<IMethodSymbol>()
                .Where(IsTestMethod)
                .ToArray();
            if (testMethods.Length == 0)
            {
                continue;
            }

            var traits = new Dictionary<(string Key, string Value), ProjectUnitSourceTestTrait>();
            var diagnostics = new List<ProjectUnitSourceDiagnostic>();
            CollectTraits(symbol, symbol.GetAttributes(), traits, diagnostics);
            foreach (var method in testMethods)
            {
                CollectTraits(symbol, method.GetAttributes(), traits, diagnostics);
            }

            yield return new TestClassCandidate(
                symbol,
                testMethods.Length,
                traits.Values
                    .OrderBy(static trait => trait.Key, StringComparer.Ordinal)
                    .ThenBy(static trait => trait.Value, StringComparer.Ordinal)
                    .ToArray(),
                diagnostics);
        }
    }

    private static bool IsTestMethod(IMethodSymbol method)
        => method.GetAttributes().Any(attribute =>
            attribute.AttributeClass is { } attributeClass
            && TestMethodAttributeNames.Contains(attributeClass.Name));

    private static void CollectTraits(
        INamedTypeSymbol owner,
        IEnumerable<AttributeData> attributes,
        IDictionary<(string Key, string Value), ProjectUnitSourceTestTrait> traits,
        ICollection<ProjectUnitSourceDiagnostic> diagnostics)
    {
        foreach (var attribute in attributes.Where(static attribute =>
                     attribute.AttributeClass is { Name: TRAIT_ATTRIBUTE }))
        {
            if (attribute.ConstructorArguments is not [{ Value: string key }, { Value: string value }])
            {
                diagnostics.Add(InvalidTraitDiagnostic(owner));
                continue;
            }

            var normalizedKey = key.Trim();
            var normalizedValue = value.Trim();
            if (normalizedKey.Length == 0 || normalizedValue.Length == 0)
            {
                diagnostics.Add(InvalidTraitDiagnostic(owner));
                continue;
            }

            traits[(normalizedKey, normalizedValue)] = new ProjectUnitSourceTestTrait(normalizedKey, normalizedValue);
        }
    }

    private static ProjectUnitSourceDiagnostic InvalidTraitDiagnostic(INamedTypeSymbol owner)
        => new(
            TRAIT_INVALID_CODE,
            ProjectUnitSourceDiagnosticSeverity.Warning,
            $"Test trait attributes on '{owner.GetRuntimeName()}' must declare constant, non-empty key and value strings.",
            null,
            null,
            null);
}
