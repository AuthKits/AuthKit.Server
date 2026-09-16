const HEADERS_KEY = "grpc-ui-headers";

export function loadPersistedHeaders(): string
{
  return sessionStorage.getItem(HEADERS_KEY) ?? "Authorization: Bearer ";
}

export function persistHeaders(text: string): void
{
  sessionStorage.setItem(HEADERS_KEY, text);
}
