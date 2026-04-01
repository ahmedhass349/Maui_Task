using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;

namespace Maui_Task.Tests;

public sealed class WebSmokeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public WebSmokeTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task SwaggerJson_IsServed()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        using var response = await client.GetAsync("/swagger/v1/swagger.json");

        response.EnsureSuccessStatusCode();
    }

    [Theory]
    [InlineData(typeof(Maui_Task.Shared.Pages.Settings))]
    [InlineData(typeof(Maui_Task.Shared.Pages.Message))]
    [InlineData(typeof(Maui_Task.Shared.Pages.Notifications))]
    [InlineData(typeof(Maui_Task.Shared.Pages.Chatbot))]
    public void ProtectedPages_AreAuthorizeGated(Type pageType)
    {
        Assert.Contains(pageType.GetCustomAttributes(inherit: true), attribute => attribute is AuthorizeAttribute);
    }

    [Theory]
    [InlineData(typeof(Maui_Task.Shared.Pages.Login))]
    [InlineData(typeof(Maui_Task.Shared.Pages.Signup))]
    [InlineData(typeof(Maui_Task.Shared.Pages.ForgotPassword))]
    [InlineData(typeof(Maui_Task.Shared.Pages.ResetPassword))]
    [InlineData(typeof(Maui_Task.Shared.Pages.ResetPasswordEmailMessage))]
    public void AuthPages_AreAllowAnonymous(Type pageType)
    {
        Assert.Contains(pageType.GetCustomAttributes(inherit: true), attribute => attribute is AllowAnonymousAttribute);
    }
}
