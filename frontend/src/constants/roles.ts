// Mirrors Application/Constants/RoleConstants.cs on the backend - keep these in sync.
export const Role = {
    MatchAdministrator: "MatchAdministrator",
    UserAdministrator: "UserAdministrator",
    CompetitionAdministrator: "CompetitionAdministrator",
} as const;

export type Role = typeof Role[keyof typeof Role];
