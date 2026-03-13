
[SetUpFixture]
public class PlaywrightSetup
{
    [OneTimeSetUp]
    public void InstallPlaywright()
    {
        // Installs browsers needed by this project
        var exitCode = Microsoft.Playwright.Program.Main(new[] { "install", "chromium", "--with-deps" });
        if (exitCode != 0)
            throw new Exception($"Playwright install failed with exit code {exitCode}");
    }
}