/**
 * X4 XSD Schema Validator
 * Validates XML nodes against X4's schema definitions
 * 
 * X4 uses several key schemas:
 * - md.xsd - Mission Director scripts
 * - aiscripts.xsd - AI behavior scripts  
 * - common.xsd - Shared types and common elements
 * - diff.xsd - Diff/patch operations
 */

import { XMLParser } from 'fast-xml-parser';

// ============================================================================
// Types
// ============================================================================

export interface SchemaElement {
    name: string;
    type?: string;
    minOccurs?: number;
    maxOccurs?: number | 'unbounded';
    attributes: SchemaAttribute[];
    children: SchemaElementRef[];
    documentation?: string;
    abstract?: boolean;
    substitutionGroup?: string;
    mixed?: boolean;
}

export interface SchemaAttribute {
    name: string;
    type: string;
    use: 'required' | 'optional' | 'prohibited';
    default?: string;
    fixed?: string;
    documentation?: string;
    enumValues?: string[];
}

export interface SchemaElementRef {
    name?: string;
    ref?: string;
    minOccurs: number;
    maxOccurs: number | 'unbounded';
    // For choice/sequence/all groups
    groupType?: 'sequence' | 'choice' | 'all';
    elements?: SchemaElementRef[];
}

export interface SchemaType {
    name: string;
    base?: string;
    attributes: SchemaAttribute[];
    elements: SchemaElementRef[];
    enumValues?: string[];
    pattern?: string;
    minInclusive?: number;
    maxInclusive?: number;
    documentation?: string;
}

export interface ParsedSchema {
    targetNamespace?: string;
    elements: Map<string, SchemaElement>;
    types: Map<string, SchemaType>;
    attributeGroups: Map<string, SchemaAttribute[]>;
    groups: Map<string, SchemaElementRef[]>;
}

export interface ValidationError {
    type: 'error' | 'warning';
    message: string;
    path: string;
    suggestion?: string;
}

export interface ValidationResult {
    valid: boolean;
    errors: ValidationError[];
    warnings: ValidationError[];
}

// ============================================================================
// Schema Parser
// ============================================================================

const xsdParserOptions = {
    ignoreAttributes: false,
    attributeNamePrefix: '@_',
    textNodeName: '#text',
    preserveOrder: true,
    trimValues: true,
};

/**
 * Parse an XSD schema file into a structured format
 */
export function parseXSDSchema(xsdContent: string): ParsedSchema {
    const parser = new XMLParser(xsdParserOptions);
    const parsed = parser.parse(xsdContent);

    const schema: ParsedSchema = {
        elements: new Map(),
        types: new Map(),
        attributeGroups: new Map(),
        groups: new Map(),
    };

    // Find the schema root
    const schemaRoot = findElement(parsed, 'xs:schema') || findElement(parsed, 'xsd:schema');
    if (!schemaRoot) {
        console.warn('No schema root found');
        return schema;
    }

    // Get target namespace
    const attrs = getAttributes(schemaRoot);
    schema.targetNamespace = attrs['targetNamespace'];

    // Process all children of schema
    const children = getChildren(schemaRoot);

    for (const child of children) {
        const childName = getElementName(child);

        if (childName === 'xs:element' || childName === 'xsd:element') {
            const element = parseElementDefinition(child, schema);
            if (element) {
                schema.elements.set(element.name, element);
            }
        } else if (childName === 'xs:complexType' || childName === 'xsd:complexType') {
            const type = parseComplexType(child, schema);
            if (type) {
                schema.types.set(type.name, type);
            }
        } else if (childName === 'xs:simpleType' || childName === 'xsd:simpleType') {
            const type = parseSimpleType(child);
            if (type) {
                schema.types.set(type.name, type);
            }
        } else if (childName === 'xs:attributeGroup' || childName === 'xsd:attributeGroup') {
            const [name, attrs] = parseAttributeGroup(child);
            if (name) {
                schema.attributeGroups.set(name, attrs);
            }
        } else if (childName === 'xs:group' || childName === 'xsd:group') {
            const [name, refs] = parseGroup(child, schema);
            if (name) {
                schema.groups.set(name, refs);
            }
        }
    }

    return schema;
}

// ============================================================================
// XSD Parsing Helpers
// ============================================================================

function findElement(obj: any, name: string): any {
    if (!obj) return null;
    if (Array.isArray(obj)) {
        for (const item of obj) {
            const found = findElement(item, name);
            if (found) return found;
        }
    }
    if (typeof obj === 'object') {
        if (name in obj) return obj;
        for (const key of Object.keys(obj)) {
            if (key === name) return obj;
            const found = findElement(obj[key], name);
            if (found) return found;
        }
    }
    return null;
}

