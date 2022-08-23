using System.Reflection;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace WtfMediatr.NediatR;

internal static class NediatR
{
    public static IServiceCollection EmpowerMediatRHandlersFor(this IServiceCollection collection, Type openType)
    {
        var knownRequestTypes = GetDeclaredTypes();

        var matchedDescriptors = collection
            .Where(d => CheckInterfaceMatch(openType, d))
            .ToList();
        foreach (var descriptor in matchedDescriptors)
        {
            var superInterfaces = BuildSuperInterfaces(descriptor.ServiceType, knownRequestTypes);
            foreach (var superInterface in superInterfaces)
            {
                var newDescriptor = RecreateDescriptor(descriptor, superInterface);
                collection.TryAdd(newDescriptor);
            }
        }

        return collection;
    }

    private static ServiceDescriptor RecreateDescriptor(ServiceDescriptor descriptor, Type superInterface)
    {
        return descriptor.ImplementationInstance != null
            ? new ServiceDescriptor(superInterface, descriptor.ImplementationInstance)
            : descriptor.ImplementationFactory != null
                ? new ServiceDescriptor(superInterface, descriptor.ImplementationFactory, descriptor.Lifetime)
                : new ServiceDescriptor(superInterface, descriptor.ImplementationType!, descriptor.Lifetime);
    }

    private static bool CheckInterfaceMatch(Type openType, ServiceDescriptor d)
    {
        return d.ServiceType.IsInterface
               && d.ServiceType.IsGenericType
               && d.ServiceType.GetGenericTypeDefinition() == openType;
    }

    private static List<TypeInfo> GetDeclaredTypes()
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.DefinedTypes)
            .ToList();
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