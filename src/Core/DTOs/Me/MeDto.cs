namespace Core.DTOs.Me;

public record MeDto(
    string                Id,
    string                FirstName,
    string                LastName,
    string?               Email,
    bool                  EmailConfirmed,
    string?               Phone,
    bool                  PhoneNumberConfirmed,
    DateTime?             DateOfBirth,
    IReadOnlyList<string> Roles,
    DateTime              CreatedAt
);
