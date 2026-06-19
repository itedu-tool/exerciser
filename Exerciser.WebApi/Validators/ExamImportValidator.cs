using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Exerciser.WebApi.DTOs;
using Exerciser.WebApi.Exceptions;

using Microsoft.Extensions.Logging;

namespace Exerciser.WebApi.Validators;

/// <summary>Интерфейс валидатора для импорта экзаменов.</summary>
public interface IExamImportValidator
{
    /// <summary>Валидировать данные импорта экзамена.</summary>
    /// <param name="importData">Данные для импорта.</param>
    /// <returns>True если валидно, иначе выбросит исключение.</returns>
    Task<bool> ValidateAsync(ImportExamDto? importData);
}

/// <summary>Реализация валидатора импорта экзаменов.</summary>
public class ExamImportValidator : IExamImportValidator
{
    private readonly ILogger<ExamImportValidator> _logger;

    // Константы для валидации
    private const int MaxTitleLength = 500;
    private const int MaxDescriptionLength = 2000;
    private const int MaxQuestionText = 1000;
    private const int MaxOptionLength = 300;
    private const int MaxCorrectAnswersCount = 100;
    private const int MinOptionsCount = 2;

    public ExamImportValidator(ILogger<ExamImportValidator> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<bool> ValidateAsync(ImportExamDto? importData)
    {
        return await Task.Run(() =>
        {
            if (importData == null)
            {
                throw new ImportValidationException("Данные импорта не могут быть пустыми");
            }

            // Валидация основных полей экзамена
            ValidateExamFields(importData);

            // Валидация вопросов
            ValidateQuestions(importData.Questions);

            _logger.LogInformation("✓ Данные экзамена успешно прошли валидацию: {Title}", importData.Title);
            return true;
        });
    }

    /// <summary>Валидировать основные поля экзамена.</summary>
    private void ValidateExamFields(ImportExamDto importData)
    {
        if (string.IsNullOrWhiteSpace(importData.Title))
        {
            throw new ImportValidationException("Название экзамена не может быть пустым");
        }

        if (importData.Title.Length > MaxTitleLength)
        {
            throw new ImportValidationException(
                $"Название экзамена не может быть длиннее {MaxTitleLength} символов. " +
                $"Текущая длина: {importData.Title.Length}");
        }

        if (!string.IsNullOrWhiteSpace(importData.Description) &&
            importData.Description.Length > MaxDescriptionLength)
        {
            throw new ImportValidationException(
                $"Описание экзамена не может быть длиннее {MaxDescriptionLength} символов. " +
                $"Текущая длина: {importData.Description?.Length}");
        }

        if (importData.Questions == null || importData.Questions.Count == 0)
        {
            throw new ImportValidationException("Экзамен должен содержать хотя бы один вопрос");
        }

        // Проверка QuestionsToShow
        if (importData.SingleChoiceToShow < 0)
        {
            throw new ImportValidationException(
                "Количество вопросов SingleChoice для показа не может быть отрицательным");
        }

        if (importData.MultipleChoiceToShow < 0)
        {
            throw new ImportValidationException(
                "Количество вопросов MultipleChoice для показа не может быть отрицательным");
        }

        if (importData.TextInputToShow < 0)
        {
            throw new ImportValidationException("Количество вопросов TextInput для показа не может быть отрицательным");
        }

        int actualSingleCount = importData.Questions.Count(q => q.Type == "SingleChoice");
        int actualMultipleCount = importData.Questions.Count(q => q.Type == "MultipleChoice");
        int actualTextCount = importData.Questions.Count(q => q.Type == "TextInput");

        if (importData.SingleChoiceToShow > actualSingleCount && importData.SingleChoiceToShow != 0)
        {
            throw new ImportValidationException(
                $"SingleChoiceToShow ({importData.SingleChoiceToShow}) превышает доступное количество ({actualSingleCount})");
        }

        if (importData.MultipleChoiceToShow > actualMultipleCount && importData.MultipleChoiceToShow != 0)
        {
            throw new ImportValidationException(
                $"MultipleChoiceToShow ({importData.MultipleChoiceToShow}) превышает доступное количество ({actualMultipleCount})");
        }

        if (importData.TextInputToShow > actualTextCount && importData.TextInputToShow != 0)
        {
            throw new ImportValidationException(
                $"TextInputToShow ({importData.TextInputToShow}) превышает доступное количество ({actualTextCount})");
        }


        _logger.LogDebug("✓ Основные поля экзамена валидны");
    }

    /// <summary>Валидировать список вопросов.</summary>
    private void ValidateQuestions(List<ImportQuestionDto> questions)
    {
        string[] validQuestionTypes = new[] { "SingleChoice", "MultipleChoice", "TextInput" };

        for (int i = 0; i < questions.Count; i++)
        {
            ImportQuestionDto question = questions[i];
            int questionIndex = i + 1;

            // Валидация текста вопроса
            if (string.IsNullOrWhiteSpace(question.Text))
            {
                throw new ImportValidationException(
                    $"Вопрос #{questionIndex}: текст вопроса не может быть пустым");
            }

            if (question.Text.Length > MaxQuestionText)
            {
                throw new ImportValidationException(
                    $"Вопрос #{questionIndex}: текст не может быть длиннее {MaxQuestionText} символов");
            }

            // Валидация типа вопроса
            if (string.IsNullOrWhiteSpace(question.Type))
            {
                throw new ImportValidationException(
                    $"Вопрос #{questionIndex}: тип вопроса не может быть пустым");
            }

            if (!validQuestionTypes.Contains(question.Type))
            {
                throw new ImportValidationException(
                    $"Вопрос #{questionIndex}: недопустимый тип '{question.Type}'. " +
                    $"Допустимые типы: {string.Join(", ", validQuestionTypes)}");
            }

            // Валидация вариантов ответов (для Single/MultipleChoice)
            if (question.Type != "TextInput")
            {
                if (question.Options == null || question.Options.Count == 0)
                {
                    throw new ImportValidationException(
                        $"Вопрос #{questionIndex} ({question.Type}): должны быть указаны варианты ответов");
                }

                if (question.Options.Count < MinOptionsCount)
                {
                    throw new ImportValidationException(
                        $"Вопрос #{questionIndex} ({question.Type}): минимум {MinOptionsCount} варианта ответа. " +
                        $"Текущее количество: {question.Options.Count}");
                }

                // Проверка длины каждого варианта
                for (int j = 0; j < question.Options.Count; j++)
                {
                    if (string.IsNullOrWhiteSpace(question.Options[j]))
                    {
                        throw new ImportValidationException(
                            $"Вопрос #{questionIndex}: вариант ответа #{j + 1} не может быть пустым");
                    }

                    if (question.Options[j].Length > MaxOptionLength)
                    {
                        throw new ImportValidationException(
                            $"Вопрос #{questionIndex}: вариант ответа #{j + 1} слишком длинный");
                    }
                }

                // Проверка уникальности вариантов
                List<IGrouping<string, string>> duplicates =
                    question.Options.GroupBy(x => x).Where(g => g.Count() > 1).ToList();
                if (duplicates.Any())
                {
                    throw new ImportValidationException(
                        $"Вопрос #{questionIndex}: найдены дублирующиеся варианты ответов");
                }
            }

            // Валидация правильных ответов
            if (question.CorrectAnswers == null || question.CorrectAnswers.Count == 0)
            {
                throw new ImportValidationException(
                    $"Вопрос #{questionIndex}: должен быть указан хотя бы один правильный ответ");
            }

            if (question.CorrectAnswers.Count > MaxCorrectAnswersCount)
            {
                throw new ImportValidationException(
                    $"Вопрос #{questionIndex}: слишком много правильных ответов");
            }

            // Для SingleChoice только один правильный ответ
            if (question is { Type: "SingleChoice", CorrectAnswers.Count: > 1 })
            {
                throw new ImportValidationException(
                    $"Вопрос #{questionIndex} (SingleChoice): допускается только один правильный ответ");
            }

            // Для TextInput нет проверки наличия в Options
            if (question.Type != "TextInput")
            {
                // Проверка, что все правильные ответы есть в вариантах
                List<string> invalidAnswers =
                    question.CorrectAnswers.Where(ca => !question.Options!.Contains(ca)).ToList();
                if (invalidAnswers.Any())
                {
                    throw new ImportValidationException(
                        $"Вопрос #{questionIndex}: правильные ответы [{string.Join(", ", invalidAnswers)}] " +
                        $"отсутствуют в вариантах ответов");
                }
            }

            _logger.LogDebug("✓ Вопрос #{QuestionIndex} валиден", questionIndex);
        }
    }
}