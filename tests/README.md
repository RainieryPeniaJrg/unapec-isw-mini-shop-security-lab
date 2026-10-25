# Phase 1 integration tests

The xUnit suite runs the real ASP.NET Core host in memory and uses PostgreSQL for persistence and the intentionally vulnerable search. Provide a **local, disposable** PostgreSQL instance whose configured user can create databases and the synthetic `minishop_search` role:

```bash
export ConnectionStrings__Shop='Host=127.0.0.1;Port=5432;Database=minishop;Username=minishop;Password=local-test-password'
dotnet test tests/MiniShop.Api.Tests/MiniShop.Api.Tests.csproj
```

The fixture creates randomly named `minishop_tests_*` databases and drops only those databases after each factory is disposed. It does not reset the configured database. The test cluster must contain no real data. Search-role credentials are synthetic and local. Tests configure Red Team and missing `LAB_MODE` separately; they do not require a globally set laboratory mode.

These tests verify application behavior, including deliberately weak controls. They do not replace the required Burp, sqlmap, browser execution, reviewed evidence, or video.

The suite was executed with the official `mcr.microsoft.com/dotnet/sdk:10.0` image and a disposable PostgreSQL instance on loopback: **Passed: 19, Failed: 0, Skipped: 0**. The original 13 tests cover catalogue/seed, cart and checkout persistence, seller/admin permissions, input validation, the five Red Team scenarios, and disabling the intentional surfaces when `LAB_MODE` is absent. Six Code First cases also verify migration/seed idempotence, model snapshot consistency, legacy schema adoption preserving rows and identity values, and rejecting schema drift without changing data or adopting migration history.

The Code First refactor was checked with an explicit application build (zero warnings/errors), `dotnet ef migrations has-pending-model-changes` (no changes), and a byte-for-byte comparison of the initial migration SQL generated before and after the refactor. See [Code First persistence](../docs/code-first.md) for the migration workflow.

The repository Dockerfile also built successfully after the refactor. A separate container adopted a copy of the previously running laboratory database: health returned `Healthy`, the catalogue MVC page returned HTTP 200, and the API retained its three seeded products. Comparing business-table data and sequence values before and after startup found no changes. The original running application and database were left untouched.

Before the Code First refactor, browser verification with Playwright covered MVC login, a persisted comment, cart quantity changes and checkout, seller product creation/edit/deletion, and the admin users/orders panel. The image built successfully with the repository Dockerfile. The host's port 5080 was occupied by another application, so that browser run used a temporary Compose override publishing only `127.0.0.1:15080`; the checked-in contract remains `127.0.0.1:5080`.

After the refactor, browser smoke verification checked the catalogue and health endpoint on the isolated legacy-copy container at `127.0.0.1:15081`. The full functional and vulnerability regression evidence for the refactor is the 19-test HTTP/persistence suite described above.

After `docker compose down -v` and a fresh startup, `/api/health` returned `Healthy` and the previous browser session was redirected from `/Account/Profile` to login, confirming that the recreated container invalidates the old MVC cookie. Red Team JWTs use the configured disposable key and expire after 30 minutes; reset alone does not revoke them while that key remains unchanged.
