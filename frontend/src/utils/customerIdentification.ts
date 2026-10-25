export const normalizeCustomerId = (value?: string | null) => (value ?? '').replace(/[\s-]/g, '').toUpperCase();

export function detectCustomerId(value?: string | null): 'Cedula' | 'Ruc' | 'FinalConsumer' | undefined {
  const id = normalizeCustomerId(value);
  if (id === '9999999999999') return 'FinalConsumer';
  if (/^[0-9]{10}$/.test(id)) return 'Cedula';
  if (/^[0-9]{13}$/.test(id)) return 'Ruc';
}

export function validCustomerCedula(id: string): boolean {
  if (!/^[0-9]{10}$/.test(id)) return false;
  const province = Number(id.slice(0, 2));
  if (!((province >= 1 && province <= 24) || province === 30) || Number(id[2]) > 5) return false;
  if (province === 30 && id[2] !== '4' && id[2] !== '5') return false;
  const sum = [...id.slice(0, 9)].reduce((total, char, index) => {
    const product = Number(char) * (index % 2 === 0 ? 2 : 1);
    return total + (product > 9 ? product - 9 : product);
  }, 0);
  return (10 - sum % 10) % 10 === Number(id[9]);
}

export function customerIdentificationError(value: string | undefined, type: string): string | undefined {
  const id = normalizeCustomerId(value);
  if (type === 'Passport') return /^[A-Z0-9]{3,20}$/.test(id) ? undefined : 'El pasaporte debe tener entre 3 y 20 letras o numeros.';
  if ((!id && type === 'FinalConsumer') || id === '9999999999999') return;
  if (/^[0-9]{10}$/.test(id)) return validCustomerCedula(id) ? undefined : 'La cedula ecuatoriana no es valida. Revisa el digito verificador.';
  if (/^[0-9]{13}$/.test(id)) {
    const province = Number(id.slice(0, 2)), third = Number(id[2]);
    if (((province >= 1 && province <= 24) || province === 30) && id.endsWith('001')
      && (third < 6 ? validCustomerCedula(id.slice(0, 10)) : third === 6 || third === 9)) return;
    return 'El RUC no tiene una estructura valida. Debe tener 13 digitos y terminar en 001.';
  }
  return 'Ingresa una cedula de 10 digitos o un RUC de 13 digitos; para otros documentos selecciona Pasaporte.';
}
