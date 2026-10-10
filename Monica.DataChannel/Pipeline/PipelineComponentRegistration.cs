using Monica.DataChannel.Abstractions.Pipeline;
using Monica.DataChannel.Models;

namespace Monica.DataChannel.Pipeline;

internal sealed record PipelineComponentRegistration(string Key, PipelineComponentStage Stage,
    int Ordinal, IPipelineComponent Component);
