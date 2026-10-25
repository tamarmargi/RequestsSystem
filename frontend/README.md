# Frontend – Angular client

Angular 21 (standalone components, zoneless, SCSS) + Angular Material. Hebrew UI, RTL. The requests screen is
built from the design in [`docs/design`](../docs/design); see the root README sections *Frontend*,
*Frontend architecture*, *Conflict handling in the UI* and *Design*.

## Run

The API must be running on http://localhost:5000 (see the root README).

```bash
npm install
npm start          # http://localhost:4200, /api is proxied to http://localhost:5000 (proxy.conf.json)
npm run build      # production build into dist/
npm test           # unit tests, single headless run (Vitest + jsdom)
npm run test:watch # unit tests in watch mode
```

The app always calls relative URLs (`/api/...`); never call http://localhost:5000 directly.

## Folder structure

```
src/
├── index.html                    # lang="he" dir="rtl", Heebo font
├── styles.scss                   # global styles + Angular Material theme mapped to the tokens
├── styles/
│   ├── _tokens.scss              # design tokens (CSS custom properties) – the only place for raw values
│   ├── _controls.scss            # shared buttons, fields, utilities
│   └── _dialogs.scss             # shared dialog chrome, history side panel
└── app/
    ├── app.config.ts             # router, HttpClient + interceptors, 'he' locale, dialog/snackbar defaults
    ├── app.routes.ts             # '' → requests page (lazy loaded)
    ├── models/                   # TypeScript types matching docs/api-contract.md
    ├── services/                 # RequestsApiService
    ├── core/                     # ApiError, error interceptor, X-User-Name interceptor, CurrentUserService
    ├── shared/                   # labels + transitions, StatusBadge, PriorityLabel, Icon, dates, error messages
    └── features/requests/
        ├── requests-list.state.ts    # query → list / stats streams, URL sync, selection
        ├── request-status-actions.ts # single update, 409 conflict flow, bulk update
        ├── testing/                  # test data builders
        └── components/
            ├── requests-page/        # container
            ├── page-header/  stats-cards/  requests-filters/  active-filters/
            ├── bulk-action-bar/  requests-table/  requests-paginator/  list-state/
            └── status-change-dialog/  conflict-dialog/  history-panel/  bulk-result-dialog/  user-name-dialog/
```

## Style rule

Component styles use only `var(--token)` from `src/styles/_tokens.scss` – no hardcoded colors, font sizes or
spacing. Raw values live in the tokens file only (media-query breakpoints are the exception: CSS variables
cannot be used there).
