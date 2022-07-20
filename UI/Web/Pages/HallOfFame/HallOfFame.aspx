<%@ Page Language="vb" AutoEventWireup="false" MasterPageFile="~/Pages/Master/Main.Master" CodeBehind="HallOfFame.aspx.vb" Inherits="Predictathon.Pages.HallOfFame" %>
<%@ MasterType TypeName="Predictathon.Master.Main" %>

<asp:Content runat="server" ContentPlaceHolderID="MainContent">
    <nug:GridView ID="gvHallOfFame" runat="server" AutoGenerateColumns="false" GridLines="None" AllowPaging="false" DataKeyNames="HallOfFameID"
        CssClass="GridView FullWidth" RowStyle-CssClass="GridViewRow" AlternatingRowStyle-CssClass="GridViewRowAlt" EnableRowClick="false">
        <Columns>
            <asp:TemplateField HeaderText="The Predictathon Hall of Fame" HeaderStyle-HorizontalAlign="Left" ItemStyle-HorizontalAlign="Center">
                <ItemTemplate>
                    <div style="width: 100%; text-align: center">
                        <div style="width: 100px; float: left; text-align: center;">
                            <asp:Image ID="imgCompetition" runat="server" style="max-height: 100px; max-width: 100px; margin-right: 10px;vertical-align: middle" ImageUrl='<%# HallOfFameImageURL(DirectCast(Container.DataItem, PredictathonModel.HallOfFame).ImageFileName) %>' />
                        </div>
                        <div style="width: 100%; height: 100%; text-align: center; vertical-align: middle">
                            <asp:Label ID="lblCompetition" runat="server" Text='<%# DirectCast(Container.DataItem, PredictathonModel.HallOfFame).CompetitionName %>'
                                style="font-weight: bold; font-size: 22px; color: #666" /><br />
                            <asp:HyperLink ID="hypWinner" runat="server">
                                <asp:Label ID="lblWinner" runat="server" Text='<%# "1st: " & DirectCast(Container.DataItem, PredictathonModel.HallOfFame).Winner %>'
                                    style="font-weight: bold; font-size: 18px; color: #999900" /><br />
                            </asp:HyperLink>
                            <asp:HyperLink ID="hypSecondPlace" runat="server">
                                <asp:Label ID="lblSecondPlace" runat="server" Text='<%# "2nd: " & DirectCast(Container.DataItem, PredictathonModel.HallOfFame).SecondPlace %>'
                                    style="font-weight: bold; font-size: 16px; color: #888888" /><br />
                            </asp:HyperLink>
                            <asp:HyperLink ID="hypThirdPlace" runat="server">
                                <asp:Label ID="lblThirdPlace" runat="server" Text='<%# "3rd: " & DirectCast(Container.DataItem, PredictathonModel.HallOfFame).ThirdPlace %>'
                                    style="font-weight: bold; font-size: 14px; color: #664400" /><br />
                            </asp:HyperLink>
                        </div>
                    </div>
                </ItemTemplate>
            </asp:TemplateField>
        </Columns>
    </nug:GridView>
</asp:Content>