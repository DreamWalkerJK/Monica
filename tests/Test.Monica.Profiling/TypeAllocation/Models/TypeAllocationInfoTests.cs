using AwesomeAssertions;
using Monica.Profiling.TypeAllocation.Models;
using Xunit;

namespace Test.Monica.Profiling.TypeAllocation.Models;

public sealed class TypeAllocationInfoTests
{
    [Theory]
    [InlineData("<>z__ReadOnlyArray<System.String>", "<>z__ReadOnlyArray<String>")]
    [InlineData("<>z__ReadOnlySingleElementList<Monica.UI.Shell.Models.NavigationGroup>+Enumerator", "<>z__ReadOnlySingleElementList<NavigationGroup>+Enumerator")]
    [InlineData("<>f__AnonymousType1<System.Type, System.Collections.Generic.IReadOnlyList<System.String>, System.Collections.Generic.IReadOnlyList<System.String>>[]", "<>f__AnonymousType1<Type, IReadOnlyList<String>, IReadOnlyList<String>>[]")]
    [InlineData("Example.<>f__AnonymousType1<System.String>[]", "<>f__AnonymousType1<String>[]")]
    [InlineData("System.Collections.Generic.List<System.String>[]", "List<String>[]")]
    [InlineData("System.Collections.Generic.List<System.String>[,]", "List<String>[,]")]
    [InlineData("System.Collections.Generic.List<System.String>[,,][]", "List<String>[,,][]")]
    [InlineData("System.Collections.Generic.List<System.String>[*]", "List<String>[*]")]
    [InlineData("Example.Generic<System.Int32>*", "Generic<Int32>*")]
    [InlineData("Example.Generic<System.Int32>&", "Generic<Int32>&")]
    [InlineData("Example.Generic<System.Int32>[]*&", "Generic<Int32>[]*&")]
    [InlineData("Example.Outer<System.String>+Enumerator", "Outer<String>+Enumerator")]
    [InlineData("Example.Outer<System.String>+Nested<System.Int32>[]", "Outer<String>+Nested<Int32>[]")]
    [InlineData("Example.Outer<System.String>+Middle+Nested<System.Int32>[,]", "Outer<String>+Middle+Nested<Int32>[,]")]
    [InlineData("System.Collections.Generic.Dictionary<System.String, System.Collections.Generic.List<Example.Widget[,]>>", "Dictionary<String, List<Widget[,]>>")]
    [InlineData("Example.Generic<Example.Outer+Nested, Example.Widget[]>", "Generic<Outer+Nested, Widget[]>")]
    [InlineData("System.Collections.Generic.List`1[[System.String, System.Private.CoreLib]]", "List[[System.String, System.Private.CoreLib]]")]
    [InlineData("System.Collections.Generic.List`1[[System.String, System.Private.CoreLib]][]", "List[[System.String, System.Private.CoreLib]][]")]
    [InlineData("Example.Generic`1", "Generic`1")]
    [InlineData("Example.Tables[System.IntPtr, SkiaSharp.SKObject]", "Tables[IntPtr, SKObject]")]
    [InlineData("Example.Tables[[System.IntPtr, Core],[SkiaSharp.SKObject, SkiaSharp]]", "Tables[IntPtr, SKObject]")]
    [InlineData("Example.Tables[System.IntPtr, SkiaSharp.SKObject][]", "Tables[IntPtr, SKObject][]")]
    [InlineData("System.Byte[]", "Byte[]")]
    [InlineData("Example.Widget[,][]", "Widget[,][]")]
    [InlineData("Example.Widget*&", "Widget*&")]
    [InlineData("Example.Outer+Nested", "Outer+Nested")]
    [InlineData("<GetEvents>d__19", "<GetEvents>d__19")]
    [InlineData("Example.<GetEvents>d__19", "<GetEvents>d__19")]
    public void ShortTypeName_WhenSupportedNameHasGenericsOrQualifiers_ShouldPreserveStructureWithoutChangingIdentity(
        string fullName, string expected)
    {
        var row = new TypeAllocationInfo { TypeName = fullName };

        row.ShortTypeName.Should().Be(expected);
        row.TypeName.Should().Be(fullName);
    }

    [Fact]
    public void ShortTypeName_WhenNamespacesCollide_ShouldKeepTheirFullNamesDistinct()
    {
        var first = new TypeAllocationInfo { TypeName = "First.Domain.Generic<First.Models.Widget>[]" };
        var second = new TypeAllocationInfo { TypeName = "Second.Domain.Generic<Second.Models.Widget>[]" };

        first.ShortTypeName.Should().Be("Generic<Widget>[]");
        second.ShortTypeName.Should().Be(first.ShortTypeName);
        first.TypeName.Should().NotBe(second.TypeName);
        first.TypeName.Should().Be("First.Domain.Generic<First.Models.Widget>[]");
        second.TypeName.Should().Be("Second.Domain.Generic<Second.Models.Widget>[]");
    }
}
