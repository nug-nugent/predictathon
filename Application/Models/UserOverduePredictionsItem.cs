namespace Predictathon.Application.Models;

/// <summary>
/// One overdue prediction for a user/competition, as returned by the UserOverduePredictionsGet
/// stored procedure. A user with overdue predictions in multiple competitions appears once per
/// competition.
/// </summary>
public class UserOverduePredictionsItem
{
    public string Username { get; set; } = "";

    public Guid UserID { get; set; }

    public Guid UserCompetitionID { get; set; }

    public string EmailAddress { get; set; } = "";

    public string CompetitionName { get; set; } = "";

    public DateTime NextPredictionDue { get; set; }
}
