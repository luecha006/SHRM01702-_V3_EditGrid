Namespace SHRM01702Ref

    Partial Public Class ModelInterfacetempEe
        Public Function IsDirty() As Boolean
            Dim ret As Boolean = False
            If Me.IsPropertiesDirty Then
                Return True
            End If
            Return False
        End Function

        Public Function GetDirtyModel() As ModelInterfacetempEe
        End Function

        Protected Overrides Sub OnPropertyChanged(ByVal strPropertyName As String)
            MyBase.OnPropertyChanged(strPropertyName)
            Me.RaisePropertyChanged(strPropertyName)
        End Sub

        Protected Overrides Sub OnCreateNew()
            MyBase.OnCreateNew()
            Me.Effdt = Today
        End Sub

        Protected Overrides Sub OnInitialize()
            MyBase.OnInitialize()
        End Sub

        Protected Overrides Sub OnClone(ByVal ret As Object)
            MyBase.OnClone(ret)
        End Sub

        Protected Overrides Sub OnHandlePropertyChanged(ByVal sender As Object, ByVal e As System.ComponentModel.PropertyChangedEventArgs)
            MyBase.OnHandlePropertyChanged(sender, e)
        End Sub

        Protected Overrides Sub OnHandleValidatedProperty(ByVal sender As Object, ByVal Propertyname As String)
        
        End Sub

        Protected Overrides Sub OnGetChildrenErrorMsg(ByRef res As System.Collections.Generic.List(Of String))

        End Sub

        Private _IsEnable As Boolean
        Public Property IsEnable() As Boolean
            Get
                Return _IsEnable
            End Get
            Set(ByVal value As Boolean)
                _IsEnable = value
            End Set
        End Property

    End Class

End Namespace