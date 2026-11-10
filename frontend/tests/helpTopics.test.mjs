import assert from 'node:assert/strict';
import test from 'node:test';
import { readFileSync, existsSync } from 'node:fs';
import { resolve } from 'node:path';
import { helpTopics } from '../src/utils/helpTopics.ts';

const root = resolve(import.meta.dirname, '../..');
const catalog = JSON.parse(readFileSync(resolve(root, 'docs/manual/catalog.json'), 'utf8'));
const articles = new Map(catalog.articles.map(article => [article.id, article]));

test('every ERP module has a catalogued contextual guide', () => {
  const source = readFileSync(resolve(root, 'frontend/src/pages/Dashboard.tsx'), 'utf8');
  const cases = [...source.matchAll(/case '([^']+)':/g)].map(match => match[1]);
  assert.ok(cases.length > 40);
  for (const key of cases.filter(key => !['welcome', 'profile'].includes(key))) {
    assert.ok(articles.has(helpTopics[key]), `Missing guide for ${key}`);
  }
  for (const [key, id] of Object.entries(helpTopics)) {
    assert.ok(cases.includes(key), `Unknown ERP module ${key}`);
    assert.ok(articles.has(id), `Unknown guide ${id}`);
  }
});

test('new operational guides retain source references without serving technical files', () => {
  const expanded = catalog.articles.filter(article => article.sources);
  assert.equal(expanded.length, 21);
  for (const article of expanded) {
    assert.ok(article.permissionSets.length > 0);
    for (const source of article.sources) assert.ok(existsSync(resolve(root, source)), source);
    assert.ok(readFileSync(resolve(root, 'docs/manual', article.file), 'utf8').length > 800);
  }
});
