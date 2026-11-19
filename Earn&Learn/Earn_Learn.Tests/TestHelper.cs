using System.Security.Claims;
using Earn_Learn.Data;
using Earn_Learn.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Earn_Learn.Tests
{
    public static class TestHelper
    {
        public static ApplicationDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var context = new ApplicationDbContext(options);
            context.Database.EnsureCreated();
            return context;
        }

        public static Mock<UserManager<Korisnik>> CreateMockUserManager()
        {
            var store = new Mock<IUserStore<Korisnik>>();
            return new Mock<UserManager<Korisnik>>(
                store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        }

        public static void SetupControllerContext(Controller controller, Korisnik? user = null, bool isAjax = false)
        {
            var httpContext = new DefaultHttpContext();
            if (isAjax)
            {
                httpContext.Request.Headers["X-Requested-With"] = "XMLHttpRequest";
            }

            if (user != null)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id),
                    new Claim(ClaimTypes.Name, user.Email ?? user.UserName ?? "test@test.com")
                };
                httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
            }

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
            controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
        }
    }
}