<%@ Control Language="vb" AutoEventWireup="false" CodeBehind="PasswordReset.ascx.vb" Inherits="Predictathon.UserControls.User.PasswordReset" %>

<asp:Panel ID="pnlPasswordReset" runat="server" DefaultButton="btnResetPassword">
    <div class="TitleBar" style="margin-bottom: 5px;">Password Reset</div>
    <asp:Label ID="lblUsernameOrEmail" runat="server" Text="Username or email address:" />
    <asp:TextBox ID="txtUsernameOrEmail" runat="server" MaxLength="128" Width="18em" />
    <p><asp:Button ID="btnResetPassword" runat="server" Text="Request Password" /></p>
    <asp:Label ID="lblError" runat="server" Visible="false" CssClass="Error" />
</asp:Panel>