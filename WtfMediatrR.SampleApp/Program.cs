using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WtfMediatr.LibraryWithCommandsAndGenericPreprocessor;

Console.WriteLine("So here is the story:");
Console.WriteLine("We have CommandA and CommandB - two request types we'll handle using MediatR. They are defined in 'WtfMediatr.LibraryWithCommandsAndGenericPreprocessor'.");
Console.WriteLine("We have a HostedService that can send these commands through Mediatr, and a service builder that creates a host.");
Console.WriteLine("There are handlers for both commands, located in a same assembly as commands.");
Console.WriteLine("Both commands implements interface ISomeCommand, and there is a postprocessor that supposed to handle requests of this type.");
Console.WriteLine("Handlers and preprocessors just adds some text to shared ConcurrentQueue, and we can use it to find out what was invoked and when.");

Console.WriteLine("");
Console.WriteLine("1. Let's try to use it in a straightforward way: services.AddMediatR(typeof(CommandA).Assembly);");
var builder = new HostBuilder()
    .ConfigureServices((_, services) =>
    {
        services.AddMediatR(typeof(CommandA).Assembly);
        services.AddHostedService<HostedService>();
        Console.WriteLine("Registered services (only MediatR-related):");
        
        foreach (var service in services.Select(s => s.ToString()).Where(s => s.Contains("MediatR")))
        {
            Console.WriteLine(service.ToString());
        }
        Console.WriteLine();
    });

Console.WriteLine("Lets' run the host!");
await builder.RunConsoleAsync();
Console.WriteLine();
Console.WriteLine();

Console.WriteLine("Surprize! PostProcessor was not invoked. Is it because postprocessors for interfaces are not supported?");
Console.WriteLine("Let's check this out! I have a separate assembly with a concrete postprocessor for CommandB.");
Console.WriteLine("2. Now I use `AddMediatR()` for 2 assemblies, original one and a separate assembly with just concrete postprocessor");
Console.WriteLine("Running host..");

builder = new HostBuilder()
    .ConfigureServices((_, services) =>
    {
        services.AddMediatR(typeof(CommandA).Assembly);
        services.AddMediatR(typeof(WtfMediatr.LibraryWithConcretePreprocessor.PostProcessorForCommandB).Assembly);
        services.AddHostedService<HostedService>();

        Console.WriteLine("Registered services (only MediatR-related):");

        foreach (var service in services.Select(s => s.ToString()).Where(s => s.Contains("MediatR")))
        {
            Console.WriteLine(service.ToString());
        }
        Console.WriteLine();
    });

await builder.RunConsoleAsync();
Console.WriteLine();
Console.WriteLine("Still no luck! Postprocessor registered in container, but still does not work!");
Console.WriteLine("3. What if we move all items to single assembly? Let's see...");

builder = new HostBuilder()
    .ConfigureServices((_, services) =>
    {
        services.AddMediatR(typeof(WtfMediatr.LibraryWithCommandsAndGenericAndConcretePostProcessors.PostProcessorForCommandB).Assembly);
        services.AddHostedService<HostedService>();

        Console.WriteLine("Registered services (only MediatR-related):");

        foreach (var service in services.Select(s => s.ToString()).Where(s => s.Contains("MediatR")))
        {
            Console.WriteLine(service.ToString());
        }
        Console.WriteLine();
    });


await builder.RunConsoleAsync();
Console.WriteLine();
Console.WriteLine("It's finally working! Well, at least for type CommandB... And most likely because this time service factory was able to generate a class from open-typed generic - we have 2 registrations of ");


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
        Console.WriteLine("Running commands");
        for (var i = 0; i < attempts; i++)
        {
            await _mediator.Send(new CommandA(), stoppingToken);
            await _mediator.Send(new CommandB(), stoppingToken);
        }
        Console.WriteLine();
        Console.WriteLine("Checking invoked steps:");
        while (StepsHandle.Steps.TryDequeue(out var result))
        {
            Console.WriteLine(result);
        }

        Console.WriteLine("Press Ctrl+C to continue...");
    }
}