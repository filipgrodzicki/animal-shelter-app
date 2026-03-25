# Build stage
FROM node:20-alpine AS build

WORKDIR /app

# Kopiuj pliki package
COPY package*.json ./

# Instaluj zależności
RUN npm ci

# Kopiuj resztę plików
COPY . .

# Buduj aplikację (pomijamy tsc, używamy tylko vite build)
RUN npx vite build

# Production stage - serwuj przez nginx
FROM nginx:alpine AS production

# Kopiuj zbudowaną aplikację
COPY --from=build /app/dist /usr/share/nginx/html

# Kopiuj konfigurację nginx
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Expose port 80
EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
