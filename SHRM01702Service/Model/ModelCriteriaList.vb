Imports System.Runtime.Serialization
Namespace Model

    <DataContract()>
    Public Class ModelCriteriaList
        Inherits SsCommon.BaseDto

        <DataMember()> _
        Public Property Company As String
         
        <DataMember()> _
        Public Property Month As String
         
        <DataMember()> _
        Public Property Year As String
          
        <DataMember()> _
        Public Property EmplClass As String
            
        <DataMember()> _
        Public Property EmplFlag As String
          
        <DataMember()> _
        Public Property Managerlvl As String
           
        <DataMember()> _
        Public Property StartDate As Date?
          
        <DataMember()> _
        Public Property EndDate As Date?
         
        <DataMember()> _
        Public Property PeriodTime As Integer?
          
        <DataMember()> _
        Public Property Emplid As String
          
        <DataMember()> _
        Public Property PeriodMastId As ModelPyperiodmaster
          
        <DataMember()> _
        Public Property Periodid As Decimal?

        <DataMember()> _
        Public Property Org As SsHrCommon.DTO.Pyorganize

        'Add by Chanchira L. on 14/10/2020
        <DataMember()> _
        Public Property chkAll As Boolean


        <DataMember()> _
        Public Property PayBranch As String

        <DataMember()> _
        Public Property PayOrg As String

    End Class
End Namespace
