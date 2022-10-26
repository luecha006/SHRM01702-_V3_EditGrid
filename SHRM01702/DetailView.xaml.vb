Imports System.Collections.ObjectModel
Imports SHRM01702.SHRM01702Ref
Imports SsSilverlight
Imports SsCommon
Imports SsAppCommon
Imports SsAppCommon.DTO
Imports SsHrCommon
Imports SsHrCommon.DTO
Imports Telerik.Windows.Controls
Imports Infragistics.Documents.Excel
Imports System.IO
Imports System.IO.IsolatedStorage
Imports System.Windows.Browser
Imports ServiceClient = SHRM01702.SHRM01702Ref.SHRM01702ServiceClient
Imports System.Globalization
Imports Telerik.Windows.Zip



<SsSilverlight.ExportWidget(ProgramCode:="SHRM01702", FormId:="DetailView", IsDefaultPage:=True)>
Partial Public Class DetailView
    Implements IClosableUC

    Public Event CloseEvent(sender As Object, dialogResult As Boolean?) Implements IClosableUC.CloseEvent
    Private tpCon As SearchTb
    Dim IsSmartpay As String = "Y"
    Dim IsGenText As String = "Y"
    Dim pathServer As String = ""
    Dim Company As String = ""

    Public Property Vm() As DetailViewModel
        Get
            Return DataContext
        End Get
        Set(ByVal value As DetailViewModel)
            DataContext = value
        End Set
    End Property

    Public Property IsBusy() As Boolean
        Get
            Return Me.busyIndicator.IsBusy
        End Get
        Set(ByVal value As Boolean)
            If value Then
                If Not Me.busyIndicator.IsBusy Then
                    Me.busyIndicator.IsBusy = value
                End If
            Else
                Me.busyIndicator.IsBusy = value
            End If
        End Set
    End Property

#Region "Events"

    Private Sub btnClose_Click(ByVal sender As Object, ByVal e As System.Windows.RoutedEventArgs) Handles btnClose.Click
        RaiseEvent CloseEvent(Me, False)
        tpCon.CloseProgram("SHRM01702")
    End Sub

    Private Sub EditListView_Loaded(ByVal sender As Object, ByVal e As System.Windows.RoutedEventArgs) Handles Me.Loaded
        If Me.DataContext Is Nothing OrElse Not (TypeOf (Me.DataContext) Is DetailViewModel) Then
            Me.Vm = New DetailViewModel
        End If
        Me.tpCon = New SearchTb(Me)
        tpCon.SetBtnClose(Me.btnClose)

        Try
#If DEBUG Then
            pathServer = "D:\Temp\HRMSFILE"
#Else
            pathServer = SsHrCommon.SsHrContext.Instance.GetKeyValue("TARGETPATH")
#End If
        Catch ex As Exception
            ViewModelUIService.WarningDlg("Please Confix Fw_Ini Program code = 'HRMODULE' and Key_Name = 'TARGETPATH'")
        End Try

        Try
#If DEBUG Then
            IsSmartpay = "N"
#Else
              IsSmartpay = SsHrCommon.SsHrContext.Instance.GetKeyValue("IS_SMART_PAY")
#End If

        Catch ex As Exception
            ViewModelUIService.WarningDlg("Please Confix Fw_Ini Program code = 'HRMODULE' and Key_Name = 'IS_SMART_PAY'")
        End Try
        Me.Dispatcher.BeginInvoke(New Action(AddressOf LoadPeriodMast))

    End Sub

#Region "Company"
    Private Sub lkCompany_SelectedItemChanged(sender As Object, e As SelectedItemChangedArg) Handles lkCompany.SelectedItemChanged
        If e.NewItem IsNot Nothing Then
            If Company <> e.NewItem.Code Then
                lblCompDescr.Content = e.NewItem.Descr1
                SetDefaultPeriodid()
            End If
        Else
            lblCompDescr.Content = Nothing
        End If

        Company = e.NewItem.Code
    End Sub

    Private Sub lkCompany_ValidatingInput(sender As Object, e As ValidatingInputArg) Handles lkCompany.ValidatingInput
        Dim _param As New SsCommon.ServiceParam
        Dim sqlText As String = String.Empty

        If _param.LocalLang <> _param.Lang Then

            sqlText = "SELECT a.company AS CODE, NVL (b.descr, a.descr) AS NAME FROM company_tbl a LEFT OUTER JOIN company_lang b ON a.company = b.company AND a.effdt = b.effdt AND B.LANGUAGE_CD = :lang WHERE a.effdt = (SELECT MAX (effdt) FROM company_tbl WHERE company = a.company AND effdt <= CURRENT_DATE ) AND a.company = :code ORDER BY a.company "
        Else
            sqlText = "SELECT a.company AS CODE, a.descr NAME FROM company_tbl a WHERE a.effdt = (SELECT MAX (effdt) FROM company_tbl WHERE company = a.company AND effdt <= CURRENT_DATE ) AND a.company = :code ORDER BY a.company "
        End If

        e.SelectStatement = sqlText

        e.SelectParams.Add("CODE", e.InputValue)
    End Sub
