using System.Reflection;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace WtfMediatr.NediatR;

internal static class NediatR
{
    private static readonly Type RequestType = typeof(IRequest<>);

    /// учитывая что IRequestHandler, Pre и PostProcessorы объявлены как контрвариантные обобщенные типы, давайте поможем MediatRу стать еще мощнее! 
    /// ну и примите во внимание, что приведенный ниже код приведен исключительно для демонстрации идеи!
    public static IServiceCollection EmpowerMediatRHandlersFor(this IServiceCollection collection, Type openType)
    {
        var knownRequestTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.DefinedTypes)
            .Where(t => t.GetTypeInfo()
                .GetInterfaces()
                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == RequestType)
            )
            .Where(t => !t.IsInterface)
            .ToList();

        foreach (var descriptor in collection.Where(d =>
                     d.ServiceType.IsInterface
                     && d.ServiceType.IsGenericType
                     && d.ServiceType.GetGenericTypeDefinition() == openType)
                     .ToList())
        {
            var serviceType = descriptor.ServiceType;

            foreach (var superInterface in BuildSuperInterfaces(serviceType, knownRequestTypes))
            {
                var newDescriptor = descriptor.ImplementationInstance != null
                    ? new ServiceDescriptor(superInterface, descriptor.ImplementationInstance)
                    : descriptor.ImplementationFactory != null
                        ? new ServiceDescriptor(superInterface, descriptor.ImplementationFactory, descriptor.Lifetime)
                        : new ServiceDescriptor(superInterface, descriptor.ImplementationType!, descriptor.Lifetime);

                collection.TryAdd(newDescriptor);
            }
        }

        return collection;
    }

    private static IEnumerable<Type> BuildSuperInterfaces(Type serviceType, List<TypeInfo> knownRequestTypes)
    {
        var genericType = serviceType.GetGenericTypeDefinition();
        var arguments = serviceType.GetGenericArguments();
        foreach (var parameters in BuildParams(arguments, knownRequestTypes))
        {
            yield return genericType.MakeGenericType(parameters.ToArray());
        }
    }

    private static IEnumerable<IEnumerable<Type>> BuildParams(IEnumerable<Type> args, List<TypeInfo> knownRequestTypes)
    {
        var variable = args.FirstOrDefault();
        if (variable == null)
        {
            yield return Enumerable.Empty<Type>();
            yield break;
        }

        var options = GenerateOptions(variable, knownRequestTypes);

        foreach (var option in options)
        {
            foreach (var rest in BuildParams(args.Skip(1), knownRequestTypes))
            {
                yield return rest.Prepend(option);
            }
        }
    }

    private static IEnumerable<Type> GenerateOptions(Type variable, List<TypeInfo> knownRequestTypes)
    {
        foreach (var type in knownRequestTypes.Where(t => t.IsAssignableTo(variable)))
        {
            yield return type;
        }

        yield return variable;

    }
}