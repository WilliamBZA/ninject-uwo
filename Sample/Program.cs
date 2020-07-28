using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using MediatR.Ninject;
using NServiceBus;
using NServiceBus.ObjectBuilder.Ninject;
using Ninject.Extensions.Conventions;
using Ninject.Extensions.NamedScope;
using Ninject.Extensions.ContextPreservation;
using Ninject.Planning.Bindings.Resolvers;
using Ninject;
using Ninject.Extensions.Conventions.BindingGenerators;
using Ninject.Syntax;
using Ninject.Activation;

class Program
{
    static async Task Main()
    {
        Console.Title = "Samples.UnitOfWork";
        var endpointConfiguration = new EndpointConfiguration("Samples.UnitOfWork");
        endpointConfiguration.UsePersistence<LearningPersistence>();
        endpointConfiguration.UseTransport<LearningTransport>();

        Ninject.StandardKernel kernel = new Ninject.StandardKernel();

        kernel.Bind<ITest>().To<Test>().InUnitOfWorkScope();
        kernel.Bind<IMediator>().To<Mediator>().InUnitOfWorkScope();
        kernel.Bind<ServiceFactory>().ToMethod(ctx => t => ctx.ContextPreservingGet(t)).InUnitOfWorkScope();

        kernel.Bind<INotificationHandler<Meow>>().To<MSub>().InUnitOfWorkScope();

        endpointConfiguration.UseContainer<NinjectBuilder>(c =>
        {
            c.ExistingKernel(kernel);
        });

        var recoverability = endpointConfiguration.Recoverability();
        recoverability.Immediate(
            customizations: immediate =>
            {
                immediate.NumberOfRetries(0);
            });
        recoverability.Delayed(
            customizations: delayed =>
            {
                delayed.NumberOfRetries(0);
            });

        #region ComponentRegistration

        endpointConfiguration.RegisterComponents(
            registration: components =>
            {
                components.ConfigureComponent<CustomManageUnitOfWork>(DependencyLifecycle.InstancePerCall);
            });

        #endregion

        var endpointInstance = await Endpoint.Start(endpointConfiguration)
            .ConfigureAwait(false);
        await Runner.Run(endpointInstance)
            .ConfigureAwait(false);
        await endpointInstance.Stop()
            .ConfigureAwait(false);
    }
}

public interface ITest { }
public class Test : ITest { public string Id = Guid.NewGuid().ToString(); }

public class MSub : INotificationHandler<Meow>
{
    public MSub(ITest t)
    {
        Test = t;
    }

    public ITest Test { get; }

    public Task Handle(Meow notification, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

public class ServiceFactoryProvider : Provider<object>
{
    private IResolutionRoot _root;

    public ServiceFactoryProvider(IResolutionRoot  root)
    {
        _root = root;
    }

    protected override object CreateInstance(IContext context)
    {
        return (ServiceFactory)(t =>
        {
            return _root.Get(t, new NamedScopeParameter("NinjectObjectBuilder"));
        });
    }
}