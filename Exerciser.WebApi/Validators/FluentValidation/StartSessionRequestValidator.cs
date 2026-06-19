using FluentValidation;

using Exerciser.WebApi.DTOs;

namespace Exerciser.WebApi.Validators.FluentValidation;

public class StartSessionRequestValidator : AbstractValidator<StartSessionRequest>
{
    public StartSessionRequestValidator()
    {
        RuleFor(x => x.GroupId)
            .NotEmpty().WithMessage("GroupId обязателен");

        RuleFor(x => x.StudentId)
            .NotEmpty().WithMessage("StudentId обязателен");
    }
}