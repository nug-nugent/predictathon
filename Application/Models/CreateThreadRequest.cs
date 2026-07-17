namespace Predictathon.Application.Models;

public class CreateThreadRequest
{
    public string Subject { get; set; } = "";

    public string FirstMessageContent { get; set; } = "";
}
