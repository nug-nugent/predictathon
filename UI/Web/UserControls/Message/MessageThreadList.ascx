<%@ Control Language="vb" AutoEventWireup="false" CodeBehind="MessageThreadList.ascx.vb" Inherits="Predictathon.UserControls.Message.MessageThreadList" %>

<div style="width: 100%; text-align: center; margin-top: 4px; margin-bottom: 4px; font-weight: bold; font-size: 18px">
    <asp:LinkButton ID="lnkNewMessageThread" runat="server" Text="START A NEW THREAD" />
</div>
<nug:GridView ID="gvMessageThreadList" runat="server" AutoGenerateColumns="false" GridLines="None" AllowPaging="true" PageSize="20" CssClass="GridView" DataKeyNames="MessageThreadID"
    RowStyle-CssClass="GridViewRow" EnableRowClick="true" AlternatingRowStyle-CssClass="GridViewRowAlt" RowHoverCssClass="GridViewRowHover" EmptyDataText="No threads found">
    <Columns>
        <asp:TemplateField HeaderText="Unread" ItemStyle-Width="5%" ItemStyle-HorizontalAlign="Center" HeaderStyle-HorizontalAlign="Center">
            <ItemTemplate>
                <asp:Image ID="imgUnread" runat="server" ImageUrl='<%# UnreadImageURL(CBool(Eval("Unread"))) %>' />
            </ItemTemplate>
        </asp:TemplateField>
        <asp:TemplateField HeaderText="Thread" HeaderStyle-HorizontalAlign="Left" ItemStyle-Width="80%">
            <ItemTemplate>
                <div runat="server" id="divLastMessage" style="float: right; margin-right: 15px; vertical-align: middle" Visible='<%#Not String.IsNullOrEmpty(Eval("LastMessage").ToString) %>'><span class="QuotationMark">&#8220;</span><asp:Label ID="lblCaption" runat="server" CssClass="Quote" Text='<%#Eval("LastMessage") %>' /><span class="QuotationMark">&#8221;</span></div>
                <asp:Label ID="lblThreadSubject" runat="server" CssClass="ThreadTitle" Text='<%#Eval("ThreadSubject") %>' /><br />
                <span class="ThreadTitleSubText">
                    Last post by <asp:Label ID="lblLastMessagePostedByUser" runat="server" Text='<%#Eval("LastMessagePostedByUser") %>' /> on 
                    <asp:Label ID="lblLastMessageDate" runat="server" Text='<%#Predictathon.CommonMethods.LongDateAndTimeString(CDate(Eval("LastMessageDate")), False) %>' /> at 
                    <asp:Label ID="lblLastMessageTime" runat="server" Text='<%#CDate(Eval("LastMessageDate")).ToString("HH:mm") %>' />
                </span>
            </ItemTemplate>
        </asp:TemplateField>
        <asp:BoundField HeaderText="Started by" ItemStyle-Width="10%" ItemStyle-HorizontalAlign="Center" HeaderStyle-HorizontalAlign="Center" DataField="StartedByUser" />
        <asp:BoundField HeaderText="Replies" ItemStyle-Width="5%" ItemStyle-CssClass="Score" ItemStyle-HorizontalAlign="Center" HeaderStyle-HorizontalAlign="Center" DataField="ReplyCount" />
    </Columns>
</nug:GridView>