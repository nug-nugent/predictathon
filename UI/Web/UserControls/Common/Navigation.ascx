<%@ Control Language="vb" AutoEventWireup="false" CodeBehind="Navigation.ascx.vb" Inherits="Predictathon.UserControls.Navigation" %>

<nav id="nav" role="navigation">
    <a href="#nav" title="Show navigation">Show navigation</a>
    <a href="#" title="Hide navigation">Hide navigation</a>
    <ul>
        <li><a runat="server" href="~/Pages/Common/MainMenu.aspx">Home</a></li>
        <li><a runat="server" href="~/Pages/Match/UserMatchPredictionList.aspx">Predictions</a></li>
        <li><a runat="server" href="~/Pages/League/LeagueTable.aspx">League Table</a></li>
        <li><a runat="server" href="~/Pages/Match/MatchResultList.aspx">Results</a></li>
        <li id="liMessageboard" runat="server"><a runat="server" href="~/Pages/Message/Messageboard.aspx">Messageboard</a></li>
        <li>
            <a>Statistics</a>
            <ul>
                <li><a runat="server" href="~/Pages/Statistics/CurrentCompetition.aspx">Current Competition</a></li>
                <li><a runat="server" href="~/Pages/Statistics/AllTime.aspx">All-Time</a></li>
            </ul>
        </li>
        <li><a runat="server" href="~/Pages/HallOfFame/HallOfFame.aspx">Hall of Fame</a></li>
        <li><a runat="server" href="~/Pages/Rules/Rules.aspx">Rules</a></li>
        <li><a runat="server" id="hypEditProfile">Edit Profile</a></li>
        <li id="liAdministration" runat="server">
            <a>Administration</a>
            <ul>
                <li id="liProcessMatches" runat="server"><a runat="server" href="~/Pages/Match/MatchProcessingList.aspx">Process Matches</a></li>
                <li id="liMatchListAdmin" runat="server"><a runat="server" href="~/Pages/Match/MatchListAdmin.aspx">Add/Edit Matches</a></li>
                <li id="liPaymentCreditList" runat="server"><a runat="server" href="~/Pages/Payment/PaymentCreditList.aspx">Add/Edit Credits</a></li>
                <li id="liCompetitionList" runat="server"><a runat="server" href="~/Pages/Competition/CompetitionList.aspx">Competitions</a></li>
            </ul>
        </li>
    </ul>
</nav>
<div class="clearfix"></div>