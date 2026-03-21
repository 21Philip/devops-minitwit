// Copyright (c) devops-gruppe-connie. All rights reserved.

using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;
using Xunit;

namespace Chirp.Web.Playwright.Test;

public class AuthenticatedTests : PageTest, IClassFixture<PlaywrightFixture>, IAsyncLifetime
{
    private readonly PlaywrightFixture fixture;
    private readonly string baseURL;

    public AuthenticatedTests(PlaywrightFixture fixture)
    {
        this.fixture = fixture;
        this.baseURL = fixture.Server.BaseAddress.ToString();
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await this.Page.GotoAsync(this.baseURL);

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
        await this.Page.GetByText("What's on your mind Cecilie?").WaitForAsync();
    }

    public override async Task DisposeAsync()
    {
        await this.fixture.ResetDatabaseAsync();
        await base.DisposeAsync();
    }

    [Fact]
    public async Task UserCanShareCheepFromPublicTimeline()
    {
        // send cheep
        var cheepTextField = this.Page.Locator("input[id='Text']");
        await cheepTextField.ClickAsync();
        await this.Expect(cheepTextField).ToBeFocusedAsync();
        await cheepTextField.FillAsync("Hello, my group is the best group");
        await this.Expect(cheepTextField).ToHaveValueAsync("Hello, my group is the best group");
        await this.Page.GetByRole(AriaRole.Button, new () { NameString = "Share" }).ClickAsync();

        // check if there is a cheep with that text on the page after share button has been clicked.
        var cheep = this.Page.GetByText("Hello, my group is the best group");
        await cheep.HighlightAsync();

        await this.Expect(cheep).ToBeVisibleAsync();
        await this.Expect(this.Page).ToHaveURLAsync(new Regex(this.baseURL));
    }

    [Fact]
    public async Task UserCanShareCheepFromUserTimeline()
    {
        await this.Page.GetByRole(AriaRole.Link, new () { NameString = "my timeline" }).ClickAsync();
        await this.Expect(this.Page).ToHaveURLAsync(new Regex(this.baseURL + $"Cecilie"));

        // send cheep
        var cheepTextField = this.Page.Locator("input[id='Text']");
        await cheepTextField.ClickAsync();
        await this.Expect(cheepTextField).ToBeFocusedAsync();
        await cheepTextField.FillAsync("Hello, my name is Cecilie");
        await this.Expect(cheepTextField).ToHaveValueAsync("Hello, my name is Cecilie");
        await this.Page.GetByRole(AriaRole.Button, new () { NameString = "Share" }).ClickAsync();

        // check if there is a cheep with that text on the page after share button has been clicked.
        var cheep = this.Page.GetByText("Hello, my name is Cecilie");
        await cheep.HighlightAsync();

        await this.Expect(cheep).ToBeVisibleAsync();
        await this.Expect(this.Page).ToHaveURLAsync(new Regex(this.baseURL + $"Cecilie"));
    }

    [Fact]
    public async Task UserCanGoToMyTimelineByClickingOnMyTimeline()
    {
        await this.Page.GetByRole(AriaRole.Link, new () { NameString = "my timeline" }).ClickAsync();
        await this.Expect(this.Page).ToHaveURLAsync(new Regex(this.baseURL + $"Cecilie"));
    }

    [Fact]
    public async Task UserCanGoToPublicTimeline()
    {
        await this.Page.GetByRole(AriaRole.Link, new () { NameString = "public timeline" }).ClickAsync();
        await this.Expect(this.Page).ToHaveURLAsync(new Regex(this.baseURL));
    }

