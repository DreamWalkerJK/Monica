using AwesomeAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Monica.AI.UI.UIChat.State;
using Monica.Core.Modularity.Extensions;
using Monica.Modules;

namespace Test.Monica.AI.UI.Modules;

public sealed class ModuleAIUITests
{
    [Fact]
    public void Composition_ShouldRegisterScopedChatWorkspace()
    {
        var builder = WebApplication.CreateBuilder();

        builder.AddMonica(monica =>
        {
            monica.ConfigureTypeDiscovery(options => options.ExcludeDefault());
            monica.AddModule<ModuleAIUI, ModuleAIUIOption>();
        });

        builder.Services.Should().ContainSingle(descriptor =>
            descriptor.ServiceType == typeof(ChatSessionWorkspace)
            && descriptor.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public void Composition_ShouldRegisterNonDisposablePageStateFactory()
    {
        var builder = WebApplication.CreateBuilder();
        builder.AddMonica(monica =>
        {
            monica.ConfigureTypeDiscovery(options => options.ExcludeDefault());
            monica.AddModule<ModuleAIUI, ModuleAIUIOption>();
        });
        builder.Services.Should().ContainSingle(descriptor => descriptor.ServiceType == typeof(ChatPageStateFactory));
        builder.Services.Should().NotContain(descriptor => descriptor.ServiceType == typeof(ChatPageState));
    }
}
