<%@ Page Language="vb" AutoEventWireup="false" MasterPageFile="~/Pages/Master/Main.Master" CodeBehind="UserDetail.aspx.vb" Inherits="Predictathon.Pages.User.UserDetail" %>

<%@ MasterType TypeName="Predictathon.Master.Main" %>
<%@ Register Src="~/UserControls/User/UserProfile.ascx" TagName="UserProfile" TagPrefix="uc1" %>
<%@ Register Src="~/UserControls/User/UserStatistics.ascx" TagName="UserStatistics" TagPrefix="uc1" %>
<%@ Register Src="~/UserControls/User/UserPredictionList.ascx" TagName="UserPredictionList" TagPrefix="uc1" %>
<%@ Register Src="~/UserControls/User/UserLeagueTable.ascx" TagName="UserLeagueTable" TagPrefix="uc1" %>
<%@ Register Src="~/UserControls/User/UserCompetitionLeagueHistory.ascx" TagName="UserCompetitionLeagueHistory" TagPrefix="uc1" %>

<asp:Content runat="server" ContentPlaceHolderID="MainContent">
    <div class="LeftPane">
        <uc1:UserProfile ID="UserProfile1" runat="server" />
        <uc1:UserStatistics ID="UserStatistics1" runat="server" Selectable="false" NeverShowEditOption="true" ShowUsername="false" />
        <br />
        <uc1:UserCompetitionLeagueHistory ID="UserCompetitionLeagueHistory1" runat="server" />
    </div>
    <div class="RightPane">
        <uc1:UserPredictionList ID="UserPredictionList1" runat="server" />
        <br />
        <uc1:UserLeagueTable ID="UserLeagueTable1" runat="server" ShowLink="true" />
    </div>
</asp:Content>