function getElementName(obj: any): string | null {
    if (!obj || typeof obj !== 'object') return null;
    const keys = Object.keys(obj).filter(k => !k.startsWith(':@') && !k.startsWith('#'));
    return keys[0] || null;
}

function getAttributes(obj: any): Record<string, string> {
    if (!obj || !obj[':@']) return {};
    const attrs: Record<string, string> = {};
    for (const [key, value] of Object.entries(obj[':@'])) {
        if (key.startsWith('@_')) {
            attrs[key.slice(2)] = String(value);
        }
    }
    return attrs;
}

function getChildren(obj: any): any[] {
    if (!obj) return [];
    const elementName = getElementName(obj);
    if (!elementName) return [];
    const content = obj[elementName];
    return Array.isArray(content) ? content : [];
}

function parseElementDefinition(element: any, schema: ParsedSchema): SchemaElement | null {
    const attrs = getAttributes(element);
    const name = attrs['name'];
    if (!name) return null;

    const result: SchemaElement = {
        name,
        type: attrs['type'],
        attributes: [],
        children: [],
        abstract: attrs['abstract'] === 'true',
        substitutionGroup: attrs['substitutionGroup'],
    };

    if (attrs['minOccurs']) {
        result.minOccurs = parseInt(attrs['minOccurs'], 10);
    }
    if (attrs['maxOccurs']) {
        result.maxOccurs = attrs['maxOccurs'] === 'unbounded' ? 'unbounded' : parseInt(attrs['maxOccurs'], 10);
    }

    // Parse inline complex type
    const children = getChildren(element);
    for (const child of children) {
        const childName = getElementName(child);
        if (childName === 'xs:complexType' || childName === 'xsd:complexType') {
            const type = parseComplexType(child, schema, true);
            if (type) {
                result.attributes = type.attributes;
                result.children = type.elements;
                result.mixed = type.name === '_mixed_';
            }
        } else if (childName === 'xs:annotation' || childName === 'xsd:annotation') {
            result.documentation = parseAnnotation(child);
        }
    }

    return result;
}

function parseComplexType(typeNode: any, schema: ParsedSchema, inline = false): SchemaType | null {
    const attrs = getAttributes(typeNode);
    const name = inline ? '_inline_' : attrs['name'];
    if (!name && !inline) return null;

    const result: SchemaType = {
        name: name || '_inline_',
        attributes: [],
        elements: [],
    };

    if (attrs['mixed'] === 'true') {
        result.name = '_mixed_';
    }

    const children = getChildren(typeNode);

    for (const child of children) {
        const childName = getElementName(child);

        if (childName === 'xs:sequence' || childName === 'xsd:sequence') {
            result.elements = parseSequence(child, schema);
        } else if (childName === 'xs:choice' || childName === 'xsd:choice') {
            result.elements = parseChoice(child, schema);
        } else if (childName === 'xs:all' || childName === 'xsd:all') {
            result.elements = parseAll(child, schema);
        } else if (childName === 'xs:attribute' || childName === 'xsd:attribute') {
            const attr = parseAttribute(child);
            if (attr) result.attributes.push(attr);
        } else if (childName === 'xs:attributeGroup' || childName === 'xsd:attributeGroup') {
            const refAttrs = getAttributes(child);
            const ref = refAttrs['ref'];
            if (ref) {
                const refName = ref.replace(/^.*:/, '');
                const groupAttrs = schema.attributeGroups.get(refName);
                if (groupAttrs) {
                    result.attributes.push(...groupAttrs);
                }
            }
        } else if (childName === 'xs:complexContent' || childName === 'xsd:complexContent') {
            parseComplexContent(child, result, schema);
        } else if (childName === 'xs:simpleContent' || childName === 'xsd:simpleContent') {
            parseSimpleContent(child, result, schema);
        } else if (childName === 'xs:annotation' || childName === 'xsd:annotation') {
            result.documentation = parseAnnotation(child);
        }
    }

    return result;
}

