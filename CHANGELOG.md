# Changelog

All notable changes to CodeHub are documented here. While CodeHub is in the 0.x line it is
**alpha software** — APIs, storage, settings, and the dashboard may change between releases.

## [0.1.0] — 2026-08-15

First public alpha.

### Added
- **Repository health console.** Sweeps every repository under one or more configured roots and
  scores each on five signals: test infrastructure, telemetry, outdated dependencies,
  vulnerabilities (CVEs / Dependabot), and open issues / pull requests, plus a rolled-up grade.
- **Birds-eye table.** One row per repository with per-column filters (name, language, version,
  branch, commits ahead/behind, last update, each signal, overall) and sortable headers. Click a row
  to drill into its projects, dependencies, signals, and GitHub state.
- **Directory picker.** A lazy, sandboxed filesystem tree with tri-state selection — choose exactly
  which directories to scan; the selection lives in the database.
- **Incremental scans.** Unchanged git repositories are skipped by comparing HEAD commit hashes, so
  startup and scheduled scans only collect what changed. Per-repository collection runs in parallel
  up to a configurable concurrency.
- **Editable settings** persisted to `codehub.json`, including a reserved Model Runner section
  (endpoint, API type, credentials, model name).
- **Row actions** on Windows hosts: Open in Explorer, Open in Terminal, Open Claude, and Open Codex
  (with a confirmation for the tool's dangerous flag).
- **Observability** carried over from the house architecture: request-history capture, an
  OpenAPI-driven API Explorer, and health endpoints.
- **Single binary experience.** The React dashboard is built during `dotnet build` and served by the
  backend at `/dashboard` on the same port. `--port` overrides the listen port.
- Internationalization in English, Spanish, French, German, and Portuguese.
