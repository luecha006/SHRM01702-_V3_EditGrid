Imports SsSilverlight
<SsSilverlight.ExportWidget(ProgramCode:="SHRM01702", FormId:="GdLoader", IsDefaultPage:=True)>
Partial Public Class GdLoader
    Inherits UserControl
    Implements IClosableUC


    Public Event CloseEvent(sender As Object, dialogResult As Boolean?) Implements SsSilverlight.IClosableUC.CloseEvent
    Private WithEvents GdCaching As New SsHrCommon.GdBp

    Public Sub New()
        InitializeComponent()

        GdCaching.AddCachLst("Month")
        GdCaching.AddCachLst("InterfaceFlag")
        GdCaching.AddCachLst("ManagerLevel")
        GdCaching.AddCachLst("EmpType")
        GdCaching.GetLstAsync()

    End Sub

    Public Property IsBusy() As Boolean
        Get
            Return Me.busyIndicator.IsBusy
        End Get
        Set(ByVal value As Boolean)
            If value Then
                If Not Me.busyIndicator.IsBusy Then
                    Me.busyIndicator.IsBusy = value
                End If
            Else
                Me.busyIndicator.IsBusy = value
            End If
        End Set
    End Property


    Private Sub GdCaching_GetLstAsyncCompleted(ByVal userState As Object) Handles GdCaching.GetLstAsyncCompleted
        Me.IsBusy = False
        Me.LayoutRoot.Children.Add(New DetailView)
    End Sub

    Private Sub GdLoader_Loaded(ByVal sender As Object, ByVal e As System.Windows.RoutedEventArgs) Handles Me.Loaded

    End Sub


End Class
