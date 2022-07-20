<%@ Page Language="vb" AutoEventWireup="false" MasterPageFile="~/Pages/Master/Main.Master" CodeBehind="MatchDetail.aspx.vb" Inherits="Predictathon.Pages.Match.MatchDetail" %>
<%@ MasterType TypeName="Predictathon.Master.Main" %>
<%@ Register Src="~/UserControls/Match/MatchPredictionList.ascx" TagName="MatchPredictionList" TagPrefix="uc1" %>

<asp:Content runat="server" ContentPlaceHolderID="MainContent">
    <h1 style="line-height: 30px; vertical-align: middle">
        <asp:Image ID="imgHomeTeam" runat="server" Height="25px" style="vertical-align: middle" />
        <asp:Literal ID="litTeamNames" runat="server" />
        <asp:Image ID="imgAwayTeam" runat="server" Height="25px" style="vertical-align: middle" />
        <asp:Label ID="lbl90Minutes" runat="server" CssClass="SubNote" Text="*after 90 minutes" />
    </h1>
    <h2 id="MatchDescriptionHeader" runat="server">
        <span id="spnMatchDescription" runat="server"><asp:Label ID="lblMatchDescription" runat="server" /></span>
    </h2>
    <h2>
        <asp:Label ID="lblMatchDate" runat="server" />
        <asp:LinkButton ID="lnkEdit" runat="server" Text="Edit" />
    </h2>
    <asp:UpdatePanel ID="pnlSave" runat="server">
        <ContentTemplate>
            <asp:Label ID="lblYourPrediction" runat="server" Text="Your prediction: " />
            <asp:TextBox ID="txtHomeTeamGoals" runat="server" CssClass="Score" MaxLength="1" Width="2em" />
            <asp:TextBox ID="txtAwayTeamGoals" runat="server" CssClass="Score" MaxLength="1" Width="2em" />
            <asp:ImageButton ID="btnSavePrediction" runat="server" ImageUrl="~/Images/Common/Save.gif" style="vertical-align: middle" />
            <asp:Label ID="lblPoints" runat="server" CssClass="Score" />
            <asp:Label ID="lblSaveProgress" runat="server" />
        </ContentTemplate>
    </asp:UpdatePanel>
    <div id="divKnockout" runat="server" visible="false"><asp:Label ID="lblKnockout" runat="server" Text="*Extra time excluded" CssClass="SubNote" /></div>
    <uc1:MatchPredictionList ID="MatchPredictionList1" runat="server" /><br />
</asp:Content>