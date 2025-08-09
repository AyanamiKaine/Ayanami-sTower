import type { EntityDetail, ComponentInfo } from './api';

export type Operator = '==' | '!=' | '>=' | '<=' | '>' | '<' | 'contains' | 'startsWith' | 'endsWith';

export interface Condition {
  component: string; // component type name (case-insensitive)
  path: string[]; // optional property path inside component data (case-insensitive)
  op: Operator;
  value: unknown;
}

export interface ParsedQuery {
  conditions: Condition[];
  errors: string[];
}

const OP_REGEX = /(==|!=|>=|<=|>|<|contains|startswith|endswith|=)/i;

function parseValue(raw: string): unknown {
  const s = raw.trim();
  if ((s.startsWith('"') && s.endsWith('"')) || (s.startsWith("'") && s.endsWith("'"))) {
    return s.slice(1, -1);
  }
  if (/^true$/i.test(s)) return true;
  if (/^false$/i.test(s)) return false;
  if (/^null$/i.test(s)) return null;
  if (/^-?\d+(?:\.\d+)?$/.test(s)) return Number(s);
  return s; // fallback raw token
}

export function parseQuery(query: string): ParsedQuery {
  const errors: string[] = [];
  const conditions: Condition[] = [];
  if (!query || !query.trim()) return { conditions, errors };

  // Split by AND/&& or comma; we do not support OR to keep it simple
  const parts = query
    .split(/\s+(?:and|&&)\s+|\s*,\s*/i)
    .map((p) => p.trim())
    .filter(Boolean);

  for (const part of parts) {
    const m = part.match(OP_REGEX);
    if (!m) {
      errors.push(`Could not find operator in: "${part}"`);
      continue;
    }
    const opToken = m[1].toLowerCase();
    let op: Operator;
    switch (opToken) {
      case '=':
        op = '==';
        break;
      case 'startswith':
        op = 'startsWith';
        break;
      case 'endswith':
        op = 'endsWith';
        break;
      default:
        op = opToken as Operator;
        break;
    }
    const [lhsRaw, rhsRaw] = [part.slice(0, m.index).trim(), part.slice((m.index ?? 0) + m[0].length).trim()];
    if (!lhsRaw || !rhsRaw) {
      errors.push(`Invalid expression: "${part}"`);
      continue;
    }
    const lhsTokens = lhsRaw.split('.').map((t) => t.trim()).filter(Boolean);
    const component = lhsTokens.shift() ?? '';
    const path = lhsTokens;
    if (!component) {
      errors.push(`Missing component in: "${part}"`);
      continue;
    }
    const value = parseValue(rhsRaw);
    conditions.push({ component, path, op, value });
  }

  return { conditions, errors };
}

function getDisplayValue(data: unknown): any {
  if (data && typeof data === 'object' && 'value' in (data as any)) {
    return (data as any).value;
  }
  return data;
}

function getComponent(detail: EntityDetail, typeName: string): ComponentInfo | undefined {
  const t = typeName.toLowerCase();
  return detail.components.find((c) => c.typeName.toLowerCase() === t);
}

function getByPathCaseInsensitive(obj: any, path: string[]): any {
  let cur = obj;
  for (const segment of path) {
    if (!cur || typeof cur !== 'object') return undefined;
    const key = Object.keys(cur).find((k) => k.toLowerCase() === segment.toLowerCase());
    if (!key) return undefined;
    cur = cur[key];
  }
  return cur;
}

function coerceComparable(a: any, b: any): { a: any; b: any; type: 'number' | 'string' | 'boolean' | 'other' } {
  if (typeof a === 'number' || typeof b === 'number') {
    const na = Number(a);
    const nb = Number(b);
    if (!Number.isNaN(na) && !Number.isNaN(nb)) return { a: na, b: nb, type: 'number' };
  }
  if (typeof a === 'boolean' || typeof b === 'boolean') {
    return { a: Boolean(a), b: Boolean(b), type: 'boolean' };
  }
  return { a: String(a ?? ''), b: String(b ?? ''), type: 'string' };
}

function compare(op: Operator, left: any, right: any): boolean {
  switch (op) {
    case 'contains':
      return String(left ?? '').toLowerCase().includes(String(right ?? '').toLowerCase());
    case 'startsWith':
      return String(left ?? '').toLowerCase().startsWith(String(right ?? '').toLowerCase());
    case 'endsWith':
      return String(left ?? '').toLowerCase().endsWith(String(right ?? '').toLowerCase());
    case '==': {
      if (left === null || left === undefined || right === null || right === undefined) return left == right; // loose equals for nullish
      const { a, b, type } = coerceComparable(left, right);
      return type === 'number' ? a === b : String(a).toLowerCase() === String(b).toLowerCase();
    }
    case '!=': {
      if (left === null || left === undefined || right === null || right === undefined) return left != right;
      const { a, b, type } = coerceComparable(left, right);
      return type === 'number' ? a !== b : String(a).toLowerCase() !== String(b).toLowerCase();
    }
    case '>=': {
      const { a, b } = coerceComparable(left, right);
      return a >= b;
    }
    case '<=': {
      const { a, b } = coerceComparable(left, right);
      return a <= b;
    }
    case '>': {
      const { a, b } = coerceComparable(left, right);
      return a > b;
    }
    case '<': {
      const { a, b } = coerceComparable(left, right);
      return a < b;
    }
    default:
      return false;
  }
}

export function matchesConditions(detail: EntityDetail, conditions: Condition[]): boolean {
  for (const cond of conditions) {
    const comp = getComponent(detail, cond.component);
    if (!comp) return false; // component not present
    const display = getDisplayValue(comp.data);
    const left = cond.path.length ? getByPathCaseInsensitive(display, cond.path) : display;
    if (!compare(cond.op, left, cond.value)) return false;
  }
  return true;
}

export function hasRequiredComponents(detail: EntityDetail, required: string[]): boolean {
  if (!required || required.length === 0) return true;
  const names = new Set(detail.components.map((c) => c.typeName.toLowerCase()));
  return required.every((r) => names.has(r.toLowerCase()));
}
