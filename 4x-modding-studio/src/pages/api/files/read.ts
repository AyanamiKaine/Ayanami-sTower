import type { APIRoute } from 'astro';
import { readFile, stat } from 'fs/promises';
import { extname } from 'path';

export interface ReadResponse {
    success: boolean;
    path: string;
    content?: string;
    size?: number;
    extension?: string;
    error?: string;
}

// Max file size to read (5MB)
const MAX_FILE_SIZE = 5 * 1024 * 1024;

// Text extensions we can read
const TEXT_EXTENSIONS = new Set([
    '.xml', '.xsd', '.lua', '.txt', '.md', '.json', '.csv'
]);

export const GET: APIRoute = async ({ url }) => {
    const filePath = url.searchParams.get('path');

    if (!filePath) {
        return new Response(JSON.stringify({
            success: false,
            path: '',
            error: 'No path provided'
        } satisfies ReadResponse), {
            status: 400,
            headers: { 'Content-Type': 'application/json' }
        });
    }

    try {
        const stats = await stat(filePath);
        const ext = extname(filePath).toLowerCase();

        if (!stats.isFile()) {
            return new Response(JSON.stringify({
                success: false,
                path: filePath,
                error: 'Path is not a file'
            } satisfies ReadResponse), {
                status: 400,
                headers: { 'Content-Type': 'application/json' }
            });
        }

        if (stats.size > MAX_FILE_SIZE) {
            return new Response(JSON.stringify({
                success: false,
                path: filePath,
                size: stats.size,
                error: `File too large (${(stats.size / 1024 / 1024).toFixed(2)}MB). Maximum size is 5MB.`
            } satisfies ReadResponse), {
                status: 400,
                headers: { 'Content-Type': 'application/json' }
            });
        }

        if (!TEXT_EXTENSIONS.has(ext)) {
            return new Response(JSON.stringify({
                success: false,
                path: filePath,
                extension: ext,
                error: `Cannot read binary file type: ${ext || 'unknown'}`
            } satisfies ReadResponse), {
                status: 400,
                headers: { 'Content-Type': 'application/json' }
            });
        }

        const content = await readFile(filePath, 'utf-8');

        return new Response(JSON.stringify({
            success: true,
            path: filePath,
            content,
            size: stats.size,
            extension: ext
        } satisfies ReadResponse), {
            headers: { 'Content-Type': 'application/json' }
        });
    } catch (error) {
        return new Response(JSON.stringify({
            success: false,
            path: filePath,
            error: error instanceof Error ? error.message : 'Unknown error'
        } satisfies ReadResponse), {
            status: 500,
            headers: { 'Content-Type': 'application/json' }
        });
    }
};
