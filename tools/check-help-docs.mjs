import assert from 'node:assert/strict';
import { readFileSync, readdirSync, existsSync } from 'node:fs';
import { createRequire } from 'node:module';
import { resolve, dirname, basename } from 'node:path';
import { pathToFileURL } from 'node:url';
const root = resolve(import.meta.dirname, '..');
const require = createRequire(resolve(root, 'frontend/package.json'));
const { fromMarkdown } = await import(pathToFileURL(require.resolve('mdast-util-from-markdown')).href);
const manual = resolve(root, 'docs/manual');
const catalog = JSON.parse(readFileSync(resolve(manual, 'catalog.json'), 'utf8'));
const ids = new Set(catalog.articles.map(a => a.id));
assert.equal(ids.size, catalog.articles.length);
const assets = new Map(catalog.assets.map(a => [a.id, a]));
assert.equal(assets.size, catalog.assets.length);
let links = 0;
function walk(node, visit) { visit(node); for (const child of node.children ?? []) walk(child, visit); }
for (const article of catalog.articles) {
  assert.equal(article.file, `articles/${article.id}.md`);
  assert.match(article.id, /^[a-z0-9]+(?:-[a-z0-9]+)*$/);
  assert.match(article.verifiedOn, /^\d{4}-\d{2}-\d{2}$/);
  assert.ok(article.title && article.summary && article.audience && article.verification.length > 20);
  const text = readFileSync(resolve(manual, article.file), 'utf8');
  assert.ok(text.startsWith('## '));
  walk(fromMarkdown(text), node => {
    assert.notEqual(node.type, 'html', 'Raw HTML is not supported');
    if (node.type === 'link') {
      links++;
      if (node.url.startsWith('/ayuda/')) assert.ok(ids.has(node.url.split('/').at(-1).split('#')[0]), node.url);
      else assert.ok(node.url === '/downloads/grimorio-inventario.apk' || node.url.startsWith('https://'), node.url);
    }
    if (node.type === 'image') {
      assert.ok(node.alt?.length > 8);
      assert.match(node.url, /^asset:[a-z0-9-]+$/);
      const asset = assets.get(node.url.slice(6));
      assert.ok(asset?.articleIds.includes(article.id));
    }
  });
}
for (const file of readdirSync(resolve(manual, 'articles'))) assert.ok(ids.has(basename(file, '.md')), file);
for (const asset of assets.values()) {
  assert.equal(asset.file, `assets/${asset.id}.png`);
  for (const id of asset.articleIds) assert.ok(ids.has(id));
  const bytes = readFileSync(resolve(manual, asset.file));
  assert.ok(bytes.length > 1000 && bytes.subarray(0, 8).equals(Buffer.from([137,80,78,71,13,10,26,10])));
}
for (const relative of ['docs/README.md','docs/staff-inventory-counts.md','docs/CONTRIBUTING.md','docs/documentation-audit.md','docs/technical/help-center.md','docs/technical/inventory-counts.md','mobile/inventory_app/README.md']) {
  const file = resolve(root, relative);
  walk(fromMarkdown(readFileSync(file, 'utf8')), node => {
    if (node.type === 'link' && !node.url.startsWith('https:') && !node.url.startsWith('#')) {
      assert.ok(existsSync(resolve(dirname(file), decodeURIComponent(node.url.split('#')[0]))), `Broken link: ${relative}: ${node.url}`);
      links++;
    }
  });
}
const audit = readFileSync(resolve(root, 'docs/documentation-audit.md'), 'utf8');
for (const file of readdirSync(resolve(root, 'docs')).filter(f => f.endsWith('.md') && !['README.md','CONTRIBUTING.md','documentation-audit.md'].includes(f))) assert.ok(audit.includes(file), file);
assert.ok(!readFileSync(resolve(root, 'backend/Grimorio.API/Grimorio.API.csproj'), 'utf8').includes('docs/**/*.md'));
assert.ok(!existsSync(resolve(root, 'frontend/public/manual')) && !existsSync(resolve(root, 'frontend/public/docs')));
console.log(`${catalog.articles.length} guides, ${assets.size} captures and ${links} links verified; document inventory covered. No network or database.`);
