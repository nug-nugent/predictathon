<%@ Control Language="vb" AutoEventWireup="false" CodeBehind="NewMessageThread.ascx.vb" Inherits="Predictathon.UserControls.Message.NewMessageThread" %>

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

<asp:Label ID="lblThreadSubject" runat="server" Text="Subject: " /><br />
<asp:TextBox ID="txtThreadSubject" runat="server" Width="30em" MaxLength="50" /><br />
<div id="divHiddenFromPublic" runat="server" visible="false">
    <asp:Label ID="lblUsername" runat="server" Text="Hidden from public: " /><asp:CheckBox ID="chkHiddenFromPublic" runat="server" />
</div>
<asp:Label ID="lblMessageContent" runat="server" Text="Message: " /><br />
<asp:TextBox ID="txtMessageContent" runat="server" TextMode="MultiLine" Rows="7" Width="99%" /><br />

<div id="divAttachment" runat="server">
    <asp:RadioButton ID="rbNone" runat="server" Text="No attachment" GroupName="rbAttachment" Checked="true" />
    <asp:RadioButton ID="rbUploadImage" runat="server" Text="Upload image" GroupName="rbAttachment" />
    <asp:RadioButton ID="rbYouTubeVideoLink" runat="server" Text="YouTube video" GroupName="rbAttachment" />
</div>
<div id="divYouTubeVideoLink" runat="server" style="display: none">
    <asp:Label ID="lblYouTubeVideoLink" runat="server" Text="URL or 11-character YouTube video ID: " /><asp:TextBox ID="txtYouTubeVideoLink" runat="server" /><br />
</div>
<div id="divUploadFile" runat="server" style="display: none">
    <asp:Label ID="lblUploadFile" runat="server" Text="Select image to upload: " /><asp:FileUpload ID="FileUpload1" runat="server" Text="Attach image" onchange="FileChosen();" />
    <span id="spnUploadFromURL" runat="server"><asp:Label ID="lblUploadFromURL" runat="server" Text="... or upload from a URL: " /><asp:TextBox ID="txtUploadFromURL" runat="server" Width="20em" /></span>
</div>
<div style="width: 100%; text-align: center"><asp:Button ID="btnSubmit" runat="server" Text="Submit" OnClientClick="DisableButton()" UseSubmitBehavior="False" /></div>