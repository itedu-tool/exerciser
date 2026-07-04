using System;

using FluentValidation;

using Exerciser.WebApi.DTOs;

namespace Exerciser.WebApi.Validators.FluentValidation;

public class FinishAttemptRequestValidator : AbstractValidator<FinishAttemptRequest>
{
    public FinishAttemptRequestValidator()
    {
        RuleFor(x => x.FinishedAt)            .NotEmpty().WithMessage("FinishedAt обязателен")
            .LessThanOrEqualTo(DateTime.UtcNow).WithMessage("FinishedAt не может быть в будущем");

        RuleFor(x => x.Answers)
            .NotNull().WithMessage("Список ответов не может быть null");

        RuleForEach(x => x.Answers)
            .SetValidator(new AnswerSubmissionDtoValidator());
    }
}

public class AnswerSubmissionDtoValidator : AbstractValidator<AnswerSubmissionDto>
{
    public AnswerSubmissionDtoValidator()
    {
        RuleFor(x => x.QuestionId)
            .NotEmpty().WithMessage("QuestionId обязателен");    }
}