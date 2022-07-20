<%@ Control Language="vb" AutoEventWireup="false" CodeBehind="RealLeagueTable.ascx.vb" Inherits="Predictathon.UserControls.Competition.RealLeagueTable" %>
<div class="GridTitle">
    <asp:Label ID="lblGridTitle" runat="server" Text="The real league table" />
</div>
<nug:GridView ID="gvCompetitionRealLeagueTable" runat="server" AutoGenerateColumns="false" GridLines="None" AllowPaging="false" CssClass="GridView NoBorder" DataKeyNames="TeamID"
    RowStyle-CssClass="GridViewRow" EnableRowClick="true" AlternatingRowStyle-CssClass="GridViewRowAlt" RowHoverCssClass="GridViewRowHover" EmptyDataText="No league data found">
    <Columns>
        <asp:BoundField HeaderText="Pos" DataField="Position" HeaderStyle-HorizontalAlign="Center" ItemStyle-HorizontalAlign="Center" />
        <asp:BoundField HeaderText="Team" DataField="ShortName" HeaderStyle-HorizontalAlign="Left" ItemStyle-HorizontalAlign="Left" />
        <asp:BoundField HeaderText="P" DataField="Played" HeaderStyle-HorizontalAlign="Center" ItemStyle-HorizontalAlign="Center" />
        <asp:BoundField HeaderText="W" DataField="Won" HeaderStyle-HorizontalAlign="Center" ItemStyle-HorizontalAlign="Center" />
        <asp:BoundField HeaderText="D" DataField="Drawn" HeaderStyle-HorizontalAlign="Center" ItemStyle-HorizontalAlign="Center" />
        <asp:BoundField HeaderText="L" DataField="Lost" HeaderStyle-HorizontalAlign="Center" ItemStyle-HorizontalAlign="Center" />
        <asp:BoundField HeaderText="GF" DataField="GoalsFor" HeaderStyle-HorizontalAlign="Center" ItemStyle-HorizontalAlign="Center" />
        <asp:BoundField HeaderText="GA" DataField="GoalsAgainst" HeaderStyle-HorizontalAlign="Center" ItemStyle-HorizontalAlign="Center" />
        <asp:BoundField HeaderText="GD" DataField="GoalDifference" HeaderStyle-HorizontalAlign="Center" ItemStyle-HorizontalAlign="Center" />
        <asp:BoundField HeaderText="Points" DataField="Points" HeaderStyle-HorizontalAlign="Center" ItemStyle-CssClass="Score" />
    </Columns>
</nug:GridView>