#End Region

#Region "Combobox"
    Private Sub cboOrg_SelectionChanged(sender As Object, e As SelectionChangedEventArgs)
        If Vm.Criteria.Company IsNot Nothing And Vm.Criteria.Org IsNot Nothing Then
            Me.Dispatcher.BeginInvoke(New Action(AddressOf GetPayBrList))
        Else
            'Me.cboBranch.ItemsSource = Nothing
        End If
    End Sub

    Private Sub cboBranch_SelectionChanged(sender As Object, e As SelectionChangedEventArgs)
        If Vm.Criteria.PayBranch IsNot Nothing Then
            Me.Dispatcher.BeginInvoke(New Action(AddressOf GetPayOrList))
        Else
            'Me.cboOr.ItemsSource = Nothing
        End If
    End Sub

    Private Sub GetPayBrList()
        Dim bp = Me.GetBp
        RemoveHandler bp.FindPayBranchCompleted, AddressOf _bp_FindPayBranchCompleted
        AddHandler bp.FindPayBranchCompleted, AddressOf Me._bp_FindPayBranchCompleted
        Dim param As New SsCommon.ServiceParam
        bp.FindPayBranchAsync(param, Vm.Criteria.Company, Vm.Criteria.Org.Orgcode, Me.IsSmartpay)
    End Sub
    Private Sub _bp_FindPayBranchCompleted(sender As Object, e As FindPayBranchCompletedEventArgs)
        If e.Error IsNot Nothing Then
            Throw e.Error
        Else
            'Me.cboBranch.ItemsSource = e.Result
        End If
    End Sub

    Private Sub GetPayOrList()
        Dim bp = Me.GetBp
        RemoveHandler bp.FindPayORCompleted, AddressOf _bp_FindPayORCompleted
        AddHandler bp.FindPayORCompleted, AddressOf Me._bp_FindPayORCompleted
        Dim param As New SsCommon.ServiceParam
        bp.FindPayORAsync(param, Vm.Criteria.Company, Vm.Criteria.Org.Orgcode, Vm.Criteria.PayBranch, Me.IsSmartpay)
    End Sub
    Private Sub _bp_FindPayORCompleted(sender As Object, e As FindPayORCompletedEventArgs)
        If e.Error IsNot Nothing Then
            Throw e.Error
        Else
            'Me.cboOr.ItemsSource = e.Result
        End If
    End Sub

#End Region

#Region "Payroll Period"
    Private Sub cboMonth_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles cboMonth.SelectionChanged
        Me.SetPeriodid()
    End Sub

    Private Sub txtYear_TextChanged(ByVal sender As Object, ByVal e As System.Windows.Controls.TextChangedEventArgs) Handles txtYear.TextChanged
        Me.SetPeriodid()
    End Sub

    Private Sub cboPayDate_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles cboPayDate.SelectionChanged
        Me.SetPeriodid()
    End Sub

    Private Sub cboPeriodMastId_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles cboPeriodMastId.SelectionChanged
        Me.SetPeriodid()
    End Sub

