<%@ Page Language="vb" AutoEventWireup="false" MasterPageFile="~/Pages/Master/Main.Master" CodeBehind="UserMatchPredictionList.aspx.vb" Inherits="Predictathon.Pages.Match.UserMatchPredictionList" %>

<%@ MasterType TypeName="Predictathon.Master.Main" %>

<asp:Content runat="server" ContentPlaceHolderID="MainContent">
    <script type="text/javascript">
        $(function () {
            $(".Score").on("input", function (e) {
                if (IsNumeric($(this).val())) {
                    FocusOnNextMatch(this);
                }
                else if ($(this).val() != "") {
                    $(this).val(0);
                    $(this.select());
                }
            }).change(function (e) {
                SavePredictionIfNecessary(this);
            }).focus(function (e) {
                $(this).select();
            })
        });

        function SavePredictionIfNecessary(input) {
            var home = $(input).data("home") ? $(input) : $('#' + $(input).data("linkedclientid"));
            var away = $(input).data("home") ? $('#' + $(input).data("linkedclientid")) : $(input);

            if (IsNumeric(home.val()) && IsNumeric(away.val())) {
                SavePrediction(home.data("matchid"), home.data("predictionid"), home.val(), away.val(), home.data("progressclientid"));
            }
            else {
                SetSaveProgress(false, false, true, home.data("progressclientid"));
            }
        }

        function FocusOnNextMatch(sender) {
            var nextMatchNumber = $(sender).data("predictionnumber") + 1;
            var nextMatch = $("*[data-predictionnumber=" + nextMatchNumber + "]").first();
            if (nextMatch.length) {
                nextMatch.focus();
            }
            else { <%' Likely the last prediction. Trigger onblur, which will result in the prediction being saved %>
                $(sender).blur();
                $(sender).focus();
            }
        }

        function SavePrediction(MatchID, PredictionID, HomeGoals, AwayGoals, lblProgressClientID) {
            SetSaveProgress(false, false, false, lblProgressClientID);
            $.post('UserMatchPredictionList.aspx?CallBack=SavePrediction',
                {
                    MatchID: MatchID
                    , PredictionID: PredictionID
                    , HomeTeamGoals: HomeGoals
                    , AwayTeamGoals: AwayGoals
                },
                function (returnValue) {
                    SetSaveProgress(returnValue, true, false, lblProgressClientID);
                }
            );
        }

        function SetSaveProgress(result, progressComplete, editInProgress, lblProgressClientID) {
            var lblProgress = $('#' + lblProgressClientID);
            if (!progressComplete) {
                if (!editInProgress) {
                    lblProgress.text('Saving...');
                    lblProgress.removeClass().addClass('SaveMessage SaveInProgress');
                }
                else {
                    lblProgress.text('Editing...');
                    lblProgress.removeClass().addClass('SaveMessage EditInProgress');
                }
            }
            else {
                if (!result) {
                    __doPostBack('', ''); <% '//lblProgress.text('Save failed.'); //lblProgress.removeClass().addClass('SaveMessage SaveFailed'); %>
                }
                else {
                    lblProgress.text('Saved.');
                    lblProgress.removeClass().addClass('SaveMessage SaveSuccessful');
                }
            }
        }
    </script>

    <div class="FullWidth" style="text-align: center; margin-bottom: 5px">
        <asp:ImageButton ID="btnPreviousMonth" runat="server" ImageUrl="~/Images/Common/Previous2.gif" AlternateText="Previous month" Style="vertical-align: middle" />
        <asp:ImageButton ID="btnPreviousWeek" runat="server" ImageUrl="~/Images/Common/Previous.gif" AlternateText="Previous week" Style="vertical-align: middle" />
        <asp:Label ID="lblFrom" runat="server" Text="Week starting: " /><nug:DatePicker ID="dteDateFrom" runat="server" Enabled="false" />
        <asp:ImageButton ID="btnNextWeek" runat="server" ImageUrl="~/Images/Common/Next.gif" AlternateText="Next week" Style="vertical-align: middle" />
        <asp:ImageButton ID="btnNextMonth" runat="server" ImageUrl="~/Images/Common/Next2.gif" AlternateText="Next month" Style="vertical-align: middle" />
    </div>

    <div class="FullWidth" style="border: 1px solid #00F">
        <asp:ListView ID="lstMatch" runat="server">
            <LayoutTemplate>
                <table class="GridView NoBorder FullWidth" style="border-style: none">
                    <asp:PlaceHolder ID="itemPlaceholder" runat="server" />
                </table>
            </LayoutTemplate>
            <ItemTemplate>
                <th id="thDateTime" runat="server" colspan="4" style="text-align: left"><%# Predictathon.CommonMethods.LongDateAndTimeString(DirectCast(Container.DataItem, PredictathonModel.UserMatchPredictionListGet_Result).MatchDateTime) %></th>
                <tr align="center" class="GridViewRow" style="vertical-align: middle">
                    <td style="width: 45%;" align="right">
                        <asp:Image ID="imgHomeTeam" runat="server" Height="16px" Style="margin-right: 10px; vertical-align: middle" ImageUrl='<%# Predictathon.TeamManager.TeamCrestImageURL(DirectCast(Container.DataItem, PredictathonModel.UserMatchPredictionListGet_Result).HomeTeamImage) %>' />
                        <asp:Label ID="lblHomeTeam" runat="server" Text='<%# DirectCast(Container.DataItem, PredictathonModel.UserMatchPredictionListGet_Result).HomeTeam %>' />
                    </td>
                    <td style="width: 5%;">
                        <asp:TextBox ID="txtHomeTeamGoals" runat="server" type="Number" min="0" max="9" CssClass="Score" data-home="true" data-predictionnumber="<%#((Container.DataItemIndex + 1) * 2) - 1 %>" data-matchid="<%# DirectCast(Container.DataItem, PredictathonModel.UserMatchPredictionListGet_Result).MatchID %>"
                            data-predictionid="<%# DirectCast(Container.DataItem, PredictathonModel.UserMatchPredictionListGet_Result).PredictionID %>" Text='<%# DirectCast(Container.DataItem, PredictathonModel.UserMatchPredictionListGet_Result).HomeTeamGoals %>' MaxLength="1" Width="2em" />
                    </td>
                    <td style="width: 5%;">
                        <asp:TextBox ID="txtAwayTeamGoals" runat="server" type="Number" min="0" max="9" CssClass="Score" data-predictionnumber="<%#(Container.DataItemIndex + 1) * 2 %>"
                            Text='<%# DirectCast(Container.DataItem, PredictathonModel.UserMatchPredictionListGet_Result).AwayTeamGoals %>' MaxLength="1" Width="2em" />
                    </td>
                    <td style="width: 45%;" align="left">
                        <asp:Label ID="lblAwayTeam" runat="server" Text='<%# DirectCast(Container.DataItem, PredictathonModel.UserMatchPredictionListGet_Result).AwayTeam %>' />
                        <asp:Image ID="imgAwayTeam" runat="server" Height="16px" Style="margin-left: 10px; vertical-align: middle" ImageUrl='<%# Predictathon.TeamManager.TeamCrestImageURL(DirectCast(Container.DataItem, PredictathonModel.UserMatchPredictionListGet_Result).AwayTeamImage) %>' />
                        <asp:Label ID="lblAsterisk" runat="server" CssClass="Score Error" Text="*" Visible="false" />
                        <span style="float: right">
                            <asp:Label ID="lblScore" runat="server" CssClass="Score" />
                            <asp:Label ID="lblProgress" runat="server" Text="" />
                        </span>
                    </td>
                </tr>
            </ItemTemplate>
            <AlternatingItemTemplate>
                <th id="thDateTime" runat="server" colspan="4" style="text-align: left"><%# Predictathon.CommonMethods.LongDateAndTimeString(DirectCast(Container.DataItem, PredictathonModel.UserMatchPredictionListGet_Result).MatchDateTime)%></th>
                <tr align="center" class="GridViewRowAlt" style="vertical-align: middle">
                    <td style="width: 45%;" align="right">
                        <asp:Image ID="imgHomeTeam" runat="server" Height="16px" Style="margin-right: 10px; vertical-align: middle" ImageUrl='<%# Predictathon.TeamManager.TeamCrestImageURL(DirectCast(Container.DataItem, PredictathonModel.UserMatchPredictionListGet_Result).HomeTeamImage) %>' />
                        <asp:Label ID="lblHomeTeam" runat="server" Text='<%# DirectCast(Container.DataItem, PredictathonModel.UserMatchPredictionListGet_Result).HomeTeam %>' />
                    </td>
                    <td style="width: 5%;">
                        <asp:TextBox ID="txtHomeTeamGoals" runat="server" type="Number" min="0" max="9" CssClass="Score" data-home="true" data-matchid="<%# DirectCast(Container.DataItem, PredictathonModel.UserMatchPredictionListGet_Result).MatchID %>" data-predictionnumber="<%#((Container.DataItemIndex + 1) * 2) - 1 %>"
                            data-predictionid="<%# DirectCast(Container.DataItem, PredictathonModel.UserMatchPredictionListGet_Result).PredictionID %>" Text='<%# DirectCast(Container.DataItem, PredictathonModel.UserMatchPredictionListGet_Result).HomeTeamGoals %>' MaxLength="1" Width="2em" />
                    </td>
                    <td style="width: 5%;">
                        <asp:TextBox ID="txtAwayTeamGoals" runat="server" type="Number" min="0" max="9" CssClass="Score" data-predictionnumber="<%#((Container.DataItemIndex + 1) * 2) %>"
                            Text='<%# DirectCast(Container.DataItem, PredictathonModel.UserMatchPredictionListGet_Result).AwayTeamGoals %>' MaxLength="1" Width="2em" />
                    </td>
                    <td style="width: 45%;" align="left">
                        <asp:Label ID="lblAwayTeam" runat="server" Text='<%# DirectCast(Container.DataItem, PredictathonModel.UserMatchPredictionListGet_Result).AwayTeam %>' />
                        <asp:Image ID="imgAwayTeam" runat="server" Height="16px" Style="margin-left: 10px; vertical-align: middle" ImageUrl='<%# Predictathon.TeamManager.TeamCrestImageURL(DirectCast(Container.DataItem, PredictathonModel.UserMatchPredictionListGet_Result).AwayTeamImage) %>' />
                        <asp:Label ID="lblAsterisk" runat="server" CssClass="Score Error" Text="*" Visible="false" />
                        <span style="float: right">
                            <asp:Label ID="lblScore" runat="server" CssClass="Score" />
                            <asp:Label ID="lblProgress" runat="server" Text="" />
                        </span>
                    </td>
                </tr>
            </AlternatingItemTemplate>
            <EmptyDataTemplate>
                No matches found.
            </EmptyDataTemplate>
        </asp:ListView>
    </div>
    <div id="divKnockout" runat="server" visible="false" class="SubNote">
        <asp:Label ID="lblKnockout" runat="server" Text="* Extra time excluded" />
    </div>
</asp:Content>
