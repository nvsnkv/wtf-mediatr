using MediatR;
using MediatR.Pipeline;
using WtfMediatr.Core;

namespace WtfMediatr.StraightForwardImplementation;

/// <summary>
/// Давайте реализуем обработчики вышеописанных команд
/// </summary>
internal sealed class StraightForwardProcessIncomeCommandHandler : IRequestHandler<ProcessIncomeCommand>, IRequestHandler<ProcessSpendingCommand>
{
    private readonly Wallet wallet;

    public StraightForwardProcessIncomeCommandHandler(Wallet wallet) { this.wallet = wallet; }


    public Task<Unit> Handle(ProcessIncomeCommand request, CancellationToken ct)
    {
        wallet.Process(request.Income);
        return Task.FromResult(Unit.Value);
    }

    public Task<Unit> Handle(ProcessSpendingCommand request, CancellationToken ct)
    {
        wallet.Process(request.Spending);
        return Task.FromResult(Unit.Value);
    }
}

/// Тривиально
internal sealed class GetBalanceQueryHandler : IRequestHandler<GetBalanceQuery, decimal>
{
    private readonly Wallet wallet;

    public GetBalanceQueryHandler(Wallet wallet)
    {
        this.wallet = wallet;
    }

    public Task<decimal> Handle(GetBalanceQuery request, CancellationToken cancellationToken)
    {
        return Task.FromResult(wallet.Balance);
    }
}

/// Тривиально
internal sealed class GetTransactionsQueryHandler : IRequestHandler<GetLoggedTransactionsQuery, IReadOnlyList<ITransaction>>
{
    private readonly TransactionLog log;

    public GetTransactionsQueryHandler(TransactionLog log)
    {
        this.log = log;
    }

    public Task<IReadOnlyList<ITransaction>> Handle(GetLoggedTransactionsQuery request, CancellationToken cancellationToken)
    {
        return Task.FromResult(log.GetLoggedTransactions());
    }
}

/// Наконец, давайте воспользуемся концепцией пост-обработчиков из MediatR для запоминания тразакций
internal sealed class ChangeBudgetCommandsPostProcessor : IRequestPostProcessor<IChangeBudgetCommand, Unit>
{
    private TransactionLog log;

    public ChangeBudgetCommandsPostProcessor(TransactionLog log)
    {
        this.log = log;
    }

    public Task Process(IChangeBudgetCommand request, Unit response, CancellationToken cancellationToken)
    {
        ITransaction transaction = request switch
        {
            ProcessIncomeCommand incomeCommand => incomeCommand.Income,
            ProcessSpendingCommand spendingCommand => spendingCommand.Spending,
            _ => throw new ArgumentException("Not supported command!")
        };

        log.Log(transaction);
        return Task.FromResult(transaction);
    }
}