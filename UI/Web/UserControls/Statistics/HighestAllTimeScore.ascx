<%@ Control Language="vb" AutoEventWireup="false" CodeBehind="HighestAllTimeScore.ascx.vb" Inherits="Predictathon.UserControls.Statistics.HighestAllTimeScore" %>
<div class="GridTitle">
    <asp:Label ID="lblGridTitle" runat="server" Text="Most Points" />
</div>
<nug:GridView ID="gvAllTimeScores" runat="server" AutoGenerateColumns="false" GridLines="None" AllowPaging="false" CssClass="GridView NoBorder" DataKeyNames="UserID"
    RowStyle-CssClass="GridViewRow" EnableRowClick="true" AlternatingRowStyle-CssClass="GridViewRowAlt" RowHoverCssClass="GridViewRowHover" EmptyDataText="No records found"
    EmptyDataRowStyle-CssClass="GridViewEmptyDataRow">
    <Columns>
        <asp:BoundField HeaderText="User" DataField="Username" ItemStyle-HorizontalAlign="Center" HeaderStyle-HorizontalAlign="Center" />
        <asp:BoundField HeaderText="Total Points" DataField="TotalScore" ItemStyle-HorizontalAlign="Center" HeaderStyle-HorizontalAlign="Center" />
    </Columns>
</nug:GridView>