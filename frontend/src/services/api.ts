const API_BASE_URL = import.meta.env.VITE_API_BASE_URL;

// Thrown for any failed API call. `messages` holds human-readable error text extracted from the
// API's RFC7807 ProblemDetails response where available.
export class ApiError extends Error {
    status: number;
    messages: string[];

    constructor(status: number, messages: string[]) {
        super(messages.join(" "));
        this.status = status;
        this.messages = messages;
    }
}

type ProblemDetails = {
    title?: string;
    detail?: string;
    errors?: Record<string, string[]>;
};

async function toApiError(response: Response): Promise<ApiError> {
    let messages: string[] = [];

    try {
        const problem: ProblemDetails = await response.json();
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

    return new ApiError(response.status, messages);
}

async function handleJsonResponse<TResponse>(response: Response): Promise<TResponse> {
    if (!response.ok) {
        throw await toApiError(response);
    }

    if (response.status === 204) {
        return undefined as TResponse;
    }

    return response.json();
}

async function doFetch(path: string, init: RequestInit): Promise<Response> {
    try {
        return await fetch(`${API_BASE_URL}${path}`, {
            // Sends/receives the HttpOnly refresh-token cookie. Requires the API's CORS policy to
            // use an explicit origin allow-list + AllowCredentials() - never AllowAnyOrigin().
            credentials: "include",
            ...init,
        });
    } catch {
        throw new ApiError(0, ["Unable to reach the server. Please try again."]);
    }
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

// Registered by UserProvider to clear the logged-in session when a background token refresh fails.
export function setSessionExpiredHandler(handler: (() => void) | null): void {
    onSessionExpired = handler;
}

type RefreshResult = { token: string; expiresAtUtc: string };

/// Exchanges the HttpOnly refresh-token cookie for a fresh access token, updating the in-memory
/// token store immediately so subsequent requests (including an in-flight retry) pick it up.
/// Throws (ApiError) if there's no valid session.
export async function refreshAccessToken(): Promise<RefreshResult> {
    const result = await postJson<RefreshResult>("/Auth/Refresh", {});
    currentToken = result.token;
    return result;
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

export async function postJsonAuthenticated<TResponse>(path: string, body: unknown): Promise<TResponse> {
    const response = await authenticatedFetch(path, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(body),
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

export async function deleteAuthenticated(path: string): Promise<void> {
    const response = await authenticatedFetch(path, { method: "DELETE" });
    return handleJsonResponse<void>(response);
}
