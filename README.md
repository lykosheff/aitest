# TicketScraper

C# console script for logging in to `https://ticket.hc-avto.ru/ru/`, opening the personal tickets/orders area, and printing the most recent purchased tickets.

Credentials are intentionally read from environment variables so the email and password are not committed to source control.

## Run

```bash
export TICKET_EMAIL='your-email@example.com'
export TICKET_PASSWORD='your-password'
dotnet restore TicketScraper/TicketScraper.csproj
dotnet build TicketScraper/TicketScraper.csproj
# After the first build, install the browser binaries used by Playwright:
pwsh TicketScraper/bin/Debug/net8.0/playwright.ps1 install chromium
dotnet run --project TicketScraper/TicketScraper.csproj -- --count 5
```

Use `--headed` to see the browser while debugging selectors:

```bash
dotnet run --project TicketScraper/TicketScraper.csproj -- --count 5 --headed
```
