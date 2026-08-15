# CodeHub Dashboard

React 19 + Vite 6 operator console for **CodeHub** — it inventories every
repository under the configured code root and scores each one on health
signals so the operator can see, at a glance, which repositories need
attention. **One table row = one repository.**

## Stack

- React 19, React Router 7
- Vite 6 build tooling
- Hand-rolled `fetch`-based `ApiClient` (no axios)
- Hand-rolled SVG charts (no charting library)
- CSS-variable theming (light / dark)
- i18next + react-i18next + browser language detector

No charting or UI-kit dependencies are used; every chart, table, modal, and
badge is built in-house against the design tokens in `src/index.css`.

## Getting started

```bash
npm install
npm run dev      # dev server on http://localhost:3000
npm run build    # production build to dist/
npm run preview  # preview the production build
npm run lint     # eslint
```

The dev server proxies `/v1.0` and `/openapi.json` to `http://127.0.0.1:8090`
as a convenience, but the `ApiClient` always talks to the absolute **Server
URL** captured at login, so the proxy is not required.

## Authentication

CodeHub is a local, single-operator tool with a **static API key**. The login
form takes a **Server URL** (default `http://127.0.0.1:8090`) and an **API
Key** (hint: `codehub-dev-key`). The client sends
`Authorization: Bearer <apiKey>` on every request and validates the key with
`GET /v1.0/api/token`. Credentials persist in `localStorage`; any `401`
anywhere dispatches a logout.

## Routes

| Route | Purpose |
| --- | --- |
| `/` | Branded login (Server URL + API Key) |
| `/dashboard/home` | Overview: KPI tiles, health distribution, attention list |
| `/dashboard/repositories` | The main repository table + detail modal |
| `/dashboard/scans` | Scan run history + live in-flight progress |
| `/dashboard/request-history` | KPI strip + activity chart + inspector modal |
| `/dashboard/api-explorer` | OpenAPI-driven API playground |
| `/dashboard/settings` | Server info / configuration |

Navigation is grouped by workflow: Overview · Inventory · Operations ·
Observability · System.

## Repositories table

Each row is a repository. Columns: Repository (name + monospace path),
Languages, Visibility, Version, Last Update (relative + tooltip), then five
signal cells rendered as `StatusBadge` (colored dot + short label + tooltip
carrying the signal's `detail` evidence) — **Test Infra**, **Telemetry**,
**Outdated Deps**, **CVEs/Dependabot**, **Issues/PRs** — then Overall and a
row-actions menu. Color is never the only signal: every badge carries a
letter/label and a tooltip. Filters, sorting, and pagination are all
backend-driven. Clicking a row opens the repository detail modal.

## Project layout

```
src/
├── components/   # Shell + shared UI (Sidebar, Topbar, DataTable, Pagination,
│                 # Modal, ConfirmModal, StatusBadge, CopyButton, FilterBar,
│                 # ActivityChart, Toast, ActionMenu, detail modals, ...)
├── views/        # Route targets (Home, Repositories, Scans, RequestHistory,
│                 # ApiExplorer, Settings)
├── context/      # AuthContext (auth + theme), ToastContext
├── hooks/        # useDebounce, useScanStatus, useApiExplorer
├── utils/        # api.js (ApiClient), openApi.js, constants.js
└── i18n/         # index.js, localeRegistry.js, resources.js, formatters.js,
                  # LanguageSelector.jsx
```

## Internationalization

The i18n foundation ships from the start: an i18next runtime initialized before
first paint, a canonical BCP 47 locale registry (en, de, ja, ar) with alias
normalization and direction metadata, locale-aware formatters
(`formatNumber`, `formatDate`, `formatRelativeTime`, `formatDuration`,
`formatBytes`, `formatList`, ...), and a shared `LanguageSelector` surfaced in
login, topbar, and settings. Selecting a locale updates
`document.documentElement.lang` / `dir` and persists across reloads. English is
the complete source catalog; de/ja/ar ship a representative subset and fall
back to English.

## Theming

Light and dark themes are driven by CSS variables on the document element
(`data-theme="dark"`). The preference persists in `localStorage` and is toggled
from the topbar.
