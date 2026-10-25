import { BulkUpdateOutcome } from '../models/bulk-update.model';
import { RequestPriority, RequestStatus } from '../models/request.model';

export const STATUSES: readonly RequestStatus[] = ['New', 'InProgress', 'Waiting', 'Completed'];

/** Highest first, as in the stats card. */
export const PRIORITIES: readonly RequestPriority[] = ['High', 'Medium', 'Low'];

export const STATUS_LABELS: Record<RequestStatus, string> = {
  New: 'חדשה',
  InProgress: 'בטיפול',
  Waiting: 'ממתינה',
  Completed: 'הושלמה',
};

/** Short explanation shown next to each target status in the status dialog. */
export const STATUS_DESCRIPTIONS: Record<RequestStatus, string> = {
  New: 'פנייה שהתקבלה וטרם טופלה',
  InProgress: 'הפנייה בטיפול פעיל',
  Waiting: 'ממתינה למידע או לגורם חיצוני',
  Completed: 'הפנייה טופלה ונסגרה',
};

export const PRIORITY_LABELS: Record<RequestPriority, string> = {
  High: 'גבוהה',
  Medium: 'בינונית',
  Low: 'נמוכה',
};

export const OUTCOME_LABELS: Record<BulkUpdateOutcome, string> = {
  Updated: 'עודכנה',
  Conflict: 'התנגשות',
  InvalidTransition: 'מעבר לא מותר',
  NotFound: 'לא נמצאה',
};

/** CSS modifier per status / priority / outcome; the colors are tokens in src/styles/_tokens.scss. */
export const STATUS_CLASSES: Record<RequestStatus, string> = {
  New: 'status--new',
  InProgress: 'status--in-progress',
  Waiting: 'status--waiting',
  Completed: 'status--completed',
};

export const PRIORITY_CLASSES: Record<RequestPriority, string> = {
  High: 'priority--high',
  Medium: 'priority--medium',
  Low: 'priority--low',
};

export const OUTCOME_CLASSES: Record<BulkUpdateOutcome, string> = {
  Updated: 'outcome--updated',
  Conflict: 'outcome--conflict',
  InvalidTransition: 'outcome--invalid',
  NotFound: 'outcome--not-found',
};

/**
 * Allowed status transitions, mirrored from the server (docs/api-contract.md) ONLY to show the allowed
 * options. The server enforces the rules and stays the source of truth.
 */
export const ALLOWED_TRANSITIONS: Record<RequestStatus, readonly RequestStatus[]> = {
  New: ['InProgress'],
  InProgress: ['Waiting', 'Completed'],
  Waiting: ['InProgress', 'Completed'],
  Completed: [],
};

export function isTransitionAllowed(from: RequestStatus, to: RequestStatus): boolean {
  return ALLOWED_TRANSITIONS[from].includes(to);
}
