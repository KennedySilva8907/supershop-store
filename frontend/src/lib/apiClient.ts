import type { ProblemDetails } from "../types/catalog";

const BASE_URL = import.meta.env.VITE_API_URL ?? "";

export class ApiError extends Error {
  readonly status: number;
  readonly problem: ProblemDetails;

  constructor(status: number, problem: ProblemDetails) {
    super(problem.detail ?? problem.title ?? "Ocorreu um erro.");
    this.name = "ApiError";
    this.status = status;
    this.problem = problem;
  }

  get fieldErrors(): Record<string, string[]> {
    return this.problem.errors ?? {};
  }
}

let readAccessToken: () => string | null = () => null;
let refreshSession: () => Promise<boolean> = async () => false;
let onSessionExpired: () => void = () => undefined;
let inFlightRefresh: Promise<boolean> | null = null;

export function configureAuth(handlers: {
  readAccessToken: () => string | null;
  refreshSession: () => Promise<boolean>;
  onSessionExpired: () => void;
}) {
  readAccessToken = handlers.readAccessToken;
  refreshSession = handlers.refreshSession;
  onSessionExpired = handlers.onSessionExpired;
}

const NO_RETRY = ["/auth/refresh", "/auth/login", "/auth/register"];

async function request(method: string, path: string, body?: unknown, signal?: AbortSignal) {
  const headers: Record<string, string> = { Accept: "application/json" };
  const token = readAccessToken();
  const isForm = body instanceof FormData;

  if (token) headers.Authorization = `Bearer ${token}`;
  if (body !== undefined && !isForm) headers["Content-Type"] = "application/json";

  return fetch(`${BASE_URL}/api${path}`, {
    method,
    headers,
    signal,
    credentials: "include",
    body: body === undefined ? undefined : isForm ? body : JSON.stringify(body),
  });
}

async function refreshOnce(): Promise<boolean> {
  inFlightRefresh ??= refreshSession().finally(() => {
    inFlightRefresh = null;
  });

  return inFlightRefresh;
}

async function send<T>(method: string, path: string, body?: unknown, signal?: AbortSignal): Promise<T> {
  let response = await request(method, path, body, signal);

  if (response.status === 401 && !NO_RETRY.includes(path)) {
    if (await refreshOnce()) {
      response = await request(method, path, body, signal);
    } else {
      onSessionExpired();
    }
  }

  if (!response.ok) {
    throw new ApiError(response.status, await readProblem(response));
  }

  return response.status === 204 ? (undefined as T) : ((await response.json()) as T);
}

export const apiGet = <T>(path: string, signal?: AbortSignal) => send<T>("GET", path, undefined, signal);

export const apiSend = <T>(method: string, path: string, body?: unknown) => send<T>(method, path, body);

export const apiUpload = <T>(path: string, form: FormData) => send<T>("POST", path, form);

export async function postRaw<T>(path: string): Promise<T> {
  const response = await fetch(`${BASE_URL}/api${path}`, {
    method: "POST",
    credentials: "include",
    headers: { Accept: "application/json" },
  });

  if (!response.ok) {
    throw new ApiError(response.status, await readProblem(response));
  }

  return (await response.json()) as T;
}

async function readProblem(response: Response): Promise<ProblemDetails> {
  try {
    return (await response.json()) as ProblemDetails;
  } catch {
    return { status: response.status, title: response.statusText };
  }
}

export function buildQuery(params: Record<string, string | number | undefined | null>): string {
  const search = new URLSearchParams();

  for (const [key, value] of Object.entries(params)) {
    if (value !== undefined && value !== null && value !== "") {
      search.set(key, String(value));
    }
  }

  const query = search.toString();
  return query ? `?${query}` : "";
}
