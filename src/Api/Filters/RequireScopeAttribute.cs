using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Api.Filters;

/// <summary>
/// Requires the JWT to carry a specific scope claim value. Blocks any token
/// that does not match — including full, unscoped tokens. Mirror of
/// BlockScopedTokenAttribute: use on actions that are only reachable via a
/// scoped token (e.g. the mandatory-password-change flow).
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class RequireScopeAttribute(string scope) : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var user = context.HttpContext.User;

        if (user.FindFirstValue("scope") != scope)
        {
            context.Result = new ForbidResult();
            return;
        }

        if (user.FindFirstValue(ClaimTypes.NameIdentifier) is null)
        {
            context.Result = new ForbidResult();
            return;
        }

        base.OnActionExecuting(context);
    }
}
