using System.Security.Claims;
using Api.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;

namespace Api.Tests.Filters;

[TestFixture]
public class BlockScopedTokenAttributeTests
{
    private const string UserId = "user-123";

    private static ActionExecutingContext MakeContext(ClaimsPrincipal user)
    {
        var httpContext = new DefaultHttpContext { User = user };
        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor());

        return new ActionExecutingContext(
            actionContext,
            [],
            new Dictionary<string, object?>(),
            controller: null!);
    }

    private static ClaimsPrincipal MakeUser(string? userId, string? scope = null)
    {
        var claims = new List<Claim>();
        if (userId is not null) claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));
        if (scope is not null) claims.Add(new Claim("scope", scope));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }

    [Test]
    public void OnActionExecuting_ChangePasswordScope_SetsForbidResult()
    {
        var context = MakeContext(MakeUser(UserId, scope: "change_password"));

        new BlockScopedTokenAttribute().OnActionExecuting(context);

        Assert.That(context.Result, Is.InstanceOf<ForbidResult>());
    }

    [Test]
    public void OnActionExecuting_MissingNameIdentifier_SetsForbidResult()
    {
        var context = MakeContext(MakeUser(userId: null));

        new BlockScopedTokenAttribute().OnActionExecuting(context);

        Assert.That(context.Result, Is.InstanceOf<ForbidResult>());
    }

    [Test]
    public void OnActionExecuting_FullToken_LeavesResultNull()
    {
        var context = MakeContext(MakeUser(UserId));

        new BlockScopedTokenAttribute().OnActionExecuting(context);

        Assert.That(context.Result, Is.Null);
    }
}
