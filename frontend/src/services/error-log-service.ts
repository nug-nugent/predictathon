import { getJsonAuthenticated } from "./api";
import { type PagedResult } from "./users-admin-service";

// Matches Application/Models/ErrorLogListItem.cs.
export type ErrorLogEntry = {
    id: number;
    level: string | null;
    message: string | null;
    timeStampUtc: string;
    exception: string | null;
    properties: string | null;
};

export async function getErrors(page: number, pageSize: number): Promise<PagedResult<ErrorLogEntry>> {
    const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });

    return getJsonAuthenticated<PagedResult<ErrorLogEntry>>(`/ErrorLog?${params.toString()}`);
}
