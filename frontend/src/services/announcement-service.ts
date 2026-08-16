import { getJson } from "./api";

// Matches Application/Constants/AnnouncementSeverities.cs.
export type AnnouncementSeverity = "Info" | "Warning";

// Matches Application/Models/ActiveAnnouncementModel.cs.
export type ActiveAnnouncement = {
    announcementID: number;
    content: string;
    showOnLoginPage: boolean;
    showOnHomepage: boolean;
    severity: AnnouncementSeverity;
    createdAtUtc: string;
};

/// Every non-expired announcement, most recent first, for the homepage and login-page feeds.
export async function getActiveAnnouncements(): Promise<ActiveAnnouncement[]> {
    return getJson<ActiveAnnouncement[]>("/Announcement/Active");
}
