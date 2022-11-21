<%@ Control Language="vb" AutoEventWireup="false" CodeBehind="Header.ascx.vb" Inherits="Predictathon.UserControls.Header" %>
<%@ Register Src="~/UserControls/Common/LoginInformation.ascx" TagName="LoginInformation" TagPrefix="uc1" %>

<script type="text/javascript">
    function HeaderClick() {
        __doPostBack('<%=HeaderText.UniqueID %>', '');
    }
</script>

<div class="MainHeader FullWidth">
    <div class="Logo">
        <h1 id="HeaderText" runat="server" class="Clickable" onclick="HeaderClick();">Predictathon</h1>
        <img runat="server" src="~/Images/Branding/Football.png" alt="Football" />
    </div>
    <div class="LoginInformation">
        <span id="LoginInformationHeader" runat="server" class="LoginInformationHeader Clickable" onclick="HeaderClick();">Predictathon</span>
        <uc1:LoginInformation ID="LoginInformation1" runat="server" />
    </div>
</div>