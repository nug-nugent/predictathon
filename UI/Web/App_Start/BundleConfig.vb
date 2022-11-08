Imports System.Web.Optimization

Public Class BundleConfig
    Public Shared Sub RegisterBundles(bundles As BundleCollection)
        bundles.Add(New ScriptBundle("~/js/matchlist").
                    Include("~/Scripts/Dist/match-list.bundle.js"))
    End Sub
End Class