function parseSimpleType(typeNode: any): SchemaType | null {
    const attrs = getAttributes(typeNode);
    const name = attrs['name'];
    if (!name) return null;

    const result: SchemaType = {
        name,
        attributes: [],
        elements: [],
    };

    const children = getChildren(typeNode);

    for (const child of children) {
        const childName = getElementName(child);

        if (childName === 'xs:restriction' || childName === 'xsd:restriction') {
            const restrictionAttrs = getAttributes(child);
            result.base = restrictionAttrs['base'];

            const restrictionChildren = getChildren(child);
            for (const rc of restrictionChildren) {
                const rcName = getElementName(rc);
                const rcAttrs = getAttributes(rc);

                if (rcName === 'xs:enumeration' || rcName === 'xsd:enumeration') {
                    if (!result.enumValues) result.enumValues = [];
                    result.enumValues.push(rcAttrs['value']);
                } else if (rcName === 'xs:pattern' || rcName === 'xsd:pattern') {
                    result.pattern = rcAttrs['value'];
                } else if (rcName === 'xs:minInclusive' || rcName === 'xsd:minInclusive') {
                    result.minInclusive = parseFloat(rcAttrs['value']);
                } else if (rcName === 'xs:maxInclusive' || rcName === 'xsd:maxInclusive') {
                    result.maxInclusive = parseFloat(rcAttrs['value']);
                }
            }
        }
    }

    return result;
}

function parseAttribute(attrNode: any): SchemaAttribute | null {
    const attrs = getAttributes(attrNode);
    const name = attrs['name'];
    if (!name) return null;

    const result: SchemaAttribute = {
        name,
        type: attrs['type'] || 'xs:string',
        use: (attrs['use'] as any) || 'optional',
        default: attrs['default'],
        fixed: attrs['fixed'],
    };

    // Parse inline simple type for enumerations
    const children = getChildren(attrNode);
    for (const child of children) {
        const childName = getElementName(child);
        if (childName === 'xs:simpleType' || childName === 'xsd:simpleType') {
            const simpleType = parseSimpleType(child);
            if (simpleType?.enumValues) {
                result.enumValues = simpleType.enumValues;
            }
        } else if (childName === 'xs:annotation' || childName === 'xsd:annotation') {
            result.documentation = parseAnnotation(child);
        }
    }

    return result;
}

function parseAttributeGroup(groupNode: any): [string | null, SchemaAttribute[]] {
    const attrs = getAttributes(groupNode);
    const name = attrs['name'];
    if (!name) return [null, []];

    const result: SchemaAttribute[] = [];
    const children = getChildren(groupNode);

    for (const child of children) {
        const childName = getElementName(child);
        if (childName === 'xs:attribute' || childName === 'xsd:attribute') {
            const attr = parseAttribute(child);
            if (attr) result.push(attr);
        }
    }

    return [name, result];
}

function parseGroup(groupNode: any, schema: ParsedSchema): [string | null, SchemaElementRef[]] {
    const attrs = getAttributes(groupNode);
    const name = attrs['name'];
    if (!name) return [null, []];

    const children = getChildren(groupNode);

    for (const child of children) {
        const childName = getElementName(child);
        if (childName === 'xs:sequence' || childName === 'xsd:sequence') {
            return [name, parseSequence(child, schema)];
        } else if (childName === 'xs:choice' || childName === 'xsd:choice') {
            return [name, parseChoice(child, schema)];
        }
    }

    return [name, []];
}

function parseSequence(seqNode: any, schema: ParsedSchema): SchemaElementRef[] {
    const result: SchemaElementRef[] = [];
    const children = getChildren(seqNode);

    for (const child of children) {
        const refs = parseElementRef(child, schema);
        result.push(...refs);
    }

    return result;
}

function parseChoice(choiceNode: any, schema: ParsedSchema): SchemaElementRef[] {
    const attrs = getAttributes(choiceNode);
    const minOccurs = attrs['minOccurs'] ? parseInt(attrs['minOccurs'], 10) : 1;
    const maxOccurs = attrs['maxOccurs'] === 'unbounded' ? 'unbounded' :
        attrs['maxOccurs'] ? parseInt(attrs['maxOccurs'], 10) : 1;

    const elements: SchemaElementRef[] = [];
    const children = getChildren(choiceNode);

    for (const child of children) {
        const refs = parseElementRef(child, schema);
        elements.push(...refs);
    }

    return [{
        groupType: 'choice',
        minOccurs,
        maxOccurs,
        elements,
    }];
}

function parseAll(allNode: any, schema: ParsedSchema): SchemaElementRef[] {
    const result: SchemaElementRef[] = [];
    const children = getChildren(allNode);

    for (const child of children) {
        const refs = parseElementRef(child, schema);
        result.push(...refs);
    }

    return result;
}

