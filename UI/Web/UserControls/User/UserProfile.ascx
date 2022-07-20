<%@ Control Language="vb" AutoEventWireup="false" CodeBehind="UserProfile.ascx.vb" Inherits="Predictathon.UserControls.User.UserProfile" %>
<script type="text/javascript">
    jQuery.fn.center = function () {
        this.css("position", "absolute");
        this.css("top", (($(window).height() - this.outerHeight()) / 2) + $(window).scrollTop() + "px");
        this.css("left", (($(window).width() - this.outerWidth()) / 2) + $(window).scrollLeft() + "px");
        return this;
    }

    function ShowPopup() {
        $("<div></div>").attr("id", "divOverlay").attr("class", "Overlay").html('').appendTo($('body'))
        $('#<%=divPopup.ClientID %>').center();
        $('#<%=divPopup.ClientID %>').show();
    }

    function HidePopup() {
        $('#<%=divPopup.ClientID %>').hide();
        $("#divOverlay").remove();
    }
</script>

<div id="divPopup" runat="server" class="PopupWindow">
    <div id="divHide" onclick="HidePopup();" class="Close"></div>
    <asp:Image ID="imgLarge" runat="server" />
</div>

<div id="divUserProfile" runat="server" class="InputBlock" style="min-height: 180px">
    <div class="TitleBar" style="margin-bottom: 5px">
        <table>
            <tr>
                <td align="left">
                    <asp:Label ID="lblUsername" runat="server" />
                </td>
                <td align="right">
                    <asp:LinkButton ID="btnEditUser" runat="server" CssClass="HighlightedLink" Text="Edit User" />
                </td>
            </tr>
        </table>
    </div>
    <div style="padding-left: 8px; vertical-align: top">
        <asp:Image ID="imgProfile" runat="server" style="margin-right: 10px; float: right" />
        <p runat="server" id="pCaption" style="margin-bottom: 5px; margin-top: 0px; vertical-align: middle"><span class="QuotationMark">&#8220;</span><asp:Label ID="lblCaption" runat="server" CssClass="Quote" /><span class="QuotationMark">&#8221;</span><br /></p>
        <p runat="server" id="pProfileText" style="margin-top: 5px"><em><asp:Label ID="lblProfileText" runat="server" CssClass="QuoteSmall" /></em></p>
        <p runat="server" id="pLocation" style="line-height: 2px"><asp:Label ID="lblLocation" runat="server" Text="Location: " /><asp:Label ID="lblLocationValue" runat="server" /><br /></p>
        <p runat="server" id="pFavouriteTeam" style="line-height: 2px"><asp:Label ID="lblFavouriteTeam" runat="server" Text="Favourite team: " /><asp:Label ID="lblFavouriteTeamValue" runat="server" /><br /></p>
        <p runat="server" id="pNoInfoAvailable" style="line-height: 2px"><em><asp:Label ID="lblNoInfoAvailable" runat="server" Text="No profile information available." /></em></p>
    </div>
</div>