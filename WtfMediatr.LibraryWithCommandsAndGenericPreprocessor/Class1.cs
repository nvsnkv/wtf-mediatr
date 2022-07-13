using System.Collections.Concurrent;
using MediatR;
using MediatR.Pipeline;

namespace WtfMediatr.LibraryWithCommandsAndGenericPreprocessor
{
    public interface ISomeCommand : IRequest
    {
        bool IsImportant { get; }
    }

    public class CommandA : ISomeCommand
    {
        public bool IsImportant { get; } = true;
    }

    public class CommandB : ISomeCommand
    {
        public bool IsImportant { get; } = false;
    }

    public class CommandAHandler : IRequestHandler<CommandA>
    {
        public Task<Unit> Handle(CommandA request, CancellationToken cancellationToken)
        {
            StepsHandle.Steps.Enqueue("CommandAHandler");
            return Task.FromResult(Unit.Value);
        }
    }

    public class CommandBHandler : IRequestHandler<CommandB>
    {
        public Task<Unit> Handle(CommandB request, CancellationToken cancellationToken)
        {
            StepsHandle.Steps.Enqueue("CommandBHandler");
            return Task.FromResult(Unit.Value);
        }
    }

    public class PostProcessorForBothCommands : IRequestPostProcessor<ISomeCommand, Unit>
    {
        public Task Process(ISomeCommand request, Unit response, CancellationToken cancellationToken)
        {
            StepsHandle.Steps.Enqueue($"PostProcessorForBothCommands. Invoked for {request.GetType()}");
            return Task.CompletedTask;
        }
    }
}

public static class StepsHandle
{
    public static readonly ConcurrentQueue<string> Steps = new();
} 