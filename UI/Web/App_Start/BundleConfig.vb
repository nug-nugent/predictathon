Imports System.Web.Optimization

Public Class BundleConfig
    Public Shared Sub RegisterBundles(bundles As BundleCollection)
        bundles.Add(New ScriptBundle("~/js/matchlist").
                    Include("~/Scripts/Dist/match-list.bundle.js"))

        bundles.Add(New StyleBundle("~/Styles/predictathon").
                    Include("~/Styles/Predictathon.css"))

        bundles.Add(New StyleBundle("~/Styles/ThirdParty/bundle").
                    IncludeDirectory("~/Styles/ThirdParty", "*.css"))
    End Sub
End Class
