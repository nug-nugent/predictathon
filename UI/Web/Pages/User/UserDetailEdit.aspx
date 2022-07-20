<%@ Page Language="vb" AutoEventWireup="false" MasterPageFile="~/Pages/Master/Main.Master" CodeBehind="UserDetailEdit.aspx.vb" Inherits="Predictathon.Pages.User.UserDetailEdit" %>
<%@ MasterType TypeName="Predictathon.Master.Main" %>

<asp:Content runat="server" ContentPlaceHolderID="MainContent">
    <script type="text/javascript" src="../../Scripts/jquery.auto-height.js"></script>
    <script type="text/javascript">
        function ChangePassword(value) {
            if (value) {
                $('#<%=trPassword.ClientID %>').show();
                $('#<%=trPassword2.ClientID %>').show();
            }
            else {
                $('#<%=trPassword.ClientID %>').hide();
                $('#<%=trPassword2.ClientID %>').hide();
            }
        }

        function ShowHideUserDetail(value) {
            if (value) {
                $('#<%=tblUserDetail.ClientID %>').show();
                $('#<%=ifImageUpload.ClientID %>').hide();
            }
            else {
                $('#<%=tblUserDetail.ClientID %>').hide();
                $('#<%=ifImageUpload.ClientID %>').show();
                $('#<%=ifImageUpload.ClientID %>').height(420);
            }
        }
        
        //resize our iframe to fit its content
        $(document).ready(function () {
            $('#<%=ifImageUpload.ClientID %>').iframeAutoHeight({ heightOffset: 20 });
        });
    </script>

    <div class="InputBlock NoHover">
        <div class="TitleBar">
            <asp:RadioButton ID="rbUserDetail" runat="server" Text="User Detail" GroupName="User" /><asp:RadioButton ID="rbProfilePicture" runat="server" Text="Profile Picture" GroupName="User" />
        </div>
        <table id="tblUserDetail" runat="server" width="100%">
            <tr>
                <td style="width: 20%">
                    <asp:Label ID="lblUsername" runat="server" Text="Username: " />
                </td>
                <td style="width: 80%">
                    <asp:TextBox ID="txtUsername" runat="server" MaxLength="50" />
                </td>
            </tr>
            <tr>
                <td>
                    <asp:Label ID="lblForenames" runat="server" Text="First name: " />
                </td>
                <td>
                    <asp:TextBox ID="txtForenames" runat="server" MaxLength="50" />
                </td>
            </tr>
            <tr>
                <td>
                    <asp:Label ID="lblSurname" runat="server" Text="Surname: " />
                </td>
                <td>
                    <asp:TextBox ID="txtSurname" runat="server" MaxLength="50" />
                </td>
            </tr>
            <tr>
                <td>
                    <asp:Label ID="lblEmailAddress" runat="server" Text="Email address: " />
                </td>
                <td>
                    <asp:TextBox ID="txtEmailAddress" runat="server" MaxLength="128" Width="18em" />
                </td>
            </tr>
            <tr>
                <td>
                    <asp:Label ID="lblEmailPredictionReminderDays" runat="server" Text="Email prediction reminders: " />
                </td>
                <td>
                    <asp:TextBox ID="txtEmailPredictionReminderDays" runat="server" Width="2em" /><asp:Label ID="lblEmailPredictionReminderDaysHelp" runat="server" Text=" (days before match - leave blank for no email)" />
                </td>
            </tr>
            <tr>
                <td>
                    <asp:Label ID="lblChangePassword" runat="server" Text="Change password: " />
                </td>
                <td>
                    <asp:CheckBox ID="chkChangePassword" runat="server" />
                </td>
            </tr>
            <tr id="trPassword" runat="server">
                <td>
                    <asp:Label ID="lblPassword" runat="server" Text="Password: " />
                </td>
                <td>
                    <asp:TextBox ID="txtPassword" runat="server" TextMode="Password" MaxLength="25" />
                </td>
            </tr>
            <tr id="trPassword2" runat="server">
                <td>
                    <asp:Label ID="lblConfirmPassword" runat="server" Text="Confirm password: " />
                </td>
                <td>
                    <asp:TextBox ID="txtConfirmPassword" runat="server" TextMode="Password" MaxLength="25" />
                </td>
            </tr>
            <tr>
                <td>
                    <asp:Label ID="lblFavouriteTeam" runat="server" Text="Favourite team: " />
                </td>
                <td>
                    <asp:TextBox ID="txtFavouriteTeam" runat="server" MaxLength="50" />
                </td>
            </tr>
            <tr>
                <td>
                    <asp:Label ID="lblLocation" runat="server" Text="Location: " />
                </td>
                <td>
                    <asp:TextBox ID="txtLocation" runat="server" MaxLength="50" />
                </td>
            </tr>
            <tr>
                <td>
                    <asp:Label ID="lblCaption" runat="server" Text="Caption: " />
                </td>
                <td>
                    <asp:TextBox ID="txtCaption" runat="server" MaxLength="30" />
                </td>
            </tr>
            <tr>
                <td>
                    <asp:Label ID="lblProfileText" runat="server" Text="Profile: " />
                </td>
                <td>
                    <asp:TextBox ID="txtProfileText" runat="server" TextMode="MultiLine" Rows="5" Width="99%" />
                </td>
            </tr>
            <tr id="trCanViewMessageboard" runat="server">
                <td>
                    <asp:Label ID="lblCanViewMessageboard" runat="server" Text="Can view messageboard: " AssociatedControlID="chkCanViewMessageboard" />
                </td>
                <td>
                    <asp:CheckBox ID="chkCanViewMessageboard" runat="server" />
                </td>
            </tr>
            <tr id="trCanViewHiddenMessageThreads" runat="server">
                <td>
                    <asp:Label ID="lblCanViewHiddenMessageThreads" runat="server" Text="Can view hidden message threads: " AssociatedControlID="chkCanViewHiddenMessageThreads" />
                </td>
                <td>
                    <asp:CheckBox ID="chkCanViewHiddenMessageThreads" runat="server" />
                </td>
            </tr>
            <tr>
                <td align="center" colspan="2">
                    <asp:Button ID="btnSubmitUserDetails" runat="server" Text="Submit " />
                    <div id="divSaveConfirmed" runat="server" style="margin-top: 10px" visible="false">
                        <asp:Label ID="lblSaved" runat="server" CssClass="Yes" Text="Your changes have been saved." />
                    </div>
                </td>
            </tr>
        </table>
        <iframe runat="server" id="ifImageUpload" width="420px" height="420px" class="auto-height" scrolling="no"></iframe>
    </div>
</asp:Content>
