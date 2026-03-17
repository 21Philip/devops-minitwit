// Copyright (c) devops-gruppe-connie. All rights reserved.

using Microsoft.Playwright;
using Xunit;

namespace Chirp.Web.Playwright.Test;

[TestFixture]
[NonParallelizable]
public class UiTests : PageTest, IClassFixture<PlaywrightFixture>, IDisposable
{
    private IBrowserContext? context;
    private IBrowser? browser;
    private PlaywrightFixture fixture;
    private string serverAddress;
    private IPlaywright playwright;
    private IPage page = null!;

    [SetUp]
    public async Task SetUp()
    {
        this.fixture = new PlaywrightFixture();
        this.serverAddress = this.fixture.ServerAddress;

        await this.InitializeBrowserAndCreateBrowserContextAsync();

        var test = TestContext.CurrentContext.Test;

        // Check if the test is marked with the "SkipSetUp" category
        if (!test.Properties["Category"].Contains("SkipSetUp"))
        {
            await this.SetUpRegisterAndLogin();
        }
    }

    [TearDown]
    public void TearDown()
    {
        this.Dispose();
    }

    [Test]
    [Category("SkipSetUp")]
    public async Task UsersCanRegister()
    {
        this.page = await this.context!.NewPageAsync();
        await this.page.GotoAsync(this.serverAddress);

        await this.page.GetByRole(AriaRole.Link, new () { NameString = "Register" }).ClickAsync();
        await this.page.WaitForURLAsync(new Regex("/Identity/Account/Register$"));

        // Username
        var usernameInput = this.page.GetByLabel("Name");
        await usernameInput.ClickAsync();
        await this.Expect(usernameInput).ToBeFocusedAsync();
        await usernameInput.FillAsync("Cecilie");
        await this.Expect(usernameInput).ToHaveValueAsync("Cecilie");
        await this.page.GetByLabel("Name").PressAsync("Tab");

        // Email
        var emailInput = this.page.GetByPlaceholder("name@example.com");
        await emailInput.FillAsync("ceel@itu.dk");
        await this.Expect(emailInput).ToHaveValueAsync("ceel@itu.dk");

        // password
        // var passwordInput = _page.GetByRole(AriaRole.Textbox, new() { NameString = "Password" });
        var passwordInput = this.page.Locator("input[id='Input_Password']");
        await passwordInput.ClickAsync();
        await passwordInput.FillAsync("Johan1234!");
        await this.Expect(passwordInput).ToHaveValueAsync("Johan1234!");
        await passwordInput.PressAsync("Tab");
        await this.Expect(passwordInput).Not.ToBeFocusedAsync();

        // var confirmPassword = _page.GetByLabel("Confirm Password");
        var confirmPassword = this.page.Locator("input[id='Input_ConfirmPassword']");
        await confirmPassword.FillAsync("Johan1234!");
        await this.Expect(confirmPassword).ToHaveValueAsync("Johan1234!");

        // click on register button
        await this.page.GetByRole(AriaRole.Button, new () { NameString = "Register" }).ClickAsync();
        await this.Expect(this.page).ToHaveURLAsync(this.serverAddress);
    }

    [Test]
    [Category("SkipSetUp")]
    public async Task UserCanRegisterAndLogin()
    {
        // go to base server address
        this.page = await this.context!.NewPageAsync();
        await this.page.GotoAsync(this.serverAddress);

        // first register user, because a new in memory database is created for each test.
        await this.page.GetByRole(AriaRole.Link, new () { NameString = "Register" }).ClickAsync();
        await this.page.WaitForURLAsync(new Regex("/Identity/Account/Register$"));
        await this.page.GetByLabel("Name").ClickAsync();
        await this.page.GetByLabel("Name").FillAsync("Cecilie");
        await this.page.GetByLabel("Name").PressAsync("Tab");
        await this.page.GetByPlaceholder("name@example.com").FillAsync("ceel@itu.dk");
        await this.page.Locator("input[id='Input_Password']").ClickAsync();
        await this.page.Locator("input[id='Input_Password']").FillAsync("Cecilie1234!");
        await this.page.Locator("input[id='Input_Password']").PressAsync("Tab");
        await this.page.Locator("input[id='Input_ConfirmPassword']").FillAsync("Cecilie1234!");
        await this.page.GetByRole(AriaRole.Button, new () { NameString = "Register" }).ClickAsync();

        await this.Expect(this.page).ToHaveURLAsync(this.serverAddress);
        var loggedIn = this.page.GetByText("What's on your mind");
        await this.Expect(loggedIn).ToBeVisibleAsync();
    }

