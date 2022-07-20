<%@ Page Language="vb" AutoEventWireup="false" MasterPageFile="~/Pages/Master/Main.Master" CodeBehind="CurrentCompetition.aspx.vb" Inherits="Predictathon.Pages.Statistics.CurrentCompetition" %>

<%@ Register Src="~/UserControls/Statistics/PredictableTeams.ascx" TagPrefix="uc1" TagName="PredictableTeams" %>
<%@ Register Src="~/UserControls/Statistics/PredictableMatches.ascx" TagPrefix="uc1" TagName="PredictableMatches" %>
<%@ Register Src="~/UserControls/Statistics/BestPredictions.ascx" TagPrefix="uc1" TagName="BestPredictions" %>
<%@ MasterType TypeName="Predictathon.Master.Main" %>

<asp:Content runat="server" ContentPlaceHolderID="MainContent">
    <div class="LeftPane">
        <uc1:BestPredictions ID="BestPredictions1" runat="server" />
        <br />
        <uc1:PredictableMatches ID="MostPredictableMatches" runat="server" HeaderText="Most Predictable Matches" MaxResults="50" ShowMostPredictable="true" ResultsPerPage="5" />
        <br />
        <uc1:PredictableMatches ID="LeastPredictableMatches" runat="server" HeaderText="Least Predictable Matches" MaxResults="50" ShowMostPredictable="false" ResultsPerPage="5" />
    </div>
    <div class="RightPane">
        <uc1:PredictableTeams ID="PredictableTeams1" runat="server" />
    </div>
</asp:Content>