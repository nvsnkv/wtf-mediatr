using MediatR;

namespace WtfMediatr.Core;

/// Давайте определим запросы, изменяющие баланс:
public interface IChangeBudgetCommand : IRequest { }

public record ProcessIncomeCommand(Income Income) : IChangeBudgetCommand;

public record ProcessSpendingCommand(Spending Spending) : IChangeBudgetCommand;

/// и дополним коллекцию комманд запросами текущего баланса и запомненных транзакций
public sealed class GetBalanceQuery : IRequest<decimal>
{ }

public sealed class GetLoggedTransactionsQuery : IRequest<IReadOnlyList<ITransaction>> { };