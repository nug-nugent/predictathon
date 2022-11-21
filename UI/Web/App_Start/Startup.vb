Imports Microsoft.Owin
Imports Owin

<Assembly: OwinStartup(GetType(Predictathon.Startup))>
Namespace Predictathon
    Public Class Startup
        Public Sub Configuration(app As IAppBuilder)
            app.MapSignalR()
        End Sub
    End Class
End Namespace