function parseElementRef(node: any, schema: ParsedSchema): SchemaElementRef[] {
    const childName = getElementName(node);
    const attrs = getAttributes(node);

    if (childName === 'xs:element' || childName === 'xsd:element') {
        const minOccurs = attrs['minOccurs'] ? parseInt(attrs['minOccurs'], 10) : 1;
        const maxOccurs = attrs['maxOccurs'] === 'unbounded' ? 'unbounded' :
            attrs['maxOccurs'] ? parseInt(attrs['maxOccurs'], 10) : 1;

        return [{
            name: attrs['name'],
            ref: attrs['ref'],
            minOccurs,
            maxOccurs,
        }];
    } else if (childName === 'xs:group' || childName === 'xsd:group') {
        const ref = attrs['ref'];
        if (ref) {
            const refName = ref.replace(/^.*:/, '');
            const groupRefs = schema.groups.get(refName);
            if (groupRefs) {
                return groupRefs;
            }
        }
    } else if (childName === 'xs:sequence' || childName === 'xsd:sequence') {
        return parseSequence(node, schema);
    } else if (childName === 'xs:choice' || childName === 'xsd:choice') {
        return parseChoice(node, schema);
    }

    return [];
}

function parseComplexContent(contentNode: any, result: SchemaType, schema: ParsedSchema) {
    const children = getChildren(contentNode);

    for (const child of children) {
        const childName = getElementName(child);
        if (childName === 'xs:extension' || childName === 'xsd:extension') {
            const attrs = getAttributes(child);
            result.base = attrs['base'];

            const extChildren = getChildren(child);
            for (const extChild of extChildren) {
                const extChildName = getElementName(extChild);

                if (extChildName === 'xs:sequence' || extChildName === 'xsd:sequence') {
                    result.elements.push(...parseSequence(extChild, schema));
                } else if (extChildName === 'xs:choice' || extChildName === 'xsd:choice') {
                    result.elements.push(...parseChoice(extChild, schema));
                } else if (extChildName === 'xs:attribute' || extChildName === 'xsd:attribute') {
                    const attr = parseAttribute(extChild);
                    if (attr) result.attributes.push(attr);
                } else if (extChildName === 'xs:attributeGroup' || extChildName === 'xsd:attributeGroup') {
                    const refAttrs = getAttributes(extChild);
                    const ref = refAttrs['ref'];
                    if (ref) {
                        const refName = ref.replace(/^.*:/, '');
                        const groupAttrs = schema.attributeGroups.get(refName);
                        if (groupAttrs) {
                            result.attributes.push(...groupAttrs);
                        }
                    }
                }
            }
        }
    }
}

function parseSimpleContent(contentNode: any, result: SchemaType, schema: ParsedSchema) {
    const children = getChildren(contentNode);

    for (const child of children) {
        const childName = getElementName(child);
        if (childName === 'xs:extension' || childName === 'xsd:extension') {
            const attrs = getAttributes(child);
            result.base = attrs['base'];

            const extChildren = getChildren(child);
            for (const extChild of extChildren) {
                const extChildName = getElementName(extChild);
                if (extChildName === 'xs:attribute' || extChildName === 'xsd:attribute') {
                    const attr = parseAttribute(extChild);
                    if (attr) result.attributes.push(attr);
                }
            }
        }
    }
}

function parseAnnotation(annotationNode: any): string {
    const children = getChildren(annotationNode);
    for (const child of children) {
        const childName = getElementName(child);
        if (childName === 'xs:documentation' || childName === 'xsd:documentation') {
            const docChildren = getChildren(child);
            for (const doc of docChildren) {
                if (doc['#text']) {
                    return String(doc['#text']);
                }
            }
        }
    }
    return '';
}

// ============================================================================
// Schema Cache and Manager
// ============================================================================

export interface SchemaManager {
    schemas: Map<string, ParsedSchema>;
    loadSchema(name: string, content: string): void;
    getElementSchema(elementName: string): SchemaElement | null;
    getTypeSchema(typeName: string): SchemaType | null;
    getAllElements(): string[];
    getValidAttributes(elementName: string): SchemaAttribute[];
    getValidChildren(elementName: string): string[];
}

