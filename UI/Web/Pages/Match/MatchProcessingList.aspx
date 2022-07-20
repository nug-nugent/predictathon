<%@ Page Language="vb" AutoEventWireup="false" MasterPageFile="~/Pages/Master/Main.Master" CodeBehind="MatchProcessingList.aspx.vb" Inherits="Predictathon.Pages.Match.MatchProcessingList" %>

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
                SaveMatchIfNecessary(this);
            }).focus(function (e) {
                $(this).select();
            })
        });

        function SaveMatchIfNecessary(input) {
            var home = $(input).data("home") ? $(input) : $('#' + $(input).data("linkedclientid"));
            var away = $(input).data("home") ? $('#' + $(input).data("linkedclientid")) : $(input);

            if (IsNumeric(home.val()) && IsNumeric(away.val())) {
                SaveMatch(home.data("matchid"), home.val(), away.val(), home.data("progressclientid"));
            }
            else {
                SetSaveProgress(false, false, true, home.data("progressclientid"));
            }
        }

        function FocusOnNextMatch(sender) {
            var nextMatchNumber = $(sender).data("matchnumber") + 1;
            var nextMatch = $("*[data-matchnumber=" + nextMatchNumber + "]").first();
            if (nextMatch.length) {
                nextMatch.focus();
            }
            else { <%' Likely the last prediction. Trigger onblur, which will result in the prediction being saved %>
                $(sender).blur();
                $(sender).focus();
            }
        }

        function SaveMatch(MatchID, HomeGoals, AwayGoals, lblProgressClientID) {
            SetSaveProgress(false, false, false, lblProgressClientID);
            $.post('MatchProcessingList.aspx?CallBack=SaveMatch',
                {
                    MatchID: MatchID
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

    <div class="FullWidth" style="border: 1px solid #00F">
        <asp:ListView ID="lstMatch" runat="server">
            <LayoutTemplate>
                <table class="GridView NoBorder FullWidth" style="border-style: none">
                    <asp:PlaceHolder ID="itemPlaceholder" runat="server" />
                </table>
            </LayoutTemplate>
            <ItemTemplate>
                <th id="thDateTime" runat="server" colspan="4" style="text-align: left"><%# Predictathon.CommonMethods.LongDateAndTimeString(DirectCast(Container.DataItem, PredictathonModel.MatchListGet_Result).MatchDateTime)%></th>
                <tr align="center" class="GridViewRow" style="vertical-align: middle">
                    <td style="width: 45%;" align="right">
                        <asp:Image ID="imgHomeTeam" runat="server" Height="16px" Style="margin-right: 10px; vertical-align: middle" ImageUrl='<%# Predictathon.TeamManager.TeamCrestImageURL(DirectCast(Container.DataItem, PredictathonModel.MatchListGet_Result).HomeTeamImage) %>' />
                        <asp:Label ID="lblHomeTeam" runat="server" Text='<%# DirectCast(Container.DataItem, PredictathonModel.MatchListGet_Result).HomeTeam %>' />
                    </td>
                    <td style="width: 5%;">
                        <asp:TextBox ID="txtHomeTeamGoals" runat="server" type="Number" min="0" max="9" CssClass="Score" data-home="true" data-matchnumber="<%#((Container.DataItemIndex + 1) * 2) - 1 %>" data-matchid="<%# DirectCast(Container.DataItem, PredictathonModel.MatchListGet_Result).MatchID %>"
                            Text='<%# DirectCast(Container.DataItem, PredictathonModel.MatchListGet_Result).HomeTeamGoals %>' MaxLength="1" Width="2em" />
                    </td>
                    <td style="width: 5%;">
                        <asp:TextBox ID="txtAwayTeamGoals" runat="server" type="Number" min="0" max="9" CssClass="Score" data-matchnumber="<%#(Container.DataItemIndex + 1) * 2 %>"
                            Text='<%# DirectCast(Container.DataItem, PredictathonModel.MatchListGet_Result).AwayTeamGoals %>' MaxLength="1" Width="2em" />
                    </td>
                    <td style="width: 45%;" align="left">
                        <asp:Label ID="lblAwayTeam" runat="server" Text='<%# DirectCast(Container.DataItem, PredictathonModel.MatchListGet_Result).AwayTeam %>' />
                        <asp:Image ID="imgAwayTeam" runat="server" Height="16px" Style="margin-left: 10px; vertical-align: middle" ImageUrl='<%# Predictathon.TeamManager.TeamCrestImageURL(DirectCast(Container.DataItem, PredictathonModel.MatchListGet_Result).AwayTeamImage) %>' />
                        <span style="float: right">
                            <asp:Label ID="lblProgress" runat="server" Text="" />
                        </span>
                    </td>
                </tr>
            </ItemTemplate>
            <AlternatingItemTemplate>
                <th id="thDateTime" runat="server" colspan="4" style="text-align: left"><%# Predictathon.CommonMethods.LongDateAndTimeString(DirectCast(Container.DataItem, PredictathonModel.MatchListGet_Result).MatchDateTime)%></th>
                <tr align="center" class="GridViewRowAlt" style="vertical-align: middle">
                    <td style="width: 45%;" align="right">
                        <asp:Image ID="imgHomeTeam" runat="server" Height="16px" Style="margin-right: 10px; vertical-align: middle" ImageUrl='<%# Predictathon.TeamManager.TeamCrestImageURL(DirectCast(Container.DataItem, PredictathonModel.MatchListGet_Result).HomeTeamImage) %>' />
                        <asp:Label ID="lblHomeTeam" runat="server" Text='<%# DirectCast(Container.DataItem, PredictathonModel.MatchListGet_Result).HomeTeam %>' />
                    </td>
                    <td style="width: 5%;">
                        <asp:TextBox ID="txtHomeTeamGoals" runat="server" type="Number" min="0" max="9" CssClass="Score" data-home="true" data-matchid="<%# DirectCast(Container.DataItem, PredictathonModel.MatchListGet_Result).MatchID %>" data-matchnumber="<%#((Container.DataItemIndex + 1) * 2) - 1 %>"
                            Text='<%# DirectCast(Container.DataItem, PredictathonModel.MatchListGet_Result).HomeTeamGoals %>' MaxLength="1" Width="2em" />                        
                    </td>
                    <td style="width: 5%;">
                        <asp:TextBox ID="txtAwayTeamGoals" runat="server" type="Number" min="0" max="9" CssClass="Score" data-matchnumber="<%#((Container.DataItemIndex + 1) * 2) %>"
                            Text='<%# DirectCast(Container.DataItem, PredictathonModel.MatchListGet_Result).AwayTeamGoals %>' MaxLength="1" Width="2em" />
                    </td>
                    <td style="width: 45%;" align="left">
                        <asp:Label ID="lblAwayTeam" runat="server" Text='<%# DirectCast(Container.DataItem, PredictathonModel.MatchListGet_Result).AwayTeam %>' />
                        <asp:Image ID="imgAwayTeam" runat="server" Height="16px" Style="margin-left: 10px; vertical-align: middle" ImageUrl='<%# Predictathon.TeamManager.TeamCrestImageURL(DirectCast(Container.DataItem, PredictathonModel.MatchListGet_Result).AwayTeamImage) %>' />
                        <span style="float: right">
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
</asp:Content>
