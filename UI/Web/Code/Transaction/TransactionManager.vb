Imports System.Data.Objects
Namespace Predictathon
    Public Class TransactionManager
        Inherits Predictathon.Manager
        Public Const EntitySetName As String = "Transactions"

        Public Shared Function Load(ByVal TransactionID As Guid) As PredictathonModel.Transaction
            Using objContext As New PredictathonModel.PredictathonEntities
                Return objContext.Transactions.FirstOrDefault(Function(Transaction) Transaction.TransactionID = TransactionID)
            End Using
        End Function

        Public Shared Sub Delete(ByVal Transaction As PredictathonModel.Transaction)
            Predictathon.Data.Delete(Transaction, EntitySetName)
        End Sub

        Public Shared Sub Save(ByVal Transaction As PredictathonModel.Transaction)
            Predictathon.Data.Save(Transaction, EntitySetName)
        End Sub

        Public Shared Function Create(ByVal TransactionID As Guid) As PredictathonModel.Transaction
            'return a new Transaction
            Return New PredictathonModel.Transaction With {.TransactionID = TransactionID, .TransactionDateTime = Date.Now}
        End Function

        Public Shared Function IsValidPayPalTransactionID(ByVal TransactionID As Guid, ByVal PayPalTransactionID As String) As Boolean
            Using objContext As New PredictathonModel.PredictathonEntities
                'the string should always be 17 characters.
                'Plus, if the PayPalTransactionID has already been used on another Transaction, it's invalid...
                Return PayPalTransactionID.Length = 17 AndAlso objContext.Transactions.Where(Function(objTransaction) objTransaction.TransactionID <> TransactionID AndAlso objTransaction.PayPalTransactionID = PayPalTransactionID).Count = 0
            End Using
        End Function
    End Class
End Namespace
