<%@ Page Language="vb" AutoEventWireup="false" CodeBehind="Error.aspx.vb" Inherits="Predictathon.Pages.Security.ErrorPage" %>
<!DOCTYPE html>

<html lang="en">
<head runat="server">
    <title>Predictathon - Error</title>
    <link id="lnkFavicon1" runat="server" rel="shortcut icon" href="~/images/favicon.ico" type="image/x-icon" />
    <link id="lnkFavicon2" runat="server" rel="icon" href="~/images/favicon.ico" type="image/ico" />
</head>
<body>
    <form id="form1" runat="server">
        <table style="width: 100%; border: 0;" border="0" cellpadding="0" cellspacing="0">
            <tr>
                <td class="HeaderRepeat">
                    &nbsp;                
                </td>
                <td class="Header" style="cursor: default">
                    &nbsp;               
                </td>
                <td class="HeaderRepeat">
                    &nbsp;
                </td>
            </tr>
        </table>
        <div style="width: 100%; text-align: center; margin-top: 30px">
            Sorry, an unexpected error has occurred.
            <p><asp:HyperLink ID="hypMainMenu" runat="server" NavigateUrl="~/Pages/Common/MainMenu.aspx">Click here to return to the main menu.</asp:HyperLink></p>
        </div>
        <div style="width: 100%; text-align: center; margin-top: 10px">
            <asp:Image ID="imgError" runat="server" ImageUrl="~/Images/Common/Error.gif" />
        </div>
    </form>
</body>
</html>