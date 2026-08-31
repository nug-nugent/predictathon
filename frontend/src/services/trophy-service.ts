// Matches Application/Models/UserTrophyModel.cs. A trophy is a competition win, grouped by series:
// repeated wins in the same series arrive as one entry carrying `winCount`, while a win in a
// competition belonging to no series arrives on its own, named after that competition.
export type UserTrophy = {
    competitionSeriesID: string | null;
    name: string;
    shortName: string | null;
    badgeIcon: string | null;
    badgeColour: string | null;
    displayOrder: number;
    winCount: number;
    mostRecentWin: string;
    /// The years won, oldest first, comma separated - e.g. "2010, 2014, 2022".
    years: string;
};
