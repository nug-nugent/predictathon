<%@ Control Language="vb" AutoEventWireup="false" CodeBehind="UserCompetitionRegistrationList.ascx.vb" Inherits="Predictathon.UserControls.Competition.UserCompetitionRegistrationList" %>
<nug:GridView ID="gvUserCompetitionRegistration" runat="server" AutoGenerateColumns="false" GridLines="None" AllowPaging="true" PageSize="3" CssClass="GridView NoBorder" DataKeyNames="CompetitionID"
    RowStyle-CssClass="GridViewRow" EnableRowClick="true" AlternatingRowStyle-CssClass="GridViewRowAlt" RowHoverCssClass="GridViewRowHover">
    <Columns>
        <asp:TemplateField HeaderStyle-HorizontalAlign="Center" HeaderText="Competitions">
            <ItemTemplate>
                <table class="FullWidth">
                    <tr>
                        <td style="width: 40px">
                            <asp:Image ID="imgCompetition" runat="server" style="float: left" Height="40px" ImageUrl='<%# ImageURL(DirectCast(Container.DataItem, PredictathonModel.UserCompetitionRegistrationListGet_Result).ImageFilename) %>' />                
                        </td>
                        <td>
                            <%# DirectCast(Container.DataItem, PredictathonModel.UserCompetitionRegistrationListGet_Result).CompetitionName%>                    
                        </td>
                        <td style="width: 50%" align="center">
                            <asp:Label ID="lblRegistrationStatus" runat="server" />                
                        </td>
                    </tr>
                </table>
            </ItemTemplate>
        </asp:TemplateField>
    </Columns>
</nug:GridView>