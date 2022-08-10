using System.Reflection;
using MediatR;
using MediatR.Pipeline;
using MediatR.Registration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace WtfMediatr.Investigations;

/// За счет чего во втором случае мы получили нужную регистрацию? Заглянем "под капот" метода AddMediatR()
internal static class TheMediatorSources
{
    public static IServiceCollection AddMediatR(
        this IServiceCollection services, 
        IEnumerable<Assembly> assemblies, 
        Action<MediatRServiceConfiguration>? configuration)
    {
        #region валидация параметров и прочая скука смертная)
        if (!assemblies.Any())
        {
            throw new ArgumentException("No assemblies found to scan. Supply at least one assembly to scan for handlers.");
        }
        var serviceConfig = new MediatRServiceConfiguration();

        configuration?.Invoke(serviceConfig);
        #endregion

        /// ServiceRegistrar - класс из MediatR.Registration.
        /// Данный метод регистрирует базовые вещи вроде IMediator, IPipelineBehavior и нам неинтересен
        ServiceRegistrar.AddRequiredServices(services, serviceConfig);

        /// А вот этот метод рассмотрим подробнее
        #region ServiceRegistrar.AddMediatRClasses(services, assemblies, serviceConfig);
        var assembliesToScan = assemblies.Distinct().ToArray();

        #region unrelated
        ConnectImplementationsToTypesClosing(typeof(IRequestHandler<,>), services, assembliesToScan, false, serviceConfig);
        ConnectImplementationsToTypesClosing(typeof(INotificationHandler<>), services, assembliesToScan, true, serviceConfig);
        ConnectImplementationsToTypesClosing(typeof(IStreamRequestHandler<,>), services, assembliesToScan, false, serviceConfig);
        ConnectImplementationsToTypesClosing(typeof(IRequestPreProcessor<>), services, assembliesToScan, true, serviceConfig);
        #endregion

        /* -> */ ConnectImplementationsToTypesClosing(typeof(IRequestPostProcessor<,>), services, assembliesToScan, true, serviceConfig);

        #region unrelated
        ConnectImplementationsToTypesClosing(typeof(IRequestExceptionHandler<,,>), services, assembliesToScan, true, serviceConfig);
        ConnectImplementationsToTypesClosing(typeof(IRequestExceptionAction<,>), services, assembliesToScan, true, serviceConfig);

        var multiOpenInterfaces = new[]
        {
            typeof(INotificationHandler<>),
            typeof(IRequestPreProcessor<>),
            typeof(IRequestPostProcessor<,>),
            typeof(IRequestExceptionHandler<,,>),
            typeof(IRequestExceptionAction<,>)
        };

        foreach (var multiOpenInterface in multiOpenInterfaces)
        {
            var arity = multiOpenInterface.GetGenericArguments().Length;

            var concretions = assembliesToScan
                .SelectMany(a => a.DefinedTypes)
                .Where(type => type.FindInterfacesThatClose(multiOpenInterface).Any())
                .Where(type => type.IsConcrete() && type.IsOpenGeneric())
                .Where(type => type.GetGenericArguments().Length == arity)
                .Where(serviceConfig.TypeEvaluator)
                .ToList();

            foreach (var type in concretions)
            {
                services.AddTransient(multiOpenInterface, type);
            }
        }
        #endregion
        #endregion

        return services;
    }

    private static void ConnectImplementationsToTypesClosing(Type openRequestInterface,
        IServiceCollection services,
        IEnumerable<Assembly> assembliesToScan,
        bool addIfAlreadyExists,
        MediatRServiceConfiguration configuration)
    {
        
        var concretions = new List<Type>(); /// типы-реализации
        var interfaces = new List<Type>(); /// интерфейсы
        #region тривиальное сканирование сборки
        foreach (var type in assembliesToScan.SelectMany(a => a.DefinedTypes).Where(t => !t.IsOpenGeneric()).Where(configuration.TypeEvaluator))
        {
            var interfaceTypes = type.FindInterfacesThatClose(openRequestInterface).ToArray();
            if (!interfaceTypes.Any()) continue;

            if (type.IsConcrete())
            {
                concretions.Add(type);
            }

            foreach (var interfaceType in interfaceTypes)
            {
                interfaces.Fill(interfaceType);
            }
        }
        #endregion

        /// а вот и место, в котором происходит "дополнительная" регистрация
        foreach (var @interface in interfaces)
        {
            /// благодаря регистрации ProcessIncomeCommandPostProcessor
            /// у нас появился интерфейс IRequestPostProcessor<ProcessIncomeCommand>
            /// к которому, в свою очередь, можно преобразовать ChangeBudgetCommandsPostProcessor
            var exactMatches = concretions.Where(x => x.CanBeCastTo(@interface)).ToList();
            if (addIfAlreadyExists)                            //     /\
            {                                                  //     ||
                foreach (var type in exactMatches)             //     ||
                {
                    services.AddTransient(@interface, type);
                }
            }
            #region остальной код, необходимый для компиляции
            else
            {
                if (exactMatches.Count > 1)
                {
                    exactMatches.RemoveAll(m => !IsMatchingWithInterface(m, @interface));
                }

                foreach (var type in exactMatches)
                {
                    services.TryAddTransient(@interface, type);
                }
            }


            if (!@interface.IsOpenGeneric())
            {
                AddConcretionsThatCouldBeClosed(@interface, concretions, services);
            }
            #endregion
        }
    }

