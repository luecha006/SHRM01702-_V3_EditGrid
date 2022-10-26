Imports System.Runtime.Serialization
Namespace Model

    <DataContract()>
    Public Class ModelPyperiodmaster
        Inherits SsHrCommon.DTO.Pyperiodmaster

        <DataMember()> _
        Public Property Description As String

    End Class
End Namespace