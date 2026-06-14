<%@ Control Language="vb" AutoEventWireup="false" CodeBehind="UserImageUpload.ascx.vb" Inherits="Predictathon.UserControls.User.UserImageUpload" %>
<asp:ScriptManagerProxy runat="server" />

<asp:HiddenField ID="hdnFileExtension" runat="server" />
<asp:HiddenField ID="hdnUserID" runat="server" />

<div id="divProfileImage" runat="server" class="image-cropper" style="margin-bottom: 10px">
    <asp:Image ID="imgProfile" runat="server" ImageUrl="~/Images/Branding/FootballBackground.jpg?width=400" Width="400px" CssClass="Image" />
</div>

<!-- success / status message -->
<asp:Label ID="lblMessage" runat="server" ForeColor="Green" />

<!-- processing spinner overlay -->
<div id="divSpinner" style="display:none; position:fixed; left:0; top:0; right:0; bottom:0; background:rgba(0,0,0,0.45); z-index:10000; text-align:center;">
    <div style="position:absolute; top:50%; left:50%; transform:translate(-50%,-50%); color:#fff; font-size:16px;">
        <div style="margin-bottom:8px">Processing...</div>
        <div style="width:40px; height:40px; margin:0 auto; border:6px solid rgba(255,255,255,0.3); border-top:6px solid #ffffff; border-radius:50%; animation:spin 1s linear infinite;"></div>
    </div>
</div>

<style type="text/css">
    @keyframes spin { from { transform: rotate(0deg); } to { transform: rotate(360deg); } }
</style>

<asp:Label ID="lblUploadFile" runat="server" Text="Upload profile picture: " />
<asp:FileUpload ID="FileUpload1" runat="server" Text="Upload new image" />
<asp:Button ID="btnUpload" runat="server" Text="Upload" OnClientClick="return false;" />
<asp:Button ID="btnSave" runat="server" Text="Save" OnClientClick="return false;" />

<!-- Cropper.js -->
<link href="https://cdnjs.cloudflare.com/ajax/libs/cropperjs/1.5.13/cropper.min.css" rel="stylesheet" />
<script src="https://cdnjs.cloudflare.com/ajax/libs/cropperjs/1.5.13/cropper.min.js"></script>

<script type="text/javascript">
    (function () {
        var init = function () {
            var cropper = null;
            var image = document.getElementById('<%= imgProfile.ClientID %>');
            var fileInput = document.getElementById('<%= FileUpload1.ClientID %>');
            var btnUpload = document.getElementById('<%= btnUpload.ClientID %>');
            var btnSave = document.getElementById('<%= btnSave.ClientID %>');
            var lblMessage = document.getElementById('<%= lblMessage.ClientID %>');

            if (!image || !fileInput || !btnSave) {
                // required elements not present yet
                return;
            }

            function destroyCropper() {
                if (cropper) {
                    cropper.destroy();
                    cropper = null;
                }
            }

            function showSpinner() { document.getElementById('divSpinner').style.display = ''; }
            function hideSpinner() { document.getElementById('divSpinner').style.display = 'none'; }

            fileInput.addEventListener('change', function (e) {
                if (!e.target.files || !e.target.files.length) return;
                var file = e.target.files[0];
                if (!file.type.match('image.*')) return;

                var reader = new FileReader();
                reader.onload = function (evt) {
                    destroyCropper();
                    var dataUrl = evt.target.result;
                    var tmp = new Image();
                    tmp.onload = function () {
                        var maxSourceDim = 1200;
                        var srcW = tmp.width, srcH = tmp.height;
                        if (srcW > maxSourceDim || srcH > maxSourceDim) {
                            var ratio = Math.min(maxSourceDim / srcW, maxSourceDim / srcH);
                            var cw = Math.round(srcW * ratio);
                            var ch = Math.round(srcH * ratio);
                            var canvas = document.createElement('canvas');
                            canvas.width = cw; canvas.height = ch;
                            var ctx = canvas.getContext('2d');
                            ctx.drawImage(tmp, 0, 0, cw, ch);
                            try { image.src = canvas.toDataURL('image/jpeg', 0.9); } catch (e) { image.src = dataUrl; }
                        } else { image.src = dataUrl; }

                        image.onload = function () {
                            cropper = new Cropper(image, { aspectRatio: 10 / 8, viewMode: 1, autoCropArea: 1 });
                            // show the Save button now that cropper is ready
                            try { document.getElementById('<%= btnSave.ClientID %>').style.display = ''; } catch (e) { }
                            try { document.getElementById('<%= btnUpload.ClientID %>').addEventListener('click', function () { document.getElementById('<%= FileUpload1.ClientID %>').click(); }); } catch (e) { }
                        };
                    };
                    tmp.src = dataUrl;
                };
                reader.readAsDataURL(file);
            });

            btnSave.addEventListener('click', function (e) {
                e.preventDefault();
                if (!cropper) { alert('Please select and crop an image first.'); return; }

                var userID = document.getElementById('<%= hdnUserID.ClientID %>').value;
                var canvasLarge = cropper.getCroppedCanvas({ width: 400, height: 320 });
                var canvasSmall = cropper.getCroppedCanvas({ width: 160, height: 128 });

                function toBlob(canvas, callback) {
                    if (canvas.toBlob) { canvas.toBlob(callback, 'image/jpeg', 0.9); }
                    else {
                        var dataUrl = canvas.toDataURL('image/jpeg', 0.9);
                        var bin = atob(dataUrl.split(',')[1]); var len = bin.length; var arr = new Uint8Array(len);
                        for (var i = 0; i < len; i++) arr[i] = bin.charCodeAt(i);
                        callback(new Blob([arr], { type: 'image/jpeg' }));
                    }
                }

                lblMessage.style.display = 'none'; showSpinner(); btnSave.disabled = true;

                toBlob(canvasLarge, function (blobLarge) {
                    toBlob(canvasSmall, function (blobSmall) {
                        var fd = new FormData(); fd.append('userID', userID); fd.append('image', blobLarge, userID + '.jpg'); fd.append('image_sm', blobSmall, userID + '_sm.jpg');
                        fetch('SaveCroppedImage.ashx', { method: 'POST', body: fd, credentials: 'same-origin' })
                            .then(function (r) { return r.json(); })
                            .then(function (json) { hideSpinner(); btnSave.disabled = false; if (json.success) { lblMessage.style.color = 'green'; lblMessage.innerText = 'Profile picture updated!'; lblMessage.style.display = ''; destroyCropper(); image.src = json.imageUrl + '?t=' + new Date().getTime(); } else { lblMessage.style.color = 'red'; lblMessage.innerText = 'Error: ' + (json.error || 'Unknown'); lblMessage.style.display = ''; } })
                            .catch(function (err) { hideSpinner(); btnSave.disabled = false; lblMessage.style.color = 'red'; lblMessage.innerText = 'Upload failed'; lblMessage.style.display = ''; });
                    });
                });
            });
        };

        if (window.jQuery) { jQuery(init); } else { document.addEventListener('DOMContentLoaded', init); }
    })();
</script>
