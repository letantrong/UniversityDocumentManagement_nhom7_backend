namespace Application.Common.Models;

public class Result
{
    public bool Succeeded { get; }
    public string Message { get; }
    public string[] Errors { get; }

    protected Result(bool succeeded, string message, IEnumerable<string>? errors = null)
    {
        Succeeded = succeeded;
        Message = message;
        Errors = errors?.ToArray() ?? Array.Empty<string>();
    }

    public static Result Success(string message = "Operation succeeded.") => new(true, message);
    public static Result Failure(string message, IEnumerable<string>? errors = null) => new(false, message, errors);
    public static Result Failure(string error) => new(false, error, new[] { error });
}

public class Result<T> : Result
{
    public T? Data { get; }

    protected Result(bool succeeded, string message, T? data, IEnumerable<string>? errors = null)
        : base(succeeded, message, errors)
    {
        Data = data;
    }

    public static Result<T> Success(T data, string message = "Operation succeeded.") => new(true, message, data);
    public new static Result<T> Failure(string message, IEnumerable<string>? errors = null) => new(false, message, default, errors);
    public new static Result<T> Failure(string error) => new(false, error, default, new[] { error });
}
