using Microsoft.Playwright;
using System.Text;
using System.Text.RegularExpressions;

const string SiteUrl = "https://ticket.hc-avto.ru/ru/";

var maxTickets = GetMaxTickets(args);
var headed = args.Contains("--headed", StringComparer.OrdinalIgnoreCase);
var email = GetSetting("TICKET_EMAIL", "Email: ");
var password = GetSetting("TICKET_PASSWORD", "Password: ", isSecret: true);

if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
{
    Console.Error.WriteLine("Email and password are required to run TicketScraper.");
    return 2;
}

await EnsureChromiumInstalledAsync();

using var playwright = await Playwright.CreateAsync();
await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
{
    Headless = !headed,
    SlowMo = headed ? 200 : 0
});

var page = await browser.NewPageAsync(new BrowserNewPageOptions
{
    Locale = "ru-RU",
    ViewportSize = new ViewportSize { Width = 1366, Height = 900 }
});
page.SetDefaultTimeout(30_000);

await page.GotoAsync(SiteUrl, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
await LoginAsync(page, email, password);
await OpenMyTicketsAsync(page);
var tickets = await ExtractTicketsAsync(page, maxTickets);

if (tickets.Count == 0)
{
    Console.WriteLine("Купленные билеты не найдены или разметка страницы изменилась.");
    return 0;
}

Console.WriteLine($"Последние купленные билеты (до {maxTickets}):");
for (var i = 0; i < tickets.Count; i++)
{
    Console.WriteLine($"\n#{i + 1}");
    Console.WriteLine(tickets[i]);
}

return 0;

static string? GetSetting(string environmentVariable, string prompt, bool isSecret = false)
{
    var value = Environment.GetEnvironmentVariable(environmentVariable);
    if (!string.IsNullOrWhiteSpace(value) || !Environment.UserInteractive)
    {
        return value;
    }

    Console.Write(prompt);
    return isSecret ? ReadSecret() : Console.ReadLine();
}

static string ReadSecret()
{
    var result = new StringBuilder();

    while (true)
    {
        var key = Console.ReadKey(intercept: true);
        if (key.Key == ConsoleKey.Enter)
        {
            Console.WriteLine();
            return result.ToString();
        }

        if (key.Key == ConsoleKey.Backspace)
        {
            if (result.Length > 0)
            {
                result.Length--;
                Console.Write("\b \b");
            }

            continue;
        }

        if (!char.IsControl(key.KeyChar))
        {
            result.Append(key.KeyChar);
            Console.Write('*');
        }
    }
}

static async Task EnsureChromiumInstalledAsync()
{
    var browserPath = Path.Combine(AppContext.BaseDirectory, "ms-playwright");
    Environment.SetEnvironmentVariable("PLAYWRIGHT_BROWSERS_PATH", browserPath);

    var exitCode = Microsoft.Playwright.Program.Main(new[] { "install", "chromium" });
    if (exitCode != 0)
    {
        throw new InvalidOperationException($"Не удалось установить Chromium для Playwright. Код выхода: {exitCode}.");
    }

    await Task.CompletedTask;
}

static int GetMaxTickets(string[] args)
{
    var index = Array.FindIndex(args, a => a.Equals("--count", StringComparison.OrdinalIgnoreCase));
    if (index >= 0 && index + 1 < args.Length && int.TryParse(args[index + 1], out var parsed) && parsed > 0)
    {
        return parsed;
    }

    return 5;
}

static async Task LoginAsync(IPage page, string email, string password)
{
    var loginOpeners = new[]
    {
        page.GetByRole(AriaRole.Link, new() { NameRegex = new Regex("войти|личный|кабинет|профиль", RegexOptions.IgnoreCase) }),
        page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("войти|личный|кабинет|профиль", RegexOptions.IgnoreCase) }),
        page.Locator("a[href*='login'], a[href*='auth'], button:has-text('Войти')")
    };

    foreach (var opener in loginOpeners)
    {
        if (await opener.First.IsVisibleAsync(new() { Timeout = 3_000 }))
        {
            await opener.First.ClickAsync();
            break;
        }
    }

    var emailInput = page.Locator("input[type='email'], input[name*='email' i], input[name*='login' i], input[autocomplete='username']").First;
    await emailInput.WaitForAsync(new() { State = WaitForSelectorState.Visible });
    await emailInput.FillAsync(email);

    var passwordInput = page.Locator("input[type='password'], input[name*='password' i], input[autocomplete='current-password']").First;
    await passwordInput.FillAsync(password);

    var submit = page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("войти|login|sign in", RegexOptions.IgnoreCase) });
    if (await submit.First.IsVisibleAsync(new() { Timeout = 3_000 }))
    {
        await submit.First.ClickAsync();
    }
    else
    {
        await passwordInput.PressAsync("Enter");
    }

    await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
}

static async Task OpenMyTicketsAsync(IPage page)
{
    var ticketLinks = new[]
    {
        page.GetByRole(AriaRole.Link, new() { NameRegex = new Regex("мои билеты|билеты|заказы|покупки", RegexOptions.IgnoreCase) }),
        page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("мои билеты|билеты|заказы|покупки", RegexOptions.IgnoreCase) }),
        page.Locator("a[href*='ticket' i], a[href*='order' i], a[href*='purchase' i]")
    };

    foreach (var link in ticketLinks)
    {
        if (await link.First.IsVisibleAsync(new() { Timeout = 5_000 }))
        {
            await link.First.ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            return;
        }
    }

    throw new InvalidOperationException("Не удалось найти раздел 'Мои билеты'. Проверьте селекторы для текущей версии сайта.");
}

static async Task<IReadOnlyList<string>> ExtractTicketsAsync(IPage page, int maxTickets)
{
    var selectors = new[]
    {
        ".ticket-card",
        ".ticket",
        "[class*='ticket' i]",
        ".order-card",
        ".order",
        "[class*='order' i]",
        "table tbody tr"
    };

    foreach (var selector in selectors)
    {
        var items = page.Locator(selector);
        var count = await items.CountAsync();
        if (count == 0)
        {
            continue;
        }

        var result = new List<string>();
        for (var i = 0; i < Math.Min(count, maxTickets); i++)
        {
            var text = (await items.Nth(i).InnerTextAsync()).Trim();
            if (!string.IsNullOrWhiteSpace(text) && !result.Contains(text))
            {
                result.Add(text);
            }
        }

        if (result.Count > 0)
        {
            return result;
        }
    }

    return Array.Empty<string>();
}