#End Region



    Private Sub btnSearch_Click(ByVal sender As Object, ByVal e As System.Windows.RoutedEventArgs) Handles btnSearch.Click
        gridShowGrid.Visibility = Windows.Visibility.Visible
        If ValidateSearch() = False Then
            Exit Sub
        End If
        Me.Vm.NewEmplNum = 0
        Me.Vm.ReEmplNum = 0
        Me.Vm.TranEmplNum = 0
        Me.Vm.UpEmplNum = 0

        Me.IsBusy = True
        Dim bp = Me.GetBp
        RemoveHandler bp.SearchCompleted, AddressOf _bp_SearchCompleted
        AddHandler bp.SearchCompleted, AddressOf _bp_SearchCompleted
        Dim param As New SsCommon.ServiceParam
        bp.SearchAsync(param, Vm.Criteria, pathServer)

    End Sub

    Private Sub btnCancel_Click(ByVal sender As Object, ByVal e As System.Windows.RoutedEventArgs) Handles btnCancel.Click
        Me.gridShowGrid.Visibility = Windows.Visibility.Collapsed
        Me.Vm.ModelList.Clear()
        Me.btnSave.IsEnabled = False
        ' luecha
        'Me.btnNewGenText.IsEnabled = False
        Me.btnSearch.IsEnabled = True
        Vm.Criteria.Year = Today.Year
        Vm.Criteria.EmplFlag = "A"
        Vm.Criteria.Month = String.Format("{0:0#}", Today.Month)
        Vm.Criteria.PeriodTime = 1
        Vm.Criteria.EmplClass = 1
        lblCompDescr.Content = ""
        Vm.Criteria.StartDate = Nothing
        Vm.Criteria.EndDate = Nothing
        Vm.Criteria.Org = Nothing
        Vm.Criteria.PeriodMastId = Me.Vm.PeriodMastLst.Item(0)
        Vm.Criteria.Managerlvl = Nothing
    End Sub

    Private Sub btnSave_Click(ByVal sender As Object, ByVal e As System.Windows.RoutedEventArgs) Handles btnSave.Click
        'Dim a = (From x In Vm.ModelList Where x.DbStatus <> "Y" Order By x.Emplid Select x).Count
        'Dim b = (From y In Vm.ModelList Where y.chk = True Order By y.Emplid Select y).Count
        'If a > 0 Then
        '    IsGenText = "N"
        '    ViewModelUIService.WarningDlg(SsCommon.Message.CreateMessageById(SsCommon.MessageDataResponse.DataNotSave).Text)
        '    Exit Sub
        'Else
        '    If b > 0 Then         'Add by Chanchira L. on 09/10/2020
        '        Me.IsBusy = True
        '        Dim bp = Me.GetBp
        '        RemoveHandler bp.InsertDataCompleted, AddressOf _bp_InsertDataCompleted
        '        AddHandler bp.InsertDataCompleted, AddressOf _bp_InsertDataCompleted
        '        Dim param As New SsCommon.ServiceParam
        '        bp.InsertDataAsync(New SsCommon.ServiceParam, Vm.Criteria, Vm.ModelList, e)

        '        For Each j In Vm.ModelList
        '            j._IsEnable = False
        '        Next

        '    Else
        '        ViewModelUIService.WarningDlg(SsCommon.Message.CreateMessageById(SsCommon.MessageDataResponse.DataNotSave).Text)
        '    End If
        'End If

    End Sub
    Private Sub lkEmployee_SelectedItemChanged(sender As Object, e As SelectedItemChangedArg) Handles lkEmployee.SelectedItemChanged
        If e.NewItem IsNot Nothing Then
            Dim item = e.NewItem
            Vm.Criteria.Emplid = e.NewItem.Emplid    'changed by chanchira l. on 21/02/2020
            'lblEmplName.Content = e.NewItem.EmpName
            'If Me.Vm.Criteria.Emplid IsNot Nothing AndAlso Not String.IsNullOrEmpty(Me.Vm.Criteria.Emplid) AndAlso (Me.cboEmplFlag.SelectedValue = "R" OrElse Me.cboEmplFlag.SelectedValue = "L") Then
            '    Me.dpkFromdt.IsEnabled = True
            '    Me.dpkTodt.IsEnabled = True
            'Else
            '    Me.dpkFromdt.IsEnabled = False
            '    Me.dpkTodt.IsEnabled = False
            'End If
        End If
    End Sub

    'Private Sub lkEmployee_ValidatingInput(sender As Object, e As SelectedItemChangedArg) Handles lkEmployee.ValidatingInput
    '    If e.NewItem IsNot Nothing Then
    '        Dim item = e.NewItem
    '        Vm.Criteria.Emplid = e.NewItem.Emplid
    '    End If
    'End Sub

#End Region

