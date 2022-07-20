<%@ Page Language="vb" AutoEventWireup="false" CodeBehind="Login.aspx.vb" Inherits="Predictathon.Pages.Login" %>
<%@ Register Src="~/UserControls/Rules/Rules.ascx" TagPrefix="uc1" TagName="Rules" %>
<%@ Register Src="~/UserControls/User/PasswordReset.ascx" TagPrefix="uc1" TagName="PasswordReset" %>
<%@ Register Src="~/UserControls/Twitter/TwitterFeed.ascx" TagName="TwitterFeed" TagPrefix="uc1" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Predictathon - Predict Football Scores - Premier League, European Championships, World Cup</title>
    <link id="lnkFavicon1" runat="server" rel="shortcut icon" href="~/images/favicon.ico" type="image/x-icon" />
    <link id="lnkFavicon2" runat="server" rel="icon" href="~/images/favicon.ico" type="image/ico" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
</head>
<body>
    <form id="form1" runat="server">
        <asp:ScriptManager ID="ScriptManager1" runat="server">
            <Scripts>
                <asp:ScriptReference Path="~/Scripts/jquery.js" ScriptMode="Release" />
            </Scripts>
        </asp:ScriptManager>

        <script type="text/javascript">
            jQuery.fn.center = function () {
                this.css("position", "absolute");
                this.css("top", (($(window).height() - this.outerHeight()) / 2) + $(window).scrollTop() + "px");
                this.css("left", (($(window).width() - this.outerWidth()) / 2) + $(window).scrollLeft() + "px");
                return this;
            }

            function ShowRules() {
                $('#<%=divRules.ClientID %>').center();
                $('#<%=divRules.ClientID %>').show();
                $('#<%=divRules.ClientID %>').append('<div id="divHideRules" onclick="HideRules();" class="Close"></div>');
                $("<div></div>").attr("id", "divOverlay").css({ position: "fixed", top: 0, left: 0, "z-index": 1000, opacity: 0.4, width: "100%", height: "100%", color: "white", "background-color": "black" }).html('').appendTo($('#<%=divContent.ClientID %>'))
            }

            function HideRules() {
                $('#<%=divRules.ClientID %>').hide(); 
                $("#divOverlay").remove();
                $("#divHideRules").remove();
            }

            function ShowPasswordReset() {
                $('#<%=divPasswordReset.ClientID %>').show();
                $('#<%=divLogin.ClientID %>').hide();
            }

            function HidePasswordReset() {
                $('#<%=divPasswordReset.ClientID %>').hide();
                $('#<%=divLogin.ClientID %>').show();
            }
        </script>

        <asp:HiddenField ID="hdnCompetitionID" runat="server" />

        <table style="width: 100%; border: 0;" border="0" cellpadding="0" cellspacing="0">
            <tr>
                <td class="HeaderRepeat">
                    &nbsp;
                </td>
                <td class="Header HeaderLogin">
                    &nbsp;
                </td>
                <td class="HeaderRepeat">
                    &nbsp;
                </td>
            </tr>
        </table>

        <div id="divContent" runat="server" class="ContentPane" style="padding: 5px">
            <div class="LeftPane" style="text-align:center"">
                <asp:Panel ID="pnlLogin" runat="server" DefaultButton="btnLogin">
                    <div id="divLogin" runat="server" class="InformationBlock">
                        <div class="TitleBar" style="margin-bottom: 10px;">
                            <asp:Label ID="lblLogin" runat="server" Text="Existing Users - Log In" />
                        </div>
                        <asp:Label ID="lblUsername" runat="server" AssociatedControlID="txtUsername" Text="Username:" />
                        <asp:TextBox ID="txtUsername" runat="server" AutoCompleteType="DisplayName" /><asp:Label ID="lblUsernameError" runat="server" Visible="false" Style="font-weight:bold; color:Red" Text=" * " /><br />
                        <asp:Label ID="lblPassword" runat="server" AssociatedControlID="txtPassword" Text="Password:" />
                        <asp:TextBox ID="txtPassword" runat="server" TextMode="Password" /><asp:Label ID="lblPasswordError" runat="server" Visible="false" Style="font-weight:bold; color:Red" Text=" * " /><br />
                        <asp:Label ID="lblError" runat="server" Visible="false" CssClass="Error" Text="Login unsuccessful" />
                        <asp:Label ID="lblRememberMe" runat="server" AssociatedControlID="chkRememberMe" Text="Remember me:" />
                        <asp:CheckBox ID="chkRememberMe" runat="server" />
                        <p><asp:Button ID="btnLogIn" runat="server" Text="Log In" /></p>
                        <p><asp:LinkButton runat="server" Text="Forgotten your password?" OnClientClick="ShowPasswordReset(); return false;" /></p>
                    </div>
                </asp:Panel>
                <div id="divPasswordReset" runat="server" style="display: none;" class="InformationBlock">
                    <uc1:PasswordReset ID="PasswordReset1" runat="server" />
                    <p><asp:LinkButton ID="lnkHidePasswordReset" runat="server" Text="Show login options" OnClientClick="HidePasswordReset(); return false;" /></p>
                </div>
                <div style="margin-top: 5px; padding-top: 5px; padding-bottom: 5px; cursor: pointer; cursor: hand" class="InformationBlock" onclick="ShowRules();">
                    <div class="TitleBar" style="margin-bottom: 10px;">
                        <asp:Label ID="lblTheRules" runat="server" Text="Predictathon - What Is It?" />
                    </div>
                    <table width="100%">
                        <tr align="center">
                            <td colspan="3">
                                <strong><em>How does it work? (Click for more)</em></strong>
                            </td>
                        </tr>
                        <tr align="center">
                            <td colspan="3">
                                You predict the result of every match in a major football competition.
                                <br />Accurate predictions earn points, points which will help you to climb the league table.
                            </td>
                        </tr>
                        <tr style="height: 10px">
                            <td colspan="3">
                                &nbsp;
                            </td>
                        </tr>
                        <tr>
                            <td colspan="3">
                                <div style="background:#CCD;">
                                    <span style="font-size:26pt; vertical-align:middle;">
                                        <asp:Label ID="lblThree" runat="server" CssClass="ThreePointer" Font-Bold="true" Text="3" />
                                    </span>
                                    <span style="vertical-align:middle;">points: A perfect prediction</span>
                                </div> 
                            </td>
                        </tr>
                        <tr>
                            <td colspan="3">
                                <div style="background:#CCD;">
                                    <span style="font-size:26pt; vertical-align:middle;">
                                        <asp:Label ID="Label1" runat="server" CssClass="TwoPointer" Font-Bold="true" Text="2" />
                                    </span>
                                    <span style="vertical-align:middle;">points: Winner + winning team's goals </span>
                                </div> 
                            </td>
                        </tr>
                        <tr>
                            <td colspan="3">
                                <div style="background:#CCD;">
                                    <span style="font-size:26pt; vertical-align:middle;">
                                        <asp:Label ID="Label2" runat="server" CssClass="OnePointer" Font-Bold="true" Text="1" />
                                    </span>
                                    <span style="vertical-align:middle;">point: The result, but not the scoreline</span>
                                </div> 
                            </td>
                        </tr>
                        <tr style="height: 10px">
                            <td colspan="3">
                                &nbsp;
                            </td>
                        </tr>
                        <tr>
                            <td colspan="3">
                                <strong><em>Where does my money go?</em></strong>
                            </td>
                        </tr>
                        <tr>
                            <td colspan="3">
                                If there's an entry fee, there's a prize fund.<br />
                                All of that fee, with the exception of any PayPal fees incurred, go into the prize fund.<br />
                                Cash prizes will be shared out among the best predictors at the end of the competition.
                            </td>
                        </tr>
                    </table>
                </div>
            </div>
            <div class="RightPane" style="text-align:center; vertical-align: top">
                <uc1:TwitterFeed ID="TwitterFeed1" runat="server" TweetsToDisplay="3" TwitterProfileName="Predictathon" />
                <asp:Label ID="lblOpenForRegistration" runat="server" CssClass="GridTitle" Text="Current competitions - click for details" />
                <nug:GridView ID="gvCompetitions" runat="server" AutoGenerateColumns="false" GridLines="None" AllowPaging="false" DataKeyNames="CompetitionID"
                    CssClass="" RowStyle-CssClass="" AlternatingRowStyle-CssClass="" EnableRowClick="true" RowClickRowCommand="ViewRecord"
                    ShowHeader="false" EmptyDataText="Registration is currently closed. Come back soon!" BorderStyle="None">
                    <Columns>
                        <asp:TemplateField>
                            <ItemTemplate>
                                <div id="divCompetition" runat="server" class="InformationBlock" style="cursor: pointer; cursor: hand;">
                                    <div class="TitleBar">
                                        <asp:Label ID="lblCompetitionName" runat="server" Text='<%#Eval("CompetitionName") %>' />
                                    </div>
                                    <div id="divImage" runat="server" style="min-height: 230px; text-align: center; float: right">
                                        <asp:Image ID="imgCompetition" runat="server" Height="180px" ImageUrl='<%# ImageURL(Eval("ImageFilename").ToString) %>' AlternateText="Trophy" />
                                    </div>
                                    <div style="text-align: center">
                                        <p style="margin-top: 5px" id="pCompetitionInformation" runat="server"><asp:Label ID="lblCompetitionInformation" runat="server" CssClass="Information" Text='<%#Eval("Information") %>' /></p>
                                        <asp:Label ID="lblStartDate" runat="server" Text="Starts: " /><asp:Label ID="lblStartDateValue" runat="server" CssClass="Information" Text='<%#Eval("StartDate", "{0:dd/MM/yyyy}") %>' /><br />
                                        <asp:Label ID="lblEndDate" runat="server" Text="Ends: " /><asp:Label ID="lblEndDateValue" runat="server" CssClass="Information" Text='<%#Eval("EndDate", "{0:dd/MM/yyyy}") %>' /><br />
                                        <asp:Label ID="lblEntranceFee" runat="server" Text="Entrance fee: " /><asp:Label ID="lblEntranceFeeValue" runat="server" CssClass="Information" Text='<%#Eval("EntranceFee", "{0:C2}") %>' /><br />
                                        <asp:Label ID="lblRegistrationStatus" runat="server" />
                                    </div>
                                </div>
                                <asp:HiddenField ID="hdnCompetitionID" runat="server" Value='<%#Eval("CompetitionID") %>' />
                            </ItemTemplate>
                        </asp:TemplateField>
                    </Columns>
                </nug:GridView>
                <asp:HiddenField ID="hdnCurrentServerDateTime" runat="server" />
            </div>
        </div>
        <div id="divRules" runat="server" class="PopupWindow" style="width: 80%">
            <uc1:Rules ID="Rules1" runat="server" ShowTransparency="false" />
        </div>
    </form>
</body>
</html>