    [Fact]
    public async Task UserCanChangeAccountInformation()
    {
        // go to account
        await this.Page.GetByRole(AriaRole.Link, new () { NameString = "Account" }).ClickAsync();
        await this.Expect(this.Page).ToHaveURLAsync(new Regex(this.baseURL + $"Identity/Account/Manage"));

        // change username
        var usernameField = this.Page.GetByPlaceholder("Username");
        await usernameField.ClickAsync();
        await usernameField.FillAsync("JohanIngeholm");
        await this.Expect(usernameField).ToHaveValueAsync("JohanIngeholm");

        // enter phonenumber
        var phonenumberField = this.Page.GetByPlaceholder("Please enter your phone number.");
        await phonenumberField.ClickAsync();
        await phonenumberField.FillAsync("31690155");
        await this.Expect(phonenumberField).ToHaveValueAsync("31690155");

        // save changes
        await this.Page.GetByRole(AriaRole.Button, new () { NameString = "Save" }).ClickAsync();
        await this.Expect(this.Page).ToHaveURLAsync(new Regex(this.baseURL + $"Identity/Account/Manage"));

        // text with changes has been saved is visible on screen to illustrate save button has been pressed.
        var textSavings = this.Page.GetByText("Your profile has been updated");
        await textSavings.ClickAsync();
        await this.Expect(this.Page.Locator("text=Your profile has been updated")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task UserCanChangeEmail()
    {
        // go to account
        await this.Page.GetByRole(AriaRole.Link, new () { NameString = "Account" }).ClickAsync();
        await this.Expect(this.Page).ToHaveURLAsync(new Regex(this.baseURL + $"Identity/Account/Manage"));

        // go to email in account
        await this.Page.GetByRole(AriaRole.Link, new () { NameString = "Email" }).ClickAsync();
        await this.Expect(this.Page).ToHaveURLAsync(new Regex(this.baseURL + $"Identity/Account/Manage/Email"));

        // enter new email
        var emailField = this.Page.GetByPlaceholder("Please enter new email");
        await emailField.ClickAsync();
        await emailField.FillAsync("jing@itu.dk");
        await this.Expect(emailField).ToHaveValueAsync("jing@itu.dk");

        // change email button
        await this.Page.GetByRole(AriaRole.Button, new () { NameString = "Change email" }).ClickAsync();

        await this.Page.GetByRole(AriaRole.Link, new () { NameString = "Account" }).ClickAsync();
        await this.Expect(this.Page).ToHaveURLAsync(new Regex(this.baseURL + $"Identity/Account/Manage"));

        var emailFieldInAccount = this.Page.GetByPlaceholder("Email");
        await this.Expect(emailFieldInAccount).ToHaveValueAsync("jing@itu.dk");

        await this.Expect(this.Page).ToHaveURLAsync(new Regex(this.baseURL + $"Identity/Account/Manage"));
    }

    [Fact]
    public async Task UserCanLogOut()
    {
        await this.Page.GetByRole(AriaRole.Link, new () { NameString = "public timeline" }).ClickAsync();
        await this.Expect(this.Page).ToHaveURLAsync(this.baseURL);

        // user can log out
        await this.Page.GetByRole(AriaRole.Button, new () { NameString = "Logout" }).ClickAsync();
        await this.Expect(this.Page).ToHaveURLAsync(new Regex(this.baseURL + $"Identity/Account/Logout"));
    }

    [Fact]
    public async Task FollowAndUnfollowOnPublicTimeline()
    {
        // find the follow-button for a specific cheep
        var followButton = this.Page.Locator("li").Filter(new ()
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

    [Fact]
    public async Task GoToAnotherUsersTimelineAndFollowAndUnfollow()
    {
        // go to another user's timeline
        var userTimelinePage = this.Page.GetByRole(AriaRole.Link, new () { Name = "Jacqualine Gilcoine" }).First;

        await userTimelinePage.ClickAsync();
        await this.Expect(this.Page).ToHaveURLAsync(new Regex(this.baseURL + $"Jacqualine"));

        // find the follow-button for a specific cheep
        var followButton = this.Page.Locator("li").Filter(new ()
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

    [Fact]
    public async Task GoToAnotherUsersTimelineAndSeeFirst32CheepsWrittenByThatAuthor()
    {
        // go to another user's timeline
        var userTimelinePage = this.Page.GetByRole(AriaRole.Link, new () { Name = "Jacqualine Gilcoine" }).First;

        await userTimelinePage.ClickAsync();
        await this.Expect(this.Page).ToHaveURLAsync(new Regex(this.baseURL + $"Jacqualine"));

        var cheeps = this.Page.Locator("li").Filter(new ()
        {
            HasText = "Jacqualine Gilcoine",
        }).GetByRole(AriaRole.Link);

        // Assert that there are exactly 32 elements
        await this.Expect(cheeps).ToHaveCountAsync(32);
    }

    [Fact]
    public async Task CheckCharCountOnWritingCheeps()
    {
        // go to my timeline
        await this.Page.GetByRole(AriaRole.Link, new () { Name = "My timeline" }).ClickAsync();
        await this.Expect(this.Page).ToHaveURLAsync(new Regex(this.baseURL + $"Cecilie"));

        // click on text field to write cheep and write a cheep.
        var cheepTextField = this.Page.Locator("input[id='Text']");
        await cheepTextField.ClickAsync();
        await this.Expect(cheepTextField).ToBeFocusedAsync();
        await cheepTextField.FillAsync("Hello, my name is Cecilie");
        await this.Expect(cheepTextField).ToHaveValueAsync("Hello, my name is Cecilie");

        // see charcount label increase to 25
        var charCountSpan = this.Page.Locator("span[id='charCount']");
        await this.Expect(charCountSpan).ToHaveTextAsync("25/160");
    }

    [Fact]
    public async Task SearchForUserAndFollow()
    {
        // search for author
        var searchField = this.Page.GetByPlaceholder("Search authors...");
        await searchField.ClickAsync();
        await searchField.FillAsync("Mellie");
        await this.Page.GetByRole(AriaRole.Button, new () { Name = "Search" }).ClickAsync();

        // show search results and click on user
        await this.Expect(this.Page).ToHaveURLAsync(new Regex(this.baseURL + $"SearchResults"));
        await this.Page.GetByRole(AriaRole.Link, new () { Name = "Mellie Yost" }).ClickAsync();
        await this.Expect(this.Page).ToHaveURLAsync(new Regex(this.baseURL + $"Mellie"));

        var followButton = this.Page.Locator("li").Filter(new ()
        {
            HasText = "But what was behind the barricade",
        }).GetByRole(AriaRole.Button, new () { NameString = "Follow" });

        // follow author
        await this.Expect(followButton).ToHaveTextAsync("Follow");
        await followButton.ClickAsync();
        await this.Expect(followButton).ToHaveTextAsync("Unfollow");
    }

    [Fact]
    public async Task UnfollowOnUserTimeline()
    {
        // find the follow-button for a specific cheep
        var followButton = this.Page.Locator("li").Filter(new ()
        {
            HasText = "Coffee House now is what we hear the worst.",
        }).GetByRole(AriaRole.Button, new () { NameString = "Follow" });

        // follow author
        await this.Expect(followButton).ToHaveTextAsync("Follow");
        await followButton.ClickAsync();
        await this.Expect(followButton).ToHaveTextAsync("Unfollow");

        // go to my timeline
        await this.Page.GetByRole(AriaRole.Link, new () { NameString = "my timeline" }).ClickAsync();
        await this.Expect(this.Page).ToHaveURLAsync(new Regex(this.baseURL + $"Cecilie"));

        // locate cheep from the author we just followed
        await this.Expect(this.Page.Locator("li:has-text('Coffee House now is what we hear the worst.')")).ToBeVisibleAsync();
        await this.Expect(followButton).ToHaveTextAsync("Unfollow");

        // unfollow author
        await followButton.ClickAsync();
        await this.Expect(followButton).ToBeHiddenAsync();
        await this.Expect(this.Page.Locator("text=There are no cheeps so far.")).ToBeVisibleAsync();

        // go back to public timeline to check the unfollow-button has changed back to follow
        await this.Page.GetByRole(AriaRole.Link, new () { NameString = "public timeline" }).ClickAsync();
        await this.Expect(this.Page).ToHaveURLAsync(this.baseURL);
        await this.Expect(followButton).ToHaveTextAsync("Follow");
    }

    [Fact]
    public async Task GoToNextPageWithButton()
    {
        var nextButton = this.Page.GetByRole(AriaRole.Link, new () { Name = "Next" });
        await nextButton.ClickAsync();
        await this.Expect(this.Page).ToHaveURLAsync(new Regex($"{this.baseURL}\\?page=2"));

        var previousButton = this.Page.GetByRole(AriaRole.Link, new () { Name = "Previous" });
        await this.Expect(previousButton).ToBeVisibleAsync();
    }

    [Fact]
    public async Task UserCanDeleteTheirAccount()
    {
        // go to about me page
        await this.Page.GetByRole(AriaRole.Link, new () { Name = "About Me" }).ClickAsync();
        await this.Expect(this.Page).ToHaveURLAsync(new Regex(this.baseURL + $"Identity/Account/Manage/PersonalData"));

        // click forget me
        await this.Page.GetByRole(AriaRole.Link, new () { Name = "Forget me" }).ClickAsync();
        await this.Expect(this.Page).ToHaveURLAsync(new Regex(this.baseURL + $"Identity/Account/Manage/DeletePersonalData"));

        // confirm delete data and close account
        var passwordInput = this.Page.GetByPlaceholder("Please enter your password");
        await passwordInput.ClickAsync();
        await passwordInput.FillAsync("Cecilie1234!");
        await this.Expect(passwordInput).ToHaveValueAsync("Cecilie1234!");
        await this.Page.GetByRole(AriaRole.Button, new () { Name = "Delete data and close my" }).ClickAsync();
        await this.Expect(this.Page).ToHaveURLAsync(this.baseURL);
    }

    [Fact]
    public async Task UserCanLikeAndUnlikeOtherCheepsOnPublicTimeline()
    {
        var likeButton = this.Page.Locator("li").Filter(new ()
        {
            HasText = "Jacqualine Gilcoine Follow Coffee House now is what we hear the worst. — 2023-",
        }).Locator("button.like-button-not-liked");

        var likeCount = this.Page.Locator("li").Filter(new ()
        {
            HasText = "Jacqualine Gilcoine Follow Coffee House now is what we hear the worst. — 2023-",
        }).Locator(".like-button-container span").Nth(1);

        var unLikeButton = this.Page.Locator("li").Filter(new ()
        {
            HasText = "Jacqualine Gilcoine Follow Coffee House now is what we hear the worst. — 2023-",
        }).Locator("button.like-button-liked");

        await this.Expect(likeCount).ToHaveTextAsync("0");
        await likeButton.ClickAsync();
        await this.Expect(likeCount).ToHaveTextAsync("1");
        await unLikeButton.ClickAsync();
        await this.Expect(likeCount).ToHaveTextAsync("0");
    }

    [Fact]
    public async Task UserCanFollowAndLikeOtherCheepsOnMyTimeline()
    {
        // find the follow-button for a specific cheep
        var followButton = this.Page.Locator("li").Filter(new ()
        {
            HasText = "Coffee House now is what we hear the worst.",
        }).GetByRole(AriaRole.Button, new () { NameString = "Follow" });

        // follow author
        await this.Expect(followButton).ToHaveTextAsync("Follow");
        await followButton.ClickAsync();
        await this.Expect(followButton).ToHaveTextAsync("Unfollow");

        // go to my timeline
        await this.Page.GetByRole(AriaRole.Link, new () { Name = "My timeline" }).ClickAsync();
        await this.Expect(this.Page).ToHaveURLAsync(new Regex(this.baseURL + $"Cecilie"));

        var likeButton = this.Page.Locator("li").Filter(new ()
        {
            HasText = "Jacqualine Gilcoine Unfollow Once, I remember, to be a rock, but it is this",
        }).GetByRole(AriaRole.Button).Nth(1);

        var likeCount0 = this.Page.GetByText("0", new () { Exact = true }).Nth(3);
        var likeCount1 = this.Page.GetByText("1", new () { Exact = true });

        var unLikeButton = this.Page.Locator("li").Filter(new ()
        {
            HasText = "Jacqualine Gilcoine Unfollow Once, I remember, to be a rock, but it is this",
        }).GetByRole(AriaRole.Button).Nth(1);

        await this.Expect(likeCount0).ToHaveTextAsync("0");
        await likeButton.ClickAsync();
        await this.Expect(likeCount1).ToHaveTextAsync("1");
        await unLikeButton.ClickAsync();
        await this.Expect(likeCount0).ToHaveTextAsync("0");
    }
}