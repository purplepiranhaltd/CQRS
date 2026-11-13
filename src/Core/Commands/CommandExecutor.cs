using Microsoft.Extensions.Logging;
using PurplePiranha.Cqrs.Core.Failures;
using PurplePiranha.FluentResults.Results;
using PurplePiranha.Cqrs.Core.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace PurplePiranha.Cqrs.Core.Commands;

/// <summary>
/// Executes a command via the correct handler.
/// </summary>
/// <seealso cref="ICommandExecutor" />
public class CommandExecutor : ICommandExecutor
{
    private readonly ICommandHandlerFactory _commandHandlerFactory;
    private readonly ILogger<CommandExecutor> _logger;


    public CommandExecutor(ICommandHandlerFactory commandHandlerFactory, ILogger<CommandExecutor> logger)
    {
        _commandHandlerFactory = commandHandlerFactory;
        _logger = logger;
    }

    /// <summary>
    /// Executes the command.
    /// </summary>
    /// <typeparam name="TCommand">The type of the command.</typeparam>
    /// <param name="command">The command.</param>
    /// <returns></returns>
    public virtual async Task<Result> ExecuteAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default) where TCommand : ICommand
    {
        _logger.LogInformation("Executing command: {CommandName}", command.GetType().Name);

        try
        {
            var handler = _commandHandlerFactory.CreateHandler<TCommand>();
            return await handler.ExecuteAsync(command, cancellationToken);
        }
        catch (CommandHandlerNotImplementedException)
        {
            return await Task.FromResult(Result.FailureResult(new CommandHandlerNotImplementedFailure(typeof(TCommand).Name)));
        }
    }

    /// <summary>
    /// Executes the command.
    /// </summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="command">The command.</param>
    /// <returns></returns>
    public async Task<Result<TResult>> ExecuteAsync<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing command: {CommandName}", command.GetType().Name);

        // We make a dynamic call to the generic method via reflection,
        // otherwise the return type would have to be specified on every call

        var commandType = command.GetType();
        var resultType = typeof(TResult);

        return await GenericMethodInvoker.InvokeAsync<Result<TResult>>(
                typeof(CommandExecutor), this, nameof(ExecuteCommandAsync), new[] { commandType, resultType }, command, cancellationToken);
    }

    /// <summary>
    /// Executes the command.
    /// </summary>
    /// <typeparam name="TCommand">The type of the command.</typeparam>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="command">The command.</param>
    /// <returns></returns>
    protected virtual async Task<Result<TResult>> ExecuteCommandAsync<TCommand, TResult>(TCommand command, CancellationToken cancellationToken = default) where TCommand : ICommand<TResult>
    {
        try
        {
            var handler = _commandHandlerFactory.CreateHandler<TCommand, TResult>();
            return await handler.ExecuteAsync(command, cancellationToken);
        }
        catch (CommandHandlerNotImplementedException)
        {
            return await Task.FromResult(Result.FailureResult<TResult>(new CommandHandlerNotImplementedFailure(typeof(TCommand).Name)));
        }
    }
}