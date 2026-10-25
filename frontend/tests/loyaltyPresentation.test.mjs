import assert from 'node:assert/strict';
import { test } from 'node:test';
import { loyaltyStampCycle, loyaltyEnrollmentUrl, isLocalLoyaltyUrl } from '../src/utils/loyaltyPresentation.ts';

test('five stamps represent only the current cycle, not a reset of total visits', () => {
  for (const [visits, completed, firstVisit, lastVisit] of [[0,0,1,5],[1,1,1,5],[4,4,1,5],[5,0,6,10],[9,4,6,10],[10,0,11,15],[24,4,21,25],[25,0,26,30],[101,1,101,105]]) {
    assert.deepEqual(loyaltyStampCycle(visits), { total:visits, completed, firstVisit, lastVisit });
  }
});
test('invalid visit counts cannot create negative or overflowing stamps', () => {
  for (const value of [-1, NaN, Infinity]) assert.deepEqual(loyaltyStampCycle(value), {total:0,completed:0,firstVisit:1,lastVisit:5});
  assert.equal(loyaltyStampCycle(4.5).completed,4);
});
test('enrollment QR targets the public site and never a private card or table', () => {
  assert.equal(loyaltyEnrollmentUrl('https://www.restaurant.test/', 'https://erp.restaurant.test'), 'https://www.restaurant.test/fidelizacion');
  assert.equal(loyaltyEnrollmentUrl('https://restaurant.test/fidelizacion/private?token=secret#card', 'https://erp.restaurant.test'), 'https://restaurant.test/fidelizacion');
  assert.equal(loyaltyEnrollmentUrl(undefined, 'http://localhost:5180'), 'http://localhost:5180/fidelizacion');
  for (const invalid of ['javascript:alert(1)', 'data:text/html,test', 'https://user:password@restaurant.test', 'not a url'])
    assert.equal(loyaltyEnrollmentUrl(invalid,'http://localhost'),null);
});
test('local and private addresses are marked as testing links', () => {
  for (const host of ['localhost','127.0.0.1','192.168.5.30','10.0.0.1','172.16.0.2','172.31.255.1','[::1]','restaurant.local'])
    assert.equal(isLocalLoyaltyUrl(`http://${host}:5180/fidelizacion`),true);
  for (const host of ['www.elcalderoflameante.com','172.32.0.1','restaurant.test'])
    assert.equal(isLocalLoyaltyUrl(`https://${host}/fidelizacion`),false);
});
