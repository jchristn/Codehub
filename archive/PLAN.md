# CodeHub — Implementation Plan

CodeHub is a local operator console for a large multi-repository code tree. It sweeps every repository nested under a configured root (default `C:\Code`), scores each one against a fixed set of health signals, and puts the repositories that need attention at the top of a single table. The backend is a C# Watson 7 service; the frontend is a React 19 dashboard. Both follow the reference architecture in `C:\Code\Agents\requirements` — with three deliberate, documented departures noted at the end.

The problem this solves is already visible in the tree today: root-level artifacts like `OUTDATED_DEPENDENCIES.md`, `LAST_UPDATE.md`, and `LARGE_FILES.md` are point-in-time, single-dimension snapshots produced by hand. CodeHub turns that recurring manual sweep into a running service with scoring, drill-down, and a periodic refresh.

## Scope decisions (locked)

Four choices set before planning, and they shape everything below:

- **Authentication is a single static key** read from the server settings JSON. One implicit tenant. The full multi-tenant AAA model in `AUTHENTICATION.md` is intentionally not built. The provider-neutral database, request-history capture, and OpenAPI surface are kept because the dashboard depends on them.
- **One table row is one repository.** With roughly 1,381 `.csproj` files under the root, a project-per-row table would be unusable. Repositories are the unit; the discrete projects inside a repository appear in its detail modal.
- **The GitHub Personal Access Token lives in the settings JSON**, under a `GitHub.PersonalAccessToken` property. When it is set, the issues / pull-requests / Dependabot columns populate from the GitHub API. When it is empty, those three columns render a "token not configured" state and everything else still works.
- **Discovery is polyglot; health-checking is C#-focused.** CodeHub detects .NET, Node, Python, and PowerShell projects and reports version, last-update, and dependency freshness for all of them. The Touchstone (test infra) and Radiant/Watson 7 (telemetry) signals apply only to C# projects, because those frameworks are C#-specific.

## What the operator sees

The main table answers one question per row: *does this repository need my attention, and why?* Columns:

| Column | Meaning | Source |
| --- | --- | --- |
| Repository | Name and path | Discovery |
| Languages | Badges: C#, Node, Python, PS | Discovery |
| Visibility | Open or closed source | GitHub API (with PAT) or heuristic |
| Version | Current version of the repo's primary project | Project manifest / git tag / CHANGELOG |
| Last Update | Most recent commit (or newest file mtime if not a git repo) | git / filesystem |
| Test Infra | Red / yellow / green — Touchstone coverage | csproj analysis |
| Telemetry | Red / yellow / green — Radiant + Watson 7, web services only | csproj analysis |
| Outdated Deps | Red / yellow / green — packages behind latest | `dotnet list package --outdated`, npm, pip |
| CVEs / Dependabot | Red / yellow / green — vulnerable packages and alerts | `dotnet list package --vulnerable` + GitHub |
| Issues / PRs | Red / yellow / green — open issue and PR pressure | GitHub API (with PAT) |
| Overall | Rolled-up health | Scoring |

Every status cell carries a colored dot, a short label, and a tooltip with the evidence behind the color — color is never the only signal, per the accessibility rules in `DASHBOARD_STYLE_AND_USABILITY.md`. Above the table sit a **Scan Now** button and a **last scanned** indicator; both also live in the topbar so they follow the operator across views.

### Traffic-light semantics

The colors mean specific things, and the tooltip states the reason. Vague coloring is worse than none.

**Test Infra (Touchstone).** Green when the repository has a Touchstone-shaped suite — a `Test.Shared` descriptor library referencing `Touchstone.Core` plus a `Test.Automated` console runner referencing `Touchstone.Cli`. Yellow when tests exist but are not Touchstone-shaped, or Touchstone is present but incomplete (for example a `Test.Shared` with no `Test.Automated` runner). Red when no test project exists at all. Not applicable, and shown as a neutral dash, when the repository contains no C# source.

**Telemetry (Radiant + Watson 7).** This signal only fires for C# web services. A repository is a web service when a project references `WatsonWebserver` (major version 7) or constructs a `Webserver`. Green when that web service also wires a Radiant host (`RadiantHost` / `RadiantSettings`, or a `Radiant` package reference) and emits telemetry. Yellow when it runs on Watson 7 but has no Radiant host, or references Radiant without actually hosting it. Red when a Watson 7 web service exposes no telemetry at all. Libraries and non-C# projects are not applicable.

