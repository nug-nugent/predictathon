Namespace Predictathon.UserControls.Rules
    Public Class Rules
        Inherits Predictathon.Web.UI.UserControl

#Region " Properties "
        Private _CompetitionID As Guid = CompetitionManager.CurrentCompetitionID
        Public Property CompetitionID() As Guid
            Get
                Return _CompetitionID
            End Get
            Set(ByVal value As Guid)
                _CompetitionID = value
            End Set
        End Property

        Public WriteOnly Property ShowTransparency() As Boolean
            Set(ByVal value As Boolean)
                If Not value Then divRules.Attributes("class") = "InputBlock NoHover"
            End Set
        End Property

        Public Property RandomTeam1() As String
            Get
                Return hdnRandomTeam1.Value
            End Get
            Set(ByVal value As String)
                hdnRandomTeam1.Value = value
            End Set
        End Property

        Public Property RandomTeam2() As String
            Get
                Return hdnRandomTeam2.Value
            End Get
            Set(ByVal value As String)
                hdnRandomTeam2.Value = value
            End Set
        End Property
#End Region

        Private Sub Page_PreRender(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.PreRender
            If Not IsPostBack Then
                Dim objCompetition As PredictathonModel.Competition = CompetitionManager.Load(Me.CompetitionID)
                If IsNothing(objCompetition) Then objCompetition = CompetitionManager.CompetitionListGet.FirstOrDefault
                Dim strCompetitionName As String = If(objCompetition.PrependNameWithThe, "the ", "") & objCompetition.CompetitionName
                litCompetitionDescription.Text = String.Format("Players predict the score for every match in {0}", If(Me.CompetitionID = CompetitionManager.CurrentCompetitionID, strCompetitionName & ".", ("a major football competition, in this example " & strCompetitionName & ".")))

                ' Get two random team names from the competition
                RandomTeam1 = TeamManager.RandomTeamGet(objCompetition.CompetitionID).TeamName
                While RandomTeam2 = "" OrElse RandomTeam2 = RandomTeam1
                    RandomTeam2 = TeamManager.RandomTeamGet(objCompetition.CompetitionID).TeamName
                End While
            End If
        End Sub
    End Class
End Namespace