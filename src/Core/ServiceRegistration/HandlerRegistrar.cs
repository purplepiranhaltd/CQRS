using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace PurplePiranha.Cqrs.Core.ServiceRegistration;

/// <summary>
/// Registers handlers with the service collection
/// </summary>
public class HandlerRegistrar
{
    private readonly Type[] _handlerTypes;

    public HandlerRegistrar(Type[] handlerTypes)
    {
        _handlerTypes = handlerTypes;
    }

    /// <summary>
    /// Registers the handlers with the service collection.
    /// </summary>
    /// <param name="services">The services.</param>
    public void RegisterHandlers(IServiceCollection services)
    {
        var implementationTypes = GetCommandHandlerTypes();
        foreach (var implementationType in implementationTypes)
        {
            var serviceTypes = GetCommandHandlerInterfaceType(implementationType);

            foreach(var serviceType in serviceTypes)
                services.AddTransient(serviceType, implementationType);
        }
    }

    /// <summary>
    /// Gets the command handler types implementation types.
    /// </summary>
    /// <returns></returns>
    private IEnumerable<Type> GetCommandHandlerTypes()
    {
        var types = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(GetLoadableTypes)
            .Where(x =>
            {
                if (x.IsInterface || x.IsAbstract || x.IsGenericTypeDefinition) return false;
                foreach (var t in _handlerTypes)
                {
                    if (t.IsAssignableFrom(x)) return true;
                }

                if (x.GetInterfaces().Any(i =>
                    i.IsGenericType && _handlerTypes.Contains(i.GetGenericTypeDefinition())
                    )
                ) return true;
                return false;
            });
        return types;
    }

    /// <summary>
    /// Gets the types from an assembly, tolerating assemblies that can't be fully loaded
    /// (e.g. missing optional dependencies) or that are dynamic.
    /// </summary>
    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        if (assembly.IsDynamic)
            return Enumerable.Empty<Type>();

        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            // Some types failed to load; use the ones that did.
            return ex.Types.Where(t => t is not null)!;
        }
        catch (Exception ex) when (ex is NotSupportedException or FileNotFoundException or FileLoadException or BadImageFormatException)
        {
            return Enumerable.Empty<Type>();
        }
    }

    /// <summary>
    /// Gets the type of the command handler service interface for a specified implementation type.
    /// </summary>
    /// <param name="commandHandlerType">Command handler implementation type</param>
    /// <returns>Command handler service interface type</returns>
    private IEnumerable<Type> GetCommandHandlerInterfaceType(Type commandHandlerType)
    {
        return commandHandlerType.FindInterfaces(filter, _handlerTypes).AsEnumerable();
    }

    private static TypeFilter filter = new TypeFilter((typeObj, criteriaObj) =>
    {
        if (criteriaObj is null)
            return false;

        // Match on the generic type definition (not just the name) so that unrelated
        // interfaces which happen to share a name are not registered.
        var types = (Type[])criteriaObj;
        return types.Contains(typeObj.IsGenericType ? typeObj.GetGenericTypeDefinition() : typeObj);
    });
}