<%@ Page Language="vb" AutoEventWireup="false" MasterPageFile="~/Pages/Master/Main.Master" CodeBehind="Rules.aspx.vb" Inherits="Predictathon.Pages.Rules.Rules" %>
<%@ MasterType TypeName="Predictathon.Master.Main" %>
<%@ Register Src="~/UserControls/Rules/Rules.ascx" TagPrefix="uc1" TagName="Rules" %>

<asp:Content runat="server" ContentPlaceHolderID="MainContent">
    <uc1:Rules ID="Rules1" runat="server" />    
</asp:Content>