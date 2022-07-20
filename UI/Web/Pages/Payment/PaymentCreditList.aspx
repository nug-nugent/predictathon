<%@ Page Language="vb" AutoEventWireup="false" MasterPageFile="~/Pages/Master/Main.Master" CodeBehind="PaymentCreditList.aspx.vb" Inherits="Predictathon.Pages.Payment.PaymentCreditList" %>
<%@ MasterType TypeName="Predictathon.Master.Main" %>

<asp:Content runat="server" ContentPlaceHolderID="MainContent">
    <table id="tblAddNew" runat="server">
        <tr>
            <td>
                <asp:Label ID="lblAdd" runat="server" CssClass="GridTitle" Text="Add Credit" />
            </td>
        </tr>
        <tr>
            <td>
                <asp:Label ID="lblCompetition" runat="server" Text="For competition: " /><asp:DropDownList ID="ddlCompetition" runat="server" DataTextField="CompetitionName" DataValueField="CompetitionID" />
                <asp:Label ID="lblExpectedUsername" runat="server" Text="For user (expected username): " /><asp:TextBox ID="txtExpectedUsername" runat="server" />
                <asp:ImageButton ID="btnAdd" runat="server" ImageUrl="~/Images/Common/Add.gif" ImageAlign="Top" AlternateText="Add" />
            </td>
        </tr>
        <tr id="trError" runat="server">
            <td>
                <asp:Label ID="lblError" runat="server" CssClass="Error" />
            </td>
        </tr>
    </table>

    <nug:GridView ID="gvPaymentCredit" runat="server" AutoGenerateColumns="false" GridLines="None" AllowPaging="true" PageSize="20" DataKeyNames="PaymentCreditID"
        CssClass="GridView FullWidth" RowStyle-CssClass="GridViewRow" AlternatingRowStyle-CssClass="GridViewRowAlt" EnableRowClick="false">
        <Columns>
            <asp:BoundField HeaderText="Expected username" DataField="ExpectedUsername" />
            <asp:BoundField HeaderText="Unique payment code" DataField="UniquePaymentCode" />
            <asp:BoundField HeaderText="Issued" DataField="IssueDate" DataFormatString="{0:dd/MM/yyyy HH:mm}" />
            <asp:BoundField HeaderText="Used" DataField="CreditUsed" />
            <asp:TemplateField HeaderText="Delete" ItemStyle-HorizontalAlign="Center">
                <ItemTemplate>
                    <asp:ImageButton ID="btnDelete" runat="server" CommandName="DeleteRecord" ToolTip="Delete" CommandArgument="<%# DirectCast(Container, GridViewRow).RowIndex %>" ImageUrl="~/Images/Common/Delete.gif"
                        OnClientClick="return confirm('Are you sure you wish to delete this credit?');" />
                </ItemTemplate>
            </asp:TemplateField>
        </Columns>
    </nug:GridView>
</asp:Content>