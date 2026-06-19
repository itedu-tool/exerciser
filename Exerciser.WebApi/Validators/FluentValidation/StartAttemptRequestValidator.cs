using FluentValidation;

using Exerciser.WebApi.DTOs;

namespace Exerciser.WebApi.Validators.FluentValidation;

public class StartAttemptRequestValidator : AbstractValidator<StartAttemptRequest>
{
    public StartAttemptRequestValidator()
    {
        RuleFor(x => x.ExamId)
            .NotEmpty().WithMessage("ExamId обязателен");
    }
}