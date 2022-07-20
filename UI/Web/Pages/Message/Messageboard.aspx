<%@ Page Language="vb" AutoEventWireup="false" MasterPageFile="~/Pages/Master/Main.Master" CodeBehind="Messageboard.aspx.vb" Inherits="Predictathon.Pages.Message.Messageboard" %>
<%@ MasterType TypeName="Predictathon.Master.Main" %>
<%@ Register Src="~/UserControls/Message/MessageThreadList.ascx" TagName="MessageThreadList" TagPrefix="uc1" %>

<asp:Content runat="server" ContentPlaceHolderID="MainContent">
    <uc1:MessageThreadList ID="MessageThreadList1" runat="server" />
</asp:Content>