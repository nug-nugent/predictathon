<%@ Control Language="vb" AutoEventWireup="false" CodeBehind="UserStatistics.ascx.vb" Inherits="Predictathon.UserControls.User.UserStatistics" %>
<div id="divUserStatistics" runat="server" class="InformationBlock">
    <div class="TitleBar" style="margin-bottom: 5px;">
        <table width="100%">
            <tr>
                <td align="left">
                    <asp:Label ID="lblUsername" runat="server" />
                </td>
                <td align="right">
                    <asp:LinkButton ID="btnEditUser" runat="server" CssClass="HighlightedLink" Text="Edit User" />
                </td>
            </tr>
        </table>
    </div>
    <table width="100%" style="margin: 5px">
        <tr>
            <td>
                <asp:Label ID="lblLeaguePosition" runat="server" Text="League position:" />  
            </td>
            <td>
                <asp:Label ID="lblLeaguePositionValue" runat="server" />
            </td>
        </tr>
        <tr>
            <td>
                <asp:Label ID="lblPoints" runat="server" Text="Points:" />  
            </td>
            <td>
                <asp:Label ID="lblPointsValue" runat="server" />
            </td>
        </tr>
        <tr id="trLastWeek" runat="server">
            <td>
                <asp:HyperLink ID="hypPointsLastWeek" runat="server" NavigateUrl="~/Pages/League/LeagueTable.aspx?Date=LastWeek">
                    <asp:Label ID="lblPointsLastWeek" runat="server" Text="Points last match week:" />
                </asp:HyperLink>
            </td>
            <td>
                <asp:Label ID="lblPointsLastWeekValue" runat="server" />
            </td>
        </tr>
        <tr id="trThisWeek" runat="server">
            <td>
                <asp:HyperLink ID="hypPointsThisWeek" runat="server" NavigateUrl="~/Pages/League/LeagueTable.aspx?Date=ThisWeek">
                    <asp:Label ID="lblPointsThisWeek" runat="server" Text="Points this match week:" />
                </asp:HyperLink>
            </td>
            <td>
                <asp:Label ID="lblPointsThisWeekValue" runat="server" />
            </td>
        </tr>
        <tr id="trNextPredictionDueIn" runat="server">
            <td>
                <asp:Hyperlink ID="hypUserMatchPredictionList1" runat="server" NavigateUrl="~/Pages/Match/Predictions.aspx">
                    <asp:Label ID="lblNextPredictionDueIn" runat="server" Text="Next prediction due in:" />
                </asp:Hyperlink>
            </td>
            <td>
                <asp:Hyperlink ID="hypUserMatchPredictionList2" runat="server" NavigateUrl="~/Pages/Match/Predictions.aspx" style="text-decoration: none">
                    <asp:Label ID="lblNextPredictionDueInValue" runat="server" />
                </asp:Hyperlink>
            </td>
        </tr>
    </table>
</div>