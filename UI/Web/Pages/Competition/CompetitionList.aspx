<%@ Page Language="vb" AutoEventWireup="false" MasterPageFile="~/Pages/Master/Main.Master" CodeBehind="CompetitionList.aspx.vb" Inherits="Predictathon.Pages.Competition.CompetitionList" %>
<%@ MasterType TypeName="Predictathon.Master.Main" %>

<asp:Content runat="server" ContentPlaceHolderID="MainContent">
    <table id="tblAddNew" runat="server">
        <tr>
            <td>
                <asp:Label ID="lblAdd" runat="server" CssClass="GridTitle" Text="Add Competition" />
            </td>
        </tr>
        <tr>
            <td>
                <asp:Label ID="lblCompetitionName" runat="server" Text="Competition name: " />
                <asp:TextBox ID="txtCompetitionName" runat="server" Width="30em" MaxLength="50" />
                <asp:ImageButton ID="btnAdd" runat="server" ImageUrl="~/Images/Common/Add.gif" ImageAlign="Top" AlternateText="Add" />
            </td>
        </tr>
        <tr id="trError" runat="server">
            <td>
                <asp:Label ID="lblError" runat="server" CssClass="Error" />
            </td>
        </tr>
    </table>

    <nug:GridView ID="gvCompetition" runat="server" AutoGenerateColumns="false" GridLines="None" AllowPaging="true" DataKeyNames="CompetitionID"
        CssClass="GridView" RowStyle-CssClass="GridViewRow" AlternatingRowStyle-CssClass="GridViewRowAlt" RowHoverCssClass="GridViewRowHover" EnableRowClick="true">
        <Columns>
            <asp:BoundField HeaderText="Competition" DataField="CompetitionName" />
            <asp:BoundField HeaderText="Start date" DataField="StartDate" DataFormatString="{0:d}" />
            <asp:BoundField HeaderText="End date" DataField="EndDate" DataFormatString="{0:d}" />
            <asp:BoundField HeaderText="Entrance fee" DataField="EntranceFee" DataFormatString="{0:C2}" />
        </Columns>
    </nug:GridView>
</asp:Content>