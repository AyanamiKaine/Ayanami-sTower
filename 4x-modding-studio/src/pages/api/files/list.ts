import type { APIRoute } from 'astro';
import { readdir, stat } from 'fs/promises';
import { join, extname, basename, resolve, normalize } from 'path';

export interface FileEntry {
    name: string;
    path: string;
    type: 'file' | 'directory';
    size?: number;
    extension?: string;
}

export interface ListResponse {
    success: boolean;
    path: string;
    entries: FileEntry[];
    error?: string;
}

// Common X4 file extensions we care about
const X4_EXTENSIONS = new Set([
    '.xml', '.xsd', '.lua', '.txt', '.md',
    '.dds', '.bc', '.gz', '.dat', '.cat'
]);

/**
 * Normalize a file path - handles trailing slashes, double slashes, etc.
 */
function normalizePath(inputPath: string): string {
    if (!inputPath) return '';
    // Use Node's normalize and resolve to clean up the path
    // This handles trailing slashes, double slashes, . and .. etc.
    return resolve(normalize(inputPath));
}

export const GET: APIRoute = async ({ url }) => {
    const rawPath = url.searchParams.get('path');
    const dirPath = rawPath ? normalizePath(rawPath) : null;

    if (!dirPath) {
        return new Response(JSON.stringify({
            success: false,
            path: '',
            entries: [],
            error: 'No path provided'
        } satisfies ListResponse), {
            status: 400,
            headers: { 'Content-Type': 'application/json' }
        });
    }

    try {
        const entries = await readdir(dirPath, { withFileTypes: true });
        const fileEntries: FileEntry[] = [];

        for (const entry of entries) {
            const fullPath = join(dirPath, entry.name);

            // Skip hidden files
            if (entry.name.startsWith('.')) continue;

            if (entry.isDirectory()) {
                fileEntries.push({
                    name: entry.name,
                    path: fullPath,
                    type: 'directory'
                });
            } else if (entry.isFile()) {
                const ext = extname(entry.name).toLowerCase();
                // Only include relevant files for X4 modding
                if (X4_EXTENSIONS.has(ext) || ext === '') {
                    try {
                        const stats = await stat(fullPath);
                        fileEntries.push({
                            name: entry.name,
                            path: fullPath,
                            type: 'file',
                            size: stats.size,
                            extension: ext || undefined
                        });
                    } catch {
                        // Skip files we can't stat
                    }
                }
            }
        }

        // Sort: directories first, then files, alphabetically
        fileEntries.sort((a, b) => {
            if (a.type !== b.type) {
                return a.type === 'directory' ? -1 : 1;
            }
            return a.name.localeCompare(b.name);
        });

        return new Response(JSON.stringify({
            success: true,
            path: dirPath,
            entries: fileEntries
        } satisfies ListResponse), {
            headers: { 'Content-Type': 'application/json' }
        });
    } catch (error) {
        return new Response(JSON.stringify({
            success: false,
            path: dirPath,
            entries: [],
            error: error instanceof Error ? error.message : 'Unknown error'
        } satisfies ListResponse), {
            status: 500,
            headers: { 'Content-Type': 'application/json' }
        });
    }
};
