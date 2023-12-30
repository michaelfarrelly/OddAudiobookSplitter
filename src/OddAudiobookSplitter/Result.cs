using System.Diagnostics.CodeAnalysis;

/// <summary>
/// See https://gist.github.com/vkhorikov/7852c7606f27c52bc288
/// </summary>
public class Result
{
    public bool Success { get; private set; }
    public string Error { get; private set; }

    public bool Failure
    {
        get { return !Success; }
    }

    protected Result(bool success, string error)
    {
        System.Diagnostics.Contracts.Contract.Requires(success || !string.IsNullOrEmpty(error));
        System.Diagnostics.Contracts.Contract.Requires(!success || string.IsNullOrEmpty(error));

        Success = success;
        Error = error;
    }

    public static Result Fail(string message)
    {
        return new Result(false, message);
    }

    public static Result<T> Fail<T>(string message)
    {
        return new Result<T>(default(T), false, message);
    }

    public static Result Ok()
    {
        return new Result(true, String.Empty);
    }

    public static Result<T> Ok<T>(T value)
    {
        return new Result<T>(value, true, String.Empty);
    }

    public static Result Combine(params Result[] results)
    {
        foreach (Result result in results)
        {
            if (result.Failure)
                return result;
        }

        return Ok();
    }
}


public class Result<T> : Result
{
    private T _value;

    public T Value
    {
        get
        {
            System.Diagnostics.Contracts.Contract.Requires(Success);

            return _value;
        }
        [param: AllowNull]
        private set { _value = value; }
    }

    protected internal Result([AllowNull] T value, bool success, string error)
        : base(success, error)
    {
        System.Diagnostics.Contracts.Contract.Requires(value != null || !success);

        Value = value;
    }
}
