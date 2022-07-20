<%@ Control Language="vb" AutoEventWireup="false" CodeBehind="MatchResultList.ascx.vb" Inherits="Predictathon.UserControls.Match.MatchResultList" %>
<div class="GridTitle">
    <asp:Label ID="lblGridTitle" runat="server" Text="Results" />
</div>
<nug:GridView ID="gvMatchResultList" runat="server" AutoGenerateColumns="false" GridLines="None" AllowPaging="true" PageSize="10" CssClass="GridView NoBorder" DataKeyNames="MatchID"
    RowStyle-CssClass="GridViewRow" EnableRowClick="true" AlternatingRowStyle-CssClass="GridViewRowAlt" RowHoverCssClass="GridViewRowHover" EmptyDataText="No matches found"
    EmptyDataRowStyle-CssClass="GridViewEmptyDataRow">
    <Columns>
        <asp:BoundField HeaderText="Date / time" DataField="MatchDateTime" DataFormatString="{0:dd/MM/yy HH:mm}" ItemStyle-HorizontalAlign="Center" />
        <asp:TemplateField HeaderText="Match" ItemStyle-HorizontalAlign="Center">
            <ItemTemplate>
                <asp:Label ID="lblTeams" runat="server" Text='<%# _
                    DirectCast(Container.DataItem, PredictathonModel.MatchResultListGet_Result).HomeTeamShortName _
                        & " vs " &  _
                    DirectCast(Container.DataItem, PredictathonModel.MatchResultListGet_Result).AwayTeamShortName %>' />
            </ItemTemplate>
        </asp:TemplateField>
        <asp:TemplateField HeaderText="Result" ItemStyle-HorizontalAlign="Center">
            <ItemTemplate>
                <asp:Label ID="lblScore" runat="server" Text='<%# DirectCast(Container.DataItem, PredictathonModel.MatchResultListGet_Result).HomeTeamGoals & "-" &  DirectCast(Container.DataItem, PredictathonModel.MatchResultListGet_Result).AwayTeamGoals %>' />
            </ItemTemplate>
        </asp:TemplateField>
        <asp:TemplateField HeaderText="Your prediction" ItemStyle-HorizontalAlign="Center">
            <ItemTemplate>
                <asp:Label ID="lblPrediction" runat="server" Text='<%# DirectCast(Container.DataItem, PredictathonModel.MatchResultListGet_Result).PredictionHomeTeamGoals & "-" &  DirectCast(Container.DataItem, PredictathonModel.MatchResultListGet_Result).PredictionAwayTeamGoals %>' />
            </ItemTemplate>
        </asp:TemplateField>
        <asp:TemplateField HeaderText="Your score" ItemStyle-HorizontalAlign="Center">
            <ItemTemplate>
                <asp:Label ID="lblYourPredictionScore" runat="server" Text='<%# If(DirectCast(Container.DataItem, PredictathonModel.MatchResultListGet_Result).YourPredictionScore.ToString, "") %>' CssClass="<%# Predictathon.PredictionManager.GetCSSClassForScore(If(DirectCast(Container.DataItem, PredictathonModel.MatchResultListGet_Result).YourPredictionScore, 0), True) %>" />                
            </ItemTemplate>
        </asp:TemplateField>
        <asp:BoundField DataField="AveragePredictionScore" HeaderText="Average score" DataFormatString="{0:N2}" ItemStyle-HorizontalAlign="Center" />
        <asp:TemplateField HeaderText="< >" ItemStyle-Width="18px" HeaderStyle-HorizontalAlign="Center" ItemStyle-HorizontalAlign="Center">
            <ItemTemplate>
                <asp:Image ID="imgComparison" runat="server" />
            </ItemTemplate>
        </asp:TemplateField>
    </Columns>
</nug:GridView>