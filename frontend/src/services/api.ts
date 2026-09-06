// Falls back to a same-origin relative path when unset, so a production build that never had
// VITE_API_BASE_URL configured still works for the common case of the API being hosted as an IIS
// sub-application (e.g. "/api") under the same domain as the frontend, rather than silently
// sending requests to "undefined/...". This is also the correct base for the non-REST endpoints
// below (SignalR hub, static /reactions mount) - whatever prefix routes a request to the API at
// all (an IIS virtual-application segment, or a distinct origin/port in dev) applies uniformly to
// everything the API serves, not just its REST controllers.
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || "/api";

export function getHubUrl(hubPath: string): string {
    return `${API_BASE_URL}${hubPath}`;
}

// Absolute URL for a file under the backend's static /reactions mount (WebApi/Assets/Reactions) -
// must be absolute, not a bare path, since these render directly in the browser (picker preview,
// message reactions) and the frontend's own origin differs from the API's.
export function getReactionImageUrl(filename: string): string {
    return `${API_BASE_URL}/reactions/${filename}`;
}

// The `type` value AuthController/ApiControllerBase uses for a login failure caused by an
// account having no usable password yet (e.g. one migrated from the legacy dbo.User table) -
// lets callers show a "reset your password" prompt instead of a generic login failure.
export const PASSWORD_RESET_REQUIRED_ERROR_TYPE = "https://example.com/probs/password-reset-required";

// Thrown for any failed API call. `messages` holds human-readable error text extracted from the
// API's RFC7807 ProblemDetails response where available. `type` is the ProblemDetails `type` URI,
// letting callers distinguish specific failure kinds (see PASSWORD_RESET_REQUIRED_ERROR_TYPE).
export class ApiError extends Error {
    status: number;
    messages: string[];
    type?: string;

    constructor(status: number, messages: string[], type?: string) {
        super(messages.join(" "));
        this.status = status;
        this.messages = messages;
        this.type = type;
    }
}

type ProblemDetails = {
    type?: string;
    title?: string;
    detail?: string;
    errors?: Record<string, string[]>;
};

async function toApiError(response: Response): Promise<ApiError> {
    let messages: string[] = [];
    let type: string | undefined;

    try {
        const problem = (await response.json()) as ProblemDetails;
        type = problem.type;
        if (problem.errors) {
            messages = Object.values(problem.errors).flat();
        } else if (problem.detail) {
            messages = [problem.detail];
        } else if (problem.title) {
            messages = [problem.title];
        }
    } catch {
        // Response body wasn't ProblemDetails JSON - fall through to the generic message.
    }

    if (messages.length === 0) {
        messages = [`The request failed (${response.status}).`];
    }

    return new ApiError(response.status, messages, type);
}

async function handleJsonResponse<TResponse>(response: Response): Promise<TResponse> {
    if (!response.ok) {
        throw await toApiError(response);
    }

    if (response.status === 204) {
        return undefined as TResponse;
    }

    return response.json() as Promise<TResponse>;
}

// Guards against a burst of concurrent background calls (e.g. the home page's several parallel
// card fetches) each independently triggering their own reload once one 503 has already done so.
let reloadedForOutage = false;

async function doFetch(path: string, init: RequestInit): Promise<Response> {
    let response: Response;

    try {
        response = await fetch(`${API_BASE_URL}${path}`, {
            // Sends/receives the HttpOnly refresh-token cookie. Requires the API's CORS policy to
            // use an explicit origin allow-list + AllowCredentials() - never AllowAnyOrigin().
            credentials: "include",
            ...init,
        });
    } catch {
        throw new ApiError(0, ["Unable to reach the server. Please try again."]);
    }

    // 503 only ever comes from the API being taken down for an upgrade (ANCM serving its
    // app_offline.htm), never from application logic. Without this, a tab that was already open
    // when the upgrade started just keeps running and renders a raw "request failed (503)" in
    // whichever sections happened to refetch. Reloading hands off to the frontend's own offline
    // page - during an upgrade index.html *is* that page (see README, "Taking the site offline").
    if (response.status === 503 && !reloadedForOutage) {
        reloadedForOutage = true;
        window.location.reload();
    }

    return response;
}

export async function getJson<TResponse>(path: string): Promise<TResponse> {
    const response = await doFetch(path, {});
    return handleJsonResponse<TResponse>(response);
}

