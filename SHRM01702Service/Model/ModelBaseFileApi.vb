Imports System.Runtime.Serialization
Namespace Model
    Public Class ModelBaseFileApi
        Public Property RequestId As String
        Public Property dateLength As String
        Public Property data As String
        Public Property parameter As Parameter
    End Class

    Public Class Parameter
        Public Property RequestId As String
        Public Property companyCode As String
        Public Property StartPeriod As String
        Public Property EndPeriod As String
        Public Property EmployeeID As String
        Public Property OP As String
        Public Property Address As String
        Public Property BookBank As String
        Public Property SSN As String
        Public Property BankBranch As String
        Public Property TaxID As String
    End Class
End Namespace


