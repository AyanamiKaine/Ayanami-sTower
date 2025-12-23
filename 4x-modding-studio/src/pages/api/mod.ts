import type { APIRoute } from 'astro';
import { mkdir, writeFile, readFile, access, readdir, stat } from 'fs/promises';
import { join, dirname, resolve, normalize, basename } from 'path';
import { constants } from 'fs';

export interface ModInfo {
    id: string;
    name: string;
    author: string;
    version: string;
    description: string;
    path: string;
    createdAt: string;
    isDLC?: boolean; // true if id contains 'ego_dlc'
}

export interface ModFile {
    name: string;
    path: string;
    relativePath: string;
    type: 'new' | 'patch' | 'config' | 'other';
    size: number;
    category: string; // e.g., 'libraries', 'md', 'aiscripts', 't', etc.
}

export interface ModContents {
    files: ModFile[];
    stats: {
        totalFiles: number;
        patchFiles: number;
        newFiles: number;
        configFiles: number;
    };
}

export interface CreateModRequest {
    action: 'create' | 'list' | 'save-diff' | 'get-info' | 'get-contents';
    // For create
    modId?: string;
    modName?: string;
    author?: string;
    version?: string;
    description?: string;
    extensionsPath?: string;
    // For save-diff
    modPath?: string;
    sourceFile?: string; // Original file path relative to X4 data
    diffContent?: string;
}

export interface ModResponse {
    success: boolean;
    error?: string;
    mod?: ModInfo;
    mods?: ModInfo[];
    savedPath?: string;
    contents?: ModContents;
}

function normalizePath(inputPath: string): string {
    if (!inputPath) return '';
    return resolve(normalize(inputPath));
}

/**
 * Generate content.xml for a new mod
 */
function generateContentXml(mod: { id: string; name: string; author: string; version: string; description: string }): string {
    return `<?xml version="1.0" encoding="utf-8"?>
<content
    id="${mod.id}"
    name="${mod.name}"
    author="${mod.author}"
    version="${mod.version}"
    date="${new Date().toISOString().split('T')[0]}"
    description="${mod.description}"
    save="0"
    enabled="1">
</content>
`;
}

/**
 * Create a new mod folder structure
 */
async function createMod(
    extensionsPath: string,
    modId: string,
    modName: string,
    author: string,
    version: string,
    description: string
): Promise<ModInfo> {
    const modPath = join(extensionsPath, modId);

    // Check if mod already exists
    try {
        await access(modPath, constants.F_OK);
        throw new Error(`Mod folder already exists: ${modId}`);
    } catch (e: any) {
        if (e.code !== 'ENOENT') throw e;
    }

    // Create mod directory
    await mkdir(modPath, { recursive: true });

    // Create content.xml
    const contentXml = generateContentXml({ id: modId, name: modName, author, version, description });
    await writeFile(join(modPath, 'content.xml'), contentXml, 'utf-8');

    // Create a README
    const readme = `# ${modName}

**Author:** ${author}  
**Version:** ${version}  

## Description

${description}

## Installation

1. Copy the \`${modId}\` folder to your X4 extensions directory
2. The mod will be automatically detected by the game

## Files Modified

(List the files your mod modifies here)

---
Created with X4 Modding Studio
`;
    await writeFile(join(modPath, 'README.md'), readme, 'utf-8');

    const modInfo: ModInfo = {
        id: modId,
        name: modName,
        author,
        version,
        description,
        path: modPath,
        createdAt: new Date().toISOString(),
        isDLC: modId.includes('ego_dlc'),
    };

    // Save mod info
    await writeFile(join(modPath, '.modinfo.json'), JSON.stringify(modInfo, null, 2), 'utf-8');

    return modInfo;
}

/**
 * List all mods in an extensions directory
 */
async function listMods(extensionsPath: string): Promise<ModInfo[]> {
    const mods: ModInfo[] = [];

    try {
        const entries = await readdir(extensionsPath, { withFileTypes: true });

        for (const entry of entries) {
            if (!entry.isDirectory() || entry.name.startsWith('.')) continue;

            const modPath = join(extensionsPath, entry.name);
            const contentXmlPath = join(modPath, 'content.xml');
            const modInfoPath = join(modPath, '.modinfo.json');

            // Check for content.xml (required for X4 mods)
            try {
                await access(contentXmlPath, constants.F_OK);
            } catch {
                continue; // Not a valid mod folder
            }

            // Try to read mod info
            let modInfo: ModInfo;
            try {
                const infoContent = await readFile(modInfoPath, 'utf-8');
                modInfo = JSON.parse(infoContent);
                modInfo.path = modPath; // Update path in case folder was moved
            } catch {
                // Parse content.xml for basic info
                try {
                    const contentXml = await readFile(contentXmlPath, 'utf-8');
                    const idMatch = contentXml.match(/id="([^"]+)"/);
                    const nameMatch = contentXml.match(/name="([^"]+)"/);
                    const authorMatch = contentXml.match(/author="([^"]+)"/);
                    const versionMatch = contentXml.match(/version="([^"]+)"/);
                    const descMatch = contentXml.match(/description="([^"]+)"/);

                    const modId = idMatch?.[1] || entry.name;
                    modInfo = {
                        id: modId,
                        name: nameMatch?.[1] || entry.name,
                        author: authorMatch?.[1] || 'Unknown',
                        version: versionMatch?.[1] || '1.0.0',
                        description: descMatch?.[1] || '',
                        path: modPath,
                        createdAt: '',
                        isDLC: modId.includes('ego_dlc'),
                    };
                } catch {
                    continue;
                }
            }

            // Ensure isDLC is set
            if (modInfo.isDLC === undefined) {
                modInfo.isDLC = modInfo.id.includes('ego_dlc');
            }

            mods.push(modInfo);
        }
    } catch (e) {
        // Extensions directory doesn't exist or can't be read
    }

    return mods;
}

