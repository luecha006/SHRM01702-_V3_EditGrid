Imports System.Runtime.Serialization

Namespace Model

    <DataContract()>
    Public Class SearchModel
        Inherits SsCommon.BaseDto
        <DataMember()>
        Public Property Emplid As String

        <DataMember()>
        Public Property Firstname As String

    End Class

End Namespace
