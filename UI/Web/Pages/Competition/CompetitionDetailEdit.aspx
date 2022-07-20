<%@ Page Language="vb" AutoEventWireup="false" MasterPageFile="~/Pages/Master/Main.Master" CodeBehind="CompetitionDetailEdit.aspx.vb" Inherits="Predictathon.Pages.Competition.CompetitionDetailEdit" ValidateRequest="false" %>
<%@ MasterType TypeName="Predictathon.Master.Main" %>

<asp:Content runat="server" ContentPlaceHolderID="MainContent">
    <div class="InputBlock NoHover">
        <div class="TitleBar">
            Competition
        </div>
        <table id="tblCompetitionDetail" runat="server" width="100%">
            <tr>
                <td style="width: 30%">
                    <asp:Label ID="lblCompetitionName" runat="server" Text="Competition name: " />
                </td>
                <td style="width: 60%">
                    <asp:TextBox ID="txtCompetitionName" runat="server" Width="30em" MaxLength="50" />
                </td>
                <td>
                    <asp:Button ID="btnChange" runat="server" Text="Change to this competition" />                
                </td>
            </tr>
            <tr>
                <td>
                    <asp:Label ID="lblPrependNameWithThe" runat="server" Text="Prepend name with 'the' where needed: " />
                </td>
                <td>
                    <asp:CheckBox ID="chkPrependNameWithThe" runat="server" />
                </td>
                <td>
                    <asp:Button ID="btnShowTeams" runat="server" Text="Show teams" />                
                </td>
            </tr>
            <tr>
                <td>
                    <asp:Label ID="lblStartDate" runat="server" Text="Start date: " />
                </td>
                <td colspan="2">
                    <nug:DatePicker ID="dteStartDate" runat="server" ValidateDateAsMandatory="true" />                
                </td>
            </tr>
            <tr>
                <td>
                    <asp:Label ID="lblEndDate" runat="server" Text="End date: " />
                </td>
                <td colspan="2">
                    <nug:DatePicker ID="dteEndDate" runat="server" ValidateDateAsMandatory="true" />                
                </td>
            </tr>
            <tr>
                <td>
                    <asp:Label ID="lblDuplicateFixturesAllowed" runat="server" Text="Duplicate fixtures allowed: " />
                </td>
                <td colspan="2">
                    <asp:CheckBox ID="chkDuplicateFixturesAllowed" runat="server" />
                </td>
            </tr>
            <tr>
                <td>
                    <asp:Label ID="lblDefaultToNeutralGround" runat="server" Text="Default matches to neutral ground: " />
                </td>
                <td colspan="2">
                    <asp:CheckBox ID="chkDefaultToNeutralGround" runat="server" />
                </td>
            </tr>
            <tr>
                <td>
                    <asp:Label ID="lblAllowTwoPointers" runat="server" Text="Allow 2-pointers: " />
                </td>
                <td colspan="2">
                    <asp:CheckBox ID="chkAllowTwoPointers" runat="server" />
                </td>
            </tr>
            <tr>
                <td>
                    <asp:Label ID="lblOpenForRegistration" runat="server" Text="Open for registration: " />
                </td>
                <td colspan="2">
                    <asp:CheckBox ID="chkOpenForRegistration" runat="server" />
                </td>
            </tr>
            <tr>
                <td>
                    <asp:Label ID="lblRegistrationAvailableOnLoginPage" runat="server" Text="Registration available on login page: " />
                </td>
                <td colspan="2">
                    <asp:CheckBox ID="chkRegistrationAvailableOnLoginPage" runat="server" />
                </td>
            </tr>
            <tr>
                <td>
                    <asp:Label ID="lblShowInHallOfFame" runat="server" Text="Show in Hall of Fame: " />
                </td>
                <td colspan="2">
                    <asp:CheckBox ID="chkShowInHallOfFame" runat="server" />
                </td>
            </tr>
            <tr>
                <td>
                    <asp:Label ID="lblEntranceFee" runat="server" Text="Entrance fee: " />
                </td>
                <td colspan="2">
                    <asp:TextBox ID="txtEntranceFee" runat="server" Width="3.5em" MaxLength="6" />
                </td>
            </tr>
            <tr>
                <td>
                    <asp:Label ID="lblPayPalPaymentAvailable" runat="server" Text="Show PayPal payment option: " />
                </td>
                <td colspan="2">
                    <asp:CheckBox ID="chkPayPalPaymentAvailable" runat="server" />
                </td>
            </tr>
            <tr>
                <td>
                    <asp:Label ID="lblImageFilename" runat="server" Text="Image filename: " />
                </td>
                <td colspan="2">
                    <asp:TextBox ID="txtImageFilename" runat="server" MaxLength="40" Width="28em" />
                </td>
            </tr>
            <tr>
                <td>
                    <asp:Label ID="lblInformation" runat="server" Text="Information: " />
                </td>
                <td colspan="2">
                    <asp:TextBox ID="txtInformation" runat="server" TextMode="MultiLine" Rows="5" Width="99%" />
                </td>
            </tr>
            <tr>
                <td align="center" colspan="3">
                    <asp:Button ID="btnSubmitDetails" runat="server" Text="Submit" />
                    <div id="divSaveConfirmed" runat="server" style="margin-top: 10px" visible="false">
                        <asp:Label ID="lblSaved" runat="server" CssClass="Yes" Text="Your changes have been saved." />
                    </div>
                </td>
            </tr>
        </table>
    </div>
</asp:Content>
