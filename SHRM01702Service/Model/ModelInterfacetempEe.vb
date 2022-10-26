Imports System.Runtime.Serialization
Namespace Model

    '<DataContract()>
    Public Class ModelInterfacetempEe
        Inherits SsHrCommon.DTO.InterfacetempEe

        '<DataMember()> _
        Public Property Employeename As String

        <DataMember()> _
        Public Property DeptName As String

        <DataMember()> _
        Public Property PositionName As String

        <DataMember()> _
        Public Property SelEmpl As Boolean

        <DataMember()> _
        Public Property DbStatus As String

        <DataMember()> _
        Public Property RowColor As String

        <DataMember()> _
        Public Property SatWork As String

        <DataMember()> _
        Public Property MarryStatusName As String

        'Add by Chanchira L. on 09/10/2020
        <DataMember()> _
        Public Property chk As Boolean

        <DataMember()> _
        Public Property _IsEnable As Boolean

        'Add by show grig for call api
        <DataMember()> _
        Public Property t_Idno_EmpId As String

        <DataMember()> _
        Public Property t_Idno_PayrollId As String

        <DataMember()> _
        Public Property t_Name As String

        <DataMember()> _
        Public Property t_Mar As String

        <DataMember()> _
        Public Property t_Bac_BankAccount As String

    End Class
End Namespace
