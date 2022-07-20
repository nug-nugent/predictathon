<%@ Page Language="vb" AutoEventWireup="false" MasterPageFile="~/Pages/Master/Main.Master" CodeBehind="UserLeagueTable.aspx.vb" Inherits="Predictathon.Pages.User.UserLeagueTable" %>
<%@ MasterType TypeName="Predictathon.Master.Main" %>
<%@ Register Src="~/UserControls/User/UserLeagueTable.ascx" TagName="UserLeagueTable" TagPrefix="uc1" %>
<%@ Register Src="~/UserControls/Competition/RealLeagueTable.ascx" TagName="RealLeagueTable" TagPrefix="uc1" %>

<asp:Content runat="server" ContentPlaceHolderID="MainContent">
    <table width="100%">
        <tr valign="top">
            <td style="width: 50%">
                <uc1:UserLeagueTable ID="UserLeagueTable1" runat="server" ShowLink="false" />
            </td>
            <td style="width: 50%">
                <uc1:RealLeagueTable ID="RealLeagueTable1" runat="server" />
            </td>
        </tr>
    </table>
</asp:Content>
