# =============================================================================
# TaskFlow Frontend — multi-stage build (Vite + React → Nginx)
# =============================================================================

# -------- Stage 1: build estático con Node ----------------------------------
FROM node:20-alpine AS build

WORKDIR /app

# Permitimos sobreescribir la URL de la API en tiempo de build.
# Si se deja vacía, el cliente usará rutas relativas (/api) que nginx proxea
# al servicio del backend (ver nginx.conf).
ARG VITE_API_URL=""
ENV VITE_API_URL=$VITE_API_URL

# Instalación reproducible de dependencias.
COPY package.json package-lock.json ./
RUN npm ci --no-audit --no-fund

# Copia del resto del código y build de producción (tsc + vite build).
COPY . .
RUN npm run build

# -------- Stage 2: runtime servido por Nginx --------------------------------
FROM nginx:1.27-alpine AS runtime

# Reemplaza la configuración por defecto.
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Copia el bundle estático.
COPY --from=build /app/dist /usr/share/nginx/html

# Puerto estándar del contenedor.
EXPOSE 80

# Healthcheck básico contra el index — útil para docker-compose / orquestadores.
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD wget -qO- http://localhost/ >/dev/null 2>&1 || exit 1

CMD ["nginx", "-g", "daemon off;"]
