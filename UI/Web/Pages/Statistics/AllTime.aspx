<%@ Page Language="vb" AutoEventWireup="false" MasterPageFile="~/Pages/Master/Main.Master" CodeBehind="AllTime.aspx.vb" Inherits="Predictathon.Pages.Statistics.AllTime" %>

<%@ Register Src="~/UserControls/Statistics/CompetitionWinners.ascx" TagPrefix="uc1" TagName="CompetitionWinners" %>
<%@ Register Src="~/UserControls/Statistics/HighestAllTimeScore.ascx" TagPrefix="uc1" TagName="HighestAllTimeScore" %>
<%@ Register Src="~/UserControls/Statistics/HighestAverageScore.ascx" TagPrefix="uc1" TagName="HighestAverageScore" %>
<%@ Register Src="~/UserControls/Statistics/HighestPercentageCorrect.ascx" TagPrefix="uc1" TagName="HighestPercentageCorrect" %>
<%@ Register Src="~/UserControls/Statistics/MostPredictions.ascx" TagPrefix="uc1" TagName="MostPredictions" %>
<%@ MasterType TypeName="Predictathon.Master.Main" %>

<asp:Content runat="server" ContentPlaceHolderID="MainContent">
    <div class="LeftPane">
        <uc1:HighestAllTimeScore ID="HighestAllTimeScore1" runat="server" /><br />
        <uc1:HighestAverageScore ID="HighestAverageScore1" runat="server" /><br />
        <uc1:HighestPercentageCorrect ID="HighestPercentageCorrect1" runat="server" /><br />
    </div>
    <div class="RightPane">
        <uc1:CompetitionWinners ID="CompetitionWinners1" runat="server" /><br />
        <uc1:MostPredictions ID="MostPredictions1" runat="server" /><br />
    </div>
</asp:Content>