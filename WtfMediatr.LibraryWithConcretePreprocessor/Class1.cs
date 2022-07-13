using MediatR;
using MediatR.Pipeline;
using WtfMediatr.LibraryWithCommandsAndGenericPreprocessor;

namespace WtfMediatr.LibraryWithConcretePreprocessor
{

    public class PostProcessorForCommandB : IRequestPostProcessor<CommandB, Unit>
    {
        public Task Process(CommandB request, Unit response, CancellationToken cancellationToken)
        {
            StepsHandle.Steps.Enqueue($"PostProcessorForCommandB invoked for {request.GetType()}");
            return Task.CompletedTask;
        }
    }
}