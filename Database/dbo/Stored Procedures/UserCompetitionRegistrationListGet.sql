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
		, Competition.CompetitionID
		, Competition.CompetitionName
		, Competition.ImageFilename
		, Competition.StartDate
		, Competition.EntranceFee
		, Registered = CAST(CASE WHEN UserCompetition.UserCompetitionID IS NULL THEN 0 ELSE 1 END AS BIT)
		, IsDefaultCompetition = ISNULL(UserCompetition.IsDefaultCompetition, CAST(0 AS BIT))
	FROM
		Competition
		LEFT JOIN (SELECT * FROM UserCompetition WHERE UserID = @UserID) UserCompetition ON Competition.CompetitionID = UserCompetition.CompetitionID
	WHERE
		(Competition.OpenForRegistration = 1 OR UserCompetition.UserCompetitionID IS NOT NULL)
	ORDER BY
		Competition.StartDate DESC
END
