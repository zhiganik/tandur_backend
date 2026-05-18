namespace Core.DTOs.Me;

public class UpdateMeRequest
{
    public string    FirstName   { get; init; } = string.Empty;
    public string    LastName    { get; init; } = string.Empty;
    public DateTime? DateOfBirth { get; init; }
}
