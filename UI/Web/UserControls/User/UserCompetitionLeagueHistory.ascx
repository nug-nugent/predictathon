<%@ Control Language="vb" AutoEventWireup="false" CodeBehind="UserCompetitionLeagueHistory.ascx.vb" Inherits="Predictathon.UserControls.User.UserCompetitionLeagueHistory" %>
<%@ Register Assembly="System.Web.DataVisualization, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35" Namespace="System.Web.UI.DataVisualization.Charting" TagPrefix="asp" %>

<div id="divChart" runat="server" class="InformationBlock">
    <div class="TitleBar" style="margin-bottom: 5px">
        <table width="100%">
            <tr>
                <td>
                    <asp:Label ID="lblUsername" runat="server" />
                </td>
            </tr>
        </table>
    </div>
    <div style="padding-top: 3px; vertical-align: top">
        <asp:Chart ID="chtLeagueHistory" runat="server" Width="500px">
            <Series>
                <asp:Series Name="Series1" ChartType="Line" Color="#2266FF" BorderWidth="3" />
            </Series>
            <ChartAreas>
                <asp:ChartArea Name="ChartArea1"> 
                    <AxisX LabelAutoFitStyle="StaggeredLabels"><LabelStyle Format="{0:dd/MM}" /></AxisX>
                    <AxisY IsReversed="true" />
                </asp:ChartArea>
            </ChartAreas>
            <BorderSkin BackColor="Transparent" PageColor="Transparent" SkinStyle="FrameThin6" /> 
        </asp:Chart>
    </div>
</div>