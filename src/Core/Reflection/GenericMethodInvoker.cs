using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace PurplePiranha.Cqrs.Core.Reflection;

/// <summary>
/// Invokes a non-public generic instance method, closed over runtime types, via reflection.
/// </summary>
/// <remarks>
/// The executors need to dispatch on the runtime type of a command or query so that the
/// correct handler is resolved without the caller having to specify every type argument.
/// This centralises the lookup, caching of the closed method and exception unwrapping.
/// </remarks>
internal static class GenericMethodInvoker
{
    private static readonly ConcurrentDictionary<MethodKey, MethodInfo> ClosedMethods = new();

    /// <summary>
    /// Invokes the generic method <paramref name="methodName"/> declared on
    /// <paramref name="declaringType"/> against <paramref name="target"/>.
    /// </summary>
    /// <typeparam name="TResult">The type the method's task produces.</typeparam>
    /// <param name="declaringType">The type that declares the method. Using this (rather than the
    /// target's runtime type) means overrides in derived classes are still dispatched correctly.</param>
    /// <param name="target">The instance to invoke the method on.</param>
    /// <param name="methodName">The name of the non-public instance method.</param>
    /// <param name="typeArguments">The type arguments to close the method over.</param>
    /// <param name="arguments">The arguments to pass to the method.</param>
    public static Task<TResult> InvokeAsync<TResult>(
        Type declaringType,
        object target,
        string methodName,
        Type[] typeArguments,
        params object?[] arguments)
    {
        var method = ClosedMethods.GetOrAdd(
            new MethodKey(declaringType, methodName, typeArguments),
            static key =>
            {
                var definition = key.DeclaringType.GetMethod(
                    key.MethodName,
                    BindingFlags.NonPublic | BindingFlags.Instance)
                    ?? throw new MissingMethodException(key.DeclaringType.FullName, key.MethodName);

                return definition.MakeGenericMethod(key.TypeArguments);
            });

        try
        {
            return (Task<TResult>)method.Invoke(target, arguments)!;
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            // Rethrow the original exception, preserving its stack trace.
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw; // unreachable - Throw() always throws
        }
    }

    private readonly struct MethodKey : IEquatable<MethodKey>
    {
        public MethodKey(Type declaringType, string methodName, Type[] typeArguments)
        {
            DeclaringType = declaringType;
            MethodName = methodName;
            TypeArguments = typeArguments;
        }

        public Type DeclaringType { get; }
        public string MethodName { get; }
        public Type[] TypeArguments { get; }

        public bool Equals(MethodKey other) =>
            DeclaringType == other.DeclaringType
            && MethodName == other.MethodName
            && TypeArguments.AsSpan().SequenceEqual(other.TypeArguments);

        public override bool Equals(object? obj) => obj is MethodKey other && Equals(other);

        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(DeclaringType);
            hash.Add(MethodName);
            foreach (var type in TypeArguments)
                hash.Add(type);
            return hash.ToHashCode();
        }
    }
}
