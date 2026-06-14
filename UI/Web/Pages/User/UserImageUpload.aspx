<%@ Page Language="vb" AutoEventWireup="false" CodeBehind="UserImageUpload.aspx.vb" Inherits="Predictathon.Pages.UserImageUpload" %>
<!DOCTYPE html>

<html lang="en">
<head runat="server">
    <title>Predictathon - Profile Image Upload</title>
</head>
<body style="background-image: none">
    <form id="form1" runat="server">
        <asp:ScriptManager ID="ScriptManager1" runat="server">
            <Scripts>
                <asp:ScriptReference Path="~/Scripts/jquery.js" ScriptMode="Release" />
                <asp:ScriptReference Path="~/Scripts/jquery.jcrop.min.js" ScriptMode="Release" />
            </Scripts>
        </asp:ScriptManager>

        <asp:HiddenField ID="hdnFileExtension" runat="server" />

        <script type="text/javascript">
            function ShowImageCropOption() {
                var image = $('#<%=imgProfile.ClientID %>');
                // Trim the querystring off the image URL.
                var path = image.attr('src');
                if (path.indexOf('?') > 0) path = path.substr(0, path.indexOf('?'));
                //Define a function to execute when the cropping rectangle changes.
                var update = function (coords) {
                    if (parseInt(coords.w) <= 0 || parseInt(coords.h) <= 0) return; //Require valid width and height

                    // Update the hidden field's value based on the new coordinates. The resizing module will handle everything else.
                    $('#<%=hdnImage.ClientID %>').val(path + '?crop=(' + coords.x + ',' + coords.y + ',' + coords.x2 + ',' + coords.y2 + ')&cropxunits=' + image.width() + '&cropyunits=' + image.height());
                }

                //Start up jCrop on the image, specifying our function to be called when the selection rectangle changes, and that a 60% black shadow should cover the cropped regions.
                image.Jcrop({ onChange: update, onSelect: update, bgColor: 'black', bgOpacity: 0.6, aspectRatio: 10 / 8, setSelect: [0, 0, 400, 500] });
            }
        </script>
        <asp:HiddenField ID="hdnImage" runat="server" />

        <div id="divProfileImage" runat="server" class="image-cropper" style="margin-bottom: 10px">
            <asp:Image ID="imgProfile" runat="server" ImageUrl="~/Images/Branding/FootballBackground.jpg?width=400" Width="400px" CssClass="Image" />
        </div>

        <!-- success / status message -->
        <asp:Label ID="lblMessage" runat="server" Visible="false" ForeColor="Green" />

        <asp:Label ID="lblUploadFile" runat="server" Text="Upload profile picture: " /><asp:FileUpload ID="FileUpload1" runat="server" Text="Upload new image" />
        <asp:Button ID="btnUpload" runat="server" Text="Upload" />
        <asp:Button ID="btnSave" runat="server" Text="Save" />
    </form>
</body>
</html>