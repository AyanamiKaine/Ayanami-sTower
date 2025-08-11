import { describe, it, expect } from 'vitest';
import { api, type WorldStatus, type EntitySummary } from '../../src/lib/api';

// Provide a mock fetch for unit tests
const g: any = globalThis;

describe('api client shape', () => {
  it('parses world status camelCase', async () => {
    g.fetch = async () => ({ ok: true, json: async () => ({ maxEntities: 1000, recycledEntityIds: 2, registeredSystems: 5, componentTypes: 9, tick: 42, deltaTime: 0.016, isPaused: false }) });
    const ws: WorldStatus = await api.worldStatus();
    expect(ws.maxEntities).toBe(1000);
    expect(ws.registeredSystems).toBe(5);
    expect(ws.tick).toBe(42);
    expect(ws.deltaTime).toBeCloseTo(0.016);
    expect(ws.isPaused).toBe(false);
  });
  it('parses entity summaries camelCase', async () => {
    g.fetch = async () => ({ ok: true, json: async () => ([{ id:1, generation:0, url:'/api/entities/1-0'}]) });
    const list: EntitySummary[] = await api.entities();
    expect(list[0].id).toBe(1);
  });
  it('propagates http errors', async () => {
    g.fetch = async () => ({ ok: false, status:400, statusText:'Bad Request', text: async () => 'oops'});
    await expect(api.worldStatus()).rejects.toThrow(/400/);
  });
});
