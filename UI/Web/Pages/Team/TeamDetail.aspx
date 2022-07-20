<%@ Page Language="vb" AutoEventWireup="false" MasterPageFile="~/Pages/Master/Main.Master" CodeBehind="TeamDetail.aspx.vb" Inherits="Predictathon.Pages.Team.TeamDetail" %>
<%@ MasterType TypeName="Predictathon.Master.Main" %>
<%@ Register Src="~/UserControls/Match/MatchResultList.ascx" TagName="MatchResultList" TagPrefix="uc1" %>

<asp:Content runat="server" ContentPlaceHolderID="MainContent">
    <table width="100%">
        <tr valign="top">
            <td style="width:40%;">
                <div id="divTeamStatistics" runat="server" class="InformationBlock">
                    <div class="TitleBar" style="margin-bottom: 5px;">
                        Team Statistics
                    </div>
                    <h1>
                        <asp:Image ID="imgTeam" runat="server" style="float: left; margin-right: 10px; margin-bottom: 8px" />
                        <asp:Label ID="lblTeamName" runat="server" />
                        <br />
                    </h1>
                    <h2>
                        <asp:Label ID="lblGoalsFor" runat="server" Text="Goals for: " /><br />
                        <asp:Label ID="lblGoalsAgainst" runat="server" Text="Goals against: " />
                    </h2>
                    <table width="100%" style="margin: 5px">
                        <tr id="trAverageGoalsForHome" runat="server">
                            <td>
                                <asp:Label ID="lblAverageGoalsForHome" runat="server" Text="Average goals for (home): " /><br />
                            </td>
                            <td>
                                <asp:Label ID="lblAverageGoalsForHomeValue" runat="server" /><br />
                            </td>
                        </tr>
                        <tr id="trAverageGoalsAgainstHome" runat="server">
                            <td>
                                <asp:Label ID="lblAverageGoalsAgainstHome" runat="server" Text="Average goals against (home): " /><br />
                            </td>
                            <td>
                                <asp:Label ID="lblAverageGoalsAgainstHomeValue" runat="server" /><br />
                            </td>
                        </tr>
                        <tr id="trAverageGoalsForAway" runat="server">
                            <td>
                                <asp:Label ID="lblAverageGoalsForAway" runat="server" Text="Average goals for (away): " /><br />
                            </td>
                            <td>
                                <asp:Label ID="lblAverageGoalsForAwayValue" runat="server" /><br />
                            </td>
                        </tr>
                        <tr id="trAverageGoalsAgainstAway" runat="server">
                            <td>
                                <asp:Label ID="lblAverageGoalsAgainstAway" runat="server" Text="Average goals against (away): " /><br />
                            </td>
                            <td>
                                <asp:Label ID="lblAverageGoalsAgainstAwayValue" runat="server" /><br />
                            </td>
                        </tr>
                        <tr id="trAverageGoalsForTotal" runat="server">
                            <td>
                                <asp:Label ID="lblAverageGoalsForTotal" runat="server" Text="Average goals for (total): " /><br />
                            </td>
                            <td>
                                <asp:Label ID="lblAverageGoalsForTotalValue" runat="server" /><br />
                            </td>
                        </tr>
                        <tr id="trAverageGoalsAgainstTotal" runat="server">
                            <td>
                                <asp:Label ID="lblAverageGoalsAgainstTotal" runat="server" Text="Average goals against (total): " />
                            </td>
                            <td>
                                <asp:Label ID="lblAverageGoalsAgainstTotalValue" runat="server" />
                            </td>
                        </tr>
                    </table>
                </div>
            </td>
            <td>
                <uc1:MatchResultList ID="MatchResultList1" runat="server" />
            </td>
        </tr>
    </table>
</asp:Content>