Imports System.Runtime.Serialization
Namespace Model

    <DataContract()>
    Public Class PayRollOrg
        Inherits SsCommon.BaseDto

        '<DataMember()> _
        'Public Property CompLst As List(Of SsHrCommon.DTO.CompanyTbl)

        <DataMember()> _
        Public Property Org As String

        <DataMember()> _
        Public Property Branch As String

        <DataMember()> _
        Public Property PayOr As String

        <DataMember()> _
        Public Property PayOrgLst As List(Of SsHrCommon.DTO.Pyorganize)

        <DataMember()> _
        Public Property BranchLst As List(Of SsHrCommon.DTO.Pyorganize)

        <DataMember()> _
        Public Property OrLst As List(Of SsHrCommon.DTO.Pyorganize)

    End Class
End Namespace