export function createSchemaManager(): SchemaManager {
    const schemas = new Map<string, ParsedSchema>();

    function loadSchema(name: string, content: string) {
        const parsed = parseXSDSchema(content);
        schemas.set(name, parsed);
    }

    function getElementSchema(elementName: string): SchemaElement | null {
        for (const schema of schemas.values()) {
            const element = schema.elements.get(elementName);
            if (element) return element;
        }
        return null;
    }

    function getTypeSchema(typeName: string): SchemaType | null {
        // Strip namespace prefix
        const cleanName = typeName.replace(/^.*:/, '');
        for (const schema of schemas.values()) {
            const type = schema.types.get(cleanName);
            if (type) return type;
        }
        return null;
    }

    function getAllElements(): string[] {
        const elements = new Set<string>();
        for (const schema of schemas.values()) {
            for (const name of schema.elements.keys()) {
                elements.add(name);
            }
        }
        return Array.from(elements).sort();
    }

    function resolveTypeAttributes(typeName: string, visited = new Set<string>()): SchemaAttribute[] {
        if (visited.has(typeName)) return [];
        visited.add(typeName);

        const type = getTypeSchema(typeName);
        if (!type) return [];

        let attributes = [...type.attributes];

        // Resolve base type attributes
        if (type.base) {
            const baseAttrs = resolveTypeAttributes(type.base, visited);
            attributes = [...baseAttrs, ...attributes];
        }

        return attributes;
    }

    function getValidAttributes(elementName: string): SchemaAttribute[] {
        const element = getElementSchema(elementName);
        if (!element) return [];

        let attributes = [...element.attributes];

        // If element has a type reference, resolve its attributes
        if (element.type) {
            const typeAttrs = resolveTypeAttributes(element.type);
            attributes = [...typeAttrs, ...attributes];
        }

        return attributes;
    }

    function resolveTypeChildren(typeName: string, visited = new Set<string>()): string[] {
        if (visited.has(typeName)) return [];
        visited.add(typeName);

        const type = getTypeSchema(typeName);
        if (!type) return [];

        const children: string[] = [];

        // Get direct element references
        for (const ref of type.elements) {
            if (ref.name) {
                children.push(ref.name);
            } else if (ref.elements) {
                // Handle choice/sequence groups
                for (const subRef of ref.elements) {
                    if (subRef.name) {
                        children.push(subRef.name);
                    }
                }
            }
        }

        // Resolve base type children
        if (type.base) {
            const baseChildren = resolveTypeChildren(type.base, visited);
            children.push(...baseChildren);
        }

        return children;
    }

    function getValidChildren(elementName: string): string[] {
        const element = getElementSchema(elementName);
        if (!element) return [];

        const children: string[] = [];

        // Direct children from element definition
        for (const ref of element.children) {
            if (ref.name) {
                children.push(ref.name);
            } else if (ref.elements) {
                for (const subRef of ref.elements) {
                    if (subRef.name) {
                        children.push(subRef.name);
                    }
                }
            }
        }

        // Children from type definition
        if (element.type) {
            const typeChildren = resolveTypeChildren(element.type);
            children.push(...typeChildren);
        }

        return [...new Set(children)].sort();
    }

    return {
        schemas,
        loadSchema,
        getElementSchema,
        getTypeSchema,
        getAllElements,
        getValidAttributes,
        getValidChildren,
    };
}

// ============================================================================
// Validator
// ============================================================================

export interface XMLValidationNode {
    name: string;
    attributes: Record<string, string>;
    children: XMLValidationNode[];
    textContent?: string;
    path: string;
}

/**
 * Validate an XML node against loaded schemas
 */
export function validateXMLNode(
    node: XMLValidationNode,
    schemaManager: SchemaManager
): ValidationResult {
    const errors: ValidationError[] = [];
    const warnings: ValidationError[] = [];

    validateNodeRecursive(node, schemaManager, errors, warnings);

    return {
        valid: errors.length === 0,
        errors,
        warnings,
    };
}

function validateNodeRecursive(
    node: XMLValidationNode,
    schemaManager: SchemaManager,
    errors: ValidationError[],
    warnings: ValidationError[]
) {
    const element = schemaManager.getElementSchema(node.name);

    if (!element) {
        // Unknown element - could be warning or error depending on context
        warnings.push({
            type: 'warning',
            message: `Unknown element: <${node.name}>`,
            path: node.path,
            suggestion: suggestSimilarElement(node.name, schemaManager),
        });
    } else {
        // Validate attributes
        validateAttributes(node, element, schemaManager, errors, warnings);
    }

    // Validate children recursively
    for (const child of node.children) {
        validateNodeRecursive(child, schemaManager, errors, warnings);
    }

    // Validate child elements if we have schema info
    if (element) {
        validateChildren(node, element, schemaManager, errors, warnings);
    }
}

