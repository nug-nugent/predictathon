<%@ Page Language="vb" AutoEventWireup="false" MasterPageFile="~/Pages/Master/Main.Master" CodeBehind="NewMessageThread.aspx.vb" Inherits="Predictathon.Pages.Message.NewMessageThread" %>
<%@ MasterType TypeName="Predictathon.Master.Main" %>
<%@ Register Src="~/UserControls/Message/NewMessageThread.ascx" TagName="NewMessageThread" TagPrefix="uc1" %>

<asp:Content runat="server" ContentPlaceHolderID="MainContent">
    <uc1:NewMessageThread ID="NewMessageThread1" runat="server" />
</asp:Content>