using FluentValidation.Results;

namespace PurplePiranha.Cqrs.Validation.Validators;

public class ValidatorExecutor : IValidatorExecutor
{
    private readonly IValidatorFactory _factory;

    public ValidatorExecutor(IValidatorFactory factory)
    {
        _factory = factory;
    }

    public async Task<ValidationResult> ExecuteAsync<TQuery>(TQuery query, CancellationToken cancellationToken = default) where TQuery : IValidationRequired
    {
        var handler = _factory.CreateValidator<TQuery>();
        return await handler.ValidateAsync(query, cancellationToken);
    }
}