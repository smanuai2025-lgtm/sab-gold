# ═══════════════════════════════════════════════════════════════════
# SabeekaGold Frontend - Production Dockerfile
# ═══════════════════════════════════════════════════════════════════

# Stage 1: Build Tailwind CSS
FROM node:18-alpine AS builder

WORKDIR /app
COPY package*.json ./
RUN npm install

COPY css css/
COPY tailwind.config.js .
RUN npm run build:tailwind

# Stage 2: Serve with Nginx
FROM nginx:alpine

# Copy Nginx configuration
COPY nginx.conf /etc/nginx/nginx.conf

# Copy frontend files
COPY . /usr/share/nginx/html

# Copy built Tailwind CSS from builder
COPY --from=builder /app/css/tailwind.css /usr/share/nginx/html/css/tailwind.css

# Remove unnecessary files
RUN rm -f /usr/share/nginx/html/Dockerfile \
    && rm -f /usr/share/nginx/html/nginx.conf \
    && rm -f /usr/share/nginx/html/*.md \
    && rm -rf /usr/share/nginx/html/node_modules \
    && rm -f /usr/share/nginx/html/package*.json \
    && rm -f /usr/share/nginx/html/tailwind.config.js \
    && rm -f /usr/share/nginx/html/css/tailwind.input.css

# Create cache directory
RUN mkdir -p /var/cache/nginx && chown -R nginx:nginx /var/cache/nginx

# Expose port
EXPOSE 80

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
  CMD wget --quiet --tries=1 --spider http://localhost/health || exit 1

# Run nginx
CMD ["nginx", "-g", "daemon off;"]
