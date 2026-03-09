/**
 * Typed error class for API responses.
 * Thrown by apiClient when the server returns a non-2xx status.
 */
export class ApiError extends Error {
  constructor(
    public readonly code: string,
    message: string,
    public readonly status: number,
  ) {
    super(message);
    this.name = "ApiError";
  }
}

/** Standard error response shape from the Azure Functions API. */
export interface ApiErrorResponse {
  code: string;
  message: string;
}
