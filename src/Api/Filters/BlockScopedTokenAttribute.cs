using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Api.Filters;

/// <summary>
/// Blocks requests that carry a scoped JWT (e.g. "change_password") and ensures
/// the NameIdentifier claim is present. Apply to any controller or action that
/// must only be reachable with a full, unscoped token.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class BlockScopedTokenAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var user = context.HttpContext.User;

        var scope = user.FindFirstValue("scope");
        if (scope == "change_password")
        {
            context.Result = new ForbidResult();
            return;
        }

        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            context.Result = new ForbidResult();
            return;
        }

        base.OnActionExecuting(context);
    }
}
