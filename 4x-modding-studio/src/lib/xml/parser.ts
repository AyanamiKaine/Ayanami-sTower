/**
 * X4 XML Parser - Using fast-xml-parser for reliability
 * Preserves order, attributes, and structure for X4 modding
 * Works on both server and client (no browser APIs)
 */

import { XMLParser } from 'fast-xml-parser';

export interface XMLNode {
    id: string;
    type: 'element' | 'text' | 'comment' | 'cdata';
    name: string;
    attributes: Record<string, string>;
    children: XMLNode[];
    parent: XMLNode | null;
    textContent: string;
    // For generating unique paths
    index: number; // Position among siblings with same name
    depth: number;
}

export interface ParseResult {
    root: XMLNode | null;
    errors: string[];
    nodeMap: Map<string, XMLNode>; // Quick lookup by ID
}

let nodeCounter = 0;

function generateNodeId(): string {
    return `node_${++nodeCounter}`;
}

// Configure fast-xml-parser for X4's needs
const parserOptions = {
    ignoreAttributes: false,
    attributeNamePrefix: '@_',
    textNodeName: '#text',
    commentPropName: '#comment',
    cdataPropName: '#cdata',
    preserveOrder: true,
    trimValues: true,
    parseTagValue: false,
    parseAttributeValue: false,
};

/**
 * Parse XML string into a structured tree
 * Uses fast-xml-parser - works on both server and client
 */
export function parseXML(xmlString: string): ParseResult {
    nodeCounter = 0;
    const errors: string[] = [];
    const nodeMap = new Map<string, XMLNode>();

    try {
        const parser = new XMLParser(parserOptions);
        const parsed = parser.parse(xmlString);

        if (!parsed || parsed.length === 0) {
            errors.push('Empty or invalid XML document');
            return { root: null, errors, nodeMap };
        }

        // Collect document-level comments that appear before the root element
        const docLevelComments: any[] = [];
        let rootData: any = null;

        for (const item of parsed) {
            const keys = Object.keys(item);
            if (keys.length === 0) continue;

            const firstKey = keys[0];

            // Skip XML declaration
            if (firstKey.startsWith('?')) continue;

            // Collect comments before root element
            if (firstKey === '#comment' && !rootData) {
                docLevelComments.push(item);
                continue;
            }

            // First non-comment, non-declaration is the root element
            if (!rootData) {
                rootData = item;
            }
        }

        if (!rootData) {
            errors.push('No root element found');
            return { root: null, errors, nodeMap };
        }

        const root = convertParsedNode(rootData, null, 0, nodeMap);

        // Insert document-level comments at the beginning of root's children
        if (docLevelComments.length > 0) {
            const commentNodes: XMLNode[] = [];
            for (const commentData of docLevelComments) {
                const commentNode = convertParsedNode(commentData, root, 1, nodeMap);
                commentNodes.push(commentNode);
            }
            root.children = [...commentNodes, ...root.children];
        }

        return { root, errors, nodeMap };
    } catch (e) {
        errors.push(`XML parsing failed: ${e instanceof Error ? e.message : 'Unknown error'}`);
        return { root: null, errors, nodeMap };
    }
}

/**
 * Convert fast-xml-parser output to our XMLNode structure
 */
