<%@ Page Language="vb" AutoEventWireup="false" CodeBehind="PaymentConfirmation.aspx.vb" Inherits="Predictathon.Pages.Payment.PaymentConfirmation" %>
<!DOCTYPE html>

<html lang="en">
<head id="Head1" runat="server">
    <title>Predictathon - Payment Confirmation</title>
    <link id="lnkFavicon1" runat="server" rel="shortcut icon" href="~/images/favicon.ico" type="image/x-icon" />
    <link id="lnkFavicon2" runat="server" rel="icon" href="~/images/favicon.ico" type="image/ico" />
    <asp:PlaceHolder runat="server">
        <%: System.Web.Optimization.Styles.Render("~/Styles/predictathon") %>
    </asp:PlaceHolder>
</head>
<body>
    <script type="text/javascript">
        function HeaderClick() {
            __doPostBack('<%=tdLogin.UniqueID %>', '');
        }
    </script>
    
    <form id="form1" runat="server">
        <asp:ScriptManager ID="ScriptManager1" runat="server">
            <Scripts>
                <asp:ScriptReference Path="~/Scripts/jquery.js" ScriptMode="Release" />
            </Scripts>
        </asp:ScriptManager>
        <table style="width: 100%; border: 0;" border="0" cellpadding="0" cellspacing="0">
            <tr>
                <td class="HeaderRepeat">
                    &nbsp;                
                </td>
                <td id="tdLogin" runat="server" class="Header" onclick="HeaderClick();">
                    &nbsp;               
                </td>
                <td class="HeaderRepeat">
                    &nbsp;
                </td>
            </tr>
        </table>
        <div id="divPaymentSuccessfulNewUser" runat="server" visible="false" style="width: 100%; text-align: center; margin-top: 30px;">
            Thanks for your payment.
            <br />
            We've sent you an email to confirm your registration.
            <br />
            <br />
            <strong>What happens next?</strong>
            <br />
            <ul>
                <li>Log on to the site and enter your first predictions via the 'Predictions' menu.</li>
                <li>Update your profile, add an image, and set email reminders via the 'edit profile' menu option.</li>
                <li>Keep up to date with your progress via the league table, which is updated every match day.</li>
                <li id="liKeepPredicting" runat="server"></li>
            </ul>
            <p><asp:LinkButton ID="btnLogin" runat="server">Click here to proceed to the main menu.</asp:LinkButton></p>
        </div>
        <div id="divPaymentSuccessfulExistingUser" runat="server" visible="false" style="width: 100%; text-align: center; margin-top: 30px;">
            Thanks for registering for the competition.
            <br />
            <br />
            <strong>What happens next?</strong>
            <br />
            <ul>
                <li>The new competition will be available on your main menu.</li>
                <li>Switch between competitions to predict results, view your league placing etc.</li>
                <li id="liKeepPredicting2" runat="server"></li>
            </ul>
            <p><asp:LinkButton ID="btnLogin2" runat="server">Click here to proceed to the main menu.</asp:LinkButton></p>
        </div>
        <div id="divPaymentFailed" runat="server" visible="false" style="width: 100%; text-align: center; margin-top: 30px;">
            Sorry, your payment was unsuccessful.<br />
        </div>
    </form>
</body>
</html>