Imports System.Web.Optimization

Public Class BundleConfig
    Public Shared Sub RegisterBundles(bundles As BundleCollection)
        bundles.Add(New ScriptBundle("~/js/matchlist").
                    Include("~/Scripts/Dist/match-list.bundle.js"))

        bundles.Add(New ScriptBundle("~/js/messagelist").
                    Include("~/Scripts/Dist/message-list.bundle.js"))

        bundles.Add(New StyleBundle("~/Styles/predictathon").
                    Include("~/Styles/Predictathon.css"))
    End Sub
End Class
