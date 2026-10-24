# Frontend – Angular client

Angular 21 (standalone components, zoneless, SCSS) + Angular Material. Hebrew UI, RTL.

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
├── index.html                # lang="he" dir="rtl", Assistant font
├── styles.scss               # global styles + Angular Material theme mapped to the tokens
├── styles/_tokens.scss       # design tokens (CSS custom properties), placeholder values
└── app/
    ├── app.config.ts         # providers: router, HttpClient + error interceptor
    ├── app.routes.ts
    ├── models/               # TypeScript types matching docs/api-contract.md
    ├── services/             # HTTP clients (RequestsApiService, HealthService)
    ├── core/                 # cross-cutting: ApiError, error interceptor
    └── features/requests/    # requests feature (requests-page = route '')
```

## Style rule

Component styles use only `var(--token)` from `src/styles/_tokens.scss` – no hardcoded colors,
font sizes or spacing. Raw values live in the tokens file only.
