<%@ Page Language="vb" AutoEventWireup="false" CodeBehind="Login.aspx.vb" Inherits="Predictathon.Pages.Login" %>

<%@ Register Src="~/UserControls/Rules/Rules.ascx" TagPrefix="uc1" TagName="Rules" %>
<%@ Register Src="~/UserControls/User/PasswordReset.ascx" TagPrefix="uc1" TagName="PasswordReset" %>

<!DOCTYPE html>

<html lang="en">
<head runat="server">
    <title>Predictathon - Predict Football Scores - Premier League, European Championships, World Cup</title>
    <link id="lnkFavicon1" runat="server" rel="shortcut icon" href="~/images/favicon.ico" type="image/x-icon" />
    <link id="lnkFavicon2" runat="server" rel="icon" href="~/images/favicon.ico" type="image/ico" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <asp:PlaceHolder runat="server">
        <%: System.Web.Optimization.Styles.Render("~/Styles/predictathon") %>
    </asp:PlaceHolder>
</head>

<body class="LoginPage">
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
                $("<div></div>").attr("id", "divOverlay").css({ position: "fixed", top: 0, left: 0, "z-index": 1000, opacity: 0.4, width: "100%", height: "100%", color: "white", "background-color": "black" }).html('').appendTo($('#LoginWrapper'))
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
                $('#<%=txtUsername.ClientID %>').focus();
                $('#<%=divPasswordReset.ClientID %>').hide();
                $('#<%=divLogin.ClientID %>').show();
            }
        </script>

        <asp:HiddenField ID="hdnCompetitionID" runat="server" />

        <div id="LoginWrapper">
            <section id="LoginDetails">
                <div class="LoginBox Blur">
                    <div class="BrandedHeader">
                        <h1>Predictathon</h1>
                        <img src="Images/Branding/Football.png" alt="Football" />
                    </div>
                    <asp:Panel ID="divLogin" runat="server" DefaultButton="btnLogin" class="LoginInput">
                        <h1>Log In</h1>
                        <asp:TextBox ID="txtUsername" runat="server" AutoCompleteType="DisplayName" placeholder="Username or email address" MaxLength="128" /><br />
                        <asp:TextBox ID="txtPassword" runat="server" TextMode="Password" placeholder="Password" /><br />
                        <asp:Label ID="lblError" runat="server" Visible="false" CssClass="Error" Text="Login unsuccessful" />
                        <asp:CheckBox ID="chkRememberMe" runat="server" Text="Remember me" TextAlign="Left" />
                        <p>
                            <asp:Button ID="btnLogIn" runat="server" Text="Log In" />
                        </p>
                        <p>
                            <asp:LinkButton runat="server" Text="Forgotten your password?" OnClientClick="ShowPasswordReset(); return false;" />
                        </p>
                    </asp:Panel>
                    <div id="divPasswordReset" runat="server" style="display: none;" class="LoginInput">
                        <uc1:PasswordReset ID="PasswordReset1" runat="server" />
                        <p>
                            <asp:LinkButton ID="lnkHidePasswordReset" runat="server" Text="Show login options" OnClientClick="HidePasswordReset(); return false;" />
                        </p>
                    </div>
                </div>
            </section>
            <section id="LoginInfo">
                <div class="LoginBox Blur">
                    <h1>New User?</h1>
                    <asp:Label ID="lblOpenForRegistration" runat="server" Text="Select a competition for info and registration" />
                    <asp:Label ID="lblNotOpenForRegistration" runat="server" Text="No competitions are open for registration - please come back before the next major competition begins." />
                    <asp:Repeater ID="rptCompetitions" runat="server">
                        <ItemTemplate>
                            <br />
                            <a href='<%#ResolveClientUrl("~/Pages/User/UserRegistration.aspx?CompetitionID=" & Eval("CompetitionID").ToString()) %>'>
                                <h1>
                                    <asp:Label ID="lblCompetitionName" runat="server" Text='<%#Eval("CompetitionName") %>' />
                                </h1>
                            </a>
                        </ItemTemplate>
                    </asp:Repeater>
                </div>
                <div class="LoginBox Blur" style="cursor: pointer;" onclick="ShowRules();">
                    <h1>What's a Predictathon? (Click for Rules)</h1>
                    You predict the result of every match in a major football competition.
                    <br />
                    Accurate predictions earn points, points which will help you to climb the league table.
                    <br />
                    <p>
                        <span style="font-size: 2em; vertical-align: middle;">
                            <asp:Label ID="lblThree" runat="server" CssClass="ThreePointer" Font-Bold="true" Text="3" />
                        </span>
                        <span style="vertical-align: middle;">points: A perfect prediction</span>
                        <br />
                        <span style="font-size: 2em; vertical-align: middle;">
                            <asp:Label ID="lblTwo" runat="server" CssClass="TwoPointer" Font-Bold="true" Text="2" />
                        </span>
                        <span style="vertical-align: middle;">points: Winner + winning team's goals </span>
                        <br />
                        <span style="font-size: 2em; vertical-align: middle;">
                            <asp:Label ID="lblOne" runat="server" CssClass="OnePointer" Font-Bold="true" Text="1" />
                        </span>
                        <span style="vertical-align: middle;">point: The result, but not the scoreline</span>
                    </p>
                    <p>
                        <h3>Where does my money go?</h3>
                        If there's an entry fee, there's a prize fund.<br />
                        All of that fee, with the exception of any PayPal fees incurred, go into the prize fund.<br />
                        Cash prizes will be shared out among the best predictors at the end of the competition.
                    </p>
                </div>
            </section>
        </div>
        <div id="divRules" runat="server" class="PopupWindow" style="width: 80%">
            <uc1:Rules ID="Rules1" runat="server" ShowTransparency="false" />
        </div>
    </form>
</body>
</html>
