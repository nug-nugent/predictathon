Namespace Predictathon.Pages.Match
    Partial Public Class MatchProcessingList
        Inherits Predictathon.Web.UI.Page

        Private Sub Page_Init(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Init
            'Should the current user be here? If not, go home...
            If Not UserManager.CurrentUser.MatchAdministrator Then Response.Redirect("~/Pages/Common/MainMenu.aspx")
        End Sub

        Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
            Me.Master.TitleSuffix = "Process Matches"

            If Not IsPostBack AndAlso CheckForCallBack() = False Then
                'databind the grid
                lstMatch.DataSource = Predictathon.MatchManager.MatchListGet(CompetitionManager.CurrentCompetitionID, Nothing, Date.Now, True)
                lstMatch.DataBind()
            End If
        End Sub

        Public Function CheckForCallBack() As Boolean
            Dim strCallBack As String = Request.Params("CallBack")
            If String.IsNullOrEmpty(strCallBack) Then
                Return False
            ElseIf strCallBack = "SaveMatch" Then
                SaveMatchFromCallBack()
            End If
            Return True
        End Function

        Private Sub SaveMatchFromCallBack()
            Dim gdMatchID As Guid
            Dim intHomeTeamGoals As Integer = 0
            Dim intAwayTeamGoals As Integer = 0

            'the CallBack should have passed these values in Request.Params. If anything's gone wrong, don't call the Save method.
            Try
                Guid.TryParse(Request.Params("MatchID"), gdMatchID)
                Integer.TryParse(Request.Params("HomeTeamGoals"), intHomeTeamGoals)
                Integer.TryParse(Request.Params("AwayTeamGoals"), intAwayTeamGoals)
            Catch ex As Exception
                'Return False
            End Try

            If SaveMatch(gdMatchID, intHomeTeamGoals, intAwayTeamGoals) = True Then
                Response.Write(True.ToString.ToLower)
                Response.End()
            Else
                Response.Write(False.ToString.ToLower)
                Response.End()
            End If
        End Sub

        Private Function SaveMatch(ByVal MatchID As Guid, ByVal HomeTeamGoals As Integer, ByVal AwayTeamGoals As Integer) As Boolean
            Dim objMatch As PredictathonModel.Match = Predictathon.MatchManager.Load(MatchID)
            'Security... is our user a match administrator? Has the match actually finished?
            If UserManager.CurrentUser.MatchAdministrator AndAlso Not IsNothing(objMatch) AndAlso objMatch.MatchDateTime <= DateAdd(DateInterval.Minute, 90, Date.Now) Then
                objMatch.HomeTeamGoals = HomeTeamGoals
                objMatch.AwayTeamGoals = AwayTeamGoals
                objMatch.MatchPlayed = True
                Predictathon.MatchManager.Save(objMatch)
                Return True
            Else
                Return False
            End If
        End Function

        Private dteLastDateTime As DateTime = Nothing
        Private blnControlHasFocus As Boolean = False
        Private Sub lstMatch_ItemDataBound(ByVal sender As Object, ByVal e As ListViewItemEventArgs) Handles lstMatch.ItemDataBound
            If e.Item.ItemType = ListViewItemType.DataItem Then
                Dim objMatchResult As PredictathonModel.MatchListGet_Result = DirectCast(e.Item.DataItem, PredictathonModel.MatchListGet_Result)

                'only show the date/time header once per date/time
                If IsNothing(dteLastDateTime) OrElse dteLastDateTime <> DirectCast(e.Item.DataItem, PredictathonModel.MatchListGet_Result).MatchDateTime Then
                    dteLastDateTime = objMatchResult.MatchDateTime
                    e.Item.FindControl("thDateTime").Visible = True
                Else
                    e.Item.FindControl("thDateTime").Visible = False
                End If

                ' Add the necessary data- attributes to make handling the onkeypress event on our textboxes possible.
                ' We'll automatically tab between textboxes, validate for numeric entry, and save the data via a CallBack when possible.
                Dim txtHomeTeamGoals As TextBox = DirectCast(e.Item.FindControl("txtHomeTeamGoals"), TextBox)
                Dim txtAwayTeamGoals As TextBox = DirectCast(e.Item.FindControl("txtAwayTeamGoals"), TextBox)
                Dim lblProgress As Label = DirectCast(e.Item.FindControl("lblProgress"), Label)

                txtHomeTeamGoals.Attributes.Add("data-linkedclientid", txtAwayTeamGoals.ClientID)
                txtHomeTeamGoals.Attributes.Add("data-progressclientid", lblProgress.ClientID)
                txtAwayTeamGoals.Attributes.Add("data-linkedclientid", txtHomeTeamGoals.ClientID)

                ' Focus on the first enabled, non-populated textbox in the list
                If Not blnControlHasFocus AndAlso String.IsNullOrEmpty(txtHomeTeamGoals.Text) Then
                    txtHomeTeamGoals.Focus()
                    blnControlHasFocus = True
                End If
            End If
        End Sub
    End Class
End Namespace