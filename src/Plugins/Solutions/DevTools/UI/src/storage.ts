const HEADERS_KEY = "grpc-ui-headers";

/** Loads the session's saved metadata headers or the default authorization header. */
export function loadPersistedHeaders(): string
{
  return sessionStorage.getItem(HEADERS_KEY) ?? "Authorization: Bearer ";
}

/** Saves metadata headers for the current browser session. */
export function persistHeaders(text: string): void
{
  sessionStorage.setItem(HEADERS_KEY, text);
}
