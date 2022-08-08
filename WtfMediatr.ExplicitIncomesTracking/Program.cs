using System.Reflection;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WtfMediatr.Core;

/// Настало время экспериметов! Создаем экземпляр сервиса и запускаем его!

var cts = new CancellationTokenSource(1000);
await new HostBuilder()
    .ConfigureServices((_, services) =>
    {
        services.AddSingleton(new Wallet());
        services.AddSingleton(new TransactionLog());

        services.AddMediatR(Assembly.GetExecutingAssembly()); // регистрируем обработчики из текущей сборки

        services.AddHostedService<SampleHostedService>();
    })
    .RunConsoleAsync(cts.Token);

#region spoiler
/* 
Результатом выполнения этого кода будет следующее:
Hurray! You've earned 400 !
Hurray! You've earned 600 !
Balance: 200
Transactions:
400
600
*/

/// Неожиданно,  в этот раз пост-процессор, успешно залогировал транзакции-пополнения, но по-прежнему проигнорировал траты.
/// Почему же так произошло?
#endregion