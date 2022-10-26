Imports System.Runtime.Serialization
Namespace Model

    <DataContract()>
    Public Class ModelHrpayrollInterface
        Inherits SsHrCommon.DTO.Hrpayrollinterface

        <DataMember()> _
        Public Property Managerlvl As String
            

        End Class
End Namespace