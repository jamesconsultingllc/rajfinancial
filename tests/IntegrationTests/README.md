# RajFinancial Integration Tests

Reqnroll-driven BDD integration tests against the deployed `func-rajfinancial-dev` Azure Functions host.

## Running locally

Provide the required configuration via `appsettings.local.json` (gitignored) or environment variables, then:

```powershell
dotnet test tests/IntegrationTests/RajFinancial.IntegrationTests.csproj
```

Required configuration (see `appsettings.json` for the full shape):

- `ConnectionStrings:SqlConnectionString`
- `Entra:TenantId`, `Entra:RopcClientId`, `Entra:ApiScope`
- `Entra:TestUsers:{Administrator|Client|Advisor}` plus `TEST_{ROLE}_PASSWORD` env vars

## Living Documentation report (Expressium LivingDoc)

The project uses the [Expressium.LivingDoc.ReqnrollPlugin](https://www.nuget.org/packages/Expressium.LivingDoc.ReqnrollPlugin/) formatter, configured in `reqnroll.json`. After every test run the plugin writes:

- `LivingDoc.ndjson` &mdash; raw Cucumber Messages stream
- `LivingDoc.html` &mdash; self-contained HTML report (open in any browser)

Both files land in the test project's output folder, e.g.:

```
tests/IntegrationTests/bin/Debug/net10.0/LivingDoc.html
tests/IntegrationTests/bin/Release/net10.0/LivingDoc.html
```

### CI

The `integration-test-dev` job in [`.github/workflows/azure-functions.yml`](../../.github/workflows/azure-functions.yml) runs the suite against the dev environment, post-processes the HTML (via [`scripts/postprocess-livingdoc.ps1`](../../scripts/postprocess-livingdoc.ps1) — see "Customizing the report" below), and publishes `LivingDoc.html` (plus the `.ndjson`) as a build artifact named **`livingdoc-report`**. The artifact is uploaded with `if: always()` so the report is viewable even when scenarios fail.

To view: open the workflow run summary on GitHub &rarr; **Artifacts** &rarr; download `livingdoc-report` &rarr; open `LivingDoc.html`.

### Customizing the report

The Expressium plugin only supports `outputFileTitle` (in-page heading) and hardcodes `<title>Expressium LivingDoc</title>` in the browser tab with no favicon. After each test run, [`scripts/postprocess-livingdoc.ps1`](../../scripts/postprocess-livingdoc.ps1) rewrites the `<title>` and embeds the client favicon as a base64 data URI so the artifact stays a single self-contained HTML file. To run it locally after `dotnet test`:

```powershell
./scripts/postprocess-livingdoc.ps1 `
  -HtmlPath tests/IntegrationTests/bin/Debug/net10.0/LivingDoc.html `
  -FaviconPath src/Client/public/favicon.ico `
  -Title 'RajFinancial Integration Tests'
```