export async function postJson<TResponse>(path: string, body: unknown): Promise<TResponse> {
    const response = await doFetch(path, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(body),
    });

    return handleJsonResponse<TResponse>(response);
}

// --- Authenticated requests ---
//
// The access token lives only in this module-level variable, kept in sync with UserProvider's
// React state via setAccessToken(). This lets plain functions (not just components) attach it to
// requests and refresh it on demand.

let currentToken: string | null = null;
let onSessionExpired: (() => void) | null = null;

export function setAccessToken(token: string | null): void {
    currentToken = token;
}

// Read by the SignalR hub client, which can't attach an Authorization header to a WebSocket
// handshake and instead sends the token as a connection query-string parameter.
export function getAccessToken(): string | null {
    return currentToken;
}

// Registered by UserProvider to clear the logged-in session when a background token refresh fails.
export function setSessionExpiredHandler(handler: (() => void) | null): void {
    onSessionExpired = handler;
}

type RefreshResult = { token: string; expiresAtUtc: string };

// The single in-flight refresh call, shared by every concurrent 401. Without this, N requests
// failing at once would fire N parallel /Auth/Refresh calls - and since the API rotates the
// refresh token on use, all but the winner would fail and spuriously log the user out.
let refreshInFlight: Promise<RefreshResult> | null = null;

/// Exchanges the HttpOnly refresh-token cookie for a fresh access token, updating the in-memory
/// token store immediately so subsequent requests (including an in-flight retry) pick it up.
/// Concurrent callers share one refresh request. Throws (ApiError) if there's no valid session.
export function refreshAccessToken(): Promise<RefreshResult> {
    refreshInFlight ??= postJson<RefreshResult>("/Auth/Refresh", {})
        .then((result) => {
            currentToken = result.token;
            return result;
        })
        .finally(() => {
            refreshInFlight = null;
        });

    return refreshInFlight;
}

function authHeaders(): HeadersInit {
    return currentToken ? { Authorization: `Bearer ${currentToken}` } : {};
}

/// Fetches an authenticated endpoint, attaching the current access token. If the token was missing
/// or expired (401), transparently refreshes it once and retries before giving up. If the refresh
/// itself fails - no valid session - notifies the registered session-expired handler and rethrows.
async function authenticatedFetch(path: string, init: RequestInit): Promise<Response> {
    const attempt = () => doFetch(path, { ...init, headers: { ...init.headers, ...authHeaders() } });

    const response = await attempt();
    if (response.status !== 401) {
        return response;
    }

    try {
        await refreshAccessToken();
    } catch (error) {
        onSessionExpired?.();
        throw error;
    }

    return attempt();
}

export async function getJsonAuthenticated<TResponse>(path: string): Promise<TResponse> {
    const response = await authenticatedFetch(path, {});
    return handleJsonResponse<TResponse>(response);
}

/// `keepalive` lets a POST outlive the page that started it, for the one case that needs it: a
/// pending prediction flushed as the tab is hidden or closed. A normal fetch is cancelled at that
/// point and the edit would be lost. Not a general-purpose option - keepalive bodies are capped at
/// 64KB and a keepalive request can't be retried after a 401, so the silent token refresh above is
/// effectively one-shot for it.
export async function postJsonAuthenticated<TResponse>(path: string, body: unknown, options?: { keepalive?: boolean }): Promise<TResponse> {
    const response = await authenticatedFetch(path, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(body),
        keepalive: options?.keepalive,
    });

    return handleJsonResponse<TResponse>(response);
}

export async function putJsonAuthenticated<TResponse>(path: string, body: unknown): Promise<TResponse> {
    const response = await authenticatedFetch(path, {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(body),
    });

    return handleJsonResponse<TResponse>(response);
}

export async function deleteJsonAuthenticated<TResponse>(path: string): Promise<TResponse> {
    const response = await authenticatedFetch(path, { method: "DELETE" });
    return handleJsonResponse<TResponse>(response);
}

// For DELETE endpoints whose response body callers don't care about.
export async function deleteAuthenticated(path: string): Promise<void> {
    return deleteJsonAuthenticated<void>(path);
}

// No Content-Type header - the browser sets the multipart boundary itself when the body is a
// FormData, and overriding it manually would drop that boundary and break parsing server-side.
export async function postFormAuthenticated<TResponse>(path: string, form: FormData): Promise<TResponse> {
    const response = await authenticatedFetch(path, { method: "POST", body: form });
    return handleJsonResponse<TResponse>(response);
}
