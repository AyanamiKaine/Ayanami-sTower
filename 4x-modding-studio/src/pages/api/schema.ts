import type { APIRoute } from "astro";
import { readFile, readdir, stat } from "fs/promises";
import { join, resolve, normalize } from "path";

export interface SchemaInfo {
    name: string;
    path: string;
    size: number;
}

export interface SchemaResponse {
    success: boolean;
    error?: string;
    schemas?: SchemaInfo[];
    content?: string;
    schemaName?: string;
}

/**
 * Normalize a file path - handles various edge cases
 */
function normalizePath(inputPath: string): string {
    if (!inputPath) return "";
    // Use Node's path normalization
    let normalized = resolve(normalize(inputPath));
    // Ensure no double slashes (except for protocol)
    normalized = normalized.replace(/\/+/g, "/");
    return normalized;
}

/**
 * API for loading XSD schemas from X4 data directory
 * 
 * GET /api/schema?action=list&basePath=/path/to/x4/data
 *   Lists all available .xsd files in libraries/
 * 
 * GET /api/schema?action=read&basePath=/path/to/x4/data&schema=md.xsd
 *   Reads a specific schema file
 */
export const GET: APIRoute = async ({ request }) => {
    const url = new URL(request.url);
    const action = url.searchParams.get("action");
    const basePath = url.searchParams.get("basePath");
    const schemaName = url.searchParams.get("schema");

    if (!basePath) {
        return new Response(JSON.stringify({
            success: false,
            error: "basePath parameter is required",
        } satisfies SchemaResponse), {
            status: 400,
            headers: { "Content-Type": "application/json" },
        });
    }

    const normalizedBase = normalizePath(basePath);

    try {
        if (action === "list") {
            // List all XSD files in libraries/
            const librariesPath = join(normalizedBase, "libraries");

            try {
                await stat(librariesPath);
            } catch {
                return new Response(JSON.stringify({
                    success: false,
                    error: `Libraries directory not found: ${librariesPath}`,
                } satisfies SchemaResponse), {
                    status: 404,
                    headers: { "Content-Type": "application/json" },
                });
            }

            const files = await readdir(librariesPath);
            const schemas: SchemaInfo[] = [];

            for (const file of files) {
                if (file.endsWith(".xsd")) {
                    const filePath = join(librariesPath, file);
                    const stats = await stat(filePath);
                    schemas.push({
                        name: file,
                        path: filePath,
                        size: stats.size,
                    });
                }
            }

            // Sort by name, with common schemas first
            const priority = ["common.xsd", "md.xsd", "aiscripts.xsd", "diff.xsd"];
            schemas.sort((a, b) => {
                const aIdx = priority.indexOf(a.name);
                const bIdx = priority.indexOf(b.name);
                if (aIdx !== -1 && bIdx !== -1) return aIdx - bIdx;
                if (aIdx !== -1) return -1;
                if (bIdx !== -1) return 1;
                return a.name.localeCompare(b.name);
            });

            return new Response(JSON.stringify({
                success: true,
                schemas,
            } satisfies SchemaResponse), {
                headers: { "Content-Type": "application/json" },
            });

        } else if (action === "read") {
            if (!schemaName) {
                return new Response(JSON.stringify({
                    success: false,
                    error: "schema parameter is required for read action",
                } satisfies SchemaResponse), {
                    status: 400,
                    headers: { "Content-Type": "application/json" },
                });
            }

            // Security: only allow .xsd files and prevent path traversal
            if (!schemaName.endsWith(".xsd") || schemaName.includes("..") || schemaName.includes("/")) {
                return new Response(JSON.stringify({
                    success: false,
                    error: "Invalid schema name",
                } satisfies SchemaResponse), {
                    status: 400,
                    headers: { "Content-Type": "application/json" },
                });
            }

            const schemaPath = join(normalizedBase, "libraries", schemaName);

            try {
                const content = await readFile(schemaPath, "utf-8");

                return new Response(JSON.stringify({
                    success: true,
                    schemaName,
                    content,
                } satisfies SchemaResponse), {
                    headers: { "Content-Type": "application/json" },
                });
            } catch (e) {
                return new Response(JSON.stringify({
                    success: false,
                    error: `Schema file not found: ${schemaName}`,
                } satisfies SchemaResponse), {
                    status: 404,
                    headers: { "Content-Type": "application/json" },
                });
            }

        } else {
            return new Response(JSON.stringify({
                success: false,
                error: "Invalid action. Use 'list' or 'read'",
            } satisfies SchemaResponse), {
                status: 400,
                headers: { "Content-Type": "application/json" },
            });
        }

    } catch (e) {
        return new Response(JSON.stringify({
            success: false,
            error: e instanceof Error ? e.message : "Unknown error",
        } satisfies SchemaResponse), {
            status: 500,
            headers: { "Content-Type": "application/json" },
        });
    }
};
