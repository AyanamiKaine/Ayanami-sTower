# Stage 1: Build the Astro.js application using Bun
FROM docker.io/oven/bun:1-alpine AS builder

# Set the working directory
WORKDIR /app

# Copy package.json, bun.lock, and tsconfig.json
# Copying these specific files first leverages Docker's layer caching.
COPY package.json bun.lock tsconfig.json ./

# Install dependencies using Bun
# --frozen-lockfile ensures the exact versions from bun.lock are used.
RUN bun install --frozen-lockfile

# Copy the rest of the application source code
COPY . .

# Build the application using Bun's script runner
RUN bun run build

# Stage 2: Create the final, small image with a web server
FROM docker.io/library/nginx:1.25-alpine

# Copy the built application from the builder stage
# The output directory for Astro's build is 'dist' by default.
COPY --from=builder /app/dist /usr/share/nginx/html

# Inform Podman that the container listens on port 80
EXPOSE 80

# Start Nginx in the foreground when the container launches
CMD ["nginx", "-g", "daemon off;"]
