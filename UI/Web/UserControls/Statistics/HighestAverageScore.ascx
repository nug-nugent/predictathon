<%@ Control Language="vb" AutoEventWireup="false" CodeBehind="HighestAverageScore.ascx.vb" Inherits="Predictathon.UserControls.Statistics.HighestAverageScore" %>
<div class="GridTitle">
    <asp:Label ID="lblGridTitle" runat="server" Text="Best Average Per Prediction" />
</div>
<nug:GridView ID="gvAverageScores" runat="server" AutoGenerateColumns="false" GridLines="None" AllowPaging="false" CssClass="GridView NoBorder" DataKeyNames="UserID"
    RowStyle-CssClass="GridViewRow" EnableRowClick="true" AlternatingRowStyle-CssClass="GridViewRowAlt" RowHoverCssClass="GridViewRowHover" EmptyDataText="No records found"
    EmptyDataRowStyle-CssClass="GridViewEmptyDataRow">
    <Columns>
        <asp:BoundField HeaderText="User" DataField="Username" ItemStyle-HorizontalAlign="Center" HeaderStyle-HorizontalAlign="Center" />
        <asp:BoundField HeaderText="Average Points" DataField="AverageScore" ItemStyle-HorizontalAlign="Center" HeaderStyle-HorizontalAlign="Center" DataFormatString="{0:0.000}" />
    </Columns>
</nug:GridView>