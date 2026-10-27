# Web image: build the Angular app with Node, serve the static files with nginx.
# Build context: frontend/ (see docker-compose.yml).

# Same Node/npm as the project and CI (24.12.0 / npm 11.6.2), so npm ci accepts package-lock.json.
FROM node:24.12.0-alpine AS build
WORKDIR /app

# Install first, from the lock file only, so the npm layer is cached until dependencies change.
COPY package.json package-lock.json ./
RUN npm ci

COPY . .
RUN npm run build

FROM nginx:1.27-alpine AS runtime
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist/requests-client/browser /usr/share/nginx/html
EXPOSE 80
