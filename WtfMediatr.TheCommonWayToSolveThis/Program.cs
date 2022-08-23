using System.Reflection;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WtfMediatr.Core;
using WtfMediatr.StraightForwardImplementation;
using WtfMediatr.TheCommonWayToSolveThis;

/// Соберем хост и запустим его!

var cts = new CancellationTokenSource(1000);
await new HostBuilder()
    .ConfigureServices((_, services) =>
    {
        services.AddSingleton(new Wallet());
        services.AddSingleton(new TransactionLog());

        services.AddMediatR(typeof(ChangeBudgetCommandsPostProcessor).Assembly);
        services.AddTransient(
            typeof(IPipelineBehavior<IChangeBudgetCommand, Unit>), // FAIL, MediatR will never ask IoC container for this interface
            typeof(LoggingPipelineBehavior));

        services.AddHostedService<SampleHostedService>();
    })
    .RunConsoleAsync(cts.Token);