using DropBear.Codex.Core;

namespace DropBear.Codex.Files.Extensions;

public static class ResultExtensions
{
    public static Result<T> ToSuccessResult<T>(this T value)
    {
        return Result<T>.Success(value);
    }
    
    public static Result<T> ToFailureResult<T>(this string error)
    {
        return Result<T>.Failure(error);
    }
}
