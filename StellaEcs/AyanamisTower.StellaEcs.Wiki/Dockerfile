# Stage 1: Build the Docusaurus application
# Your package.json requires Node.js 18 or newer. This stage uses a matching version.
FROM oven/bun:1-alpine AS builder

# Set the working directory inside the container
WORKDIR /app

# Copy package.json and package-lock.json to leverage Docker layer caching
COPY package.json bun.lockb* ./

# Install project dependencies
RUN bun install

# Copy the rest of your Docusaurus source code
COPY . .

# Build the Docusaurus application for production
# The output will be in the /app/build directory
RUN bun run build

# Stage 2: Create the final, lightweight image with an Nginx web server
FROM nginx:1.25-alpine

# Copy the static build output from the builder stage to the Nginx html directory
COPY --from=builder /app/build /usr/share/nginx/html

# Expose port 80 to allow traffic to Nginx
EXPOSE 80

# This command starts Nginx in the foreground when the container is launched
CMD ["nginx", "-g", "daemon off;"]
