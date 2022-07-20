<%@ Control Language="vb" AutoEventWireup="false" CodeBehind="HighestPercentageCorrect.ascx.vb" Inherits="Predictathon.UserControls.Statistics.HighestPercentageCorrect" %>
<div class="GridTitle">
    <asp:Label ID="lblGridTitle" runat="server" Text="Prediction - % Correct (1, 2, or 3 points)" />
</div>
<nug:GridView ID="gvPercentageCorrect" runat="server" AutoGenerateColumns="false" GridLines="None" AllowPaging="false" CssClass="GridView NoBorder" DataKeyNames="UserID"
    RowStyle-CssClass="GridViewRow" EnableRowClick="true" AlternatingRowStyle-CssClass="GridViewRowAlt" RowHoverCssClass="GridViewRowHover" EmptyDataText="No records found"
    EmptyDataRowStyle-CssClass="GridViewEmptyDataRow">
    <Columns>
        <asp:BoundField HeaderText="User" DataField="Username" ItemStyle-HorizontalAlign="Center" HeaderStyle-HorizontalAlign="Center" />
        <asp:BoundField HeaderText="Percentage Correct" DataField="CorrectPredictionPercentage" ItemStyle-HorizontalAlign="Center" HeaderStyle-HorizontalAlign="Center" DataFormatString="{0:0.00}%" />
    </Columns>
</nug:GridView>