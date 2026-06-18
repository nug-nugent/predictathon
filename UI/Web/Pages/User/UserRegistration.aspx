<%@ Page Language="vb" AutoEventWireup="false" CodeBehind="UserRegistration.aspx.vb" Inherits="Predictathon.Pages.UserRegistration" %>
<!DOCTYPE html>

<html lang="en">
<head runat="server">
    <title>Predictathon - New User Registration</title>
    <link id="lnkFavicon1" runat="server" rel="shortcut icon" href="~/images/favicon.ico" type="image/x-icon" />
    <link id="lnkFavicon2" runat="server" rel="icon" href="~/images/favicon.ico" type="image/ico" />
    <asp:PlaceHolder runat="server">
        <link href='<%= ResolveUrl("~/Styles/Predictathon.css") %>' rel="stylesheet" type="text/css" />
    </asp:PlaceHolder>
</head>
<body>
    <script type="text/javascript">
        function HeaderClick() {
            __doPostBack('<%=tdLogin.UniqueID %>', '');
        }

        function ShowPaymentOption(Provider) { 
            if(Provider == 'PayPal') {
                $('#<%=pPaymentCredit.ClientID %>').hide();
                $('#<%=pPayPal.ClientID %>').show();
            }
            else {
                $('#<%=pPayPal.ClientID %>').hide();
                $('#<%=pPaymentCredit.ClientID %>').show();
            }
        }
    </script>

    <form id="form1" runat="server">
        <asp:ScriptManager ID="ScriptManager1" runat="server">
            <Scripts>
                <asp:ScriptReference Path="~/Scripts/jquery.js" ScriptMode="Release" />
            </Scripts>
        </asp:ScriptManager>
        <asp:HiddenField ID="hdnp" runat="server" />
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
        <div style="width: 100%; text-align: center; margin-top: 30px">
            <div id="divCompetitionDetail" runat="server" style="width: 100%">
                <asp:Label ID="lblCompetitionNameValue" runat="server" /><br />
                <asp:Label ID="lblStartDate" runat="server" Text="Starts: " /><asp:Label ID="lblStartDateValue" runat="server" CssClass="Information" /><br />
                <asp:Label ID="lblEndDate" runat="server" Text="Ends: " /><asp:Label ID="lblEndDateValue" runat="server" CssClass="Information" /><br />
                <asp:Label ID="lblRegistrationStatus" runat="server" CssClass="Yes" Text="Registration open - sign up here!" /><br />
                <span id="spnEntranceFee" runat="server"><asp:Label ID="lblEntranceFee" runat="server" Text="Entrance fee: " /><asp:Label ID="lblEntranceFeeValue" runat="server" CssClass="Information" /><br /></span>
                <p id="pCompetitionInformation" runat="server"><asp:Label ID="lblCompetitionInformation" runat="server" CssClass="Information" /></p>
            </div>
            <div id="divUserDetail" runat="server" style="width: 100%">
                <hr />
                <p><asp:Label ID="lblWarning" runat="server" CssClass="Warning">
                    If you already have a user account, please <a href="~/Login.aspx">log on</a> and sign up via the main menu.
                    <br />Only complete the following details if you are a new user.
                </asp:Label></p>
                <hr />
                <p><asp:Label ID="lblYourDetails" runat="server" CssClass="HeaderText" Text="Your Details" /></p>
                <table id="tblUserDetail" runat="server" width="100%">
                    <tr>
                        <td align="right">
                            <asp:Label ID="lblForenames" runat="server" Text="First name: " />
                        </td>
                        <td align="left">
                            <asp:TextBox ID="txtForenames" runat="server" MaxLength="50" />
                        </td>
                    </tr>
                    <tr>
                        <td align="right">
                            <asp:Label ID="lblSurname" runat="server" Text="Surname: " />
                        </td>
                        <td align="left">
                            <asp:TextBox ID="txtSurname" runat="server" MaxLength="50" />
                        </td>
                    </tr>
                    <tr>
                        <td style="width: 50%" align="right">
                            <asp:Label ID="lblUsername" runat="server" Text="Username: " />
                        </td>
                        <td style="width: 50%" align="left">
                            <asp:TextBox ID="txtUsername" runat="server" MaxLength="50" />
                        </td>
                    </tr>
                    <tr>
                        <td align="right">
                            <asp:Label ID="lblPassword" runat="server" Text="Password: " />
                        </td>
                        <td align="left">
                            <asp:TextBox ID="txtPassword" runat="server" TextMode="Password" MaxLength="25" />
                        </td>
                    </tr>
                    <tr>
                        <td align="right">
                            <asp:Label ID="lblConfirmPassword" runat="server" Text="Confirm password: " />
                        </td>
                        <td align="left">
                            <asp:TextBox ID="txtConfirmPassword" runat="server" TextMode="Password" MaxLength="25" />
                        </td>
                    </tr>
                    <tr>
                        <td align="right">
                            <asp:Label ID="lblEmailAddress" runat="server" Text="Email address: " />
                        </td>
                        <td align="left">
                            <asp:TextBox ID="txtEmailAddress" runat="server" MaxLength="128" Width="18em" />
                        </td>
                    </tr>
                    <tr style="height: 10px">
                        <td colspan="2">
                            &nbsp;
                        </td>
                    </tr>
                    <tr>
                        <td align="center" colspan="2">
                            <asp:Button ID="btnSubmitUserDetails" runat="server" Text="Submit " />
                        </td>
                    </tr>
                </table>
            </div>
            <div id="divPayment" runat="server" class="InformationBlock" style="margin-top: 10px; padding: 15px;" visible="false">
                <asp:Label ID="lblPaymentRequest" runat="server" Font-Bold="true" /><br />
                <p>
                    <asp:Label ID="lblPaymentRequest2" runat="server" Font-Bold="true" />
                </p>
                <p id="pPaymentOptions" runat="server">
                    <asp:RadioButton ID="rbPaymentCredit" runat="server" Text="Pay by authorisation code" GroupName="rbPaymentType" Checked="true" />
                    <asp:RadioButton ID="rbPayPal" runat="server" Text="Pay by PayPal" GroupName="rbPaymentType" />
                </p>
                <p id="pPaymentCredit" runat="server">
                    <asp:Label ID="lblPaymentCreditCode" runat="server" Text="Authorisation code: " /><asp:TextBox ID="txtPaymentCreditCode" runat="server" MaxLength="10" />
                    <asp:Button ID="btnSubmitPaymentCreditCode" runat="server" Text="Submit" />
                </p>
                <p id="pPayPal" runat="server" style="display: none">
                    <asp:ImageButton ID="btnPayPal" runat="server" ImageUrl="https://www.paypalobjects.com/en_GB/i/btn/btn_paynow_LG.gif" AlternateText="PayPal — The safer, easier way to pay online." />
                </p>
            </div>
            <div id="divRegistrationConfirmed" runat="server" visible="false" style="width: 100%; text-align: center; margin-top: 30px;">
                Thanks for registering.
                <br />
                We've sent you an email to confirm your details.
                <br />
                <br />
                <strong>What happens next?</strong>
                <br />
                <ul>
                    <li>Enter your first predictions via the 'Predictions' menu.</li>
                    <li>Update your profile, add an image, and set email reminders via the 'edit profile' menu option.</li>
                    <li>Keep up to date with your progress via the league table, which is updated every match day.</li>
                    <li id="liKeepPredicting" runat="server"></li>
                </ul>
                <p><asp:LinkButton ID="btnLogin" runat="server">Click here to proceed to the main menu.</asp:LinkButton></p>
            </div>
        </div>
    </form>
</body>
</html>