using Lending.Application.Common;

namespace Lending.UnitTests.Common;

public sealed class ResultTests
{
    [Fact]
    public void Success_has_no_error()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Failure_carries_error()
    {
        var error = AppError.Conflict("tenant.slug.duplicate", "Slug already exists.");
        var result = Result.Failure(error);

        Assert.True(result.IsFailure);
        Assert.Same(error, result.Error);
    }

    [Fact]
    public void Generic_success_exposes_value()
    {
        var result = Result.Success(42);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void Generic_failure_throws_when_value_is_read()
    {
        var result = Result.Failure<int>(AppError.NotFound("tenant.not_found", "Not found."));

        Assert.True(result.IsFailure);
        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void Failure_without_error_throws()
    {
        Assert.Throws<ArgumentNullException>(() => Result.Failure(null!));
        Assert.Throws<ArgumentNullException>(() => Result.Failure<int>(null!));
    }
}
