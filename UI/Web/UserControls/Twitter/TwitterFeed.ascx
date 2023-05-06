<%@ Control Language="vb" AutoEventWireup="false" CodeBehind="TwitterFeed.ascx.vb" Inherits="Predictathon.UserControls.Twitter.TwitterFeed" %>

<a class="twitter-timeline" 
    href="https://twitter.com/<%=TwitterProfileName%>?ref_src=twsrc%5Etfw"
    data-height="300"
    data-tweet-limit="<%=TweetsToDisplay%>">
    Tweets by <%=TwitterProfileName %>
</a>
<script async src="https://platform.twitter.com/widgets.js" charset="utf-8"></script>