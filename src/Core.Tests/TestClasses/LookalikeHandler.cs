using PurplePiranha.Cqrs.Core.Commands;
using PurplePiranha.FluentResults.Results;

namespace PurplePiranha.Cqrs.Core.Tests.Lookalike
{
    // Same name and arity as the real ICommandHandler<T>, but a different type.
    public interface ICommandHandler<T> { }
}

namespace PurplePiranha.Cqrs.Core.Tests
{
    public record LookalikeCommand() : ICommand;

    // Implements the real handler interface and an unrelated interface of the same name.
    public class LookalikeHandler : ICommandHandler<LookalikeCommand>, Lookalike.ICommandHandler<LookalikeCommand>
    {
        public Task<Result> ExecuteAsync(LookalikeCommand command, CancellationToken cancellationToken = default)
            => Task.FromResult(Result.SuccessResult());
    }
}