function validateAttributes(
    node: XMLValidationNode,
    element: SchemaElement,
    schemaManager: SchemaManager,
    errors: ValidationError[],
    warnings: ValidationError[]
) {
    const validAttributes = schemaManager.getValidAttributes(node.name);
    const validAttrNames = new Set(validAttributes.map(a => a.name));

    // Check for invalid attributes
    for (const [attrName, attrValue] of Object.entries(node.attributes)) {
        if (!validAttrNames.has(attrName)) {
            const suggestion = suggestSimilarAttribute(attrName, validAttributes);
            errors.push({
                type: 'error',
                message: `Invalid attribute '${attrName}' on <${node.name}>`,
                path: `${node.path}/@${attrName}`,
                suggestion,
            });
        } else {
            // Validate attribute value
            const attrSchema = validAttributes.find(a => a.name === attrName);
            if (attrSchema) {
                validateAttributeValue(node, attrName, attrValue, attrSchema, schemaManager, errors, warnings);
            }
        }
    }

    // Check for required attributes
    for (const attr of validAttributes) {
        if (attr.use === 'required' && !(attr.name in node.attributes)) {
            errors.push({
                type: 'error',
                message: `Missing required attribute '${attr.name}' on <${node.name}>`,
                path: node.path,
                suggestion: `Add ${attr.name}="${attr.default || ''}"`,
            });
        }
    }
}

function validateAttributeValue(
    node: XMLValidationNode,
    attrName: string,
    attrValue: string,
    attrSchema: SchemaAttribute,
    schemaManager: SchemaManager,
    errors: ValidationError[],
    warnings: ValidationError[]
) {
    // Check enumeration values
    if (attrSchema.enumValues && attrSchema.enumValues.length > 0) {
        if (!attrSchema.enumValues.includes(attrValue)) {
            errors.push({
                type: 'error',
                message: `Invalid value '${attrValue}' for attribute '${attrName}' on <${node.name}>`,
                path: `${node.path}/@${attrName}`,
                suggestion: `Valid values: ${attrSchema.enumValues.join(', ')}`,
            });
        }
    }

    // Check type constraints
    const typeName = attrSchema.type.replace(/^xs:|^xsd:/, '');
    const type = schemaManager.getTypeSchema(attrSchema.type);

    if (type?.enumValues && type.enumValues.length > 0) {
        if (!type.enumValues.includes(attrValue)) {
            errors.push({
                type: 'error',
                message: `Invalid value '${attrValue}' for attribute '${attrName}' on <${node.name}>`,
                path: `${node.path}/@${attrName}`,
                suggestion: `Valid values: ${type.enumValues.join(', ')}`,
            });
        }
    }

    if (type?.pattern) {
        const regex = new RegExp(`^${type.pattern}$`);
        if (!regex.test(attrValue)) {
            warnings.push({
                type: 'warning',
                message: `Value '${attrValue}' may not match expected pattern for '${attrName}'`,
                path: `${node.path}/@${attrName}`,
            });
        }
    }

    // Basic type validation
    if (typeName === 'integer' || typeName === 'int') {
        if (!/^-?\d+$/.test(attrValue)) {
            errors.push({
                type: 'error',
                message: `Attribute '${attrName}' expects an integer, got '${attrValue}'`,
                path: `${node.path}/@${attrName}`,
            });
        }
    } else if (typeName === 'boolean') {
        if (!['true', 'false', '0', '1'].includes(attrValue.toLowerCase())) {
            errors.push({
                type: 'error',
                message: `Attribute '${attrName}' expects a boolean, got '${attrValue}'`,
                path: `${node.path}/@${attrName}`,
                suggestion: 'Use "true" or "false"',
            });
        }
    } else if (typeName === 'decimal' || typeName === 'float' || typeName === 'double') {
        if (!/^-?\d+(\.\d+)?$/.test(attrValue)) {
            errors.push({
                type: 'error',
                message: `Attribute '${attrName}' expects a number, got '${attrValue}'`,
                path: `${node.path}/@${attrName}`,
            });
        }
    }
}

function validateChildren(
    node: XMLValidationNode,
    element: SchemaElement,
    schemaManager: SchemaManager,
    errors: ValidationError[],
    warnings: ValidationError[]
) {
    const validChildNames = schemaManager.getValidChildren(node.name);

    if (validChildNames.length === 0) {
        // No schema info about valid children - skip validation
        return;
    }

    const validChildSet = new Set(validChildNames);

    for (const child of node.children) {
        if (!validChildSet.has(child.name)) {
            const suggestion = suggestSimilarChild(child.name, validChildNames);
            errors.push({
                type: 'error',
                message: `Invalid child element <${child.name}> in <${node.name}>`,
                path: child.path,
                suggestion,
            });
        }
    }
}

