import { ApiError, isApiError } from '../core/api-error';

/** True when the API could not be reached (see toApiError: network error, or a proxy reporting the API is down). */
export function isServerUnreachable(error: ApiError | null | undefined): boolean {
  return error?.status === 0;
}

/** A short Hebrew message for a snackbar. 400 shows the ProblemDetails detail from the server. */
export function errorMessage(error: unknown): string {
  if (!isApiError(error)) {
    return 'אירעה שגיאה לא צפויה.';
  }
  if (isServerUnreachable(error)) {
    return 'השרת אינו זמין כרגע. אפשר לנסות שוב בעוד רגע.';
  }
  if (error.status === 400) {
    const firstFieldError = error.errors ? Object.values(error.errors).flat()[0] : undefined;
    return error.detail ?? firstFieldError ?? error.title;
  }
  if (error.status === 404) {
    return 'הפנייה לא נמצאה';
  }
  return `אירעה שגיאה בשרת (${error.status}). אפשר לנסות שוב.`;
}
