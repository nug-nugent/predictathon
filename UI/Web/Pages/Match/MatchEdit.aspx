<%@ Page Language="vb" AutoEventWireup="false" MasterPageFile="~/Pages/Master/Main.Master" CodeBehind="MatchEdit.aspx.vb" Inherits="Predictathon.Pages.Match.MatchEdit" %>
<%@ MasterType TypeName="Predictathon.Master.Main" %>
<%@ Register Src="~/UserControls/Match/MatchPredictionList.ascx" TagName="MatchPredictionList" TagPrefix="uc1" %>

<asp:Content runat="server" ContentPlaceHolderID="MainContent">
    <h1 style="line-height: 30px; vertical-align: middle">
        <asp:Image ID="imgHomeTeam" runat="server" Height="25px" style="vertical-align: middle" />
        <asp:Literal ID="litTeamNames" runat="server" />
        <asp:Image ID="imgAwayTeam" runat="server" Height="25px" style="vertical-align: middle" />
    </h1>
    <h2 id="MatchDescriptionHeader" runat="server">
        <span id="spnMatchDescription" runat="server"><asp:Label ID="lblMatchDescription" runat="server" /></span>
    </h2>
    <h2>
        <asp:Label ID="lblMatchDate" runat="server" Text="Match date/time:" /><br />
        <nug:DatePicker ID="dteMatchDate" runat="server" ValidateDateAsMandatory="true" />&nbsp;at&nbsp;
        <nug:TimePicker ID="tmeMatchTime" runat="server" ValidateTimeAsMandatory="true" />
    </h2>
    <asp:ImageButton ID="btnSave" runat="server" ImageUrl="~/Images/Common/Save.gif" style="vertical-align: middle" />
</asp:Content>