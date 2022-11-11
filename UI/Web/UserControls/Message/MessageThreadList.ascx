<%@ Control Language="vb" AutoEventWireup="false" CodeBehind="MessageThreadList.ascx.vb" Inherits="Predictathon.UserControls.Message.MessageThreadList" %>

<script>
    $(document).ready(function () {
        $.timeago.settings.strings.seconds = 'moments';
        $.timeago.settings.strings.minute = 'a minute';
        $.timeago.settings.strings.hour = 'an hour';
        $.timeago.settings.strings.hours = '%d hours';
        $.timeago.settings.strings.month = 'a month';
        $.timeago.settings.strings.year = 'a year';
        $('time').timeago().show();
    });
</script>

<div style="width: 100%; text-align: center; margin-top: 4px; margin-bottom: 4px; font-weight: bold; font-size: 18px">
    <asp:LinkButton ID="lnkNewMessageThread" runat="server" Text="START A NEW THREAD" />
</div>
<div style="max-width: 1000px; margin: auto">
    <nug:GridView ID="gvMessageThreadList" runat="server" AutoGenerateColumns="false" GridLines="None" AllowPaging="true" PageSize="20" CssClass="GridView NoBorder Padded" DataKeyNames="MessageThreadID"
        RowStyle-CssClass="GridViewRow" EnableRowClick="true" AlternatingRowStyle-CssClass="GridViewRowAlt" RowHoverCssClass="GridViewRowHover" EmptyDataText="No threads found">
        <Columns>
            <asp:TemplateField HeaderText="Threads">
                <ItemTemplate>
                    <asp:Image ID="imgUnread" runat="server" ImageUrl="~/Images/Message/Unread.gif" Visible='<%# CBool(Eval("Unread")) %>' Style="float: left; padding-right: 8px; padding-top: 3px" />
                    <div class="ThreadCol">
                        <div>
                            <asp:Label ID="lblThreadSubject" runat="server" CssClass="ThreadTitle" Text='<%#Eval("ThreadSubject") %>' />
                        </div>
                        <div class="ThreadLastMessage">
                            <div runat="server" id="div1" Visible='<%#Not String.IsNullOrEmpty(Eval("LastMessage").ToString) %>'>
                                <span class="QuotationMark">&#8220;</span><asp:Label ID="Label1" runat="server" CssClass="Quote" Text='<%#Eval("LastMessage") %>' /><span class="QuotationMark">&#8221;</span>
                            </div>
                        </div>
                    </div>
                    <div class="ThreadCol">
                        <div class="ThreadStartedBy">Started by <%#Eval("StartedByUser") %></div>
                        <div class="ThreadLastMessageBy">
                            Last post 
                            <time style="display:none" datetime="<%#CDate(Eval("LastMessageDate")).ToString("o") %>"><%#CDate(Eval("LastMessageDate")).ToString("f") %></time>
                            by <asp:Label ID="lblLastMessagePostedByUser" runat="server" Text='<%#Eval("LastMessagePostedByUser") %>' />
                        </div>
                    </div>
                </ItemTemplate>
            </asp:TemplateField>
            <asp:BoundField HeaderText="Replies" ItemStyle-Width="60px" ItemStyle-CssClass="Score" ItemStyle-HorizontalAlign="Center" HeaderStyle-HorizontalAlign="Center" DataField="ReplyCount" />
        </Columns>
    </nug:GridView>
</div>
