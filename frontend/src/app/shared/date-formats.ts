/** DatePipe formats used across the UI (local time, 'he' locale). */
export const DATE_FORMAT = 'dd.MM.yyyy';
export const DATE_TIME_FORMAT = 'dd.MM.yyyy · HH:mm';
export const TIME_FORMAT = 'HH:mm';

const relativeTime = new Intl.RelativeTimeFormat('he', { numeric: 'auto' });

/** "לפני 12 שניות", "לפני 3 דקות"… relative to now. */
export function timeAgo(iso: string, now: Date = new Date()): string {
  const seconds = Math.round((new Date(iso).getTime() - now.getTime()) / 1000);
  const abs = Math.abs(seconds);
  if (abs < 60) return relativeTime.format(seconds, 'second');
  if (abs < 3600) return relativeTime.format(Math.round(seconds / 60), 'minute');
  if (abs < 86400) return relativeTime.format(Math.round(seconds / 3600), 'hour');
  return relativeTime.format(Math.round(seconds / 86400), 'day');
}

/** Converts a yyyy-MM-dd value (from <input type="date"> or the URL) to dd.MM.yyyy for display. */
export function formatIsoDate(value: string): string {
  const [year, month, day] = value.split('-');
  return `${day}.${month}.${year}`;
}
