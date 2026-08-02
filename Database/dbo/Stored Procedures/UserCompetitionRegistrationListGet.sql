-- =============================================
-- Author:		David Huggett
-- Create date: 04/01/2012
-- Description:	Returns a list of competitions either signed up to, or available for signup from the main menu, for a particular user
-- =============================================
CREATE PROCEDURE [dbo].[UserCompetitionRegistrationListGet]
	@UserID UNIQUEIDENTIFIER
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from interfering with SELECT statements.
	SET NOCOUNT ON;
	
	SELECT
		UserCompetition.UserCompetitionID
		, c.CompetitionID
		, c.CompetitionName
		, c.ImageFilename
		, c.StartDate
		, c.EntranceFee
		, Registered = CAST(CASE WHEN UserCompetition.UserCompetitionID IS NULL THEN 0 ELSE 1 END AS BIT)
		, IsDefaultCompetition = ISNULL(UserCompetition.IsDefaultCompetition, CAST(0 AS BIT))
	FROM
		[dbo].[Competition] AS c
		LEFT JOIN (SELECT uc.UserCompetitionID, uc.UserID, uc.CompetitionID, uc.AmountPaid, uc.PaymentProvider, uc.PaymentCreditID, uc.IsDefaultCompetition, uc.LastEmailReminderSent FROM [dbo].[UserCompetition] AS uc WHERE uc.UserID = @UserID) UserCompetition ON c.CompetitionID = UserCompetition.CompetitionID
	WHERE
		(c.OpenForRegistration = 1 OR UserCompetition.UserCompetitionID IS NOT NULL)
	ORDER BY
		c.StartDate DESC
END