#Region "Function"

    Private Function GetBp() As ServiceClient
        Return ServiceFactory.Instance.CreateServiceProxy(Of ServiceClient)(GetType(ServiceClient))
    End Function

    Private Sub LoadPeriodMast()
        Me.IsBusy = True
        Dim bp = Me.GetBp
        RemoveHandler bp.FindPayrollPeriodMastCompleted, AddressOf _bp_FindPayrollPeriodMastCompleted
        AddHandler bp.FindPayrollPeriodMastCompleted, AddressOf _bp_FindPayrollPeriodMastCompleted
        Dim param As New SsCommon.ServiceParam
        bp.FindPayrollPeriodMastAsync(param)
    End Sub

    Private Sub Init()
        Me.btnSave.IsEnabled = False
        'luecha
        'Me.btnNewGenText.IsEnabled = False

        Me.btnSearch.IsEnabled = True

        Vm.Criteria.Year = Today.Year
        Vm.Criteria.EmplFlag = "A"
        Vm.Criteria.Month = String.Format("{0:0#}", Today.Month)
        Vm.Criteria.PeriodTime = 1
        Vm.Criteria.EmplClass = 1
        Vm.Criteria.StartDate = Nothing
        Vm.Criteria.EndDate = Nothing
        Vm.Criteria.PeriodMastId = Me.Vm.PeriodMastLst.Item(0)
        'Add by Chanchira L. on 21/02/2020
        Vm.Criteria.Emplid = Nothing

    End Sub

    Private Function ValidateSearch() As Boolean
        Dim _flag As Boolean = True
        Dim _param As New SsCommon.ServiceParam
        If Vm.Criteria.Year IsNot Nothing Then
            If Vm.Criteria.Year.ToString.Length <> 4 Then
                Dim msg As SsCommon.Message = SsCommon.Message.CreateMessageById(SsCommon.MessageValidation.DidNotMatchTheRequiredPattern)
                msg.SetParams("Year")
                msg.SetParams("YYYY")
                ViewModelUIService.WarningDlg("Warning" & vbCrLf & msg.ToString)
                _flag = False
            End If
        End If
        If Vm.Criteria.Year Is Nothing OrElse Vm.Criteria.Month Is Nothing OrElse Vm.Criteria.Company Is Nothing OrElse Vm.Criteria.EmplFlag Is Nothing AndAlso _
           Vm.Criteria.StartDate Is Nothing OrElse Vm.Criteria.EndDate Is Nothing Then
            Dim msg As SsCommon.Message = SsCommon.Message.CreateMessageById(SsCommon.MessageValidation.CannotBeNull)
            msg.SetParams("Criterias (Year, Month, Period Time, Company)")
            ViewModelUIService.WarningDlg("Warning" & vbCrLf & msg.Text)
            _flag = False
        Else
            Dim fDate, fDate2 As Date
            DateTime.TryParseExact("15" & Vm.Criteria.Month & "" & txtYear.Text.Trim, _
                                                                  "ddMMyyyy", Nothing, DateTimeStyles.None, fDate)
            If Vm.Criteria.EndDate <= Vm.Criteria.StartDate Then
                Dim msg As SsCommon.Message = SsCommon.Message.CreateMessageById(SsCommon.MessageValidation.MustBeGreaterThan)
                msg.SetParams("End Date")
                msg.SetParams("Start Date")
                ViewModelUIService.WarningDlg("Warning" & vbCrLf & msg.Text)
                _flag = False
            Else

                'If Vm.Criteria.StartDate < DateAdd("m", -1, fDate) Then
                '    Dim msg As SsCommon.Message = SsCommon.Message.CreateMessageById(SsCommon.MessageValidation.MustBeGreaterThanOrEqual)
                '    msg.SetParams("Start Date")
                '    msg.SetParams("End of Period")
                '    'msg.SetParams(DateAdd("m", -1, fDate))
                '    ViewModelUIService.WarningDlg("Warning" & vbCrLf & msg.Text)
                '    _flag = False
                'End If

                'DateTime.TryParseExact("01" & Vm.Criteria.Month & "" & txtYear.Text.Trim, _
                '                                                                "ddMMyyyy", Nothing, DateTimeStyles.None, fDate2)
                'If Vm.Criteria.EndDate > DateAdd("d", -1, DateAdd("m", 1, fDate2)) Then
                '    Dim msg As SsCommon.Message = SsCommon.Message.CreateMessageById(SsCommon.MessageValidation.MustBeLessThenOrEqualTo) _
                '                                  .SetParams("End Date").SetParams(" End of Period")
                '    ViewModelUIService.WarningDlg("Warning" & vbCrLf & msg.Text)
                '    _flag = False
                'End If


            End If
        End If

        Return _flag
    End Function

    Private Sub SetPeriodid()

        If Not String.IsNullOrEmpty(Vm.Criteria.Company) AndAlso Vm.Criteria.PeriodMastId IsNot Nothing AndAlso Vm.Criteria.PeriodTime IsNot Nothing AndAlso _
              Not String.IsNullOrEmpty(Vm.Criteria.Year) AndAlso Not String.IsNullOrEmpty(Vm.Criteria.Month) Then

            Me.IsBusy = True
            Dim bp = Me.GetBp
            RemoveHandler bp.FindPayrollPeriodCompleted, AddressOf _bp_FindPayrollPeriodCompleted
            AddHandler bp.FindPayrollPeriodCompleted, AddressOf _bp_FindPayrollPeriodCompleted
            Dim param As New SsCommon.ServiceParam
            bp.FindPayrollPeriodAsync(Vm.Criteria.Company, Vm.Criteria.PeriodMastId.Periodmastid, Vm.Criteria.PeriodTime, Vm.Criteria.Year, Vm.Criteria.Month)

        Else
            Vm.Criteria.StartDate = Nothing
            Vm.Criteria.EndDate = Nothing
        End If
    End Sub

    Private Sub SetDefaultPeriodid()

        If Not String.IsNullOrEmpty(Vm.Criteria.Company) AndAlso Vm.Criteria.PeriodMastId IsNot Nothing Then
            Me.IsBusy = True
            Dim bp = Me.GetBp
            RemoveHandler bp.FindDefaultPeriodAndOrgCompleted, AddressOf _bp_FindDefaultPeriodAndOrgCompleted
            AddHandler bp.FindDefaultPeriodAndOrgCompleted, AddressOf _bp_FindDefaultPeriodAndOrgCompleted
            Dim param As New SsCommon.ServiceParam
            bp.FindDefaultPeriodAndOrgAsync(Vm.Criteria.Company, Vm.Criteria.PeriodMastId.Periodmastid, IsSmartpay)
        Else
            Vm.Criteria.Year = String.Empty
            Vm.Criteria.Month = String.Empty
            Vm.Criteria.Periodid = Nothing
            Vm.Criteria.PeriodTime = Nothing
            Vm.Criteria.StartDate = Nothing
            Vm.Criteria.EndDate = Nothing
        End If
    End Sub

    Private Sub btnGenText_Click(sender As Object, e As RoutedEventArgs) Handles btnGenText.Click
        GenText()
    End Sub

    'Gen Text ฝั่ง Server
    Private Sub GenText()
        If IsGenText = "Y" Then
            Me.IsBusy = True
            Dim bp = GetBp()
            RemoveHandler bp.GenerateTextInToServerCompleted, AddressOf _bp_GenerateTextInToServerCompleted
            AddHandler bp.GenerateTextInToServerCompleted, AddressOf _bp_GenerateTextInToServerCompleted
            'Changed by Chanchira L. on 15/10/2020 เพิ่ม parameter ส่งข้อมูลใน grid ไป
            'bp.GenerateTextInToServerAsync(New SsCommon.ServiceParam, Vm.Criteria, SsCommon.SsContext.Instance.ShortDateFormat, pathServer)
            bp.GenerateTextInToServerAsync(New SsCommon.ServiceParam, Vm.Criteria, SsCommon.SsContext.Instance.ShortDateFormat, pathServer, Vm.ModelList)
        Else
            ViewModelUIService.WarningDlg("Can not Export text")
        End If
    End Sub
