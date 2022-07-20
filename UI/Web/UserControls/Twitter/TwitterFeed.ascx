<%@ Control Language="vb" AutoEventWireup="false" CodeBehind="TwitterFeed.ascx.vb" Inherits="Predictathon.UserControls.Twitter.TwitterFeed" %>

<div id="divTwitter" runat="server" class="InformationBlock">
    <asp:Hyperlink ID="hypTwitter" runat="server" Target="_blank">
        <asp:Image ID="imgTwitpic" runat="server" style="float: left; position: relative; margin-right: 5px; margin-bottom: 5px" />
    </asp:Hyperlink>
    <h1><asp:Label ID="lblName" runat="server" /> <asp:Label ID="lblScreenName" runat="server" ForeColor="#888888" /></h1>
    <hr />
    <asp:ListView ID="lstTimeline" runat="server">
        <ItemTemplate>
            <%# Eval("TweetContent") %>
            <span class="SubNote"><%# Eval("CreatedAtRelativeDateTime") %></span>
            <hr />
        </ItemTemplate>
    </asp:ListView>
    <div style="width: 100%; text-align: center; margin-top: 5px; margin-bottom: 10px">
        <asp:HyperLink ID="hypTwitter2" runat="server" Target="_blank">
            <strong>See more...</strong>
        </asp:HyperLink>
    </div>
</div>