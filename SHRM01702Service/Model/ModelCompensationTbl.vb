Imports System.Runtime.Serialization
Namespace Model

    <DataContract()>
    Public Class ModelCompensationTbl
        Inherits SsHrCommon.DTO.CompensationTbl

        <DataMember()> _
        Public Property BaseSalary As String
        
        <DataMember()> _
        Public Property Increasingyn As String
          
    End Class
End Namespace
