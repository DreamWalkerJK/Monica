using Microsoft.Extensions.Options;
using Monica.DevOps.Git.Abstractions;
using Monica.DevOps.Git.Models;
using Monica.DevOps.Git.Services;
using Monica.Modules;
using Xunit;

namespace Test.Monica.DevOps.Git.Services;

public sealed class GitCredentialManagerTests
{
    [Fact]
    public void GetResolverInfos_WhenResolverIsGeneric_ShouldExposeReadableTypeAndPreserveIdentityPrecedenceWithoutResolving()
    {
        IGitCredentialResolver[] resolvers = [
            new GenericMetadataResolver<string>("first-low", 1),
            new GenericMetadataResolver<string>("Zulu", 5),
            new GenericMetadataResolver<string>("alpha", 5)];
        var manager = new GitCredentialManager(Options.Create(new ModuleGitOption()), resolvers);

        var metadata = manager.GetResolverInfos();

        Assert.Equal(new[] { "alpha", "Zulu", "first-low" }, metadata.Select(x => x.ResolverId));
        Assert.Equal(new[] { 5, 5, 1 }, metadata.Select(x => x.Order));
        Assert.Equal(new[] { "Resolver alpha", "Resolver Zulu", "Resolver first-low" }, metadata.Select(x => x.DisplayName));
        Assert.All(metadata, item =>
        {
            Assert.Contains("GenericMetadataResolver", item.ImplementationType);
            Assert.Contains("String", item.ImplementationType);
            Assert.DoesNotContain("`1", item.ImplementationType);
            Assert.DoesNotContain("[[", item.ImplementationType);
            Assert.DoesNotContain("Version=", item.ImplementationType);
            Assert.DoesNotContain("Culture=", item.ImplementationType);
            Assert.DoesNotContain("PublicKeyToken=", item.ImplementationType);
        });
    }

    [Fact]
    public void GetCredentialInfos_WhenConfiguredSecretsDiffer_ShouldDescribeActualTokenOrPasswordWithoutResolving()
    {
        var options = new ModuleGitOption
        {
            CredentialRegistrations = [
                new() { Id = "Token", AuthenticationType = GitAuthenticationType.Token, Token = "actual-token" },
                new() { Id = "password", AuthenticationType = GitAuthenticationType.UserPassword, Password = "actual-password", UserName = "alice" },
                new() { Id = "Blank", AuthenticationType = GitAuthenticationType.UserPassword, Token = " ", Password = "\t" },
                new() { Id = "absent", AuthenticationType = GitAuthenticationType.Token, UserName = "named-user" }]
        };
        var manager = new GitCredentialManager(Options.Create(options), [new GenericMetadataResolver<string>("never-resolve", 0)]);

        var metadata = manager.GetCredentialInfos();

        Assert.Equal(new[] { "absent", "Blank", "password", "Token" }, metadata.Select(x => x.Id));
        Assert.Equal(new[] { false, false, true, true }, metadata.Select(x => x.HasSecret));
        Assert.Equal("alice", metadata[2].UserName);
        Assert.Equal(GitAuthenticationType.UserPassword, metadata[2].AuthenticationType);
        Assert.Equal(GitAuthenticationType.Token, metadata[3].AuthenticationType);
    }
}

internal sealed class GenericMetadataResolver<T>(string id, int order) : IGitCredentialResolver
{
    public string ResolverId => id;
    public string DisplayName => $"Resolver {id}";
    public int Order => order;
    public ValueTask<GitResolvedCredential?> ResolveAsync(GitCredentialResolutionContext context,
        CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("Metadata reads must not resolve credentials.");
}
