Namespace Predictathon.Pages.Competition
    Partial Public Class CompetitionList
        Inherits Predictathon.Web.UI.Page

        Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
            Me.Master.TitleSuffix = "Competition List"

            If Not IsPostBack Then
                'databind the grid
                gvCompetition.DataSource = Predictathon.CompetitionManager.CompetitionListGet
                gvCompetition.DataBind()
            End If
        End Sub

        Private Sub gvCompetition_RowCommand(ByVal sender As Object, ByVal e As GridViewCommandEventArgs) Handles gvCompetition.RowCommand
            If e.CommandName = "EditRecord" Then
                Response.Redirect("~/Pages/Competition/CompetitionDetailEdit.aspx?CompetitionID=" & gvCompetition.DataKeyValue(e).ToString, False)
            End If
        End Sub

        Private Sub btnAdd_Click(sender As Object, e As ImageClickEventArgs) Handles btnAdd.Click
            Dim blnValid As Boolean = Validation.ValidateControl(txtCompetitionName, lblCompetitionName, Validation.ValidationType.NullOrEmpty)
            If blnValid Then
                'add a new competition, save the record, and redirect to it
                Dim objCompetition As PredictathonModel.Competition = CompetitionManager.Create(Guid.NewGuid, txtCompetitionName.Text)
                CompetitionManager.Save(objCompetition)
                Response.Redirect("~/Pages/Competition/CompetitionDetailEdit.aspx?CompetitionID=" & objCompetition.CompetitionID.ToString)
            End If
        End Sub
    End Class
End Namespace