﻿
using System.Reflection;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WtfMediatr.Core;

/// Соберем хост и запустим его!

var cts = new CancellationTokenSource(1000);
await new HostBuilder()
    .ConfigureServices((_, services) =>
    {
        services.AddSingleton(new Wallet());
        services.AddSingleton(new TransactionLog());

        services.AddMediatR(Assembly.GetExecutingAssembly()); 

        services.AddHostedService<SampleHostedService>();
    })
    .RunConsoleAsync(cts.Token);
#region spoiler
/* 
Результатом выполнения этого кода будет следующее:
Balance: 200
Transactions:


Транзакции были применены, но ни одна из них ни была залогирована
*/
#endregion