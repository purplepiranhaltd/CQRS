using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using PurplePiranha.Cqrs.Core.Queries;
using PurplePiranha.Cqrs.Validation.Validators;
using PurplePiranha.FluentResults.Results;
using PurplePiranha.Cqrs.Core.Reflection;
using ValidationFailure = PurplePiranha.Cqrs.Validation.Failures.ValidationFailure;

namespace PurplePiranha.Cqrs.Validation.Queries;

public class ValidatingQueryExecutor : IQueryExecutor
{

    private readonly IQueryExecutor _queryExecutor;
    private readonly IValidatorExecutor _validatorExecutor;
    private readonly ILogger<ValidatingQueryExecutor> _logger;


    public ValidatingQueryExecutor(
        IQueryExecutor queryExecutor,
        IValidatorExecutor validatorExecutor,
        ILogger<ValidatingQueryExecutor> logger)
    {
        _queryExecutor = queryExecutor;
        _validatorExecutor = validatorExecutor;
        _logger = logger;
    }

    public async Task<Result<TResult>> ExecuteAsync<TResult>(IQuery<TResult> query)
    {
        _logger.LogInformation("Executing query (with validation): {QueryName}", query.GetType().Name);
        return await CallPerformExecuteAsync<TResult>(query);
    }

    private async Task<Result<TResult>> CallPerformExecuteAsync<TResult>(IQuery<TResult> query)
    {
        var queryType = query.GetType();
        var resultType = typeof(TResult);

        return await GenericMethodInvoker.InvokeAsync<Result<TResult>>(
                typeof(ValidatingQueryExecutor), this, nameof(PerformExecutionAsync), new[] { queryType, resultType }, query);
    }

    

    private async Task<ValidationResult> CallPerformValidatationAsync<TQuery>(TQuery query) where TQuery : notnull
    {
        var queryType = query.GetType();

        return await GenericMethodInvoker.InvokeAsync<ValidationResult>(
                typeof(ValidatingQueryExecutor), this, nameof(PerformValidatationAsync), new[] { queryType }, query);
    }

    private async Task<Result<TResult>> PerformExecutionAsync<TQuery, TResult>(TQuery query) where TQuery : IQuery<TResult>
    {
        if (query is IValidationRequired)
        {
            var validationResult = await CallPerformValidatationAsync(query);

            if (!validationResult.IsValid)
                return Result.FailureResult(ValidationFailure.CreateForQuery<TQuery, TResult>(validationResult));
        }          

        return await _queryExecutor.ExecuteAsync(query);
    }

    /// <summary>
    /// Performs the validation
    /// </summary>
    /// <typeparam name="TQuery">The type of the query.</typeparam>
    /// <param name="query">The query.</param>
    /// <returns></returns>
    private async Task<ValidationResult> PerformValidatationAsync<TQuery>(TQuery query) where TQuery : IValidationRequired
    {
        return await _validatorExecutor.ExecuteAsync(query);
    }
}