/**
 * Save a diff.xml file to the correct mirror path in a mod
 */
async function saveDiffXml(
    modPath: string,
    sourceFile: string,
    diffContent: string
): Promise<string> {
    // Determine the target path
    // Source: /path/to/x4_unpacked/libraries/factions.xml
    // Target: /path/to/mod/libraries/factions.xml (as diff.xml)

    // Extract relative path from source
    // We need to mirror the structure - find common X4 data folders
    const x4Folders = ['libraries', 'md', 'aiscripts', 't', 'maps', 'assets', 'index', 'extensions'];
    let relativePath = '';

    for (const folder of x4Folders) {
        const folderIndex = sourceFile.indexOf(`/${folder}/`);
        if (folderIndex !== -1) {
            relativePath = sourceFile.substring(folderIndex + 1);
            break;
        }
    }

    if (!relativePath) {
        // Fallback: use the filename in a 'patches' folder
        relativePath = `patches/${basename(sourceFile)}`;
    }

    // Change extension to indicate it's a diff
    // For X4, the file keeps the same name but contains diff XML
    const targetPath = join(modPath, relativePath);
    const targetDir = dirname(targetPath);

    // Create directory structure
    await mkdir(targetDir, { recursive: true });

    // Write the diff content
    await writeFile(targetPath, diffContent, 'utf-8');

    return targetPath;
}

/**
 * Get mod info from a mod path
 */
async function getModInfo(modPath: string): Promise<ModInfo | null> {
    const modInfoPath = join(modPath, '.modinfo.json');
    const contentXmlPath = join(modPath, 'content.xml');

    try {
        // Try .modinfo.json first
        const infoContent = await readFile(modInfoPath, 'utf-8');
        const modInfo = JSON.parse(infoContent);
        modInfo.path = modPath;
        return modInfo;
    } catch {
        // Fall back to content.xml
        try {
            const contentXml = await readFile(contentXmlPath, 'utf-8');
            const idMatch = contentXml.match(/id="([^"]+)"/);
            const nameMatch = contentXml.match(/name="([^"]+)"/);
            const authorMatch = contentXml.match(/author="([^"]+)"/);
            const versionMatch = contentXml.match(/version="([^"]+)"/);
            const descMatch = contentXml.match(/description="([^"]+)"/);

            return {
                id: idMatch?.[1] || basename(modPath),
                name: nameMatch?.[1] || basename(modPath),
                author: authorMatch?.[1] || 'Unknown',
                version: versionMatch?.[1] || '1.0.0',
                description: descMatch?.[1] || '',
                path: modPath,
                createdAt: '',
            };
        } catch {
            return null;
        }
    }
}

/**
 * Get all files in a mod and categorize them
 */
async function getModContents(modPath: string): Promise<ModContents> {
    const files: ModFile[] = [];
    const x4Categories = ['libraries', 'md', 'aiscripts', 't', 'maps', 'assets', 'index', 'extensions', 'ui', 'cutscenes', 'music', 'sfx', 'voice'];
    const configFiles = ['content.xml', '.modinfo.json', 'README.md', 'readme.md', 'LICENSE', 'license'];

    async function scanDirectory(dirPath: string, relativePath: string = '') {
        try {
            const entries = await readdir(dirPath, { withFileTypes: true });

            for (const entry of entries) {
                const fullPath = join(dirPath, entry.name);
                const entryRelativePath = relativePath ? `${relativePath}/${entry.name}` : entry.name;

                if (entry.isDirectory()) {
                    // Skip hidden directories
                    if (entry.name.startsWith('.')) continue;
                    await scanDirectory(fullPath, entryRelativePath);
                } else if (entry.isFile()) {
                    // Skip hidden files (except .modinfo.json)
                    if (entry.name.startsWith('.') && entry.name !== '.modinfo.json') continue;

                    const stats = await stat(fullPath);

                    // Determine category
                    let category = 'other';
                    const pathParts = entryRelativePath.split('/');
                    for (const cat of x4Categories) {
                        if (pathParts[0] === cat) {
                            category = cat;
                            break;
                        }
                    }

                    // Determine file type
                    let fileType: 'new' | 'patch' | 'config' | 'other' = 'other';

                    if (configFiles.includes(entry.name)) {
                        fileType = 'config';
                    } else if (entry.name.endsWith('.xml')) {
                        // Check if it's a diff/patch file by reading first few bytes
                        try {
                            const content = await readFile(fullPath, 'utf-8');
                            const first500 = content.substring(0, 500);
                            if (first500.includes('<diff>') || first500.includes('<diff ')) {
                                fileType = 'patch';
                            } else {
                                fileType = 'new';
                            }
                        } catch {
                            fileType = 'new';
                        }
                    } else {
                        fileType = 'new';
                    }

                    files.push({
                        name: entry.name,
                        path: fullPath,
                        relativePath: entryRelativePath,
                        type: fileType,
                        size: stats.size,
                        category,
                    });
                }
            }
        } catch (e) {
            // Directory not readable
        }
    }

    await scanDirectory(modPath);

    // Sort files: config first, then by category, then by path
    files.sort((a, b) => {
        if (a.type === 'config' && b.type !== 'config') return -1;
        if (a.type !== 'config' && b.type === 'config') return 1;
        if (a.category !== b.category) return a.category.localeCompare(b.category);
        return a.relativePath.localeCompare(b.relativePath);
    });

    return {
        files,
        stats: {
            totalFiles: files.length,
            patchFiles: files.filter(f => f.type === 'patch').length,
            newFiles: files.filter(f => f.type === 'new').length,
            configFiles: files.filter(f => f.type === 'config').length,
        }
    };
}

