using System.Collections.Generic;

using FluentValidation.TestHelper;
using Exerciser.WebApi.DTOs;
using Exerciser.WebApi.Validators.FluentValidation;
using Xunit;

namespace Exerciser.WebApi.Tests.Unit.Validators;

public class ImportExamDtoValidatorTests
{
    private readonly ImportExamDtoValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_Title_Is_Empty()
    {
        var dto = new ImportExamDto { Title = "", Questions = new List<ImportQuestionDto>() };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void Should_Have_Error_When_Title_Exceeds_MaxLength()
    {
        var dto = new ImportExamDto { Title = new string('A', 501), Questions = new List<ImportQuestionDto>() };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void Should_Have_Error_When_Questions_Is_Empty()
    {
        var dto = new ImportExamDto { Title = "Valid Title", Questions = new List<ImportQuestionDto>() };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Questions);
    }

    [Fact]
    public void Should_Have_Error_When_SingleChoiceToShow_Is_Negative()
    {
        var dto = new ImportExamDto { Title = "Valid Title", Questions = new List<ImportQuestionDto>(), SingleChoiceToShow = -1 };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.SingleChoiceToShow);
    }

    [Fact]
    public void Should_Have_Error_When_Question_Text_Is_Empty()
    {
        var dto = new ImportExamDto
        {
            Title = "Valid Title",
            Questions = new List<ImportQuestionDto>
            {
                new() { Text = "", Type = "SingleChoice", Options = new List<string> { "A", "B" }, CorrectAnswers = new List<string> { "A" } }
            }
        };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor("Questions[0].Text");
    }

    [Fact]
    public void Should_Have_Error_When_SingleChoice_Has_More_Than_One_Correct_Answer()
    {
        var dto = new ImportExamDto
        {
            Title = "Valid Title",
            Questions = new List<ImportQuestionDto>
            {
                new() { Text = "Q1", Type = "SingleChoice", Options = new List<string> { "A", "B" }, CorrectAnswers = new List<string> { "A", "B" } }
            }
        };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor("Questions[0]");
    }

    [Fact]
    public void Should_Not_Have_Error_When_Valid_Exam_With_All_Fields()
    {
        var dto = new ImportExamDto
        {
            Title = "Valid Title",
            Description = "Description",
            Questions = new List<ImportQuestionDto>
            {
                new() { Text = "Q1", Type = "SingleChoice", Options = new List<string> { "A", "B" }, CorrectAnswers = new List<string> { "A" } }
            },
            SingleChoiceToShow = 1,
            MultipleChoiceToShow = 0,
            TextInputToShow = 0
        };
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveAnyValidationErrors();
    }
}