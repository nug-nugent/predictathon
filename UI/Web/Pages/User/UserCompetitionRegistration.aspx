<%@ Page Language="vb" AutoEventWireup="false" MasterPageFile="~/Pages/Master/Main.Master" CodeBehind="UserCompetitionRegistration.aspx.vb" Inherits="Predictathon.Pages.User.UserCompetitionRegistration" %>
<%@ MasterType TypeName="Predictathon.Master.Main" %>

<asp:Content ID="Content1" runat="server" ContentPlaceHolderID="MainContent">
    <script type="text/javascript">
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
    <asp:HiddenField ID="hdnp" runat="server" />
    <div style="width: 100%; text-align: center; margin-top: 10px">
        <asp:Label ID="lblCompetitionNameValue" runat="server" /><br />
        <asp:Label ID="lblStartDate" runat="server" Text="Starts: " /><asp:Label ID="lblStartDateValue" runat="server" CssClass="Information" /><br />
        <asp:Label ID="lblEndDate" runat="server" Text="Ends: " /><asp:Label ID="lblEndDateValue" runat="server" CssClass="Information" /><br />
        <asp:Label ID="lblRegistrationStatus" runat="server" CssClass="Yes" Text="Registration open - sign up here!" /><br />
        <span id="spnEntranceFee" runat="server"><asp:Label ID="lblEntranceFee" runat="server" Text="Entrance fee: " /><asp:Label ID="lblEntranceFeeValue" runat="server" CssClass="Information" /><br /></span>
        <p id="pCompetitionInformation" runat="server"><asp:Label ID="lblCompetitionInformation" runat="server" CssClass="Information" /></p>
        <div id="divEnterCompetition" runat="server" style="width: 100%; text-align: center; margin-top: 15px">
            <hr />
            <asp:Button ID="btnEnterCompetition" runat="server" Text="Enter Competition" />
        </div>
        <hr />
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
            Thanks for registering for the competition.
            <br />
            <br />
            <strong>What happens next?</strong>
            <br />
            <ul>
                <li>The new competition will be available on your main menu.</li>
                <li>Switch between competitions to predict results, view your league placing etc.</li>
                <li id="liKeepPredicting" runat="server"></li>
            </ul>
            <p><asp:LinkButton ID="btnLogin" runat="server">Click here to proceed to the main menu.</asp:LinkButton></p>
        </div>
    </div>
</asp:Content>