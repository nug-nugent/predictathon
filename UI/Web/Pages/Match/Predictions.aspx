<%@ Page Language="vb" AutoEventWireup="false" MasterPageFile="~/Pages/Master/Main.Master" CodeBehind="Predictions.aspx.vb" Inherits="Predictathon.Pages.Match.Predictions" %>

<%@ MasterType TypeName="Predictathon.Master.Main" %>

<asp:Content runat="server" ContentPlaceHolderID="MainContent">
    <asp:PlaceHolder runat="server"> 
        <%: System.Web.Optimization.Scripts.Render("~/js/matchlist") %> 
    </asp:PlaceHolder>
    <script type="text/javascript">
        window.matches = <%= MatchesJson %>;
        window.weeks = <%= CompetitionWeeksJson %>;
        window.loadedWeek = "<%= LoadedWeek.ToString("s") %>";
    </script>

    <div id="match-list"></div>
</asp:Content>
