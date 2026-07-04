using System;
using System.Collections.Generic;
using System.Linq;

using Exerciser.WebApi.DTOs;
using Exerciser.WebApi.Models;

namespace Exerciser.WebApi.Extensions;

/// <summary>
/// Статические методы для преобразования между DTO и моделями.
/// </summary>
public static class MappingExtensions
{
    /// <summary>
    /// Вычисляет максимально возможный балл для вопроса.
    /// </summary>
    public static int GetMaxScore(this Question question)
    {
        return question.Type switch
        {
            QuestionType.SingleChoice => 1,
            QuestionType.MultipleChoice => question.CorrectAnswers.Count,
            QuestionType.TextInput => 3,
            _ => 0
        };
    }

    /// <summary>
    /// Вычисляет максимально возможный балл для снимка вопроса.
    /// </summary>
    public static int GetMaxScore(this QuestionSnapshot question)
    {
        return question.Type switch
        {
            QuestionType.SingleChoice => 1,
            QuestionType.MultipleChoice => question.CorrectAnswers.Count,
            QuestionType.TextInput => 3,
            _ => 0
        };
    }

    public static Exam ToExam(this ImportExamDto dto)
    {
        return new Exam
        {
            Title = dto.Title,
            Description = dto.Description,
            Questions = dto.Questions.Select(q => new Question
            {
                Text = q.Text,
                Type = q.Type,
                Options = q.Options ?? new List<string>(),
                CorrectAnswers = q.CorrectAnswers
            }).ToList(),
            SingleChoiceToShow = dto.SingleChoiceToShow,
            MultipleChoiceToShow = dto.MultipleChoiceToShow,
            TextInputToShow = dto.TextInputToShow,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static ExamSummaryDto ToSummaryDto(this Exam exam)
    {
        return new ExamSummaryDto
        {
            Id = exam.Id,
            Title = exam.Title,
            Description = exam.Description,
            QuestionsCount = exam.Questions.Count,
            SingleChoiceCount = exam.Questions.Count(q => q.Type == QuestionType.SingleChoice),
            MultipleChoiceCount = exam.Questions.Count(q => q.Type == QuestionType.MultipleChoice),
            TextInputCount = exam.Questions.Count(q => q.Type == QuestionType.TextInput),            SingleChoiceToShow = exam.SingleChoiceToShow,
            MultipleChoiceToShow = exam.MultipleChoiceToShow,
            TextInputToShow = exam.TextInputToShow,
            CreatedAt = exam.CreatedAt
        };
    }

    public static ExamDetailsDto ToDetailsDto(this Exam exam)
    {
        return new ExamDetailsDto
        {
            Id = exam.Id,
            Title = exam.Title,
            Description = exam.Description,
            CreatedAt = exam.CreatedAt,
            Questions = exam.Questions.Select(q => new QuestionDetailsDto
            {
                Id = q.Id,
                Text = q.Text,
                Type = q.Type,
                Options = q.Options,
                CorrectAnswers = q.CorrectAnswers
            }).ToList(),
            SingleChoiceToShow = exam.SingleChoiceToShow,
            MultipleChoiceToShow = exam.MultipleChoiceToShow,
            TextInputToShow = exam.TextInputToShow
        };
    }
}