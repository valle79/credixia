namespace Lending.Application.Common;

/// <summary>
/// Explicit outcome for use cases. Expected business failures are values, not exceptions.
/// </summary>
public class Result
{
    protected Result(bool isSuccess, AppError? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public AppError? Error { get; }

    public static Result Success() => new(true, null);

    public static Result Failure(AppError error) =>
        new(false, error ?? throw new ArgumentNullException(nameof(error)));

    public static Result<T> Success<T>(T value) => Result<T>.FromValue(value);

    public static Result<T> Failure<T>(AppError error) => Result<T>.FromError(error);
}

public sealed class Result<T> : Result
{
    private readonly T? _value;

    private Result(bool isSuccess, T? value, AppError? error)
        : base(isSuccess, error)
    {
        _value = value;
    }

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("A failed result does not contain a value.");

    internal static Result<T> FromValue(T value) => new(true, value, null);

    internal static Result<T> FromError(AppError error) =>
        new(false, default, error ?? throw new ArgumentNullException(nameof(error)));
}
