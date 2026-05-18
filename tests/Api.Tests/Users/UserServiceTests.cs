using Core.Domain.Entities;
using Core.DTOs.Common;
using Core.DTOs.Users;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Services;
using Moq;

namespace Api.Tests.Users;

[TestFixture]
public class UserServiceTests
{
    private Mock<IUserRepository>       _repo             = null!;
    private Mock<IRefreshTokenService>  _refreshTokens    = null!;
    private UserService                 _service          = null!;

    private static readonly PaginationQuery DefaultQuery = new() { Page = 1, Limit = 20 };

    [SetUp]
    public void SetUp()
    {
        _repo          = new Mock<IUserRepository>();
        _refreshTokens = new Mock<IRefreshTokenService>();
        _service       = new UserService(_repo.Object, _refreshTokens.Object);
    }

    // GetPagedAsync
    [Test]
    public async Task GetPagedAsync_ReturnsMappedPagedResult()
    {
        var users = new List<AppUser>
        {
            MakeUser("alice", "Alice", "Smith", email: "a@x.com"),
            MakeUser("bob",   "Bob",   "Jones", email: "b@x.com"),
        };
        _repo.Setup(r => r.CountAsync()).ReturnsAsync(2);
        _repo.Setup(r => r.GetPagedAsync(1, 20)).ReturnsAsync(users);
        _repo.Setup(r => r.GetRolesMapAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(new Dictionary<string, IReadOnlyList<string>>
            {
                [users[0].Id] = ["User"],
                [users[1].Id] = ["Admin"],
            });

        var result = await _service.GetPagedAsync(DefaultQuery);

        Assert.That(result.Data, Has.Count.EqualTo(2));
        Assert.That(result.Total, Is.EqualTo(2));
        Assert.That(result.Data[0].Roles, Contains.Item("User"));
        Assert.That(result.Data[1].Roles, Contains.Item("Admin"));
    }

    [Test]
    public async Task GetPagedAsync_MasksEmailAndPhone()
    {
        var user = MakeUser("u1", "John", "Doe", email: "john@example.com", phone: "+79001234567");
        _repo.Setup(r => r.CountAsync()).ReturnsAsync(1);
        _repo.Setup(r => r.GetPagedAsync(1, 20)).ReturnsAsync([user]);
        _repo.Setup(r => r.GetRolesMapAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(new Dictionary<string, IReadOnlyList<string>> { [user.Id] = [] });

        var result = await _service.GetPagedAsync(DefaultQuery);

        var dto = result.Data[0];
        Assert.That(dto.Email, Does.Contain("*"));
        Assert.That(dto.Email, Does.StartWith("j"));
        Assert.That(dto.Phone, Does.Contain("*"));
    }

    [Test]
    public async Task GetPagedAsync_IncludesEmailAndPhoneConfirmedFlags()
    {
        var user = MakeUser("u1", "Ann", "Lee", emailConfirmed: true, phoneConfirmed: false);
        _repo.Setup(r => r.CountAsync()).ReturnsAsync(1);
        _repo.Setup(r => r.GetPagedAsync(1, 20)).ReturnsAsync([user]);
        _repo.Setup(r => r.GetRolesMapAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(new Dictionary<string, IReadOnlyList<string>> { [user.Id] = [] });

        var result = await _service.GetPagedAsync(DefaultQuery);

        Assert.That(result.Data[0].EmailConfirmed, Is.True);
        Assert.That(result.Data[0].PhoneNumberConfirmed, Is.False);
    }

    // UpdateProfileAsync
    [Test]
    public async Task UpdateProfileAsync_NonExistingUser_ReturnsNotFound()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<string>())).ReturnsAsync((AppUser?)null);

        var result = await _service.UpdateProfileAsync("missing-id", new UpdateProfileRequest
        {
            FirstName = "A", LastName = "B", PhoneNumber = "+1234567890",
        });

        Assert.That(result, Is.InstanceOf<UserUpdateResult.NotFound>());
        _repo.Verify(r => r.UpdateAsync(It.IsAny<AppUser>()), Times.Never);
    }

    [Test]
    public async Task UpdateProfileAsync_Success_ReturnsSuccess()
    {
        var user = MakeUser("u1", "Old", "Name");
        _repo.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);
        _repo.Setup(r => r.UpdateAsync(user)).ReturnsAsync((true, Array.Empty<string>()));

        var result = await _service.UpdateProfileAsync(user.Id, new UpdateProfileRequest
        {
            FirstName = "New", LastName = "Name", PhoneNumber = "+9998887766",
        });

        Assert.That(result, Is.InstanceOf<UserUpdateResult.Success>());
        Assert.That(user.FirstName, Is.EqualTo("New"));
        Assert.That(user.PhoneNumberConfirmed, Is.True);
    }

    [Test]
    public async Task UpdateProfileAsync_IdentityFailure_ReturnsFailed()
    {
        var user   = MakeUser("u1", "Old", "Name");
        var errors = new[] { "Email already taken." };
        _repo.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);
        _repo.Setup(r => r.UpdateAsync(user)).ReturnsAsync((false, errors));

        var result = await _service.UpdateProfileAsync(user.Id, new UpdateProfileRequest
        {
            FirstName = "A", LastName = "B", PhoneNumber = "+1234567890",
        });

        var failed = result as UserUpdateResult.Failed;
        Assert.That(failed, Is.Not.Null);
        Assert.That(failed!.Errors, Contains.Item("Email already taken."));
    }

    // DeleteAsync
    [Test]
    public async Task DeleteAsync_RevokesTokensAndDelegatesDelete()
    {
        var userId = "user-123";
        _repo.Setup(r => r.DeleteAsync(userId)).ReturnsAsync(true);
        _refreshTokens.Setup(r => r.RevokeAllForUserAsync(userId)).Returns(Task.CompletedTask);

        var result = await _service.DeleteAsync(userId);

        Assert.That(result, Is.True);
        _refreshTokens.Verify(r => r.RevokeAllForUserAsync(userId), Times.Once);
        _repo.Verify(r => r.DeleteAsync(userId), Times.Once);
    }

    [Test]
    public async Task DeleteAsync_NonExistingUser_ReturnsFalse()
    {
        _repo.Setup(r => r.DeleteAsync(It.IsAny<string>())).ReturnsAsync(false);
        _refreshTokens.Setup(r => r.RevokeAllForUserAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

        Assert.That(await _service.DeleteAsync("ghost-id"), Is.False);
    }

    private static AppUser MakeUser(string id, string firstName, string lastName,
        string? email = null, string? phone = null,
        bool emailConfirmed = false, bool phoneConfirmed = false) =>
        new()
        {
            Id                   = id,
            FirstName            = firstName,
            LastName             = lastName,
            Email                = email,
            PhoneNumber          = phone,
            EmailConfirmed       = emailConfirmed,
            PhoneNumberConfirmed = phoneConfirmed,
            CreatedAt            = DateTime.UtcNow,
        };
}
