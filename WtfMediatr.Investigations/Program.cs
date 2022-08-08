using MediatR;
using Microsoft.Extensions.DependencyInjection;
using WtfMediatr.Core;
using WtfMediatr.Investigations;

///Давайте поисследуем содержимое IoC контейнеров в первом и во втором случаях

var straightForwardServices = new ServiceCollection();
var explicitServices = new ServiceCollection();
    
TheMediatorSources.AddMediatR(straightForwardServices,
    new[] { typeof(WtfMediatr.StraightForwardImplementation.ChangeBudgetCommandsPostProcessor).Assembly },
    _ => { });

TheMediatorSources.AddMediatR(explicitServices,
    new[] { typeof(WtfMediatr.ExplicitIncomesTracking.ChangeBudgetCommandsPostProcessor).Assembly },
    _ => { });

/// Посмотрим, какие пост-процессоры были зарегистрированы
#region Печать зарегстрированных сервисов...
void PrintServices(IServiceCollection services)
{
    foreach (var descriptor in services.Where(s => s.ServiceType.FullName.Contains("Post")))
    {
        Console.WriteLine($"{descriptor.ServiceType} -> {descriptor.ImplementationType}");
    }
}

Console.WriteLine("1. Container for staightforward implementation:");
PrintServices(straightForwardServices);

Console.WriteLine();
Console.WriteLine("2. Container for explicit implementation:");
PrintServices(explicitServices);
#endregion
#region output
/*
1. Container for staightforward implementation:
MediatR.Pipeline.IRequestPostProcessor`2[WtfMediatr.Core.IChangeBudgetCommand,MediatR.Unit] 
-> WtfMediatr.StraightForwardImplementation.ChangeBudgetCommandsPostProcessor

2. Container for explicit implementation:
MediatR.Pipeline.IRequestPostProcessor`2[WtfMediatr.Core.IChangeBudgetCommand,MediatR.Unit] 
-> WtfMediatr.ExplicitIncomesTracking.ChangeBudgetCommandsPostProcessor
MediatR.Pipeline.IRequestPostProcessor`2[WtfMediatr.Core.ProcessIncomeCommand,MediatR.Unit] 
-> WtfMediatr.ExplicitIncomesTracking.ChangeBudgetCommandsPostProcessor
MediatR.Pipeline.IRequestPostProcessor`2[WtfMediatr.Core.ProcessIncomeCommand,MediatR.Unit] 
-> WtfMediatr.ExplicitIncomesTracking.ProcessIncomeCommandPostProcessor
 */

/// Обратите внимание, во втором случае ChangeBudgetCommandsPostProcessor был зарегистрирован несколько раз,
/// как IRequestPostProcessor<ProcessIncomeCommand> и как IRequestPostProcessor<IChangeBudgetCommand>
/// и это не смотря на то, что мы не объявляли это явно!
/// 
/// будучи зарегистрированным как постобработчик для конкретной команды, он естественным образом попал в пайплайн, собранный MediatRом для обработки ProcessIncomeCommand
#endregion


