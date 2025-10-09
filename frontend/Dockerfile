# ========================================
# Multi-stage Dockerfile for Angular Frontend
# Target: Linux containers (AMD64/ARM64)
# ========================================

# Build stage
FROM --platform=$BUILDPLATFORM node:18-alpine AS build

# Set Node.js options to use legacy OpenSSL provider (fix for webpack with Node 18)
ENV NODE_OPTIONS=--openssl-legacy-provider

WORKDIR /app

# Copy package files
COPY package*.json ./

# Install dependencies with legacy peer deps support
RUN npm ci --legacy-peer-deps --only=production=false

# Copy source code
COPY . .

# Build Angular app for production
RUN npm run build -- --configuration production --output-path=dist

# Production stage - Nginx on Alpine Linux
FROM nginx:alpine AS final

# Install curl for healthchecks
RUN apk add --no-cache curl

# Remove default nginx config
RUN rm -rf /usr/share/nginx/html/*

# Copy custom nginx configuration
COPY nginx.conf /etc/nginx/nginx.conf

# Copy built application from build stage
COPY --from=build /app/dist /usr/share/nginx/html

# Create non-root user and setup directories with proper permissions for ACA
RUN addgroup -g 1001 nginx-user && \
    adduser -D -u 1001 -G nginx-user nginx-user && \
    # Create temp directories that nginx needs
    mkdir -p /tmp/client_temp /tmp/proxy_temp_path /tmp/fastcgi_temp /tmp/uwsgi_temp /tmp/scgi_temp && \
    # Set ownership of all necessary directories
    chown -R nginx-user:nginx-user /usr/share/nginx/html && \
    chown -R nginx-user:nginx-user /var/cache/nginx && \
    chown -R nginx-user:nginx-user /etc/nginx && \
    chown -R nginx-user:nginx-user /tmp && \
    # Create PID directory
    mkdir -p /tmp && \
    # Make sure nginx config is readable
    chmod -R 755 /etc/nginx

USER nginx-user

# Expose port
EXPOSE 80

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=30s --retries=3 \
    CMD curl -f http://localhost/ || exit 1

# Start nginx
CMD ["nginx", "-g", "daemon off;"]