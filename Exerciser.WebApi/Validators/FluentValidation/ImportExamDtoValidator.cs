using System.Collections.Generic;
using System.Linq;

using FluentValidation;

using Exerciser.WebApi.DTOs;
using Exerciser.WebApi.Models;
namespace Exerciser.WebApi.Validators.FluentValidation;

public class ImportExamDtoValidator : AbstractValidator<ImportExamDto>
{
    public ImportExamDtoValidator()
    {
        #region Основные поля

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Название экзамена не может быть пустым")
            .MaximumLength(500).WithMessage("Название экзамена не может быть длиннее 500 символов");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Описание экзамена не может быть длиннее 2000 символов")
            .When(x => x.Description != null);

        #endregion

        #region Количество вопросов для показа

        RuleFor(x => x.SingleChoiceToShow)
            .GreaterThanOrEqualTo(0).WithMessage("SingleChoiceToShow не может быть отрицательным");

        RuleFor(x => x.MultipleChoiceToShow)
            .GreaterThanOrEqualTo(0).WithMessage("MultipleChoiceToShow не может быть отрицательным");

        RuleFor(x => x.TextInputToShow)
            .GreaterThanOrEqualTo(0).WithMessage("TextInputToShow не может быть отрицательным");

        #endregion

        #region Список вопросов

        RuleFor(x => x.Questions)
            .NotNull().WithMessage("Список вопросов не может быть null")
            .NotEmpty().WithMessage("Экзамен должен содержать хотя бы один вопрос");

        RuleForEach(x => x.Questions)
            .SetValidator(new ImportQuestionDtoValidator());

        #endregion
    }
}

public class ImportQuestionDtoValidator : AbstractValidator<ImportQuestionDto>
{
    public ImportQuestionDtoValidator()
    {
        #region Текст вопроса

        RuleFor(x => x.Text)
            .NotEmpty().WithMessage("Текст вопроса не может быть пустым")
            .MaximumLength(1000).WithMessage("Текст вопроса не может быть длиннее 1000 символов");

        #endregion

        #region Тип вопроса

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Недопустимый тип вопроса. Допустимые: SingleChoice, MultipleChoice, TextInput");
        #endregion

        #region Валидация в зависимости от типа

        RuleFor(x => x)
            .Custom((dto, context) =>
            {
                if (dto.Type == QuestionType.TextInput)
                {
                    return;
                }

                #region Проверка вариантов

                if (dto.Options == null || dto.Options.Count < 2)
                {
                    context.AddFailure($"Для типа {dto.Type} необходимо минимум 2 варианта ответа");
                    return;
                }

                if (dto.Options.Any(string.IsNullOrWhiteSpace))
                {
                    context.AddFailure("Варианты ответов не могут быть пустыми");
                }

                if (dto.Options.GroupBy(o => o).Any(g => g.Count() > 1))
                {
                    context.AddFailure("Варианты ответов не должны дублироваться");
                }

                #endregion

                #region Правильные ответы

                if (dto.CorrectAnswers == null || dto.CorrectAnswers.Count == 0)
                {
                    context.AddFailure("Должен быть указан хотя бы один правильный ответ");
                    return;
                }

                if (dto.Type == QuestionType.SingleChoice && dto.CorrectAnswers.Count > 1)
                {
                    context.AddFailure("Для типа SingleChoice допускается только один правильный ответ");
                }

                List<string> invalidAnswers = dto.CorrectAnswers.Except(dto.Options).ToList();
                if (invalidAnswers.Any())
                {
                    context.AddFailure(
                        $"Правильные ответы [{string.Join(", ", invalidAnswers)}] отсутствуют в вариантах");
                }

                #endregion
            });

        #endregion
    }
}