<%@ Page Language="vb" AutoEventWireup="false" MasterPageFile="~/Pages/Master/Main.Master" CodeBehind="MatchListAdmin.aspx.vb" Inherits="Predictathon.Pages.Match.MatchListAdmin" %>
<%@ MasterType TypeName="Predictathon.Master.Main" %>

<asp:Content runat="server" ContentPlaceHolderID="MainContent">
    <table>
        <tr>
            <td>
                <asp:Label ID="lblSearchCriteria" runat="server" CssClass="GridTitle" Text="Search criteria" />
            </td>
        </tr>
        <tr>
            <td>
                <asp:CheckBox ID="chkIncludePlayedMatches" runat="server" Text="Include played matches" AutoPostBack="true" />
            </td>
        </tr>
    </table>

    <table id="tblAddNew" runat="server">
        <tr>
            <td colspan="4">
                <asp:Label ID="lblAdd" runat="server" CssClass="GridTitle" Text="Add Match" />
            </td>
        </tr>
        <tr>
            <td>
                <asp:DropDownList ID="ddlHomeTeam" runat="server" DataTextField="TeamName" DataValueField="TeamID" />&nbsp;vs&nbsp;
                <asp:DropDownList ID="ddlAwayTeam" runat="server" DataTextField="TeamName" DataValueField="TeamID" />
            </td>
            <td>
                &nbsp;on&nbsp;<nug:DatePicker ID="dteMatchDate" runat="server" ValidateDateAsMandatory="true" />&nbsp;at&nbsp;
                <nug:TimePicker ID="tmeMatchTime" runat="server" ValidateTimeAsMandatory="true" />
            </td>
            <td>
                <asp:Label ID="lblNeutral" runat="server" Text="Neutral ground: " /><asp:CheckBox ID="chkNeutralGround" runat="server" />
            </td>
            <td rowspan="2">
                <asp:ImageButton ID="btnAdd" runat="server" ImageUrl="~/Images/Common/Add.gif" ImageAlign="Top" AlternateText="Add" style="margin-left: 15px" />
            </td>
        </tr>
        <tr>
            <td>
                or <asp:TextBox ID="txtHomeTeamTBC" runat="server" MaxLength="50" /> vs <asp:TextBox ID="txtAwayTeamTBC" runat="server" MaxLength="50" />
            </td>
            <td>
                Description: <asp:TextBox ID="txtMatchDescription" runat="server" MaxLength="50" />
            </td>
            <td>
                <asp:Label ID="lblKnockout" runat="server" Text="Knockout: " /><asp:CheckBox ID="chkKnockout" runat="server" />
            </td>
        </tr>
        <tr id="trError" runat="server">
            <td colspan="4">
                <asp:Label ID="lblError" runat="server" CssClass="Error" />
            </td>
        </tr>
    </table>

    <nug:GridView ID="gvMatch" runat="server" AutoGenerateColumns="false" GridLines="None" AllowPaging="true" PageSize="20" DataKeyNames="MatchID"
        CssClass="GridView FullWidth" RowStyle-CssClass="GridViewRow" AlternatingRowStyle-CssClass="GridViewRowAlt" RowHoverCssClass="GridViewRowHover" EnableRowClick="true" 
        RowClickRowCommand="EnableRowEdit">
        <Columns>
            <asp:TemplateField HeaderText="Home">
                <ItemTemplate>
                    <asp:Label ID="lblHomeTeam" runat="server" Text='<% #DirectCast(Container.DataItem, PredictathonModel.MatchListGet_Result).HomeTeam %>' />
                </ItemTemplate>
                <EditItemTemplate>
                    <asp:DropDownList ID="ddlHomeTeam" runat="server" DataTextField="TeamName" DataValueField="TeamID" /><br />
                    or <asp:TextBox ID="txtHomeTeamTBC" runat="server" Text='<%# DirectCast(Container.DataItem, PredictathonModel.MatchListGet_Result).HomeTeamTBC %>' MaxLength="50" />
                </EditItemTemplate>
            </asp:TemplateField>
            <asp:TemplateField HeaderText="Away">
                <ItemTemplate>
                    <asp:Label ID="lblAwayTeam" runat="server" Text='<%# DirectCast(Container.DataItem, PredictathonModel.MatchListGet_Result).AwayTeam %>' />
                </ItemTemplate>
                <EditItemTemplate>
                    <asp:DropDownList ID="ddlAwayTeam" runat="server" DataTextField="TeamName" DataValueField="TeamID" /><br />
                    or <asp:TextBox ID="txtAwayTeamTBC" runat="server" Text='<%# DirectCast(Container.DataItem, PredictathonModel.MatchListGet_Result).AwayTeamTBC %>' MaxLength="50" />
                </EditItemTemplate>
            </asp:TemplateField>
            <asp:TemplateField HeaderText="Date">
                <ItemTemplate>
                    <asp:Label ID="lblMatchDate" runat="server" Text='<%# DirectCast(Container.DataItem, PredictathonModel.MatchListGet_Result).MatchDateTime.ToString("dd/MM/yyyy") %>' />
                </ItemTemplate>
                <EditItemTemplate>
                    <nug:DatePicker ID="dteMatchDate" runat="server" ValidateDateAsMandatory="true" Text='<%# DirectCast(Container.DataItem, PredictathonModel.MatchListGet_Result).MatchDateTime.ToString("dd/MM/yyyy") %>' />                
                </EditItemTemplate>
            </asp:TemplateField>
            <asp:TemplateField HeaderText="Time">
                <ItemTemplate>
                    <asp:Label ID="lblMatchTime" runat="server" Text='<% #DirectCast(Container.DataItem, PredictathonModel.MatchListGet_Result).MatchDateTime.ToString("HH:mm") %>' />
                </ItemTemplate>
                <EditItemTemplate>
                    <nug:TimePicker ID="tmeMatchTime" runat="server" ValidateTimeAsMandatory="true" Text='<% #DirectCast(Container.DataItem, PredictathonModel.MatchListGet_Result).MatchDateTime.ToString("HH:mm") %>' />
                </EditItemTemplate>
            </asp:TemplateField>
            <asp:TemplateField HeaderText="Description">
                <ItemTemplate>
                    <asp:Label ID="lblDescription" runat="server" Text='<% #DirectCast(Container.DataItem, PredictathonModel.MatchListGet_Result).Description %>' />
                </ItemTemplate>
                <EditItemTemplate>
                    <asp:TextBox ID="txtMatchDescription" runat="server" Text='<% #DirectCast(Container.DataItem, PredictathonModel.MatchListGet_Result).Description %>' />
                </EditItemTemplate>
            </asp:TemplateField>
            <asp:TemplateField HeaderText="Home goals">
                <ItemTemplate>
                    <asp:Label ID="lblHomeTeamGoals" runat="server" Text='<% #DirectCast(Container.DataItem, PredictathonModel.MatchListGet_Result).HomeTeamGoals %>' />
                </ItemTemplate>
                <EditItemTemplate>
                    <asp:TextBox ID="txtHomeTeamGoals" runat="server" Text='<% #DirectCast(Container.DataItem, PredictathonModel.MatchListGet_Result).HomeTeamGoals %>' MaxLength="1" Width="2em" />
                </EditItemTemplate>
            </asp:TemplateField>
            <asp:TemplateField HeaderText="Away goals">
                <ItemTemplate>
                    <asp:Label ID="lblAwayTeamGoals" runat="server" Text='<% #DirectCast(Container.DataItem, PredictathonModel.MatchListGet_Result).AwayTeamGoals %>' />
                </ItemTemplate>
                <EditItemTemplate>
                    <asp:TextBox ID="txtAwayTeamGoals" runat="server" Text='<% #DirectCast(Container.DataItem, PredictathonModel.MatchListGet_Result).AwayTeamGoals %>' MaxLength="1" Width="2em" />
                </EditItemTemplate>
            </asp:TemplateField>
            <asp:TemplateField HeaderText="Neutral">
                <ItemTemplate>
                    <asp:Label ID="lblNeutralGround" runat="server" Text='<% #DirectCast(Container.DataItem, PredictathonModel.MatchListGet_Result).NeutralGround %>' />
                </ItemTemplate>
                <EditItemTemplate>
                    <asp:CheckBox ID="chkNeutralGround" runat="server" Checked='<% #DirectCast(Container.DataItem, PredictathonModel.MatchListGet_Result).NeutralGround %>' />
                </EditItemTemplate>
            </asp:TemplateField>
            <asp:TemplateField HeaderText="Knockout">
                <ItemTemplate>
                    <asp:Label ID="lblKnockout" runat="server" Text='<% #DirectCast(Container.DataItem, PredictathonModel.MatchListGet_Result).Knockout %>' />
                </ItemTemplate>
                <EditItemTemplate>
                    <asp:CheckBox ID="chkKnockout" runat="server" Checked='<% #DirectCast(Container.DataItem, PredictathonModel.MatchListGet_Result).Knockout %>' />
                </EditItemTemplate>
            </asp:TemplateField>
            <asp:TemplateField HeaderText="Save" Visible="false" ItemStyle-HorizontalAlign="Center">
                <EditItemTemplate>
                    <asp:ImageButton ID="btnSave" runat="server" CommandName="SaveRecord" ToolTip="Save" CommandArgument="<%# DirectCast(Container, GridViewRow).RowIndex %>" ImageUrl="~/Images/Common/Save.gif" />
                </EditItemTemplate>
            </asp:TemplateField>
            <asp:TemplateField HeaderText="Cancel" Visible="false" ItemStyle-HorizontalAlign="Center">
                <EditItemTemplate>
                    <asp:ImageButton ID="btnCancel" runat="server" CommandName="CancelEdit" ToolTip="Cancel" ImageUrl="~/Images/Common/Cancel.gif" />
                </EditItemTemplate>
            </asp:TemplateField>
            <asp:TemplateField HeaderText="Delete" Visible="false" ItemStyle-HorizontalAlign="Center">
                <EditItemTemplate>
                    <asp:ImageButton ID="btnDelete" runat="server" CommandName="DeleteRecord" ToolTip="Delete" CommandArgument="<%# DirectCast(Container, GridViewRow).RowIndex %>" ImageUrl="~/Images/Common/Delete.gif"
                        OnClientClick="return confirm('Are you sure you wish to delete this match?');" />
                </EditItemTemplate>
            </asp:TemplateField>
        </Columns>
    </nug:GridView>
</asp:Content>