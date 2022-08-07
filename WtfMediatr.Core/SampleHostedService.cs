using MediatR;
using Microsoft.Extensions.Hosting;

namespace WtfMediatr.Core;

/// <summary>
/// Для эмуляции поведения ASP.Net Core приложения, создадим тестовый HostedService
/// </summary>
public class SampleHostedService : IHostedService
{
    private readonly IMediator mediator;

    public SampleHostedService(IMediator mediator)
    {
        this.mediator = mediator;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await mediator.Send(new ProcessIncomeCommand(new Income(400)), cancellationToken);
        await mediator.Send(new ProcessIncomeCommand(new Income(600)), cancellationToken);

        await mediator.Send(new ProcessSpendingCommand(new Spending(-500)), cancellationToken);
        await mediator.Send(new ProcessSpendingCommand(new Spending(-300)), cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        var balance = await mediator.Send(new GetBalanceQuery(), cancellationToken);
        var transactions = await mediator.Send(new GetLoggedTransactionsQuery(), cancellationToken);

        Console.WriteLine($"Balance: {balance}");
        Console.WriteLine("Transactions: ");
        foreach (var transaction in transactions)
        {
            Console.WriteLine(transaction.Amount);
        }
    }
}

// И продолжим этот рассказ в сборке 'WtfMediatr.StraightfForwardImplementation'