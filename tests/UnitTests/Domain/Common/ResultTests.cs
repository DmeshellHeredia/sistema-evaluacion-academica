using FluentAssertions;
using SistemaEvaluacionAcademica.Application.Common;

namespace SistemaEvaluacionAcademica.UnitTests.Domain.Common;

public class ResultTests
{

    [Fact]
    public void Success_ShouldSetIsSuccessTrue()
    {
        var result = Result<string>.Success("data");
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Success_ShouldSetDataCorrectly()
    {
        var result = Result<int>.Success(42);
        result.Data.Should().Be(42);
    }

    [Fact]
    public void Success_DefaultStatusCode_ShouldBe200()
    {
        var result = Result<bool>.Success(true);
        result.StatusCode.Should().Be(200);
    }

    [Fact]
    public void Success_WithCustomStatusCode_ShouldBeSet()
    {
        var result = Result<string>.Success("created", statusCode: 201);
        result.StatusCode.Should().Be(201);
    }

    [Fact]
    public void Success_ErrorMessage_ShouldBeNull()
    {
        var result = Result<string>.Success("ok");
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void Success_Errors_ShouldBeEmpty()
    {
        var result = Result<string>.Success("ok");
        result.Errors.Should().BeEmpty();
    }


    [Fact]
    public void Failure_ShouldSetIsSuccessFalse()
    {
        var result = Result<string>.Failure("error");
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Failure_ShouldSetErrorMessage()
    {
        var message = "El recurso no existe.";
        var result = Result<string>.Failure(message);
        result.ErrorMessage.Should().Be(message);
    }

    [Fact]
    public void Failure_DefaultStatusCode_ShouldBe400()
    {
        var result = Result<string>.Failure("bad request");
        result.StatusCode.Should().Be(400);
    }

    [Fact]
    public void Failure_Data_ShouldBeNull()
    {
        var result = Result<string>.Failure("error");
        result.Data.Should().BeNull();
    }


    [Fact]
    public void Failure_WithMultipleErrors_ShouldSetErrorsCollection()
    {
        var errors = new[] { "Error 1", "Error 2", "Error 3" };
        var result = Result<string>.Failure(errors);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().BeEquivalentTo(errors);
        result.StatusCode.Should().Be(422);
    }

    [Fact]
    public void Failure_WithMultipleErrors_ErrorMessage_ShouldBeNull()
    {
        var result = Result<string>.Failure(new[] { "e1", "e2" });
        result.ErrorMessage.Should().BeNull();
    }


    [Fact]
    public void NotFound_ShouldSetStatusCode404()
    {
        var result = Result<string>.NotFound("No encontrado.");
        result.StatusCode.Should().Be(404);
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("No encontrado.");
    }


    [Fact]
    public void Unauthorized_ShouldSetStatusCode401()
    {
        var result = Result<string>.Unauthorized();
        result.StatusCode.Should().Be(401);
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Unauthorized_WithCustomMessage_ShouldUseIt()
    {
        var result = Result<string>.Unauthorized("Credenciales inválidas.");
        result.ErrorMessage.Should().Be("Credenciales inválidas.");
    }


    [Fact]
    public void Forbidden_ShouldSetStatusCode403()
    {
        var result = Result<string>.Forbidden();
        result.StatusCode.Should().Be(403);
        result.IsSuccess.Should().BeFalse();
    }


    [Fact]
    public void PagedResult_Create_ShouldCalculateTotalPagesCorrectly()
    {
        var items = new[] { "a", "b", "c" };
        var paged = PagedResult<string>.Create(items, totalCount: 25, page: 2, pageSize: 10);

        paged.TotalPages.Should().Be(3); // ceil(25/10) = 3
        paged.TotalCount.Should().Be(25);
        paged.Page.Should().Be(2);
        paged.PageSize.Should().Be(10);
    }

    [Fact]
    public void PagedResult_HasNextPage_ShouldBeTrueWhenNotLastPage()
    {
        var paged = PagedResult<string>.Create(new[] { "a" }, 25, 1, 10);
        paged.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public void PagedResult_HasNextPage_ShouldBeFalseOnLastPage()
    {
        var paged = PagedResult<string>.Create(new[] { "a" }, 25, 3, 10);
        paged.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public void PagedResult_HasPreviousPage_ShouldBeFalseOnFirstPage()
    {
        var paged = PagedResult<string>.Create(new[] { "a" }, 25, 1, 10);
        paged.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public void PagedResult_HasPreviousPage_ShouldBeTrueAfterFirstPage()
    {
        var paged = PagedResult<string>.Create(new[] { "a" }, 25, 2, 10);
        paged.HasPreviousPage.Should().BeTrue();
    }

    [Theory]
    [InlineData(10, 10, 1)]   // exacto: 1 página
    [InlineData(11, 10, 2)]   // una más
    [InlineData(1,  10, 1)]   // un ítem
    [InlineData(100,10, 10)]  // exacto: 10 páginas
    [InlineData(101,10, 11)]  // 10 + 1 extra
    public void PagedResult_TotalPages_ShouldRoundUp(int total, int pageSize, int expectedPages)
    {
        var paged = PagedResult<int>.Create(Array.Empty<int>(), total, 1, pageSize);
        paged.TotalPages.Should().Be(expectedPages);
    }
}
