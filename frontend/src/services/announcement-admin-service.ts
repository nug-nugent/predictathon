import { getJsonAuthenticated, postJsonAuthenticated, putJsonAuthenticated, deleteAuthenticated } from "./api";
import type { AnnouncementSeverity } from "./announcement-service";

// Matches Application/Models/CreateAnnouncementModel.cs.
export type CreateAnnouncementAdmin = {
    content: string;
    showOnLoginPage: boolean;
    showOnHomepage: boolean;
    severity: AnnouncementSeverity;
    expiryDateTimeUtc: string | null;
};

// Matches Application/Models/AnnouncementModel.cs.
export type AnnouncementAdmin = CreateAnnouncementAdmin & {
    announcementID: number;
    createdAtUtc: string;
    createdByUserID: string;
};

/// Every announcement including expired ones, most recent first, for the Announcements Admin page.
export async function getAllAnnouncements(): Promise<AnnouncementAdmin[]> {
    return getJsonAuthenticated<AnnouncementAdmin[]>("/Announcement");
}

export async function createAnnouncement(model: CreateAnnouncementAdmin): Promise<AnnouncementAdmin> {
    return postJsonAuthenticated<AnnouncementAdmin>("/Announcement", model);
}

export async function updateAnnouncement(announcementId: number, model: AnnouncementAdmin): Promise<AnnouncementAdmin> {
    return putJsonAuthenticated<AnnouncementAdmin>(`/Announcement/${announcementId}`, model);
}

export async function deleteAnnouncement(announcementId: number): Promise<void> {
    return deleteAuthenticated(`/Announcement/${announcementId}`);
}
