using PurplePiranha.Cqrs.Core.Commands;
using PurplePiranha.Cqrs.Permissions.Abstractions;
using PurplePiranha.Cqrs.Permissions.Decorators;
using PurplePiranha.Cqrs.Permissions.Factories;
using PurplePiranha.FluentResults.Results;
using PurplePiranha.Cqrs.Core.Reflection;

namespace PurplePiranha.Cqrs.Permissions.Executors;

public class PermissionCheckingCommandExecutor : ICommandExecutor
{
    private readonly ICommandExecutor _commandExecutor;
    private readonly IPermissionCheckerExecutor _permissionCheckerExecutor;
    private readonly NotAuthorisedFailureFactory _notAuthorisedFailureFactory;

    public PermissionCheckingCommandExecutor(
        ICommandExecutor commandExecutor, 
        IPermissionCheckerExecutor permissionCheckerExecutor,
        NotAuthorisedFailureFactory notAuthorisedFailureFactory
        )
    {
        _commandExecutor = commandExecutor;
        _permissionCheckerExecutor = permissionCheckerExecutor;
        _notAuthorisedFailureFactory = notAuthorisedFailureFactory;
    }
    public async Task<Result> ExecuteAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default) where TCommand : ICommand
    {
        return await PerformExecutionAsync<TCommand>(command, cancellationToken);
    }

    public async Task<Result<TResult>> ExecuteAsync<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default)
    {
        return await CallPerformExecuteTAsync<TResult>(command, cancellationToken);
    }

    private async Task<Result<TResult>> CallPerformExecuteTAsync<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default)
    {
        var commandType = command.GetType();
        var resultType = typeof(TResult);

        return await GenericMethodInvoker.InvokeAsync<Result<TResult>>(
                typeof(PermissionCheckingCommandExecutor), this, nameof(PerformExecutionTAsync), new[] { commandType, resultType }, command, cancellationToken);
    }

    private async Task<bool> CallPerformPermissionCheckingAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default) where TCommand : notnull
    {
        var commandType = command.GetType();

        return await GenericMethodInvoker.InvokeAsync<bool>(
                typeof(PermissionCheckingCommandExecutor), this, nameof(PerformPermissionCheckingAsync), new[] { commandType }, command, cancellationToken);
    }

    private async Task<Result<TResult>> PerformExecutionTAsync<TCommand, TResult>(TCommand command, CancellationToken cancellationToken = default) where TCommand : ICommand<TResult>
    {
        if (command is IPermissionRequired)
        {
            var hasPermission = await CallPerformPermissionCheckingAsync(command, cancellationToken);

            if (!hasPermission)
                return Result.FailureResult<TResult>(_notAuthorisedFailureFactory.GetNotAuthorisedFailure());
        }

        return await _commandExecutor.ExecuteAsync(command, cancellationToken);
    }

    private async Task<Result> PerformExecutionAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default) where TCommand: ICommand
    {
        if (command is IPermissionRequired)
        {
            var hasPermission = await CallPerformPermissionCheckingAsync(command, cancellationToken);

            if (!hasPermission)
                return Result.FailureResult(_notAuthorisedFailureFactory.GetNotAuthorisedFailure());
        }

        return await _commandExecutor.ExecuteAsync(command, cancellationToken);
    }

    private async Task<bool> PerformPermissionCheckingAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default) where TCommand : IPermissionRequired
    {
        return await _permissionCheckerExecutor.ExecuteAsync(command, cancellationToken);
    }

}