// ============================================================================
// Suggestion Helpers (Levenshtein distance for typo detection)
// ============================================================================

function levenshteinDistance(a: string, b: string): number {
    const matrix: number[][] = [];

    for (let i = 0; i <= b.length; i++) {
        matrix[i] = [i];
    }
    for (let j = 0; j <= a.length; j++) {
        matrix[0][j] = j;
    }

    for (let i = 1; i <= b.length; i++) {
        for (let j = 1; j <= a.length; j++) {
            if (b[i - 1] === a[j - 1]) {
                matrix[i][j] = matrix[i - 1][j - 1];
            } else {
                matrix[i][j] = Math.min(
                    matrix[i - 1][j - 1] + 1, // substitution
                    matrix[i][j - 1] + 1,     // insertion
                    matrix[i - 1][j] + 1      // deletion
                );
            }
        }
    }

    return matrix[b.length][a.length];
}

function findSimilar(input: string, candidates: string[], maxDistance = 3): string | null {
    let best: string | null = null;
    let bestDistance = maxDistance + 1;

    for (const candidate of candidates) {
        const distance = levenshteinDistance(input.toLowerCase(), candidate.toLowerCase());
        if (distance < bestDistance) {
            bestDistance = distance;
            best = candidate;
        }
    }

    return bestDistance <= maxDistance ? best : null;
}

function suggestSimilarElement(name: string, schemaManager: SchemaManager): string | undefined {
    const allElements = schemaManager.getAllElements();
    const similar = findSimilar(name, allElements);
    return similar ? `Did you mean <${similar}>?` : undefined;
}

function suggestSimilarAttribute(name: string, validAttributes: SchemaAttribute[]): string | undefined {
    const attrNames = validAttributes.map(a => a.name);
    const similar = findSimilar(name, attrNames);
    return similar ? `Did you mean '${similar}'?` : `Valid attributes: ${attrNames.slice(0, 5).join(', ')}${attrNames.length > 5 ? '...' : ''}`;
}

function suggestSimilarChild(name: string, validChildren: string[]): string | undefined {
    const similar = findSimilar(name, validChildren);
    return similar ? `Did you mean <${similar}>?` : `Valid children: ${validChildren.slice(0, 5).join(', ')}${validChildren.length > 5 ? '...' : ''}`;
}

// ============================================================================
// Built-in X4 Schema Definitions (Partial - for common elements)
// ============================================================================

/**
 * Pre-defined schema info for common X4 MD elements
 * This serves as a fallback when XSD files aren't loaded
 */
