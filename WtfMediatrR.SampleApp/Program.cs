using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WtfMediatr.LibraryWithCommandsAndGenericAndConcretePostProcessors;
using WtfMediatr.LibraryWithCommandsAndGenericPreprocessor;
using WtfMediatr.LibraryWithConcretePreprocessor;

var builder = new HostBuilder()
    .ConfigureServices((_, services) =>
    {
        services.AddMediatR(typeof(CommandA).Assembly);
        services.AddHostedService<HostedService>();
    });

Console.WriteLine("Running host without concrete builder:");
await builder.RunConsoleAsync();
Console.WriteLine();


builder = new HostBuilder()
    .ConfigureServices((_, services) =>
    {
        services.AddMediatR(typeof(CommandA).Assembly);
        services.AddMediatR(typeof(WtfMediatr.LibraryWithConcretePreprocessor.PostProcessorForCommandB).Assembly);
        services.AddHostedService<HostedService>();
    });

Console.WriteLine("Running host with concrete builder from other assembly:");
await builder.RunConsoleAsync();
Console.WriteLine();

builder = new HostBuilder()
    .ConfigureServices((_, services) =>
    {
        services.AddMediatR(typeof(WtfMediatr.LibraryWithCommandsAndGenericAndConcretePostProcessors.PostProcessorForCommandB).Assembly);
        services.AddHostedService<HostedService>();
    });

Console.WriteLine("Running host with concrete builder from same assembly:");
await builder.RunConsoleAsync();
Console.WriteLine();


Console.WriteLine("Running host with concrete builder from same assembly, 2 attempts:");
HostedService.attempts = 2;
builder = new HostBuilder()
    .ConfigureServices((_, services) =>
    {
        services.AddMediatR(typeof(WtfMediatr.LibraryWithCommandsAndGenericAndConcretePostProcessors.PostProcessorForCommandB).Assembly);
        services.AddHostedService<HostedService>();
    });

await builder.RunConsoleAsync();
Console.WriteLine();

class HostedService : BackgroundService
{
    public static int attempts = 1;

    private IMediator _mediator;

    public HostedService(IMediator mediator)
    {
        _mediator = mediator;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        for (var i = 0; i < attempts; i++)
        {
            await _mediator.Send(new CommandA(), stoppingToken);
            await _mediator.Send(new CommandB(), stoppingToken);
        }

        while (StepsHandle.Steps.TryDequeue(out var result))
        {
            Console.WriteLine(result);
        }
    }
}