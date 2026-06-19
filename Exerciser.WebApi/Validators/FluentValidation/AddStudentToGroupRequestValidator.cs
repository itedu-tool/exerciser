using FluentValidation;

using Exerciser.WebApi.DTOs;

namespace Exerciser.WebApi.Validators.FluentValidation;

public class AddStudentToGroupRequestValidator : AbstractValidator<AddStudentToGroupRequest>
{
    public AddStudentToGroupRequestValidator()
    {
        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Фамилия обязательна")
            .MaximumLength(100).WithMessage("Фамилия не может быть длиннее 100 символов");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("Имя обязательно")
            .MaximumLength(100).WithMessage("Имя не может быть длиннее 100 символов");

        RuleFor(x => x.Patronymic)
            .MaximumLength(100).WithMessage("Отчество не может быть длиннее 100 символов")
            .When(x => x.Patronymic != null);
    }
}