    [Test]
    public async Task UserCanShareCheepFromPublicTimeline()
    {
        // send cheep
        var cheepTextField = this.page.Locator("input[id='Text']");
        await cheepTextField.ClickAsync();
        await this.Expect(cheepTextField).ToBeFocusedAsync();
        await cheepTextField.FillAsync("Hello, my group is the best group");
        await this.Expect(cheepTextField).ToHaveValueAsync("Hello, my group is the best group");
        await this.page.GetByRole(AriaRole.Button, new () { NameString = "Share" }).ClickAsync();

        // check if there is a cheep with that text on the page after share button has been clicked.
        var cheep = this.page.GetByText("Hello, my group is the best group");
        await cheep.HighlightAsync();

        await this.Expect(cheep).ToBeVisibleAsync();
        await this.Expect(this.page).ToHaveURLAsync(new Regex(this.serverAddress));
    }

    [Test]
    public async Task UserCanShareCheepFromUserTimeline()
    {
        await this.page.GetByRole(AriaRole.Link, new () { NameString = "my timeline" }).ClickAsync();
        await this.Expect(this.page).ToHaveURLAsync(new Regex(this.serverAddress + $"Cecilie"));

        // send cheep
        var cheepTextField = this.page.Locator("input[id='Text']");
        await cheepTextField.ClickAsync();
        await this.Expect(cheepTextField).ToBeFocusedAsync();
        await cheepTextField.FillAsync("Hello, my name is Cecilie");
        await this.Expect(cheepTextField).ToHaveValueAsync("Hello, my name is Cecilie");
        await this.page.GetByRole(AriaRole.Button, new () { NameString = "Share" }).ClickAsync();

        // check if there is a cheep with that text on the page after share button has been clicked.
        var cheep = this.page.GetByText("Hello, my name is Cecilie");
        await cheep.HighlightAsync();

        await this.Expect(cheep).ToBeVisibleAsync();
        await this.Expect(this.page).ToHaveURLAsync(new Regex(this.serverAddress + $"Cecilie"));
    }

    [Test]
    public async Task UserCanGoToMyTimelineByClickingOnMyTimeline()
    {
        await this.page.GetByRole(AriaRole.Link, new () { NameString = "my timeline" }).ClickAsync();
        await this.Expect(this.page).ToHaveURLAsync(new Regex(this.serverAddress + $"Cecilie"));
    }

    [Test]
    public async Task UserCanGoToPublicTimeline()
    {
        await this.page.GetByRole(AriaRole.Link, new () { NameString = "public timeline" }).ClickAsync();
        await this.Expect(this.page).ToHaveURLAsync(new Regex(this.serverAddress));
    }

    [Test]
    public async Task UserCanChangeAccountInformation()
    {
        // go to account
        await this.page.GetByRole(AriaRole.Link, new () { NameString = "Account" }).ClickAsync();
        await this.Expect(this.page).ToHaveURLAsync(new Regex(this.serverAddress + $"Identity/Account/Manage"));

        // change username
        var usernameField = this.page.GetByPlaceholder("Username");
        await usernameField.ClickAsync();
        await usernameField.FillAsync("JohanIngeholm");
        await this.Expect(usernameField).ToHaveValueAsync("JohanIngeholm");

        // enter phonenumber
        var phonenumberField = this.page.GetByPlaceholder("Please enter your phone number.");
        await phonenumberField.ClickAsync();
        await phonenumberField.FillAsync("31690155");
        await this.Expect(phonenumberField).ToHaveValueAsync("31690155");

        // save changes
        await this.page.GetByRole(AriaRole.Button, new () { NameString = "Save" }).ClickAsync();
        await this.Expect(this.page).ToHaveURLAsync(new Regex(this.serverAddress + $"Identity/Account/Manage"));

        // text with changes has been saved is visible on screen to illustrate save button has been pressed.
        var textSavings = this.page.GetByText("Your profile has been updated");
        await textSavings.ClickAsync();
        await this.Expect(this.page.Locator("text=Your profile has been updated")).ToBeVisibleAsync();
    }

