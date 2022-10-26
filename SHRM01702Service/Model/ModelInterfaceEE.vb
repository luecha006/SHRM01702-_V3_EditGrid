Imports System.Runtime.Serialization
Namespace Model

    <DataContract()>
    Public Class ModelInterfaceEE
        Inherits SsCommon.BaseDto

        <DataMember()> _
        Public Property Filename As String

        <DataMember()> _
        Public Property TTyp As String

        <DataMember>
        Public Property TFunc As String

        <DataMember()> _
        Public Property TCom As String

        <DataMember()> _
        Public Property TCode As String

        <DataMember()> _
        Public Property TTitle As String

        <DataMember()> _
        Public Property TName As String

        <DataMember()> _
        Public Property tTAdd1 As String

        <DataMember()> _
        Public Property TAdd2 As String

        <DataMember()> _
        Public Property TOpe As String

        <DataMember()> _
        Public Property TBrh As String

        <DataMember()> _
        Public Property TOrg As String

        <DataMember()> _
        Public Property TShf As Integer

        <DataMember()> _
        Public Property TTaxid As String

        <DataMember()> _
        Public Property TSex As Integer

        <DataMember()> _
        Public Property TMar As Integer

        <DataMember()> _
        Public Property TChildtotal As Integer

        <DataMember()> _
        Public Property TChildscholl As Integer

        <DataMember()> _
        Public Property TDedinsurance As String

        <DataMember()> _
        Public Property TDedhomeinterest As String

        <DataMember()> _
        Public Property TDedprovidence As String

        <DataMember()> _
        Public Property TDeddonation As String

        <DataMember()> _
        Public Property TDeddonation2 As String

        <DataMember()> _
        Public Property TIdno As String

        <DataMember()> _
        Public Property TScno As String

        <DataMember()> _
        Public Property TSta As Integer

        <DataMember()> _
        Public Property TBdate As String

        <DataMember()> _
        Public Property TEdate As String

        <DataMember()> _
        Public Property TFdate As String

        <DataMember()> _
        Public Property TTdate As String

        <DataMember()> _
        Public Property TRdate As String

        <DataMember()> _
        Public Property TPos As Integer

        <DataMember()> _
        Public Property TPc As String

        <DataMember()> _
        Public Property TBno As String

        <DataMember()> _
        Public Property TBty As Integer

        <DataMember()> _
        Public Property TBac As String

        <DataMember()> _
        Public Property TTaxcalmethod As Integer

        <DataMember()> _
        Public Property TNocalsocial As Integer

        <DataMember()> _
        Public Property TDedltf As Integer

        <DataMember()> _
        Public Property TDedrmf As Integer

        <DataMember()> _
        Public Property TDedfather As Integer

        <DataMember()> _
        Public Property TFatherid As String

        <DataMember()> _
        Public Property TMotherid As Integer

        <DataMember()> _
        Public Property TTitleCouple As String

        <DataMember()> _
        Public Property TNameCouple As String

        <DataMember()> _
        Public Property TSurnCouple As String

        <DataMember()> _
        Public Property TBdateCouple As Integer

        <DataMember()> _
        Public Property TIdCouple As Integer

        <DataMember()> _
        Public Property TIdCoupleFat As Integer

        <DataMember()> _
        Public Property TIdCoupleMot As Integer

        <DataMember()> _
        Public Property TSaly As String

        <DataMember()> _
        Public Property TBonusrate As String

        <DataMember()> _
        Public Property TPorovidenDdt As Integer

        <DataMember()> _
        Public Property TDedEmpPer As Integer

        <DataMember()> _
        Public Property TDedComPer As Integer

        <DataMember()> _
        Public Property TPalw As String

        <DataMember()> _
        Public Property TSalw1 As String

        <DataMember()> _
        Public Property TMalw1 As String

        <DataMember()> _
        Public Property TFalw1 As String

        <DataMember()> _
        Public Property TTelw1 As String

        <DataMember()> _
        Public Property TOther As String

        <DataMember()> _
        Public Property TSpec1 As String

        <DataMember()> _
        Public Property THelp1 As String

        <DataMember()> _
        Public PropertytMded1 As String

        <DataMember()> _
        Public Property TOded1 As String

        <DataMember()> _
        Public Property TTrcom As Integer

        <DataMember()> _
        Public Property TTrcode As Integer

        <DataMember()> _
        Public Property TTrope As Integer

        <DataMember()> _
        Public Property TTrbrh As Integer

        <DataMember()> _
        Public Property TTrorg As Integer

        <DataMember()> _
        Public Property TTrshf As Integer

        <DataMember()> _
        Public Property TTrdate As String

        <DataMember()> _
        Public Property TSlded As String

        <DataMember()> _
        Public Property TSlbal As String

        <DataMember()> _
        Public Property TGsb As String

        <DataMember()> _
        Public Property TLlded As String

        <DataMember()> _
        Public Property TLlbal As String

        <DataMember()> _
        Public Property THarw1 As String

        <DataMember()> _
        Public Property THouw1 As String

        <DataMember()> _
        Public Property TSpaw1 As String

        <DataMember()> _
        Public Property TOinc1 As String

        <DataMember()> _
        Public Property TVehw1 As String

        <DataMember()> _
        Public Property TPemwf As String

        <DataMember()> _
        Public Property TPcpwf As String

        <DataMember()> _
        Public Property THealthy As String

        <DataMember()> _
        Public Property TPempv As String

        <DataMember()> _
        Public Property TPcppv As String

        <DataMember()> _
        Public Property TBkname As String

        <DataMember()> _
        Public Property TBkbrname As String

        <DataMember()> _
        Public Property TYinc As String

        <DataMember()> _
        Public Property TYainc1 As String

        <DataMember()> _
        Public Property TYainc2 As String

        <DataMember()> _
        Public Property TYainc3 As String

        <DataMember()> _
        Public Property TYtax As String

        <DataMember()> _
        Public Property TYatax1 As String

        <DataMember()> _
        Public Property TYatax2 As String

        <DataMember()> _
        Public Property TYatax3 As String

        <DataMember()> _
        Public Property TYpalw As String

        <DataMember()> _
        Public Property TYsalw As String

        <DataMember()> _
        Public Property TYmalw As String

        <DataMember()> _
        Public Property TYfalw As String

        <DataMember()> _
        Public Property TYharw As String

        <DataMember()> _
        Public Property TYhouw As String

        <DataMember()> _
        Public Property TYtelw As String

        <DataMember()> _
        Public Property TYspaw As String

        <DataMember()> _
        Public Property TYoinc As String

        <DataMember()> _
        Public Property TYmded As String

        <DataMember()> _
        Public Property TYoded As String

        <DataMember()> _
        Public Property TYot As String

        <DataMember()> _
        Public Property TYrw As String

        <DataMember()> _
        Public Property TYlate As String

        <DataMember()> _
        Public Property TYslded As String

        <DataMember()> _
        Public Property TYllded As String

        <DataMember()> _
        Public Property TYlvded As String

        <DataMember()> _
        Public Property TYadj As String

        <DataMember()> _
        Public Property TYemwf As String

        <DataMember()> _
        Public Property TYcpwf As String

        <DataMember()> _
        Public Property TYempv As String

        <DataMember()> _
        Public Property TYcppv As String

        <DataMember()> _
        Public Property TYhealthy As String

        <DataMember()> _
        Public Property THarmful As Integer

        <DataMember()> _
        Public Property TWorkhour As Integer

        <DataMember()> _
        Public Property TNid As String

        <DataMember()> _
        Public Property SyncStatus As String

        <DataMember()> _
        Public Property ErrMessage As String

        <DataMember()> _
        Public Property Createuser As String

        <DataMember()> _
        Public Property Accessdate As Date

        <DataMember()> _
        Public Property Programcode As String

        <DataMember()> _
        Public Property SyncOption As String

        <DataMember()> _
        Public Property Emplid As String

        <DataMember()> _
        Public Property Periodid As Integer

        <DataMember()> _
        Public Property Line As String

        <DataMember()> _
        Public Property Workingday As Integer

        <DataMember()> _
        Public Property WorkingdayStatus As String

        <DataMember()> _
        Public Property Hourperday As Integer

        <DataMember()> _
        Public Property HourperdayStatus As String



    End Class
End Namespace

