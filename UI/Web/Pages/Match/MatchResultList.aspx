<%@ Page Language="vb" AutoEventWireup="false" MasterPageFile="~/Pages/Master/Main.Master" CodeBehind="MatchResultList.aspx.vb" Inherits="Predictathon.Pages.Match.MatchResultList" %>
<%@ MasterType TypeName="Predictathon.Master.Main" %>

<asp:Content runat="server" ContentPlaceHolderID="MainContent">
    <nug:GridView ID="gvMatch" runat="server" AutoGenerateColumns="false" GridLines="None" AllowPaging="true" PageSize="20" CssClass="GridView FullWidth" DataKeyNames="MatchID"
        RowStyle-CssClass="GridViewRow" EnableRowClick="true" AlternatingRowStyle-CssClass="GridViewRowAlt" RowHoverCssClass="GridViewRowHover" EmptyDataText="No results found">
        <Columns>
            <asp:BoundField HeaderText="Date / time" DataField="MatchDateTime" DataFormatString="{0:dd/MM/yy HH:mm}" ItemStyle-HorizontalAlign="Center" />
            <asp:TemplateField HeaderText="Match" ItemStyle-HorizontalAlign="Center">
                <ItemTemplate>
                    <asp:Label ID="lblTeams" runat="server" Text='<%# _
                        DirectCast(Container.DataItem, PredictathonModel.MatchResultListGet_Result).HomeTeam _
                            & " vs " &  _
                        DirectCast(Container.DataItem, PredictathonModel.MatchResultListGet_Result).AwayTeam %>' />
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
                    <asp:Label ID="lblYourPredictionScore" runat="server" CssClass="Score" Text='<%# DirectCast(Container.DataItem, PredictathonModel.MatchResultListGet_Result).YourPredictionScore %>' />               
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
</asp:Content>