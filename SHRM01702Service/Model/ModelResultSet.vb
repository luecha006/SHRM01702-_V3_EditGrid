Imports System.Runtime.Serialization
Namespace Model

    <DataContract()>
    Public Class ModelResultSet
        Inherits SsCommon.BaseDto

        <DataMember()> _
        Public Property EmplidNo As String
        
        <DataMember()> _
        Public Property Company As String
          
        <DataMember()> _
        Public Property Month As String

        <DataMember()> _
        Public Property Year As Integer

        <DataMember()> _
        Public Property EmplClass As String
       
        <DataMember()> _
        Public Property EmplFlag As String
        
        <DataMember()> _
        Public Property LastEmplFlag As String
       
        <DataMember()> _
        Public Property EmplSel As String
       
        <DataMember()> _
        Public Property PeriodTime As Integer
         
        <DataMember()> _
        Public Property EmplRcd As Integer
          
        <DataMember()> _
        Public Property Managerlvl As String

    End Class
End Namespace