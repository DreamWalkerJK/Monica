using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Reflection.Emit;
using AwesomeAssertions;
using Monica.Configuration.Annotations;
using Monica.Configuration.Models;
using Monica.Configuration.Serialization;
using Monica.Configuration.Services;
using Monica.Configuration.Services.Support;
using Xunit;

namespace Test.Monica.Configuration.Services;

public sealed class ConfigurationValidationRevisionTests
{
    [Fact]
    public void Scan_WhenPortableOptionsAreRebuilt_ShouldKeepPublicationBytesIdentical()
    {
        var first = CreateOptionsType(false);
        var second = CreateOptionsType(false);
        first.Module.ModuleVersionId.Should().NotBe(second.Module.ModuleVersionId);
        var scanner = new ConfigurationDefinitionScanner(new ConfigurationSchemaHasher());

        var before = scanner.Scan(first);
        var after = scanner.Scan(second);

        before.ValidationContract.Capability.Should().Be(ConfigurationValidationCapability.PortableOnly);
        before.ValidationContract.Revision.Should().NotBeNullOrWhiteSpace();
        after.ValidationContract.Should().Be(before.ValidationContract);
        after.SchemaHash.Should().Be(before.SchemaHash);
        ConfigurationDefinitionSchemaCodec.SerializeSchema(after)
            .Should().Be(ConfigurationDefinitionSchemaCodec.SerializeSchema(before));
    }

    [Fact]
    public void Scan_WhenExecutableObjectRulesAreRebuilt_ShouldInvalidateValidationRevisionOnly()
    {
        var scanner = new ConfigurationDefinitionScanner(new ConfigurationSchemaHasher());
        var before = scanner.Scan(CreateOptionsType(true));
        var after = scanner.Scan(CreateOptionsType(true));

        after.ValidationContract.Capability.Should().Be(ConfigurationValidationCapability.ObjectCode);
        after.SchemaHash.Should().Be(before.SchemaHash);
        after.ValidationContract.Revision.Should().NotBe(before.ValidationContract.Revision);
    }

    private static Type CreateOptionsType(bool objectRules)
    {
        // Separate modules simulate two builds with the same public type identity and schema.
        var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("ValidationRevisionFixture"), AssemblyBuilderAccess.RunAndCollect);
        var type = assembly.DefineDynamicModule("Options").DefineType("Fixture.Options", TypeAttributes.Public);
        type.DefineDefaultConstructor(MethodAttributes.Public);
        type.SetCustomAttribute(new CustomAttributeBuilder(
            typeof(ConfigurationAttribute).GetConstructor([typeof(string)])!, ["RevisionFixture"]));
        if (objectRules)
        {
            type.AddInterfaceImplementation(typeof(IValidatableObject));
            var method = type.DefineMethod(nameof(IValidatableObject.Validate),
                MethodAttributes.Public | MethodAttributes.Virtual,
                typeof(IEnumerable<ValidationResult>), [typeof(ValidationContext)]);
            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Ret);
        }

        return type.CreateType()!;
    }
}
