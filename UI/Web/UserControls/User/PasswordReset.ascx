<%@ Control Language="vb" AutoEventWireup="false" CodeBehind="PasswordReset.ascx.vb" Inherits="Predictathon.UserControls.User.PasswordReset" %>

<asp:Panel ID="pnlPasswordReset" runat="server" DefaultButton="btnResetPassword" class="LoginInput">
    <asp:TextBox ID="txtUsernameOrEmail" runat="server" MaxLength="128" placeholder="Username or email address" />
    <p><asp:Button ID="btnResetPassword" runat="server" Text="Request Password" /></p>
    <asp:Label ID="lblError" runat="server" Visible="false" CssClass="Error" />
</asp:Panel>