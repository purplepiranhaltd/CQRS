using PurplePiranha.Cqrs.Permissions.Abstractions;
using PurplePiranha.Cqrs.Permissions.Decorators;

namespace PurplePiranha.Cqrs.Permissions.Executors;

public class PermissionCheckerExecutor : IPermissionCheckerExecutor
{
    private readonly IPermissionCheckerFactory _factory;

    public PermissionCheckerExecutor(IPermissionCheckerFactory factory) 
    {
        _factory = factory;
    }
    public async Task<bool> ExecuteAsync<T>(T obj, CancellationToken cancellationToken = default) where T : IPermissionRequired
    {
        //TODO: Should be be using the CancellationToken throughout the permission checking process?
        var handler = _factory.CreatePermissionChecker<T>();
        await handler.InitialiseAsync();
        return await handler.HasPermissionAsync(obj);
    }
}
