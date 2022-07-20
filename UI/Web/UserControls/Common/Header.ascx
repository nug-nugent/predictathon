<%@ Control Language="vb" AutoEventWireup="false" CodeBehind="Header.ascx.vb" Inherits="Predictathon.UserControls.Header" %>
<%@ Register Src="~/UserControls/Common/LoginInformation.ascx" TagName="LoginInformation" TagPrefix="uc1" %>

<script type="text/javascript">
    function HeaderClick() {
        __doPostBack('<%=tdMainMenu.UniqueID %>', '');
    }
</script>

<table style="width: 100%; border: 0;" border="0" cellpadding="0" cellspacing="0">
    <tr>
        <td class="HeaderRepeat HeaderStart">
            &nbsp;
        </td>
        <td id="tdMainMenu" runat="server" class="Header HeaderHome" onclick="HeaderClick();">
            &nbsp;
        </td>
        <td class="HeaderRepeat" style="padding-right: 20px">
            <span class="Clickable" onclick="HeaderClick();">Predictathon</span><br />
            <uc1:LoginInformation ID="LoginInformation1" runat="server" />   
        </td>
    </tr>
</table>