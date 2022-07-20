Namespace Predictathon
    Namespace Web.UI
        Public Class Page
            Inherits System.Web.UI.Page

#Region " Properties "
            Private _KeepSessionAlive As Boolean
            Public Property KeepSessionAlive() As Boolean
                Get
                    Return _KeepSessionAlive
                End Get
                Set(ByVal value As Boolean)
                    _KeepSessionAlive = value
                End Set
            End Property
#End Region

            Private Sub Page_Init(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Init
                Me.MaintainScrollPositionOnPostBack = True

                ' Ensure that the current username, if there is one, is in sync between ASP.NET Identity and our Session.
                ' This method will redirect the user to the login page if necessary.
                Security.CheckForIdentityUserMismatch()
            End Sub

            Private Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
                If Me.KeepSessionAlive AndAlso Not IsNothing(HttpContext.Current.Session("CurrentUserID")) Then
                    'these functions will make a callback to a pointless 'RetainSession' handler 30 seconds before the session times out.
                    'This will keep the user's session alive.
                    Dim objJScript As New System.Text.StringBuilder
                    With objJScript
                        .Append("$(function() {").AppendLine()
                        .Append("   setInterval(""KeepSessionAlive();"", ").Append(((Session.Timeout * 60000) - 30000).ToString).Append(");").AppendLine()
                        .Append("});").AppendLine()
                        .AppendLine()
                        .Append("function KeepSessionAlive() {").AppendLine()
                        .Append("   $.post(""").Append(CommonMethods.CurrentURLRoot & "Handlers/RetainSession.ashx").Append(""");").AppendLine()
                        .Append("}").AppendLine()
                    End With

                    ScriptManager.RegisterStartupScript(Me, Page.GetType, "KeepSessionAlive", objJScript.ToString, True)
                End If
            End Sub
        End Class

        Public Class UserControl
            Inherits System.Web.UI.UserControl
        End Class

        Namespace WebControls
            Public Class GridView
                Inherits System.Web.UI.WebControls.GridView

#Region " Properties "
                Private _EnableRowClick As Boolean = False
                Public Property EnableRowClick() As Boolean
                    Get
                        Return _EnableRowClick
                    End Get
                    Set(ByVal value As Boolean)
                        _EnableRowClick = value
                    End Set
                End Property

                Private _RowHoverCssClass As String
                Public Property RowHoverCssClass() As String
                    Get
                        Return _RowHoverCssClass
                    End Get
                    Set(ByVal value As String)
                        _RowHoverCssClass = value
                    End Set
                End Property

                Private _RowClickRowCommand As String = "EditRecord"
                Public Property RowClickRowCommand() As String
                    Get
                        Return _RowClickRowCommand
                    End Get
                    Set(ByVal value As String)
                        _RowClickRowCommand = value
                    End Set
                End Property

                <ComponentModel.DefaultValue(PagerTypes.Custom), ComponentModel.Category("Paging"), ComponentModel.Description("Indicates whether to use the built-in custom pager or not.")>
                Public Property PagerType() As PagerTypes
                    Get
                        If Not ViewState("PagerType") Is Nothing Then
                            Return DirectCast(ViewState("PagerType"), PagerTypes)
                        Else
                            Return PagerTypes.Custom
                        End If
                    End Get
                    Set(ByVal value As PagerTypes)
                        ViewState("PagerType") = value
                    End Set
                End Property
#End Region

#Region " Paging "
                Public Enum PagerTypes
                    Regular = 0
                    Custom = 1
                End Enum

                Protected Overrides Sub InitializePager(ByVal row As System.Web.UI.WebControls.GridViewRow, ByVal columnSpan As Integer, ByVal pagedDataSource As System.Web.UI.WebControls.PagedDataSource)
                    Select Case Me.PagerType
                        Case PagerTypes.Custom
                            InitCustomPager(row, columnSpan, pagedDataSource)
                        Case Else
                            MyBase.InitializePager(row, columnSpan, pagedDataSource)
                    End Select
                End Sub

                Private Sub InitCustomPager(ByVal row As System.Web.UI.WebControls.GridViewRow, ByVal columnSpan As Integer, ByVal pagedDataSource As System.Web.UI.WebControls.PagedDataSource)
                    Dim pnlPager As Panel = New Panel()
                    With pnlPager
                        .ID = "pnlPager"
                        .CssClass = Me.PagerStyle.CssClass
                    End With

                    Dim tblPager As Table = New Table()
                    With tblPager
                        .ID = "tblPager"
                        .CellPadding = 1
                        .CellSpacing = 0
                        .Style.Add("width", "100%")
                        '.Style.Add("height", "100%")
                        .BorderStyle = BorderStyle.None
                        .GridLines = GridLines.None
                    End With

                    Dim trPager As TableRow = New TableRow()
                    trPager.ID = "trPager"

                    Dim ltlPageIndex As Literal = New Literal()
                    ltlPageIndex.ID = "ltlPageIndex"
                    ltlPageIndex.Text = (Me.PageIndex + 1).ToString()

                    Dim ltlPageCount As Literal = New Literal()
                    ltlPageCount.ID = "ltlPageCount"
                    ltlPageCount.Text = Me.PageCount.ToString()

                    Dim tcPageXofY As TableCell = New TableCell()
                    With tcPageXofY
                        .ID = "tcPageXofY"
                        .Style.Add("width", "30%")
                        .Style.Add("text-align", "left")
                        .Style.Add("padding-left", "3px")
                        .Style.Add("border", "0px")
                        .Controls.Add(New LiteralControl("Page "))
                        .Controls.Add(ltlPageIndex)
                        .Controls.Add(New LiteralControl(" of "))
                        .Controls.Add(ltlPageCount)
                    End With

                    Dim ibtnFirst As ImageButton = New ImageButton()
                    With ibtnFirst
                        .ID = "ibtnFirst"
                        .CommandName = "First"
                        .ToolTip = "First Page"
                        .ImageAlign = ImageAlign.AbsMiddle
                        .CssClass = "PagerButton"
                        .CausesValidation = False
                        AddHandler .Command, AddressOf Me.PagerCommand
                    End With

                    Dim ibtnPrevious As ImageButton = New ImageButton()
                    With ibtnPrevious
                        .ID = "ibtnPrevious"
                        .CommandName = "Previous"
                        .ToolTip = "Previous Page"
                        .ImageAlign = ImageAlign.AbsMiddle
                        .CssClass = "PagerButton"
                        .CausesValidation = False
                        AddHandler .Command, AddressOf Me.PagerCommand
                    End With

                    Dim ibtnNext As ImageButton = New ImageButton()
                    With ibtnNext
                        .ID = "ibtnNext"
                        .CommandName = "Next"
                        .ToolTip = "Next Page"
                        .ImageAlign = ImageAlign.AbsMiddle
                        .CssClass = "PagerButton"
                        .CausesValidation = False
                        AddHandler .Command, AddressOf Me.PagerCommand
                    End With

                    Dim ibtnLast As ImageButton = New ImageButton()
                    With ibtnLast
                        .ID = "ibtnLast"
                        .CommandName = "Last"
                        .ToolTip = "Last Page"
                        .ImageAlign = ImageAlign.AbsMiddle
                        .CssClass = "PagerButton"
                        .CausesValidation = False
                        AddHandler .Command, AddressOf Me.PagerCommand
                    End With

                    Dim strImagePath As String = "~/Images/Common/Gridview/"
                    If Me.PageIndex > 0 Then
                        ibtnFirst.ImageUrl = strImagePath & "page-first.gif"
                        ibtnPrevious.ImageUrl = strImagePath & "page-prev.gif"
                        ibtnFirst.Enabled = True
                        ibtnPrevious.Enabled = True
                    Else
                        ibtnFirst.ImageUrl = strImagePath & "page-first-disabled.gif"
                        ibtnPrevious.ImageUrl = strImagePath & "page-prev-disabled.gif"
                        ibtnFirst.Enabled = False
                        ibtnPrevious.Enabled = False
                        ibtnFirst.Style.Add("cursor", "default")
                        ibtnPrevious.Style.Add("cursor", "default")
                    End If

                    If Me.PageIndex < Me.PageCount - 1 Then
                        ibtnNext.ImageUrl = strImagePath & "page-next.gif"
                        ibtnLast.ImageUrl = strImagePath & "page-last.gif"
                        ibtnNext.Enabled = True
                        ibtnLast.Enabled = True
                    Else
                        ibtnNext.ImageUrl = strImagePath & "page-next-disabled.gif"
                        ibtnLast.ImageUrl = strImagePath & "page-last-disabled.gif"
                        ibtnNext.Enabled = False
                        ibtnLast.Enabled = False
                        ibtnNext.Style.Add("cursor", "default")
                        ibtnLast.Style.Add("cursor", "default")
                    End If

                    Dim tcPagerBtns As TableCell = New TableCell()
                    With tcPagerBtns
                        .ID = "tcPagerBtns"
                        .Style.Add("width", "40%")
                        .Style.Add("text-align", "center")
                        .Style.Add("border", "0px")
                        .Controls.Add(ibtnFirst)
                        .Controls.Add(ibtnPrevious)
                        .Controls.Add(New LiteralControl("&nbsp;&nbsp;&nbsp;&nbsp;"))
                        .Controls.Add(ibtnNext)
                        .Controls.Add(ibtnLast)
                    End With

                    Dim ddlPages As DropDownList = New DropDownList()
                    With ddlPages
                        .ID = "ddlPages"
                        .CssClass = "paging_gridview_pgr_ddl"
                        .AutoPostBack = True
                        For i As Integer = 1 To Me.PageCount Step +1
                            .Items.Add(New ListItem(i.ToString(), i.ToString()))
                        Next i
                        .SelectedIndex = Me.PageIndex
                        .CausesValidation = False
                        AddHandler .SelectedIndexChanged, AddressOf Me.ddlPages_SelectedIndexChanged
                    End With

                    Dim tcPagerDDL As TableCell = New TableCell()
                    With tcPagerDDL
                        .ID = "tcPagerDDL"
                        .Style.Add("width", "30%")
                        .Style.Add("text-align", "right")
                        .Style.Add("padding-right", "3px")
                        .Style.Add("border", "0px")
                        .Controls.Add(New LiteralControl("Page:"))
                        .Controls.Add(ddlPages)
                    End With

                    'add cells to row
                    trPager.Cells.Add(tcPageXofY)
                    trPager.Cells.Add(tcPagerBtns)
                    trPager.Cells.Add(tcPagerDDL)

                    'add row to table
                    tblPager.Rows.Add(trPager)

                    'add table to div
                    pnlPager.Controls.Add(tblPager)

                    'add div to pager row
                    row.Controls.AddAt(0, New TableCell())
                    row.Cells(0).ColumnSpan = columnSpan
                    row.Cells(0).Controls.Add(pnlPager)
                End Sub

                Protected Sub ddlPages_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs)
                    Dim newPageIndex As Integer = CType(sender, DropDownList).SelectedIndex
                    OnPageIndexChanging(New GridViewPageEventArgs(newPageIndex))
                End Sub

                Protected Sub PagerCommand(ByVal sender As Object, ByVal e As System.Web.UI.WebControls.CommandEventArgs)
                    Dim curPageIndex As Integer = Me.PageIndex
                    Dim newPageIndex As Integer = 0

                    Select Case e.CommandName
                        Case "First"
                            newPageIndex = 0
                        Case "Previous"
                            If curPageIndex > 0 Then
                                newPageIndex = curPageIndex - 1
                            End If
                        Case "Next"
                            If Not curPageIndex = Me.PageCount Then
                                newPageIndex = curPageIndex + 1
                            End If
                        Case "Last"
                            newPageIndex = Me.PageCount
                    End Select

                    OnPageIndexChanging(New GridViewPageEventArgs(newPageIndex))
                End Sub
#End Region

                Protected Overrides Sub OnRowCreated(ByVal e As System.Web.UI.WebControls.GridViewRowEventArgs)
                    If e.Row.RowType = DataControlRowType.DataRow Then
                        If Me.EnableRowClick Then
                            e.Row.Attributes.Add("onclick", Page.ClientScript.GetPostBackEventReference(Me, Me.RowClickRowCommand & "$" & e.Row.RowIndex.ToString))
                        End If
                        If Not String.IsNullOrEmpty(Me.RowHoverCssClass) Then
                            e.Row.Attributes.Add("onmouseover", "Highlight(this, '" & Me.RowHoverCssClass & "');")
                            e.Row.Attributes.Add("onmouseout", "Highlight_Off(this);")
                        End If
                    End If
                    MyBase.OnRowCreated(e)
                End Sub

                Protected Overrides Sub Render(ByVal writer As System.Web.UI.HtmlTextWriter)
                    If Me.CssClass = "GridView" AndAlso Me.EnableRowClick Then Me.CssClass &= " GridViewWithRowClick"
                    'If Me.CssClass = String.Empty Then Me.CssClass = "GridView"
                    'If Me.EnableRowClick Then Me.CssClass &= " GridViewWithRowClick"
                    MyBase.Render(writer)
                End Sub

                Public Function DataKeyValue(ByVal e As GridViewCommandEventArgs, Optional ByVal DataKeyIndex As Integer = 0) As Guid?
                    Try
                        Return New Guid(Me.DataKeys(Convert.ToInt32(e.CommandArgument))(DataKeyIndex).ToString())
                    Catch ex As Exception
                        Return Nothing
                    End Try
                End Function

                Public Function SelectedRowIndex(ByVal e As GridViewCommandEventArgs) As Integer
                    Return Convert.ToInt32(e.CommandArgument)
                End Function

                Public Shadows Function SelectedRow(ByVal e As GridViewCommandEventArgs) As GridViewRow
                    Return Me.Rows(Convert.ToInt32(e.CommandArgument))
                End Function

                Public Sub EnableEditMode(ByVal e As GridViewCommandEventArgs)
                    Me.EditIndex = Me.SelectedRowIndex(e)
                    Me.EnableRowClick = False
                End Sub

                Public Sub DisableEditMode(ByVal EnableRowClick As Boolean)
                    Me.EditIndex = -1
                    Me.EnableRowClick = EnableRowClick
                End Sub

                Private Sub Page_PageIndexChanging(ByVal sender As Object, ByVal e As GridViewPageEventArgs) Handles Me.PageIndexChanging
                    'ensure the scroll position jumps back to the top of the page on the page index changing
                    ScriptManager.RegisterStartupScript(Me, GetType(Predictathon.Web.UI.WebControls.GridView), "ResetScrollPositionOnStartup", "ResetScrollPosition();", True)
                End Sub
            End Class

            Public Class DatePicker
                Inherits System.Web.UI.WebControls.TextBox

#Region " Properties "
                ''' <summary>
                ''' Gets or sets the minimum selectable date value. Defaults to 1 year before the current date.
                ''' </summary>
                ''' <remarks></remarks>
                Public Property MinDate() As Date? = DateAdd(DateInterval.Year, -1, Date.Today)

                ''' <summary>
                ''' Gets or sets the maximum selectable date value. Defaults to 1 year after the current date.
                ''' </summary>
                ''' <remarks></remarks>
                Public Property MaxDate() As Date? = DateAdd(DateInterval.Year, 1, Date.Today)

                Public Property ShowButtonPanel() As Boolean = False

                Public Property ValidateMinMaxDates() As Boolean = False

                Public Property ValidateDateAsMandatory() As Boolean = False

                Public Property Value() As Date
                    Get
                        If IsDate(Me.Text) Then
                            Return CDate(Me.Text)
                        Else
                            Me.Text = String.Empty

                            Return Date.MinValue
                        End If
                    End Get
                    Set(ByVal value As Date)
                        If value > Date.MinValue Then
                            Me.Text = value.ToString("dd/MM/yyyy")
                        Else
                            Me.Text = ""
                        End If
                    End Set
                End Property
#End Region

                Protected Overrides Sub Render(ByVal writer As HtmlTextWriter)
                    Dim strJScript As String = "$(document).ready(function() { $('#" & Me.ClientID & "').datepicker({ dateFormat: 'dd/mm/yy'" & Me.Options & "}); });"
                    Page.ClientScript.RegisterStartupScript(GetType(DatePicker), "DatePicker_" & Me.UniqueID, strJScript, True)
                    Me.Width = Unit.Parse("5em")
                    MyBase.Render(writer)
                End Sub

                Private Function Options() As String
                    Dim strOptions As String = ""
                    If Me.MinDate.HasValue Then strOptions &= ", minDate: new Date(" & Me.MinDate.Value.Year & ", " & Me.MinDate.Value.Month - 1 & ", " & Me.MinDate.Value.Day & ")"
                    If Me.MaxDate.HasValue Then strOptions &= ", maxDate: new Date(" & Me.MaxDate.Value.Year & ", " & Me.MaxDate.Value.Month - 1 & ", " & Me.MaxDate.Value.Day & ")"
                    If Me.ShowButtonPanel Then strOptions &= ", showButtonPanel: true"

                    Return strOptions
                End Function

                Public Function HasValidValue() As Boolean
                    Dim blnValid As Boolean = True

                    If Me.ValidateDateAsMandatory Then
                        If Me.Value = Date.MinValue Then blnValid = False
                    End If

                    If blnValid AndAlso Me.ValidateMinMaxDates Then
                        If Me.MinDate.HasValue AndAlso Me.Value < Me.MinDate.Value Then
                            blnValid = False
                        ElseIf Me.MaxDate.HasValue AndAlso Me.Value > Me.MaxDate.Value Then
                            blnValid = False
                        End If
                    End If

                    Return blnValid
                End Function
            End Class

            Public Class TimePicker
                Inherits System.Web.UI.WebControls.TextBox

#Region " Properties "
                Public Property Value() As Date
                    Get
                        If IsDate(Me.Text) Then
                            Return CDate(Me.Text)
                        Else
                            Me.Text = String.Empty
                            Return Nothing
                        End If
                    End Get
                    Set(ByVal value As Date)
                        Me.Text = value.ToString("HH:mm")
                    End Set
                End Property

                Private _ValidateTimeAsMandatory As Boolean
                Public Property ValidateTimeAsMandatory() As Boolean
                    Get
                        Return _ValidateTimeAsMandatory
                    End Get
                    Set(ByVal value As Boolean)
                        _ValidateTimeAsMandatory = value
                    End Set
                End Property
#End Region

                Protected Overrides Sub Render(ByVal writer As HtmlTextWriter)
                    Dim strJScript As String = "$(document).ready(function() { $('#" & Me.ClientID & "').timeEntry({show24Hours: true, showSeconds: false, noSeparatorEntry: true, spinnerImage: ''}); });"
                    Page.ClientScript.RegisterStartupScript(GetType(TimePicker), "TimePicker_" & Me.UniqueID, strJScript, True)
                    Me.Width = Unit.Parse("2.5em")
                    MyBase.Render(writer)
                End Sub

                Public Function HasValidValue() As Boolean
                    If Not String.IsNullOrEmpty(Me.Text) AndAlso Not IsDate(Me.Text) Then
                        Return False
                    ElseIf Me.ValidateTimeAsMandatory Then
                        If String.IsNullOrEmpty(Me.Text) Then
                            Return False
                        Else
                            Return Not IsNothing(Me.Value) AndAlso Me.Value <> Date.MinValue
                        End If
                    End If
                End Function
            End Class

        End Namespace
    End Namespace
End Namespace