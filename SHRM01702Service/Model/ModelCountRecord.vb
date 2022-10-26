Imports System.Runtime.Serialization
Namespace Model

    <DataContract()>
    Public Class ModelCountRecord
        Inherits SsCommon.BaseDto

        <DataMember()> _
        Public Property CntMaster As Integer
            
        <DataMember()> _
        Public Property CntTran As Integer
            
    End Class
End Namespace