**Outdated Dependencies.** Green when nothing is behind latest stable. Yellow when only minor or patch updates are available. Red when at least one dependency is a major version behind. Severity is driven by the worst drift in the repository, not the count — one major-behind package outranks forty patch bumps.

**CVEs / Dependabot.** Green when no vulnerable packages and no open alerts. Yellow when the worst severity is low or moderate. Red when anything is high or critical. NuGet vulnerabilities come from `dotnet list package --vulnerable` and need no network beyond the NuGet index; Dependabot alerts come from GitHub and require the PAT.

**Issues / PRs.** Green when there are no open pull requests and few open issues. Yellow when open issues exist but no pull requests are stale. Red when pull requests are open and aging. Without a PAT the cell shows "token not configured" and does not affect the overall score.

## Repository and project discovery

Discovery runs in two passes because the tree is not flat — repositories nest (`Less3/S3Server-6.0`, `CommittedCoaches/Chronos`), and a naive per-top-folder walk gets the boundaries wrong.

The first pass resolves **repository boundaries**. A repository is a directory containing `.git`. A top-level folder under the root that has no `.git` anywhere beneath it is treated as its own repository so nothing is lost. Manual include/exclude overrides, stored in the database and editable from the UI, win over the automatic result: an operator can pin a path in or out and the periodic sweep will respect it.

The second pass walks each repository as deep as needed and identifies **discrete projects** by their manifests, skipping `bin`, `obj`, `node_modules`, `dist`, and `.git`:

- **.NET** — `*.csproj`, `*.sln`, `*.slnx`, `Directory.Packages.props`, `Directory.Build.props`
- **Node** — `package.json`
- **Python** — `pyproject.toml`, `requirements*.txt`, `setup.py`
- **PowerShell** — `*.psd1`, `*.psm1`

A repository's row aggregates the facts from its projects. Version comes from the primary project — the packable one, or the one whose name matches the repository, falling back to the highest version found, then to the latest git tag, then to the top heading in `CHANGELOG.md`. Last-update prefers the git committer date and falls back to the newest project-file mtime, which is the same method that produced `LAST_UPDATE.md`.

## Architecture: collectors, then scoring

The engine keeps deterministic work out of any model and reserves the database for computed facts. Two layers:

**Collectors** are independent, deterministic C# services. Each takes a repository (or project) and returns structured facts:

- `DiscoveryCollector` — repository boundaries and the project inventory
- `GitCollector` — remote URL, last commit timestamp, open/closed inference
- `VersionCollector` — primary version resolution
- `DependencyCollector` — runs `dotnet list package --outdated --format json` and `--vulnerable --format json`, plus `npm outdated` and a best-effort PyPI check; caches results keyed by manifest and lockfile hash
- `TouchstoneCollector` — parses project references to classify test coverage
- `TelemetryCollector` — detects Watson 7 hosting and Radiant wiring
- `GitHubCollector` — issues, pull requests, Dependabot alerts, and the private flag, only when a PAT is present

**Scoring** is a pure function from a repository's collected facts to per-signal statuses and an overall grade. Keeping it pure makes it testable in isolation and keeps the thresholds in one place.

Results are written to the database as structured rows — not a JSON blob with an index, per the persistence rule in `BACKEND_ARCHITECTURE.md`. Scans are incremental: a repository is re-collected only when its git HEAD moved or its newest project-file mtime changed since the last scan, so a quiet sweep is cheap. The slow part is the external `dotnet list` and GitHub calls, so those run under a bounded `SemaphoreSlim` and are cached aggressively.

### Scan orchestration

A hosted `ScanService` owns the loop. It triggers on a manual **Scan Now** request (`POST /v1.0/api/scan`, optionally scoped to one repository) or on a timer whose interval comes from settings (default six hours). It exposes progress so the UI can show an in-flight scan, and it stamps a per-repository and a global *last scanned* time. External process invocations are parallelized per project up to `Scan.MaxConcurrency`.

## Backend structure

