Namespace Predictathon.UserControls.Message
    Public Class MessageThreadList
        Inherits Predictathon.Web.UI.UserControl

#Region " Properties "
#End Region

        Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
            If Not IsPostBack Then
                RebuildList()
            End If
        End Sub

        Private Sub RebuildList()
            Dim objUser As PredictathonModel.User = Predictathon.UserManager.CurrentUser
            If IsNothing(Session("UserFirstViewedMessageboard")) Then
                ' Keep track of the date/time the user last viewed the messageboard, from the last value of objUser.LastViewedMessageboard.
                ' This value will be unchanged for the rest of the user's session.
                If objUser.LastViewedMessageboard.HasValue Then
                    Session("UserFirstViewedMessageboard") = objUser.LastViewedMessageboard.Value
                Else
                    Session("UserFirstViewedMessageboard") = Date.Parse("01/01/1900") 'nonsensical arbitrary date from the past
                End If
            End If

            gvMessageThreadList.DataSource = Predictathon.MessageThreadManager.MessageThreadListGet(CDate(Session("UserFirstViewedMessageboard")), objUser.CanViewHiddenMessageThreads)
            gvMessageThreadList.DataBind()

            'set CurrentUser.LastViewedMessageboard to now
            objUser.LastViewedMessageboard = Date.Now
            Predictathon.UserManager.Save(objUser)
        End Sub

        Private Sub gvMessageThreadList_RowCommand(ByVal sender As Object, ByVal e As GridViewCommandEventArgs) Handles gvMessageThreadList.RowCommand
            If e.CommandName = "EditRecord" Then
                Response.Redirect("~/Pages/Message/MessageThreadDetail.aspx?MessageThreadID=" & gvMessageThreadList.DataKeyValue(e).ToString)
            End If
        End Sub

        Private Sub gvMessageThreadList_PageIndexChanging(ByVal sender As Object, ByVal e As GridViewPageEventArgs) Handles gvMessageThreadList.PageIndexChanging
            gvMessageThreadList.PageIndex = e.NewPageIndex
            RebuildList()
        End Sub

        Private Sub lnkNewMessageThread_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles lnkNewMessageThread.Click
            Response.Redirect("~/Pages/Message/NewMessageThread.aspx")
        End Sub
    End Class
End Namespace