export const POST: APIRoute = async ({ request }) => {
    try {
        const body: CreateModRequest = await request.json();

        switch (body.action) {
            case 'create': {
                if (!body.extensionsPath || !body.modId || !body.modName) {
                    return new Response(JSON.stringify({
                        success: false,
                        error: 'Missing required fields: extensionsPath, modId, modName'
                    } satisfies ModResponse), {
                        status: 400,
                        headers: { 'Content-Type': 'application/json' }
                    });
                }

                const mod = await createMod(
                    normalizePath(body.extensionsPath),
                    body.modId,
                    body.modName,
                    body.author || 'Unknown',
                    body.version || '1.0.0',
                    body.description || ''
                );

                return new Response(JSON.stringify({
                    success: true,
                    mod
                } satisfies ModResponse), {
                    headers: { 'Content-Type': 'application/json' }
                });
            }

            case 'list': {
                if (!body.extensionsPath) {
                    return new Response(JSON.stringify({
                        success: false,
                        error: 'Missing extensionsPath'
                    } satisfies ModResponse), {
                        status: 400,
                        headers: { 'Content-Type': 'application/json' }
                    });
                }

                const mods = await listMods(normalizePath(body.extensionsPath));

                return new Response(JSON.stringify({
                    success: true,
                    mods
                } satisfies ModResponse), {
                    headers: { 'Content-Type': 'application/json' }
                });
            }

            case 'save-diff': {
                if (!body.modPath || !body.sourceFile || !body.diffContent) {
                    return new Response(JSON.stringify({
                        success: false,
                        error: 'Missing required fields: modPath, sourceFile, diffContent'
                    } satisfies ModResponse), {
                        status: 400,
                        headers: { 'Content-Type': 'application/json' }
                    });
                }

                const savedPath = await saveDiffXml(
                    normalizePath(body.modPath),
                    body.sourceFile,
                    body.diffContent
                );

                return new Response(JSON.stringify({
                    success: true,
                    savedPath
                } satisfies ModResponse), {
                    headers: { 'Content-Type': 'application/json' }
                });
            }

            case 'get-info': {
                if (!body.modPath) {
                    return new Response(JSON.stringify({
                        success: false,
                        error: 'Missing modPath'
                    } satisfies ModResponse), {
                        status: 400,
                        headers: { 'Content-Type': 'application/json' }
                    });
                }

                const mod = await getModInfo(normalizePath(body.modPath));

                if (!mod) {
                    return new Response(JSON.stringify({
                        success: false,
                        error: 'Could not read mod info'
                    } satisfies ModResponse), {
                        status: 404,
                        headers: { 'Content-Type': 'application/json' }
                    });
                }

                return new Response(JSON.stringify({
                    success: true,
                    mod
                } satisfies ModResponse), {
                    headers: { 'Content-Type': 'application/json' }
                });
            }

            case 'get-contents': {
                if (!body.modPath) {
                    return new Response(JSON.stringify({
                        success: false,
                        error: 'Missing modPath'
                    } satisfies ModResponse), {
                        status: 400,
                        headers: { 'Content-Type': 'application/json' }
                    });
                }

                const contents = await getModContents(normalizePath(body.modPath));

                return new Response(JSON.stringify({
                    success: true,
                    contents
                } satisfies ModResponse), {
                    headers: { 'Content-Type': 'application/json' }
                });
            }

            default:
                return new Response(JSON.stringify({
                    success: false,
                    error: 'Unknown action'
                } satisfies ModResponse), {
                    status: 400,
                    headers: { 'Content-Type': 'application/json' }
                });
        }
    } catch (error) {
        return new Response(JSON.stringify({
            success: false,
            error: error instanceof Error ? error.message : 'Unknown error'
        } satisfies ModResponse), {
            status: 500,
            headers: { 'Content-Type': 'application/json' }
        });
    }
};
