namespace Predictathon.Application.Models;

/// <summary>
/// A row in the messageboard thread list, as returned by the MessageThreadListGet stored procedure.
/// Property names match its output columns exactly so CallStoredProcedureAsync's column-to-property
/// mapping picks them up with no extra configuration.
/// </summary>
public class MessageThreadSummaryModel
{
    public Guid MessageThreadID { get; set; }

    public string ThreadSubject { get; set; } = "";

    public string StartedByUsername { get; set; } = "";

    public DateTime StartedDateTime { get; set; }

    public bool HiddenFromPublic { get; set; }

    public bool Unread { get; set; }

    public int MessageCount { get; set; }

    public DateTime LastMessageDateTime { get; set; }

    public string LastMessageByUsername { get; set; } = "";
}
