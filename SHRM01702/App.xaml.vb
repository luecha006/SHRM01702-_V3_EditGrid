Imports SsHrCommon
Imports SsCommon

Partial Public Class App
    Inherits Application

    Public Sub New()
        InitializeComponent()
    End Sub

    Private Sub Application_Startup(ByVal o As Object, ByVal e As StartupEventArgs) Handles Me.Startup
        Telerik.Windows.Controls.StyleManager.ApplicationTheme = New Telerik.Windows.Controls.Office_SilverTheme
        'Load ApplicationContext

        SsAppCommon.SsAppContext.Instance.SetInitParameter(e.InitParams)
        SsHrCommon.SsHrContext.Instance.SetId = "VNCPG"
        SsHrCommon.SsHrContext.Instance.Lang = "en-US"
        SsHrCommon.SsHrContext.Instance.LocalLang = "vi-VN"
        SsHrCommon.SsHrContext.Instance.UserName = "kitti.jee"
        Dim view As New SsAppCommon.MainTemplateView
        RemoveHandler view.LoadProgramContextCompleted, AddressOf view_LoadContextCompleted
        AddHandler view.LoadProgramContextCompleted, AddressOf view_LoadContextCompleted
        Me.RootVisual = view
        view.LoadProgramContextAsync("SHRM01702")
    End Sub

    Private Sub view_LoadContextCompleted(ByVal sender As Object)
        CType(sender, SsAppCommon.MainTemplateView).Navigate(New GdLoader())
    End Sub

    Private Sub Application_Exit(ByVal o As Object, ByVal e As EventArgs) Handles Me.Exit

    End Sub

    Private Sub Application_UnhandledException(ByVal sender As Object, ByVal e As ApplicationUnhandledExceptionEventArgs) Handles Me.UnhandledException

        ' If the app is running outside of the debugger then report the exception using
        ' the browser's exception mechanism. On IE this will display it a yellow alert 
        ' icon in the status bar and Firefox will display a script error.
        If Not System.Diagnostics.Debugger.IsAttached Then

            ' NOTE: This will allow the application to continue running after an exception has been thrown
            ' but not handled. 
            ' For production applications this error handling should be replaced with something that will 
            ' report the error to the website and stop the application.

            Deployment.Current.Dispatcher.BeginInvoke(New Action(Of ApplicationUnhandledExceptionEventArgs)(AddressOf ReportErrorToDOM), e)
        End If
        SsSilverlight.ViewModelUIService.ExceptionDlg(e.ExceptionObject)
        e.Handled = True
    End Sub

    Private Sub ReportErrorToDOM(ByVal e As ApplicationUnhandledExceptionEventArgs)

        Try
            Dim errorMsg As String = e.ExceptionObject.Message + e.ExceptionObject.StackTrace
            errorMsg = errorMsg.Replace(""""c, "'"c).Replace(ChrW(13) & ChrW(10), "\n")

            System.Windows.Browser.HtmlPage.Window.Eval("throw new Error(""Unhandled Error in Silverlight Application " + errorMsg + """);")
        Catch

        End Try
    End Sub

End Class



'Partial Public Class App
'    Inherits Application

'    Public Sub New()
'        InitializeComponent()
'    End Sub

'    Private Sub Application_Startup(ByVal o As Object, ByVal e As StartupEventArgs) Handles Me.Startup
'        Telerik.Windows.Controls.StyleManager.ApplicationTheme = New Telerik.Windows.Controls.Office_SilverTheme
'        'Load ApplicationContext
'        SsHrContext.Instance.SetInitParameter(e.InitParams)
'        SsHrCommon.SsHrContext.Instance.SetId = "VNCPG"
'        SsHrCommon.SsHrContext.Instance.Lang = "en-US"
'        SsHrCommon.SsHrContext.Instance.LocalLang = "vi-VN"
'        SsHrCommon.SsHrContext.Instance.UserName = "kitti.jee"
'        Dim view As New SsAppCommon.MainTemplateView
'        RemoveHandler view.LoadProgramContextCompleted, AddressOf view_LoadContextCompleted
'        AddHandler view.LoadProgramContextCompleted, AddressOf view_LoadContextCompleted
'        Me.RootVisual = view
'        view.LoadProgramContextAsync("SHRM01702")
'    End Sub

'    Private Sub view_LoadContextCompleted(ByVal sender As Object)
'        CType(sender, SsAppCommon.MainTemplateView).Navigate(New DetailView())
'    End Sub

'    Private Sub Application_Exit(ByVal o As Object, ByVal e As EventArgs) Handles Me.Exit

'    End Sub

'    Private Sub Application_UnhandledException(ByVal sender As Object, ByVal e As ApplicationUnhandledExceptionEventArgs) Handles Me.UnhandledException

'        ' If the app is running outside of the debugger then report the exception using
'        ' the browser's exception mechanism. On IE this will display it a yellow alert 
'        ' icon in the status bar and Firefox will display a script error.
'        If Not System.Diagnostics.Debugger.IsAttached Then

'            ' NOTE: This will allow the application to continue running after an exception has been thrown
'            ' but not handled. 
'            ' For production applications this error handling should be replaced with something that will 
'            ' report the error to the website and stop the application.

'            Deployment.Current.Dispatcher.BeginInvoke(New Action(Of ApplicationUnhandledExceptionEventArgs)(AddressOf ReportErrorToDOM), e)
'        End If
'        SsSilverlight.ViewModelUIService.ExceptionDlg(e.ExceptionObject)
'        e.Handled = True
'    End Sub

'    Private Sub ReportErrorToDOM(ByVal e As ApplicationUnhandledExceptionEventArgs)

'        Try
'            Dim errorMsg As String = e.ExceptionObject.Message + e.ExceptionObject.StackTrace
'            errorMsg = errorMsg.Replace(""""c, "'"c).Replace(ChrW(13) & ChrW(10), "\n")

'            System.Windows.Browser.HtmlPage.Window.Eval("throw new Error(""Unhandled Error in Silverlight Application " + errorMsg + """);")
'        Catch

'        End Try
'    End Sub

'End Class
