--http://predictathon.co.uk/Pages/Tasks/ScheduledTasks.aspx?TaskToRun=UserCompetitionLeagueHistorySet
DECLARE @ProcessToday BIT = 0;
DECLARE @Date DATE;
DECLARE @CompetitionID UNIQUEIDENTIFIER = (SELECT TOP 1 CompetitionID FROM Competition WHERE CompetitionName = 'Premier League 2018/19');

DECLARE crsMatchDate CURSOR FORWARD_ONLY READ_ONLY FOR
SELECT DISTINCT CAST(MatchDateTime AS DATE) FROM Match WHERE MatchPlayed = 1 AND CAST(MatchDateTime AS DATE) NOT IN (SELECT [Date] FROM UserCompetitionLeagueHistory) AND CompetitionID = @CompetitionID ORDER BY CAST(MatchDateTime AS DATE) ASC

OPEN crsMatchDate
FETCH NEXT FROM crsMatchDate INTO @Date

WHILE @@FETCH_STATUS = 0   
BEGIN 
	IF @ProcessToday = 0 AND @Date = CAST(GETDATE() AS DATE) 
	BEGIN
		CLOSE crsMatchDate;
		DEALLOCATE crsMatchDate;
		RETURN;
	END

	EXECUTE UserCompetitionLeagueHistorySet @Date, @CompetitionID
	FETCH NEXT FROM crsMatchDate INTO @Date
END

CLOSE crsMatchDate;
DEALLOCATE crsMatchDate;