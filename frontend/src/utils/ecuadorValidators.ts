// Validators for Ecuador IDs and phone numbers

// Cédula: 10 digits, modulo 10 with province check
export const isValidEcuadorCedula = (value: string): boolean => {
  const ced = (value || '').trim();
  if (!/^\d{10}$/.test(ced)) return false;

  const province = parseInt(ced.slice(0, 2), 10);
  if (province < 1 || province > 24) return false;

  const digits = ced.split('').map(Number);
  const checkDigit = digits[9];

  const sum = digits.slice(0, 9).reduce((acc, digit, idx) => {
    if (idx % 2 === 0) {
      const prod = digit * 2;
      return acc + (prod > 9 ? prod - 9 : prod);
    }
    return acc + digit;
  }, 0);

  const computed = (10 - (sum % 10)) % 10;
  return computed === checkDigit;
};

// RUC: 13 digits. For naturales: first 10 = cédula válida, last 3 = 001
// For simplicity we validate natural person RUC; extend if needed for public/private.
export const isValidEcuadorRuc = (value: string): boolean => {
  const ruc = (value || '').trim();
  if (!/^\d{13}$/.test(ruc)) return false;
  const establishment = ruc.slice(10);
  if (establishment !== '001') return false;
  return isValidEcuadorCedula(ruc.slice(0, 10));
};

// Celular Ecuador: empieza con 09 y 10 dígitos
export const isValidEcuadorCell = (value: string): boolean => {
  const phone = (value || '').trim();
  return /^09\d{8}$/.test(phone);
};
