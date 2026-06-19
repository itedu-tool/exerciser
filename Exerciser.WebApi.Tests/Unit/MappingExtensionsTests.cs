using System;

using Exerciser.WebApi.DTOs;
using Exerciser.WebApi.Extensions;
using Exerciser.WebApi.Models;

using Xunit;

namespace Exerciser.WebApi.Tests.Unit;

public class MappingExtensionsTests
{
    [Fact]
    public void ToExam_Should_Map_All_Fields_Correctly()
    {
        ImportExamDto dto = new()
        {
            Title = "Test Exam",
            Description = "Description",
            SingleChoiceToShow = 2,
            MultipleChoiceToShow = 3,
            TextInputToShow = 1,
            Questions =
            [
                new ImportQuestionDto
                {
                    Text = "Q1", Type = "SingleChoice", Options = ["A", "B"], CorrectAnswers = ["A"]
                }
            ]
        };

        Exam exam = dto.ToExam();

        Assert.Equal(dto.Title, exam.Title);
        Assert.Equal(dto.Description, exam.Description);
        Assert.Equal(dto.SingleChoiceToShow, exam.SingleChoiceToShow);
        Assert.Equal(dto.MultipleChoiceToShow, exam.MultipleChoiceToShow);
        Assert.Equal(dto.TextInputToShow, exam.TextInputToShow);
        Assert.Single(exam.Questions);
        Assert.Equal("Q1", exam.Questions[0].Text);
        Assert.Equal("SingleChoice", exam.Questions[0].Type);
        Assert.Equal(["A", "B"], exam.Questions[0].Options);
        Assert.Equal(["A"], exam.Questions[0].CorrectAnswers);
    }

    [Fact]
    public void ToSummaryDto_Should_Map_Correctly()
    {
        Exam exam = new()
        {
            Id = Guid.NewGuid(),
            Title = "Test",
            Description = "Desc",
            CreatedAt = DateTime.UtcNow,
            SingleChoiceToShow = 1,
            MultipleChoiceToShow = 2,
            TextInputToShow = 0,
            Questions =
            [
                new Question { Text = "Single question", Type = "SingleChoice", Options = [], CorrectAnswers = [] },
                new Question { Text = "Multiple question", Type = "MultipleChoice", Options = [], CorrectAnswers = [] }
            ]
        };

        ExamSummaryDto dto = exam.ToSummaryDto();

        Assert.Equal(exam.Id, dto.Id);
        Assert.Equal(exam.Title, dto.Title);
        Assert.Equal(exam.Description, dto.Description);
        Assert.Equal(exam.SingleChoiceToShow, dto.SingleChoiceToShow);
        Assert.Equal(exam.MultipleChoiceToShow, dto.MultipleChoiceToShow);
        Assert.Equal(exam.TextInputToShow, dto.TextInputToShow);
        Assert.Equal(exam.Questions.Count, dto.QuestionsCount);
        Assert.Equal(1, dto.SingleChoiceCount);
        Assert.Equal(1, dto.MultipleChoiceCount);
        Assert.Equal(0, dto.TextInputCount);
    }
}