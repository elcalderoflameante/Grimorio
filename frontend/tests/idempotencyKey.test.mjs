import assert from 'node:assert/strict';
import test from 'node:test';
import { createIdempotencyKey } from '../src/utils/idempotencyKey.ts';

const uuid = /^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;

test('uses the browser UUID implementation when available', () => {
  assert.match(createIdempotencyKey(), uuid);
});

test('generates valid independent UUIDs without randomUUID, as on LAN HTTP origins', () => {
  const original = Object.getOwnPropertyDescriptor(globalThis, 'crypto');
  const getRandomValues = crypto.getRandomValues.bind(crypto);
  Object.defineProperty(globalThis, 'crypto', { configurable: true, value: { getRandomValues } });
  try {
    const ids = Array.from({ length: 100 }, createIdempotencyKey);
    ids.forEach(id => assert.match(id, uuid));
    assert.equal(new Set(ids).size, ids.length);
  } finally {
    Object.defineProperty(globalThis, 'crypto', original);
  }
});
