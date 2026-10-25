import { test } from 'node:test';
import assert from 'node:assert/strict';
import { customerIdentificationError, detectCustomerId, normalizeCustomerId, validCustomerCedula } from '../src/utils/customerIdentification.ts';

const cedula = prefix => {
  const sum = [...prefix].reduce((total, c, i) => {
    const n = Number(c) * (i % 2 === 0 ? 2 : 1); return total + (n > 9 ? n - 9 : n);
  }, 0);
  return prefix + (10 - sum % 10) % 10;
};
test('detects and normalizes Ecuadorian IDs without losing leading zeros', () => {
  const id = cedula('010123456');
  assert.equal(normalizeCustomerId(` ${id.slice(0,2)}-${id.slice(2)} `), id);
  assert.equal(detectCustomerId(id), 'Cedula');
  assert.equal(detectCustomerId(id + '001'), 'Ruc');
  assert.equal(detectCustomerId('9999999999999'), 'FinalConsumer');
  assert.equal(validCustomerCedula(id), true);
});
test('cedula checksum, province and third digit validation', () => {
  assert.equal(validCustomerCedula(cedula('304123456')), true);
  for (const value of ['0000000000', cedula('176123456'), cedula('300123456'), '1712345670']) {
    assert.ok(customerIdentificationError(value, 'Cedula'));
  }
});
test('RUC validates natural identities and allows modern public/private sequences', () => {
  for (const id of [cedula('171234567') + '001', '1793189906001', '0993366721001', '1760001550001']) {
    assert.equal(customerIdentificationError(id, 'Ruc'), undefined);
  }
  for (const id of ['1712345670001', '1793189906000', '179318990600', '1793189906ABC']) {
    assert.ok(customerIdentificationError(id, 'Ruc'));
  }
});
test('passport and final consumer retain explicit independent behavior', () => {
  assert.equal(customerIdentificationError('AB-123', 'Passport'), undefined);
  assert.equal(customerIdentificationError('1234567890', 'Passport'), undefined);
  assert.ok(customerIdentificationError('', 'Passport'));
  assert.equal(customerIdentificationError('', 'FinalConsumer'), undefined);
  assert.ok(customerIdentificationError('abc', 'FinalConsumer'));
});
