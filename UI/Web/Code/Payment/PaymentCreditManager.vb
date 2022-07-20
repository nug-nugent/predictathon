Namespace Predictathon
    Public Class PaymentCreditManager
        Inherits Predictathon.Manager
        Public Const EntitySetName As String = "PaymentCredits"

        Public Shared Function Load(ByVal PaymentCreditID As Guid) As PredictathonModel.PaymentCredit
            Using objContext As New PredictathonModel.PredictathonEntities
                Return objContext.PaymentCredits.FirstOrDefault(Function(PaymentCredit) PaymentCredit.PaymentCreditID = PaymentCreditID)
            End Using
        End Function

        Public Shared Sub Delete(ByVal PaymentCredit As PredictathonModel.PaymentCredit)
            Predictathon.Data.Delete(PaymentCredit, EntitySetName)
        End Sub

        Public Shared Sub Save(ByVal PaymentCredit As PredictathonModel.PaymentCredit)
            Predictathon.Data.Save(PaymentCredit, EntitySetName)
        End Sub

        Public Shared Function Create(ByVal PaymentCreditID As Guid, ByVal CompetitionID As Guid, ByVal ExpectedUsername As String) As PredictathonModel.PaymentCredit
            'return a new PaymentCredit. Generate its 'unique payment code' with a piece of GUID
            Return New PredictathonModel.PaymentCredit With {.PaymentCreditID = PaymentCreditID, _
                                                             .ForCompetitionID = CompetitionID, _
                                                             .ExpectedUsername = ExpectedUsername, _
                                                             .IssueDate = Date.Now, _
                                                             .UniquePaymentCode = Security.RandomCharacterStringGet(10)}
        End Function

        Public Shared Function PaymentCreditListGet(ByVal ForCompetitionID As Guid?, ByVal PaymentCode As String, Optional ByVal ValidOnly As Boolean = False) As List(Of PredictathonModel.PaymentCredit)
            Using objContext As New PredictathonModel.PredictathonEntities
                Return objContext.PaymentCredits.Where(Function(PaymentCredit) _
                                                        (ForCompetitionID.HasValue = False OrElse PaymentCredit.ForCompetitionID = ForCompetitionID.Value) _
                                                        AndAlso (ValidOnly = False OrElse PaymentCredit.CreditUsed = False) _
                                                        AndAlso (String.IsNullOrEmpty(PaymentCode) OrElse PaymentCredit.UniquePaymentCode = PaymentCode)). _
                                                        OrderByDescending(Of Date)(Function(PaymentCredit) PaymentCredit.IssueDate). _
                                                        ThenBy(Function(PaymentCredit) PaymentCredit.ExpectedUsername).ToList()
            End Using
        End Function

        Public Shared Function IsValidPaymentCreditCode(ByVal CompetitionID As Guid, ByVal PaymentCode As String) As Boolean
            If String.IsNullOrEmpty(PaymentCode) Then
                Return False
            Else
                Return PaymentCreditListGet(CompetitionID, PaymentCode, True).Count > 0
            End If
        End Function
    End Class
End Namespace
