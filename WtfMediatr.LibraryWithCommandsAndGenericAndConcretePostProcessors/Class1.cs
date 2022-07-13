using MediatR;
using MediatR.Pipeline;
using WtfMediatr.LibraryWithCommandsAndGenericPreprocessor;

namespace WtfMediatr.LibraryWithCommandsAndGenericAndConcretePostProcessors
{
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

    public class PostProcessorForOtherCommands : IRequestPostProcessor<ISomeCommand, Unit>
    {
        public Task Process(ISomeCommand request, Unit response, CancellationToken cancellationToken)
        {
            StepsHandle.Steps.Enqueue($"PostProcessorForOtherCommands. Invoked for {request.GetType()}");
            return Task.CompletedTask;
        }
    }

    public class PostProcessorForCommandB : IRequestPostProcessor<CommandB, Unit>
    {
        public Task Process(CommandB request, Unit response, CancellationToken cancellationToken)
        {
            StepsHandle.Steps.Enqueue($"PostProcessorForCommandB. Invoked for {request.GetType()}");
            return Task.CompletedTask;
        }
    }
}