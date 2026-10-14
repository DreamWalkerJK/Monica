using AwesomeAssertions;
using Monica.Core.Results;

namespace Test.Monica.DataChannel.Support;

internal static class ResultAssertions
{
    public static T Success<T>(Res<T> result) where T : class?
    {
        result.Status.Should().Be(ResStatus.Ok);
        result.Message.Should().BeNullOrEmpty();
        result.Data.Should().NotBeNull();
        return result.Data!;
    }
}
