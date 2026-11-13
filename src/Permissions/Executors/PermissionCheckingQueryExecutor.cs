using PurplePiranha.Cqrs.Core.Queries;
using PurplePiranha.Cqrs.Permissions.Abstractions;
using PurplePiranha.Cqrs.Permissions.Decorators;
using PurplePiranha.Cqrs.Permissions.Factories;
using PurplePiranha.FluentResults.Results;
using PurplePiranha.Cqrs.Core.Reflection;

namespace PurplePiranha.Cqrs.Permissions.Executors;

public class PermissionCheckingQueryExecutor : IQueryExecutor
{
    #region Fields
    private readonly IQueryExecutor _queryExecutor;
    private readonly IPermissionCheckerExecutor _permissionCheckerExecutor;
    private readonly NotAuthorisedFailureFactory _notAuthorisedFailureFactory;
    #endregion

    #region Ctr
    public PermissionCheckingQueryExecutor(
        IQueryExecutor queryExecutor, 
        IPermissionCheckerExecutor permissionCheckerExecutor,
        NotAuthorisedFailureFactory notAuthorisedFailureFactory
        )
    {
        _queryExecutor = queryExecutor;
        _permissionCheckerExecutor = permissionCheckerExecutor;
        _notAuthorisedFailureFactory = notAuthorisedFailureFactory;
    }
    #endregion
    #region Methods
    public async Task<Result<TResult>> ExecuteAsync<TResult>(IQuery<TResult> query)
    {
        return await CallPerformExecuteAsync<TResult>(query);
    }
    #endregion
    #region Helpers

    private async Task<Result<TResult>> CallPerformExecuteAsync<TResult>(IQuery<TResult> query)
    {
        var queryType = query.GetType();
        var resultType = typeof(TResult);

        return await GenericMethodInvoker.InvokeAsync<Result<TResult>>(
                typeof(PermissionCheckingQueryExecutor), this, nameof(PerformExecutionAsync), new[] { queryType, resultType }, query);
    }

    private async Task<bool> CallPerformPermissionCheckingAsync<TQuery>(TQuery query) where TQuery : notnull
    {
        var queryType = query.GetType();

        return await GenericMethodInvoker.InvokeAsync<bool>(
                typeof(PermissionCheckingQueryExecutor), this, nameof(PerformPermissionCheckingAsync), new[] { queryType }, query);
    }

    private async Task<Result<TResult>> PerformExecutionAsync<TQuery, TResult>(TQuery query) where TQuery : IQuery<TResult>
    {
        if (query is IPermissionRequired)
        {
            var hasPermission = await CallPerformPermissionCheckingAsync(query);

            if (!hasPermission)
                return Result.FailureResult(_notAuthorisedFailureFactory.GetNotAuthorisedFailure());
        }

        return await _queryExecutor.ExecuteAsync(query);
    }

    private async Task<bool> PerformPermissionCheckingAsync<TQuery>(TQuery query) where TQuery : IPermissionRequired
    {
        return await _permissionCheckerExecutor.ExecuteAsync(query);
    }
    #endregion

}