Watson 7 is the only HTTP stack, following the host pattern in `BACKEND_ARCHITECTURE.md` — a thin `Program.cs`, an instance `CodeHubServer`, per-feature route registrars, the `AuthenticateRequest` hook, and the mandatory `Preflight` / `PostRouting` routes.

```
codehub/
|-- src/
|   |-- CodeHub.sln
|   |-- CodeHub.Core/
|   |   |-- Constants.cs                     # ID prefixes: repo_ prj_ dep_ sig_ scan_ gh_ req_
|   |   |-- Database/
|   |   |   |-- DatabaseDriverBase.cs
|   |   |   |-- DatabaseDriverFactory.cs
|   |   |   |-- DatabaseSettings.cs
|   |   |   |-- DatabaseTypeEnum.cs
|   |   |   |-- SchemaMigration.cs
|   |   |   |-- Interfaces/                   # IRepositoryMethods, IProjectMethods,
|   |   |   |                                 # IDependencyMethods, ISignalMethods,
|   |   |   |                                 # IScanRunMethods, IGitHubSnapshotMethods,
|   |   |   |                                 # IOverrideMethods, IRequestHistoryMethods
|   |   |   |-- Sqlite/                       # implemented in v1
|   |   |   |-- Mysql/ Postgresql/ SqlServer/ # abstraction present, deferred (see Divergences)
|   |   |-- Enums/                            # HealthStatusEnum, SignalTypeEnum,
|   |   |                                     # ProjectTypeEnum, SourceVisibilityEnum, ScanTriggerEnum
|   |   |-- Helpers/IdGenerator.cs            # PrettyId K-sortable IDs
|   |   |-- Models/                           # Repository, Project, Dependency, Signal,
|   |   |                                     # ScanRun, GitHubSnapshot, RepositoryOverride,
|   |   |                                     # RequestHistoryEntry
|   |   |-- Requests/  Responses/
|   |   |-- Security/RequestContext.cs        # simplified: IsAuthenticated only
|   |   |-- Services/
|   |       |-- Collectors/                   # the collectors listed above
|   |       |-- ScanService.cs
|   |       |-- ScoringService.cs
|   |       |-- GitHubService.cs
|   |-- CodeHub.Server/
|   |   |-- Program.cs                        # Bootstrapper.Run(args)
|   |   |-- CodeHubServer.cs
|   |   |-- Settings/
|   |   |-- Routes/                           # Health, Auth, Repository, Project,
|   |   |                                     # Scan, Settings, RequestHistory
|   |   |-- Serialization/
|   |-- Test.Shared/                          # Touchstone descriptors (we dogfood our own signal)
|   |-- Test.Automated/                       # Touchstone.Cli console runner
|   |-- Test.Xunit/  Test.Nunit/
|-- dashboard/                                # React 19 + Vite 6
|-- assets/                                   # logo.png, logo.ico (already present)
|-- .gitignore
|-- README.md  CHANGELOG.md  LICENSE.md
|-- codehub.json                              # default settings
```

Authentication is a constant-time comparison of the `Authorization: Bearer <key>` value against `Settings.Auth.ApiKey` inside the `AuthenticateRequest` hook. Success populates a minimal `RequestContext` with `IsAuthenticated = true` and stashes it in `ctx.Metadata`; failure returns 401. Code obeys the strict C# standard throughout — no `var`, no tuples, in-namespace `using` directives, XML docs on public surfaces, null-checked setters, clamped numerics.

### Data model

Structured tables, one concern each. Primary identifiers are `PrettyId` strings with stable prefixes.

- `repositories` — id, path, name, visibility, primary language, current version, last commit UTC, last scanned UTC, overall health, included flag
- `repository_languages` — child rows, one per detected language
- `projects` — id, repo id, path, type, name, version, target framework, is-web-service, has-touchstone, has-radiant, has-watson7
- `project_dependencies` — id, project id, package, current version, latest version, drift level, is-vulnerable, severity
- `signals` — id, repo id, signal type, status, detail text (the tooltip evidence)
- `scan_runs` — id, started UTC, completed UTC, trigger, repos scanned, status
- `github_snapshots` — repo id, open issues, open PRs, Dependabot open, Dependabot high/critical, fetched UTC
- `repository_overrides` — path, include-or-exclude, note
- `request_history` — the capture store required by the backend reference

