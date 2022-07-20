Imports System.Linq
Imports System.Xml.Linq
Imports System.Collections
Imports System.Collections.Generic

'TODO - priority 7 - Twitter feed - make the rebuild list method an async callback

Namespace Predictathon.UserControls.Twitter
    Public Class TwitterFeed
        Inherits Predictathon.Web.UI.UserControl

#Region " Properties "
        Private Shared dteLastUpdated As System.Nullable(Of DateTime) = Nothing
        Private Shared xDoc As XDocument = Nothing

        'We'll get the latest tweets every 10 minutes
        Private Shared Interval As Double = 10
        Private Shared ReadOnly Property IsTimeForUpdate() As Boolean
            Get
                Return dteLastUpdated.HasValue AndAlso DateTime.Now > dteLastUpdated.Value.AddMinutes(Interval)
            End Get
        End Property

        Private _TweetsToDisplay As Integer = 10
        Public Property TweetsToDisplay() As Integer
            Get
                Return _TweetsToDisplay
            End Get
            Set(ByVal value As Integer)
                _TweetsToDisplay = value
            End Set
        End Property

        Private _TwitterProfileName As String
        Public Property TwitterProfileName() As String
            Get
                Return _TwitterProfileName
            End Get
            Set(ByVal value As String)
                _TwitterProfileName = value
            End Set
        End Property
#End Region

        Protected Sub Page_Load(ByVal sender As Object, ByVal e As EventArgs) Handles Me.Load
            If Not IsPostBack Then
                BuildTimeline()
            End If
        End Sub

        Protected Sub BuildTimeline()
            ' Bind our list to the last n Tweets for @TwitterProfileName
            Dim lstTweet As List(Of Predictathon.Twitter.Tweet) = Predictathon.Twitter.GetUserTimeLine(Me.TwitterProfileName, Me.TweetsToDisplay)
			If lstTweet Is Nothing OrElse lstTweet.Count = 0 Then
				Me.Visible = False
			Else
				Me.Visible = True
				lstTimeline.DataSource = lstTweet
				lstTimeline.DataBind()
			End If
        End Sub

        Private blnHeaderBound As Boolean = False
        Private Sub lstTimeline_ItemDataBound(ByVal sender As Object, ByVal e As System.Web.UI.WebControls.ListViewItemEventArgs) Handles lstTimeline.ItemDataBound
            If e.Item.ItemType = ListViewItemType.DataItem Then
                If Not blnHeaderBound Then
                    blnHeaderBound = True
                    Dim objTweet As Predictathon.Twitter.Tweet = DirectCast(e.Item.DataItem, Predictathon.Twitter.Tweet)
					lblName.Text = objTweet.Name
					lblScreenName.Text = "@" & objTweet.ScreenName & ""
                    imgTwitpic.AlternateText = objTweet.ScreenName
                    imgTwitpic.ImageUrl = objTweet.ProfileImageURL
                    hypTwitter.NavigateUrl = objTweet.ProfileURL
                    hypTwitter2.NavigateUrl = objTweet.ProfileURL
                End If
            End If
        End Sub
    End Class
End Namespace