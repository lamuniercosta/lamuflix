using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using ValidationException = LamuFlix.Core.Pipeline.ValidationException;
using LamuFlix.Infrastructure.Pipeline;
using Shouldly;
using Xunit;

namespace LamuFlix.Test;

public sealed class ValidationDecoratorTests
{
    [Fact]
    public async Task HandleAsync_MultipleValidators_AggregatesFailuresAndSkipsHandler()
    {
        // arrange
        var calls = 0;
        var decorator = new ValidationDecorator<SampleRequest, string>(
            (_, _) =>
            {
                calls++;
                return Task.FromResult("ok");
            },
            [new NameValidator(), new SecondNameValidator(), new CodeValidator()]);

        // act
        var exception = await Should.ThrowAsync<ValidationException>(
            () => decorator.HandleAsync(new SampleRequest("", ""), CancellationToken.None));

        // assert
        calls.ShouldBe(0);
        exception.Errors["Name"].ShouldBe(["name", "name-2"]);
        exception.Errors["Code"].ShouldBe(["code"]);
    }

    [Fact]
    public async Task HandleAsync_ZeroValidators_InvokesHandler()
    {
        // arrange
        var decorator = new ValidationDecorator<SampleRequest, string>(
            (_, _) => Task.FromResult("ok"),
            []);

        // act
        var result = await decorator.HandleAsync(new SampleRequest("a", "b"), CancellationToken.None);

        // assert
        result.ShouldBe("ok");
    }

    private sealed record SampleRequest(string Name, string Code);

    private sealed class NameValidator : AbstractValidator<SampleRequest>
    {
        public NameValidator() => RuleFor(request => request.Name).Must(_ => false).WithMessage("name");
    }

    private sealed class SecondNameValidator : AbstractValidator<SampleRequest>
    {
        public SecondNameValidator() => RuleFor(request => request.Name).Must(_ => false).WithMessage("name-2");
    }

    private sealed class CodeValidator : AbstractValidator<SampleRequest>
    {
        public CodeValidator() => RuleFor(request => request.Code).Must(_ => false).WithMessage("code");
    }
}
