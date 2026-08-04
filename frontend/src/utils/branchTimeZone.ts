import dayjs from 'dayjs';

export const DEFAULT_BRANCH_TIME_ZONE = 'America/Guayaquil';
const STORAGE_KEY = 'branchTimeZoneId';

export const getStoredBranchTimeZone = () =>
  localStorage.getItem(STORAGE_KEY) || DEFAULT_BRANCH_TIME_ZONE;

export const setBranchTimeZone = (timeZoneId?: string | null) => {
  const value = timeZoneId || DEFAULT_BRANCH_TIME_ZONE;
  localStorage.setItem(STORAGE_KEY, value);
  dayjs.tz.setDefault(value);
};

export const clearBranchTimeZone = () => {
  localStorage.removeItem(STORAGE_KEY);
  dayjs.tz.setDefault(DEFAULT_BRANCH_TIME_ZONE);
};

export const toBranchDayjs = (value?: string | Date | number | null) => {
  if (!value) return null;
  return dayjs(value).tz(getStoredBranchTimeZone());
};

export const formatBranchDateTime = (value?: string | Date | number | null, fallback = '-') =>
  toBranchDayjs(value)?.format('DD/MM/YYYY HH:mm') ?? fallback;

export const formatBranchDateTimeSeconds = (value?: string | Date | number | null, fallback = '-') =>
  toBranchDayjs(value)?.format('DD/MM/YYYY HH:mm:ss') ?? fallback;

export const formatBranchDate = (value?: string | Date | number | null, fallback = '-') =>
  toBranchDayjs(value)?.format('DD/MM/YYYY') ?? fallback;

export const formatBranchTime = (value?: string | Date | number | null, fallback = '-') =>
  toBranchDayjs(value)?.format('HH:mm') ?? fallback;

export const formatBranchTimeSeconds = (value?: string | Date | number | null, fallback = '-') =>
  toBranchDayjs(value)?.format('HH:mm:ss') ?? fallback;

export const branchStartOfDayUtcIso = (value?: dayjs.ConfigType) => {
  if (!value) return undefined;
  const date = dayjs(value).format('YYYY-MM-DD');
  return dayjs.tz(`${date} 00:00:00`, getStoredBranchTimeZone()).toISOString();
};

export const branchEndOfDayUtcIso = (value?: dayjs.ConfigType) => {
  if (!value) return undefined;
  const date = dayjs(value).format('YYYY-MM-DD');
  return dayjs.tz(`${date} 23:59:59.999`, getStoredBranchTimeZone()).toISOString();
};

export const branchDateTimeUtcIso = (value?: dayjs.ConfigType) => {
  if (!value) return undefined;
  const dateTime = dayjs(value).format('YYYY-MM-DD HH:mm:ss');
  return dayjs.tz(dateTime, getStoredBranchTimeZone()).toISOString();
};

export const branchDateRangeToUtcIso = (
  range?: [dayjs.ConfigType | null | undefined, dayjs.ConfigType | null | undefined] | null
) => ({
  from: branchStartOfDayUtcIso(range?.[0] ?? undefined),
  to: branchEndOfDayUtcIso(range?.[1] ?? undefined),
});
