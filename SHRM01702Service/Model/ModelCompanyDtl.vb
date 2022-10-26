Imports System.Runtime.Serialization
Namespace Model

    <DataContract()>
    Public Class ModelCompanyDtl
        Inherits SsCommon.BaseDto

        <DataMember()> _
        Public Property Company As String

        <DataMember()> _
        Public Property Effdt As Date

        <DataMember()> _
        Public Property Month As String

        <DataMember()> _
        Public Property NosatWorkday As Decimal?

        <DataMember()> _
        Public Property SatWorkday As Decimal?

    End Class
End Namespace

