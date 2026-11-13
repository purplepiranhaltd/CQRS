using Microsoft.Extensions.Logging;
using PurplePiranha.Cqrs.Core.Commands;
using PurplePiranha.Cqrs.Core.Failures;
using PurplePiranha.FluentResults.Results;
using PurplePiranha.Cqrs.Core.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace PurplePiranha.Cqrs.Core.Queries;

/// <summary>
/// Executes a query via the correct handler.
/// </summary>
/// <seealso cref="IQueryExecutor" />
public class QueryExecutor : IQueryExecutor
{
    private readonly IQueryHandlerFactory _queryHandlerFactory;
    private readonly ILogger<QueryExecutor> _logger;


    public QueryExecutor(IQueryHandlerFactory queryHandlerFactory, ILogger<QueryExecutor> logger)
    {
        _queryHandlerFactory = queryHandlerFactory;
        _logger = logger;
    }

    public async Task<Result<TResult>> ExecuteAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing query: {QueryName}", query.GetType().Name);

        var queryType = query.GetType();
        var resultType = typeof(TResult);

        return await GenericMethodInvoker.InvokeAsync<Result<TResult>>(
                typeof(QueryExecutor), this, nameof(ExecuteQueryAsync), new[] { queryType, resultType }, query, cancellationToken);
    }

    protected virtual async Task<Result<TResult>> ExecuteQueryAsync<TQuery, TResult>(TQuery query, CancellationToken cancellationToken = default) where TQuery : IQuery<TResult>
    {
        try
        {
            var handler = _queryHandlerFactory.CreateHandler<TQuery, TResult>();

            return await handler.ExecuteAsync(query, cancellationToken);
        }
        catch (QueryHandlerNotImplementedException)
        {
            return await Task.FromResult(Result.FailureResult<TResult>(new QueryHandlerNotImplementedFailure(typeof(TQuery).Name)));
        }
    }
}