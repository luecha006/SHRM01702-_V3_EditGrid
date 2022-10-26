Imports System.Runtime.Serialization
Namespace Model

    <DataContract()>
    Public Class Model
        Inherits SsCommon.BaseDto

        <DataMember()> _
        Public Property Pyperiod As SsHrCommon.DTO.Pyperiod

        <DataMember()> _
        Public Property OrgLst As List(Of SsHrCommon.DTO.Pyorganize)

    End Class
End Namespace
