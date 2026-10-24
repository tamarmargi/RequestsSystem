import { HttpErrorResponse } from '@angular/common/http';

/** Normalized error thrown by the HTTP layer, built from an RFC 7807 ProblemDetails body. */
export interface ApiError {
  /** HTTP status; 0 when the server could not be reached. */
  status: number;
  title: string;
  detail?: string;
  /** Validation errors: field name → messages. */
  errors?: Record<string, string[]>;
  /** On 409: the current RequestDto on the server. */
  current?: unknown;
}

interface ProblemDetails {
  title?: string;
  detail?: string;
  errors?: Record<string, string[]>;
  current?: unknown;
}

export function toApiError(response: HttpErrorResponse): ApiError {
  if (response.status === 0) {
    return { status: 0, title: 'server unreachable' };
  }

  const body = isProblemDetails(response.error) ? response.error : undefined;
  const error: ApiError = {
    status: response.status,
    title: body?.title || response.statusText || `HTTP ${response.status}`,
  };
  if (body?.detail) error.detail = body.detail;
  if (body?.errors) error.errors = body.errors;
  if (body?.current !== undefined) error.current = body.current;
  return error;
}

export function isApiError(value: unknown): value is ApiError {
  return typeof value === 'object' && value !== null
    && typeof (value as ApiError).status === 'number'
    && typeof (value as ApiError).title === 'string';
}

function isProblemDetails(value: unknown): value is ProblemDetails {
  return typeof value === 'object' && value !== null;
}
