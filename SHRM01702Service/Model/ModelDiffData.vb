Imports System.Runtime.Serialization
Namespace Model

    <DataContract()>
    Public Class ModelDiffData
        Inherits SsCommon.BaseDto


        <DataMember()> _
        Public Property emplid As String
         
        <DataMember()> _
        Public Property dateofrecord As Date
          
    End Class
End Namespace
