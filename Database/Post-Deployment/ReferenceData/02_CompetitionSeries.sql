/*
The competition series a competition can belong to - the grouping behind profile trophies, so that
winning the World Cup three times reads as one badge with a count rather than three unrelated ones.

Reference data with fixed, well-known GUIDs rather than an admin-maintained table: a series is a
long-lived taxonomy that changes once every few years, not something worth a CRUD screen. Which
competition belongs to which series is per-competition data, set on the Competition admin page.

BadgeIcon is a lucide icon name; the frontend maps it to a component and falls back to a trophy for
anything it doesn't recognise, so reusing an existing icon for a new series needs no code change.
*/
MERGE [dbo].[CompetitionSeries] AS target
USING (VALUES
    ('B1111111-1111-1111-1111-111111111111', 'World Cup', 'WC', 'trophy', '#D4AF37', 10),
    ('B2222222-2222-2222-2222-222222222222', 'European Championships', 'EUR', 'star', '#1E4FD1', 20),
    ('B3333333-3333-3333-3333-333333333333', 'Premier League', 'PL', 'crown', '#3D195B', 30)
) AS source ([CompetitionSeriesID], [SeriesName], [ShortName], [BadgeIcon], [BadgeColour], [DisplayOrder])
ON target.[CompetitionSeriesID] = CONVERT(UNIQUEIDENTIFIER, source.[CompetitionSeriesID])
WHEN NOT MATCHED THEN
    INSERT ([CompetitionSeriesID], [SeriesName], [ShortName], [BadgeIcon], [BadgeColour], [DisplayOrder])
    VALUES (CONVERT(UNIQUEIDENTIFIER, source.[CompetitionSeriesID]), source.[SeriesName], source.[ShortName],
            source.[BadgeIcon], source.[BadgeColour], source.[DisplayOrder]);