Migrations are versioned, tracked in a `schema_migrations` table, and idempotent. First boot seeds nothing beyond the schema; the first scan fills the tables. SQLite writes are serialized with a `SemaphoreSlim`.

### API surface

All routes are versioned under `/v1.0/api`, use typed DTOs, read `RequestContext` from `ctx.Metadata`, and are registered with OpenAPI metadata so the API Explorer can introspect them.

| Method | Path | Purpose |
| --- | --- | --- |
| GET | `/v1.0/api/health` | Anonymous health check |
| GET | `/v1.0/api/token` | Validate the static key |
| GET | `/v1.0/api/repositories` | Paged, filterable, sortable repository list |
| GET | `/v1.0/api/repositories/{id}` | Repository detail: projects, dependencies, signals, GitHub |
| POST | `/v1.0/api/repositories/{id}/include` | Manual include override |
| POST | `/v1.0/api/repositories/{id}/exclude` | Manual exclude override |
| GET | `/v1.0/api/projects/{id}` | Single project detail |
| POST | `/v1.0/api/scan` | Trigger a scan (all, or one repository) |
| GET | `/v1.0/api/scan/status` | Current or last scan progress |
| GET | `/v1.0/api/scan/runs` | Scan run history |
| GET | `/v1.0/api/settings` | Redacted settings (root, interval, whether a PAT is set) |
| GET/DELETE | `/v1.0/api/request-history[...]` | Request capture, per the backend reference |
| GET | `/openapi.json` | Spec for the API Explorer |

### Settings shape

```json
{
  "CreatedUtc": "2026-08-14T00:00:00.000Z",
  "Rest": { "Hostname": "127.0.0.1", "Port": 8090, "Ssl": false },
  "Auth": { "ApiKey": "override-via-env" },
  "Database": { "Type": "Sqlite", "Filename": "data/codehub.db" },
  "Scan": {
    "RootPath": "C:\\Code",
    "IntervalHours": 6,
    "MaxConcurrency": 8,
    "DependencyCheck": true,
    "ExcludeGlobs": ["**/bin/**", "**/obj/**", "**/node_modules/**", "**/dist/**"]
  },
  "GitHub": { "PersonalAccessToken": "", "Owner": "jchristn" },
  "Logging": { "ConsoleLogging": true, "FileLogging": true, "LogDirectory": "logs", "LogFilename": "codehub.log" },
  "RequestHistory": { "Enabled": true, "RetentionDays": 30 }
}
```

Secrets override from the environment — `CODEHUB_AUTH_API_KEY`, `CODEHUB_GITHUB_PAT`, `CODEHUB_SCAN_ROOT` — and never appear in the `GET /settings` response. Loopback binds to `127.0.0.1`, not `localhost`, to avoid the Windows IPv6 resolution stall.

## Frontend

React 19 on Vite 6, React Router 7, the hand-rolled `fetch`-based `ApiClient`, no axios, no charting library. The i18n foundation (i18next, locale registry, locale-aware formatters, a shared language selector) ships from the start rather than as a later cleanup, matching the Tempo/Armada baseline. The favicon comes from `assets/logo.ico` and the login/topbar logo from `assets/logo.png`.

Reference dashboards worth studying before building: **Tempo** for the table frame and pagination mechanics, **Hydra** for route-specific headers and the request inspector, **Armada** for grouped navigation and detail routes, **Conductor** for KPI-driven overview.

Navigation, grouped by workflow:

- **Overview** — Home
- **Repositories** — the main table
- **Scans** — run history and live progress
- **Observability** — Request History, API Explorer
- **System** — Settings / Server Info

### Route inventory

| Route | Operator job | Backend resources | Table / filter needs | Actions and modals | Empty / error states |
| --- | --- | --- | --- | --- | --- |
| `/dashboard/home` | Grasp overall tree health at a glance | repositories summary, scan status | Health distribution, attention list | Scan Now, jump to filtered table | No scan yet; scan failed |
| `/dashboard/repositories` | Find repositories needing attention | repositories list/detail | Health, language, visibility, per-signal red/yellow filters; sort; paginate | Row → detail modal; include/exclude; Scan Now; refresh | No repos discovered; no filter matches |
| `/dashboard/scans` | Watch and review sweeps | scan runs, scan status | Status, trigger, time filters | View run detail | No runs yet |
| `/dashboard/request-history` | Investigate the app's own traffic | request-history list/detail/summary | Method, status, path, time | Inspector modal, delete | No traffic; backend disabled |
| `/dashboard/api-explorer` | Exercise the API live | `/openapi.json`, raw execution | Endpoint search | Execute, copy, confirm destructive | Spec missing |
| `/dashboard/settings` | Inspect configuration | settings | — | Copy server URL | — |

