<%@ Control Language="vb" AutoEventWireup="false" CodeBehind="CompetitionWinners.ascx.vb" Inherits="Predictathon.UserControls.Statistics.CompetitionWinners" %>
<div class="GridTitle">
    <asp:Label ID="lblGridTitle" runat="server" Text="Competition Winners" />
</div>
<nug:GridView ID="gvWinners" runat="server" AutoGenerateColumns="false" GridLines="None" AllowPaging="false" CssClass="GridView NoBorder" DataKeyNames="UserID"
    RowStyle-CssClass="GridViewRow" EnableRowClick="true" AlternatingRowStyle-CssClass="GridViewRowAlt" RowHoverCssClass="GridViewRowHover" EmptyDataText="No records found"
    EmptyDataRowStyle-CssClass="GridViewEmptyDataRow">
    <Columns>
        <asp:BoundField HeaderText="User" DataField="Username" ItemStyle-HorizontalAlign="Center" HeaderStyle-HorizontalAlign="Center" />
        <asp:BoundField HeaderText="Wins" DataField="Wins" ItemStyle-HorizontalAlign="Center" HeaderStyle-HorizontalAlign="Center" />
        <asp:BoundField HeaderText="2nds" DataField="SecondPlaces" ItemStyle-HorizontalAlign="Center" HeaderStyle-HorizontalAlign="Center" />
        <asp:BoundField HeaderText="3rds" DataField="ThirdPlaces" ItemStyle-HorizontalAlign="Center" HeaderStyle-HorizontalAlign="Center" />
    </Columns>
</nug:GridView>