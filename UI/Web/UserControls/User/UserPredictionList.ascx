<%@ Control Language="vb" AutoEventWireup="false" CodeBehind="UserPredictionList.ascx.vb" Inherits="Predictathon.UserControls.User.UserPredictionList" %>
<div class="GridTitle">
    <asp:Label ID="lblGridTitle" runat="server" Text="Predictions" />
</div>
<nug:GridView ID="gvUserPredictionList" runat="server" AutoGenerateColumns="false" GridLines="None" AllowPaging="true" PageSize="10" CssClass="GridView NoBorder" DataKeyNames="MatchID"
    RowStyle-CssClass="GridViewRow" EnableRowClick="true" AlternatingRowStyle-CssClass="GridViewRowAlt" RowHoverCssClass="GridViewRowHover" EmptyDataText="No predictions found">
    <Columns>
        <asp:TemplateField HeaderText="" ItemStyle-HorizontalAlign="Center" HeaderStyle-Width="35%">
            <ItemTemplate>
                <asp:Image ID="imgHomeTeam" runat="server" Height="16px" style="margin-right: 10px;vertical-align: middle" ImageUrl='<%# Predictathon.TeamManager.TeamCrestImageURL(DirectCast(Container.DataItem, PredictathonModel.UserMatchPredictionListGet_Result).HomeTeamImage) %>' />
                <asp:Label ID="lblHomeTeam" runat="server" Text='<%# DirectCast(Container.DataItem, PredictathonModel.UserMatchPredictionListGet_Result).HomeTeamShortName %>' />
            </ItemTemplate>
        </asp:TemplateField>
        <asp:TemplateField HeaderText="" ItemStyle-HorizontalAlign="Center" HeaderStyle-Width="15%">
            <ItemTemplate>
                <asp:Label ID="lblHomeTeamGoals" runat="server" CssClass="Score" Text='<%# DirectCast(Container.DataItem, PredictathonModel.UserMatchPredictionListGet_Result).HomeTeamGoals %>' />
                -
                <asp:Label ID="lblAwayTeamGoals" runat="server" CssClass="Score" Text='<%# DirectCast(Container.DataItem, PredictathonModel.UserMatchPredictionListGet_Result).AwayTeamGoals %>' />
            </ItemTemplate>
        </asp:TemplateField>
        <asp:TemplateField HeaderText="" ItemStyle-HorizontalAlign="Center" HeaderStyle-Width="35%">
            <ItemTemplate>
                <asp:Label ID="lblAwayTeam" runat="server" Text='<%# DirectCast(Container.DataItem, PredictathonModel.UserMatchPredictionListGet_Result).AwayTeamShortName %>' />
                <asp:Image ID="imgAwayTeam" runat="server" Height="16px" style="margin-left: 10px; vertical-align: middle" ImageUrl='<%# Predictathon.TeamManager.TeamCrestImageURL(DirectCast(Container.DataItem, PredictathonModel.UserMatchPredictionListGet_Result).AwayTeamImage) %>' />
            </ItemTemplate>
        </asp:TemplateField>
        <asp:TemplateField HeaderText="Points" ItemStyle-HorizontalAlign="Center" HeaderStyle-Width="15%">
            <ItemTemplate>
                <asp:Label ID="lblScore" runat="server" Text='<%# If(DirectCast(Container.DataItem, PredictathonModel.UserMatchPredictionListGet_Result).Score.ToString, "") %>' CssClass="<%# Predictathon.PredictionManager.GetCSSClassForScore(If(DirectCast(Container.DataItem, PredictathonModel.UserMatchPredictionListGet_Result).Score, 0), True) %>" />                
            </ItemTemplate>
        </asp:TemplateField>
    </Columns>
</nug:GridView>