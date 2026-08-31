import { getJsonAuthenticated, putJsonAuthenticated, postFormAuthenticated, deleteAuthenticated } from "./api";
import type { MatchPrediction } from "./prediction-service";
import type { UserTrophy } from "./trophy-service";

// Matches Application/Models/UserProfileModel.cs.
export type UserProfile = {
    userID: string;
    username: string;
    caption: string | null;
    location: string | null;
    favouriteTeam: string | null;
    profileText: string | null;
    avatarUrl: string | null;
    avatarLargeUrl: string | null;
    trophies: UserTrophy[];
};

export async function getUserProfile(userId: string): Promise<UserProfile> {
    return getJsonAuthenticated<UserProfile>(`/User/${userId}/Profile`);
}

// Matches Application/Models/UserProfileEditModel.cs / UpdateProfileModel.cs. CanViewMessageboard
// and CanViewHiddenMessageThreads are only persisted server-side when the caller holds
// UserAdministrator - see ProfileEditPage.
export type UserProfileEdit = {
    userId: string;
    userName: string;
    email: string;
    forenames: string | null;
    surname: string | null;
    favouriteTeam: string | null;
    location: string | null;
    caption: string | null;
    profileText: string | null;
    emailPredictionReminderDays: number | null;
    canViewMessageboard: boolean;
    canViewHiddenMessageThreads: boolean;
};

export async function getUserProfileForEdit(userId: string): Promise<UserProfileEdit> {
    return getJsonAuthenticated<UserProfileEdit>(`/User/${userId}/ProfileEdit`);
}

export async function updateProfile(userId: string, model: UserProfileEdit): Promise<UserProfileEdit> {
    return putJsonAuthenticated<UserProfileEdit>(`/User/${userId}/ProfileEdit`, model);
}

export type AvatarCropRect = { x: number; y: number; width: number; height: number };

// The thumbnail and full-size URLs of a freshly uploaded avatar. Both carry the server's version
// stamp, so they replace the previous picture in the UI rather than being served from cache.
export type UploadedAvatar = { avatarUrl: string; avatarLargeUrl: string };

/// Uploads the current user's avatar - the original image plus the crop rectangle chosen in the
/// client-side cropper (in the original image's pixel coordinates). The server derives both the
/// large and thumbnail sizes from this itself, rather than trusting client-produced crops.
export async function uploadAvatar(image: File, crop: AvatarCropRect): Promise<UploadedAvatar> {
    const form = new FormData();
    form.append("image", image);
    form.append("cropX", String(Math.round(crop.x)));
    form.append("cropY", String(Math.round(crop.y)));
    form.append("cropWidth", String(Math.round(crop.width)));
    form.append("cropHeight", String(Math.round(crop.height)));

    return postFormAuthenticated<UploadedAvatar>("/User/Avatar", form);
}

/// Removes the current user's avatar, reverting to the initials fallback.
export async function removeAvatar(): Promise<void> {
    return deleteAuthenticated("/User/Avatar");
}

/// A user's prediction history for a competition, most recent first. Future matches are only
/// included when viewing your own history.
export async function getUserPredictionHistory(competitionId: string, userId: string): Promise<MatchPrediction[]> {
    return getJsonAuthenticated<MatchPrediction[]>(`/Match/${competitionId}/UserPredictions/${userId}`);
}

// Matches Application/Models/CompetitionUserLeagueTableItem.cs.
export type CompetitionUserLeagueTableItem = {
    teamID: string;
    position: number;
    played: number;
    won: number;
    lost: number;
    drawn: number;
    shortName: string;
    points: number;
    goalsFor: number;
    goalsAgainst: number;
    goalDifference: number;
};

/// The league table for a competition as it would be had all of a user's predictions come true.
export async function getUserLeagueTable(competitionId: string, userId: string): Promise<CompetitionUserLeagueTableItem[]> {
    return getJsonAuthenticated<CompetitionUserLeagueTableItem[]>(`/Competition/${competitionId}/UserLeagueTable/${userId}`);
}

// Matches Application/Models/UserCompetitionLeagueHistoryItem.cs.
export type UserCompetitionLeagueHistoryItem = {
    username: string;
    date: string;
    score: number;
    leaguePosition: number;
};

/// A user's league-position history for a competition, ordered by date ascending.
export async function getUserLeagueHistory(competitionId: string, userId: string): Promise<UserCompetitionLeagueHistoryItem[]> {
    return getJsonAuthenticated<UserCompetitionLeagueHistoryItem[]>(`/Competition/${competitionId}/UserLeagueHistory/${userId}`);
}
