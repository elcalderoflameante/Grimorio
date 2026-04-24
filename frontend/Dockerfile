FROM node:22-alpine AS build
WORKDIR /app

COPY package*.json ./
RUN npm ci

COPY . .

ARG VITE_API_URL=/api
ARG VITE_PUBLIC_APP_URL=https://example.com

ENV VITE_API_URL=${VITE_API_URL}
ENV VITE_PUBLIC_APP_URL=${VITE_PUBLIC_APP_URL}

RUN npm run build

FROM caddy:2.8-alpine AS runtime
WORKDIR /srv

COPY Caddyfile /etc/caddy/Caddyfile
COPY --from=build /app/dist /srv

EXPOSE 80
EXPOSE 443
