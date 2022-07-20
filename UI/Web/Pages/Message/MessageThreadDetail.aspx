<%@ Page Language="vb" AutoEventWireup="false" MasterPageFile="~/Pages/Master/Main.Master" CodeBehind="MessageThreadDetail.aspx.vb" Inherits="Predictathon.Pages.Message.MessageThreadDetail" %>
<%@ MasterType TypeName="Predictathon.Master.Main" %>
<%@ Register Src="~/UserControls/Message/NewMessage.ascx" TagName="NewMessage" TagPrefix="uc1" %>

<asp:Content runat="server" ContentPlaceHolderID="MainContent">
    <script type="module" src="../../Scripts/emoji-button.js"></script>
    <script type="text/javascript">
        $(document).ready(function () {
            if (window.initEmojiButton) initEmojiButton('<%=Request.ApplicationPath.TrimEnd("/"c) %>/Images/Message/Reactions');
        });
    </script>

    <script type="text/javascript" src="https://unpkg.com/@popperjs/core@2"></script>
    <script type="text/javascript" src="https://unpkg.com/tippy.js@6"></script>
    <link rel="stylesheet" href="https://unpkg.com/tippy.js@6/themes/light-border.css" />

    <script type="text/javascript" src="../../Scripts/PostReactions.js"></script>

    <script type="text/javascript">
        jQuery.fn.center = function () {
            this.css("position", "absolute");
            this.css("top", (($(window).height() - this.outerHeight()) / 2) + $(window).scrollTop() + "px");
            this.css("left", (($(window).width() - this.outerWidth()) / 2) + $(window).scrollLeft() + "px");
            return this;
        }

        function ShowPopup(ImageURL) {
            $("<div></div>").attr("id", "divOverlay").attr("class", "Overlay").html('').appendTo($('body'))
            $('#<%=imgLarge.ClientID %>').load(function () {
                $('#<%=divPopup.ClientID %>').center();
                $('#<%=divPopup.ClientID %>').show();
                $('#<%=imgLarge.ClientID %>').off();
            }).attr("src", ImageURL);
        }

        function HidePopup() {
            $('#<%=divPopup.ClientID %>').hide();
            $("#divOverlay").remove();
        }

        $(document).ready(function () {
            $('.ThreadMessageText').find('a').attr('target', '_blank');

            $.timeago.settings.strings.seconds = 'moments';
            $.timeago.settings.strings.minute = 'a minute';
            $.timeago.settings.strings.hour = 'an hour';
            $.timeago.settings.strings.hours = '%d hours';
            $.timeago.settings.strings.month = 'a month';
            $.timeago.settings.strings.year = 'a year';
            $('time').timeago().show();
        });
    </script>

    <div id="divPopup" runat="server" class="PopupWindow">
        <div id="divHide" onclick="HidePopup();" class="Close"></div>
        <asp:Image ID="imgLarge" runat="server" />
    </div>

    <div style="width: 100%; text-align: center; margin-top: 4px; margin-bottom: 4px">
        <asp:Label ID="lblThreadSubject" runat="server" CssClass="ThreadTitle" />
    </div>

    <nug:GridView ID="gvMessageList" runat="server" AutoGenerateColumns="false" GridLines="None" AllowPaging="true" PageSize="10" CssClass="FullWidth" DataKeyNames="MessageId"
        ShowHeader="false" EnableRowClick="false" EmptyDataText="No messages found" BorderStyle="None" CellSpacing="15">
        <Columns>
            <asp:TemplateField>
                <ItemTemplate>
                    <div class="ThreadMessage">
                        <div class="ThreadMessageProfileImage">
                            <asp:HyperLink ID="hypUserImage" runat="server" NavigateUrl='<%# "~/Pages/User/UserDetail.aspx?UserID=" & DirectCast(Container.DataItem, ThreadMessageDto).UserId.ToString %>'>
                                <asp:Image ID="imgUser" runat="server" ImageUrl='<%# UserImageURL(DirectCast(Container.DataItem, ThreadMessageDto).UserImageUploaded, DirectCast(Container.DataItem, ThreadMessageDto).UserId) %>' />
                            </asp:HyperLink>
                        </div>
                        <div class="ThreadMessageHeader">
                            <div class="ThreadMessagePostedBy">
                                <asp:HyperLink ID="hypUser" runat="server" NavigateUrl='<%# "~/Pages/User/UserDetail.aspx?UserID=" & DirectCast(Container.DataItem, ThreadMessageDto).UserId.ToString %>'>
                                    <%# DirectCast(Container.DataItem, ThreadMessageDto).Username%></asp:HyperLink>
                            </div>
                            <div class="ThreadMessagePostedWhen">
                                <time style="display: none" datetime="<%# DirectCast(Container.DataItem, ThreadMessageDto).MessageDateTime.ToString("o") %>">
                                    <%# DirectCast(Container.DataItem, ThreadMessageDto).MessageDateTime.ToString("f") %>
                                    </time>
                            </div>

                            <div class="ThreadMessageAddReaction"></div>

                            <asp:HiddenField ID="hdnMessageId" runat="server" Value='<%# Eval("MessageId").ToString %>' />
                            <asp:HiddenField ID="hdnReactions" runat="server" Value="<%# Predictathon.MessageReactionManager.GetReactionsJson(DirectCast(Container.DataItem, ThreadMessageDto).Reactions) %>" />
                        </div>
                        <div class="ThreadMessageText">
                            <div id="divYouTubeVideo" runat="server" class="ThreadMessageLinkedVideo" visible="false">
                            </div>
                            <div id="divLinkedImage" runat="server" class="ThreadMessageLinkedImage" visible="false">
                                <asp:Image ID="imgMessage" runat="server" style="cursor: pointer" />
                            </div>
                            <%# Predictathon.MessageManager.FormatMessage(DirectCast(Container.DataItem, ThreadMessageDto).MessageContent)%>
                        </div>
                        <div class="ThreadMessageFooter">
                            This was <%# DirectCast(Container.DataItem, ThreadMessageDto).Username%>'s
                            <%# Predictathon.CommonMethods.IntegerToOrdinal(DirectCast(Container.DataItem, ThreadMessageDto).UserPostNumber) %>  post, of 
                            <%# DirectCast(Container.DataItem, ThreadMessageDto).UserTotalPosts %> in total.
                        </div>
                    </div>
                </ItemTemplate>
            </asp:TemplateField>
        </Columns>
    </nug:GridView>
    <div style="width: 100%; text-align: center; margin-top: 4px; margin-bottom: 4px">
        <asp:Label ID="lblReply" runat="server" CssClass="ThreadTitle" Text="Reply" />
    </div>
    <uc1:NewMessage ID="NewMessage1" runat="server" />
</asp:Content>