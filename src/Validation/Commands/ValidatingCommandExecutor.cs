using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using PurplePiranha.Cqrs.Core.Commands;
using PurplePiranha.Cqrs.Validation.Validators;
using PurplePiranha.FluentResults.Results;
using PurplePiranha.Cqrs.Core.Reflection;
using ValidationFailure = PurplePiranha.Cqrs.Validation.Failures.ValidationFailure;

namespace PurplePiranha.Cqrs.Validation.Commands;

public class ValidatingCommandExecutor : ICommandExecutor
{
    private readonly ICommandExecutor _commandExecutor;
    private readonly IValidatorExecutor _validatorExecutor;
    private readonly ILogger<ValidatingCommandExecutor> _logger;


    public ValidatingCommandExecutor(
        ICommandExecutor commandExecutor,
        IValidatorExecutor validatorExecutor,
        ILogger<ValidatingCommandExecutor> logger
        )
    {
        _commandExecutor = commandExecutor;
        _validatorExecutor = validatorExecutor;
        _logger = logger;
    }

    public async Task<Result> ExecuteAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default) where TCommand : ICommand
    {
        _logger.LogInformation("Executing command (with validation): {CommandName}", command.GetType().Name);
        return await PerformExecutionAsync<TCommand>(command, cancellationToken);
    }

    public async Task<Result<TResult>> ExecuteAsync<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing command (with validation): {CommandName}", command.GetType().Name);
        return await CallPerformExecuteTAsync<TResult>(command, cancellationToken);
    }

    private async Task<Result<TResult>> CallPerformExecuteTAsync<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default)
    {
        var commandType = command.GetType();
        var resultType = typeof(TResult);

        return await GenericMethodInvoker.InvokeAsync<Result<TResult>>(
                typeof(ValidatingCommandExecutor), this, nameof(PerformExecutionTAsync), new[] { commandType, resultType }, command, cancellationToken);
    }

    private async Task<ValidationResult> CallPerformValidatationAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default) where TCommand : notnull
    {
        var queryType = command.GetType();

        return await GenericMethodInvoker.InvokeAsync<ValidationResult>(
                typeof(ValidatingCommandExecutor), this, nameof(PerformValidatationAsync), new[] { queryType }, command, cancellationToken);
    }

    private async Task<Result<TResult>> PerformExecutionTAsync<TCommand, TResult>(TCommand query, CancellationToken cancellationToken = default) where TCommand : ICommand<TResult>
    {
        if (query is IValidationRequired)
        {
            var validationResult = await CallPerformValidatationAsync(query, cancellationToken);

            if (!validationResult.IsValid)
                return Result.FailureResult(ValidationFailure.CreateForCommand<TCommand, TResult>(validationResult));
        }

        return await _commandExecutor.ExecuteAsync(query, cancellationToken);
    }

    private async Task<Result> PerformExecutionAsync<TCommand>(TCommand query, CancellationToken cancellationToken = default) where TCommand : ICommand
    {
        if (query is IValidationRequired)
        {
            var validationResult = await CallPerformValidatationAsync(query, cancellationToken);

            if (!validationResult.IsValid)
                return Result.FailureResult(ValidationFailure.CreateForCommand<TCommand>(validationResult));
        }            

        return await _commandExecutor.ExecuteAsync(query, cancellationToken);
    }

    /// <summary>
    /// Performs the validation
    /// </summary>
    /// <typeparam name="TCommand">The type of the query.</typeparam>
    /// <param name="query">The query.</param>
    /// <returns></returns>
    private async Task<ValidationResult> PerformValidatationAsync<TCommand>(TCommand query, CancellationToken cancellationToken = default) where TCommand : IValidationRequired
    {
        return await _validatorExecutor.ExecuteAsync(query, cancellationToken);
    }
}
