using MediatR;
using WtfMediatr.Core;

namespace WtfMediatr.TheCommonWayToSolveThis;

public class LoggingPipelineBehavior : IPipelineBehavior<IChangeBudgetCommand, Unit>
{
    private readonly TransactionLog _log;

    public LoggingPipelineBehavior(TransactionLog log)
    {
        _log = log;
    }

    public async Task<Unit> Handle(IChangeBudgetCommand request, CancellationToken cancellationToken, RequestHandlerDelegate<Unit> next)
    {
        var result = await next();

        ITransaction transaction = request switch
        {
            ProcessIncomeCommand incomeCommand => incomeCommand.Income,
            ProcessSpendingCommand spendingCommand => spendingCommand.Spending,
            _ => throw new ArgumentException("Not supported command!")
        };

        

        _log.Log(transaction);
        return result;
    }
}