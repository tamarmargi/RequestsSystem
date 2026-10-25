import { Injectable, signal } from '@angular/core';

const STORAGE_KEY = 'requests.userName';
export const DEFAULT_USER_NAME = 'משתמש';
/** Matches the server's ChangedBy length. */
export const USER_NAME_MAX_LENGTH = 100;

/**
 * The name of the current user, sent as X-User-Name and stored by the server as changedBy.
 * There is no authentication yet; the name is chosen by the user and kept in localStorage.
 */
@Injectable({ providedIn: 'root' })
export class CurrentUserService {
  private readonly name = signal(readStoredName() ?? DEFAULT_USER_NAME);

  readonly userName = this.name.asReadonly();

  setUserName(value: string): void {
    const trimmed = value.trim().slice(0, USER_NAME_MAX_LENGTH);
    const next = trimmed || DEFAULT_USER_NAME;
    this.name.set(next);
    try {
      localStorage.setItem(STORAGE_KEY, next);
    } catch {
      // Storage can be unavailable (private mode, blocked site data): keep the name for this session only.
    }
  }
}

function readStoredName(): string | null {
  try {
    return localStorage.getItem(STORAGE_KEY)?.trim() || null;
  } catch {
    return null;
  }
}
