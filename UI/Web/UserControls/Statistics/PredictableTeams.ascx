<%@ Control Language="vb" AutoEventWireup="false" CodeBehind="PredictableTeams.ascx.vb" Inherits="Predictathon.UserControls.Statistics.PredictableTeams" %>
<div class="GridTitle">
    <asp:Label ID="lblGridTitle" runat="server" Text="Average Prediction Score By Team" />
</div>
<nug:GridView ID="gvPredictableTeams" runat="server" AutoGenerateColumns="false" GridLines="None" AllowPaging="false" CssClass="GridView NoBorder" DataKeyNames="TeamID"
    RowStyle-CssClass="GridViewRow" EnableRowClick="true" AlternatingRowStyle-CssClass="GridViewRowAlt" RowHoverCssClass="GridViewRowHover" EmptyDataText="No teams found"
    EmptyDataRowStyle-CssClass="GridViewEmptyDataRow">
    <Columns>
        <asp:TemplateField HeaderText="" ItemStyle-HorizontalAlign="Center" HeaderStyle-Width="22%">
            <ItemTemplate>
                <asp:Image ID="imgTeam" runat="server" Height="16px" style="margin-right: 10px;vertical-align: middle" ImageUrl='<%# Predictathon.TeamManager.TeamCrestImageURL(DirectCast(Container.DataItem, PredictathonModel.AverageScoreByTeamListGet_Result).TeamImage) %>' />
                <asp:Label ID="lblTeam" runat="server" Text='<%# DirectCast(Container.DataItem, PredictathonModel.AverageScoreByTeamListGet_Result).ShortName %>' />
            </ItemTemplate>
        </asp:TemplateField>
        <asp:BoundField HeaderText="Average score" DataField="AverageScore" DataFormatString="{0:N2}" ItemStyle-HorizontalAlign="Center" HeaderStyle-Width="12%" />
    </Columns>
</nug:GridView>