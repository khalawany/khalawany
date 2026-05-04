using KhalawanyTube.Models;
using Microsoft.AspNetCore.Identity;

namespace KhalawanyTube.Middleware;

public class BlockedUserMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(
        HttpContext ctx,
        UserManager<ApplicationUser> users,
        SignInManager<ApplicationUser> signIn)
    {
        if (ctx.User.Identity?.IsAuthenticated == true)
        {
            var user = await users.GetUserAsync(ctx.User);
            if (user?.IsBlocked == true)
            {
                await signIn.SignOutAsync();
                ctx.Response.Redirect("/Account/Login");
                return;
            }
        }

        await next(ctx);
    }
}
