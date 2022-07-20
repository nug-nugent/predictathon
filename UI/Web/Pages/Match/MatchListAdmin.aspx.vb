Namespace Predictathon.Pages.Match
    Partial Public Class MatchListAdmin
        Inherits Predictathon.Web.UI.Page

        Private Sub Page_Init(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Init
            ' Should the current user be here? If not, go home...
            If Not UserManager.CurrentUser.MatchAdministrator Then Response.Redirect("~/Pages/Common/MainMenu.aspx")
        End Sub

        Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
            Me.Master.TitleSuffix = "Match Administration"

            If Not IsPostBack Then
                ' Databind the grid
                RebuildList()
                BindDropdowns(ddlHomeTeam, ddlAwayTeam)

                ' Default to the last match date/time for the current competition in the DB... 
                Dim objLastMatch As PredictathonModel.Match = MatchManager.MatchListGet(CompetitionManager.CurrentCompetitionID, Date.Today, Nothing, Nothing, Nothing, True).FirstOrDefault
                If Not IsNothing(objLastMatch) Then
                    dteMatchDate.Value = objLastMatch.MatchDateTime
                    tmeMatchTime.Value = objLastMatch.MatchDateTime
                End If

                chkNeutralGround.Checked = CompetitionManager.CurrentCompetition.DefaultToNeutralGround
            End If

            Form.DefaultButton = btnAdd.UniqueID
        End Sub

        Private Sub RebuildList()
            gvMatch.DataSource = MatchManager.MatchListGet(CompetitionManager.CurrentCompetitionID, Nothing, Nothing, Not chkIncludePlayedMatches.Checked)
            gvMatch.DataBind()
        End Sub

        Private Sub BindDropdowns(ByRef ddlHome As DropDownList, ByRef ddlAway As DropDownList)
            Dim lstTeam As List(Of PredictathonModel.Team) = TeamManager.TeamListGet(CompetitionManager.CurrentCompetitionID)
            ddlHome.DataSource = lstTeam
            ddlHome.DataBind()
            ddlHome.Items.Insert(0, New ListItem With {.Text = String.Empty, .Value = Guid.Empty.ToString})

            ddlAway.DataSource = lstTeam
            ddlAway.DataBind()
            ddlAway.Items.Insert(0, New ListItem With {.Text = String.Empty, .Value = Guid.Empty.ToString})
        End Sub

        Private Sub gvMatch_PageIndexChanging(ByVal sender As Object, ByVal e As GridViewPageEventArgs) Handles gvMatch.PageIndexChanging
            gvMatch.PageIndex = e.NewPageIndex
            RebuildList()
        End Sub

        Private Sub gvMatch_RowCommand(ByVal sender As Object, ByVal e As GridViewCommandEventArgs) Handles gvMatch.RowCommand
            If e.CommandName = "EnableRowEdit" Then
                gvMatch.Columns(9).Visible = True 'save
                gvMatch.Columns(10).Visible = True 'cancel
                gvMatch.Columns(11).Visible = True 'delete
                gvMatch.EnableEditMode(e)
                RebuildList()

                Dim ddlHome As DropDownList = DirectCast(gvMatch.SelectedRow(e).FindControl("ddlHomeTeam"), DropDownList)
                Dim ddlAway As DropDownList = DirectCast(gvMatch.SelectedRow(e).FindControl("ddlAwayTeam"), DropDownList)
                BindDropdowns(ddlHome, ddlAway)
                Dim objMatch As PredictathonModel.Match = MatchManager.Load(gvMatch.DataKeyValue(e).Value)
                ddlHome.SelectedValue = objMatch.HomeTeamID.ToString
                ddlAway.SelectedValue = objMatch.AwayTeamID.ToString
                'lock the goals if the match is yet to be played
                If Not objMatch.MatchPlayed Then
                    Dim txtHomeTeamGoals As System.Web.UI.WebControls.TextBox = DirectCast(gvMatch.SelectedRow(e).FindControl("txtHomeTeamGoals"), System.Web.UI.WebControls.TextBox)
                    Dim txtAwayTeamGoals As System.Web.UI.WebControls.TextBox = DirectCast(gvMatch.SelectedRow(e).FindControl("txtAwayTeamGoals"), System.Web.UI.WebControls.TextBox)
                    txtHomeTeamGoals.Enabled = False
                    txtAwayTeamGoals.Enabled = False
                End If
            ElseIf e.CommandName = "SaveRecord" Then
                'find the relevant controls within the row
                Dim ddlHome As DropDownList = DirectCast(gvMatch.SelectedRow(e).FindControl("ddlHomeTeam"), DropDownList)
                Dim ddlAway As DropDownList = DirectCast(gvMatch.SelectedRow(e).FindControl("ddlAwayTeam"), DropDownList)
                Dim txtHomeTBC As TextBox = DirectCast(gvMatch.SelectedRow(e).FindControl("txtHomeTeamTBC"), TextBox)
                Dim txtAwayTBC As TextBox = DirectCast(gvMatch.SelectedRow(e).FindControl("txtAwayTeamTBC"), TextBox)
                Dim dteMatchDateGrid As Predictathon.Web.UI.WebControls.DatePicker = DirectCast(gvMatch.SelectedRow(e).FindControl("dteMatchDate"), Predictathon.Web.UI.WebControls.DatePicker)
                Dim tmeMatchTimeGrid As Predictathon.Web.UI.WebControls.TimePicker = DirectCast(gvMatch.SelectedRow(e).FindControl("tmeMatchTime"), Predictathon.Web.UI.WebControls.TimePicker)
                Dim txtHomeTeamGoals As System.Web.UI.WebControls.TextBox = DirectCast(gvMatch.SelectedRow(e).FindControl("txtHomeTeamGoals"), System.Web.UI.WebControls.TextBox)
                Dim txtAwayTeamGoals As System.Web.UI.WebControls.TextBox = DirectCast(gvMatch.SelectedRow(e).FindControl("txtAwayTeamGoals"), System.Web.UI.WebControls.TextBox)

                Dim intHomeTeamGoals As Integer?
                Dim intAwayTeamGoals As Integer?
                If Not String.IsNullOrEmpty(txtHomeTeamGoals.Text) AndAlso Not String.IsNullOrEmpty(txtAwayTeamGoals.Text) Then
                    'parse the content of the home/away goals into integers.
                    'Annoying workaround here as we can't do TryParse on nullable integers...
                    Dim intHomeGoals As Integer
                    Integer.TryParse(txtHomeTeamGoals.Text, intHomeGoals)
                    intHomeTeamGoals = intHomeGoals
                    Dim intAwayGoals As Integer
                    Integer.TryParse(txtAwayTeamGoals.Text, intAwayGoals)
                    intAwayTeamGoals = intAwayGoals
                End If
                Dim blnNeutralGround As Boolean = DirectCast(gvMatch.SelectedRow(e).FindControl("chkNeutralGround"), System.Web.UI.WebControls.CheckBox).Checked
                Dim blnKnockout As Boolean = DirectCast(gvMatch.SelectedRow(e).FindControl("chkKnockout"), System.Web.UI.WebControls.CheckBox).Checked

                'TODO - priority 5 - validation (this is an admin-only page)
                'Update the record and save it
                Dim objMatch As PredictathonModel.Match = MatchManager.Load(gvMatch.DataKeyValue(e).Value)
                If ddlHome.SelectedValue <> Guid.Empty.ToString Then
                    objMatch.HomeTeamID = New Guid(ddlHome.SelectedValue)
                Else
                    objMatch.HomeTeamID = Nothing
                End If
                objMatch.HomeTeamTBC = txtHomeTBC.Text

                If ddlAway.SelectedValue <> Guid.Empty.ToString Then
                    objMatch.AwayTeamID = New Guid(ddlAway.SelectedValue)
                Else
                    objMatch.AwayTeamID = Nothing
                End If
                objMatch.AwayTeamTBC = txtAwayTBC.Text

                objMatch.Description = DirectCast(gvMatch.SelectedRow(e).FindControl("txtMatchDescription"), System.Web.UI.WebControls.TextBox).Text
                objMatch.MatchDateTime = dteMatchDateGrid.Value.AddHours(tmeMatchTimeGrid.Value.Hour).AddMinutes(tmeMatchTimeGrid.Value.Minute)
                objMatch.HomeTeamGoals = intHomeTeamGoals
                objMatch.AwayTeamGoals = intAwayTeamGoals
                objMatch.NeutralGround = blnNeutralGround
                objMatch.Knockout = blnKnockout

                MatchManager.Save(objMatch)
                DisableEditMode()
                RebuildList()
            ElseIf e.CommandName = "DeleteRecord" Then
                'Get the record, then delete it.
                Dim objMatch As PredictathonModel.Match = MatchManager.Load(gvMatch.DataKeyValue(e).Value)
                MatchManager.Delete(objMatch)
                DisableEditMode()
                RebuildList()
            ElseIf e.CommandName = "CancelEdit" Then
                DisableEditMode()
                RebuildList()
            End If
        End Sub

        Private Sub DisableEditMode()
            gvMatch.Columns(9).Visible = False 'save
            gvMatch.Columns(10).Visible = False 'cancel
            gvMatch.Columns(11).Visible = False 'delete
            gvMatch.DisableEditMode(True)
        End Sub

        Private Sub btnAdd_Click(ByVal sender As Object, ByVal e As System.Web.UI.ImageClickEventArgs) Handles btnAdd.Click
            If OnValidate() Then
                AddMatch()
                BindDropdowns(ddlHomeTeam, ddlAwayTeam)
                RebuildList()
            End If
        End Sub

        Private Sub AddMatch()
            Dim gdHomeTeamID As Guid?
            Dim gdAwayTeamID As Guid?
            If ddlHomeTeam.SelectedValue <> Guid.Empty.ToString Then gdHomeTeamID = New Guid(ddlHomeTeam.SelectedValue)
            If ddlAwayTeam.SelectedValue <> Guid.Empty.ToString Then gdAwayTeamID = New Guid(ddlAwayTeam.SelectedValue)

            Dim objMatch As PredictathonModel.Match = MatchManager.Create(Guid.NewGuid, _
                                                        CompetitionManager.CurrentCompetitionID, _
                                                        gdHomeTeamID, _
                                                        gdAwayTeamID)

            objMatch.MatchDateTime = dteMatchDate.Value.AddHours(tmeMatchTime.Value.Hour).AddMinutes(tmeMatchTime.Value.Minute)
            objMatch.NeutralGround = chkNeutralGround.Checked
            objMatch.Knockout = chkKnockout.Checked
            objMatch.Description = txtMatchDescription.Text

            If Not gdHomeTeamID.HasValue Then objMatch.HomeTeamTBC = txtHomeTeamTBC.Text.Trim
            If Not gdAwayTeamID.HasValue Then objMatch.AwayTeamTBC = txtAwayTeamTBC.Text.Trim
            MatchManager.Save(objMatch)
        End Sub

        Private Function OnValidate() As Boolean
            Dim blnValid As Boolean = True

            If ddlHomeTeam.SelectedValue = Guid.Empty.ToString AndAlso txtHomeTeamTBC.Text = String.Empty Then
                ddlHomeTeam.BackColor = Drawing.Color.Red
                txtHomeTeamTBC.BackColor = Drawing.Color.Red
                blnValid = False
            Else
                ddlHomeTeam.BackColor = Drawing.Color.White
                txtHomeTeamTBC.BackColor = Drawing.Color.White
            End If

            If ddlAwayTeam.SelectedValue = Guid.Empty.ToString AndAlso txtAwayTeamTBC.Text = String.Empty Then
                ddlAwayTeam.BackColor = Drawing.Color.Red
                txtAwayTeamTBC.BackColor = Drawing.Color.Red
                blnValid = False
            Else
                ddlAwayTeam.BackColor = Drawing.Color.White
                txtAwayTeamTBC.BackColor = Drawing.Color.White
            End If

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

            If blnValid Then
                'is this a duplicate fixture? Do we care?
                If Not CompetitionManager.CurrentCompetition.DuplicateFixturesAllowed Then
                    Dim objMatch As PredictathonModel.Match = MatchManager.MatchListGet(CompetitionManager.CurrentCompetitionID, Nothing, Nothing, New Guid(ddlHomeTeam.SelectedValue), New Guid(ddlAwayTeam.SelectedValue), True).FirstOrDefault
                    If Not IsNothing(objMatch) Then
                        'it's a duplicate, and we do care...
                        lblError.Visible = True
                        lblError.Text = String.Format("A match between these teams already exists (for {0})", objMatch.MatchDateTime.ToString("dd/MM/yyyy"))
                        blnValid = False
                    End If
                End If

                If blnValid Then lblError.Visible = False
            End If

            Return blnValid
        End Function

        Private Sub chkIncludePlayedMatches_CheckedChanged(sender As Object, e As EventArgs) Handles chkIncludePlayedMatches.CheckedChanged
            RebuildList()
        End Sub
    End Class
End Namespace