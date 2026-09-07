/*
Delete the accounts registration.spec.ts and no-competitions.spec.ts leave behind.

Both mint a username from Date.now() on every run and neither cleans up after itself, so the count
only goes up - a development database a few weeks old had 55 of them, every one sitting in Sample
Cup's league table on nought points. Harmless one at a time, and steadily less so.

Scoped to the two prefixes those specs use, which nothing else in the sample data goes near.

Only the tables a self-registered account can actually reach are cleared. The other ten that
reference Identity.Users need either an admin role or message-board activity that neither spec
performs, so a test account should never appear in one - and if that ever changes, the delete below
fails on the foreign key and says so, which is what we want. Quietly removing a real live score or a
Hall of Fame row to get a test account out of the way would be far worse than a noisy seed.
*/

SET NOCOUNT ON

DECLARE @Purge TABLE (UserID UNIQUEIDENTIFIER PRIMARY KEY);

INSERT INTO @Purge (UserID)
SELECT [Id]
FROM [Identity].[Users]
WHERE [UserName] LIKE 'e2e-test-%' OR [UserName] LIKE 'e2e-nocomp-%';

-- Prediction and PredictionHistory point at each other, so the link has to be broken from the
-- Prediction side before either row can go.
UPDATE p
SET p.[PredictionHistoryID] = NULL
FROM [dbo].[Prediction] AS p
INNER JOIN @Purge AS x ON x.[UserID] = p.[UserID];

DELETE h
FROM [dbo].[PredictionHistory] AS h
INNER JOIN [dbo].[Prediction] AS p ON p.[PredictionID] = h.[PredictionID]
INNER JOIN @Purge AS x ON x.[UserID] = p.[UserID];

DELETE p FROM [dbo].[Prediction] AS p INNER JOIN @Purge AS x ON x.[UserID] = p.[UserID];
DELETE t FROM [dbo].[Transaction] AS t INNER JOIN @Purge AS x ON x.[UserID] = t.[UserID];
DELETE c FROM [dbo].[PaymentCredit] AS c INNER JOIN @Purge AS x ON x.[UserID] = c.[UsedByUserID];
DELETE c FROM [dbo].[PaymentCredit] AS c INNER JOIN @Purge AS x ON x.[UserID] = c.[IssuedByUserID];
DELETE u FROM [dbo].[UserCompetition] AS u INNER JOIN @Purge AS x ON x.[UserID] = u.[UserID];

DELETE r FROM [Identity].[RefreshTokens] AS r INNER JOIN @Purge AS x ON x.[UserID] = r.[UserId];
DELETE c FROM [Identity].[UserClaims] AS c INNER JOIN @Purge AS x ON x.[UserID] = c.[UserId];
DELETE l FROM [Identity].[UserLogins] AS l INNER JOIN @Purge AS x ON x.[UserID] = l.[UserId];
DELETE r FROM [Identity].[UserRoles] AS r INNER JOIN @Purge AS x ON x.[UserID] = r.[UserId];
DELETE t FROM [Identity].[UserTokens] AS t INNER JOIN @Purge AS x ON x.[UserID] = t.[UserId];

DELETE u FROM [Identity].[Users] AS u INNER JOIN @Purge AS x ON x.[UserID] = u.[Id];

DECLARE @Purged INT = (SELECT COUNT(*) FROM @Purge);
PRINT CONCAT('Purged ', @Purged, ' leftover e2e test account(s).');
GO
