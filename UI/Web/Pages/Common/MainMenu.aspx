<%@ Page Title="" Language="vb" AutoEventWireup="false" MasterPageFile="~/Pages/Master/Main.Master" CodeBehind="MainMenu.aspx.vb" Inherits="Predictathon.Pages.MainMenu" %>

<%@ MasterType TypeName="Predictathon.Master.Main" %>
<%@ Register Src="~/UserControls/User/UserStatistics.ascx" TagName="UserStatistics" TagPrefix="uc1" %>
<%@ Register Src="~/UserControls/Match/UserMatchPredictionList.ascx" TagName="UserMatchPredictionList" TagPrefix="uc1" %>
<%@ Register Src="~/UserControls/Competition/UserCompetitionRegistrationList.ascx" TagName="UserCompetitionRegistrationList" TagPrefix="uc1" %>
<%@ Register Src="~/UserControls/Twitter/TwitterFeed.ascx" TagName="TwitterFeed" TagPrefix="uc1" %>

<asp:Content ID="ContentMain" ContentPlaceHolderID="MainContent" runat="server">
    <div class="LeftPane">
        <uc1:UserStatistics ID="UserStatistics1" runat="server" Selectable="true" ShowNextPredictionDueIn="true" ShowUsername="true" />
        <br />
        <uc1:TwitterFeed ID="TwitterFeed1" runat="server" TweetsToDisplay="3" TwitterProfileName="Predictathon" />
        <br />
        <uc1:UserCompetitionRegistrationList ID="UserCompetitionRegistrationList1" runat="server" />
    </div>
    <div class="RightPane">
        <uc1:UserMatchPredictionList ID="UserMatchPredictionList1" runat="server" GridTitleText="Current Matches" />
        <br />
        <uc1:UserMatchPredictionList ID="UserMatchPredictionList2" runat="server" GridTitleText="Future Matches" />
    </div>
</asp:Content>