<%@ Page Language="vb" AutoEventWireup="false" MasterPageFile="~/Pages/Master/Main.Master" CodeBehind="LeagueTable.aspx.vb" Inherits="Predictathon.Pages.League.LeagueTable" %>
<%@ MasterType TypeName="Predictathon.Master.Main" %>

<asp:Content runat="server" ContentPlaceHolderID="MainContent">

    <script type="text/javascript">
        function ToggleDateSelectors() {
            if ($('#<%=spnMonth.ClientID %>').is(":visible")) {
                $('#<%=spnMonth.ClientID %>').hide();
                $('#<%=spnFromTo.ClientID %>').show();
                $('#<%=ddlMonth.ClientID %>').val('0');
                $('#<%=hdnSearchMode.ClientID %>').val('FromTo');
            }
            else {
                $('#<%=spnFromTo.ClientID %>').hide();
                $('#<%=spnMonth.ClientID %>').show();
                $('#<%=dteFrom.ClientID %>').val('');
                $('#<%=dteTo.ClientID %>').val('');
                $('#<%=hdnSearchMode.ClientID %>').val('Month');
            }
        }
    </script>
    <asp:HiddenField ID="hdnSearchMode" runat="server" Value="Month" />

    <div id="divDateRange" runat="server" style="width: 100%; text-align: center; margin-bottom: 5px;">
        <span id="spnMonth" runat="server" style="display: inline">
            <asp:Label ID="lblMonth" runat="server" Text="Month: " /><asp:DropDownList ID="ddlMonth" runat="server" AutoPostBack="true" />
            <asp:LinkButton ID="btnToggleMonth" runat="server" Text=" (more options)" OnClientClick="ToggleDateSelectors(); return false;" />
        </span>
        <span id="spnFromTo" runat="server" style="display: none">
            <asp:ImageButton ID="btnPreviousMonth" runat="server" ImageUrl="~/Images/Common/Previous2.gif" AlternateText="Previous month" style="vertical-align: middle" />
            <asp:ImageButton ID="btnPreviousWeek" runat="server" ImageUrl="~/Images/Common/Previous.gif" AlternateText="Previous week" style="vertical-align: middle" />
            <asp:Label ID="lblFrom" runat="server" Text="From: " /><nug:DatePicker ID="dteFrom" runat="server" />
            <asp:Label ID="lblTo" runat="server" Text="To: " /><nug:DatePicker ID="dteTo" runat="server" />
            <asp:Button ID="btnSearch" runat="server" Text="Search" />
            <asp:ImageButton ID="btnNextWeek" runat="server" ImageUrl="~/Images/Common/Next.gif" AlternateText="Next week" style="vertical-align: middle" />
            <asp:ImageButton ID="btnNextMonth" runat="server" ImageUrl="~/Images/Common/Next2.gif" AlternateText="Next month" style="vertical-align: middle" />
            <asp:LinkButton ID="btnToggleFromTo" runat="server" Text=" (fewer options)" OnClientClick="ToggleDateSelectors(); return false;" />
        </span>
    </div>
    <nug:GridView ID="gvLeagueTable" runat="server" AutoGenerateColumns="false" GridLines="None" AllowPaging="false" DataKeyNames="UserID"
        CssClass="GridView NoBorder FullWidth" RowStyle-CssClass="GridViewRow" AlternatingRowStyle-CssClass="GridViewRowAlt" RowHoverCssClass="GridViewRowHover" EnableRowClick="true" RowClickRowCommand="ViewRecord">
        <Columns>
            <%-- Be careful when adding, deleting, or moving columns - the 2-pointer column may be hidden in the code-behind --%>
            <asp:BoundField HeaderText="Pos" DataField="LeaguePosition" ItemStyle-Width="30px" ItemStyle-HorizontalAlign="Center" HeaderStyle-HorizontalAlign="Center" />
            <asp:TemplateField HeaderText="" ItemStyle-Width="18px" HeaderStyle-HorizontalAlign="Center" ItemStyle-HorizontalAlign="Center">
                <ItemTemplate>
                    <asp:Image ID="imgProgress" runat="server" />
                </ItemTemplate>
            </asp:TemplateField>
            <asp:BoundField HeaderText="User" DataField="Username" />
            <asp:BoundField HeaderText="3" DataField="ThreePointers" HeaderStyle-CssClass="LeftBorder nonEssentialInfo" ItemStyle-CssClass="ThreePointer LeftBorder nonEssentialInfo" HeaderStyle-HorizontalAlign="Center" ItemStyle-HorizontalAlign="Center" />
            <asp:BoundField HeaderText="2" DataField="TwoPointers" HeaderStyle-CssClass="nonEssentialInfo" ItemStyle-CssClass="TwoPointer nonEssentialInfo" HeaderStyle-HorizontalAlign="Center" ItemStyle-HorizontalAlign="Center" />
            <asp:BoundField HeaderText="1" DataField="OnePointers" HeaderStyle-CssClass="nonEssentialInfo" ItemStyle-CssClass="OnePointer nonEssentialInfo" HeaderStyle-HorizontalAlign="Center" ItemStyle-HorizontalAlign="Center" />
            <asp:BoundField HeaderText="0" DataField="NoPointers" HeaderStyle-CssClass="nonEssentialInfo" ItemStyle-CssClass="NoPointer nonEssentialInfo" HeaderStyle-HorizontalAlign="Center" ItemStyle-HorizontalAlign="Center" />
            <asp:BoundField HeaderText="-" DataField="NoPredictions" HeaderStyle-CssClass="nonEssentialInfo" ItemStyle-CssClass="NoPrediction nonEssentialInfo" HeaderStyle-HorizontalAlign="Center" ItemStyle-HorizontalAlign="Center" />
            <asp:BoundField HeaderText="Points" DataField="Score" HeaderStyle-HorizontalAlign="Center" HeaderStyle-CssClass="LeftBorder" ItemStyle-CssClass="Score LeftBorder" />
            <asp:BoundField HeaderText="AGD*" DataField="AverageGoalDifference" DataFormatString="{0:N2}" HeaderStyle-CssClass="LeftBorder" HeaderStyle-HorizontalAlign="Center" ItemStyle-CssClass="LeftBorder" ItemStyle-HorizontalAlign="Center" />
        </Columns>
    </nug:GridView>
    <div class="SubNote">* Average goal difference per prediction</div>
</asp:Content>