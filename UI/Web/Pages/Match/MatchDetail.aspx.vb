Namespace Predictathon.Pages.Match
    Partial Public Class MatchDetail
        Inherits Predictathon.Web.UI.Page

#Region "Properties"
        Private ReadOnly Property MatchID As Guid
            Get
                Return New Guid(Request.QueryString("MatchID"))
            End Get
        End Property
#End Region

        Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
            lnkEdit.Visible = UserManager.CurrentUser.MatchAdministrator

            If Not IsPostBack Then
                Me.Master.TitleSuffix = "Match Detail"
                MatchPredictionList1.MatchID = Me.MatchID

                ' This is a bit naughty, but avoiding the MatchManager.Load method saves us going around the houses with multiple loads etc
                Using objContext As New PredictathonModel.PredictathonEntities
                    Dim objMatch As PredictathonModel.Match = objContext.Matches.FirstOrDefault(Function(Match) Match.MatchID = MatchID)
                    Dim objHomeTeam As PredictathonModel.Team = objMatch.HomeTeam
                    Dim objAwayTeam As PredictathonModel.Team = objMatch.AwayTeam

                    litTeamNames.Text = If(Not IsNothing(objHomeTeam),
                                           TeamDetailHyperlink(objHomeTeam.TeamID) & objHomeTeam.TeamName & "</a>" _
                                            , objMatch.HomeTeamTBC) &
                                        If(objMatch.HomeTeamGoals.HasValue, " " & objMatch.HomeTeamGoals.ToString & " - ", "") & If(objMatch.AwayTeamGoals.HasValue, objMatch.AwayTeamGoals.ToString & " ", " vs ") &
                                        If(Not IsNothing(objAwayTeam),
                                            TeamDetailHyperlink(objAwayTeam.TeamID) & objAwayTeam.TeamName & "</a>" _
                                            , objMatch.AwayTeamTBC)

                    If IsNothing(objHomeTeam) OrElse String.IsNullOrEmpty(objHomeTeam.ImageName) Then
                        imgHomeTeam.Visible = False
                    Else
                        imgHomeTeam.ImageUrl = "~/Images/TeamCrests/" & objHomeTeam.ImageName
                    End If

                    If IsNothing(objAwayTeam) OrElse String.IsNullOrEmpty(objAwayTeam.ImageName) Then
                        imgAwayTeam.Visible = False
                    Else
                        imgAwayTeam.ImageUrl = "~/Images/TeamCrests/" & objAwayTeam.ImageName
                    End If

                    'if the match finished in a draw, show the 'after 90 minutes' label
                    lbl90Minutes.Visible = (objMatch.Knockout AndAlso objMatch.MatchPlayed AndAlso objMatch.HomeTeamGoals.Value = objMatch.AwayTeamGoals.Value)

                    lblMatchDate.Text = CommonMethods.LongDateAndTimeString(objMatch.MatchDateTime)
                    MatchDescriptionHeader.Visible = Not String.IsNullOrEmpty(objMatch.Description)
                    lblMatchDescription.Text = objMatch.Description

                    'the prediction can only be saved if the scheduled kick-off time is more than 5 minutes ago...
                    If objMatch.MatchDateTime <= DateAdd(DateInterval.Minute, 5, DateTime.Now) Then
                        txtHomeTeamGoals.Text = "L" 'this may be overwritten with the real prediction in a sec...
                        txtAwayTeamGoals.Text = "L" 'this may be overwritten with the real prediction in a sec...
                        txtHomeTeamGoals.Enabled = False
                        txtAwayTeamGoals.Enabled = False
                        btnSavePrediction.Visible = False
                    Else
                        If objMatch.Knockout Then
                            lblYourPrediction.Text = "Your prediction*:"
                            divKnockout.Visible = True
                        End If
                    End If
                End Using

                Dim objPrediction As PredictathonModel.Prediction = PredictionManager.Search(Me.MatchID, UserManager.CurrentUserID).FirstOrDefault
                If Not IsNothing(objPrediction) Then
                    txtHomeTeamGoals.Text = objPrediction.HomeTeamGoals.ToString
                    txtAwayTeamGoals.Text = objPrediction.AwayTeamGoals.ToString
                    If objPrediction.Score.HasValue Then
                        lblPoints.Text = "  (" & objPrediction.Score.ToString & String.Format(" point{0})", If(objPrediction.Score = 1, "", "s"))
                        lblPoints.CssClass &= " " & PredictionManager.GetCSSClassForScore(CInt(objPrediction.Score))
                    End If
                End If
            End If
        End Sub

        Private Sub btnSavePrediction_Click(ByVal sender As Object, ByVal e As System.Web.UI.ImageClickEventArgs) Handles btnSavePrediction.Click
            If OnValidate() = True Then
                Dim gdPredictionID As Guid?
                Dim objPrediction As PredictathonModel.Prediction = PredictionManager.Search(Me.MatchID, UserManager.CurrentUserID).FirstOrDefault
                If Not IsNothing(objPrediction) Then gdPredictionID = objPrediction.PredictionID

                'Ensure it's not too late to save the prediction at this point...
                Dim objMatch As PredictathonModel.Match = MatchManager.Load(Me.MatchID)
                If Not objMatch.MatchDateTime <= DateAdd(DateInterval.Minute, 5, DateTime.Now) Then
                    'save the prediction
                    Predictathon.PredictionManager.CreateAndSave(Me.MatchID, _
                                                        gdPredictionID, _
                                                        Predictathon.UserManager.CurrentUserID, _
                                                        CInt(txtHomeTeamGoals.Text), _
                                                        CInt(txtAwayTeamGoals.Text))
                    lblSaveProgress.Text = "Saved"
                    lblSaveProgress.ForeColor = Drawing.Color.Green
                Else
                    lblSaveProgress.Text = "Too late!"
                    lblSaveProgress.ForeColor = Drawing.Color.Red
                    txtHomeTeamGoals.Enabled = False
                    txtAwayTeamGoals.Enabled = False
                End If
            End If
        End Sub

        Private Function OnValidate() As Boolean
            Dim blnValid As Boolean = True
            blnValid = blnValid And Validation.ValidateControl(txtHomeTeamGoals, lblYourPrediction, Validation.ValidationType.Numeric)
            blnValid = blnValid And Validation.ValidateControl(txtAwayTeamGoals, lblYourPrediction, Validation.ValidationType.Numeric)
            Return blnValid
        End Function

        Protected Function TeamDetailHyperlink(ByVal TeamID As Guid) As String
            Return String.Format("<a href=""{0}"">", CommonMethods.CurrentURLRoot & "Pages/Team/TeamDetail.aspx?TeamID=" & TeamID.ToString)
        End Function

        Private Sub lnkEdit_Click(sender As Object, e As EventArgs) Handles lnkEdit.Click
            Response.Redirect("MatchEdit.aspx?MatchID=" & MatchID.ToString())
        End Sub
    End Class
End Namespace