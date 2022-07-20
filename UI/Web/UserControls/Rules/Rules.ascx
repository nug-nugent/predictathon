<%@ Control Language="vb" AutoEventWireup="false" CodeBehind="Rules.ascx.vb" Inherits="Predictathon.UserControls.Rules.Rules" %>
<asp:HiddenField ID="hdnRandomTeam1" runat="server" />
<asp:HiddenField ID="hdnRandomTeam2" runat="server" />

<div id="divRules" runat="server" class="InformationBlock NoHover">
    <div class="TitleBar">Rules of the game</div>
    <div style="margin: 6px">
        <p class="HeaderText" style="margin-top: 2px; margin-bottom: 2px">
            Scoring points
        </p>
        <hr />
        <p><asp:Literal ID="litCompetitionDescription" runat="server" /> They then score points based on the accuracy of those predictions.</p>
        <p>For example, if you predicted that <%= RandomTeam1%> would beat <%=RandomTeam2 %> 4-2:</p>
        <ul>
            <li>If the result was 4-2 you would receive the maximum 3 points for a perfect prediction.</li>
            <li>If the result was 4-0, 4-1, or 4-3 you would score 2 points because you said that <%=RandomTeam1 %> would win and score 4 goals.</li>
            <li>If the result was e.g. 2-1, 3-0, 5-1, or 1-0 you would score 1 point, as you correctly predicted the winning team, but not the score.</li>
            <li>If the game ended in a draw, <%=RandomTeam2 %> won, or you failed to make a prediction, you would score 0 points.</li>
        </ul>
        <p>In another scenario, if you predicted that <%= RandomTeam1%> would draw 1-1 with <%=RandomTeam2 %>:</p> 
        <ul>
            <li>If the result was 1-1 you would receive the maximum 3 points for a perfect prediction.</li>
            <li>If the result was 0-0, 2-2, 3-3, 4-4, or a higher-scoring draw, you would score 1 point.</li>
            <li>If either team won or you failed to make a prediction, you would score 0 points.</li>
        </ul>
        <hr />      
        <p class="HeaderText" style="margin-top: 2px; margin-bottom: 2px">
            Goal difference
        </p>
        <hr />
        Throughout the competition, your goal difference will be adversely affected by all but perfect predictions.<br />
        <ul>
            <li>If you predict a 2-1 home win and the result is a 2-0 home win, 1 goal will be subtracted from your goal difference.</li>
            <li>If you predict a 0-2 away win and the result is a 2-0 home win, 4 goals will be subtracted from your goal difference.</li>
        </ul>
        <hr />      
        <p class="HeaderText" style="margin-top: 2px; margin-bottom: 2px">
            Deciding the victor
        </p>
        <hr />
        At the end of the competition, players on equal points will be separated by goal difference.<br />
        In the event of a tie, they'll then be separated firstly by the number of 3-pointers they’ve scored, then 2-pointers, and finally 1-pointers.<br />
    </div>
</div>