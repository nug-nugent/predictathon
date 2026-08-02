namespace Predictathon.Application.Models;

/// <summary>
/// One row of the admin Error Log page - a single Warning-or-above event written to
/// <c>dbo.ErrorLog</c> by the Serilog MSSqlServer sink.
/// </summary>
public class ErrorLogListItem
{
    public int Id { get; set; }

    public string? Level { get; set; }

    /// <summary>The rendered log message.</summary>
    public string? Message { get; set; }

    /// <summary>When the event occurred, in UTC (kind is set so it serialises with a Z suffix).</summary>
    public DateTime TimeStampUtc { get; set; }

    /// <summary>Full exception detail (type, message, stack trace), when the event carried one.</summary>
    public string? Exception { get; set; }

    /// <summary>The event's structured properties, as the sink's XML blob.</summary>
    public string? Properties { get; set; }
}