#End Region

    Private Sub cboEmplFlag_SelectionChanged1(sender As Object, e As SelectionChangedEventArgs) Handles cboEmplFlag.SelectionChanged
        'test
        '#If DEBUG Then
        '        Vm.Criteria.Emplid = "VN00023685"
        '#End If

        'If Me.Vm.Criteria.Emplid IsNot Nothing AndAlso Not String.IsNullOrEmpty(Me.Vm.Criteria.Emplid) AndAlso (Me.cboEmplFlag.SelectedValue = "R" OrElse Me.cboEmplFlag.SelectedValue = "L") Then
        '    Me.dpkFromdt.IsEnabled = True
        '    Me.dpkTodt.IsEnabled = True
        'Else
        '    Me.dpkFromdt.IsEnabled = False
        '    Me.dpkTodt.IsEnabled = False
        'End If
    End Sub


#Region "Call Service"
    Private Sub _bp_SearchCompleted(ByVal sender As Object, ByVal e As SearchCompletedEventArgs)
        Me.IsBusy = False
        If e.Error IsNot Nothing Then
            Throw e.Error
        Else
            If e.Result.ResultLst IsNot Nothing Then
                Dim Obj = e.Result.Result
                Me.Vm.TranEmplNum = Obj.CntTran
                Me.txbStr2.Text = String.Format("{0} rows matching values were found", Obj.CntMaster)
                If Obj.CntMaster + Obj.CntTran > 0 Then
                    Me.Vm.ModelList = e.Result.ResultLst
                    'If Me.Vm.ModelList IsNot Nothing AndAlso Me.Vm.ModelList.Count > 0 Then
                    '    Me.Vm.NewEmplNum = (From i In Me.Vm.ModelList Where i.Emplflag = "N" Select i).Count
                    '    Me.Vm.ReEmplNum = (From i In Me.Vm.ModelList Where i.Emplflag = "R" Select i).Count
                    '    Me.Vm.UpEmplNum = (From i In Me.Vm.ModelList Where i.Emplflag = "U" Select i).Count
                    'End If

                    Me.dgEmpl.UpdateLayout()
                    btnSave.IsEnabled = True
                    ' luecha
                    'Me.btnNewGenText.IsEnabled = True
                Else
                    Me.Vm.ModelList = Nothing
                    btnSave.IsEnabled = False
                    ' luecha
                    'Me.btnNewGenText.IsEnabled = False
                End If
                btnSave.IsEnabled = True
            End If

        End If

        'Add by Chanchira L. on 12/06/2017
        '--------------------------------------
        If e.Result.Result.CntMaster > 0 Then      'Add by Chanchira L. on 15/09/2020
            Me.Vm.CountRemark = (From i In Me.Vm.ModelList Where i.Remarks <> "" Select i).Count
            If Me.Vm.CountRemark = 0 Then
                Me.btnSave.IsEnabled = True
                ' luecha
                'Me.btnNewGenText.IsEnabled = True
                'Me.btnExcel.IsEnabled = True
                'Me.btnGenText.IsEnabled = True
            Else
                Me.btnSave.IsEnabled = False
                Me.btnGenText.IsEnabled = False
                Me.btnExcel.IsEnabled = False
                ' luecha
                'Me.btnNewGenText.IsEnabled = False
            End If
            'If e.Result.Result.CntMaster = 0 Then
            '    Me.btnExcel.IsEnabled = False
            '    Me.btnGenText.IsEnabled = False
            'Else
            '    If Me.Vm.CountRemark = 0 Then
            '        Me.btnSave.IsEnabled = True
            '        Me.btnNewGenText.IsEnabled = True
            '        'Me.btnExcel.IsEnabled = True
            '        'Me.btnGenText.IsEnabled = True
            '    Else
            '        Me.btnSave.IsEnabled = False
            '        Me.btnGenText.IsEnabled = False
            '        Me.btnExcel.IsEnabled = False
            '        Me.btnNewGenText.IsEnabled = False
            '    End If

            'End If
        Else
            Me.btnExcel.IsEnabled = False
            Me.btnGenText.IsEnabled = False
        End If
        '--------------------------------------
    End Sub

    Private Sub _bp_FindPayrollPeriodMastCompleted(ByVal sender As Object, ByVal e As FindPayrollPeriodMastCompletedEventArgs)
        Me.IsBusy = False
        If e.Error IsNot Nothing Then
            Throw e.Error
        Else
            Me.Vm.PeriodMastLst = e.Result
        End If
        Me.Dispatcher.BeginInvoke(New Action(AddressOf Init))
    End Sub

    Private Sub _bp_FindPayrollPeriodCompleted(ByVal sender As Object, ByVal e As FindPayrollPeriodCompletedEventArgs)
        Me.IsBusy = False
        If e.Error IsNot Nothing Then
            Throw e.Error
        Else
            Vm.PeriodObj = e.Result
        End If

        If Vm.PeriodObj IsNot Nothing Then
            Vm.Criteria.StartDate = Vm.PeriodObj.Periodstartdate
            Vm.Criteria.EndDate = Vm.PeriodObj.Periodenddate
            Vm.Criteria.Periodid = Vm.PeriodObj.Periodid
            ' luecha
            'If Vm.PeriodObj.Periodstatus = "Y" Then
            '    ViewModelUIService.WarningDlg("Warning" & vbCrLf & "Period closed!!!")
            'End If
        Else
            ViewModelUIService.WarningDlg("Warning" & vbCrLf & "Period No Found!!!")
            Vm.Criteria.Year = String.Empty
            Vm.Criteria.Periodid = Nothing
            Vm.Criteria.Month = String.Empty
            Vm.Criteria.PeriodTime = Nothing
            Vm.Criteria.StartDate = Nothing
            Vm.Criteria.EndDate = Nothing
        End If
    End Sub

    Private Sub _bp_FindDefaultPeriodAndOrgCompleted(ByVal sender As Object, ByVal e As FindDefaultPeriodAndOrgCompletedEventArgs)
        Me.IsBusy = False
        If e.Error IsNot Nothing Then
            Throw e.Error
        Else
            Vm.PeriodObj = e.Result.Pyperiod
            Vm.OrgLst = e.Result.OrgLst
        End If

        If Vm.PeriodObj IsNot Nothing Then
            Vm.Criteria.Year = Vm.PeriodObj.Periodyear
            Vm.Criteria.Month = String.Format("{0:0#}", Vm.PeriodObj.Periodmonth)
            Vm.Criteria.PeriodTime = Vm.PeriodObj.Periodtime
            Vm.Criteria.Periodid = Vm.PeriodObj.Periodid
            Vm.Criteria.StartDate = Vm.PeriodObj.Periodstartdate
            Vm.Criteria.EndDate = Vm.PeriodObj.Periodenddate
        Else
            ViewModelUIService.WarningDlg("Warning" & vbCrLf & "Period No Found!!!")
            Vm.Criteria.Year = String.Empty
            Vm.Criteria.Month = String.Empty
            Vm.Criteria.Periodid = Nothing
            Vm.Criteria.PeriodTime = Nothing
            Vm.Criteria.StartDate = Nothing
            Vm.Criteria.EndDate = Nothing
        End If
    End Sub

    Private Sub _bp_InsertDataCompleted(ByVal sender As Object, ByVal e As InsertDataCompletedEventArgs)
        Me.IsBusy = False
        If e.Error IsNot Nothing Then
            Throw e.Error
        Else
            If e.Result Then
                IsGenText = "Y"
                ViewModelUIService.InfoDlg(SsCommon.Message.CreateMessageById(SsCommon.MessageDataResponse.SaveComplete).Text)
                btnSave.IsEnabled = False
                ' luecha
                'Me.btnNewGenText.IsEnabled = False

                'Add by Chanchira L. on 18/03/2020
                btnGenText.IsEnabled = True
                btnExcel.IsEnabled = True
                'btnSearch_Click(Me, e.UserState)
            Else
                ViewModelUIService.WarningDlg(SsCommon.Message.CreateMessageById(SsCommon.MessageDataResponse.DataNotSave).Text)
            End If
        End If
    End Sub

    Private Sub _bp_GenerateTextCompleted(sender As Object, e As GenerateTextCompletedEventArgs)
        Me.IsBusy = False
        If e.Error IsNot Nothing Then
            Throw e.Error
        Else
            If e.Result IsNot Nothing Then

                If e.Result.Message IsNot Nothing Then
                    ViewModelUIService.WarningDlg(e.Result.Message)
                    Exit Sub
                End If

                Using zipPackage__1 As ZipPackage = ZipPackage.Create(e.UserState.OpenFile())
                    If e.Result IsNot Nothing Then
                        Dim name = e.Result.Result.Keys(0)
                        Dim jj = e.Result.Result

                        Dim tse = jj.Values
                        For Each i In e.Result.Result

                            zipPackage__1.AddStream(name, New MemoryStream(i.Value))

                        Next

                    End If
                End Using

                ViewModelUIService.InfoDlg("Generate Completed")

            Else
                ViewModelUIService.InfoDlg("No data found")


            End If

        End If

    End Sub

    Private Sub _bp_GenerateTextInToServerCompleted(sender As Object, e As GenerateTextInToServerCompletedEventArgs)
        Me.IsBusy = False

        ViewModelUIService.InfoDlg(e.Result)

    End Sub