    [Test]
    public async Task UserCanChangeEmail()
    {
        // go to account
        await this.page.GetByRole(AriaRole.Link, new () { NameString = "Account" }).ClickAsync();
        await this.Expect(this.page).ToHaveURLAsync(new Regex(this.serverAddress + $"Identity/Account/Manage"));

        // go to email in account
        await this.page.GetByRole(AriaRole.Link, new () { NameString = "Email" }).ClickAsync();
        await this.Expect(this.page).ToHaveURLAsync(new Regex(this.serverAddress + $"Identity/Account/Manage/Email"));

        // enter new email
        var emailField = this.page.GetByPlaceholder("Please enter new email");
        await emailField.ClickAsync();
        await emailField.FillAsync("jing@itu.dk");
        await this.Expect(emailField).ToHaveValueAsync("jing@itu.dk");

        // change email button
        await this.page.GetByRole(AriaRole.Button, new () { NameString = "Change email" }).ClickAsync();

        await this.page.GetByRole(AriaRole.Link, new () { NameString = "Account" }).ClickAsync();
        await this.Expect(this.page).ToHaveURLAsync(new Regex(this.serverAddress + $"Identity/Account/Manage"));

        var emailFieldInAccount = this.page.GetByPlaceholder("Email");
        await this.Expect(emailFieldInAccount).ToHaveValueAsync("jing@itu.dk");

        await this.Expect(this.page).ToHaveURLAsync(new Regex(this.serverAddress + $"Identity/Account/Manage"));
    }

    [Test]
    public async Task UserCanLogOut()
    {
        await this.page.GetByRole(AriaRole.Link, new () { NameString = "public timeline" }).ClickAsync();
        await this.Expect(this.page).ToHaveURLAsync(this.serverAddress);

        // user can log out
        await this.page.GetByRole(AriaRole.Button, new () { NameString = "Logout" }).ClickAsync();
        await this.Expect(this.page).ToHaveURLAsync(new Regex(this.serverAddress + $"Identity/Account/Logout"));
    }

    [Test]
    public async Task FollowAndUnfollowOnPublicTimeline()
    {
        // find the follow-button for a specific cheep
        var followButton = this.page.Locator("li").Filter(new ()
        {
            HasText = "Coffee House now is what we hear the worst.",
        }).GetByRole(AriaRole.Button, new () { NameString = "Follow" });

        // follow author
        await this.Expect(followButton).ToHaveTextAsync("Follow");
        await followButton.ClickAsync();
        await this.Expect(followButton).ToHaveTextAsync("Unfollow");

        // unfollow author
        await followButton.ClickAsync();
        await this.Expect(followButton).ToHaveTextAsync("Follow");
    }

    [Test]
    public async Task GoToAnotherUsersTimelineAndFollowAndUnfollow()
    {
        // go to another user's timeline
        var userTimelinePage = this.page.GetByRole(AriaRole.Link, new () { Name = "Jacqualine Gilcoine" }).First;

        await userTimelinePage.ClickAsync();
        await this.Expect(this.page).ToHaveURLAsync(new Regex(this.serverAddress + $"Jacqualine"));

        // find the follow-button for a specific cheep
        var followButton = this.page.Locator("li").Filter(new ()
        {
            HasText = "Coffee House now is what we hear the worst.",
        }).GetByRole(AriaRole.Button, new () { NameString = "Follow" });

        // follow author
        await this.Expect(followButton).ToHaveTextAsync("Follow");
        await followButton.ClickAsync();
        await this.Expect(followButton).ToHaveTextAsync("Unfollow");

        // unfollow author
        await followButton.ClickAsync();
        await this.Expect(followButton).ToHaveTextAsync("Follow");
    }

    [Test]
    public async Task GoToAnotherUsersTimelineAndSeeFirst32CheepsWrittenByThatAuthor()
    {
        // go to another user's timeline
        var userTimelinePage = this.page.GetByRole(AriaRole.Link, new () { Name = "Jacqualine Gilcoine" }).First;

        await userTimelinePage.ClickAsync();
        await this.Expect(this.page).ToHaveURLAsync(new Regex(this.serverAddress + $"Jacqualine"));

        var cheeps = this.page.Locator("li").Filter(new ()
        {
            HasText = "Jacqualine Gilcoine",
        }).GetByRole(AriaRole.Link);

        // Assert that there are exactly 32 elements
        await this.Expect(cheeps).ToHaveCountAsync(32);
    }

    [Test]
    public async Task CheckCharCountOnWritingCheeps()
    {
        // go to my timeline
        await this.page.GetByRole(AriaRole.Link, new () { Name = "My timeline" }).ClickAsync();
        await this.Expect(this.page).ToHaveURLAsync(new Regex(this.serverAddress + $"Cecilie"));

        // click on text field to write cheep and write a cheep.
        var cheepTextField = this.page.Locator("input[id='Text']");
        await cheepTextField.ClickAsync();
        await this.Expect(cheepTextField).ToBeFocusedAsync();
        await cheepTextField.FillAsync("Hello, my name is Cecilie");
        await this.Expect(cheepTextField).ToHaveValueAsync("Hello, my name is Cecilie");

        // see charcount label increase to 25
        var charCountSpan = this.page.Locator("span[id='charCount']");
        await this.Expect(charCountSpan).ToHaveTextAsync("25/160");
    }

