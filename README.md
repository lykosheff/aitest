# TicketScraper

C# console utility for logging in to `https://ticket.hc-avto.ru/ru/`, opening the personal tickets/orders area, and printing the most recent purchased tickets.

The utility is configured so the release build produces a single Windows executable. After publishing, a user only needs to start `TicketScraper.exe`; on the first run it installs the Chromium browser used by Playwright into the `ms-playwright` folder next to the executable.

Credentials can still be provided through environment variables, but they are no longer required before launch. If `TICKET_EMAIL` or `TICKET_PASSWORD` is missing, the executable asks for them in the console.

## Build the exe

```bash
dotnet restore TicketScraper/TicketScraper.csproj
dotnet publish TicketScraper/TicketScraper.csproj -c Release
```

The executable will be created at:

```text
TicketScraper/bin/Release/net8.0/win-x64/publish/TicketScraper.exe
```

## Run

Start the published executable:

```powershell
.\TicketScraper.exe
```

Optional arguments are still available:

```powershell
.\TicketScraper.exe --count 5
.\TicketScraper.exe --count 5 --headed
```

You can skip interactive credential prompts by setting environment variables before launch:

```powershell
$env:TICKET_EMAIL = 'your-email@example.com'
$env:TICKET_PASSWORD = 'your-password'
.\TicketScraper.exe --count 5
```
