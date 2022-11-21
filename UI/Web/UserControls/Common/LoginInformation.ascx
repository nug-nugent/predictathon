<%@ Control Language="vb" AutoEventWireup="false" CodeBehind="LoginInformation.ascx.vb" Inherits="Predictathon.UserControls.LoginInformation" %>
<span style="font-size: large">
    <asp:Label ID="lblCurrentCompetition" runat="server" />
</span>
<span>
    <asp:Label ID="lblLoginInfo" runat="server" Text="Logged in as " /><asp:HyperLink ID="hypUser" runat="server" />
    <asp:LinkButton ID="btnLogout" runat="server" ForeColor="Yellow" Font-Bold="true" Text="(log out)" />
</span>