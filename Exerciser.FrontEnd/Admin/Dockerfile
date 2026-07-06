# Build stage
FROM node:20-alpine AS build
WORKDIR /app

# Copy package files
COPY package*.json ./

# Install dependencies
RUN npm ci

# Copy source code
COPY . .

# Build application (Pug to HTML + copy scripts)
RUN npm run build

# Production stage
FROM nginx:alpine

# Copy custom nginx configuration
COPY nginx.conf /etc/nginx/nginx.conf

# Copy built assets from build stage
COPY --from=build /app/public /usr/share/nginx/html

# Expose port
EXPOSE 5000

# Start nginx
CMD ["nginx", "-g", "daemon off;"]
