Namespace Predictathon.Pages.Competition
    Partial Public Class CompetitionTeamList
        Inherits Predictathon.Web.UI.Page

#Region "Properties"
        Protected Friend ReadOnly Property CompetitionID As Guid
            Get
                Return New Guid(Request.QueryString("CompetitionID"))
            End Get
        End Property
#End Region

        Private Sub Page_Init(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Init
            'Should the current user be here? If not, go home...
            If Not UserManager.CurrentUser.CompetitionAdministrator Then Response.Redirect("~/Pages/Common/MainMenu.aspx")
        End Sub

        Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
            Me.Master.TitleSuffix = "Team Administration"

            If Not IsPostBack Then
                'databind the grid
                RebuildList()
                BindDropdown()

                lblAdd.Text = "Teams for " & CompetitionManager.Load(Me.CompetitionID).CompetitionName
            End If

            Form.DefaultButton = btnAdd.UniqueID
        End Sub

        Private Sub RebuildList()
            gvTeamCompetition.DataSource = Predictathon.TeamCompetitionManager.TeamCompetitionListGet(Me.CompetitionID, False)
            gvTeamCompetition.DataBind()
        End Sub

        Private Sub BindDropdown()
            ddlTeam.DataSource = Predictathon.TeamCompetitionManager.TeamCompetitionListGet(Me.CompetitionID, True)
            ddlTeam.DataBind()
        End Sub

        Private Sub gvTeam_PageIndexChanging(ByVal sender As Object, ByVal e As GridViewPageEventArgs) Handles gvTeamCompetition.PageIndexChanging
            gvTeamCompetition.PageIndex = e.NewPageIndex
            RebuildList()
        End Sub

        Private Sub gvTeamCompetition_RowCommand(ByVal sender As Object, ByVal e As GridViewCommandEventArgs) Handles gvTeamCompetition.RowCommand
            If e.CommandName = "DeleteRecord" Then
                'Get the record, then delete it.
                Dim objTeamCompetition As PredictathonModel.TeamCompetition = Predictathon.TeamCompetitionManager.Load(gvTeamCompetition.DataKeyValue(e).Value)
                TeamCompetitionManager.Delete(objTeamCompetition)
                RebuildList()
                BindDropdown()
            End If
        End Sub

        Private Sub btnAdd_Click(ByVal sender As Object, ByVal e As System.Web.UI.ImageClickEventArgs) Handles btnAdd.Click
            If OnValidate() Then
                AddTeamCompetition()
                RebuildList()
                BindDropdown()
            End If
        End Sub

        Private Sub AddTeamCompetition()
            Dim objTeamCompetition As PredictathonModel.TeamCompetition = TeamCompetitionManager.Create(Guid.NewGuid, _
                                                        New Guid(ddlTeam.SelectedValue), _
                                                        Me.CompetitionID)
            TeamCompetitionManager.Save(objTeamCompetition)
            ddlTeam.SelectedIndex = -1
        End Sub

        Private Function OnValidate() As Boolean
            Dim blnValid As Boolean = True
            blnValid = blnValid And Validation.ValidateControl(ddlTeam, lblTeam, Validation.ValidationType.NullOrEmpty)
            Return blnValid
        End Function
    End Class
End Namespace