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
