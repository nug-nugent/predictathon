Namespace Predictathon.Pages.Payment
    Partial Public Class PaymentCreditList
        Inherits Predictathon.Web.UI.Page

        Private Sub Page_Init(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Init
            'Should the current user be here? If not, go home...
            If Not UserManager.CurrentUser.UserAdministrator Then Response.Redirect("~/Pages/Common/MainMenu.aspx")
        End Sub

        Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
            Me.Master.TitleSuffix = "Payment Credit Administration"

            If Not IsPostBack Then
                'databind the grid
                RebuildList()
                BindDropdown()
            End If

            Form.DefaultButton = btnAdd.UniqueID
        End Sub

        Private Sub RebuildList()
            gvPaymentCredit.DataSource = Predictathon.PaymentCreditManager.PaymentCreditListGet(Nothing, "")
            gvPaymentCredit.DataBind()
        End Sub

        Private Sub BindDropdown()
            ddlCompetition.DataSource = Predictathon.CompetitionManager.CompetitionListGet
            ddlCompetition.DataBind()
        End Sub

        Private Sub gvPaymentCredit_PageIndexChanging(ByVal sender As Object, ByVal e As GridViewPageEventArgs) Handles gvPaymentCredit.PageIndexChanging
            gvPaymentCredit.PageIndex = e.NewPageIndex
            RebuildList()
        End Sub

        Private Sub gvPaymentCredit_RowCommand(ByVal sender As Object, ByVal e As GridViewCommandEventArgs) Handles gvPaymentCredit.RowCommand
            If e.CommandName = "DeleteRecord" Then
                'Get the record, then delete it.
                Dim objPaymentCredit As PredictathonModel.PaymentCredit = PaymentCreditManager.Load(gvPaymentCredit.DataKeyValue(e).Value)
                PaymentCreditManager.Delete(objPaymentCredit)
                RebuildList()
            End If
        End Sub

        Private Sub btnAdd_Click(ByVal sender As Object, ByVal e As System.Web.UI.ImageClickEventArgs) Handles btnAdd.Click
            If OnValidate() Then
                AddPaymentCredit()
                RebuildList()
            End If
        End Sub

        Private Sub AddPaymentCredit()
            Dim objPaymentCredit As PredictathonModel.PaymentCredit = PaymentCreditManager.Create(Guid.NewGuid, _
                                                        New Guid(ddlCompetition.SelectedValue), _
                                                        txtExpectedUsername.Text)
            PaymentCreditManager.Save(objPaymentCredit)
            txtExpectedUsername.Text = String.Empty
        End Sub

        Private Function OnValidate() As Boolean
            Dim blnValid As Boolean = True
            blnValid = blnValid And Validation.ValidateControl(txtExpectedUsername, lblExpectedUsername, Validation.ValidationType.NullOrEmpty)

            Return blnValid
        End Function
    End Class
End Namespace