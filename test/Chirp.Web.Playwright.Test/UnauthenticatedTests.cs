// Copyright (c) devops-gruppe-connie. All rights reserved.

using Chirp.Web.Playwright.Test;
using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;
using Xunit;

public class UnauthenticatedTests : PageTest, IClassFixture<PlaywrightFixture>, IAsyncLifetime
{
    private readonly PlaywrightFixture fixture;
    private readonly string baseURL;

    public UnauthenticatedTests(PlaywrightFixture fixture)
    {
        this.fixture = fixture;
        this.baseURL = fixture.BaseURL;
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await this.Page.GotoAsync(this.baseURL);
    }

    public override async Task DisposeAsync()
    {
        await this.fixture.ResetDatabaseAsync();
        await base.DisposeAsync();
    }

    [Fact]
    public async Task UsersCanRegister()
    {
        await this.Page.GetByRole(AriaRole.Link, new () { NameString = "Register" }).ClickAsync();
        await this.Page.WaitForURLAsync(new Regex("/Identity/Account/Register$"));

        // Username
        var usernameInput = this.Page.GetByLabel("Name");
        await usernameInput.ClickAsync();
        await this.Expect(usernameInput).ToBeFocusedAsync();
        await usernameInput.FillAsync("Cecilie");
        await this.Expect(usernameInput).ToHaveValueAsync("Cecilie");
        await this.Page.GetByLabel("Name").PressAsync("Tab");

        // Email
        var emailInput = this.Page.GetByPlaceholder("name@example.com");
        await emailInput.FillAsync("ceel@itu.dk");
        await this.Expect(emailInput).ToHaveValueAsync("ceel@itu.dk");

        // password
        // var passwordInput = _page.GetByRole(AriaRole.Textbox, new() { NameString = "Password" });
        var passwordInput = this.Page.Locator("input[id='Input_Password']");
        await passwordInput.ClickAsync();
        await passwordInput.FillAsync("Johan1234!");
        await this.Expect(passwordInput).ToHaveValueAsync("Johan1234!");
        await passwordInput.PressAsync("Tab");
        await this.Expect(passwordInput).Not.ToBeFocusedAsync();

        // var confirmPassword = _page.GetByLabel("Confirm Password");
        var confirmPassword = this.Page.Locator("input[id='Input_ConfirmPassword']");
        await confirmPassword.FillAsync("Johan1234!");
        await this.Expect(confirmPassword).ToHaveValueAsync("Johan1234!");

        // click on register button
        await this.Page.GetByRole(AriaRole.Button, new () { NameString = "Register" }).ClickAsync();
        await this.Expect(this.Page).ToHaveURLAsync(this.baseURL);
    }

    [Fact]
    public async Task UserCanRegisterAndLogin()
    {
        // first register user, because a new in memory database is created for each test.
        await this.Page.GetByRole(AriaRole.Link, new () { NameString = "Register" }).ClickAsync();
        await this.Page.WaitForURLAsync(new Regex("/Identity/Account/Register$"));
        await this.Page.GetByLabel("Name").ClickAsync();
        await this.Page.GetByLabel("Name").FillAsync("Cecilie");
        await this.Page.GetByLabel("Name").PressAsync("Tab");
        await this.Page.GetByPlaceholder("name@example.com").FillAsync("ceel@itu.dk");
        await this.Page.Locator("input[id='Input_Password']").ClickAsync();
        await this.Page.Locator("input[id='Input_Password']").FillAsync("Cecilie1234!");
        await this.Page.Locator("input[id='Input_Password']").PressAsync("Tab");
        await this.Page.Locator("input[id='Input_ConfirmPassword']").FillAsync("Cecilie1234!");
        await this.Page.GetByRole(AriaRole.Button, new () { NameString = "Register" }).ClickAsync();

        await this.Expect(this.Page).ToHaveURLAsync(this.baseURL);
        var loggedIn = this.Page.GetByText("What's on your mind");
        await this.Expect(loggedIn).ToBeVisibleAsync();
    }
}