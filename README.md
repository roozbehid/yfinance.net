# YFinance.Net

YFinance.Net is an independent .NET library for working with Yahoo Finance public endpoints.

It was created and is maintained by Roozbeh GH. The package takes inspiration from the Python yfinance project for scope, but this repository contains an original .NET implementation built for C# developers.

## Features

- Quote summary and company profile data
- Price history and adjustment helpers
- Financial statements, holders, insider data, calendars, and valuation measures
- Search, lookup, and market endpoints
- Predefined and custom screeners with .NET-friendly APIs
- Live streaming updates over Yahoo's websocket feed

## Installation

```bash
dotnet add package YFinance.Net
```

## Quick Start

```csharp
using YFinance.Net;

using var client = new YahooFinanceClient();

var profile = await client.GetCompanyProfileAsync("MSFT");
var history = await client.GetPriceHistoryAsync("MSFT");
var gainers = await client.GetPredefinedScreenerAsync(PredefinedScreenerId.DayGainers);
```


## Legal

YFinance.Net is released under the MIT License. See the LICENSE file in this repository for the full text.

Yahoo, Y!Finance, and Yahoo Finance are registered trademarks of Yahoo, Inc.

YFinance.Net is not affiliated with, endorsed by, or vetted by Yahoo, Inc. It uses Yahoo's publicly available endpoints and is intended for research and educational purposes. Review Yahoo's terms of use before using downloaded data in your own applications.
