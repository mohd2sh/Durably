# Durably.UI ClientApp

Angular source for the Durably dashboard. The MSBuild targets under `src/Durably.UI` build this app into `wwwroot` and embed those assets in the NuGet package. Consumers do not need Node.js.

For package usage (`AddDurablyUI` / `MapDurablyUI`), see [../README.md](../README.md).

## Prerequisites

* Node.js 20+
* npm

## Local development

```bash
npm ci
npm start
```

`ng serve` defaults to `http://localhost:4200/`. The ASP.NET host still serves the packaged SPA at the configured `MapDurablyUI` prefix unless you wire a separate proxy for ClientApp work.

## Production build

```bash
npm ci
npm run build
```

Output lands in `../wwwroot`. Prefer building via the parent project so package embed and CI stay aligned:

```bash
dotnet build ../Durably.UI.csproj -c Release
```

To skip the Angular build when iterating on the C# host only:

```bash
dotnet build ../Durably.UI.csproj -c Release -p:SkipAngularBuild=true
```
