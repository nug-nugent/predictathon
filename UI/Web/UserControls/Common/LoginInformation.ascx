<%@ Control Language="vb" AutoEventWireup="false" CodeBehind="LoginInformation.ascx.vb" Inherits="Predictathon.UserControls.LoginInformation" %>
<span class="LoginInformation">
    <span style="font-size: large"><asp:Label ID="lblCurrentCompetition" runat="server" /><br /></span>
    <asp:Label ID="lblLoginInfo" runat="server" Text="You are logged in as " /><asp:HyperLink ID="hypUser" runat="server" />
    <asp:LinkButton ID="btnLogout" runat="server" ForeColor="Yellow" Font-Bold="true" Text="(log out)" />
</span>