#End Region

    'Add by Chanchira L. on 12/06/2017
    Private Sub btnExcel_Click(sender As Object, e As RoutedEventArgs) Handles btnExcel.Click
        Dim dlg As New SaveFileDialog
        dlg.DefaultExt = ".xls"
        dlg.Filter = "Excel 2002-2010|*.xls"
        Dim res As Boolean? = dlg.ShowDialog
        If ValidateSearch() Then
            If res Then
                Me.IsBusy = True
                Dim bp = Me.GetBp
                RemoveHandler bp.PrintExcelCompleted, AddressOf _Bp_PrintExcelCompleted
                AddHandler bp.PrintExcelCompleted, AddressOf Me._Bp_PrintExcelCompleted
                Dim param As New SsCommon.ServiceParam
                Dim headers As New Dictionary(Of String, String)
                For Each o In Me.dgEmpl.Columns
                    If o.IsVisible = True Then
                        If Not String.IsNullOrEmpty(o.UniqueName) AndAlso Not String.IsNullOrEmpty(o.UniqueName.Replace("col", "")) AndAlso TypeOf (o.Header) Is System.String Then
                            Dim dbCol As String = o.UniqueName.Replace("col", "")
                            If o.ColumnGroupName IsNot Nothing AndAlso Not String.IsNullOrEmpty(o.ColumnGroupName) Then
                                Dim ColName = ""
                                Dim i = 0
                                Do While i < dgEmpl.ColumnGroups.Count
                                    If dgEmpl.ColumnGroups(i).Name = o.ColumnGroupName Then
                                        ColName = dgEmpl.ColumnGroups(i).Header
                                        Exit Do
                                    End If
                                    i += 1
                                Loop
                                headers.Add(SsCommon.PascalCaseString.GetDbStr(dbCol), ColName & " " & o.Header)
                            Else
                                headers.Add(SsCommon.PascalCaseString.GetDbStr(dbCol), o.Header)
                            End If
                        End If
                    End If
                Next

                Try
                    'Changed by Chanchira L. on 15/10/2020
                    'bp.PrintExcelAsync(param, Vm.Criteria, headers, dlg.OpenFile)


                    Dim Chklst As New ObservableCollection(Of ModelInterfacetempEe)
                    If Not Vm.Criteria.chkAll Then
                        For Each o In Vm.ModelList
                            If o.chk Then
                                Chklst.Add(o)
                            End If
                        Next
                    End If
                    bp.PrintExcelAsync(param, Vm.Criteria, headers, Chklst, dlg.OpenFile)
                Catch ex As Exception
                    Me.IsBusy = False
                End Try

            End If
        Else
            ViewModelUIService.WarningDlg(Vm.ErrorMsg)
        End If
    End Sub

    Private Sub _Bp_PrintExcelCompleted(ByVal sender As Object, ByVal e As PrintExcelCompletedEventArgs)
        Me.IsBusy = False
        If e.Error IsNot Nothing Then
            Dim fs As System.IO.Stream = DirectCast(e.UserState, System.IO.Stream)
            fs.Close()
            Throw e.Error
        Else
            If e.Result Is Nothing Then
                ViewModelUIService.InfoDlg("No data")

            Else
                Dim obj = e.Result
                Dim fileBytes As Byte() = TryCast(obj, Byte())
                Using fs As System.IO.Stream = DirectCast(e.UserState, System.IO.Stream)
                    fs.Write(fileBytes, 0, fileBytes.Length)
                    fs.Close()
                End Using
                ViewModelUIService.InfoDlg(SsCommon.Message.CreateMessageById(SsCommon.MessageDataResponse.ExcelCompleted).Text)

            End If
        End If
    End Sub

    Private Sub ChkSelectAll_Checked(ByVal sender As Object, ByVal e As System.Windows.RoutedEventArgs)
        If Vm.ModelList.Item(0)._IsEnable = True Then
            Vm.Criteria.chkAll = True
            For Each o In Vm.ModelList
                o.chk = True
            Next
        Else
        End If
    End Sub

    Private Sub ChkUnSelectAll_Checked(ByVal sender As Object, ByVal e As System.Windows.RoutedEventArgs)
        If Vm.ModelList.Item(0)._IsEnable = True Then
            Vm.Criteria.chkAll = False
            For Each o In Vm.ModelList
                o.chk = False
            Next
        Else
        End If
    End Sub