    [Test]
    public async Task SearchForUserAndFollow()
    {
        // search for author
        var searchField = this.page.GetByPlaceholder("Search authors...");
        await searchField.ClickAsync();
        await searchField.FillAsync("Mellie");
        await this.page.GetByRole(AriaRole.Button, new () { Name = "Search" }).ClickAsync();

        // show search results and click on user
        await this.Expect(this.page).ToHaveURLAsync(new Regex(this.serverAddress + $"SearchResults"));
        await this.page.GetByRole(AriaRole.Link, new () { Name = "Mellie Yost" }).ClickAsync();
        await this.Expect(this.page).ToHaveURLAsync(new Regex(this.serverAddress + $"Mellie"));

        var followButton = this.page.Locator("li").Filter(new ()
        {
            HasText = "But what was behind the barricade",
        }).GetByRole(AriaRole.Button, new () { NameString = "Follow" });

        // follow author
        await this.Expect(followButton).ToHaveTextAsync("Follow");
        await followButton.ClickAsync();
        await this.Expect(followButton).ToHaveTextAsync("Unfollow");
    }

    [Test]
    public async Task UnfollowOnUserTimeline()
    {
        // find the follow-button for a specific cheep
        var followButton = this.page.Locator("li").Filter(new ()
        {
            HasText = "Coffee House now is what we hear the worst.",
        }).GetByRole(AriaRole.Button, new () { NameString = "Follow" });

        // follow author
        await this.Expect(followButton).ToHaveTextAsync("Follow");
        await followButton.ClickAsync();
        await this.Expect(followButton).ToHaveTextAsync("Unfollow");

        // go to my timeline
        await this.page.GetByRole(AriaRole.Link, new () { NameString = "my timeline" }).ClickAsync();
        await this.Expect(this.page).ToHaveURLAsync(new Regex(this.serverAddress + $"Cecilie"));

        // locate cheep from the author we just followed
        await this.Expect(this.page.Locator("li:has-text('Coffee House now is what we hear the worst.')")).ToBeVisibleAsync();
        await this.Expect(followButton).ToHaveTextAsync("Unfollow");

        // unfollow author
        await followButton.ClickAsync();
        await this.Expect(followButton).ToBeHiddenAsync();
        await this.Expect(this.page.Locator("text=There are no cheeps so far.")).ToBeVisibleAsync();

        // go back to public timeline to check the unfollow-button has changed back to follow
        await this.page.GetByRole(AriaRole.Link, new () { NameString = "public timeline" }).ClickAsync();
        await this.Expect(this.page).ToHaveURLAsync(this.serverAddress);
        await this.Expect(followButton).ToHaveTextAsync("Follow");
    }

    [Test]
    public async Task GoToNextPageWithButton()
    {
        var nextButton = this.page.GetByRole(AriaRole.Link, new () { Name = "Next" });
        await nextButton.ClickAsync();
        await this.Expect(this.page).ToHaveURLAsync(new Regex($"{this.serverAddress}\\?page=2"));

        var previousButton = this.page.GetByRole(AriaRole.Link, new () { Name = "Previous" });
        await this.Expect(previousButton).ToBeVisibleAsync();
    }

    [Test]
    public async Task UserCanDeleteTheirAccount()
    {
        // go to about me page
        await this.page.GetByRole(AriaRole.Link, new () { Name = "About Me" }).ClickAsync();
        await this.Expect(this.page).ToHaveURLAsync(new Regex(this.serverAddress + $"Identity/Account/Manage/PersonalData"));

        // click forget me
        await this.page.GetByRole(AriaRole.Link, new () { Name = "Forget me" }).ClickAsync();
        await this.Expect(this.page).ToHaveURLAsync(new Regex(this.serverAddress + $"Identity/Account/Manage/DeletePersonalData"));

        // confirm delete data and close account
        var passwordInput = this.page.GetByPlaceholder("Please enter your password");
        await passwordInput.ClickAsync();
        await passwordInput.FillAsync("Cecilie1234!");
        await this.Expect(passwordInput).ToHaveValueAsync("Cecilie1234!");
        await this.page.GetByRole(AriaRole.Button, new () { Name = "Delete data and close my" }).ClickAsync();
        await this.Expect(this.page).ToHaveURLAsync(this.serverAddress);
    }

