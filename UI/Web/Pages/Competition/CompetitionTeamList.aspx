<%@ Page Language="vb" AutoEventWireup="false" MasterPageFile="~/Pages/Master/Main.Master" CodeBehind="CompetitionTeamList.aspx.vb" Inherits="Predictathon.Pages.Competition.CompetitionTeamList" %>
<%@ MasterType TypeName="Predictathon.Master.Main" %>

<asp:Content runat="server" ContentPlaceHolderID="MainContent">
    <table id="tblAddNew" runat="server">
        <tr>
            <td>
                <asp:Label ID="lblAdd" runat="server" CssClass="GridTitle" />
            </td>
        </tr>
        <tr>
            <td>
                <asp:Label ID="lblTeam" runat="server" Text="Team: " /><asp:DropDownList ID="ddlTeam" runat="server" DataTextField="TeamName" DataValueField="TeamID" />
                <asp:ImageButton ID="btnAdd" runat="server" ImageUrl="~/Images/Common/Add.gif" ImageAlign="Top" AlternateText="Add" />
            </td>
        </tr>
        <tr id="trError" runat="server">
            <td>
                <asp:Label ID="lblError" runat="server" CssClass="Error" />
            </td>
        </tr>
    </table>

    <nug:GridView ID="gvTeamCompetition" runat="server" AutoGenerateColumns="false" GridLines="None" AllowPaging="true" PageSize="20" DataKeyNames="TeamCompetitionID"
        CssClass="GridView" RowStyle-CssClass="GridViewRow" AlternatingRowStyle-CssClass="GridViewRowAlt" EnableRowClick="false">
        <Columns>
            <asp:BoundField HeaderText="Team" DataField="TeamName" />
            <asp:TemplateField HeaderText="Delete" ItemStyle-HorizontalAlign="Center">
                <ItemTemplate>
                    <asp:ImageButton ID="btnDelete" runat="server" CommandName="DeleteRecord" ToolTip="Delete" CommandArgument="<%# DirectCast(Container, GridViewRow).RowIndex %>" ImageUrl="~/Images/Common/Delete.gif"
                        OnClientClick="return confirm('Are you sure you wish to remove this team from the competition?');" />
                </ItemTemplate>
            </asp:TemplateField>
        </Columns>
    </nug:GridView>
</asp:Content>