<%@ Control Language="vb" AutoEventWireup="false" CodeBehind="MostPredictions.ascx.vb" Inherits="Predictathon.UserControls.Statistics.MostPredictions" %>
<div class="GridTitle">
    <asp:Label ID="lblGridTitle" runat="server" Text="Prolific Predictors - Most Predictions In Total" />
</div>
<nug:GridView ID="gvMostPredictions" runat="server" AutoGenerateColumns="false" GridLines="None" AllowPaging="false" CssClass="GridView NoBorder" DataKeyNames="UserID"
    RowStyle-CssClass="GridViewRow" EnableRowClick="true" AlternatingRowStyle-CssClass="GridViewRowAlt" RowHoverCssClass="GridViewRowHover" EmptyDataText="No records found"
    EmptyDataRowStyle-CssClass="GridViewEmptyDataRow">
    <Columns>
        <asp:BoundField HeaderText="User" DataField="Username" ItemStyle-HorizontalAlign="Center" HeaderStyle-HorizontalAlign="Center" />
        <asp:BoundField HeaderText="Total Points" DataField="TotalPredictions" ItemStyle-HorizontalAlign="Center" HeaderStyle-HorizontalAlign="Center" />
    </Columns>
</nug:GridView>