namespace Core.DTOs.Users;

public abstract record UserUpdateResult
{
    public sealed record NotFound : UserUpdateResult;
    public sealed record Success : UserUpdateResult;
    public sealed record Failed(string[] Errors) : UserUpdateResult;
}
