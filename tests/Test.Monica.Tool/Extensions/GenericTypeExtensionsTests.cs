using Monica.Tool.Extensions;

namespace Test.Monica.Tool.Extensions;

public sealed class GenericTypeExtensionsTests
{
    [Theory]
    [InlineData(typeof(List<string>[]), "System.Collections.Generic.List<System.String>[]")]
    [InlineData(typeof(List<string>[,]), "System.Collections.Generic.List<System.String>[,]")]
    [InlineData(typeof(List<string>[,,]), "System.Collections.Generic.List<System.String>[,,]")]
    [InlineData(typeof(List<string>[][]), "System.Collections.Generic.List<System.String>[][]")]
    [InlineData(typeof(List<string[]>[]), "System.Collections.Generic.List<System.String[]>[]")]
    [InlineData(typeof(List<List<string>[]>), "System.Collections.Generic.List<System.Collections.Generic.List<System.String>[]>")]
    [InlineData(typeof(Dictionary<string, List<int[]>[]>[,]), "System.Collections.Generic.Dictionary<System.String,System.Collections.Generic.List<System.Int32[]>[]>[,]")]
    public void GetCleanFullName_WhenArraysContainGenericTypes_ShouldPreserveShapeWithoutAssemblyIdentity(Type type, string expected)
    {
        var name = type.GetCleanFullName();

        name.Should().Be(expected);
    }

    [Fact]
    public void GetCleanFullName_WhenArrayHasOneNonVectorDimension_ShouldPreserveItsArrayKind()
    {
        var type = typeof(List<string>).MakeArrayType(1);

        type.GetCleanFullName().Should().Be("System.Collections.Generic.List<System.String>[*]");
    }
}
