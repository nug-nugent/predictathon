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

export async function postJson<TResponse>(path: string, body: unknown): Promise<TResponse> {
    let response: Response;

    try {
        response = await fetch(`${API_BASE_URL}${path}`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(body),
        });
    } catch {
        throw new ApiError(0, ["Unable to reach the server. Please try again."]);
    }

    if (!response.ok) {
        throw await toApiError(response);
    }

    return response.json();
}