    #region Любопытные методы, позволяющие с помощью рефлексии отыскать имплементации абстракций MediartR
    private static bool IsMatchingWithInterface(Type handlerType, Type handlerInterface)
    {
        if (handlerType == null || handlerInterface == null)
        {
            return false;
        }

        if (handlerType.IsInterface)
        {
            if (handlerType.GenericTypeArguments.SequenceEqual(handlerInterface.GenericTypeArguments))
            {
                return true;
            }
        }
        else
        {
            return IsMatchingWithInterface(handlerType.GetInterface(handlerInterface.Name), handlerInterface);
        }

        return false;
    }

    private static void AddConcretionsThatCouldBeClosed(Type @interface, List<Type> concretions, IServiceCollection services)
    {
        foreach (var type in concretions
                     .Where(x => x.IsOpenGeneric() && x.CouldCloseTo(@interface)))
        {
            try
            {
                services.TryAddTransient(@interface, type.MakeGenericType(@interface.GenericTypeArguments));
            }
            catch (Exception)
            {
            }
        }
    }

    private static bool CouldCloseTo(this Type openConcretion, Type closedInterface)
    {
        var openInterface = closedInterface.GetGenericTypeDefinition();
        var arguments = closedInterface.GenericTypeArguments;

        var concreteArguments = openConcretion.GenericTypeArguments;
        return arguments.Length == concreteArguments.Length && openConcretion.CanBeCastTo(openInterface);
    }

    private static bool CanBeCastTo(this Type pluggedType, Type pluginType)
    {
        if (pluggedType == null) return false;

        if (pluggedType == pluginType) return true;

        return pluginType.GetTypeInfo().IsAssignableFrom(pluggedType.GetTypeInfo());
    }

    private static bool IsOpenGeneric(this Type type)
    {
        return type.GetTypeInfo().IsGenericTypeDefinition || type.GetTypeInfo().ContainsGenericParameters;
    }

    private static IEnumerable<Type> FindInterfacesThatClose(this Type pluggedType, Type templateType)
    {
        return FindInterfacesThatClosesCore(pluggedType, templateType).Distinct();
    }

    private static IEnumerable<Type> FindInterfacesThatClosesCore(Type pluggedType, Type templateType)
    {
        if (pluggedType == null) yield break;

        if (!pluggedType.IsConcrete()) yield break;

        if (templateType.GetTypeInfo().IsInterface)
        {
            foreach (
                var interfaceType in
                pluggedType.GetInterfaces()
                    .Where(type => type.GetTypeInfo().IsGenericType && (type.GetGenericTypeDefinition() == templateType)))
            {
                yield return interfaceType;
            }
        }
        else if (pluggedType.GetTypeInfo().BaseType.GetTypeInfo().IsGenericType &&
                 (pluggedType.GetTypeInfo().BaseType.GetGenericTypeDefinition() == templateType))
        {
            yield return pluggedType.GetTypeInfo().BaseType;
        }

        if (pluggedType.GetTypeInfo().BaseType == typeof(object)) yield break;

        foreach (var interfaceType in FindInterfacesThatClosesCore(pluggedType.GetTypeInfo().BaseType, templateType))
        {
            yield return interfaceType;
        }
    }

    private static bool IsConcrete(this Type type)
    {
        return !type.GetTypeInfo().IsAbstract && !type.GetTypeInfo().IsInterface;
    }

    private static void Fill<T>(this IList<T> list, T value)
    {
        if (list.Contains(value)) return;
        list.Add(value);
    }
    #endregion
}

/// Таким образом, ПостПроцессор, логирующий транзакции был явно зарегистрирован благодаря наличию еще одного пост-процессора для того же типа.
/// Можем ли мы доработать этот код так, чтобы регистрировать ChangeBudgetCommandsPostProcessor для всех команд, меняющих баланс кошелька?