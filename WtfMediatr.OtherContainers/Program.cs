using System.Net.NetworkInformation;
using Autofac;
using Autofac.Features.Variance;
using MediatR;
using MediatR.Pipeline;
using StructureMap;
using WtfMediatr.Core;
using WtfMediatr.StraightForwardImplementation;

var structureMap = new Container(cfg =>
{
    cfg.Scan(scanner =>
    {
        scanner.AssemblyContainingType<Wallet>();
        scanner.AssemblyContainingType<ChangeBudgetCommandsPostProcessor>(); // Our assembly with requests & handlers
        scanner.WithDefaultConventions();
        scanner.ConnectImplementationsToTypesClosing(typeof(IRequestHandler<,>));
        scanner.ConnectImplementationsToTypesClosing(typeof(INotificationHandler<>));
        scanner.ConnectImplementationsToTypesClosing(typeof(IRequestPreProcessor<>));
        scanner.ConnectImplementationsToTypesClosing(typeof(IRequestPostProcessor<,>));
    });
    cfg.For<ServiceFactory>().Use<ServiceFactory>(ctx => ctx.GetInstance);
    cfg.For<IMediator>().Use<Mediator>();
});

var service = structureMap.GetInstance<SampleHostedService>();

Console.WriteLine("SctructureMap: ");
await service.StartAsync(CancellationToken.None);
await service.StopAsync(CancellationToken.None);

var autofacBuider =  new ContainerBuilder();
autofacBuider.RegisterSource(new ContravariantRegistrationSource());
autofacBuider
    .RegisterType<Mediator>()
    .As<IMediator>()
    .InstancePerLifetimeScope();

autofacBuider.Register<ServiceFactory>(context =>
{
    var c = context.Resolve<IComponentContext>();
    return t => c.Resolve(t);
});

autofacBuider.RegisterInstance(new Wallet());
autofacBuider.RegisterInstance(new TransactionLog());
autofacBuider
    .RegisterAssemblyTypes(typeof(ChangeBudgetCommandsPostProcessor).Assembly)
    .AsImplementedInterfaces();

var autofacContainer = autofacBuider.Build();
service = autofacContainer.Resolve<SampleHostedService>();

Console.WriteLine("Autofac: ");
await service.StartAsync(CancellationToken.None);
await service.StopAsync(CancellationToken.None);
