namespace Predictathon.Application.Models;

/// <summary>
/// Detail view of a message thread (subject + metadata, not its messages).
/// </summary>
public class MessageThreadModel
{
    public Guid MessageThreadID { get; set; }

    public string ThreadSubject { get; set; } = "";

    public bool HiddenFromPublic { get; set; }
}
