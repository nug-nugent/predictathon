<%@ Control Language="vb" AutoEventWireup="false" CodeBehind="BestPredictions.ascx.vb" Inherits="Predictathon.UserControls.Statistics.BestPredictions" %>
<div class="GridTitle">
    <asp:Label ID="lblGridTitle" runat="server" Text="Best Predictions" />
</div>
<nug:GridView ID="gvPredictions" runat="server" AutoGenerateColumns="false" GridLines="None" AllowPaging="true" PageSize="10" CssClass="GridView NoBorder" DataKeyNames="MatchID"
    RowStyle-CssClass="GridViewRow" EnableRowClick="true" AlternatingRowStyle-CssClass="GridViewRowAlt" RowHoverCssClass="GridViewRowHover" EmptyDataText="No matches found"
    EmptyDataRowStyle-CssClass="GridViewEmptyDataRow">
    <Columns>
        <asp:BoundField HeaderText="User" DataField="Username" ItemStyle-HorizontalAlign="Center" HeaderStyle-HorizontalAlign="Center" />
        <asp:TemplateField HeaderText="Match" ItemStyle-HorizontalAlign="Center">
            <ItemTemplate>
                <asp:Label ID="lblTeams" runat="server" Text='<%# _
                    DirectCast(Container.DataItem, PredictathonModel.MatchPredictionAverageBiggestDifferencesGet_Result).HomeTeamShortName _
                        & " vs " &  _
                    DirectCast(Container.DataItem, PredictathonModel.MatchPredictionAverageBiggestDifferencesGet_Result).AwayTeamShortName %>' />
            </ItemTemplate>
        </asp:TemplateField>
        <asp:TemplateField HeaderText="Result" ItemStyle-HorizontalAlign="Center">
            <ItemTemplate>
                <asp:Label ID="lblScore" runat="server" Text='<%# DirectCast(Container.DataItem, PredictathonModel.MatchPredictionAverageBiggestDifferencesGet_Result).HomeTeamGoals & "-" &  DirectCast(Container.DataItem, PredictathonModel.MatchPredictionAverageBiggestDifferencesGet_Result).AwayTeamGoals %>' />
            </ItemTemplate>
        </asp:TemplateField>
        <asp:TemplateField HeaderText="Prediction" ItemStyle-HorizontalAlign="Center">
            <ItemTemplate>
                <asp:Label ID="lblPrediction" runat="server" Text='<%# DirectCast(Container.DataItem, PredictathonModel.MatchPredictionAverageBiggestDifferencesGet_Result).PredictionHomeTeamGoals & "-" &  DirectCast(Container.DataItem, PredictathonModel.MatchPredictionAverageBiggestDifferencesGet_Result).PredictionAwayTeamGoals %>' />
            </ItemTemplate>
        </asp:TemplateField>
        <asp:TemplateField HeaderText="Score" ItemStyle-HorizontalAlign="Center">
            <ItemTemplate>
                <asp:Label ID="lblPredictionScore" runat="server" Text='<%# DirectCast(Container.DataItem, PredictathonModel.MatchPredictionAverageBiggestDifferencesGet_Result).PredictionScore.ToString %>' CssClass='<%# Predictathon.PredictionManager.GetCSSClassForScore(DirectCast(Container.DataItem, PredictathonModel.MatchPredictionAverageBiggestDifferencesGet_Result).PredictionScore, True) %>' />                
            </ItemTemplate>
        </asp:TemplateField>
        <asp:BoundField DataField="AveragePredictionScore" HeaderText="Average score" DataFormatString="{0:N2}" ItemStyle-HorizontalAlign="Center" />
        <asp:BoundField DataField="ScoreDifference" HeaderText="Difference" DataFormatString="{0:N2}" ItemStyle-HorizontalAlign="Center" />
    </Columns>
</nug:GridView>