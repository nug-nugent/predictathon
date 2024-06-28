Namespace Predictathon
    Public Class MessageThreadManager
        Inherits Predictathon.Manager
        Public Const EntitySetName As String = "MessageThreads"

#Region " Properties "
        'Every time the current user views a thread, we need to keep track of the date and time they did so.
        Public Shared ReadOnly Property MessageThreadsReadThisSession As DataTable
            Get
                If Not IsNothing(HttpContext.Current.Session("MessageThreadsReadThisSession")) Then
                    Return DirectCast(HttpContext.Current.Session("MessageThreadsReadThisSession"), DataTable)
                Else
                    Dim dtblMessageThreadsReadThisSession As New DataTable
                    dtblMessageThreadsReadThisSession.Columns.Add("UniqueID", GetType(Guid))
                    dtblMessageThreadsReadThisSession.Columns.Add("DateAndTime", GetType(DateTime))
                    HttpContext.Current.Session("MessageThreadsReadThisSession") = dtblMessageThreadsReadThisSession
                    Return dtblMessageThreadsReadThisSession
                End If
            End Get
        End Property
#End Region

        Public Shared Function Load(ByVal MessageThreadID As Guid) As PredictathonModel.MessageThread
            Using objContext As New PredictathonModel.PredictathonEntities
                'load the object and return it
                Return objContext.MessageThreads.FirstOrDefault(Function(MessageThread) MessageThread.MessageThreadID = MessageThreadID)
            End Using
        End Function

        Public Shared Sub Delete(ByVal MessageThread As PredictathonModel.MessageThread)
            Predictathon.Data.Delete(MessageThread, EntitySetName)
        End Sub

        Public Shared Sub Save(ByVal MessageThread As PredictathonModel.MessageThread)
            Predictathon.Data.Save(MessageThread, EntitySetName)
        End Sub

        Public Shared Function IsSaved(ByVal MessageThreadID As Guid) As Boolean
            Using objContext As New PredictathonModel.PredictathonEntities
                Return (objContext.MessageThreads.Any(Function(MessageThread) MessageThread.MessageThreadID = MessageThreadID))
            End Using
        End Function

        Public Shared Function Create(ByVal MessageThreadID As Guid) As PredictathonModel.MessageThread
            Return New PredictathonModel.MessageThread With {.MessageThreadID = MessageThreadID, .StartedByUserID = UserManager.CurrentUserID, .StartedDateTime = Date.Now}
        End Function

        Public Shared Sub UpdateLastReadForSession(ByVal MessageThreadID As Guid)
            UpdateLastReadForSession(MessageThreadID, Date.Now)
        End Sub

        Public Shared Sub UpdateLastReadForSession(ByVal MessageThreadID As Guid, lastReadDate As Date)
            'this message thread has presumably been loaded because it's being read. Track this in the current session
            Dim dtblReadMessageThreads As New DataTable
            If Not IsNothing(HttpContext.Current.Session("MessageThreadsReadThisSession")) Then
                dtblReadMessageThreads = DirectCast(HttpContext.Current.Session("MessageThreadsReadThisSession"), DataTable)
            Else
                dtblReadMessageThreads.Columns.Add("UniqueID", GetType(Guid))
                dtblReadMessageThreads.Columns.Add("DateAndTime", GetType(DateTime))
            End If
            HttpContext.Current.Session("MessageThreadsReadThisSession") = dtblReadMessageThreads

            ' We have a datatable of MessageThreadIDs and their last read date/time. Add to it or update as necessary
            Dim dRowMessageThread() As DataRow = dtblReadMessageThreads.Select("UniqueID = '" & MessageThreadID.ToString & "'")
            If dRowMessageThread.GetLength(0) > 0 Then
                dRowMessageThread(0)("DateAndTime") = lastReadDate
                dRowMessageThread(0).AcceptChanges()
            Else
                Dim dRowNew As DataRow = dtblReadMessageThreads.NewRow
                dRowNew("UniqueID") = MessageThreadID
                dRowNew("DateAndTime") = lastReadDate
                dtblReadMessageThreads.Rows.Add(dRowNew)
            End If
        End Sub

        Public Shared Function MessageThreadListGet(ByVal UserLastViewedMessageboard As Date, ByVal IncludeHiddenFromPublic As Boolean) As DataTable
			If UserLastViewedMessageboard = Date.MinValue Then UserLastViewedMessageboard = Date.Parse("01/01/1900") 'arbitrary, nonsense... but it'll work

			Dim lstParams As New List(Of System.Data.SqlClient.SqlParameter)
			lstParams.Add(New System.Data.SqlClient.SqlParameter("UserLastViewedMessageboard", SqlDbType.DateTime) With {.Value = UserLastViewedMessageboard})
			lstParams.Add(New System.Data.SqlClient.SqlParameter("MessageThreadsReadThisSession", SqlDbType.Structured) With {.Value = MessageThreadsReadThisSession})
			lstParams.Add(New System.Data.SqlClient.SqlParameter("IncludeHiddenFromPublic", SqlDbType.Bit) With {.Value = IncludeHiddenFromPublic})

			Return Predictathon.Data.CallSP("MessageThreadListGet", lstParams)
		End Function
    End Class
End Namespace
