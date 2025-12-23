import type { APIRoute } from 'astro';
import { readdir, readFile, stat } from 'fs/promises';
import { join, extname, resolve, normalize } from 'path';

export interface SearchResult {
    filePath: string;
    relativePath: string;
    matches: SearchMatch[];
}

export interface SearchMatch {
    line: number;
    content: string;
    context: string; // Surrounding context
}

export interface SearchResponse {
    success: boolean;
    query: string;
    results: SearchResult[];
    totalMatches: number;
    searchedFiles: number;
    error?: string;
}

/**
 * Normalize a file path
 */
function normalizePath(inputPath: string): string {
    if (!inputPath) return '';
    return resolve(normalize(inputPath));
}

/**
 * Recursively find all XML files in a directory
 */
async function findXMLFiles(dir: string, maxDepth: number = 5, currentDepth: number = 0): Promise<string[]> {
    if (currentDepth >= maxDepth) return [];

    const files: string[] = [];

    try {
        const entries = await readdir(dir, { withFileTypes: true });

        for (const entry of entries) {
            // Skip hidden files and common non-data directories
            if (entry.name.startsWith('.')) continue;
            if (['node_modules', '__pycache__', '.git'].includes(entry.name)) continue;

            const fullPath = join(dir, entry.name);

            if (entry.isDirectory()) {
                const subFiles = await findXMLFiles(fullPath, maxDepth, currentDepth + 1);
                files.push(...subFiles);
            } else if (entry.isFile() && extname(entry.name).toLowerCase() === '.xml') {
                files.push(fullPath);
            }
        }
    } catch {
        // Skip directories we can't read
    }

    return files;
}

/**
 * Search a file for matches
 */
async function searchFile(
    filePath: string,
    basePath: string,
    query: string,
    isRegex: boolean,
    caseSensitive: boolean,
    maxMatchesPerFile: number = 50
): Promise<SearchResult | null> {
    try {
        const content = await readFile(filePath, 'utf-8');
        const lines = content.split('\n');
        const matches: SearchMatch[] = [];

        const flags = caseSensitive ? 'g' : 'gi';
        let regex: RegExp;

        try {
            regex = isRegex ? new RegExp(query, flags) : new RegExp(escapeRegex(query), flags);
        } catch {
            return null; // Invalid regex
        }

        for (let i = 0; i < lines.length && matches.length < maxMatchesPerFile; i++) {
            const line = lines[i];
            if (regex.test(line)) {
                // Reset regex lastIndex for next test
                regex.lastIndex = 0;

                // Get context (1 line before and after)
                const contextStart = Math.max(0, i - 1);
                const contextEnd = Math.min(lines.length - 1, i + 1);
                const context = lines.slice(contextStart, contextEnd + 1).join('\n');

                matches.push({
                    line: i + 1, // 1-indexed
                    content: line.trim(),
                    context: context,
                });
            }
        }

        if (matches.length > 0) {
            return {
                filePath,
                relativePath: filePath.replace(basePath, '').replace(/^\//, ''),
                matches,
            };
        }
    } catch {
        // Skip files we can't read
    }

    return null;
}

function escapeRegex(str: string): string {
    return str.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}

export const GET: APIRoute = async ({ url }) => {
    const query = url.searchParams.get('q');
    const basePath = url.searchParams.get('path');
    const isRegex = url.searchParams.get('regex') === 'true';
    const caseSensitive = url.searchParams.get('case') === 'true';
    const maxResults = parseInt(url.searchParams.get('max') || '100', 10);

    if (!query) {
        return new Response(JSON.stringify({
            success: false,
            query: '',
            results: [],
            totalMatches: 0,
            searchedFiles: 0,
            error: 'No search query provided'
        } satisfies SearchResponse), {
            status: 400,
            headers: { 'Content-Type': 'application/json' }
        });
    }

    if (!basePath) {
        return new Response(JSON.stringify({
            success: false,
            query,
            results: [],
            totalMatches: 0,
            searchedFiles: 0,
            error: 'No base path provided'
        } satisfies SearchResponse), {
            status: 400,
            headers: { 'Content-Type': 'application/json' }
        });
    }

    const normalizedPath = normalizePath(basePath);

    try {
        // Find all XML files
        const xmlFiles = await findXMLFiles(normalizedPath);
        const results: SearchResult[] = [];
        let totalMatches = 0;

        // Search each file (with concurrency limit)
        const batchSize = 20;
        for (let i = 0; i < xmlFiles.length && results.length < maxResults; i += batchSize) {
            const batch = xmlFiles.slice(i, i + batchSize);
            const batchResults = await Promise.all(
                batch.map(file => searchFile(file, normalizedPath, query, isRegex, caseSensitive))
            );

            for (const result of batchResults) {
                if (result && results.length < maxResults) {
                    results.push(result);
                    totalMatches += result.matches.length;
                }
            }
        }

        return new Response(JSON.stringify({
            success: true,
            query,
            results,
            totalMatches,
            searchedFiles: xmlFiles.length,
        } satisfies SearchResponse), {
            headers: { 'Content-Type': 'application/json' }
        });
    } catch (error) {
        return new Response(JSON.stringify({
            success: false,
            query,
            results: [],
            totalMatches: 0,
            searchedFiles: 0,
            error: error instanceof Error ? error.message : 'Search failed'
        } satisfies SearchResponse), {
            status: 500,
            headers: { 'Content-Type': 'application/json' }
        });
    }
};
