using System.Diagnostics;
using MediatR;
using MediatR.Pipeline;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WtfMediatr.Core;
using WtfMediatr.NediatR;
using WtfMediatr.StraightForwardImplementation;

/// Соберем хост и запустим его!
var sw = new Stopwatch();
var cts = new CancellationTokenSource(1000);
await new HostBuilder()
    .ConfigureServices((_, services) =>
    {
        services.AddSingleton(new Wallet());
        services.AddSingleton(new TransactionLog());

        sw.Start();
        services.AddMediatR(typeof(ChangeBudgetCommandsPostProcessor).Assembly);
        sw.Stop();

        Console.WriteLine($"Registered MediatR in {sw.Elapsed}");

        sw.Restart();
        services.EmpowerMediatRHandlersFor(typeof(IRequestPostProcessor<,>));
        sw.Start();
        Console.WriteLine($"Empowered postprocessors in {sw.Elapsed}");

        services.AddHostedService<SampleHostedService>();
    })
    .RunConsoleAsync(cts.Token);
#region spoiler
/*
Balance: 200
Transactions:
400
600
-500
-300

ну чтож, теперь это работает!

*/
#endregion