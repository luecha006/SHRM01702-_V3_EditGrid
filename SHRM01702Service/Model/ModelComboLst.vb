Imports System.Runtime.Serialization

Namespace Model

    <DataContract()>
    Public Class ModelComboLst
        Inherits SsCommon.BaseDto

        <DataMember()>
        Public Property key As String

        <DataMember()>
        Public Property Name As String

    End Class

End Namespace