### Key views

**Home** is a command center, not a greeting. KPI tiles: total repositories, needs-attention count, green/yellow/red split, repositories with no tests, web services missing telemetry, repositories with high or critical CVEs. A health-distribution visual, an attention list of the worst repositories linking straight into the filtered table, and the last-scanned indicator with a Scan Now action.

**Repositories** is the table described earlier. Filters and sorting are backend-driven; page size persists; row clicks that land on a control do not open the modal. The include/exclude toggle is a row action.

**Repository detail modal** — a custom modal, ESC and backdrop dismissable, focus-trapped, body-scroll-locked. Header shows the repository name, a copyable path, and the visibility badge. Sections: overview (version, last commit, languages, overall grade); a signal breakdown where each traffic light expands to its evidence ("3 of 5 C# projects have Touchstone tests; missing: Foo.Core, Bar.Server"); a projects table (type, version, framework, web-service flag, Touchstone, Radiant/Watson 7, outdated count, vulnerable count); a dependency list of what is outdated or vulnerable; a GitHub panel (issues, PRs, Dependabot when the PAT is set); scan info; and raw JSON for diagnostics.

**Request History**, its **detail modal**, and the OpenAPI-driven **API Explorer** are built to the reference bar because the backend exposes request capture and `/openapi.json`.

A reusable **StatusIndicator** component renders every traffic light: dot plus label plus tooltip, so the meaning survives for colorblind users and screen readers. Themes are token-driven light and dark, verified at 1280 / 768 / 390 px.

## Delivery phases

1. **Scaffold** — repository housekeeping files, solution, settings, per `REPOSITORY_REQUIREMENTS.md`.
2. **Backend skeleton** — Watson 7 host, static-key auth, SQLite database and migrations, health, OpenAPI, request-history capture, settings endpoint.
3. **Scan engine, local signals** — discovery, git/version/Touchstone/telemetry collectors, scoring, persistence, manual and periodic orchestration.
4. **Dependency and CVE signals** — `dotnet list` outdated and vulnerable collectors, npm and pip best-effort.
5. **GitHub signals** — issues, pull requests, Dependabot, and visibility, gated on the PAT, degrading cleanly without it.
6. **Frontend** — shell, Home, Repositories with detail modal, Scans, Request History, API Explorer, Settings, i18n, themes, responsive and visual QA.
7. **Tests and packaging** — Touchstone suites (CodeHub scoring its own signal green), docs.

## Deliberate divergences from the reference

Each of these is a conscious choice, not an oversight, and the backend reference explicitly allows justified divergence.

The **single static key** replaces the multi-tenant AAA model because CodeHub is a local, single-operator tool. There is one implicit tenant, and `RequestContext` carries no real tenant boundary. Tenants, users, credentials, roles, and sessions are not built. If CodeHub ever needs to be shared, the auth layer is the seam where that model would slot back in.

**SQLite is the only implemented database provider** in v1. The provider-neutral `DatabaseDriverBase` and factory are still in place so the other three providers can be added later, but building the full four-provider test matrix for a desktop tool with one user would be effort spent where no one benefits.

**Request-history capture stays**, even though a local tool generates little traffic, because it is cheap and it powers the Request History and API Explorer views that the dashboard standard requires. Dropping it would save nothing worth the missing surface.

The honest risk in this plan sits in the dependency and GitHub collectors, not the CRUD. `dotnet list package --outdated` shells out per project and reaches the NuGet index; across hundreds of C# projects that is the slowest part of any scan, and it is why the design leans so hard on incremental rescans and lockfile-hash caching. The GitHub side is bounded by rate limits and the Dependabot alerts endpoint's security scope. Get those two collectors right — cached, bounded, and gracefully degrading — and the rest is a well-worn Watson 7 service with a React console on top.