    [Test]
    public async Task UserCanLikeAndUnlikeOtherCheepsOnPublicTimeline()
    {
        var likeButton = this.page.Locator("li").Filter(new ()
        {
            HasText = "Jacqualine Gilcoine Follow Coffee House now is what we hear the worst. — 2023-",
        }).Locator("button.like-button-not-liked");

        var likeCount = this.page.Locator("li").Filter(new ()
        {
            HasText = "Jacqualine Gilcoine Follow Coffee House now is what we hear the worst. — 2023-",
        }).Locator(".like-button-container span").Nth(1);

        var unLikeButton = this.page.Locator("li").Filter(new ()
        {
            HasText = "Jacqualine Gilcoine Follow Coffee House now is what we hear the worst. — 2023-",
        }).Locator("button.like-button-liked");

        await this.Expect(likeCount).ToHaveTextAsync("0");
        await likeButton.ClickAsync();
        await this.Expect(likeCount).ToHaveTextAsync("1");
        await unLikeButton.ClickAsync();
        await this.Expect(likeCount).ToHaveTextAsync("0");
    }

    [Test]
    public async Task UserCanFollowAndLikeOtherCheepsOnMyTimeline()
    {
        // find the follow-button for a specific cheep
        var followButton = this.page.Locator("li").Filter(new ()
        {
            HasText = "Coffee House now is what we hear the worst.",
        }).GetByRole(AriaRole.Button, new () { NameString = "Follow" });

        // follow author
        await this.Expect(followButton).ToHaveTextAsync("Follow");
        await followButton.ClickAsync();
        await this.Expect(followButton).ToHaveTextAsync("Unfollow");

        // go to my timeline
        await this.page.GetByRole(AriaRole.Link, new () { Name = "My timeline" }).ClickAsync();
        await this.Expect(this.page).ToHaveURLAsync(new Regex(this.serverAddress + $"Cecilie"));

        var likeButton = this.page.Locator("li").Filter(new ()
        {
            HasText = "Jacqualine Gilcoine Unfollow Once, I remember, to be a rock, but it is this",
        }).GetByRole(AriaRole.Button).Nth(1);

        var likeCount0 = this.page.GetByText("0", new () { Exact = true }).Nth(3);
        var likeCount1 = this.page.GetByText("1", new () { Exact = true });

        var unLikeButton = this.page.Locator("li").Filter(new ()
        {
            HasText = "Jacqualine Gilcoine Unfollow Once, I remember, to be a rock, but it is this",
        }).GetByRole(AriaRole.Button).Nth(1);

        await this.Expect(likeCount0).ToHaveTextAsync("0");
        await likeButton.ClickAsync();
        await this.Expect(likeCount1).ToHaveTextAsync("1");
        await unLikeButton.ClickAsync();
        await this.Expect(likeCount0).ToHaveTextAsync("0");
    }

    // dispose browser and context after each test

    /// <inheritdoc/>
    public void Dispose()
    {
        this.context?.DisposeAsync().GetAwaiter().GetResult();
        this.browser?.DisposeAsync().GetAwaiter().GetResult();
    }

    private async Task SetUpRegisterAndLogin()
    {
        this.page = await this.context!.NewPageAsync();
        await this.page.GotoAsync(this.serverAddress);

        // first register user, because a new in memory database is created for each test.
        await this.page.GetByRole(AriaRole.Link, new () { NameString = "Register" }).ClickAsync();
        await this.page.WaitForURLAsync(new Regex("/Identity/Account/Register$"));
        await this.page.GetByLabel("Name").ClickAsync();
        await this.page.GetByLabel("Name").FillAsync("Cecilie");
        await this.page.GetByLabel("Name").PressAsync("Tab");
        await this.page.GetByPlaceholder("name@example.com").FillAsync("ceel@itu.dk");
        await this.page.Locator("input[id='Input_Password']").ClickAsync();
        await this.page.Locator("input[id='Input_Password']").FillAsync("Cecilie1234!");
        await this.page.Locator("input[id='Input_Password']").PressAsync("Tab");
        await this.page.Locator("input[id='Input_ConfirmPassword']").FillAsync("Cecilie1234!");
        await this.page.GetByRole(AriaRole.Button, new () { NameString = "Register" }).ClickAsync();
        await this.page.GetByText("What's on your mind Cecilie?").WaitForAsync();
    }

    private async Task InitializeBrowserAndCreateBrowserContextAsync()
    {
        this.playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        this.browser = await this.playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true, // Set to false if you want to see the browser
        });

        this.context = await this.browser.NewContextAsync(new BrowserNewContextOptions());
    }
}