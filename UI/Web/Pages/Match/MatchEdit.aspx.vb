Namespace Predictathon.Pages.Match
    Partial Public Class MatchEdit
        Inherits Predictathon.Web.UI.Page

#Region "Properties"
        Private ReadOnly Property MatchID As Guid
            Get
                Return New Guid(Request.QueryString("MatchID"))
            End Get
        End Property
#End Region

        Private Sub Page_Init(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Init
            ' Should the current user be here? If not, go home...
            If Not UserManager.CurrentUser.MatchAdministrator Then Response.Redirect("~/Pages/Common/MainMenu.aspx")
        End Sub

        Protected Sub Page_Load(ByVal sender As Object, ByVal e As EventArgs) Handles Me.Load
            If Not IsPostBack Then
                Me.Master.TitleSuffix = "Match Edit"

                ' This is a bit naughty, but avoiding the MatchManager.Load method saves us going around the houses with multiple loads etc
                Using objContext As New PredictathonModel.PredictathonEntities
                    Dim objMatch As PredictathonModel.Match = objContext.Matches.FirstOrDefault(Function(Match) Match.MatchID = MatchID)
                    Dim objHomeTeam As PredictathonModel.Team = objMatch.HomeTeam
                    Dim objAwayTeam As PredictathonModel.Team = objMatch.AwayTeam

                    dteMatchDate.Value = objMatch.MatchDateTime.Date
                    tmeMatchTime.Value = Date.MinValue.AddHours(objMatch.MatchDateTime.Hour).AddMinutes(objMatch.MatchDateTime.Minute)

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

                    MatchDescriptionHeader.Visible = Not String.IsNullOrEmpty(objMatch.Description)
                    lblMatchDescription.Text = objMatch.Description
                End Using
            End If
        End Sub

        Private Sub btnSave_Click(ByVal sender As Object, ByVal e As System.Web.UI.ImageClickEventArgs) Handles btnSave.Click
            If OnValidate() Then
                Dim objMatch As PredictathonModel.Match = MatchManager.Load(MatchID)
                objMatch.MatchDateTime = dteMatchDate.Value.AddHours(tmeMatchTime.Value.Hour).AddMinutes(tmeMatchTime.Value.Minute)
                MatchManager.Save(objMatch)
                Response.Redirect("MatchDetail.aspx?MatchID=" & MatchID.ToString)
            End If
        End Sub

        Private Function OnValidate() As Boolean
            Dim blnValid As Boolean = True

            If Not dteMatchDate.HasValidValue Then
                dteMatchDate.BackColor = Drawing.Color.Red
                blnValid = False
            Else
                dteMatchDate.BackColor = Drawing.Color.White
            End If

            If Not tmeMatchTime.HasValidValue Then
                tmeMatchTime.BackColor = Drawing.Color.Red
                blnValid = False
            Else
                tmeMatchTime.BackColor = Drawing.Color.White
            End If

            Return blnValid
        End Function

        Protected Function TeamDetailHyperlink(ByVal TeamID As Guid) As String
            Return String.Format("<a href=""{0}"">", CommonMethods.CurrentURLRoot & "Pages/Team/TeamDetail.aspx?TeamID=" & TeamID.ToString)
        End Function
    End Class
End Namespace