export const X4_BUILTIN_SCHEMAS: Record<string, { attributes: SchemaAttribute[], children: string[] }> = {
    // Mission Director elements
    'mdscript': {
        attributes: [
            { name: 'name', type: 'xs:string', use: 'required' },
            { name: 'xmlns:xsi', type: 'xs:string', use: 'optional' },
            { name: 'xsi:noNamespaceSchemaLocation', type: 'xs:string', use: 'optional' },
        ],
        children: ['cues', 'patch'],
    },
    'cues': {
        attributes: [],
        children: ['cue', 'library'],
    },
    'cue': {
        attributes: [
            { name: 'name', type: 'xs:string', use: 'required' },
            { name: 'onfail', type: 'xs:string', use: 'optional', enumValues: ['cancel', 'complete'] },
            { name: 'instantiate', type: 'xs:boolean', use: 'optional' },
            { name: 'namespace', type: 'xs:string', use: 'optional' },
            { name: 'version', type: 'xs:integer', use: 'optional' },
            { name: 'checkinterval', type: 'xs:string', use: 'optional' },
            { name: 'checktime', type: 'xs:string', use: 'optional' },
        ],
        children: ['conditions', 'actions', 'cues', 'delay', 'patch'],
    },
    'conditions': {
        attributes: [],
        children: [
            'check_value', 'check_any', 'check_all', 'check_age',
            'event_cue_signalled', 'event_object_signalled', 'event_player_changed_zone',
            'event_object_destroyed', 'event_object_attacked', 'event_conversation_started',
            'event_game_loaded', 'event_game_saved', 'event_player_changed_sector',
        ],
    },
    'actions': {
        attributes: [],
        children: [
            'set_value', 'do_if', 'do_else', 'do_elseif', 'do_while', 'do_all',
            'do_any', 'do_for_each', 'create_ship', 'create_station', 'create_cue_actor',
            'debug_text', 'show_notification', 'speak_actor', 'start_conversation',
            'signal_cue', 'cancel_cue', 'complete_cue', 'reset_cue',
            'set_faction_relation', 'add_relation', 'set_object_name',
            'destroy_object', 'remove_object', 'add_money', 'remove_money',
        ],
    },
    'create_ship': {
        attributes: [
            { name: 'name', type: 'xs:string', use: 'optional' },
            { name: 'macro', type: 'xs:string', use: 'required' },
            { name: 'zone', type: 'xs:string', use: 'optional' },
            { name: 'sector', type: 'xs:string', use: 'optional' },
            { name: 'class', type: 'xs:string', use: 'optional' },
            { name: 'owner', type: 'xs:string', use: 'optional' },
            { name: 'race', type: 'xs:string', use: 'optional' },
        ],
        children: ['pilot', 'position', 'rotation', 'loadout', 'owner'],
    },
    'set_value': {
        attributes: [
            { name: 'name', type: 'xs:string', use: 'required' },
            { name: 'exact', type: 'xs:string', use: 'optional' },
            { name: 'min', type: 'xs:string', use: 'optional' },
            { name: 'max', type: 'xs:string', use: 'optional' },
            { name: 'operation', type: 'xs:string', use: 'optional', enumValues: ['set', 'add', 'subtract', 'multiply', 'divide'] },
        ],
        children: [],
    },
    'do_if': {
        attributes: [
            { name: 'value', type: 'xs:string', use: 'required' },
        ],
        children: [
            'set_value', 'do_if', 'do_else', 'do_elseif', 'do_while', 'do_all',
            'create_ship', 'debug_text', 'signal_cue', 'show_notification',
        ],
    },
    'debug_text': {
        attributes: [
            { name: 'text', type: 'xs:string', use: 'required' },
            { name: 'filter', type: 'xs:string', use: 'optional', enumValues: ['error', 'general', 'scripts', 'ai', 'savegame'] },
            { name: 'chance', type: 'xs:decimal', use: 'optional' },
        ],
        children: [],
    },
    'signal_cue': {
        attributes: [
            { name: 'cue', type: 'xs:string', use: 'required' },
            { name: 'param', type: 'xs:string', use: 'optional' },
            { name: 'param2', type: 'xs:string', use: 'optional' },
        ],
        children: [],
    },
    // Diff elements
    'diff': {
        attributes: [],
        children: ['add', 'replace', 'remove'],
    },
    'add': {
        attributes: [
            { name: 'sel', type: 'xs:string', use: 'required' },
            { name: 'pos', type: 'xs:string', use: 'optional', enumValues: ['before', 'after', 'prepend', 'append'] },
            { name: 'type', type: 'xs:string', use: 'optional' },
        ],
        children: [],
    },
    'replace': {
        attributes: [
            { name: 'sel', type: 'xs:string', use: 'required' },
        ],
        children: [],
    },
    'remove': {
        attributes: [
            { name: 'sel', type: 'xs:string', use: 'required' },
        ],
        children: [],
    },
    // Faction elements
    'faction': {
        attributes: [
            { name: 'id', type: 'xs:string', use: 'required' },
            { name: 'name', type: 'xs:string', use: 'optional' },
            { name: 'description', type: 'xs:string', use: 'optional' },
            { name: 'primaryrace', type: 'xs:string', use: 'optional' },
            { name: 'behaviourset', type: 'xs:string', use: 'optional' },
        ],
        children: ['relation', 'relations', 'licences', 'colour'],
    },
    'relation': {
        attributes: [
            { name: 'faction', type: 'xs:string', use: 'required' },
            { name: 'relation', type: 'xs:decimal', use: 'required' },
            { name: 'locked', type: 'xs:boolean', use: 'optional' },
        ],
        children: [],
    },
};

/**
 * Create a schema manager pre-loaded with built-in X4 definitions
 */
export function createX4SchemaManager(): SchemaManager {
    const manager = createSchemaManager();

    // Add built-in schemas as a pseudo-parsed schema
    const builtinSchema: ParsedSchema = {
        elements: new Map(),
        types: new Map(),
        attributeGroups: new Map(),
        groups: new Map(),
    };

    for (const [elementName, def] of Object.entries(X4_BUILTIN_SCHEMAS)) {
        builtinSchema.elements.set(elementName, {
            name: elementName,
            attributes: def.attributes,
            children: def.children.map(name => ({ name, minOccurs: 0, maxOccurs: 'unbounded' as const })),
        });
    }

    manager.schemas.set('_builtin_', builtinSchema);

    return manager;
}