#Region "NewGenText"

    Private Sub btnNewGenText_Click(ByVal sender As Object, ByVal e As System.Windows.RoutedEventArgs) Handles btnNewGenText.Click
        Dim a = (From x In Vm.ModelList Where x.DbStatus <> "Y" Order By x.Emplid Select x).Count
        Dim b = (From y In Vm.ModelList Where y.chk = True Order By y.Emplid Select y).Count
        Dim lst As New ObservableCollection(Of ModelInterfacetempEe)

        'If Not Vm.Criteria.chkAll Then
        '    For Each o In Vm.ModelList
        '        If o.chk Then
        '            lst.Add(o)
        '        End If
        '    Next
        'End If

        'If a > 0 Then
        '    ViewModelUIService.WarningDlg(SsCommon.Message.CreateMessageById(SsCommon.MessageDataResponse.DataNotSave).Text)
        '    Exit Sub
        'Else
        '    If Vm.Criteria.chkAll OrElse lst.Count > 0 OrElse ValidateSearch() Then
        '        Me.IsBusy = True
        '        Dim bp = Me.GetBp
        '        RemoveHandler bp.NewGenerateTextCompleted, AddressOf _bp_NewGenerateTextCompleted
        '        AddHandler bp.NewGenerateTextCompleted, AddressOf _bp_NewGenerateTextCompleted
        '        Dim param As New SsCommon.ServiceParam
        '        bp.NewGenerateTextAsync(param, Vm.Criteria, lst, SsCommon.SsContext.Instance.ShortDateFormat, pathServer)
        '        'bp.NewGenerateTextAsync(param, Vm.Criteria, Vm.ModelList, SsCommon.SsContext.Instance.ShortDateFormat, pathServer)
        '    Else
        '        ViewModelUIService.WarningDlg(SsCommon.Message.CreateMessageById(SsCommon.MessageDataResponse.DataNotSave).Text)
        '    End If
        'End If

        If ValidateSearch() = False Then
            Exit Sub
        End If

        Me.IsBusy = True
        Dim bp = Me.GetBp
        RemoveHandler bp.NewGenerateTextCompleted, AddressOf _bp_NewGenerateTextCompleted
        AddHandler bp.NewGenerateTextCompleted, AddressOf _bp_NewGenerateTextCompleted
        Dim param As New SsCommon.ServiceParam
        bp.NewGenerateTextAsync(param, Vm.Criteria, lst, SsCommon.SsContext.Instance.ShortDateFormat, pathServer)

    End Sub

    Private Sub _bp_NewGenerateTextCompleted(ByVal sender As Object, ByVal e As NewGenerateTextCompletedEventArgs)
        Me.IsBusy = False
        If e.Error IsNot Nothing Then
            Throw e.Error
        Else
            If e.Result.IsSuccess Then
                ViewModelUIService.InfoDlg(e.Result.Result)
                btnExcel.IsEnabled = True
            Else
                ViewModelUIService.WarningDlg(e.Result.Result)
            End If
        End If
    End Sub

#End Region

    Private Sub expMain_Expanded(sender As Object, e As Telerik.Windows.RadRoutedEventArgs) Handles expMain.Expanded

    End Sub

    Private Sub dgEmpl_SelectionChanged(sender As Object, e As SelectionChangeEventArgs) Handles dgEmpl.SelectionChanged

    End Sub
End Class