namespace Predictathon.Application.Constants;

/// <summary>
/// The shared colour palette used for outbound HTML emails. Deliberately kept in step with the
/// live site's own brand tokens (frontend/src/theme.ts: brand.headerBg, action.fg,
/// border.hairline, nav.fg) rather than invented separately, so emails read as the same product
/// as the site. Centralised here so both the branded email shell (EmailService) and any content
/// built inside it (e.g. the prediction reminder's data table) use the same values.
/// </summary>
public static class EmailStyle
{
    public const string HeaderBlue = "#1E4FD1";

    public const string BodyInk = "#1C1D21";

    public const string FooterBg = "#F7F8FA";

    public const string FooterBorder = "#E4E6EB";

    public const string FooterInk = "#6B7280";
}
