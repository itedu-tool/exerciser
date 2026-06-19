using FluentValidation;

using Exerciser.WebApi.DTOs;

namespace Exerciser.WebApi.Validators.FluentValidation;

public class CreateGroupRequestValidator : AbstractValidator<CreateGroupRequest>
{
    public CreateGroupRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Название группы обязательно")
            .MaximumLength(200).WithMessage("Название группы не может быть длиннее 200 символов");
    }
}