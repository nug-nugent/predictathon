Namespace Predictathon
    Public Class UserCompetitionManager
        Inherits Predictathon.Manager
        Public Const EntitySetName As String = "UserCompetitions"

        Public Shared Function Load(ByVal UserCompetitionID As Guid) As PredictathonModel.UserCompetition
            Using objContext As New PredictathonModel.PredictathonEntities
                Return objContext.UserCompetitions.FirstOrDefault(Function(UserCompetition) UserCompetition.UserCompetitionID = UserCompetitionID)
            End Using
        End Function

        Public Shared Function Load(ByVal UserID As Guid, ByVal CompetitionID As Guid) As PredictathonModel.UserCompetition
            Using objContext As New PredictathonModel.PredictathonEntities
                Return objContext.UserCompetitions.FirstOrDefault(Function(UserCompetition) UserCompetition.UserID = UserID And UserCompetition.CompetitionID = CompetitionID)
            End Using
        End Function

        Public Shared Sub Delete(ByVal UserCompetition As PredictathonModel.UserCompetition)
            Predictathon.Data.Delete(UserCompetition, EntitySetName)
        End Sub

        Public Shared Sub Save(ByVal UserCompetition As PredictathonModel.UserCompetition)
            Predictathon.Data.Save(UserCompetition, EntitySetName)
        End Sub

        Public Shared Function Create(ByVal UserCompetitionID As Guid, ByVal UserID As Guid, ByVal CompetitionID As Guid) As PredictathonModel.UserCompetition
            Return New PredictathonModel.UserCompetition With {.UserCompetitionID = UserCompetitionID, .UserID = UserID, .CompetitionID = CompetitionID}
        End Function

        ''' <summary>
        ''' Creates a UserCompetition based on the values in a Transaction record, then updates and saves the Transaction.
        ''' </summary>
        ''' <param name="Transaction"></param>
        ''' <remarks></remarks>
        Public Shared Sub CreateUserCompetitionFromTransaction(ByVal Transaction As PredictathonModel.Transaction)
            'create a User record
            Dim objUserCompetition As PredictathonModel.UserCompetition = UserCompetitionManager.Create(Guid.NewGuid, Transaction.UserID.Value, Transaction.CompetitionID.Value)
            objUserCompetition.AmountPaid = If(Transaction.ActualPaymentAmount, 0D)
            Transaction.UserCompetitionID = objUserCompetition.UserCompetitionID
            If Not String.IsNullOrEmpty(Transaction.PayPalTransactionID) Then objUserCompetition.PaymentProvider = "PayPal"

            'save the records..
            UserCompetitionManager.Save(objUserCompetition)
            TransactionManager.Save(Transaction)

            'make the competition the user's default
            SetDefaultCompetitionForUser(objUserCompetition.UserID, objUserCompetition.CompetitionID)
            CompetitionManager.CurrentCompetitionID = objUserCompetition.CompetitionID
        End Sub

        Public Shared Sub SetDefaultCompetitionForUser(ByVal UserID As Guid, ByVal NewDefaultCompetitionID As Guid)
            Using objContext As New PredictathonModel.PredictathonEntities
                'update all UserCompetitions with 'default=true' to 'default=false'
                For Each objUserCompetition As PredictathonModel.UserCompetition In objContext.UserCompetitions.Where(Function(UserCompetition) _
                        UserCompetition.UserID = UserID _
                        AndAlso UserCompetition.CompetitionID <> NewDefaultCompetitionID _
                        AndAlso UserCompetition.IsDefaultCompetition = True)
                    objUserCompetition.IsDefaultCompetition = False

                    objContext.Detach(objUserCompetition)
                    Save(objUserCompetition)
                Next

                'if the new default competition isn't already the default, make it so
                Dim objDefaultUserCompetition As PredictathonModel.UserCompetition = objContext.UserCompetitions.Where(Function(UserCompetition) _
                        UserCompetition.UserID = UserID _
                        AndAlso UserCompetition.CompetitionID = NewDefaultCompetitionID _
                        AndAlso UserCompetition.IsDefaultCompetition = False).FirstOrDefault

                If Not IsNothing(objDefaultUserCompetition) Then
                    objDefaultUserCompetition.IsDefaultCompetition = True
                    objContext.Detach(objDefaultUserCompetition)
                    Save(objDefaultUserCompetition)
                End If
            End Using
        End Sub
    End Class
End Namespace
