<%@ Page Language="vb" AutoEventWireup="false" MasterPageFile="~/Pages/Master/Main.Master" CodeBehind="MessageThreadDetail.aspx.vb" Inherits="Predictathon.Pages.Message.MessageThreadDetail" %>
<%@ MasterType TypeName="Predictathon.Master.Main" %>
<%@ Register Src="~/UserControls/Message/NewMessage.ascx" TagName="NewMessage" TagPrefix="uc1" %>

<asp:Content runat="server" ContentPlaceHolderID="MainContent">
    <asp:PlaceHolder runat="server"> 
        <%: System.Web.Optimization.Scripts.Render("~/js/messagelist") %> 
    </asp:PlaceHolder>
    <script type="text/javascript">
        window.appPath = "<%= AppPath %>";
        window.currentUserId = "<%= CurrentUserId %>";
        window.threadId = "<%= ThreadId %>";
        window.threadTitle = "<%= ThreadTitle %>";
        window.messagesBefore = <%= MessagesBefore %>;
        window.messagesAfter = <%= MessagesAfter %>;
        window.firstUnreadMessageId = "<%= FirstUnreadMessageId %>";
        window.threadMessages = <%= MessagesJson %>;
    </script>

    <div id="message-list"></div>

    <div class="ReplyContainer">
        <uc1:NewMessage ID="NewMessage1" runat="server" />
    </div>
</asp:Content>