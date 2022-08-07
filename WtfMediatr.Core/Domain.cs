namespace WtfMediatr.Core;
// 1. ПОСТАНОВКА ЗАДАЧИ
/// Давайте попробуем реализовать минималистичный набор сервисов для контроля за персональным бюджетом

/// Создадим класс, контролирующий бюджет
public sealed class Wallet
{
    public decimal Balance { get; private set; } = 0;

    public void Process(ITransaction t)
    {
        if (Balance + t.Amount < 0)
        {
            throw new InvalidOperationException("Cannot process transaction!");
        }

        Balance += t.Amount;
    }
}

/// Введем интерфейс для операций с бюджетом
public interface ITransaction { decimal Amount { get; } }

/// Выделим класс доходов
public sealed class Income : ITransaction
{
    public Income(decimal amount)
    {
        Amount = amount > 0 ? amount : throw new ArgumentException("Income with negative amount makes no sense!");
    }

    public decimal Amount { get; }
}

// И расходов
public sealed class Spending : ITransaction
{
    public Spending(decimal amount)
    {
        Amount = amount < 0 ? amount : throw new ArgumentException("Spending with positive amount makes no sense!");
    }

    public decimal Amount { get; }
}

// Наконец, создадим класс для хранения истории транзакций
public sealed class TransactionLog
{
    
    private readonly List<ITransaction> log = new();
    // [улучшим хранение истории после релиза MVP в прод]

    public void Log(ITransaction t)
    {
        log.Add(t);
    }

    public IReadOnlyList<ITransaction> GetLoggedTransactions() => log.AsReadOnly();
}
