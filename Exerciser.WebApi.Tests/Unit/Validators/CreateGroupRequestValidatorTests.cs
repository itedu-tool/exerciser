using FluentValidation.TestHelper;

using Exerciser.WebApi.DTOs;
using Exerciser.WebApi.Validators.FluentValidation;

using Xunit;

namespace Exerciser.WebApi.Tests.Unit.Validators;

public class CreateGroupRequestValidatorTests
{
    private readonly CreateGroupRequestValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_Name_Is_Empty()
    {
        CreateGroupRequest request = new() { Name = "" };
        TestValidationResult<CreateGroupRequest>? result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Should_Have_Error_When_Name_Exceeds_MaxLength()
    {
        CreateGroupRequest request = new() { Name = new string('A', 201) };
        TestValidationResult<CreateGroupRequest>? result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Name_Is_Valid()
    {
        CreateGroupRequest request = new() { Name = "Группа 1" };
        TestValidationResult<CreateGroupRequest>? result = _validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }
}