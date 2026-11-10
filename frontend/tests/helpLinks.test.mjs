import test from 'node:test';
import assert from 'node:assert/strict';
import { helpLink } from '../src/utils/helpLinks.ts';

test('manual links allow only intentional navigation, HTTPS references and authenticated images', () => {
  for (const link of ['/ayuda', '/ayuda/inventario-contar', '/ayuda/inicio#requisitos', '/downloads/grimorio-inventario.apk', 'https://example.invalid/guide'])
    assert.equal(helpLink(link), link);
  assert.equal(helpLink('asset:inventario-configuracion', true), 'asset:inventario-configuracion');
  for (const link of ['javascript:alert(1)', 'data:text/html,hello', '//tracker.invalid/image', '/api/users', '../.env', '/docs/technical', 'http://example.invalid', 'https://user:password@example.invalid', '/ayuda/../../login'])
    assert.equal(helpLink(link), '', link);
  for (const link of ['https://tracker.invalid/image.png', '/downloads/image.png', 'data:image/png;base64,xxx', 'asset:../private'])
    assert.equal(helpLink(link, true), '', link);
});
