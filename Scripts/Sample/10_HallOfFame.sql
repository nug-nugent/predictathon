/*
Past winners, so profile trophies and the Hall of Fame page have something to show. Deliberately
shaped like production's oldest entries: CompetitionID is NULL (these competitions predate the
Competition table) and the series is set on the Hall of Fame row itself, which is the only way
those rows can ever be classified.

Between them the rows cover every case the trophy grouping has to handle - repeated wins in one
series collapsing into a counted badge (DemoPredictor's three World Cups), a single win in a
series (DemoAdmin's Euros), and a win in a one-off competition belonging to no series at all
(the Millennium Shield), which stays its own trophy named after the competition.

Sample Cup itself is left without an entry on purpose: it still has matches to play, so the admin
Hall of Fame generation flow stays exercisable against it.
*/

SET NOCOUNT ON

MERGE INTO [dbo].[HallOfFame] AS [Target]
USING (VALUES
 ('EA000000-0000-0000-0000-000000000001','World Cup 1998','B1111111-1111-1111-1111-111111111111','1998-07-12','CA4D2ADD-8728-430C-4712-08DEE0003952','DA000000-0000-0000-0000-000000000001','DA000000-0000-0000-0000-000000000002')
,('EA000000-0000-0000-0000-000000000002','World Cup 2006','B1111111-1111-1111-1111-111111111111','2006-07-09','CA4D2ADD-8728-430C-4712-08DEE0003952','DA000000-0000-0000-0000-000000000003','DA000000-0000-0000-0000-000000000004')
,('EA000000-0000-0000-0000-000000000003','World Cup 2014','B1111111-1111-1111-1111-111111111111','2014-07-13','CA4D2ADD-8728-430C-4712-08DEE0003952','DA000000-0000-0000-0000-000000000005','DA000000-0000-0000-0000-000000000006')
,('EA000000-0000-0000-0000-000000000004','Euro 2004','B2222222-2222-2222-2222-222222222222','2004-07-04','CC44CE3F-CC89-44E9-98E1-08DEE1D3E748','CA4D2ADD-8728-430C-4712-08DEE0003952','DA000000-0000-0000-0000-000000000007')
,('EA000000-0000-0000-0000-000000000005','Premier League 2015/16','B3333333-3333-3333-3333-333333333333','2016-05-15','DA000000-0000-0000-0000-000000000001','DA000000-0000-0000-0000-000000000008','DA000000-0000-0000-0000-000000000009')
,('EA000000-0000-0000-0000-000000000006','Premier League 2018/19','B3333333-3333-3333-3333-333333333333','2019-05-12','CC44CE3F-CC89-44E9-98E1-08DEE1D3E748','DA000000-0000-0000-0000-000000000010','CA4D2ADD-8728-430C-4712-08DEE0003952')
,('EA000000-0000-0000-0000-000000000007','Millennium Shield',NULL,'2000-12-31','CA4D2ADD-8728-430C-4712-08DEE0003952','DA000000-0000-0000-0000-000000000002','DA000000-0000-0000-0000-000000000003')
,('EA000000-0000-0000-0000-000000000008','Coronation Cup',NULL,'2002-06-02','CC44CE3F-CC89-44E9-98E1-08DEE1D3E748','DA000000-0000-0000-0000-000000000004','DA000000-0000-0000-0000-000000000005')
) AS [Source] ([HallOfFameID], [CompetitionName], [CompetitionSeriesID], [EndDate], [WinnerUserID], [SecondPlaceUserID], [ThirdPlaceUserID])
ON ([Target].[HallOfFameID] = CONVERT(UNIQUEIDENTIFIER, [Source].[HallOfFameID]))
WHEN NOT MATCHED BY TARGET THEN
    INSERT ([HallOfFameID], [CompetitionID], [CompetitionName], [CompetitionSeriesID], [EndDate],
            [Winner], [WinnerUserID], [SecondPlace], [SecondPlaceUserID], [ThirdPlace], [ThirdPlaceUserID])
    VALUES (CONVERT(UNIQUEIDENTIFIER, [Source].[HallOfFameID]), NULL, [Source].[CompetitionName],
            CONVERT(UNIQUEIDENTIFIER, [Source].[CompetitionSeriesID]), CONVERT(DATE, [Source].[EndDate]),
            (SELECT CAST(u.UserName AS VARCHAR(50)) FROM [Identity].[Users] u WHERE u.Id = CONVERT(UNIQUEIDENTIFIER, [Source].[WinnerUserID])),
            CONVERT(UNIQUEIDENTIFIER, [Source].[WinnerUserID]),
            (SELECT CAST(u.UserName AS VARCHAR(50)) FROM [Identity].[Users] u WHERE u.Id = CONVERT(UNIQUEIDENTIFIER, [Source].[SecondPlaceUserID])),
            CONVERT(UNIQUEIDENTIFIER, [Source].[SecondPlaceUserID]),
            (SELECT CAST(u.UserName AS VARCHAR(50)) FROM [Identity].[Users] u WHERE u.Id = CONVERT(UNIQUEIDENTIFIER, [Source].[ThirdPlaceUserID])),
            CONVERT(UNIQUEIDENTIFIER, [Source].[ThirdPlaceUserID]));

-- Sample Cup belongs to a series too, purely so the admin form's series picker has a competition
-- with something selected to look at. Guarded so it never overwrites a hand-made change.
UPDATE [dbo].[Competition]
SET [CompetitionSeriesID] = 'B1111111-1111-1111-1111-111111111111'
WHERE [CompetitionID] = 'CA000000-0000-0000-0000-000000000001'
  AND [CompetitionSeriesID] IS NULL;
