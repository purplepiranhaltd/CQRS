using Microsoft.Extensions.DependencyInjection;
using PurplePiranha.Cqrs.Core.Commands;
using PurplePiranha.Cqrs.Core.ServiceRegistration;
using PurplePiranha.Cqrs.Core.Tests.TestClasses.Commands;

namespace PurplePiranha.Cqrs.Core.Tests;

public class HandlerRegistrarUnitTests
{
    [Test]
    public void RegisterHandlers_RegistersHandlersForMatchingGenericInterface()
    {
        var services = new ServiceCollection();
        new HandlerRegistrar(new[] { typeof(ICommandHandler<>) }).RegisterHandlers(services);

        Assert.That(
            services.Any(x => x.ServiceType == typeof(ICommandHandler<TestCommand>)
                && x.ImplementationType == typeof(TestCommandHandler)),
            Is.True);
    }

    [Test]
    public void RegisterHandlers_IgnoresInterfacesThatOnlyShareANameWithAHandlerInterface()
    {
        var services = new ServiceCollection();
        new HandlerRegistrar(new[] { typeof(ICommandHandler<>) }).RegisterHandlers(services);

        Assert.That(services.Any(x => x.ServiceType == typeof(Lookalike.ICommandHandler<LookalikeCommand>)), Is.False);
        Assert.That(
            services.Any(x => x.ServiceType == typeof(ICommandHandler<LookalikeCommand>)
                && x.ImplementationType == typeof(LookalikeHandler)),
            Is.True);
    }
}