function convertParsedNode(
    data: any,
    parent: XMLNode | null,
    depth: number,
    nodeMap: Map<string, XMLNode>
): XMLNode {
    const id = generateNodeId();

    // Get element name (the non-attribute key)
    const elementName = Object.keys(data).find(k => !k.startsWith(':@')) || 'unknown';

    // Determine node type based on element name
    let nodeType: 'element' | 'text' | 'comment' | 'cdata' = 'element';
    let textContent = '';

    if (elementName === '#comment') {
        nodeType = 'comment';
        textContent = String(data['#comment'] ?? '');
    } else if (elementName === '#cdata') {
        nodeType = 'cdata';
        textContent = String(data['#cdata'] ?? '');
    } else if (elementName === '#text') {
        nodeType = 'text';
        textContent = String(data['#text'] ?? '');
    }

    const node: XMLNode = {
        id,
        type: nodeType,
        name: elementName,
        attributes: {},
        children: [],
        parent,
        textContent,
        index: 0,
        depth,
    };

    // For special nodes (comment, cdata, text), we're done
    if (nodeType !== 'element') {
        nodeMap.set(id, node);
        return node;
    }

    // Extract attributes from :@ property
    if (data[':@']) {
        for (const [key, value] of Object.entries(data[':@'])) {
            if (key.startsWith('@_')) {
                node.attributes[key.slice(2)] = String(value);
            }
        }
    }

    // Process children
    const childData = data[elementName];
    if (Array.isArray(childData)) {
        const siblingCounts = new Map<string, number>();

        for (const child of childData) {
            const childKeys = Object.keys(child);

            // Handle text content
            if (childKeys.includes('#text')) {
                const textContent = String(child['#text']).trim();
                if (textContent) {
                    const textNode: XMLNode = {
                        id: generateNodeId(),
                        type: 'text',
                        name: '#text',
                        attributes: {},
                        children: [],
                        parent: node,
                        textContent,
                        index: 0,
                        depth: depth + 1,
                    };
                    node.children.push(textNode);
                    nodeMap.set(textNode.id, textNode);
                }
                continue;
            }

            // Handle comments
            if (childKeys.includes('#comment')) {
                const commentNode: XMLNode = {
                    id: generateNodeId(),
                    type: 'comment',
                    name: '#comment',
                    attributes: {},
                    children: [],
                    parent: node,
                    textContent: String(child['#comment']),
                    index: 0,
                    depth: depth + 1,
                };
                node.children.push(commentNode);
                nodeMap.set(commentNode.id, commentNode);
                continue;
            }

            // Handle CDATA
            if (childKeys.includes('#cdata')) {
                const cdataNode: XMLNode = {
                    id: generateNodeId(),
                    type: 'cdata',
                    name: '#cdata',
                    attributes: {},
                    children: [],
                    parent: node,
                    textContent: String(child['#cdata']),
                    index: 0,
                    depth: depth + 1,
                };
                node.children.push(cdataNode);
                nodeMap.set(cdataNode.id, cdataNode);
                continue;
            }

            // Handle element children
            const childNode = convertParsedNode(child, node, depth + 1, nodeMap);

            // Track sibling index
            const count = siblingCounts.get(childNode.name) || 0;
            childNode.index = count;
            siblingCounts.set(childNode.name, count + 1);

            node.children.push(childNode);
        }
    }

    nodeMap.set(id, node);
    return node;
}

/**
 * Serialize XMLNode back to XML string
 */
export function serializeNode(node: XMLNode, indent: number = 0): string {
    const spaces = '  '.repeat(indent);

    if (node.type === 'text') {
        return node.textContent;
    }

    if (node.type === 'comment') {
        return `${spaces}<!-- ${node.textContent} -->`;
    }

    if (node.type === 'cdata') {
        return `${spaces}<![CDATA[${node.textContent}]]>`;
    }

    // Build attributes string
    const attrs = Object.entries(node.attributes)
        .map(([key, value]) => `${key}="${escapeXML(value)}"`)
        .join(' ');

    const openTag = attrs ? `<${node.name} ${attrs}>` : `<${node.name}>`;

    // Self-closing for empty elements
    if (node.children.length === 0 && !node.textContent) {
        return `${spaces}${attrs ? `<${node.name} ${attrs} />` : `<${node.name} />`}`;
    }

    // Single text child - inline
    if (node.children.length === 1 && node.children[0].type === 'text') {
        return `${spaces}${openTag}${node.children[0].textContent}</${node.name}>`;
    }

    // Multiple children
    const childContent = node.children
        .map(child => serializeNode(child, indent + 1))
        .join('\n');

    return `${spaces}${openTag}\n${childContent}\n${spaces}</${node.name}>`;
}

function escapeXML(str: string): string {
    return str
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&apos;');
}

/**
 * Get the full path from root to a node (for debugging)
 */
export function getNodePath(node: XMLNode): string[] {
    const path: string[] = [];
    let current: XMLNode | null = node;

    while (current) {
        path.unshift(current.name);
        current = current.parent;
    }

    return path;
}
