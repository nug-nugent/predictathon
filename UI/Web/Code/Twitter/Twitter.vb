Namespace Predictathon
	''' <summary>
	''' Obsolete class which previously fetched Tweets from a timeline.
	''' </summary>
	<Obsolete>
	Public Class Twitter

		Public Structure Tweet
			Private _TweetContent As String
			Public Property TweetContent() As String
				Get
					Return _TweetContent
				End Get
				Set(ByVal value As String)
					_TweetContent = value
				End Set
			End Property

			Private _ScreenName As String
			Public Property ScreenName() As String
				Get
					Return _ScreenName
				End Get
				Set(ByVal value As String)
					_ScreenName = value
				End Set
			End Property

			Private _Name As String
			Public Property Name() As String
				Get
					Return _Name
				End Get
				Set(ByVal value As String)
					_Name = value
				End Set
			End Property

			Private _ProfileImageURL As String
			Public Property ProfileImageURL() As String
				Get
					Return _ProfileImageURL
				End Get
				Set(ByVal value As String)
					_ProfileImageURL = value
				End Set
			End Property

			Private _CreatedAt As Date
			Public Property CreatedAt() As Date
				Get
					Return _CreatedAt
				End Get
				Set(ByVal value As Date)
					_CreatedAt = value
				End Set
			End Property

			Private _CreatedAtRelativeDateTime As String
			Public Property CreatedAtRelativeDateTime() As String
				Get
					Return _CreatedAtRelativeDateTime
				End Get
				Set(ByVal value As String)
					_CreatedAtRelativeDateTime = value
				End Set
			End Property

			Private _ProfileURL As String
			Public Property ProfileURL() As String
				Get
					Return _ProfileURL
				End Get
				Set(ByVal value As String)
					_ProfileURL = value
				End Set
			End Property
		End Structure

		Protected Class NativeTwitterData
			Public created_at As String
			Public text As String
			Public user As TwitterUserData

			Public Structure TwitterUserData
				Public profile_image_url As String
				Public name As String
				Public screen_name As String
			End Structure

			Public Function ConvertToTweet() As Tweet
				Dim objTweet As New Tweet
				With objTweet
					.TweetContent = Predictathon.MessageManager.FormatMessage(Me.text).Replace("<br /><br />", "<br />")
					'parse Twitter's ridiculous 'created_date' into a date
					.CreatedAt = Date.ParseExact(Me.created_at, "ddd MMM dd HH:mm:ss zzz yyyy", System.Globalization.CultureInfo.InvariantCulture)
					.CreatedAtRelativeDateTime = RelativeTime(.CreatedAt)
					.Name = Me.user.name
					.ScreenName = Me.user.screen_name
					.ProfileImageURL = Me.user.profile_image_url
					.ProfileURL = "http://twitter.com/" & .ScreenName
				End With

				Return objTweet
			End Function
		End Class

		Public Shared Function GetUserTimeLine(ByVal UserScreenName As String, ByVal MaxTweetsToReturn As Integer) As List(Of Tweet)
			Dim strResourceURL As String = "https://api.twitter.com/1.1/statuses/user_timeline.json"

			Dim lstParams As New List(Of KeyValuePair(Of String, String))
			lstParams.Add(New KeyValuePair(Of String, String)("count", MaxTweetsToReturn.ToString))
			lstParams.Add(New KeyValuePair(Of String, String)("include_rts", "1"))
			lstParams.Add(New KeyValuePair(Of String, String)("screen_name", UserScreenName))

			Return GetTwitterResponseAsTweets(strResourceURL, lstParams)
		End Function

		Protected Shared Function GetTwitterResponseAsTweets(ByVal ResourceURL As String, ByVal Parameters As List(Of KeyValuePair(Of String, String))) As List(Of Tweet)
			Dim strResponseData As String = GetTwitterResponseString(ResourceURL, Parameters)
			If String.IsNullOrEmpty(strResponseData) Then
				Return Nothing
			Else
				Return ParseTwitterResponseString(strResponseData)
			End If
		End Function

		Protected Shared Function ParseTwitterResponseString(ByVal ResponseData As String) As List(Of Tweet)
			If String.IsNullOrEmpty(ResponseData) Then Return Nothing

			Dim objSerializer As New System.Web.Script.Serialization.JavaScriptSerializer
			Try
				Dim lstNativeTwitterData As List(Of NativeTwitterData) = objSerializer.Deserialize(Of List(Of NativeTwitterData))(ResponseData)
				Dim lstTweet As New List(Of Tweet)
				For Each objNativeTwitterData As NativeTwitterData In lstNativeTwitterData
					lstTweet.Add(objNativeTwitterData.ConvertToTweet)
				Next

				Return lstTweet
			Catch ex As Exception
				Return Nothing
			End Try
		End Function

		Protected Shared Function RelativeTime(ByVal dtTime As DateTime) As String
			Dim timeDiff As TimeSpan = DateTime.Now.Subtract(dtTime)

			If timeDiff.TotalMinutes < 1 Then
				Return "less than a minute ago."
			ElseIf timeDiff.TotalMinutes < 2 Then
				Return "about one minute ago"
			ElseIf timeDiff.TotalMinutes < 60 Then
				Return String.Format("about {0:N0} minutes ago", timeDiff.TotalMinutes)
			ElseIf timeDiff.TotalHours < 2 Then
				Return "about an hour ago"
			ElseIf timeDiff.TotalHours < 12 Then
				Return String.Format("about {0:N0} hours ago", timeDiff.TotalHours)
			ElseIf timeDiff.TotalDays < 28 Then
				Return String.Format("{0} day{1} ago", Math.Round(timeDiff.TotalDays), If(Math.Round(timeDiff.TotalDays) = 1, "", "s"))
			Else
				Return CommonMethods.LongDateAndTimeString(dtTime, False)
			End If
		End Function

		Protected Shared Function GetTwitterResponseString(ByVal ResourceURL As String, ByVal Parameters As List(Of KeyValuePair(Of String, String))) As String
			'We're going to make a web request, passing the necessary authentication tokens to Twitter
			System.Net.ServicePointManager.Expect100Continue = False

			'loop through our parameters and build up a suitable querystring
			Dim strQueryString As String = ""
			For Each strParam As KeyValuePair(Of String, String) In Parameters
				If strQueryString = "" Then
					strQueryString &= "?"
				Else
					strQueryString &= "&"
				End If

				strQueryString &= strParam.Key & "=" & Uri.EscapeDataString(strParam.Value)
			Next

			Dim objWebRequest As System.Net.HttpWebRequest = CType(System.Net.WebRequest.Create(ResourceURL & strQueryString), System.Net.HttpWebRequest)
			objWebRequest.Headers.Add("Authorization", oAuthHeaderGet(ResourceURL, Parameters))
			objWebRequest.Method = "GET"
			objWebRequest.ContentType = "application/x-www-form-urlencoded"

			'attempt to get a response
			Try
				Dim objWebResponse As System.Net.WebResponse = objWebRequest.GetResponse()
				Return New System.IO.StreamReader(objWebResponse.GetResponseStream()).ReadToEnd()
			Catch ex As Exception
				Return String.Empty
			End Try
		End Function

		Protected Shared Function oAuthHeaderGet(ByVal ResourceURL As String, ByVal Parameters As List(Of KeyValuePair(Of String, String))) As String
			'oAuth application keys
			Dim oAuthToken As String = "441178785-LNzXonX5iDuIoFsJV2u4gP09lbQ7AC6pwheP9Cqa"
			Dim oAuthTokenSecret As String = "3bMC88sCTww1vJOjVH1e7nTl8Rkog1CG573DniezJE"
			Dim oAuthConsumerKey As String = "NGz0yX35QN1TaRQ00Hd1Cw"
			Dim oAuthConsumerSecret As String = "kgNb9zRcGGZxv4y51gauCzrI5aqXZgSPi85qCanE"

			'oAuth implementation details
			Dim oAuthVersion As String = "1.0"
			Dim oAuthSignatureMethod As String = "HMAC-SHA1"

			'Unique request details
			Dim oAuthNonce As String = Convert.ToBase64String(New ASCIIEncoding().GetBytes(DateTime.Now.Ticks.ToString()))
			Dim TimeSpan As TimeSpan = DateTime.UtcNow - New DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)
			Dim oAuthTimestamp As String = Convert.ToInt64(TimeSpan.TotalSeconds).ToString()

			'Create a list of string, string objects representing our oAuth authentication details:
			Dim lstParameters As New List(Of KeyValuePair(Of String, String))
			lstParameters.Add(New KeyValuePair(Of String, String)("oauth_consumer_key", oAuthConsumerKey))
			lstParameters.Add(New KeyValuePair(Of String, String)("oauth_nonce", oAuthNonce))
			lstParameters.Add(New KeyValuePair(Of String, String)("oauth_signature_method", oAuthSignatureMethod))
			lstParameters.Add(New KeyValuePair(Of String, String)("oauth_timestamp", oAuthTimestamp))
			lstParameters.Add(New KeyValuePair(Of String, String)("oauth_token", oAuthToken))
			lstParameters.Add(New KeyValuePair(Of String, String)("oauth_version", oAuthVersion))
			'Now add our additional params to said list:
			For Each strParam In Parameters
				lstParameters.Add(strParam)
			Next
			lstParameters.Sort(Function(kv1, kv2) kv1.Key.CompareTo(kv2.Key)) 'sort the entire list alphabetically, by key

			Dim strBaseString As String = ""
			For Each strParam In lstParameters
				If strBaseString <> "" Then strBaseString &= "&"
				strBaseString &= strParam.Key & "=" & strParam.Value
			Next

			strBaseString = String.Concat("GET&", Uri.EscapeDataString(ResourceURL), "&", Uri.EscapeDataString(strBaseString))

			Dim strCompositeKey As String = String.Concat(Uri.EscapeDataString(oAuthConsumerSecret), "&", Uri.EscapeDataString(oAuthTokenSecret))

			Dim oAuthSignature As String
			Using hasher As New System.Security.Cryptography.HMACSHA1(ASCIIEncoding.ASCII.GetBytes(strCompositeKey))
				oAuthSignature = Convert.ToBase64String(hasher.ComputeHash(ASCIIEncoding.ASCII.GetBytes(strBaseString)))
			End Using

			' create the request header
			Dim strHeaderFormat As String = "OAuth oauth_nonce=""{0}"", oauth_signature_method=""{1}"", " &
			  "oauth_timestamp=""{2}"", oauth_consumer_key=""{3}"", " &
			  "oauth_token=""{4}"", oauth_signature=""{5}"", " &
			  "oauth_version=""{6}"""

			Return String.Format(strHeaderFormat,
			 Uri.EscapeDataString(oAuthNonce),
			 Uri.EscapeDataString(oAuthSignatureMethod),
			 Uri.EscapeDataString(oAuthTimestamp),
			 Uri.EscapeDataString(oAuthConsumerKey),
			 Uri.EscapeDataString(oAuthToken),
			 Uri.EscapeDataString(oAuthSignature),
			 Uri.EscapeDataString(oAuthVersion))
		End Function
	End Class
End Namespace