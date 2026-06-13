type TableSortable = {
  code?: string | null;
  area?: string | null;
};

const collator = new Intl.Collator('es', { numeric: true, sensitivity: 'base' });

const getTableNumber = (code?: string | null) => {
  const trimmed = code?.trim() ?? '';
  const parsed = Number.parseInt(trimmed, 10);
  return Number.isNaN(parsed) ? null : parsed;
};

export const compareTablesByNumber = <T extends TableSortable>(a: T, b: T) => {
  const aNumber = getTableNumber(a.code);
  const bNumber = getTableNumber(b.code);

  if (aNumber !== null && bNumber !== null && aNumber !== bNumber) {
    return aNumber - bNumber;
  }

  if (aNumber !== null && bNumber === null) return -1;
  if (aNumber === null && bNumber !== null) return 1;

  const codeCompare = collator.compare(a.code ?? '', b.code ?? '');
  if (codeCompare !== 0) return codeCompare;

  return collator.compare(a.area ?? '', b.area ?? '');
};
