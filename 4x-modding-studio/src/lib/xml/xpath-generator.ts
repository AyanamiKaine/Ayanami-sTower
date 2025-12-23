/**
 * X4-Optimized XPath Generator
 * 
 * Generates the shortest, most robust XPath selectors following X4's preferences:
 * 1. [@id='...'] - Always prefer ID
 * 2. [@name='...'] - Use name if ID missing
 * 3. [@macro='...'] - Common in macro definitions
 * 4. Other unique attributes as fallback
 * 5. Positional [n] - Last resort (brittle)
 */

import type { XMLNode } from './parser';

// X4's preferred attributes in priority order
const PRIORITY_ATTRIBUTES = ['id', 'name', 'macro', 'class', 'type', 'ref', 'faction', 'race'];

export interface XPathResult {
    selector: string;
    confidence: 'high' | 'medium' | 'low';
    warning?: string;
}

/**
 * Generate an XPath selector for a node
 * Returns the shortest unique path following X4 conventions
 */
export function generateXPath(node: XMLNode): XPathResult {
    const pathParts: string[] = [];
    let current: XMLNode | null = node;
    let usedPositional = false;

    while (current && current.parent) {
        const part = generateNodeSelector(current);
        if (part.usedPositional) {
            usedPositional = true;
        }
        pathParts.unshift(part.selector);
        current = current.parent;
    }

    // Add root element
    if (current) {
        pathParts.unshift(current.name);
    }

    const selector = '/' + pathParts.join('/');

    return {
        selector,
        confidence: usedPositional ? 'low' : hasStrongIdentifier(node) ? 'high' : 'medium',
        warning: usedPositional
            ? 'Uses positional selector - may break if game updates change order'
            : undefined,
    };
}

interface NodeSelector {
    selector: string;
    usedPositional: boolean;
}

/**
 * Generate selector for a single node in the path
 */
function generateNodeSelector(node: XMLNode): NodeSelector {
    // Try priority attributes first
    for (const attr of PRIORITY_ATTRIBUTES) {
        if (node.attributes[attr]) {
            return {
                selector: `${node.name}[@${attr}='${node.attributes[attr]}']`,
                usedPositional: false,
            };
        }
    }

    // Try to find ANY unique attribute
    const uniqueAttr = findUniqueAttribute(node);
    if (uniqueAttr) {
        return {
            selector: `${node.name}[@${uniqueAttr}='${node.attributes[uniqueAttr]}']`,
            usedPositional: false,
        };
    }

    // Check if this is the only element with this name among siblings
    if (node.parent) {
        const siblings = node.parent.children.filter(
            c => c.type === 'element' && c.name === node.name
        );
        if (siblings.length === 1) {
            return {
                selector: node.name,
                usedPositional: false,
            };
        }
    }

    // Last resort: positional selector (1-indexed in XPath)
    return {
        selector: `${node.name}[${node.index + 1}]`,
        usedPositional: true,
    };
}

/**
 * Find a unique attribute among siblings
 */
function findUniqueAttribute(node: XMLNode): string | null {
    if (!node.parent) return null;

    const siblings = node.parent.children.filter(
        c => c.type === 'element' && c.name === node.name && c.id !== node.id
    );

    // Try each attribute to see if it's unique
    for (const [attr, value] of Object.entries(node.attributes)) {
        const isUnique = !siblings.some(s => s.attributes[attr] === value);
        if (isUnique) {
            return attr;
        }
    }

    return null;
}

/**
 * Check if node has a strong identifier (id, name, macro)
 */
function hasStrongIdentifier(node: XMLNode): boolean {
    return !!(node.attributes.id || node.attributes.name || node.attributes.macro);
}

/**
 * Generate XPath for selecting an attribute of a node
 */
export function generateAttributeXPath(node: XMLNode, attributeName: string): XPathResult {
    const nodeXPath = generateXPath(node);
    return {
        selector: `${nodeXPath.selector}/@${attributeName}`,
        confidence: nodeXPath.confidence,
        warning: nodeXPath.warning,
    };
}

/**
 * Validate an XPath selector against a document
 * Returns true if the selector matches exactly one node
 */
export function validateXPath(xmlDoc: Document, xpath: string): { valid: boolean; matchCount: number } {
    try {
        const result = xmlDoc.evaluate(
            xpath,
            xmlDoc,
            null,
            XPathResult.ORDERED_NODE_SNAPSHOT_TYPE,
            null
        );
        return {
            valid: result.snapshotLength === 1,
            matchCount: result.snapshotLength,
        };
    } catch {
        return { valid: false, matchCount: 0 };
    }
}

/**
 * Simplify XPath by removing unnecessary predicates
 * Tests progressively shorter paths until uniqueness is lost
 */
export function simplifyXPath(xmlDoc: Document, fullPath: string): string {
    const parts = fullPath.split('/').filter(Boolean);

    // Try removing predicates from end to start
    for (let i = parts.length - 1; i >= 0; i--) {
        const part = parts[i];

        // Try without predicate
        const simplified = part.replace(/\[.*\]$/, '');
        if (simplified !== part) {
            const testParts = [...parts];
            testParts[i] = simplified;
            const testPath = '/' + testParts.join('/');

            const { matchCount } = validateXPath(xmlDoc, testPath);
            if (matchCount === 1) {
                parts[i] = simplified;
            }
        }
    }

    return '/' + parts.join('/');
}
