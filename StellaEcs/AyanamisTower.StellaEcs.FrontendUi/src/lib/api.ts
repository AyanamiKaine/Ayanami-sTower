// Build an API base that works in browser, dev server, and during SSR/build.
const isSSR = typeof window === 'undefined';
const devBase = '/api';
const prodBase = (import.meta as any).env.PUBLIC_API_BASE_URL || 'http://localhost:5123/api';
let resolvedBase = devBase;
if (!((import.meta as any).env?.DEV)) {
  resolvedBase = prodBase;
} else if (isSSR) {
  // During dev SSR (getStaticPaths/astro server), use absolute to backend
  resolvedBase = 'http://localhost:5123/api';
}
export const API_BASE = resolvedBase;

async function handle<T>(res: Response): Promise<T> {
  if (!res.ok) {
    const text = await res.text();
    throw new Error(`${res.status} ${res.statusText}: ${text}`);
  }
  return res.json();
}

export interface WorldStatus { maxEntities: number; recycledEntityIds: number; registeredSystems: number; componentTypes: number; tick: number; deltaTime: number; }
export interface EntitySummary { id: number; url: string; }
export interface ComponentInfo { typeName: string; data?: unknown; pluginOwner?: string; }
export interface EntityDetail { id: number; components: ComponentInfo[]; }
export interface SystemInfo { name: string; enabled: boolean; pluginOwner: string; }
export interface ServiceInfo { typeName: string; methods: string[]; pluginOwner: string; }
export interface PluginInfo { name: string; version: string; author: string; description: string; prefix: string; url: string; }
export interface PluginDetail extends PluginInfo { systems: string[]; services: string[]; components: string[]; }

export const api = {
  worldStatus: () => fetch(`${API_BASE}/world/status`).then(handle<WorldStatus>),
  entities: () => fetch(`${API_BASE}/entities`).then(handle<EntitySummary[]>),
  entity: (idGen: string) => fetch(`${API_BASE}/entities/${idGen}`).then(handle<EntityDetail>),
  createEntity: () => fetch(`${API_BASE}/entities`, { method: 'POST' }).then(handle<EntitySummary>),
  deleteEntity: (idGen: string) => fetch(`${API_BASE}/entities/${idGen}`, { method: 'DELETE' }).then(handle<{ message: string }>),
  addComponent: (idGen: string, type: string, data: unknown) => fetch(`${API_BASE}/entities/${idGen}/components/${type}`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(data) }).then(handle<{ message: string }>),
  removeComponent: (idGen: string, type: string) => fetch(`${API_BASE}/entities/${idGen}/components/${type}`, { method: 'DELETE' }).then(handle<{ message: string }>),
  systems: () => fetch(`${API_BASE}/systems`).then(handle<SystemInfo[]>),
  disableSystem: (name: string) => fetch(`${API_BASE}/systems/${encodeURIComponent(name)}/disable`, { method: 'POST' }).then(handle<{ message: string }>),
  enableSystem: (name: string) => fetch(`${API_BASE}/systems/${encodeURIComponent(name)}/enable`, { method: 'POST' }).then(handle<{ message: string }>),
  components: () => fetch(`${API_BASE}/components`).then(handle<ComponentInfo[]>),
  services: () => fetch(`${API_BASE}/services`).then(handle<ServiceInfo[]>),
  invokeService: (type: string, method: string, params: Record<string, unknown>) => fetch(`${API_BASE}/services/${encodeURIComponent(type)}/${method}`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(params) }).then(handle<unknown>),
  plugins: () => fetch(`${API_BASE}/plugins`).then(handle<PluginInfo[]>),
  pluginDetail: (prefix: string) => fetch(`${API_BASE}/plugins/${prefix}`).then(handle<PluginDetail>)
};
