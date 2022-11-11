<%@ Control Language="vb" AutoEventWireup="false" CodeBehind="MatchPredictionList.ascx.vb" Inherits="Predictathon.UserControls.Match.MatchPredictionList" %>
<div class="GridTitle">
    <asp:Label ID="lblGridTitle" runat="server" Text="Predictions" />
</div>
<nug:GridView ID="gvMatchPredictionList" runat="server" AutoGenerateColumns="false" GridLines="None" AllowPaging="false" CssClass="GridView NoBorder" DataKeyNames="UserID"
    RowStyle-CssClass="GridViewRow" EnableRowClick="true" AlternatingRowStyle-CssClass="GridViewRowAlt" RowHoverCssClass="GridViewRowHover" EmptyDataText="No predictions found">
    <Columns>
        <asp:BoundField DataField="Username" HeaderText="User" />
        <asp:TemplateField HeaderText="Prediction" ItemStyle-HorizontalAlign="Center">
            <ItemTemplate>
                <asp:Label ID="lblTeams" runat="server" Text='<%# DirectCast(Container.DataItem, PredictathonModel.MatchPredictionListGet_Result).HomeTeamGoals & "-" &  DirectCast(Container.DataItem, PredictathonModel.MatchPredictionListGet_Result).AwayTeamGoals %>' />
            </ItemTemplate>
        </asp:TemplateField>
        <asp:BoundField DataField="Score" HeaderText="Points" ItemStyle-HorizontalAlign="Center" />
    </Columns>
</nug:GridView>