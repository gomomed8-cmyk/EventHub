using EventHub.Application.Common.Results;
using EventHub.Application.Common.Errors;

public class Result<TValue> : Result
{
    private readonly TValue _value;

    public TValue Value =>
        IsSuccess
            ? _value
            : throw new InvalidOperationException(
                "Cannot access the value of a failed result.");

    private Result(TValue value)
        : base(true, Array.Empty<Error>())
    {
        _value = value;
    }

    private Result(Error error)
        : base(false, new[] { error })
    {
        _value = default!;
    }

    private Result(IReadOnlyList<Error> errors)
        : base(false, errors)
    {
        _value = default!;
    }

    public static Result<TValue> Ok(TValue value)
        => new(value);

    public static new Result<TValue> Fail(Error error)
        => new(error);

    public  static new Result<TValue> Fail(IReadOnlyList<Error> errors)
        => new(errors);

    public static implicit operator Result<TValue>(TValue value)
        => Ok(value);

    public static implicit operator Result<TValue>(Error error)
        => Fail(error);
}