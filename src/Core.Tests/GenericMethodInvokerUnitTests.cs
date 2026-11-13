using PurplePiranha.Cqrs.Core.Reflection;

namespace PurplePiranha.Cqrs.Core.Tests;

public class GenericMethodInvokerUnitTests
{
    private class Target
    {
        private Task<string> Describe<T>(T value) => Task.FromResult($"{typeof(T).Name}:{value}");

        protected virtual Task<string> Overridable<T>(T value) => Task.FromResult($"base:{typeof(T).Name}");

        private Task<string> Throws<T>() => throw new InvalidOperationException("boom");
    }

    private class DerivedTarget : Target
    {
        protected override Task<string> Overridable<T>(T value) => Task.FromResult($"derived:{typeof(T).Name}");
    }

    [Test]
    public async Task InvokeAsync_ClosesMethodOverRuntimeTypeAndReturnsResult()
    {
        var result = await GenericMethodInvoker.InvokeAsync<string>(
            typeof(Target), new Target(), "Describe", new[] { typeof(int) }, 5);

        Assert.That(result, Is.EqualTo("Int32:5"));
    }

    [Test]
    public async Task InvokeAsync_CalledRepeatedlyWithDifferentTypes_UsesTheCorrectClosedMethod()
    {
        var target = new Target();

        var first = await GenericMethodInvoker.InvokeAsync<string>(typeof(Target), target, "Describe", new[] { typeof(int) }, 1);
        var second = await GenericMethodInvoker.InvokeAsync<string>(typeof(Target), target, "Describe", new[] { typeof(string) }, "a");
        var third = await GenericMethodInvoker.InvokeAsync<string>(typeof(Target), target, "Describe", new[] { typeof(int) }, 2);

        Assert.That(new[] { first, second, third }, Is.EqualTo(new[] { "Int32:1", "String:a", "Int32:2" }));
    }

    [Test]
    public async Task InvokeAsync_DispatchesToOverrideOnDerivedInstance()
    {
        var result = await GenericMethodInvoker.InvokeAsync<string>(
            typeof(Target), new DerivedTarget(), "Overridable", new[] { typeof(int) }, 1);

        Assert.That(result, Is.EqualTo("derived:Int32"));
    }

    [Test]
    public void InvokeAsync_MethodThrowsSynchronously_ThrowsOriginalExceptionNotTargetInvocationException()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            GenericMethodInvoker.InvokeAsync<string>(typeof(Target), new Target(), "Throws", new[] { typeof(int) }));

        Assert.That(ex!.Message, Is.EqualTo("boom"));
    }

    [Test]
    public void InvokeAsync_MethodDoesNotExist_ThrowsMissingMethodException()
    {
        Assert.Throws<MissingMethodException>(() =>
            GenericMethodInvoker.InvokeAsync<string>(typeof(Target), new Target(), "DoesNotExist", new[] { typeof(int) }));
    }
}
