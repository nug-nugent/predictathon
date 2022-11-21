<%@ Control Language="vb" AutoEventWireup="false" CodeBehind="NewMessage.ascx.vb" Inherits="Predictathon.UserControls.Message.NewMessage" %>

<script type="text/javascript">
    function ShowUploadOptions(Show) {
        switch (Show) {
            case 'None':
                $('#<%=divYouTubeVideoLink.ClientID %>').hide();
                $('#<%=divUploadFile.ClientID %>').hide();
                break;
            case 'Video':
                $('#<%=divYouTubeVideoLink.ClientID %>').show();
                $('#<%=divUploadFile.ClientID %>').hide();
                break;
            case 'Image':
                $('#<%=divUploadFile.ClientID %>').show();
                $('#<%=divYouTubeVideoLink.ClientID %>').hide();
                break;
        }
    }
    
    function FileChosen() {
        document.getElementById('<%=txtUploadFromURL.ClientID %>').value='';
        $('#<%=spnUploadFromURL.ClientID %>').hide();
    }

    function DisableButton() {
        document.getElementById('<%= btnSubmit.ClientID %>').disabled = true;
        return true;
    }
</script>

<asp:TextBox ID="txtMessageContent" runat="server" TextMode="MultiLine" Rows="4" placeholder="Reply..." class="ReplyTextArea" /><br />

<div id="divAttachment" runat="server" style="padding-bottom: 5px">
    <asp:RadioButton ID="rbNone" runat="server" Text="No attachment" GroupName="rbAttachment" Checked="true" />
    <asp:RadioButton ID="rbUploadImage" runat="server" Text="Image" GroupName="rbAttachment" />
    <asp:RadioButton ID="rbYouTubeVideoLink" runat="server" Text="YouTube video" GroupName="rbAttachment" />
    <asp:Button ID="btnSubmit" runat="server" Text="Submit" OnClientClick="DisableButton()" UseSubmitBehavior="False" style="float: right" />
</div>
<div id="divYouTubeVideoLink" runat="server" style="display: none">
    <asp:Label ID="lblYouTubeVideoLink" runat="server" Text="URL or 11-character YouTube video ID: " /><asp:TextBox ID="txtYouTubeVideoLink" runat="server" /><br />
</div>
<div id="divUploadFile" runat="server" style="display: none">
    <asp:Label ID="lblUploadFile" runat="server" Text="Select image to upload: " /><asp:FileUpload ID="FileUpload1" runat="server" Text="Attach image" onchange="FileChosen();" />
    <span id="spnUploadFromURL" runat="server"><asp:Label ID="lblUploadFromURL" runat="server" Text="... or upload from a URL: " /><asp:TextBox ID="txtUploadFromURL" runat="server" Width="20em" /></span>
</div>