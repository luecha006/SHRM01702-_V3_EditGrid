Imports System.ServiceModel
Imports System.ServiceModel.Activation
Imports SsHrCommon
Imports SsHrCommon.DTO
Imports Oracle.DataAccess.Client
Imports SsAppCommon
Imports System.IO
'Imports Microsoft.Office.Interop.Excel
Imports System.Runtime.InteropServices
'Imports Microsoft.SqlServer.Management.Smo.Agent
'Imports Microsoft.SqlServer.Management.Smo
Imports System.Threading

Imports SHRM01702Service.Controller
Imports Ionic.Zip
Imports System.Collections.ObjectModel
Imports SHRM01702Service.Model
Imports Newtonsoft.Json
Imports System.Data

<SsCommon.WcfErrorBehavior()>
<SsCommon.WcfSilverlightFaultBehavior()>
<ServiceContract(Namespace:="")>
<AspNetCompatibilityRequirements(RequirementsMode:=AspNetCompatibilityRequirementsMode.Allowed)>
Public Class SHRM01702Service
    Private authen As New System.Collections.Generic.List(Of SsHrCommon.DTO.Pygeneraldt)
    Private controller As New Controller

    Public Function CountSearch(param As SsCommon.ServiceParam, ByVal con As System.Data.Common.DbConnection, _cri As Model.ModelCriteriaList) As Model.ModelCountRecord
        Dim dpBp As New SsHrCommonService
        Dim ret As New Model.ModelCountRecord
        Dim col As String = "A.Grade"
        Dim sqlText As String = String.Empty
        authen = GetGdDetails("HRAUTHEN")
        If authen IsNot Nothing AndAlso authen.Count > 0 Then
            Dim val = (From x In authen Where x.Dtcode = "HR" Select x.Localdescription).FirstOrDefault
            If val = "M" Then
                col = "A.manager_level"
            End If
        End If

        ' Find All Data for Interface and Write Data into temporary table
        InsertIntoHrPayrollEmplTemp(param, _cri)

        Dim cmd As OracleCommand = con.CreateCommand

        cmd.CommandType = CommandType.Text
        cmd.BindByName = True

        sqlText = "SELECT count(*) FROM INTERFACETEMP_EE A WHERE A.EMPL_CLASS = NVL (:C_EMPLCLASS, A.EMPL_CLASS) AND A.COMPANY = :C_COMP AND A.PAYORGCODE like NVL(:PAYORGCODE || '%',A.PAYORGCODE) AND A.EMPLID = NVL (:C_EMPLID, A.EMPLID) AND " & _
                         "A.MANAGER_LEVEL = NVL(:MANAGERLVL,A.MANAGER_LEVEL) AND A.PERIODMASTID = :PERIODMASTID AND A.EMPLID = NVL(:C_EMPLID,A.EMPLID) AND A.EMPLFLAG = NVL(:EMPLFLAG,A.EMPLFLAG) AND A.EMPLFLAG <> 'D' {0} "

        cmd.Parameters.Clear()
        If _cri.EndDate IsNot Nothing Then
            sqlText = String.Format(sqlText, "AND A.EFFDT <= :DateTo {0}")
            cmd.Parameters.Add(New OracleParameter With {.ParameterName = "DateTo", .Value = _cri.EndDate})
        End If


        Dim authenstr = String.Format("AND A.DEPTID IN ({0}) ", dpBp.StringAuthenDepartment(param.UserName, "A.DEPTID", col, Today))

        cmd.CommandText = String.Format(sqlText, authenstr)
        'cmd.Parameters.Clear()
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_EMPLCLASS", .Value = _cri.EmplClass})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_COMP", .Value = _cri.Company})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "PAYORGCODE", .Value = If(_cri.Org Is Nothing, "", _cri.Org.Relateid)})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "MANAGERLVL", .Value = If(String.IsNullOrEmpty(_cri.Managerlvl), "", _cri.Managerlvl)})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "PERIODMASTID", .Value = _cri.PeriodMastId.Periodmastid})
        'cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_STDATE", .Value = _cri.StartDate})
        'cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_EDDATE", .Value = _cri.EndDate})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_EMPLID", .Value = If(String.IsNullOrEmpty(_cri.Emplid), "", _cri.Emplid)})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "EMPLFLAG", .Value = _cri.EmplFlag})


        Try
            If cmd.Connection.State = ConnectionState.Closed Then
                cmd.Connection.Open()
            End If

            ret.CntMaster = cmd.ExecuteScalar()
            ret.CntTran = 0
        Catch ex As Exception
            Throw ex
        End Try

        Return ret
    End Function

    'Changed by Chanchira L. on 20/02/2020
    <OperationContract()> _
    Public Function Search(ByVal param As SsCommon.ServiceParam, ByVal _cri As Model.ModelCriteriaList, ByVal pathServer As String) As SsCommon.ServiceResult(Of Model.ModelCountRecord, Model.ModelInterfacetempEe)
        Dim con As System.Data.Common.DbConnection = New OracleConnection(Util.GetOraConnectionString)
        Dim cmd As OracleCommand = con.CreateCommand
        cmd.BindByName = True

        Dim dpBp As New SsHrCommonService
        Dim ret As New SsCommon.ServiceResult(Of Model.ModelCountRecord, Model.ModelInterfacetempEe)
        Dim retCountRecord As New Model.ModelCountRecord
        Dim retTempEe As New SsCommon.ServiceResult(Of Model.ModelCountRecord, Model.ModelInterfacetempEe)
        Dim col As String = "A.Grade"
        Dim sqlText As String = String.Empty
        authen = GetGdDetails("HRAUTHEN")

        'If authen IsNot Nothing AndAlso authen.Count > 0 Then
        '    Dim val = (From x In authen Where x.Dtcode = "HR" Select x.Localdescription).FirstOrDefault
        '    If val = "M" Then
        '        col = "A.manager_level"
        '    End If
        'End If

        'cmd.CommandType = CommandType.Text
        'cmd.BindByName = True

        'sqlText = "SELECT a.EMPLID,a.payrollid,a.empl_name,nvl(a.empl_name,' ') employeename,a.effdt,(select localdescription from pygeneraldt where gdcode = 'MarryStatus' and dtcode = a.emp_marrystatus) marry_status_name,a.bankaccount,decode(a.remarks,null,'Y','N') db_status,decode(a.remarks,null,'black','red') Row_Color, nvl(a.remarks,'') remarks " & _
        '          "FROM INTERFACETEMP_EE A WHERE A.EMPL_CLASS = NVL (:C_EMPLCLASS, A.EMPL_CLASS) AND A.COMPANY = :C_COMP AND A.PAYORGCODE like NVL(:PAYORGCODE || '%',A.PAYORGCODE) AND A.EMPLID = NVL (:C_EMPLID, A.EMPLID) AND " & _
        '                 "A.MANAGER_LEVEL = NVL(:MANAGERLVL,A.MANAGER_LEVEL) AND A.PERIODMASTID = :PERIODMASTID AND A.EMPLID = NVL(:C_EMPLID,A.EMPLID) AND A.EMPLFLAG = NVL(:EMPLFLAG,A.EMPLFLAG) AND A.EMPLFLAG <> 'D' {0} ORDER BY A.EMPLID"
        ''--------------------------------------------------

        'cmd.Parameters.Clear()
        'If _cri.EndDate IsNot Nothing Then
        '    sqlText = String.Format(sqlText, "AND A.EFFDT <= :DateTo {0}")
        '    cmd.Parameters.Add(New OracleParameter With {.ParameterName = "DateTo", .Value = _cri.EndDate})
        'End If

        'Dim authenstr = String.Format("AND A.DEPTID IN ({0}) ", dpBp.StringAuthenDepartment(param.UserName, "A.DEPTID", col, Today))
        'cmd.CommandText = String.Format(sqlText, authenstr)

        'If _cri.EmplFlag = "A" Then
        '    _cri.EmplFlag = ""
        'Else
        '    _cri.EmplFlag = _cri.EmplFlag
        'End If

        'cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_EMPLCLASS", .Value = _cri.EmplClass})
        'cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_COMP", .Value = _cri.Company})
        'cmd.Parameters.Add(New OracleParameter With {.ParameterName = "PAYORGCODE", .Value = If(_cri.Org Is Nothing, "", _cri.Org.Relateid)})
        'cmd.Parameters.Add(New OracleParameter With {.ParameterName = "MANAGERLVL", .Value = If(String.IsNullOrEmpty(_cri.Managerlvl), "", _cri.Managerlvl)})
        'cmd.Parameters.Add(New OracleParameter With {.ParameterName = "PERIODMASTID", .Value = _cri.PeriodMastId.Periodmastid})
        'cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_EMPLID", .Value = If(String.IsNullOrEmpty(_cri.Emplid), "", _cri.Emplid)})
        'cmd.Parameters.Add(New OracleParameter With {.ParameterName = "EMPLFLAG", .Value = _cri.EmplFlag})

        Try
            Dim fileName As String = ""
            '---------------------
            Dim curPath As String = pathServer
            If curPath(curPath.Length - 1) <> "\" Then
                curPath = curPath & "\"
            End If

            Dim responseAT = controller.Call_AccessToken()
            If (Not IsNothing(responseAT)) Then
                Dim responseBF64 As ModelBaseFileApi = controller.Call_BaseFile64(_cri, responseAT.access_token)

                fileName = responseBF64.parameter.RequestId

                If (Not IsNothing(responseBF64)) Then
                    'UnZipFile64
                    ConvertBaseToFileAndWrite(responseBF64, pathServer)
                End If
            End If

            Dim strText As String = File.ReadAllText(curPath & fileName & "\" & fileName & ".txt")
            Dim dtEmpList As List(Of ModelEmpoyeeResponseApi) = Newtonsoft.Json.JsonConvert.DeserializeObject(Of List(Of ModelEmpoyeeResponseApi))(strText)

            'If cmd.Connection.State = ConnectionState.Closed Then
            '    cmd.Connection.Open()
            'End If

            'ret.Result = CountSearch(param, con, _cri)

            retCountRecord.CntMaster = dtEmpList.Count
            retCountRecord.CntTran = 0
            ret.Result = retCountRecord

            If dtEmpList IsNot Nothing AndAlso dtEmpList.Count > 0 Then
                For Each r In dtEmpList
                    Dim m As New Model.ModelInterfacetempEe

                    m.Emplid = r.t_idno
                    m.t_Idno_PayrollId = r.t_idno
                    m.t_Name = r.t_name
                    m.t_Mar = r.t_mar
                    m.t_Bac_BankAccount = r.t_bac
                    m._IsEnable = True

                    ret.ResultLst.Add(m)
                Next
            End If

            '---------------------

            'If cmd.Connection.State = ConnectionState.Closed Then
            '    cmd.Connection.Open()
            'End If

            'ret.Result = CountSearch(param, con, _cri)

            'If ret.Result.CntMaster > 0 Then
            '    Dim rdr As System.Data.Common.DbDataReader = cmd.ExecuteReader()
            '    Do While (rdr.Read)
            '        Dim m As New Model.ModelInterfacetempEe
            '        m.RetrieveFromDataReader(rdr)

            '        m._IsEnable = True      'Add by Chanchira L. on 19/10/2020

            '        ret.ResultLst.Add(m)
            '    Loop
            'End If

        Catch ex As Exception
            Throw ex
        Finally
            con.Close()
        End Try
        Return ret
    End Function

    <OperationContract()> _
    Public Function SearchTest(ByVal param As SsCommon.ServiceParam, ByVal _cri As Model.ModelCriteriaList, ByVal pathServer As String) As SsCommon.ServiceResult(Of Model.ModelCountRecord, Model.ModelInterfacetempEe)
        Dim con As System.Data.Common.DbConnection = New OracleConnection(Util.GetOraConnectionString)
        Dim cmd As OracleCommand = con.CreateCommand
        cmd.BindByName = True

        Dim dpBp As New SsHrCommonService
        Dim ret As New SsCommon.ServiceResult(Of Model.ModelCountRecord, Model.ModelInterfacetempEe)
        Dim retTempEe As New SsCommon.ServiceResult(Of Model.ModelCountRecord, Model.ModelInterfacetempEe)
        Dim col As String = "A.Grade"
        Dim sqlText As String = String.Empty
        authen = GetGdDetails("HRAUTHEN")


        'luecha
        Try
            Dim responseAT = controller.Call_AccessToken()
            If (Not IsNothing(responseAT)) Then
                Dim responseBF64 As ModelBaseFileApi = controller.Call_BaseFile64(_cri, responseAT.access_token)
                If (Not IsNothing(responseBF64)) Then
                    'UnZipFile64
                    ConvertBaseToFileAndWrite(responseBF64, pathServer)
                End If
            End If
        Catch ex As Exception
            Throw ex
        Finally
            con.Close()
        End Try

        Return ret
    End Function

    'Public Function InsertData(param As SsCommon.ServiceParam, _criteria As Model.ModelCriteriaList) As Boolean
    <OperationContract()> _
    Public Function InsertData(param As SsCommon.ServiceParam, _criteria As Model.ModelCriteriaList, ByRef EmplidList As List(Of Model.ModelInterfacetempEe)) As Boolean
        Dim status = True
        Dim dpBp As New SsHrCommonService
        Dim ret As New List(Of Model.ModelInterfaceLog)
        Dim con As System.Data.Common.DbConnection = New OracleConnection(Util.GetOraConnectionString)
        Dim cmd As OracleCommand = con.CreateCommand
        cmd.BindByName = True
        authen = GetGdDetails("HRAUTHEN")
        Dim d As Date = Now
        Dim sqlText As String = String.Empty
        Dim col As String = "A.Grade"
        Dim authenstr = String.Format("AND A.DEPTID IN ({0}) ", dpBp.StringAuthenDepartment(param.UserName, "A.DEPTID", col, Today))

        If authen IsNot Nothing AndAlso authen.Count > 0 Then
            Dim val = (From x In authen Where x.Dtcode = "HR" Select x.Localdescription).FirstOrDefault
            If val = "M" Then
                col = "A.manager_level"
            End If
        End If

        If _criteria.EmplFlag = "A" Then
            _criteria.EmplFlag = ""
        Else
            _criteria.EmplFlag = _criteria.EmplFlag
        End If

        'Add by Chanchira L. on 14/10/2020 check all record or select item
        '--------------------------------------------------
        Dim Criteria_Emplid As String = ""
        If Not _criteria.chkAll Then
            'Dim Emplid = (From y In EmplidList Where y.chk = True Order By y.Emplid Select y)
            For Each o In EmplidList
                If o.chk Then
                    Criteria_Emplid += IIf(Criteria_Emplid <> "", ",", "") & "'" & o.Emplid & "'"
                End If
                o._IsEnable = False
            Next
        Else
            Criteria_Emplid = If(String.IsNullOrEmpty(_criteria.Emplid), String.Empty, _criteria.Emplid)
            For Each o In EmplidList
                o._IsEnable = False
            Next
        End If
        '--------------------------------------------------

        Try
            If cmd.Connection.State = ConnectionState.Closed Then
                cmd.Connection.Open()
            End If
            Dim trn As System.Data.Common.DbTransaction = con.BeginTransaction()
            Try
                sqlText = "delete from interface_ee a " & _
                           "WHERE A.ISINTERFACE = 'N' AND A.EMPL_CLASS = NVL (:C_EMPLCLASS, A.EMPL_CLASS) AND A.COMPANY = :C_COMP AND A.PAYORGCODE like NVL(:PAYORGCODE || '%',A.PAYORGCODE) AND A.MANAGER_LEVEL = NVL(:MANAGERLVL,A.MANAGER_LEVEL)   " & _
                           "AND A.EMPLFLAG = NVL(:EMPLFLAG,A.EMPLFLAG) AND A.PERIODMASTID = :PERIODMASTID  {0} "
                If _criteria.chkAll Then
                    sqlText += " AND A.EMPLID = NVL('" & Criteria_Emplid & "',A.EMPLID) "
                Else
                    sqlText += " AND A.EMPLID IN (" & Criteria_Emplid & ")"
                End If
                '--------------------------------------------------

                cmd.Parameters.Clear()
                cmd.Parameters.Add(New OracleParameter With {.ParameterName = "EMPLFLAG", .Value = _criteria.EmplFlag})
                cmd.Parameters.Add(New OracleParameter With {.ParameterName = "MANAGERLVL", .Value = If(String.IsNullOrEmpty(_criteria.Managerlvl), String.Empty, _criteria.Managerlvl)})
                cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_EMPLCLASS", .Value = If(String.IsNullOrEmpty(_criteria.EmplClass), String.Empty, _criteria.EmplClass)})
                cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_COMP", .Value = _criteria.Company})
                cmd.Parameters.Add(New OracleParameter With {.ParameterName = "PAYORGCODE", .Value = If(_criteria.Org Is Nothing, "", _criteria.Org.Relateid)})
                'Marked by Chanchira L. on 14/10/2020
                'cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_EMPLID", .Value = If(String.IsNullOrEmpty(_criteria.Emplid), String.Empty, _criteria.Emplid)})
                cmd.Parameters.Add(New OracleParameter With {.ParameterName = "PERIODMASTID", .Value = _criteria.PeriodMastId.Periodmastid})
                cmd.CommandText = String.Format(sqlText, authenstr)
                cmd.Transaction = trn
                cmd.ExecuteNonQuery()

                sqlText = "delete from interface_compen a " & _
                          "WHERE NVL(A.ISINTERFACE,'N') = 'N' " & _
                          "AND EXISTS (SELECT 1 FROM INTERFACE_COMPEN B ,JOB_EE A  WHERE B.EMPLID = A.EMPLID AND B.EMPL_RCD = A.EMPL_RCD AND B.EFFDT = A.EFFDT AND B.EFFSEQ = A.EFFSEQ AND A.PERIODMASTID = :PERIODMASTID " & _
                          "AND b.EMPLFLAG  = NVL(:EMPLFLAG,b.EMPLFLAG) AND A.EMPL_CLASS = NVL (:C_EMPLCLASS, A.EMPL_CLASS) AND A.COMPANY = :C_COMP AND A.PAYORGCODE like NVL(:PAYORGCODE || '%',A.PAYORGCODE) " & _
                          "AND A.MANAGER_LEVEL = NVL(:MANAGERLVL,A.MANAGER_LEVEL)  {0}) " & _
                          "AND EMPLFLAG <> 'D' "

                If _criteria.chkAll Then
                    sqlText += " AND A.EMPLID = NVL('" & Criteria_Emplid & "',A.EMPLID) "
                Else
                    sqlText += " AND A.EMPLID IN (" & Criteria_Emplid & ")"
                End If
                '--------------------------------------------------

                cmd.Parameters.Clear()
                cmd.Parameters.Add(New OracleParameter With {.ParameterName = "EMPLFLAG", .Value = _criteria.EmplFlag})
                cmd.Parameters.Add(New OracleParameter With {.ParameterName = "MANAGERLVL", .Value = If(String.IsNullOrEmpty(_criteria.Managerlvl), String.Empty, _criteria.Managerlvl)})
                cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_EMPLCLASS", .Value = If(String.IsNullOrEmpty(_criteria.EmplClass), String.Empty, _criteria.EmplClass)})
                cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_COMP", .Value = _criteria.Company})
                cmd.Parameters.Add(New OracleParameter With {.ParameterName = "PAYORGCODE", .Value = If(_criteria.Org Is Nothing, "", _criteria.Org.Relateid)})
                'Marked by Chanchira L. on 14/10/2020
                'cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_EMPLID", .Value = If(String.IsNullOrEmpty(_criteria.Emplid), String.Empty, _criteria.Emplid)})
                cmd.Parameters.Add(New OracleParameter With {.ParameterName = "PERIODMASTID", .Value = _criteria.PeriodMastId.Periodmastid})
                cmd.CommandText = String.Format(sqlText, authenstr)
                cmd.Transaction = trn
                cmd.ExecuteNonQuery()
                '-----------------------------------------

                sqlText = "INSERT INTO INTERFACE_EE(EMPLID,EMPL_RCD,EFFDT,EFFSEQ,ACTION,ACTION_DT,ACTION_REASON,PER_ORG,DEPTID,JOBCODE,POSITION_NBR,POSITION_LEVEL," & _
                                                               "REPORT_TO,POSN_OVRD,HR_STATUS,EMPL_STATUS,LOCATION,JOB_ENTRY_DT,DEPT_ENTRY_DT,POSITION_ENTRY_DT,POSNLEVEL_ENTRY_DT,SHIFT," & _
                                                               "REG_TEMP,FULL_PART_TIME,COMPANY,PAYGROUP,POITYPE,EMPLGROUP,EMPLIDCODE,HOLIDAY_SCHEDULE,STD_HOURS,STD_HRS_FREQUENCY,OFFICER_CD," & _
                                                               "EMPL_CLASS,GRADE,GRADE_ENTRY_DT,COMP_FREQUENCY,COMPRATE,CHANGE_AMT,CHANGE_PCT,CURRENCY_CD,BUSINESS_UNIT,SETID_DEPT,SETID_JOBCODE," & _
                                                               "HIRE_DT,LAST_HIRE_DT,TERMINATION_DT,ASGN_START_DT,LST_ASGN_START_DT,ASGN_END_DT,LAST_DATE_WORKED,EXPECTED_RETURN_DT,EXPECTED_END_DATE," & _
                                                               "PC_DATE_CPG,PROBATION_DT,PROBATION,PROBATION_TYPE,SOCIALWELF_PREFIX,CALSOCIALWELF,SOCIALWELFBEFYN,SOCIALWELFID,PERCENTSOCIALWELF," & _
                                                               "SOCIAL_BRANCH_CPG,ISCOMPSOCIALWELF,PAYORGCODE,PERIODMASTID,BONUS,CALTAXMETHOD,CCA_CPG,JOB_INDICATOR,JOBOPEN_NO,PAYROLLID,MANAGER_LEVEL," & _
                                                               "SAL_ADMIN_PLAN,EMPLFLAG,ISINTERFACE,PRE_COMPANY,CREATEUSER,CREATEDATE,MODIFYDATE,PROGRAMCODE,DATEOFINTERFACE,USERINTERFACE," & _
                                                               "EMPL_TITLE,EMPL_NAME,EMPL_SEX,EMP_MARRYSTATUS,STATUS,BIRTHDATE,BANKCODE,ACCOUNTID,BANKACCOUNT,BANK_BRANCH,HARMFUL,WORKHOUR,NID,ADDRESS1,ADDRESS2,SCNO,TAXID,WORKDAY_MONTH) " & _
                                   "SELECT EMPLID,EMPL_RCD,EFFDT,EFFSEQ,ACTION,ACTION_DT,ACTION_REASON,PER_ORG,DEPTID,JOBCODE,POSITION_NBR,POSITION_LEVEL,REPORT_TO,POSN_OVRD,HR_STATUS, " & _
                                    "       EMPL_STATUS,LOCATION,JOB_ENTRY_DT,DEPT_ENTRY_DT,POSITION_ENTRY_DT,POSNLEVEL_ENTRY_DT,SHIFT,REG_TEMP,FULL_PART_TIME,COMPANY,PAYGROUP,POITYPE,EMPLGROUP, " & _
                                    "       EMPLIDCODE,HOLIDAY_SCHEDULE,STD_HOURS,STD_HRS_FREQUENCY,OFFICER_CD,EMPL_CLASS,GRADE,GRADE_ENTRY_DT,COMP_FREQUENCY,COMPRATE,CHANGE_AMT,CHANGE_PCT, " & _
                                    "       CURRENCY_CD,BUSINESS_UNIT,SETID_DEPT,SETID_JOBCODE,HIRE_DT,LAST_HIRE_DT,TERMINATION_DT,ASGN_START_DT,LST_ASGN_START_DT,ASGN_END_DT,LAST_DATE_WORKED, " & _
                                    "       EXPECTED_RETURN_DT,EXPECTED_END_DATE,PC_DATE_CPG,PROBATION_DT,PROBATION,PROBATION_TYPE,SOCIALWELF_PREFIX,CALSOCIALWELF,SOCIALWELFBEFYN,SOCIALWELFID, " & _
                                    "       PERCENTSOCIALWELF,SOCIAL_BRANCH_CPG,ISCOMPSOCIALWELF,PAYORGCODE,PERIODMASTID,BONUS,CALTAXMETHOD,CCA_CPG,JOB_INDICATOR,JOBOPEN_NO,PAYROLLID, " & _
                                    "       MANAGER_LEVEL,SAL_ADMIN_PLAN,EMPLFLAG,ISINTERFACE,PRE_COMPANY,CREATEUSER,CREATEDATE,MODIFYDATE,PROGRAMCODE,:DATEOFINTERFACE,:USERINTERFACE, " & _
                                    "       EMPL_TITLE,EMPL_NAME,EMPL_SEX,EMP_MARRYSTATUS,STATUS,BIRTHDATE,BANKCODE,ACCOUNTID,BANKACCOUNT,BANK_BRANCH,HARMFUL,WORKHOUR,NID,ADDRESS1,ADDRESS2,SCNO,TAXID,WORKDAY_MONTH " & _
                                    "FROM INTERFACETEMP_EE A " & _
                                    "WHERE A.ISINTERFACE = 'N' AND A.EMPL_CLASS = NVL (:C_EMPLCLASS, A.EMPL_CLASS) AND A.COMPANY = :C_COMP AND A.PAYORGCODE like NVL(:PAYORGCODE || '%',A.PAYORGCODE) AND A.MANAGER_LEVEL = NVL(:MANAGERLVL,A.MANAGER_LEVEL)   " & _
                                    " AND A.EMPLFLAG = NVL(:EMPLFLAG,A.EMPLFLAG) AND A.PERIODMASTID = :PERIODMASTID  {0} "
                If _criteria.chkAll Then
                    sqlText += " AND A.EMPLID = NVL('" & Criteria_Emplid & "',A.EMPLID) "
                Else
                    sqlText += " AND A.EMPLID IN (" & Criteria_Emplid & ")"
                End If
                '-----------------------------------------

                cmd.Parameters.Clear()
                cmd.Parameters.Add(New OracleParameter With {.ParameterName = "EMPLFLAG", .Value = _criteria.EmplFlag})
                cmd.Parameters.Add(New OracleParameter With {.ParameterName = "MANAGERLVL", .Value = If(String.IsNullOrEmpty(_criteria.Managerlvl), String.Empty, _criteria.Managerlvl)})
                cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_EMPLCLASS", .Value = If(String.IsNullOrEmpty(_criteria.EmplClass), String.Empty, _criteria.EmplClass)})
                cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_COMP", .Value = _criteria.Company})
                cmd.Parameters.Add(New OracleParameter With {.ParameterName = "PAYORGCODE", .Value = If(_criteria.Org Is Nothing, "", _criteria.Org.Relateid)})
                cmd.Parameters.Add(New OracleParameter With {.ParameterName = "DATEOFINTERFACE", .Value = d})
                cmd.Parameters.Add(New OracleParameter With {.ParameterName = "PERIODMASTID", .Value = _criteria.PeriodMastId.Periodmastid})
                cmd.Parameters.Add(New OracleParameter With {.ParameterName = "USERINTERFACE", .Value = param.UserName})
                cmd.CommandText = String.Format(sqlText, authenstr)
                cmd.Transaction = trn
                cmd.ExecuteNonQuery()

                sqlText = "INSERT INTO INTERFACE_COMPEN(EMPLID,EMPL_RCD,EFFDT,EFFSEQ,COMP_RATECD,INCEXPTYPE,PAYQTY,COMPENSATION_RATE,CHANGE_AMT,FREQUENCY,EMPLFLAG,ISINTERFACE," & _
                                                   "CREATEUSER,CREATEDATE,MODIFYDATE,PROGRAMCODE,DATEOFINTERFACE,USERINTERFACE) " & _
                          "SELECT EMPLID,EMPL_RCD,EFFDT,EFFSEQ,COMP_RATECD,INCEXPTYPE,PAYQTY,COMPENSATION_RATE,CHANGE_AMT,FREQUENCY,EMPLFLAG,ISINTERFACE,CREATEUSER, " & _
                          "       CREATEDATE,MODIFYDATE,PROGRAMCODE,:DATEOFINTERFACE,:USERINTERFACE  " & _
                         "FROM INTERFACETEMP_COMPEN A " & _
                          "WHERE A.EMPL_CLASS = NVL (:C_EMPLCLASS, A.EMPL_CLASS) AND A.COMPANY = :C_COMP AND A.MANAGER_LEVEL = NVL(:MANAGERLVL,A.MANAGER_LEVEL)   " & _
                          "AND A.EMPLFLAG = NVL(:EMPLFLAG,A.EMPLFLAG) AND A.PERIODMASTID = :PERIODMASTID  {0}"
                If _criteria.chkAll Then
                    sqlText += "AND A.EMPLID = NVL('" & Criteria_Emplid & "',A.EMPLID) "
                Else
                    sqlText += " AND A.EMPLID IN (" & Criteria_Emplid & ")"
                End If
                '-----------------------------------------

                cmd.Parameters.Clear()
                cmd.Parameters.Add(New OracleParameter With {.ParameterName = "EMPLFLAG", .Value = _criteria.EmplFlag})
                cmd.Parameters.Add(New OracleParameter With {.ParameterName = "MANAGERLVL", .Value = If(String.IsNullOrEmpty(_criteria.Managerlvl), String.Empty, _criteria.Managerlvl)})
                cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_EMPLCLASS", .Value = If(String.IsNullOrEmpty(_criteria.EmplClass), String.Empty, _criteria.EmplClass)})
                cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_COMP", .Value = _criteria.Company})
                cmd.Parameters.Add(New OracleParameter With {.ParameterName = "DATEOFINTERFACE", .Value = d})
                cmd.Parameters.Add(New OracleParameter With {.ParameterName = "USERINTERFACE", .Value = param.UserName})
                cmd.Parameters.Add(New OracleParameter With {.ParameterName = "PERIODMASTID", .Value = _criteria.PeriodMastId.Periodmastid})
                cmd.CommandText = String.Format(sqlText, authenstr)
                cmd.Transaction = trn
                cmd.ExecuteNonQuery()

                'Changed by Chanchira L. on 14/10/2020
                '----------------------------------------------
                'ret = FindHrPayrollEmpl(cmd, _criteria, authenstr, d)
                ret = FindHrPayrollEmpl(cmd, _criteria, authenstr, d, Criteria_Emplid)
                '----------------------------------------------
                'InsertHrPayrollLog(cmd, trn, _criteria, param, ret, d)

                trn.Commit()

                'Add by Chanchira L. on 19/10/2020
                For Each j In EmplidList
                    j._IsEnable = False
                Next
            Catch ex As Exception
                trn.Rollback()
                Throw ex
            End Try
        Catch ex As Exception
            status = False
            Throw ex
        Finally
            con.Close()
        End Try

        Return status

    End Function

#Region "Insert And Update 'D' Flag To Temporary Table"

    Private Sub InsertIntoHrPayrollEmplTemp(ByVal param As SsCommon.ServiceParam, ByVal _criteria As Model.ModelCriteriaList)
        Dim _d As Date = Now
        Dim con As System.Data.Common.DbConnection = New OracleConnection(Util.GetOraConnectionString)
        Dim cmd As OracleCommand = con.CreateCommand
        cmd.BindByName = True
        Dim _dtoTempEE As New List(Of Model.ModelInterfacetempEe)
        Dim _dtoTempComp As New List(Of Model.ModelInterfacetempCompen)
        Dim _dtoCompanyDtl As New Model.ModelCompanyDtl
        Dim dpBp As New SsHrCommonService
        Dim col As String = "A.Grade"
        Dim sqlText As String = String.Empty
        Dim authenstr As String

        If authen IsNot Nothing AndAlso authen.Count > 0 Then
            Dim val = (From x In authen Where x.Dtcode = "HR" Select x.Localdescription).FirstOrDefault
            If val = "M" Then
                col = "A.manager_level"
            End If
        End If
        authenstr = String.Format("AND A.DEPTID IN ({0}) ", dpBp.StringAuthenDepartment(param.UserName, "A.DEPTID", col, Today))

        Try

            If cmd.Connection.State = ConnectionState.Closed Then
                cmd.Connection.Open()
            End If

            Dim trn As System.Data.Common.DbTransaction = con.BeginTransaction()

            'Delete Data in Temporary Table
            'Changed by Chanchira L. on 21/02/2020 เอาเงื่อนไข effdt, effseq ออก
            'sqlText = "DELETE FROM INTERFACETEMP_COMPEN dd WHERE EXISTS (SELECT 1 FROM (SELECT * FROM (WITH BCOM AS (SELECT A.EMPLID, A.EMPL_RCD, A.EFFDT, A.EFFSEQ, COMP_RATECD FROM INTERFACETEMP_COMPEN B, JOB_EE A WHERE B.EMPLID = A.EMPLID AND B.EMPL_RCD = A.EMPL_RCD AND B.EFFDT    = A.EFFDT AND B.EFFSEQ   = A.EFFSEQ {0} ) SELECT C.EMPLID, C.EMPL_RCD, C.EFFDT, C.EFFSEQ, C.COMP_RATECD FROM INTERFACETEMP_COMPEN C WHERE EXISTS (SELECT * FROM BCOM X WHERE C.EMPLID    = X.EMPLID AND C.EMPL_RCD    = X.EMPL_RCD AND C.EFFDT       = X.EFFDT AND C.EFFSEQ      = X.EFFSEQ AND C.COMP_RATECD = X.COMP_RATECD ) ) ) xxx WHERE xxx.EMPLID    = dd.EMPLID AND xxx.EMPL_RCD    = dd.EMPL_RCD AND xxx.EFFDT       = dd.EFFDT AND xxx.EFFSEQ      = dd.EFFSEQ AND xxx.COMP_RATECD = dd.COMP_RATECD )"
            sqlText = "DELETE FROM INTERFACETEMP_COMPEN A " & _
                      "WHERE A.EMPLFLAG = NVL (:EMPLFLAG, A.EMPLFLAG) " & _
                      "AND A.MANAGER_LEVEL = NVL (:MANAGERLVL, A.MANAGER_LEVEL) " & _
                      "AND A.EMPL_CLASS = NVL (:EMPLCLASS, A.EMPL_CLASS)   " & _
                      "AND A.COMPANY = :COMP " & _
                      "AND A.EFFDT <= :C_EDDATE " & _
                      "AND A.EMPLID = NVL (:EMPLID, A.EMPLID) " & _
                      "AND A.PERIODMASTID = :PERIODMASTID {0} "
            cmd.Parameters.Clear()
            cmd.Parameters.Add(New OracleParameter With {.ParameterName = "EMPLFLAG", .Value = If(_criteria.EmplFlag = "A", "", _criteria.EmplFlag)})
            cmd.Parameters.Add(New OracleParameter With {.ParameterName = "MANAGERLVL", .Value = If(String.IsNullOrEmpty(_criteria.Managerlvl), String.Empty, _criteria.Managerlvl)})
            cmd.Parameters.Add(New OracleParameter With {.ParameterName = "EMPLCLASS", .Value = If(String.IsNullOrEmpty(_criteria.EmplClass), String.Empty, _criteria.EmplClass)})
            cmd.Parameters.Add(New OracleParameter With {.ParameterName = "COMP", .Value = _criteria.Company})
            'cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_STDATE", .Value = _criteria.StartDate})
            cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_EDDATE", .Value = _criteria.EndDate})
            cmd.Parameters.Add(New OracleParameter With {.ParameterName = "EMPLID", .Value = If(String.IsNullOrEmpty(_criteria.Emplid), String.Empty, _criteria.Emplid)})
            cmd.Parameters.Add(New OracleParameter With {.ParameterName = "PERIODMASTID", .Value = _criteria.PeriodMastId.Periodmastid})

            cmd.CommandText = String.Format(sqlText, authenstr)
            cmd.Transaction = trn
            cmd.ExecuteNonQuery()

            '================================================
            'sqlText = "DELETE FROM INTERFACETEMP_COMPEN A  " & _
            '            "WHERE  A.ISINTERFACE = 'Y' AND A.EMPLFLAG = NVL (:EMPLFLAG, A.EMPLFLAG) AND A.MANAGER_LEVEL = NVL (:MANAGERLVL, A.MANAGER_LEVEL) AND A.EMPL_CLASS = NVL (:EMPLCLASS, A.EMPL_CLASS)   " & _
            '            "AND A.COMPANY = :COMP AND A.EFFDT BETWEEN :C_STDATE AND :C_EDDATE AND A.EMPLID = NVL (:EMPLID, A.EMPLID) AND A.PERIODMASTID = :PERIODMASTID {0} "
            'cmd.Parameters.Clear()

            'cmd.Parameters.Add(New OracleParameter With {.ParameterName = "EMPLFLAG", .Value = If(_criteria.EmplFlag = "A", "", _criteria.EmplFlag)})
            'cmd.Parameters.Add(New OracleParameter With {.ParameterName = "MANAGERLVL", .Value = If(String.IsNullOrEmpty(_criteria.Managerlvl), String.Empty, _criteria.Managerlvl)})
            'cmd.Parameters.Add(New OracleParameter With {.ParameterName = "EMPLCLASS", .Value = If(String.IsNullOrEmpty(_criteria.EmplClass), String.Empty, _criteria.EmplClass)})
            'cmd.Parameters.Add(New OracleParameter With {.ParameterName = "COMP", .Value = _criteria.Company})
            'cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_STDATE", .Value = _criteria.StartDate})
            'cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_EDDATE", .Value = _criteria.EndDate})
            'cmd.Parameters.Add(New OracleParameter With {.ParameterName = "EMPLID", .Value = If(String.IsNullOrEmpty(_criteria.Emplid), String.Empty, _criteria.Emplid)})
            'cmd.Parameters.Add(New OracleParameter With {.ParameterName = "PERIODMASTID", .Value = _criteria.PeriodMastId.Periodmastid})
            'cmd.CommandText = String.Format(sqlText, authenstr)
            'cmd.Transaction = trn
            'cmd.ExecuteNonQuery()
            '================================================

            'Changed by Chanchira L. on 21/02/2020 
            'sqlText = "Delete From INTERFACETEMP_EE A WHERE  1=1 {0}"
            sqlText = "delete from interfacetemp_ee A " & _
                      "where A.EMPLFLAG = NVL (:EMPLFLAG, A.EMPLFLAG) " & _
                      "AND A.MANAGER_LEVEL = NVL (:MANAGERLVL, A.MANAGER_LEVEL) " & _
                      "AND A.EMPL_CLASS = NVL (:EMPLCLASS, A.EMPL_CLASS)   " & _
                      "AND A.COMPANY = :COMP " & _
                      "AND A.PAYORGCODE like NVL(:PAYORGCODE || '%',A.PAYORGCODE) " & _
                      "AND A.EFFDT <= :C_EDDATE " & _
                      "AND A.EMPLID = NVL (:EMPLID, A.EMPLID) " & _
                      "AND A.PERIODMASTID = :PERIODMASTID  {0} "

            cmd.Parameters.Clear()
            cmd.Parameters.Add(New OracleParameter With {.ParameterName = "EMPLFLAG", .Value = If(_criteria.EmplFlag = "A", "", _criteria.EmplFlag)})
            cmd.Parameters.Add(New OracleParameter With {.ParameterName = "MANAGERLVL", .Value = If(String.IsNullOrEmpty(_criteria.Managerlvl), String.Empty, _criteria.Managerlvl)})
            cmd.Parameters.Add(New OracleParameter With {.ParameterName = "EMPLCLASS", .Value = If(String.IsNullOrEmpty(_criteria.EmplClass), String.Empty, _criteria.EmplClass)})
            cmd.Parameters.Add(New OracleParameter With {.ParameterName = "COMP", .Value = _criteria.Company})
            cmd.Parameters.Add(New OracleParameter With {.ParameterName = "PAYORGCODE", .Value = If(_criteria.Org Is Nothing, "", _criteria.Org.Relateid)})
            'cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_STDATE", .Value = _criteria.StartDate})
            cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_EDDATE", .Value = _criteria.EndDate})
            cmd.Parameters.Add(New OracleParameter With {.ParameterName = "EMPLID", .Value = If(String.IsNullOrEmpty(_criteria.Emplid), String.Empty, _criteria.Emplid)})
            cmd.Parameters.Add(New OracleParameter With {.ParameterName = "PERIODMASTID", .Value = _criteria.PeriodMastId.Periodmastid})

            cmd.CommandText = String.Format(sqlText, authenstr)
            cmd.Transaction = trn
            cmd.ExecuteNonQuery()
            '================================================

            'sqlText = "DELETE FROM INTERFACETEMP_EE A  " & _
            '            "WHERE A.ISINTERFACE = 'Y' AND A.EMPLFLAG = NVL (:EMPLFLAG, A.EMPLFLAG) AND A.MANAGER_LEVEL = NVL (:MANAGERLVL, A.MANAGER_LEVEL) AND A.EMPL_CLASS = NVL (:EMPLCLASS, A.EMPL_CLASS)   " & _
            '            "AND A.COMPANY = :COMP AND A.PAYORGCODE like NVL(:PAYORGCODE || '%',A.PAYORGCODE) AND A.EFFDT BETWEEN :C_STDATE AND :C_EDDATE AND A.EMPLID = NVL (:EMPLID, A.EMPLID) AND A.PERIODMASTID = :PERIODMASTID  {0} "
            'cmd.Parameters.Clear()

            'cmd.Parameters.Add(New OracleParameter With {.ParameterName = "EMPLFLAG", .Value = If(_criteria.EmplFlag = "A", "", _criteria.EmplFlag)})
            'cmd.Parameters.Add(New OracleParameter With {.ParameterName = "MANAGERLVL", .Value = If(String.IsNullOrEmpty(_criteria.Managerlvl), String.Empty, _criteria.Managerlvl)})
            'cmd.Parameters.Add(New OracleParameter With {.ParameterName = "EMPLCLASS", .Value = If(String.IsNullOrEmpty(_criteria.EmplClass), String.Empty, _criteria.EmplClass)})
            'cmd.Parameters.Add(New OracleParameter With {.ParameterName = "COMP", .Value = _criteria.Company})
            'cmd.Parameters.Add(New OracleParameter With {.ParameterName = "PAYORGCODE", .Value = If(_criteria.Org Is Nothing, "", _criteria.Org.Relateid)})
            'cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_STDATE", .Value = _criteria.StartDate})
            'cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_EDDATE", .Value = _criteria.EndDate})
            'cmd.Parameters.Add(New OracleParameter With {.ParameterName = "EMPLID", .Value = If(String.IsNullOrEmpty(_criteria.Emplid), String.Empty, _criteria.Emplid)})
            'cmd.Parameters.Add(New OracleParameter With {.ParameterName = "PERIODMASTID", .Value = _criteria.PeriodMastId.Periodmastid})

            'cmd.CommandText = String.Format(sqlText, authenstr)
            'cmd.Transaction = trn
            'cmd.ExecuteNonQuery()
            '================================================
            '        trn.Commit()
            '    Catch ex As Exception
            '        trn.Rollback()
            '        Throw ex
            '    End Try
            'Catch ex As Exception
            '    Throw ex
            'Finally
            '    con.Close()
            'End Try

            'Try
            '    If cmd.Connection.State = ConnectionState.Closed Then
            '        cmd.Connection.Open()
            '    End If

            '    Dim trn As System.Data.Common.DbTransaction = con.BeginTransaction()
            '    Try

            '------------------------------------------
            'Changed by Chanchira L. on 19/04/2021
            Dim sqlInsEmployee As String = ""
            Dim sqlInsCompen As String = ""
            'FindEmployeeForInterface(param, _criteria, _dtoTempEE)
            'FindCompenForInterface(param, _criteria, _dtoTempComp)
            'FindCompanyDtl(param, _criteria, _dtoCompanyDtl)
            FindEmployeeForInterface(param, _criteria, sqlInsEmployee)
            FindCompenForInterface(param, _criteria, sqlInsCompen)
            '------------------------------------------

            Try
                If sqlInsEmployee <> "" Then
                    Dim insertstr = "INSERT INTO INTERFACETEMP_EE (EMPLID,EMPL_RCD,EFFDT,EFFSEQ,ACTION,ACTION_DT,ACTION_REASON,PER_ORG,DEPTID,JOBCODE,POSITION_NBR,POSITION_LEVEL,REPORT_TO," & _
                                    "POSN_OVRD,HR_STATUS,EMPL_STATUS,LOCATION,JOB_ENTRY_DT,DEPT_ENTRY_DT,POSITION_ENTRY_DT,POSNLEVEL_ENTRY_DT,SHIFT,REG_TEMP,FULL_PART_TIME, " & _
                                    "COMPANY,PAYGROUP,POITYPE,EMPLGROUP,EMPLIDCODE,HOLIDAY_SCHEDULE,STD_HOURS,STD_HRS_FREQUENCY,OFFICER_CD,EMPL_CLASS,GRADE,GRADE_ENTRY_DT,COMP_FREQUENCY," & _
                                    "COMPRATE,CHANGE_AMT,CHANGE_PCT,CURRENCY_CD,BUSINESS_UNIT,SETID_DEPT,SETID_JOBCODE,HIRE_DT,LAST_HIRE_DT,TERMINATION_DT,ASGN_START_DT,LST_ASGN_START_DT," & _
                                    "ASGN_END_DT,LAST_DATE_WORKED,EXPECTED_RETURN_DT,EXPECTED_END_DATE,PC_DATE_CPG,PROBATION_DT,PROBATION,PROBATION_TYPE,SOCIALWELF_PREFIX,CALSOCIALWELF," & _
                                    "SOCIALWELFBEFYN,SOCIALWELFID,PERCENTSOCIALWELF,SOCIAL_BRANCH_CPG,ISCOMPSOCIALWELF,PAYORGCODE,PERIODMASTID,BONUS,CALTAXMETHOD,CCA_CPG,JOB_INDICATOR,JOBOPEN_NO," & _
                                    "PAYROLLID,MANAGER_LEVEL,SAL_ADMIN_PLAN,EMPLFLAG,ISINTERFACE,PRE_COMPANY,CREATEUSER,CREATEDATE,MODIFYDATE,PROGRAMCODE,REMARKS,RATEID,RATEQTY,AMOUNT,INCEXPCODE,INCEXPAMT,INCEXPQTY," & _
                                    "EMPL_TITLE,EMPL_NAME,EMPL_SEX,EMP_MARRYSTATUS,STATUS,BIRTHDATE,BANKCODE,ACCOUNTID,BANKACCOUNT,BANK_BRANCH,HARMFUL,WORKHOUR,NID,ADDRESS1,ADDRESS2,SCNO,TAXID,WORKDAY_MONTH) "

                    cmd.CommandText = insertstr & sqlInsEmployee

                    cmd.Parameters.Clear()

                    cmd.Parameters.Add(New OracleParameter With {.ParameterName = "LANG", .Value = param.Lang})
                    cmd.Parameters.Add(New OracleParameter With {.ParameterName = "LOCAL", .Value = param.LocalLang})
                    cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_STDATE", .Value = _criteria.StartDate})
                    cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_EDDATE", .Value = _criteria.EndDate})
                    cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_EMPLCLASS", .Value = If(String.IsNullOrEmpty(_criteria.EmplClass), "", _criteria.EmplClass)})
                    cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_COMP", .Value = _criteria.Company})
                    cmd.Parameters.Add(New OracleParameter With {.ParameterName = "PAYORGCODE", .Value = If(_criteria.Org Is Nothing, "", _criteria.Org.Relateid)})
                    cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_EMPLID", .Value = If(String.IsNullOrEmpty(_criteria.Emplid), "", _criteria.Emplid)})
                    cmd.Parameters.Add(New OracleParameter With {.ParameterName = "EMPLFLAG", .Value = _criteria.EmplFlag})
                    cmd.Parameters.Add(New OracleParameter With {.ParameterName = "MANAGERLVL", .Value = If(String.IsNullOrEmpty(_criteria.Managerlvl), "", _criteria.Managerlvl)})
                    cmd.Parameters.Add(New OracleParameter With {.ParameterName = "PERIODMASTID", .Value = _criteria.PeriodMastId.Periodmastid})

                    cmd.Transaction = trn
                    cmd.ExecuteNonQuery()
                End If

                'If _dtoTempEE IsNot Nothing AndAlso _dtoTempEE.Count > 0 Then
                '    'insert to temporary table of master

                '    For Each _dto In _dtoTempEE
                '        'Dim insertstr = "INSERT INTO INTERFACETEMP_EE (EMPLID,EMPL_RCD,EFFDT,EFFSEQ,ACTION,ACTION_DT,ACTION_REASON,PER_ORG,DEPTID,JOBCODE,POSITION_NBR,POSITION_LEVEL,REPORT_TO," & _
                '        '                       "POSN_OVRD,HR_STATUS,EMPL_STATUS,LOCATION,JOB_ENTRY_DT,DEPT_ENTRY_DT,POSITION_ENTRY_DT,POSNLEVEL_ENTRY_DT,SHIFT,REG_TEMP,FULL_PART_TIME, " & _
                '        '                       "COMPANY,PAYGROUP,POITYPE,EMPLGROUP,EMPLIDCODE,HOLIDAY_SCHEDULE,STD_HOURS,STD_HRS_FREQUENCY,OFFICER_CD,EMPL_CLASS,GRADE,GRADE_ENTRY_DT,COMP_FREQUENCY," & _
                '        '                       "COMPRATE,CHANGE_AMT,CHANGE_PCT,CURRENCY_CD,BUSINESS_UNIT,SETID_DEPT,SETID_JOBCODE,HIRE_DT,LAST_HIRE_DT,TERMINATION_DT,ASGN_START_DT,LST_ASGN_START_DT," & _
                '        '                       "ASGN_END_DT,LAST_DATE_WORKED,EXPECTED_RETURN_DT,EXPECTED_END_DATE,PC_DATE_CPG,PROBATION_DT,PROBATION,PROBATION_TYPE,SOCIALWELF_PREFIX,CALSOCIALWELF," & _
                '        '                       "SOCIALWELFBEFYN,SOCIALWELFID,PERCENTSOCIALWELF,SOCIAL_BRANCH_CPG,ISCOMPSOCIALWELF,PAYORGCODE,PERIODMASTID,BONUS,CALTAXMETHOD,CCA_CPG,JOB_INDICATOR,JOBOPEN_NO," & _
                '        '                       "PAYROLLID,MANAGER_LEVEL,SAL_ADMIN_PLAN,EMPLFLAG,ISINTERFACE,PRE_COMPANY,CREATEUSER,CREATEDATE,MODIFYDATE,PROGRAMCODE,REMARKS,RATEID,RATEQTY,AMOUNT,INCEXPCODE,INCEXPAMT,INCEXPQTY," & _
                '        '                       "EMPL_TITLE,EMPL_NAME,EMPL_SEX,EMP_MARRYSTATUS,STATUS,BIRTHDATE,BANKCODE,ACCOUNTID,BANKACCOUNT,BANK_BRANCH,HARMFUL,WORKHOUR,NID,ADDRESS1,ADDRESS2) " & _
                '        '          "VALUES(:EMPLID,:EMPL_RCD,:EFFDT,:EFFSEQ,:ACTION,:ACTION_DT,:ACTION_REASON,:PER_ORG,:DEPTID,:JOBCODE,:POSITION_NBR,:POSITION_LEVEL,:REPORT_TO,:POSN_OVRD,:HR_STATUS,:EMPL_STATUS,:LOCATION," & _
                '        '                 If(_dto.JobEntryDt Is Nothing, "NULL,", ":JOB_ENTRY_DT,") & If(_dto.DeptEntryDt Is Nothing, "NULL,", ":DEPT_ENTRY_DT,") & If(_dto.PositionEntryDt Is Nothing, "NULL,", ":POSITION_ENTRY_DT,") & _
                '        '                 If(_dto.PosnlevelEntryDt Is Nothing, "NULL,", ":POSNLEVEL_ENTRY_DT,") & ":SHIFT,:REG_TEMP,:FULL_PART_TIME,:COMPANY,:PAYGROUP,:POITYPE,:EMPLGROUP,:EMPLIDCODE,:HOLIDAY_SCHEDULE," & _
                '        '                 ":STD_HOURS,:STD_HRS_FREQUENCY,:OFFICER_CD,:EMPL_CLASS,:GRADE," & If(_dto.GradeEntryDt Is Nothing, "NULL,", ":GRADE_ENTRY_DT,") & ":COMP_FREQUENCY,:COMPRATE,:CHANGE_AMT,:CHANGE_PCT," & _
                '        '                 ":CURRENCY_CD,:BUSINESS_UNIT,:SETID_DEPT,:SETID_JOBCODE," & If(_dto.HireDt Is Nothing, "NULL,", ":HIRE_DT,") & If(_dto.LastHireDt Is Nothing, "NULL,", ":LAST_HIRE_DT,") & _
                '        '                 If(_dto.TerminationDt Is Nothing, "NULL,", ":TERMINATION_DT,") & If(_dto.AsgnStartDt Is Nothing, "NULL,", ":ASGN_START_DT,") & If(_dto.LstAsgnStartDt Is Nothing, "NULL,", ":LST_ASGN_START_DT,") & _
                '        '                 If(_dto.AsgnEndDt Is Nothing, "NULL,", ":ASGN_END_DT,") & If(_dto.LastDateWorked Is Nothing, "NULL,", ":LAST_DATE_WORKED,") & If(_dto.ExpectedReturnDt Is Nothing, "NULL,", ":EXPECTED_RETURN_DT,") & _
                '        '                 If(_dto.ExpectedEndDate Is Nothing, "NULL,", ":EXPECTED_END_DATE,") & If(_dto.PcDateCpg Is Nothing, "NULL,", ":PC_DATE_CPG,") & If(_dto.ProbationDt Is Nothing, "NULL,", ":PROBATION_DT,") & ":PROBATION," & _
                '        '                 ":PROBATION_TYPE,:SOCIALWELF_PREFIX,:CALSOCIALWELF,:SOCIALWELFBEFYN,:SOCIALWELFID,:PERCENTSOCIALWELF,:SOCIAL_BRANCH_CPG,:ISCOMPSOCIALWELF,:PAYORGCODE," & _
                '        '                  If(_dto.Periodmastid.HasValue OrElse (_criteria.PeriodMastId IsNot Nothing AndAlso _criteria.PeriodMastId.Periodmastid <> 0), ":PERIODMASTID,", "NULL,") & _
                '        '                 ":BONUS,:CALTAXMETHOD,:CCA_CPG,:JOB_INDICATOR,:JOBOPEN_NO,:PAYROLLID,:MANAGER_LEVEL,:SAL_ADMIN_PLAN,:EMPLFLAG,:ISINTERFACE,:PRE_COMPANY,:CREATEUSER," & If(_dto.Createdate Is Nothing, "NULL,", ":CREATEDATE,") & _
                '        '                 If(_dto.Modifydate Is Nothing, "NULL,", ":MODIFYDATE,") & ":PROGRAMCODE,:REMARKS,:RATEID,:RATEQTY,:AMOUNT,:INCEXPCODE,:INCEXPAMT,:INCEXPQTY," & _
                '        '                 ":EMPL_TITLE,:EMPL_NAME,:EMPL_SEX,:EMP_MARRYSTATUS,:STATUS," & If(_dto.Birthdate.HasValue, ":BIRTHDATE,", "NULL,") & ":BANKCODE," & If(_dto.Accountid.HasValue, ":ACCOUNTID,", "NULL,") & ":BANKACCOUNT,:BANK_BRANCH,:HARMFUL,:WORKHOUR,:NID,:ADDRESS1,:ADDRESS2)"

                '        Dim insertstr = "INSERT INTO INTERFACETEMP_EE (EMPLID,EMPL_RCD,EFFDT,EFFSEQ,ACTION,ACTION_DT,ACTION_REASON,PER_ORG,DEPTID,JOBCODE,POSITION_NBR,POSITION_LEVEL,REPORT_TO," & _
                '                             "POSN_OVRD,HR_STATUS,EMPL_STATUS,LOCATION,JOB_ENTRY_DT,DEPT_ENTRY_DT,POSITION_ENTRY_DT,POSNLEVEL_ENTRY_DT,SHIFT,REG_TEMP,FULL_PART_TIME, " & _
                '                             "COMPANY,PAYGROUP,POITYPE,EMPLGROUP,EMPLIDCODE,HOLIDAY_SCHEDULE,STD_HOURS,STD_HRS_FREQUENCY,OFFICER_CD,EMPL_CLASS,GRADE,GRADE_ENTRY_DT,COMP_FREQUENCY," & _
                '                             "COMPRATE,CHANGE_AMT,CHANGE_PCT,CURRENCY_CD,BUSINESS_UNIT,SETID_DEPT,SETID_JOBCODE,HIRE_DT,LAST_HIRE_DT,TERMINATION_DT,ASGN_START_DT,LST_ASGN_START_DT," & _
                '                             "ASGN_END_DT,LAST_DATE_WORKED,EXPECTED_RETURN_DT,EXPECTED_END_DATE,PC_DATE_CPG,PROBATION_DT,PROBATION,PROBATION_TYPE,SOCIALWELF_PREFIX,CALSOCIALWELF," & _
                '                             "SOCIALWELFBEFYN,SOCIALWELFID,PERCENTSOCIALWELF,SOCIAL_BRANCH_CPG,ISCOMPSOCIALWELF,PAYORGCODE,PERIODMASTID,BONUS,CALTAXMETHOD,CCA_CPG,JOB_INDICATOR,JOBOPEN_NO," & _
                '                             "PAYROLLID,MANAGER_LEVEL,SAL_ADMIN_PLAN,EMPLFLAG,ISINTERFACE,PRE_COMPANY,CREATEUSER,CREATEDATE,MODIFYDATE,PROGRAMCODE,REMARKS,RATEID,RATEQTY,AMOUNT,INCEXPCODE,INCEXPAMT,INCEXPQTY," & _
                '                             "EMPL_TITLE,EMPL_NAME,EMPL_SEX,EMP_MARRYSTATUS,STATUS,BIRTHDATE,BANKCODE,ACCOUNTID,BANKACCOUNT,BANK_BRANCH,HARMFUL,WORKHOUR,NID,ADDRESS1,ADDRESS2,SCNO,TAXID,WORKDAY_MONTH)" & _
                '                     "VALUES(:EMPLID,:EMPL_RCD,:EFFDT,:EFFSEQ,:ACTION,:ACTION_DT,:ACTION_REASON,:PER_ORG,:DEPTID,:JOBCODE,:POSITION_NBR,:POSITION_LEVEL,:REPORT_TO,:POSN_OVRD,:HR_STATUS,:EMPL_STATUS,:LOCATION," & _
                '                       If(_dto.JobEntryDt Is Nothing, "NULL,", ":JOB_ENTRY_DT,") & If(_dto.DeptEntryDt Is Nothing, "NULL,", ":DEPT_ENTRY_DT,") & If(_dto.PositionEntryDt Is Nothing, "NULL,", ":POSITION_ENTRY_DT,") & _
                '                       If(_dto.PosnlevelEntryDt Is Nothing, "NULL,", ":POSNLEVEL_ENTRY_DT,") & ":SHIFT,:REG_TEMP,:FULL_PART_TIME,:COMPANY,:PAYGROUP,:POITYPE,:EMPLGROUP,:EMPLIDCODE,:HOLIDAY_SCHEDULE," & _
                '                       ":STD_HOURS,:STD_HRS_FREQUENCY,:OFFICER_CD,:EMPL_CLASS,:GRADE," & If(_dto.GradeEntryDt Is Nothing, "NULL,", ":GRADE_ENTRY_DT,") & ":COMP_FREQUENCY,:COMPRATE,:CHANGE_AMT,:CHANGE_PCT," & _
                '                       ":CURRENCY_CD,:BUSINESS_UNIT,:SETID_DEPT,:SETID_JOBCODE," & If(_dto.HireDt Is Nothing, "NULL,", ":HIRE_DT,") & If(_dto.LastHireDt Is Nothing, "NULL,", ":LAST_HIRE_DT,") & _
                '                       If(_dto.TerminationDt Is Nothing, "NULL,", ":TERMINATION_DT,") & If(_dto.AsgnStartDt Is Nothing, "NULL,", ":ASGN_START_DT,") & If(_dto.LstAsgnStartDt Is Nothing, "NULL,", ":LST_ASGN_START_DT,") & _
                '                       If(_dto.AsgnEndDt Is Nothing, "NULL,", ":ASGN_END_DT,") & If(_dto.LastDateWorked Is Nothing, "NULL,", ":LAST_DATE_WORKED,") & If(_dto.ExpectedReturnDt Is Nothing, "NULL,", ":EXPECTED_RETURN_DT,") & _
                '                       If(_dto.ExpectedEndDate Is Nothing, "NULL,", ":EXPECTED_END_DATE,") & If(_dto.PcDateCpg Is Nothing, "NULL,", ":PC_DATE_CPG,") & If(_dto.ProbationDt Is Nothing, "NULL,", ":PROBATION_DT,") & ":PROBATION," & _
                '                       ":PROBATION_TYPE,:SOCIALWELF_PREFIX,:CALSOCIALWELF,:SOCIALWELFBEFYN,:SOCIALWELFID,:PERCENTSOCIALWELF,:SOCIAL_BRANCH_CPG,:ISCOMPSOCIALWELF,:PAYORGCODE," & _
                '                        If(_dto.Periodmastid.HasValue OrElse (_criteria.PeriodMastId IsNot Nothing AndAlso _criteria.PeriodMastId.Periodmastid <> 0), ":PERIODMASTID,", "NULL,") & _
                '                       ":BONUS,:CALTAXMETHOD,:CCA_CPG,:JOB_INDICATOR,:JOBOPEN_NO,:PAYROLLID,:MANAGER_LEVEL,:SAL_ADMIN_PLAN,:EMPLFLAG,:ISINTERFACE,:PRE_COMPANY,:CREATEUSER," & If(_dto.Createdate Is Nothing, "NULL,", ":CREATEDATE,") & _
                '                       If(_dto.Modifydate Is Nothing, "NULL,", ":MODIFYDATE,") & ":PROGRAMCODE,:REMARKS,:RATEID,:RATEQTY,:AMOUNT,:INCEXPCODE,:INCEXPAMT,:INCEXPQTY," & _
                '                       ":EMPL_TITLE,:EMPL_NAME,:EMPL_SEX,:EMP_MARRYSTATUS,:STATUS," & If(_dto.Birthdate.HasValue, ":BIRTHDATE,", "NULL,") & ":BANKCODE," & If(_dto.Accountid.HasValue, ":ACCOUNTID,", "NULL,") & ":BANKACCOUNT,:BANK_BRANCH,:HARMFUL,:WORKHOUR,:NID,:ADDRESS1,:ADDRESS2, " & _
                '                       ":SCNO, :TAXID, :WORKDAY_MONTH)"

                '        cmd.CommandText = insertstr

                '        cmd.Parameters.Clear()

                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "EMPLID", .Value = _dto.Emplid})
                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "EMPL_RCD", .Value = _dto.EmplRcd})
                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "EFFDT", .Value = _dto.Effdt})
                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "EFFSEQ", .Value = _dto.Effseq})
                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "ACTION", .Value = _dto.Action})
                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "ACTION_DT", .Value = _dto.ActionDt})
                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "ACTION_REASON", .Value = _dto.ActionReason})
                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "PER_ORG", .Value = If(String.IsNullOrEmpty(_dto.PerOrg), "", _dto.PerOrg)})
                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "DEPTID", .Value = If(String.IsNullOrEmpty(_dto.Deptid), "", _dto.Deptid)})
                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "JOBCODE", .Value = If(String.IsNullOrEmpty(_dto.Jobcode), "", _dto.Jobcode)})
                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "POSITION_NBR", .Value = If(String.IsNullOrEmpty(_dto.PositionNbr), "", _dto.PositionNbr)})
                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "POSITION_LEVEL", .Value = If(String.IsNullOrEmpty(_dto.PositionLevel), "", _dto.PositionLevel)})
                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "REPORT_TO", .Value = If(String.IsNullOrEmpty(_dto.ReportTo), "", _dto.ReportTo)})
                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "POSN_OVRD", .Value = _dto.PosnOvrd})
                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "HR_STATUS", .Value = If(String.IsNullOrEmpty(_dto.HrStatus), "", _dto.HrStatus)})
                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "EMPL_STATUS", .Value = If(String.IsNullOrEmpty(_dto.EmplStatus), "", _dto.EmplStatus)})
                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "LOCATION", .Value = If(String.IsNullOrEmpty(_dto.Location), "", _dto.Location)})
                '        If _dto.JobEntryDt IsNot Nothing Then
                '            cmd.Parameters.Add(New OracleParameter With {.ParameterName = "JOB_ENTRY_DT", .Value = _dto.JobEntryDt})
                '        End If
                '        If _dto.DeptEntryDt IsNot Nothing Then
                '            cmd.Parameters.Add(New OracleParameter With {.ParameterName = "DEPT_ENTRY_DT", .Value = _dto.DeptEntryDt})
                '        End If
                '        If _dto.PositionEntryDt IsNot Nothing Then
                '            cmd.Parameters.Add(New OracleParameter With {.ParameterName = "POSITION_ENTRY_DT", .Value = _dto.PositionEntryDt})
                '        End If
                '        If _dto.PosnlevelEntryDt IsNot Nothing Then
                '            cmd.Parameters.Add(New OracleParameter With {.ParameterName = "POSNLEVEL_ENTRY_DT", .Value = _dto.PosnlevelEntryDt})
                '        End If
                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "SHIFT", .Value = If(String.IsNullOrEmpty(_dto.Shift), "", _dto.Shift)})
                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "REG_TEMP", .Value = If(String.IsNullOrEmpty(_dto.RegTemp), "", _dto.RegTemp)})
                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "FULL_PART_TIME", .Value = If(String.IsNullOrEmpty(_dto.FullPartTime), "", _dto.FullPartTime)})
                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "COMPANY", .Value = If(String.IsNullOrEmpty(_dto.Company), "", _dto.Company)})
                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "PAYGROUP", .Value = If(String.IsNullOrEmpty(_dto.Paygroup), "", _dto.Paygroup)})
                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "POITYPE", .Value = If(String.IsNullOrEmpty(_dto.Poitype), "", _dto.Poitype)})
                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "EMPLGROUP", .Value = If(String.IsNullOrEmpty(_dto.Emplgroup), "", _dto.Emplgroup)})
                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "EMPLIDCODE", .Value = If(String.IsNullOrEmpty(_dto.Emplidcode), "", _dto.Emplidcode)})
                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "HOLIDAY_SCHEDULE", .Value = If(String.IsNullOrEmpty(_dto.HolidaySchedule), "", _dto.HolidaySchedule)})
                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "STD_HOURS", .Value = If(_dto.StdHours Is Nothing, 0, _dto.StdHours)})
                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "STD_HRS_FREQUENCY", .Value = If(String.IsNullOrEmpty(_dto.StdHrsFrequency), "", _dto.StdHrsFrequency)})
                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "OFFICER_CD", .Value = If(String.IsNullOrEmpty(_dto.OfficerCd), "", _dto.OfficerCd)})
                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "EMPL_CLASS", .Value = If(String.IsNullOrEmpty(_dto.EmplClass), "", _dto.EmplClass)})
                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "GRADE", .Value = If(String.IsNullOrEmpty(_dto.Grade), "", _dto.Grade)})
                '        If _dto.GradeEntryDt IsNot Nothing Then
                '            cmd.Parameters.Add(New OracleParameter With {.ParameterName = "GRADE_ENTRY_DT", .Value = _dto.GradeEntryDt})
                '        End If

                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "COMP_FREQUENCY", .Value = If(String.IsNullOrEmpty(_dto.CompFrequency), "", _dto.CompFrequency)})
                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "COMPRATE", .Value = If(_dto.Comprate Is Nothing, 0, _dto.Comprate)})
                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "CHANGE_AMT", .Value = If(_dto.ChangeAmt Is Nothing, 0, _dto.ChangeAmt)})
                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "CHANGE_PCT", .Value = If(_dto.ChangePct Is Nothing, 0, _dto.ChangePct)})
                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "CURRENCY_CD", .Value = If(String.IsNullOrEmpty(_dto.CurrencyCd), "", _dto.CurrencyCd)})
                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "BUSINESS_UNIT", .Value = If(String.IsNullOrEmpty(_dto.BusinessUnit), "", _dto.BusinessUnit)})
                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "SETID_DEPT", .Value = If(String.IsNullOrEmpty(_dto.SetidDept), "", _dto.SetidDept)})
                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "SETID_JOBCODE", .Value = If(String.IsNullOrEmpty(_dto.SetidJobcode), "", _dto.SetidJobcode)})
                '        If _dto.HireDt IsNot Nothing Then
                '            cmd.Parameters.Add(New OracleParameter With {.ParameterName = "HIRE_DT", .Value = _dto.HireDt})
                '        End If
                '        If _dto.LastHireDt IsNot Nothing Then
                '            cmd.Parameters.Add(New OracleParameter With {.ParameterName = "LAST_HIRE_DT", .Value = _dto.LastHireDt})
                '        End If
                '        If _dto.TerminationDt IsNot Nothing Then
                '            cmd.Parameters.Add(New OracleParameter With {.ParameterName = "TERMINATION_DT", .Value = _dto.TerminationDt})
                '        End If
                '        If _dto.AsgnStartDt IsNot Nothing Then
                '            cmd.Parameters.Add(New OracleParameter With {.ParameterName = "ASGN_START_DT", .Value = _dto.AsgnStartDt})
                '        End If
                '        If _dto.LstAsgnStartDt IsNot Nothing Then
                '            cmd.Parameters.Add(New OracleParameter With {.ParameterName = "LST_ASGN_START_DT", .Value = _dto.LstAsgnStartDt})
                '        End If
                '        If _dto.AsgnEndDt IsNot Nothing Then
                '            cmd.Parameters.Add(New OracleParameter With {.ParameterName = "ASGN_END_DT", .Value = _dto.AsgnEndDt})
                '        End If
                '        If _dto.LastDateWorked IsNot Nothing Then
                '            cmd.Parameters.Add(New OracleParameter With {.ParameterName = "LAST_DATE_WORKED", .Value = _dto.LastDateWorked})
                '        End If
                '        If _dto.ExpectedReturnDt IsNot Nothing Then
                '            cmd.Parameters.Add(New OracleParameter With {.ParameterName = "EXPECTED_RETURN_DT", .Value = _dto.ExpectedReturnDt})
                '        End If
                '        If _dto.ExpectedEndDate IsNot Nothing Then
                '            cmd.Parameters.Add(New OracleParameter With {.ParameterName = "EXPECTED_END_DATE", .Value = _dto.ExpectedEndDate})
                '        End If
                '        If _dto.PcDateCpg IsNot Nothing Then
                '            cmd.Parameters.Add(New OracleParameter With {.ParameterName = "PC_DATE_CPG", .Value = _dto.PcDateCpg})
                '        End If
                '        If _dto.ProbationDt IsNot Nothing Then
                '            cmd.Parameters.Add(New OracleParameter With {.ParameterName = "PROBATION_DT", .Value = _dto.ProbationDt})
                '        End If

                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "PROBATION", .Value = If(_dto.Probation Is Nothing, 0, _dto.Probation)})
                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "PROBATION_TYPE", .Value = If(String.IsNullOrEmpty(_dto.ProbationType), "", _dto.ProbationType)})
                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "SOCIALWELF_PREFIX", .Value = If(String.IsNullOrEmpty(_dto.SocialwelfPrefix), "", _dto.SocialwelfPrefix)})
                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "CALSOCIALWELF", .Value = If(String.IsNullOrEmpty(_dto.Calsocialwelf), "", _dto.Calsocialwelf)})
                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "SOCIALWELFBEFYN", .Value = If(String.IsNullOrEmpty(_dto.Socialwelfbefyn), "", _dto.Socialwelfbefyn)})
                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "SOCIALWELFID", .Value = If(String.IsNullOrEmpty(_dto.Socialwelfid), "", _dto.Socialwelfid)})
                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "PERCENTSOCIALWELF", .Value = If(_dto.Percentsocialwelf Is Nothing, 0, _dto.Percentsocialwelf)})
                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "SOCIAL_BRANCH_CPG", .Value = If(String.IsNullOrEmpty(_dto.SocialBranchCpg), "", _dto.SocialBranchCpg)})
                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "ISCOMPSOCIALWELF", .Value = If(String.IsNullOrEmpty(_dto.Iscompsocialwelf), "", _dto.Iscompsocialwelf)})
                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "PAYORGCODE", .Value = If(String.IsNullOrEmpty(_dto.Payorgcode), "", _dto.Payorgcode)})
                '        If _dto.Periodmastid.HasValue OrElse (_criteria.PeriodMastId IsNot Nothing AndAlso _criteria.PeriodMastId.Periodmastid <> 0) Then
                '            cmd.Parameters.Add(New OracleParameter With {.ParameterName = "PERIODMASTID", .Value = If(_dto.Periodmastid.HasValue, _dto.Periodmastid, _criteria.PeriodMastId.Periodmastid)})
                '        End If
                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "BONUS", .Value = If(_dto.Bonus Is Nothing, 0, _dto.Bonus)})
                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "CALTAXMETHOD", .Value = If(String.IsNullOrEmpty(_dto.Caltaxmethod), "", _dto.Caltaxmethod)})
                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "CCA_CPG", .Value = If(String.IsNullOrEmpty(_dto.CcaCpg), "", _dto.CcaCpg)})
                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "JOB_INDICATOR", .Value = If(String.IsNullOrEmpty(_dto.JobIndicator), "", _dto.JobIndicator)})
                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "JOBOPEN_NO", .Value = If(String.IsNullOrEmpty(_dto.JobopenNo), "", _dto.JobopenNo)})
                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "PAYROLLID", .Value = If(String.IsNullOrEmpty(_dto.Payrollid), "", _dto.Payrollid)})
                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "MANAGER_LEVEL", .Value = If(String.IsNullOrEmpty(_dto.ManagerLevel), "", _dto.ManagerLevel)})
                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "SAL_ADMIN_PLAN", .Value = If(String.IsNullOrEmpty(_dto.SalAdminPlan), "", _dto.SalAdminPlan)})
                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "EMPLFLAG", .Value = If(String.IsNullOrEmpty(_dto.Emplflag), "", _dto.Emplflag)})
                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "ISINTERFACE", .Value = If(String.IsNullOrEmpty(_dto.Isinterface), "", "N")})    '_dto.Isinterface)})
                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "PRE_COMPANY", .Value = If(String.IsNullOrEmpty(_dto.PreCompany), "", _dto.PreCompany)})
                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "CREATEUSER", .Value = If(String.IsNullOrEmpty(_dto.Createuser), "", _dto.Createuser)})

                '        If _dto.Createdate IsNot Nothing Then
                '            'Changed by Chanchira L. on 13/03/2020
                '            'cmd.Parameters.Add(New OracleParameter With {.ParameterName = "CREATEDATE", .Value = Now})
                '            cmd.Parameters.Add(New OracleParameter With {.ParameterName = "CREATEDATE", .Value = _dto.Createdate})
                '        End If

                '        If _dto.Modifydate IsNot Nothing Then
                '            'Changed by Chanchira L. on 13/03/2020
                '            'cmd.Parameters.Add(New OracleParameter With {.ParameterName = "MODIFYDATE", .Value = Now})
                '            cmd.Parameters.Add(New OracleParameter With {.ParameterName = "MODIFYDATE", .Value = _dto.Modifydate})
                '        End If

                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "PROGRAMCODE", .Value = param.ProgramCode})
                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "REMARKS", .Value = If(String.IsNullOrEmpty(_dto.Remarks), "", _dto.Remarks)})
                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "RATEID", .Value = If(String.IsNullOrEmpty(_dto.Rateid), "", _dto.Rateid)})
                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "RATEQTY", .Value = If(_dto.Rateqty Is Nothing, 0, _dto.Rateqty)})
                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "AMOUNT", .Value = If(_dto.Amount Is Nothing, 0, _dto.Amount)})
                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "INCEXPCODE", .Value = If(String.IsNullOrEmpty(_dto.Incexpcode), "", _dto.Incexpcode)})
                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "INCEXPAMT", .Value = If(_dto.Incexpamt Is Nothing, 0, _dto.Incexpamt)})
                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "INCEXPQTY", .Value = If(_dto.Incexpqty Is Nothing, 0, _dto.Incexpqty)})

                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "EMPL_TITLE", .Value = If(String.IsNullOrEmpty(_dto.EmplTitle), "", _dto.EmplTitle)})
                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "EMPL_NAME", .Value = If(String.IsNullOrEmpty(_dto.EmplName), "", _dto.EmplName)})
                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "EMPL_SEX", .Value = If(String.IsNullOrEmpty(_dto.EmplSex), "", _dto.EmplSex)})
                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "EMP_MARRYSTATUS", .Value = If(String.IsNullOrEmpty(_dto.EmpMarrystatus), "", _dto.EmpMarrystatus)})
                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "STATUS", .Value = If(String.IsNullOrEmpty(_dto.Status), "", _dto.Status)})
                '        If _dto.Birthdate.HasValue Then
                '            cmd.Parameters.Add(New OracleParameter With {.ParameterName = "BIRTHDATE", .Value = _dto.Birthdate.Value})
                '        End If
                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "BANKCODE", .Value = If(String.IsNullOrEmpty(_dto.Bankcode), "", _dto.Bankcode)})
                '        If _dto.Accountid.HasValue Then
                '            cmd.Parameters.Add(New OracleParameter With {.ParameterName = "ACCOUNTID", .Value = _dto.Accountid.Value})
                '        End If
                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "BANKACCOUNT", .Value = If(String.IsNullOrEmpty(_dto.Bankaccount), "", _dto.Bankaccount)})
                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "BANK_BRANCH", .Value = If(String.IsNullOrEmpty(_dto.BankBranch), "", _dto.BankBranch)})
                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "HARMFUL", .Value = If(_dto.Harmful.HasValue, _dto.Harmful.Value, 0)})
                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "WORKHOUR", .Value = If(_dto.Workhour.HasValue, _dto.Workhour.Value, 0)})
                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "NID", .Value = If(String.IsNullOrEmpty(_dto.Nid), "", _dto.Nid)})
                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "ADDRESS1", .Value = If(String.IsNullOrEmpty(_dto.Address1), "", _dto.Address1)})
                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "ADDRESS2", .Value = If(String.IsNullOrEmpty(_dto.Address2), "", _dto.Address2)})
                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "SCNO", .Value = If(String.IsNullOrEmpty(_dto.Scno), "", _dto.Scno)})
                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "TAXID", .Value = If(String.IsNullOrEmpty(_dto.Taxid), "", _dto.Taxid)})
                '        If _dto.SatWork = "N" Then
                '            'nosat_workday 
                '            cmd.Parameters.Add(New OracleParameter With {.ParameterName = "WORKDAY_MONTH", .Value = If(_dtoCompanyDtl.NosatWorkday.HasValue, _dtoCompanyDtl.NosatWorkday, 0)})
                '        Else
                '            'sat_workday
                '            cmd.Parameters.Add(New OracleParameter With {.ParameterName = "WORKDAY_MONTH", .Value = If(_dtoCompanyDtl.SatWorkday.HasValue, _dtoCompanyDtl.SatWorkday, 0)})
                '        End If


                '        cmd.Transaction = trn
                '        cmd.ExecuteNonQuery()
                '    Next
                'End If

                If sqlInsEmployee <> "" Then
                    Dim insertstr = "INSERT INTO INTERFACETEMP_COMPEN (EMPLID,EMPL_RCD,EFFDT,EFFSEQ,COMP_RATECD,INCEXPTYPE,PAYQTY,COMPENSATION_RATE,CHANGE_AMT,FREQUENCY,EMPLFLAG," & _
                                    "   ISINTERFACE,CREATEUSER,CREATEDATE,MODIFYDATE,PROGRAMCODE,GRADE,MANAGER_LEVEL,COMPANY, DEPTID, EMPL_CLASS,PERIODMASTID) "

                    cmd.CommandText = insertstr & sqlInsCompen

                    cmd.Parameters.Clear()

                    cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_STDATE", .Value = _criteria.StartDate})
                    cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_EDDATE", .Value = _criteria.EndDate})
                    cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_EMPLCLASS", .Value = If(String.IsNullOrEmpty(_criteria.EmplClass), "", _criteria.EmplClass)})
                    cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_COMP", .Value = _criteria.Company})
                    cmd.Parameters.Add(New OracleParameter With {.ParameterName = "PAYORGCODE", .Value = If(_criteria.Org Is Nothing, "", _criteria.Org.Relateid)})
                    cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_EMPLID", .Value = If(String.IsNullOrEmpty(_criteria.Emplid), "", _criteria.Emplid)})
                    cmd.Parameters.Add(New OracleParameter With {.ParameterName = "EMPLFLAG", .Value = _criteria.EmplFlag})
                    cmd.Parameters.Add(New OracleParameter With {.ParameterName = "MANAGERLVL", .Value = If(String.IsNullOrEmpty(_criteria.Managerlvl), "", _criteria.Managerlvl)})
                    cmd.Parameters.Add(New OracleParameter With {.ParameterName = "PERIODMASTID", .Value = _criteria.PeriodMastId.Periodmastid})

                    cmd.Transaction = trn
                    cmd.ExecuteNonQuery()
                End If

                'If _dtoTempComp IsNot Nothing AndAlso _dtoTempComp.Count > 0 Then
                '    For Each _dto In _dtoTempComp
                '        Dim insertstr = "INSERT INTO INTERFACETEMP_COMPEN (EMPLID,EMPL_RCD,EFFDT,EFFSEQ,COMP_RATECD,INCEXPTYPE,PAYQTY,COMPENSATION_RATE,CHANGE_AMT,FREQUENCY,EMPLFLAG," & _
                '                                               "ISINTERFACE,CREATEUSER,CREATEDATE,MODIFYDATE,PROGRAMCODE,GRADE,MANAGER_LEVEL,COMPANY, DEPTID, EMPL_CLASS,PERIODMASTID) " & _
                '                 "VALUES(:EMPLID,:EMPL_RCD,:EFFDT,:EFFSEQ,:COMP_RATECD,:INCEXPTYPE,:PAYQTY,:COMPENSATION_RATE,:CHANGE_AMT,:FREQUENCY,:EMPLFLAG," & _
                '                        ":ISINTERFACE,:CREATEUSER," & If(_dto.Createdate Is Nothing, "NULL,", ":CREATEDATE,") & If(_dto.Modifydate Is Nothing, "NULL,", ":MODIFYDATE,") & ":PROGRAMCODE,:GRADE,:MANAGER_LEVEL,:COMPANY, :DEPTID, :EMPL_CLASS," & _
                '                        If(_dto.Periodmastid.HasValue OrElse (_criteria.PeriodMastId IsNot Nothing AndAlso _criteria.PeriodMastId.Periodmastid <> 0), ":PERIODMASTID", "NULL") & ")"

                '        cmd.CommandText = insertstr
                '        cmd.Parameters.Clear()

                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "EMPLID", .Value = _dto.Emplid})
                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "EMPL_RCD", .Value = _dto.EmplRcd})
                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "EFFDT", .Value = _dto.Effdt})
                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "EFFSEQ", .Value = _dto.Effseq})
                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "COMP_RATECD", .Value = _dto.CompRatecd})
                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "INCEXPTYPE", .Value = If(String.IsNullOrEmpty(_dto.Incexptype), "", _dto.Incexptype)})
                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "PAYQTY", .Value = If(_dto.Payqty Is Nothing, 0, _dto.Payqty)})
                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "COMPENSATION_RATE", .Value = If(_dto.CompensationRate Is Nothing, 0, _dto.CompensationRate)})
                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "CHANGE_AMT", .Value = If(_dto.ChangeAmt Is Nothing, 0, _dto.ChangeAmt)})
                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "FREQUENCY", .Value = If(String.IsNullOrEmpty(_dto.Frequency), "", _dto.Frequency)})
                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "EMPLFLAG", .Value = If(String.IsNullOrEmpty(_dto.Emplflag), "", _dto.Emplflag)})
                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "ISINTERFACE", .Value = If(String.IsNullOrEmpty(_dto.Isinterface), "", "N")})   '_dto.Isinterface
                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "CREATEUSER", .Value = If(String.IsNullOrEmpty(_dto.Createuser), "", _dto.Createuser)})

                '        If _dto.Createdate IsNot Nothing Then
                '            cmd.Parameters.Add(New OracleParameter With {.ParameterName = "CREATEDATE", .Value = _dto.Createdate})
                '        End If

                '        If _dto.Modifydate IsNot Nothing Then
                '            cmd.Parameters.Add(New OracleParameter With {.ParameterName = "MODIFYDATE", .Value = _dto.Modifydate})
                '        End If

                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "PROGRAMCODE", .Value = If(String.IsNullOrEmpty(_dto.Programcode), "", _dto.Programcode)})
                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "GRADE", .Value = If(String.IsNullOrEmpty(_dto.Grade), "", _dto.Grade)})
                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "MANAGER_LEVEL", .Value = If(String.IsNullOrEmpty(_dto.ManagerLevel), "", _dto.ManagerLevel)})
                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "COMPANY", .Value = If(String.IsNullOrEmpty(_dto.Company), "", _dto.Company)})
                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "DEPTID", .Value = If(String.IsNullOrEmpty(_dto.Deptid), "", _dto.Deptid)})
                '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "EMPL_CLASS", .Value = If(String.IsNullOrEmpty(_dto.EmplClass), "", _dto.EmplClass)})
                '        If _dto.Periodmastid.HasValue OrElse (_criteria.PeriodMastId IsNot Nothing AndAlso _criteria.PeriodMastId.Periodmastid <> 0) Then
                '            cmd.Parameters.Add(New OracleParameter With {.ParameterName = "PERIODMASTID", .Value = If(_dto.Periodmastid.HasValue, _dto.Periodmastid, _criteria.PeriodMastId.Periodmastid)})
                '        End If

                '        cmd.Transaction = trn
                '        cmd.ExecuteNonQuery()
                '    Next
                'End If

                trn.Commit()
            Catch ex As Exception
                trn.Rollback()
                Throw ex
            End Try
        Catch ex As Exception
            Throw ex
        Finally
            con.Close()
        End Try


    End Sub

    Private Sub UpdateDelFlag(ByVal trn As System.Data.Common.DbTransaction, ByVal cmd As System.Data.Common.DbCommand, param As SsCommon.ServiceParam, _cri As Model.ModelCriteriaList, col As String, authenstr As String)
        Dim dpBp As New SsHrCommonService
        Dim sqlText As String = String.Empty

        sqlText = "UPDATE INTERFACE_EE A  SET A.EMPLFLAG = 'D' , A.ISINTERFACE = 'Y' WHERE NOT EXISTS (SELECT '1' FROM INTERFACETEMP_EE X WHERE X.EFFDT BETWEEN :C_STDATE AND :C_EDDATE " & _
                        "AND A.EMPLID = X.EMPLID AND A.EMPL_RCD = X.EMPL_RCD AND A.EFFDT = X.EFFDT AND A.EFFSEQ = X.EFFSEQ ) " & _
                        "AND A.EMPL_CLASS = NVL (:C_EMPLCLASS, A.EMPL_CLASS) AND A.COMPANY = :C_COMP AND A.PAYORGCODE like NVL(:PAYORGCODE || '%',A.PAYORGCODE) AND A.EMPLID = NVL (:C_EMPLID, A.EMPLID) AND A.MANAGER_LEVEL = NVL(:MANAGERLVL,A.MANAGER_LEVEL)  " & _
                        "AND A.EFFDT BETWEEN :C_STDATE AND :C_EDDATE AND A.EMPLFLAG = NVL(:EMPLFLAG,A.EMPLFLAG) AND A.PERIODMASTID = :PERIODMASTID AND A.ISINTERFACE = 'N' {0} "


        cmd.CommandText = String.Format(sqlText, authenstr)
        cmd.Parameters.Clear()
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_EMPLCLASS", .Value = _cri.EmplClass})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_COMP", .Value = _cri.Company})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "PAYORGCODE", .Value = If(_cri.Org Is Nothing, "", _cri.Org.Relateid)})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "MANAGERLVL", .Value = If(String.IsNullOrEmpty(_cri.Managerlvl), "", _cri.Managerlvl)})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_STDATE", .Value = _cri.StartDate})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_EDDATE", .Value = _cri.EndDate})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_EMPLID", .Value = If(String.IsNullOrEmpty(_cri.Emplid), "", _cri.Emplid)})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "EMPLFLAG", .Value = _cri.EmplFlag})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "PERIODMASTID", .Value = _cri.PeriodMastId.Periodmastid})
        cmd.Transaction = trn
        cmd.ExecuteNonQuery()

        'sqlText = "UPDATE INTERFACE_COMPEN B SET EMPLFLAG = 'D' , ISINTERFACE = 'Y' WHERE NOT EXISTS (SELECT '1' FROM INTERFACETEMP_COMPEN A WHERE A.EFFDT BETWEEN :C_STDATE AND :C_EDDATE " & _
        '               "AND B.EMPLID = A.EMPLID AND B.EMPL_RCD = A.EMPL_RCD AND B.EFFDT = A.EFFDT AND B.EFFSEQ = A.EFFSEQ AND B.COMP_RATECD = A.COMP_RATECD " & _
        '               "AND A.EMPL_CLASS = NVL (:C_EMPLCLASS, A.EMPL_CLASS) AND A.COMPANY = :C_COMP AND A.EMPLID = NVL (:C_EMPLID, A.EMPLID) AND A.MANAGER_LEVEL = NVL(:MANAGERLVL,A.MANAGER_LEVEL)  " & _
        '               "AND A.EFFDT BETWEEN :C_STDATE AND :C_EDDATE AND A.EMPLFLAG = NVL(:EMPLFLAG,A.EMPLFLAG) AND A.PERIODMASTID = :PERIODMASTID AND A.ISINTERFACE = 'N' {0} ) AND B.ISINTERFACE = 'N'"


        sqlText = "UPDATE INTERFACE_COMPEN B SET EMPLFLAG = 'D' , ISINTERFACE = 'Y' WHERE NOT EXISTS (SELECT '1' FROM INTERFACETEMP_COMPEN A WHERE A.EFFDT BETWEEN :C_STDATE AND :C_EDDATE " & _
                     "AND B.EMPLID = A.EMPLID AND B.EMPL_RCD = A.EMPL_RCD AND B.EFFDT = A.EFFDT AND B.EFFSEQ = A.EFFSEQ AND B.COMP_RATECD = A.COMP_RATECD " & _
                     "AND A.EMPL_CLASS = NVL (:C_EMPLCLASS, A.EMPL_CLASS) AND A.COMPANY = :C_COMP AND A.EMPLID = NVL (:C_EMPLID, A.EMPLID) AND A.MANAGER_LEVEL = NVL(:MANAGERLVL,A.MANAGER_LEVEL)  " & _
                     "AND A.EFFDT BETWEEN :C_STDATE AND :C_EDDATE AND A.EMPLFLAG = NVL(:EMPLFLAG,A.EMPLFLAG) AND A.PERIODMASTID = :PERIODMASTID AND A.ISINTERFACE = 'N' {0} ) AND B.ISINTERFACE = 'N' " & _
                     "AND B.EMPLFLAG = NVL(:EMPLFLAG,B.EMPLFLAG) AND B.EMPLID = NVL (:C_EMPLID, B.EMPLID)  AND B.EFFDT BETWEEN :C_STDATE AND :C_EDDATE "


        cmd.CommandText = String.Format(sqlText, authenstr)
        cmd.Parameters.Clear()
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_EMPLCLASS", .Value = _cri.EmplClass})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_COMP", .Value = _cri.Company})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "MANAGERLVL", .Value = If(String.IsNullOrEmpty(_cri.Managerlvl), "", _cri.Managerlvl)})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_STDATE", .Value = _cri.StartDate})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_EDDATE", .Value = _cri.EndDate})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_EMPLID", .Value = If(String.IsNullOrEmpty(_cri.Emplid), "", _cri.Emplid)})
        'cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_EMPLID", .Value = "140010"})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "EMPLFLAG", .Value = _cri.EmplFlag})
        'cmd.Parameters.Add(New OracleParameter With {.ParameterName = "EMPLFLAG", .Value = "N"})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "PERIODMASTID", .Value = _cri.PeriodMastId.Periodmastid})
        cmd.Transaction = trn
        cmd.ExecuteNonQuery()



    End Sub


    Private Sub InsertDelFlag(ByVal cmd As System.Data.Common.DbCommand, ByVal Trn As System.Data.Common.DbTransaction, param As SsCommon.ServiceParam, _cri As Model.ModelCriteriaList, col As String, authenstr As String)
        Dim dpBp As New SsHrCommonService
        Dim sqlText As String = String.Empty

        sqlText = "INSERT " & _
                    "INTO INTERFACE_COMPEN " & _
                    "  ( " & _
                    "    EMPLID, " & _
                    "    EMPL_RCD, " & _
                    "    EFFDT, " & _
                    "    EFFSEQ, " & _
                    "    COMP_RATECD, " & _
                    "    INCEXPTYPE, " & _
                    "    PAYQTY, " & _
                    "    COMPENSATION_RATE, " & _
                    "    CHANGE_AMT, " & _
                    "    FREQUENCY, " & _
                    "    EMPLFLAG, " & _
                    "    ISINTERFACE, " & _
                    "    CREATEUSER, " & _
                    "    CREATEDATE, " & _
                    "    MODIFYDATE, " & _
                    "    PROGRAMCODE, " & _
                    "    DATEOFINTERFACE, " & _
                    "    USERINTERFACE " & _
                    "  ) " & _
                    "SELECT A.EMPLID, " & _
                    "  A.EMPL_RCD, " & _
                    "  (SELECT MAX(C.EFFDT) " & _
                    "  FROM INTERFACETEMP_COMPEN C " & _
                    "  WHERE C.EMPLID = A.EMPLID " & _
                    "  AND C.EFFDT BETWEEN :C_STDATE AND :C_EDDATE " & _
                    "  ) EFFDT, " & _
                    "  A.EFFSEQ, " & _
                    "  A.COMP_RATECD, " & _
                    "  A.INCEXPTYPE, " & _
                    "  A.PAYQTY, " & _
                    "  A.COMPENSATION_RATE, " & _
                    "  A.CHANGE_AMT, " & _
                    "  A.FREQUENCY, " & _
                    "  'D', " & _
                    "  'Y', " & _
                    "  A.CREATEUSER, " & _
                    "  A.CREATEDATE, " & _
                    "  A.MODIFYDATE, " & _
                    "  A.PROGRAMCODE, " & _
                    "  :DATEOFINTERFACE, " & _
                    "  :USERINTERFACE " & _
                    "FROM INTERFACETEMP_COMPEN A " & _
                    "INNER JOIN INTERFACE_COMPEN I " & _
                    "ON A.EMPLID         = I.EMPLID " & _
                    "AND A.EMPL_RCD      = I.EMPL_RCD " & _
                    "AND A.EFFDT         = I.EFFDT " & _
                    "AND A.EFFSEQ        = I.EFFSEQ " & _
                    "AND A.COMP_RATECD   = I.COMP_RATECD " & _
                    "AND A.INCEXPTYPE    = I.INCEXPTYPE " & _
                    "AND I.EFFDT       = " & _
                    "  (SELECT MAX(IC.EFFDT) " & _
                    "  FROM INTERFACE_COMPEN IC " & _
                    "  WHERE A.EMPLID    = IC.EMPLID " & _
                    "  AND A.EMPL_RCD    = IC.EMPL_RCD " & _
                    "  AND A.COMP_RATECD = IC.COMP_RATECD " & _
                    "  AND A.INCEXPTYPE  = IC.INCEXPTYPE " & _
                    "  ) " & _
                    "AND I.EFFSEQ = " & _
                    "  (SELECT MAX(IC.EFFSEQ) " & _
                    "  FROM INTERFACE_COMPEN IC " & _
                    "  WHERE A.EMPLID    = IC.EMPLID " & _
                    "  AND A.EMPL_RCD    = IC.EMPL_RCD " & _
                    "  AND A.EFFDT       = IC.EFFDT " & _
                    "  AND A.COMP_RATECD = IC.COMP_RATECD " & _
                    "  AND A.INCEXPTYPE  = IC.INCEXPTYPE " & _
                    "  ) " & _
                    "WHERE I.ISINTERFACE = 'N' " & _
                    "AND I.EMPLFLAG     <> 'D' " & _
                    "AND NOT EXISTS " & _
                    "  (SELECT '1' " & _
                    "  FROM INTERFACETEMP_COMPEN B " & _
                    "  WHERE A.EMPLID    = B.EMPLID " & _
                    "  AND A.EMPL_RCD    = B.EMPL_RCD " & _
                    "  AND A.COMP_RATECD = B.COMP_RATECD " & _
                    "  AND B.EFFDT BETWEEN :C_STDATE AND :C_EDDATE " & _
                    "  ) " & _
                    "   AND A.EMPLID IN " & _
                    "  (SELECT B.EMPLID " & _
                    "  FROM INTERFACETEMP_COMPEN B " & _
                    "  WHERE A.EMPLID    = B.EMPLID " & _
                    "  AND A.EMPL_RCD    = B.EMPL_RCD " & _
                    "  AND B.EFFDT BETWEEN :C_STDATE AND :C_EDDATE " & _
                    "  ) " & _
                    "AND A.EMPLFLAG      = NVL(:EMPLFLAG,A.EMPLFLAG) " & _
                    "AND A.EMPLID        = NVL (:C_EMPLID, A.EMPLID) " & _
                    "AND A.EMPL_CLASS    = NVL (:C_EMPLCLASS, A.EMPL_CLASS) " & _
                    "AND A.COMPANY       = :C_COMP " & _
                    "AND A.MANAGER_LEVEL = NVL(:MANAGERLVL,A.MANAGER_LEVEL) " & _
                    "AND A.EMPLFLAG      = NVL(:EMPLFLAG,A.EMPLFLAG) " & _
                    "AND A.PERIODMASTID  = :PERIODMASTID {0}"

        cmd.Parameters.Clear()
        cmd.CommandText = String.Format(sqlText, authenstr)
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_EMPLCLASS", .Value = _cri.EmplClass})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_COMP", .Value = _cri.Company})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "MANAGERLVL", .Value = If(String.IsNullOrEmpty(_cri.Managerlvl), "", _cri.Managerlvl)})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_STDATE", .Value = _cri.StartDate})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_EDDATE", .Value = _cri.EndDate})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_EMPLID", .Value = If(String.IsNullOrEmpty(_cri.Emplid), "", _cri.Emplid)})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "EMPLFLAG", .Value = _cri.EmplFlag})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "PERIODMASTID", .Value = _cri.PeriodMastId.Periodmastid})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "DATEOFINTERFACE", .Value = Now})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "USERINTERFACE", .Value = param.UserName})
        cmd.Transaction = Trn

        Try
            cmd.ExecuteNonQuery()
        Catch ex As Exception
            Throw ex
        End Try
    End Sub

#End Region

#Region "FindDataForInterface"

    Private Sub ValidateRecord(ByRef _dto As Model.ModelInterfacetempEe)
        With _dto
            .Remarks = ""

            If .Rateid Is Nothing OrElse .Rateid = String.Empty Then
                .Remarks &= "Salary cannot be null."
            End If

            'Marked by Chanchira L. on 19/04/2021 มีเช็คด้านล่าง
            'If .Payrollid Is Nothing OrElse .Payrollid = String.Empty Then
            '    .Remarks &= IIf(.Remarks = "", "", " , ") & "Payroll Id. cannot be null."
            'End If

            If .Payorgcode Is Nothing OrElse .Payorgcode = String.Empty Then
                .Remarks &= IIf(.Remarks = "", "", " , ") & "Payorgcode cannot be null."
            End If

            If Not .Periodmastid.HasValue Then
                .Remarks &= IIf(.Remarks = "", "", " , ") & "Periodmaster Id. cannot be null."
            End If

            If .EmpMarrystatus Is Nothing OrElse .EmpMarrystatus = String.Empty Then
                .Remarks &= IIf(.Remarks = "", "", " , ") & "MarryStatus cannot be null."
            End If

            If .Bankcode Is Nothing OrElse .Bankcode = String.Empty Then
                .Remarks &= IIf(.Remarks = "", "", " , ") & "Bankcode cannot be null."
            End If

            If Not .Accountid.HasValue Then
                .Remarks &= IIf(.Remarks = "", "", " , ") & "Accountid cannot be null."
            End If

            If .Bankaccount Is Nothing OrElse .Bankaccount = String.Empty Then
                .Remarks &= IIf(.Remarks = "", "", " , ") & "Bankaccount cannot be null."
            End If

            'Add by Chanchira L. on 09/03/2020 
            If .Payrollid Is Nothing OrElse .Payrollid = String.Empty Then
                .Remarks &= IIf(.Remarks = "", "", " , ") & "National ID. cannot be null."
            End If

            If .SatWork Is Nothing OrElse .SatWork = String.Empty Then
                .Remarks &= IIf(.Remarks = "", "", " , ") & "Job Data --> Saturday Work cannot be null."
            End If

            If Not FindCompBank(_dto) Then
                .Remarks &= IIf(.Remarks = "", "", " , ") & "No data found in Table PYCOMBANK. (Company = " & .Company & ", Emplid = " & .Emplid & ")"
            End If

        End With

    End Sub

#End Region

#Region "Function"
    'Add by Chanchira L. on 09/03/2020  FindCompBank
    Private Function FindCompBank(ByRef _dto As Model.ModelInterfacetempEe) As Boolean
        Dim ret As Integer
        Dim con As System.Data.Common.DbConnection = New OracleConnection(Util.GetOraConnectionString)
        Dim cmd As OracleCommand = con.CreateCommand
        cmd.CommandType = CommandType.Text
        cmd.BindByName = True
        cmd.CommandText = "select count(*) from pycombank a where a.company = :company and a.bankid = (select b.bank_cd from pers_bank b where b.emplid = :emplid and account_id = '1')"

        cmd.Parameters.Clear()
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "company", .Value = _dto.Company})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "emplid", .Value = _dto.Emplid})
        Try
            If cmd.Connection.State = ConnectionState.Closed Then
                cmd.Connection.Open()
            End If
            ret = cmd.ExecuteScalar()

        Catch ex As Exception
            Throw (ex)
        Finally
            con.Close()
        End Try

        Return IIf(ret = 0, False, True)
    End Function

    Private Sub FindCompanyDtl(ByVal param As SsCommon.ServiceParam, ByVal _criteria As Model.ModelCriteriaList, ByRef dtoTempComp As Model.ModelCompanyDtl)
        Dim con As System.Data.Common.DbConnection = New OracleConnection(Util.GetOraConnectionString)
        Dim cmd As OracleCommand = con.CreateCommand
        cmd.CommandType = CommandType.Text
        cmd.BindByName = True
        'cmd.CommandText = "select a.* from COMPANY_DTL a where a.COMPANY = :COMPANY and a.MONTH = :MONTH and a.EFFDT = (SELECT MAX(b.EFFDT) FROM COMPANY_DTL b WHERE b.COMPANY = a.COMPANY and b.MONTH = a.MONTH and  b.EFFDT BETWEEN :C_STDATE AND :C_EDDATE) "
        cmd.CommandText = "select a.* from company_dtl a where a.company = :company and a.month = :month and a.effdt = (select max(b.effdt) from company_dtl b where b.company = a.company and b.month = a.month and b.effdt <= sysdate)"

        cmd.Parameters.Clear()
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "company", .Value = _criteria.Company})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "month", .Value = _criteria.Month})
        'cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_STDATE", .Value = _criteria.StartDate})
        'cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_EDDATE", .Value = _criteria.EndDate})
        Try
            If cmd.Connection.State = ConnectionState.Closed Then
                cmd.Connection.Open()
            End If

            Dim rdr As System.Data.Common.DbDataReader = cmd.ExecuteReader()

            If rdr.Read Then
                dtoTempComp.RetrieveFromDataReader(rdr)
            End If

        Catch ex As Exception
            Throw (ex)
        Finally
            con.Close()
        End Try

    End Sub

    <OperationContract()> _
    Public Function GetGdDetails(ByVal gdCode As String) As System.Collections.Generic.List(Of SsHrCommon.DTO.Pygeneraldt)
        Dim ret As New List(Of SsHrCommon.DTO.Pygeneraldt)
        Dim con As System.Data.Common.DbConnection = New OracleConnection(Util.GetOraConnectionString)
        Dim cmd As OracleCommand = con.CreateCommand
        cmd.CommandType = CommandType.Text
        cmd.BindByName = True
        cmd.CommandText = "SELECT * FROM PYGENERALDT Where GDCODE = :GDCODE Order By DTCODE"

        cmd.Parameters.Clear()
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "GDCODE", .Value = gdCode})

        Try
            If cmd.Connection.State = ConnectionState.Closed Then
                cmd.Connection.Open()
            End If

            Dim rdr As System.Data.Common.DbDataReader = cmd.ExecuteReader()

            While (rdr.Read)
                Dim res = New SsHrCommon.DTO.Pygeneraldt
                res.RetrieveFromDataReader(rdr)
                ret.Add(res)
            End While

        Catch ex As Exception
            Throw ex
        Finally
            con.Close()
        End Try
        Return ret
    End Function

    Private Function GetCurrentTreeId(Effdt As Date) As Integer
        Dim ret As Integer
        Dim con As System.Data.Common.DbConnection = New OracleConnection(Util.GetOraConnectionString)
        Dim cmd As OracleCommand = con.CreateCommand
        cmd.CommandType = CommandType.Text
        cmd.BindByName = True
        cmd.CommandText = "SELECT MAX(TREEID) TREEID FROM DEPTTREE_TBL WHERE EFFECTIVEDATE <= :Effdt "

        cmd.Parameters.Clear()
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "Effdt", .Value = Effdt})
        Try
            If cmd.Connection.State = ConnectionState.Closed Then
                cmd.Connection.Open()
            End If
            ret = cmd.ExecuteScalar()

        Catch ex As Exception
            Throw (ex)
        Finally
            con.Close()
        End Try

        Return ret
    End Function

    <OperationContract()> _
    Public Function FindPayrollPeriod(Company As String, PerIodMastId As Integer, Periodtime As Integer, PYear As String, PMonth As String) As SsHrCommon.DTO.Pyperiod
        Dim ret As New SsHrCommon.DTO.Pyperiod
        Dim con As System.Data.Common.DbConnection = New OracleConnection(Util.GetOraConnectionString)
        Dim cmd As OracleCommand = con.CreateCommand
        cmd.CommandType = CommandType.Text
        cmd.BindByName = True
        cmd.CommandText = "SELECT * FROM Pyperiod Where COMPANY = :COMP AND PERIODMASTID = :PERIODMASTID AND PERIODYEAR = :PYEAR AND PERIODMONTH = :PMONTH AND PERIODTIME = :PERIODTIME"

        cmd.Parameters.Clear()
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "COMP", .Value = Company})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "PERIODMASTID", .Value = PerIodMastId})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "PYEAR", .Value = PYear})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "PMONTH", .Value = PMonth})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "PERIODTIME", .Value = Periodtime})

        Try
            If cmd.Connection.State = ConnectionState.Closed Then
                cmd.Connection.Open()
            End If

            Dim rdr As System.Data.Common.DbDataReader = cmd.ExecuteReader()

            If rdr.Read Then
                ret.RetrieveFromDataReader(rdr)
            Else
                ret = Nothing
            End If

        Catch ex As Exception
            Throw ex
        Finally
            con.Close()
        End Try
        Return ret
    End Function

    <OperationContract()> _
    Public Function FindPayrollPeriodMast(_param As SsCommon.ServiceParam) As List(Of Model.ModelPyperiodmaster)
        Dim ret As New List(Of Model.ModelPyperiodmaster)
        Dim con As System.Data.Common.DbConnection = New OracleConnection(Util.GetOraConnectionString)
        Dim cmd As OracleCommand = con.CreateCommand
        cmd.CommandType = CommandType.Text
        cmd.BindByName = True

        cmd.CommandText = "SELECT * FROM Pyperiodmaster order by PERIODMASTID"

        Try
            If cmd.Connection.State = ConnectionState.Closed Then
                cmd.Connection.Open()
            End If

            'Periodmaste
            Dim rdr As System.Data.Common.DbDataReader = cmd.ExecuteReader()
            While (rdr.Read)
                Dim res = New Model.ModelPyperiodmaster
                res.RetrieveFromDataReader(rdr)
                If _param.Lang <> _param.LocalLang Then
                    res.Description = res.Engdescription
                Else
                    res.Description = res.Localdescription
                End If

                ret.Add(res)
            End While


        Catch ex As Exception
            Throw ex
        Finally
            con.Close()
        End Try
        Return ret
    End Function

    <OperationContract()> _
    Public Function FindPyorganize(_param As SsCommon.ServiceParam, ByVal IsSmartPay As String, ByVal company As String) As List(Of SsHrCommon.DTO.Pyorganize)
        Dim ret As New List(Of SsHrCommon.DTO.Pyorganize)
        Dim con As System.Data.Common.DbConnection = New OracleConnection(Util.GetOraConnectionString)
        Dim cmd As OracleCommand = con.CreateCommand
        cmd.CommandType = CommandType.Text
        cmd.BindByName = True

        If IsSmartPay Is Nothing OrElse String.IsNullOrEmpty(IsSmartPay) Then
            cmd.CommandText = "SELECT * FROM PYORGANIZE where LEVELID = '2' and RELATEID Like :COM || '%'  order by RELATEID"
        Else
            If IsSmartPay = "Y" Then
                cmd.CommandText = "SELECT * FROM PYORGANIZE where LEVELID = '2' and RELATEID Like :COM || '%'  order by RELATEID"
            ElseIf IsSmartPay = "N" Then
                cmd.CommandText = "SELECT * FROM PYORGANIZE where LEVELID = '2' and COMPANY Like :COM || '%'AND TREENO = (SELECT MAX(TO_NUMBER(P.TREENO)) FROM PYORGANIZE P) order by RELATEID"
            End If
        End If

        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "COM", .Value = company})

        Try
            If cmd.Connection.State = ConnectionState.Closed Then
                cmd.Connection.Open()
            End If

            Dim rdr As System.Data.Common.DbDataReader = cmd.ExecuteReader()
            While (rdr.Read)
                Dim res = New SsHrCommon.DTO.Pyorganize
                res.RetrieveFromDataReader(rdr)

                ret.Add(res)
            End While


        Catch ex As Exception
            Throw ex
        Finally
            con.Close()
        End Try
        Return ret
    End Function

    <OperationContract()> _
    Public Function FindDefaultPeriodAndOrg(Company As String, PerIodMastId As Integer, ByVal IsSmartPay As String) As Model.Model
        Dim ret As New Model.Model
        Dim con As System.Data.Common.DbConnection = New OracleConnection(Util.GetOraConnectionString)

        Dim cmd As OracleCommand = con.CreateCommand
        cmd.CommandType = CommandType.Text
        cmd.BindByName = True

        Dim cmd2 As OracleCommand = con.CreateCommand
        cmd2.CommandType = CommandType.Text
        cmd2.BindByName = True

        'PYPERIOD
        cmd.CommandText = " SELECT * FROM PYPERIOD WHERE PERIODID = (SELECT MIN (PERIODID) FROM PYPERIOD WHERE COMPANY = :COMP AND PERIODMASTID = :PERIODMASTID AND PERIODSTATUS = 'N') "
        cmd.Parameters.Clear()
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "COMP", .Value = Company})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "PERIODMASTID", .Value = PerIodMastId})

        'PYORGANIZE
        If IsSmartPay Is Nothing OrElse String.IsNullOrEmpty(IsSmartPay) Then
            cmd2.CommandText = "SELECT * FROM PYORGANIZE where LEVELID = '2' and COMPANYID Like :COM || '%'  order by RELATEID"
        Else
            If IsSmartPay = "Y" Then
                cmd2.CommandText = "SELECT * FROM PYORGANIZE where LEVELID = '2' and COMPANYID Like :COM || '%'  order by RELATEID"
            ElseIf IsSmartPay = "N" Then
                cmd2.CommandText = "SELECT * FROM PYORGANIZE where LEVELID = '2' and COMPANY Like :COM || '%'  AND TREENO = (SELECT P.TREENO FROM PYORGTREE P WHERE P.TREENO  = (SELECT MAX(TO_NUMBER(O.TREENO)) FROM PYORGTREE O WHERE O.EFFDT <= SYSDATE)) order by RELATEID"
            End If
        End If
        cmd2.Parameters.Clear()
        cmd2.Parameters.Add(New OracleParameter With {.ParameterName = "COM", .Value = Company})

        Try
            If con.State = ConnectionState.Closed Then
                con.Open()
            End If

            'PYPERIOD
            Dim rdrP As System.Data.Common.DbDataReader = cmd.ExecuteReader()
            If rdrP.Read Then
                Dim m As New SsHrCommon.DTO.Pyperiod
                m.RetrieveFromDataReader(rdrP)
                ret.Pyperiod = m
            End If

            'PYORGANIZE
            Dim rdrO As System.Data.Common.DbDataReader = cmd2.ExecuteReader()
            ret.OrgLst = New List(Of SsHrCommon.DTO.Pyorganize)
            While (rdrO.Read)
                Dim res = New SsHrCommon.DTO.Pyorganize
                res.RetrieveFromDataReader(rdrO)
                ret.OrgLst.Add(res)
            End While

        Catch ex As Exception
            Throw ex
        Finally
            con.Close()
        End Try
        Return ret
    End Function

    Private Function FindHrPayrollEmpl(ByVal cmd As System.Data.Common.DbCommand, ByVal _cri As Model.ModelCriteriaList, authenstr As String, _d As Date, ByVal Criteria_Emplid As String) As List(Of Model.ModelInterfaceLog)
        Dim ret As New List(Of Model.ModelInterfaceLog)
        Dim sqlText As String = String.Empty

        sqlText = "SELECT :PERIODID PERIODID, USERINTERFACE,COMPANY  COMPANYCODE,DATEOFINTERFACE,NVL(:EMPLCLASS,'') EMPLCLASS,EMPLFLAG,:INTERFACEYEAR,:INTERFACEMONTH,:PERIODTIME,:PROGRAMCODE,CNTEMPL, " & _
                  "            NVL(:MANAGERLVL,'') MANAGER_LEVEL ,:PERIODMASTID, EMPLID " & _
                  "FROM( " & _
                  "   SELECT USERINTERFACE,COMPANY,DATEOFINTERFACE,EMPLFLAG,PROGRAMCODE,PERIODMASTID,EMPLID,COUNT (EMPLID)  CNTEMPL " & _
                  "   FROM INTERFACE_EE A " & _
                  "   WHERE A.EMPL_CLASS = NVL (:EMPLCLASS, A.EMPL_CLASS) AND A.COMPANY = :C_COMP AND A.PAYORGCODE like NVL(:PAYORGCODE || '%',A.PAYORGCODE) AND A.MANAGER_LEVEL = NVL(:MANAGERLVL,A.MANAGER_LEVEL)   " & _
                  "   AND A.EMPLFLAG = NVL(:EMPLFLAG,A.EMPLFLAG) AND A.EMPLFLAG <> 'D'  " & _
                  "   AND trunc(A.DATEOFINTERFACE) = trunc(:DATEOFINTERFACE) AND A.PERIODMASTID = :PERIODMASTID  {0} "
        If _cri.chkAll Then
            sqlText += " AND A.EMPLID = NVL('" & Criteria_Emplid & "',A.EMPLID) "
        Else
            sqlText += " AND A.EMPLID IN (" & Criteria_Emplid & ")"
        End If

        cmd.Parameters.Clear()
        If _cri.EndDate IsNot Nothing Then
            sqlText = String.Format(sqlText, "AND A.EFFDT <= :DateTo {0}")
            cmd.Parameters.Add(New OracleParameter With {.ParameterName = "DateTo", .Value = _cri.EndDate})
        End If

        sqlText += "  GROUP BY USERINTERFACE,COMPANY,DATEOFINTERFACE,EMPLFLAG,PROGRAMCODE,PERIODMASTID,EMPLID) "
        '-----------------------------------

        'cmd.Parameters.Clear()
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "EMPLFLAG", .Value = _cri.EmplFlag})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "MANAGERLVL", .Value = If(String.IsNullOrEmpty(_cri.Managerlvl), String.Empty, _cri.Managerlvl)})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "EMPLCLASS", .Value = If(String.IsNullOrEmpty(_cri.EmplClass), String.Empty, _cri.EmplClass)})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_COMP", .Value = _cri.Company})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "PAYORGCODE", .Value = If(_cri.Org Is Nothing, "", _cri.Org.Relateid)})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "PERIODID", .Value = _cri.Periodid})
        'cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_STDATE", .Value = _cri.StartDate})
        'cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_EDDATE", .Value = _cri.EndDate})
        'Marked by Chanchira L. on 14/10/2020
        'cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_EMPLID", .Value = If(String.IsNullOrEmpty(_cri.Emplid), String.Empty, _cri.Emplid)})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "INTERFACEYEAR", .Value = _cri.Year})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "INTERFACEMONTH", .Value = _cri.Month})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "PERIODTIME", .Value = _cri.PeriodTime})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "PROGRAMCODE", .Value = "SHRM01702"})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "PERIODMASTID", .Value = _cri.PeriodMastId.Periodmastid})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "DATEOFINTERFACE", .Value = _d})
        cmd.CommandText = String.Format(sqlText, authenstr)


        Dim rdr As System.Data.Common.DbDataReader = cmd.ExecuteReader()

        While rdr.Read
            Dim m As New Model.ModelInterfaceLog
            m.RetrieveFromDataReader(rdr)
            ret.Add(m)
        End While

        If ret.Count = 0 Then
            ret = Nothing
        End If

        Return ret
    End Function

    Private Function NewFindHrPayrollEmpl(ByVal cmd As System.Data.Common.DbCommand, ByVal _cri As Model.ModelCriteriaList, authenstr As String, _d As Date, ByVal Criteria_Emplid As String) As String
        Dim ret As New List(Of Model.ModelInterfaceLog)
        Dim sqlText As String = String.Empty

        sqlText = "SELECT :PERIODID PERIODID, USERINTERFACE,COMPANY  COMPANYCODE,DATEOFINTERFACE,NVL(:EMPLCLASS,'') EMPLCLASS,EMPLFLAG,:INTERFACEYEAR,:INTERFACEMONTH,:PERIODTIME,:PROGRAMCODE,CNTEMPL, " & _
                  "            NVL(:MANAGERLVL,'') MANAGER_LEVEL ,:PERIODMASTID, EMPLID " & _
                  "FROM( " & _
                  "   SELECT USERINTERFACE,COMPANY,DATEOFINTERFACE,EMPLFLAG,PROGRAMCODE,PERIODMASTID,EMPLID,COUNT (EMPLID)  CNTEMPL " & _
                  "   FROM INTERFACE_EE A " & _
                  "   WHERE A.EMPL_CLASS = NVL (:EMPLCLASS, A.EMPL_CLASS) AND A.COMPANY = :C_COMP AND A.PAYORGCODE like NVL(:PAYORGCODE || '%',A.PAYORGCODE) AND A.MANAGER_LEVEL = NVL(:MANAGERLVL,A.MANAGER_LEVEL)   " & _
                  "   AND A.EMPLFLAG = NVL(:EMPLFLAG,A.EMPLFLAG) AND A.EMPLFLAG <> 'D'  " & _
                  "   AND trunc(A.DATEOFINTERFACE) = trunc(:DATEOFINTERFACE) AND A.PERIODMASTID = :PERIODMASTID  {0} "
        If _cri.chkAll Then
            sqlText += " AND A.EMPLID = NVL('" & Criteria_Emplid & "',A.EMPLID) "
        Else
            sqlText += " AND A.EMPLID IN (" & Criteria_Emplid & ")"
        End If

        cmd.Parameters.Clear()
        If _cri.EndDate IsNot Nothing Then
            sqlText = String.Format(sqlText, "AND A.EFFDT <= :DateTo {0}")
            cmd.Parameters.Add(New OracleParameter With {.ParameterName = "DateTo", .Value = _cri.EndDate})
        End If

        sqlText += "  GROUP BY USERINTERFACE,COMPANY,DATEOFINTERFACE,EMPLFLAG,PROGRAMCODE,PERIODMASTID,EMPLID) "
        '-----------------------------------

        'cmd.Parameters.Clear()
        'cmd.Parameters.Add(New OracleParameter With {.ParameterName = "EMPLFLAG", .Value = _cri.EmplFlag})
        'cmd.Parameters.Add(New OracleParameter With {.ParameterName = "MANAGERLVL", .Value = If(String.IsNullOrEmpty(_cri.Managerlvl), String.Empty, _cri.Managerlvl)})
        'cmd.Parameters.Add(New OracleParameter With {.ParameterName = "EMPLCLASS", .Value = If(String.IsNullOrEmpty(_cri.EmplClass), String.Empty, _cri.EmplClass)})
        'cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_COMP", .Value = _cri.Company})
        'cmd.Parameters.Add(New OracleParameter With {.ParameterName = "PAYORGCODE", .Value = If(_cri.Org Is Nothing, "", _cri.Org.Relateid)})
        'cmd.Parameters.Add(New OracleParameter With {.ParameterName = "PERIODID", .Value = _cri.Periodid})
        'cmd.Parameters.Add(New OracleParameter With {.ParameterName = "INTERFACEYEAR", .Value = _cri.Year})
        'cmd.Parameters.Add(New OracleParameter With {.ParameterName = "INTERFACEMONTH", .Value = _cri.Month})
        'cmd.Parameters.Add(New OracleParameter With {.ParameterName = "PERIODTIME", .Value = _cri.PeriodTime})
        'cmd.Parameters.Add(New OracleParameter With {.ParameterName = "PROGRAMCODE", .Value = "SHRM01702"})
        'cmd.Parameters.Add(New OracleParameter With {.ParameterName = "PERIODMASTID", .Value = _cri.PeriodMastId.Periodmastid})
        'cmd.Parameters.Add(New OracleParameter With {.ParameterName = "DATEOFINTERFACE", .Value = _d})
        cmd.CommandText = String.Format(sqlText, authenstr)

        'Dim rdr As System.Data.Common.DbDataReader = cmd.ExecuteReader()

        'While rdr.Read
        '    Dim m As New Model.ModelInterfaceLog
        '    m.RetrieveFromDataReader(rdr)
        '    ret.Add(m)
        'End While

        'If ret.Count = 0 Then
        '    ret = Nothing
        'End If

        Return cmd.CommandText
    End Function

    Private Sub InsertHrPayrollLog(ByVal cmd As System.Data.Common.DbCommand, ByVal Trn As System.Data.Common.DbTransaction, ByVal _cri As Model.ModelCriteriaList, ByVal _param As SsCommon.ServiceParam, _d As Date, strSql As String)

        '------------------------------------------------------
        'Changed by Chanchira L. on 19/04/2021
        'cmd.CommandText = "INSERT INTO INTERFACE_LOG (PERIODID,USERINTERFACE,COMPANYCODE,DATEOFINTERFACE,EMPLCLASS,EMPLFLAG,INTERFACEYEAR,INTERFACEMONTH,PERIODTIME,PROGRAMCODE,CNTEMPL,MANAGER_LEVEL,PERIODMASTID,EMPLID) " & _
        '                  "VALUES(:PERIODID,:USERINTERFACE,:COMPANYCODE,:DATEOFINTERFACE,:EMPLCLASS,:EMPLFLAG,:INTERFACEYEAR,:INTERFACEMONTH,:PERIODTIME,:PROGRAMCODE,:CNTEMPL,:MANAGER_LEVEL,:PERIODMASTID,:EMPLID)"

        'Dim sqlText As String = String.Empty
        'If _strlst IsNot Nothing Then
        '    For Each _dto In _strlst

        '        cmd.Parameters.Clear()
        '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "PERIODID", .Value = _dto.Periodid})
        '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "USERINTERFACE", .Value = _dto.Userinterface})
        '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "COMPANYCODE", .Value = _dto.Companycode})
        '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "DATEOFINTERFACE", .Value = _dto.Dateofinterface})
        '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "EMPLCLASS", .Value = _cri.EmplClass})
        '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "EMPLFLAG", .Value = If(String.IsNullOrEmpty(_dto.Emplflag), "", _dto.Emplflag)})
        '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "INTERFACEYEAR", .Value = _cri.Year})
        '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "INTERFACEMONTH", .Value = _cri.Month})
        '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "PERIODTIME", .Value = _cri.PeriodTime})
        '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "PROGRAMCODE", .Value = "SHRM01702"})
        '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "CNTEMPL", .Value = _dto.Cntempl})
        '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "MANAGER_LEVEL", .Value = If(String.IsNullOrEmpty(_cri.Managerlvl), "", _cri.Managerlvl)})
        '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "PERIODMASTID", .Value = _cri.PeriodMastId.Periodmastid})
        '        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "EMPLID", .Value = _dto.Emplid})
        '        cmd.Transaction = Trn
        '        cmd.ExecuteNonQuery()
        '    Next

        'End If
        '------------------------------------------------------
        Dim strInsert = "INSERT INTO INTERFACE_LOG (PERIODID,USERINTERFACE,COMPANYCODE,DATEOFINTERFACE,EMPLCLASS,EMPLFLAG,INTERFACEYEAR,INTERFACEMONTH,PERIODTIME,PROGRAMCODE,CNTEMPL,MANAGER_LEVEL,PERIODMASTID,EMPLID) " & strSql

        cmd.CommandText = strInsert

        cmd.Parameters.Clear()
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "EMPLFLAG", .Value = _cri.EmplFlag})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "MANAGERLVL", .Value = If(String.IsNullOrEmpty(_cri.Managerlvl), String.Empty, _cri.Managerlvl)})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "EMPLCLASS", .Value = If(String.IsNullOrEmpty(_cri.EmplClass), String.Empty, _cri.EmplClass)})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_COMP", .Value = _cri.Company})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "PAYORGCODE", .Value = If(_cri.Org Is Nothing, "", _cri.Org.Relateid)})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "PERIODID", .Value = _cri.Periodid})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "INTERFACEYEAR", .Value = _cri.Year})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "INTERFACEMONTH", .Value = _cri.Month})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "PERIODTIME", .Value = _cri.PeriodTime})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "PROGRAMCODE", .Value = "SHRM01702"})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "PERIODMASTID", .Value = _cri.PeriodMastId.Periodmastid})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "DATEOFINTERFACE", .Value = _d})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "DateTo", .Value = _cri.EndDate})

        cmd.Transaction = Trn
        cmd.ExecuteNonQuery()

    End Sub

    Private Sub DeleteB4Insert(ByVal trn As System.Data.Common.DbTransaction, ByVal cmd As System.Data.Common.DbCommand, ByVal _criteria As Model.ModelCriteriaList, authenstr As String)
        Dim sqlText As String = String.Empty
        sqlText = "Delete From INTERFACE_EE A " & _
                  "WHERE   A.EMPL_CLASS = NVL (:C_EMPLCLASS, A.EMPL_CLASS) AND A.COMPANY = :C_COMP AND A.PAYORGCODE like NVL(:PAYORGCODE || '%',A.PAYORGCODE) AND A.MANAGER_LEVEL = NVL(:MANAGERLVL,A.MANAGER_LEVEL) AND A.PERIODMASTID = :PERIODMASTID " & _
                  "AND A.EMPLID = NVL(:C_EMPLID,A.EMPLID) AND A.EFFDT BETWEEN :C_STDATE AND :C_EDDATE AND A.EMPLFLAG = NVL(:EMPLFLAG,A.EMPLFLAG)  AND   A.EMPLFLAG  <> 'D' {0}"

        cmd.Parameters.Clear()
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "EMPLFLAG", .Value = _criteria.EmplFlag})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "MANAGERLVL", .Value = If(String.IsNullOrEmpty(_criteria.Managerlvl), String.Empty, _criteria.Managerlvl)})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_EMPLCLASS", .Value = If(String.IsNullOrEmpty(_criteria.EmplClass), String.Empty, _criteria.EmplClass)})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_COMP", .Value = _criteria.Company})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "PAYORGCODE", .Value = If(_criteria.Org Is Nothing, "", _criteria.Org.Relateid)})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_STDATE", .Value = _criteria.StartDate})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_EDDATE", .Value = _criteria.EndDate})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_EMPLID", .Value = If(String.IsNullOrEmpty(_criteria.Emplid), String.Empty, _criteria.Emplid)})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "PERIODMASTID", .Value = _criteria.PeriodMastId.Periodmastid})
        cmd.CommandText = String.Format(sqlText, authenstr)
        cmd.Transaction = trn
        cmd.ExecuteNonQuery()

        sqlText = "Delete From INTERFACE_COMPEN C " & _
                 "WHERE EXISTS (SELECT 1 FROM INTERFACE_COMPEN B ,JOB_EE A  WHERE B.EMPLID = A.EMPLID AND B.EMPL_RCD = A.EMPL_RCD AND B.EFFDT = A.EFFDT AND B.EFFSEQ = A.EFFSEQ AND A.PERIODMASTID = :PERIODMASTID " & _
                                "AND C.EMPLID = B.EMPLID AND C.EMPL_RCD = B.EMPL_RCD AND C.EFFDT = B.EFFDT AND C.EFFSEQ = B.EFFSEQ AND C.COMP_RATECD = B.COMP_RATECD " & _
                                "AND A.EMPL_CLASS = NVL (:C_EMPLCLASS, A.EMPL_CLASS) AND A.COMPANY = :C_COMP AND A.PAYORGCODE like NVL(:PAYORGCODE || '%',A.PAYORGCODE) AND A.MANAGER_LEVEL = NVL(:MANAGERLVL,A.MANAGER_LEVEL) AND A.EMPLID = NVL(:C_EMPLID,A.EMPLID) {0}) " & _
                                "AND EFFDT BETWEEN :C_STDATE AND :C_EDDATE  AND EMPLFLAG = NVL(:EMPLFLAG,EMPLFLAG) AND EMPLFLAG <> 'D' "
        cmd.Parameters.Clear()
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "EMPLFLAG", .Value = _criteria.EmplFlag})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "MANAGERLVL", .Value = If(String.IsNullOrEmpty(_criteria.Managerlvl), String.Empty, _criteria.Managerlvl)})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_EMPLCLASS", .Value = If(String.IsNullOrEmpty(_criteria.EmplClass), String.Empty, _criteria.EmplClass)})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_COMP", .Value = _criteria.Company})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "PAYORGCODE", .Value = If(_criteria.Org Is Nothing, "", _criteria.Org.Relateid)})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_STDATE", .Value = _criteria.StartDate})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_EDDATE", .Value = _criteria.EndDate})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_EMPLID", .Value = If(String.IsNullOrEmpty(_criteria.Emplid), String.Empty, _criteria.Emplid)})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "PERIODMASTID", .Value = _criteria.PeriodMastId.Periodmastid})
        cmd.CommandText = String.Format(sqlText, authenstr)
        cmd.Transaction = trn
        cmd.ExecuteNonQuery()




        'If Insert record that deleted

        sqlText = "Delete From INTERFACE_EE A " & _
                  "WHERE EXISTS (SELECT 1 FROM INTERFACETEMP_EE X WHERE X.EFFDT BETWEEN :C_STDATE AND :C_EDDATE AND X.PERIODMASTID = :PERIODMASTID " & _
                        "AND A.EMPLID = X.EMPLID AND A.EMPL_RCD = X.EMPL_RCD AND A.EFFDT = X.EFFDT AND A.EFFSEQ = X.EFFSEQ ) " & _
                        "AND A.EMPL_CLASS = NVL (:C_EMPLCLASS, A.EMPL_CLASS) AND A.COMPANY = :C_COMP AND A.PAYORGCODE like NVL(:PAYORGCODE || '%',A.PAYORGCODE) AND A.EMPLID = NVL (:C_EMPLID, A.EMPLID) AND A.MANAGER_LEVEL = NVL(:MANAGERLVL,A.MANAGER_LEVEL)  " & _
                        "AND A.EFFDT BETWEEN :C_STDATE AND :C_EDDATE AND A.EMPLFLAG = NVL(:EMPLFLAG,A.EMPLFLAG) {0} AND A.EMPLFLAG = 'D'"

        cmd.CommandText = String.Format(sqlText, authenstr)
        cmd.Parameters.Clear()
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_EMPLCLASS", .Value = _criteria.EmplClass})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_COMP", .Value = _criteria.Company})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "PAYORGCODE", .Value = If(_criteria.Org Is Nothing, "", _criteria.Org.Relateid)})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "MANAGERLVL", .Value = If(String.IsNullOrEmpty(_criteria.Managerlvl), "", _criteria.Managerlvl)})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_STDATE", .Value = _criteria.StartDate})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_EDDATE", .Value = _criteria.EndDate})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_EMPLID", .Value = If(String.IsNullOrEmpty(_criteria.Emplid), "", _criteria.Emplid)})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "EMPLFLAG", .Value = _criteria.EmplFlag})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "PERIODMASTID", .Value = _criteria.PeriodMastId.Periodmastid})
        cmd.Transaction = trn
        cmd.ExecuteNonQuery()

        sqlText = "Delete From INTERFACE_COMPEN B " & _
                 " WHERE  EXISTS (SELECT 1 FROM INTERFACETEMP_COMPEN A WHERE A.EFFDT BETWEEN :C_STDATE AND :C_EDDATE AND A.PERIODMASTID = :PERIODMASTID " & _
                       "AND B.EMPLID = A.EMPLID AND B.EMPL_RCD = A.EMPL_RCD AND B.EFFDT = A.EFFDT AND B.EFFSEQ = A.EFFSEQ AND B.COMP_RATECD = A.COMP_RATECD " & _
                       "AND  A.EMPL_CLASS = NVL (:C_EMPLCLASS, A.EMPL_CLASS) AND A.COMPANY = :C_COMP AND A.EMPLID = NVL (:C_EMPLID, A.EMPLID) AND A.MANAGER_LEVEL = NVL(:MANAGERLVL,A.MANAGER_LEVEL)  " & _
                       "AND A.EFFDT BETWEEN :C_STDATE AND :C_EDDATE AND A.EMPLFLAG = NVL(:EMPLFLAG,A.EMPLFLAG) {0}) AND EMPLFLAG = 'D'"


        cmd.CommandText = String.Format(sqlText, authenstr)
        cmd.Parameters.Clear()
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_EMPLCLASS", .Value = _criteria.EmplClass})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_COMP", .Value = _criteria.Company})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "MANAGERLVL", .Value = If(String.IsNullOrEmpty(_criteria.Managerlvl), "", _criteria.Managerlvl)})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_STDATE", .Value = _criteria.StartDate})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_EDDATE", .Value = _criteria.EndDate})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_EMPLID", .Value = If(String.IsNullOrEmpty(_criteria.Emplid), "", _criteria.Emplid)})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "EMPLFLAG", .Value = _criteria.EmplFlag})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "PERIODMASTID", .Value = _criteria.PeriodMastId.Periodmastid})
        cmd.Transaction = trn
        cmd.ExecuteNonQuery()



    End Sub

    <OperationContract()> _
    Public Function GenerateText(ByVal param As SsCommon.ServiceParam, ByVal _criteria As Model.ModelCriteriaList, ByVal formatdt As String) As SsCommon.ServiceResult(Of Dictionary(Of String, Byte()), String)
        Dim dpBp As New SsHrCommonService
        Dim col As String = "A.Grade"
        Dim ret As Byte() = Nothing
        Dim Ret1 As New Dictionary(Of String, Byte())
        Dim Ret2 As New SsCommon.ServiceResult(Of Dictionary(Of String, Byte()), String)
        'Dim dt As New List(Of Model.ModelInterfaceEE)
        Dim dt As New DataTable
        Dim con As System.Data.Common.DbConnection = New OracleConnection(Util.GetOraConnectionString)
        Dim cmd As OracleCommand = con.CreateCommand
        cmd.BindByName = True
        Dim sqlText As String = String.Empty

        sqlText = "SELECT * FROM(SELECT  A.EMPL_CLASS AS T_TYP, A.EMPLFLAG AS T_FUNC, A.COMPANY AS T_COM, A.PAYROLLID AS T_CODE,A.EMPL_TITLE AS T_TITLE,    " & _
                    "        A.EMPL_NAME AS T_NAME,   " & _
                    "        TO_CHAR(A.ADDRESS1) AS T_ADD1,   " & _
                    "        TO_CHAR(A.ADDRESS2) AS T_ADD2,   " & _
                    "          (select split_text(A.PAYORGCODE,2,'.') from dual) AS T_OPE,   " & _
                    "          (select split_text(A.PAYORGCODE,3,'.') from dual) AS T_BRH,    " & _
                    "          (select split_text(A.PAYORGCODE,4,'.') from dual) AS T_ORG,    " & _
                    "          A.EMPL_SEX AS T_SEX, A.EMP_MARRYSTATUS AS MAR, A.EMPLID AS T_IDNO,    " & _
                    "          A.STATUS AS T_STA,   " & _
                    "        A.BIRTHDATE AS T_BDATE, A.HIRE_DT AS T_EDATE, A.PROBATION_DT AS T_FDATE, A.EXPECTED_END_DATE AS T_RDATE,A.GRADE AS T_PC, A.BANKCODE AS T_BNO,   " & _
                    "        A.ACCOUNTID AS T_BTY, A.BANKACCOUNT AS T_BAC, A.CALTAXMETHOD AS T_TAXCALMETHOD,A.BANK_BRANCH AS T_BKBRNAME,b.COMP_RATECD,b.COMPENSATION_RATE, " & _
                    "        A.HARMFUL,A.WORKHOUR,A.NID " & _
                    " FROM INTERFACE_EE A LEFT OUTER JOIN INTERFACE_COMPEN B ON A.EMPLID = B.EMPLID AND A.EMPL_RCD = B.EMPL_RCD AND A.EFFDT = b.EFFDT AND A.EFFSEQ = B.EFFSEQ " & _
                    "WHERE A.EMPL_CLASS = NVL (:C_EMPLCLASS, A.EMPL_CLASS) AND A.COMPANY = :C_COMP AND A.PAYORGCODE like NVL(:PAYORGCODE || '%',A.PAYORGCODE) AND A.EMPLID = NVL (:C_EMPLID, A.EMPLID) AND A.MANAGER_LEVEL = NVL(:MANAGERLVL,A.MANAGER_LEVEL)  " & _
                    "  AND A.PERIODMASTID = :PERIODMASTID AND A.EFFDT BETWEEN :C_STDATE AND :C_EDDATE AND A.EMPLFLAG = (CASE WHEN :EMPLFLAG = 'A' THEN A.EMPLFLAG ELSE NVL(:EMPLFLAG,A.EMPLFLAG)END) {0})  " & _
                    "PIVOT  (SUM(COMPENSATION_RATE) FOR (COMP_RATECD) IN ( 'AGSALY' AS T_SALY,'AGSPPS' AS T_PALW,'AGUPCY' AS T_SALW1,'AGMEAL' AS T_MALW1,'AGHARM' AS T_FALW1, " & _
                    "                                                                  'AGTELE' AS T_TELW1,'NONE' AS T_OTHER, 'NONE' AS T_SPEC1, 'AGDMEL' AS T_MDED1,'NONE' AS T_ODED1, " & _
                    "                                                                  'AGDLOA' AS T_SLDED,'AGDBGH' AS T_LLDED,'AGHSHP' AS T_HARW1,'AGRSDT' AS T_HOUW1,'AGSPIC' AS T_SPAW1, " & _
                    "                                                                  'AGOTHC' AS T_OINC1, 'AGVHLE' AS T_VEHW1)) "

        cmd.Parameters.Clear()

        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_STDATE", .Value = _criteria.StartDate})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_EDDATE", .Value = _criteria.EndDate})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_EMPLCLASS", .Value = If(String.IsNullOrEmpty(_criteria.EmplClass), "", _criteria.EmplClass)})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_COMP", .Value = _criteria.Company})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "PAYORGCODE", .Value = If(_criteria.Org Is Nothing, "", _criteria.Org.Relateid)})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_EMPLID", .Value = If(String.IsNullOrEmpty(_criteria.Emplid), "", _criteria.Emplid)})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "EMPLFLAG", .Value = _criteria.EmplFlag})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "MANAGERLVL", .Value = If(String.IsNullOrEmpty(_criteria.Managerlvl), "", _criteria.Managerlvl)})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "PERIODMASTID", .Value = _criteria.PeriodMastId.Periodmastid})
        'cmd.Parameters.Add(New OracleParameter With {.ParameterName = "LANG", .Value = param.Lang})
        'cmd.Parameters.Add(New OracleParameter With {.ParameterName = "LOCAL", .Value = param.LocalLang})

        Dim authenstr = String.Format("AND A.DEPTID IN ({0}) ", dpBp.StringAuthenDepartment(param.UserName, "A.DEPTID", col, Today))

        cmd.CommandText = String.Format(sqlText, authenstr)

        'Changed by Chanchira L. on 13/03/2020
        'เปลี่ยน update a.isinterface = 'Y'
        'Update  A.ISINTERFACE = 'N' In table INTERFACETEMP_EE
        Dim cmdEE As OracleCommand = con.CreateCommand
        'sqlText = "UPDATE INTERFACETEMP_EE A SET A.ISINTERFACE = 'N' " & _
        '          "WHERE A.ISINTERFACE = 'Y' AND A.EMPLFLAG = NVL (:EMPLFLAG, A.EMPLFLAG) AND A.MANAGER_LEVEL = NVL (:MANAGERLVL, A.MANAGER_LEVEL) AND A.EMPL_CLASS = NVL (:EMPLCLASS, A.EMPL_CLASS)   " & _
        '          "AND A.COMPANY = :COMP AND A.PAYORGCODE like NVL(:PAYORGCODE || '%',A.PAYORGCODE) AND A.EFFDT BETWEEN :C_STDATE AND :C_EDDATE AND A.EMPLID = NVL (:EMPLID, A.EMPLID) AND A.PERIODMASTID = :PERIODMASTID  {0} "
        sqlText = "UPDATE INTERFACETEMP_EE A SET A.ISINTERFACE = 'Y' " & _
                  "WHERE A.ISINTERFACE = 'N' AND A.EMPLFLAG = NVL (:EMPLFLAG, A.EMPLFLAG) AND A.MANAGER_LEVEL = NVL (:MANAGERLVL, A.MANAGER_LEVEL) AND A.EMPL_CLASS = NVL (:EMPLCLASS, A.EMPL_CLASS)   " & _
                  "AND A.COMPANY = :COMP AND A.PAYORGCODE like NVL(:PAYORGCODE || '%',A.PAYORGCODE) AND A.EFFDT BETWEEN :C_STDATE AND :C_EDDATE AND A.EMPLID = NVL (:EMPLID, A.EMPLID) AND A.PERIODMASTID = :PERIODMASTID  {0} "
        cmdEE.Parameters.Clear()
        cmdEE.Parameters.Add(New OracleParameter With {.ParameterName = "EMPLFLAG", .Value = If(_criteria.EmplFlag = "A", "", _criteria.EmplFlag)})
        cmdEE.Parameters.Add(New OracleParameter With {.ParameterName = "MANAGERLVL", .Value = If(String.IsNullOrEmpty(_criteria.Managerlvl), String.Empty, _criteria.Managerlvl)})
        cmdEE.Parameters.Add(New OracleParameter With {.ParameterName = "EMPLCLASS", .Value = If(String.IsNullOrEmpty(_criteria.EmplClass), String.Empty, _criteria.EmplClass)})
        cmdEE.Parameters.Add(New OracleParameter With {.ParameterName = "COMP", .Value = _criteria.Company})
        cmdEE.Parameters.Add(New OracleParameter With {.ParameterName = "PAYORGCODE", .Value = If(_criteria.Org Is Nothing, "", _criteria.Org.Relateid)})
        cmdEE.Parameters.Add(New OracleParameter With {.ParameterName = "C_STDATE", .Value = _criteria.StartDate})
        cmdEE.Parameters.Add(New OracleParameter With {.ParameterName = "C_EDDATE", .Value = _criteria.EndDate})
        cmdEE.Parameters.Add(New OracleParameter With {.ParameterName = "EMPLID", .Value = If(String.IsNullOrEmpty(_criteria.Emplid), String.Empty, _criteria.Emplid)})
        cmdEE.Parameters.Add(New OracleParameter With {.ParameterName = "PERIODMASTID", .Value = _criteria.PeriodMastId.Periodmastid})
        cmdEE.CommandText = String.Format(sqlText, authenstr)


        'Changed by Chanchira L. on 13/03/2020
        'เปลี่ยน update a.isinterface = 'Y'
        'Update  A.ISINTERFACE = 'N' In table INTERFACE_COMPEN 
        Dim cmdCompen As OracleCommand = con.CreateCommand
        'sqlText = "UPDATE INTERFACETEMP_COMPEN A  SET A.ISINTERFACE = 'N'" & _
        '                  "WHERE  A.ISINTERFACE = 'Y' AND A.EMPLFLAG = NVL (:EMPLFLAG, A.EMPLFLAG) AND A.MANAGER_LEVEL = NVL (:MANAGERLVL, A.MANAGER_LEVEL) AND A.EMPL_CLASS = NVL (:EMPLCLASS, A.EMPL_CLASS)   " & _
        '                  "AND A.COMPANY = :COMP AND A.EFFDT BETWEEN :C_STDATE AND :C_EDDATE AND A.EMPLID = NVL (:EMPLID, A.EMPLID) AND A.PERIODMASTID = :PERIODMASTID {0} "
        sqlText = "UPDATE INTERFACETEMP_COMPEN A  SET A.ISINTERFACE = 'Y'" & _
                          "WHERE  A.ISINTERFACE = 'N' AND A.EMPLFLAG = NVL (:EMPLFLAG, A.EMPLFLAG) AND A.MANAGER_LEVEL = NVL (:MANAGERLVL, A.MANAGER_LEVEL) AND A.EMPL_CLASS = NVL (:EMPLCLASS, A.EMPL_CLASS)   " & _
                          "AND A.COMPANY = :COMP AND A.EFFDT BETWEEN :C_STDATE AND :C_EDDATE AND A.EMPLID = NVL (:EMPLID, A.EMPLID) AND A.PERIODMASTID = :PERIODMASTID {0} "
        cmdCompen.Parameters.Clear()
        cmdCompen.Parameters.Add(New OracleParameter With {.ParameterName = "EMPLFLAG", .Value = If(_criteria.EmplFlag = "A", "", _criteria.EmplFlag)})
        cmdCompen.Parameters.Add(New OracleParameter With {.ParameterName = "MANAGERLVL", .Value = If(String.IsNullOrEmpty(_criteria.Managerlvl), String.Empty, _criteria.Managerlvl)})
        cmdCompen.Parameters.Add(New OracleParameter With {.ParameterName = "EMPLCLASS", .Value = If(String.IsNullOrEmpty(_criteria.EmplClass), String.Empty, _criteria.EmplClass)})
        cmdCompen.Parameters.Add(New OracleParameter With {.ParameterName = "COMP", .Value = _criteria.Company})
        cmdCompen.Parameters.Add(New OracleParameter With {.ParameterName = "C_STDATE", .Value = _criteria.StartDate})
        cmdCompen.Parameters.Add(New OracleParameter With {.ParameterName = "C_EDDATE", .Value = _criteria.EndDate})
        cmdCompen.Parameters.Add(New OracleParameter With {.ParameterName = "EMPLID", .Value = If(String.IsNullOrEmpty(_criteria.Emplid), String.Empty, _criteria.Emplid)})
        cmdCompen.Parameters.Add(New OracleParameter With {.ParameterName = "PERIODMASTID", .Value = _criteria.PeriodMastId.Periodmastid})
        cmdCompen.CommandText = String.Format(sqlText, authenstr)



        Try

            If con.State = ConnectionState.Closed Then
                con.Open()
            End If

            Dim trn As System.Data.Common.DbTransaction = con.BeginTransaction()
            Try
                'Dim rdr As System.Data.Common.DbDataReader = cmd.ExecuteReader()

                'Do While (rdr.Read)
                '    Dim m As New Model.ModelInterfaceEE
                '     m.RetrieveFromDataReader(rdr)
                '    dt.Add(m)
                'Loop
                If TypeOf (dt) Is DataTable Then
                    Dim da As New OracleDataAdapter(cmd)
                    ' Dim aa As New DataTable
                    'da.Fill(aa)
                    da.Fill(dt)

                End If

                'Update  A.ISINTERFACE = 'N' In table INTERFACETEMP_EE
                cmdEE.Transaction = trn
                cmdEE.ExecuteNonQuery()

                'Update  A.ISINTERFACE = 'N' In table INTERFACE_COMPEN 
                cmdCompen.Transaction = trn
                cmdCompen.ExecuteNonQuery()

                trn.Commit()
            Catch ex As Exception
                trn.Rollback()
                Throw ex
            End Try
        Catch ex As Exception
            Throw ex
        Finally
            con.Close()
        End Try

        Dim nameFile As String = String.Empty

        '***สร้างไฟล์****'
        Dim curPath As String = AppDomain.CurrentDomain.BaseDirectory
        If curPath(curPath.Length - 1) <> "\" Then
            curPath = curPath & "\"
        End If
        curPath = curPath & "Temp\"
        Dim fileName As String = Me.GetType.Name.Replace("Service", "") & "_" & Guid.NewGuid.ToString '& ".prn"


        If dt IsNot Nothing AndAlso dt.Rows.Count > 0 Then
            ' nameFile = "TR" & Period.Month & Period.Year & _cri.Company & ".prn"
            'nameFile = "TR" & Today.Day & Today.Month & Today.Year & _criteria.Company & _criteria.Org.Relateid & ".prn"

            If _criteria.Org IsNot Nothing AndAlso (_criteria.Org.Relateid IsNot Nothing OrElse Not String.IsNullOrEmpty(_criteria.Org.Relateid)) Then
                Dim op = _criteria.Org.Relateid.Split(".")
                nameFile = "MS" & Today.ToString("dd") & Today.ToString("MM") & Today.ToString("yyyy") & _criteria.Company & op(1) & ".prn"
            Else
                nameFile = "MS" & Today.ToString("dd") & Today.ToString("MM") & Today.ToString("yyyy") & _criteria.Company & ".prn"
            End If

            Dim dtData As New System.Text.StringBuilder

            For Each r In dt.Rows
                Dim st As New System.Text.StringBuilder
                For Each c In dt.Columns
                    If IsDBNull(r(c.columnName).ToString.Trim) OrElse String.IsNullOrEmpty(r(c.columnName).ToString.Trim) Then
                        st.Append(";")
                    Else
                        If TypeOf (r(c.columnName)) Is Date Then
                            st.Append(String.Format(r(c.columnName).ToString.Trim, formatdt).Trim & ";")
                        ElseIf TypeOf (r(c.columnName)) Is Integer OrElse TypeOf (r(c.columnName)) Is Long OrElse TypeOf (r(c.columnName)) Is Double Then
                            st.Append(r(c.columnName).ToString("#0.00") & ";")
                        Else
                            st.Append(r(c).ToString & ";")
                        End If
                    End If
                Next
                Right(st.ToString, 1)
                dtData.AppendLine(st.ToString)
            Next

            Dim oFile As System.IO.FileInfo
            oFile = New System.IO.FileInfo(curPath & fileName.ToString)

            ' For Each u In dtDataLst
            Using writer As New System.IO.StreamWriter(curPath & fileName, True, System.Text.Encoding.UTF8)
                writer.WriteLine(dtData.ToString)
            End Using

            Using oFileStream As System.IO.FileStream = oFile.OpenRead()
                Dim lBytes As Long = oFileStream.Length
                If (lBytes > 0) Then
                    Dim fileData(lBytes - 1) As Byte
                    ' Read the file into a byte array
                    oFileStream.Read(fileData, 0, lBytes)
                    oFileStream.Close()
                    ret = fileData
                End If
            End Using


            System.IO.File.Delete(curPath & fileName.ToString)


            Ret1.Add(nameFile, ret)
            Ret2.Result = Ret1

        Else
            Return Nothing
        End If


        Return Ret2

    End Function

    '<OperationContract()> _
    'Public Function GenerateTextInToServer(ByVal param As SsCommon.ServiceParam, ByVal _criteria As Model.ModelCriteriaList, ByVal formatdt As String, ByVal pathServer As String) As String
    '    Dim ret As String = "Export text file Completed"
    '    Dim dpBp As New SsHrCommonService
    '    Dim col As String = "A.Grade"
    '    Dim dt As New DataTable
    '    Dim con As System.Data.Common.DbConnection = New OracleConnection(Util.GetOraConnectionString)
    '    Dim cmd As OracleCommand = con.CreateCommand
    '    cmd.BindByName = True
    '    Dim sqlText As String = String.Empty

    '    'sqlText = "SELECT * FROM(SELECT  A.EMPL_CLASS AS T_TYP, A.EMPLFLAG AS T_FUNC, A.COMPANY AS T_COM, A.PAYROLLID AS T_CODE,A.EMPL_TITLE AS T_TITLE,    " & _
    '    '            "        A.EMPL_NAME AS T_NAME,   " & _
    '    '            "        TO_CHAR(A.ADDRESS1) AS T_ADD1,   " & _
    '    '            "        TO_CHAR(A.ADDRESS2) AS T_ADD2,   " & _
    '    '            "          (select split_text(A.PAYORGCODE,2,'.') from dual) AS T_OPE,   " & _
    '    '            "          (select split_text(A.PAYORGCODE,3,'.') from dual) AS T_BRH,    " & _
    '    '            "          (select split_text(A.PAYORGCODE,4,'.') from dual) AS T_ORG,    " & _
    '    '            "          A.EMPL_SEX AS T_SEX, A.EMP_MARRYSTATUS AS MAR, A.EMPLID AS T_IDNO,    " & _
    '    '            "          A.STATUS AS T_STA,   " & _
    '    '            "        A.BIRTHDATE AS T_BDATE, A.HIRE_DT AS T_EDATE, A.PROBATION_DT AS T_FDATE, A.EXPECTED_END_DATE AS T_RDATE,A.GRADE AS T_PC, A.BANKCODE AS T_BNO,   " & _
    '    '            "        A.ACCOUNTID AS T_BTY, A.BANKACCOUNT AS T_BAC, A.CALTAXMETHOD AS T_TAXCALMETHOD,A.BANK_BRANCH AS T_BKBRNAME,b.COMP_RATECD,b.COMPENSATION_RATE, " & _
    '    '            "        A.HARMFUL,A.WORKHOUR,A.NID " & _
    '    '            " FROM INTERFACE_EE A LEFT OUTER JOIN INTERFACE_COMPEN B ON A.EMPLID = B.EMPLID AND A.EMPL_RCD = B.EMPL_RCD AND A.EFFDT = b.EFFDT AND A.EFFSEQ = B.EFFSEQ " & _
    '    '            "WHERE A.EMPL_CLASS = NVL (:C_EMPLCLASS, A.EMPL_CLASS) AND A.COMPANY = :C_COMP AND A.PAYORGCODE like NVL(:PAYORGCODE || '%',A.PAYORGCODE) AND A.EMPLID = NVL (:C_EMPLID, A.EMPLID) AND A.MANAGER_LEVEL = NVL(:MANAGERLVL,A.MANAGER_LEVEL)  " & _
    '    '            "  AND A.PERIODMASTID = :PERIODMASTID AND A.EFFDT BETWEEN :C_STDATE AND :C_EDDATE AND A.EMPLFLAG = (CASE WHEN :EMPLFLAG = 'A' THEN A.EMPLFLAG ELSE NVL(:EMPLFLAG,A.EMPLFLAG)END) {0})  " & _
    '    '            "PIVOT  (SUM(COMPENSATION_RATE) FOR (COMP_RATECD) IN ( 'AGSALY' AS T_SALY,'AGSPPS' AS T_PALW,'AGUPCY' AS T_SALW1,'AGMEAL' AS T_MALW1,'AGHARM' AS T_FALW1, " & _
    '    '            "                                                                  'AGTELE' AS T_TELW1,'NONE' AS T_OTHER, 'NONE' AS T_SPEC1, 'AGDMEL' AS T_MDED1,'NONE' AS T_ODED1, " & _
    '    '            "                                                                  'AGDLOA' AS T_SLDED,'AGDBGH' AS T_LLDED,'AGHSHP' AS T_HARW1,'AGRSDT' AS T_HOUW1,'AGSPIC' AS T_SPAW1, " & _
    '    '            "                                                                  'AGOTHC' AS T_OINC1, 'AGVHLE' AS T_VEHW1)) "
    '    sqlText = "SELECT T_TYP, T_FUNC, T_COM, T_CODE, T_TITLE, T_NAME, T_ADD1, T_ADD2, T_OPE, T_BRH, T_ORG, '' AS T_SHF, T_TAXID, T_SEX, MAR,'' AS T_CHILDTOTAL, " & _
    '                " '' AS T_CHILDSCHOLL, '' AS T_DEDINSURANCE, '' AS T_DEDHOMEINTEREST, '' AS T_DEDPROVIDENCE, '' AS	T_DEDDONATION, '' AS T_DEDDONATION2, " & _
    '                " T_IDNO, T_SCNO, T_STA, T_BDATE, T_EDATE, T_FDATE, '' AS T_TDATE, T_RDATE, '' AS T_POS, T_PC, T_BNO, T_BTY, T_BAC,T_TAXCALMETHOD, " & _
    '                " '' AS T_NOCALSOCIAL, '' AS T_DEDLTF, '' AS T_DEDRMF, ''	AS T_DEDFATHER, ''	AS T_FATHERID, ''	AS T_MOTHERID, '' AS T_TITLE_COUPLE, '' AS T_NAME_COUPLE, " & _
    '                " '' AS T_SURN_COUPLE, ''	AS T_BDATE_COUPLE, ''	AS T_ID_COUPLE, '' AS T_ID_COUPLE_FAT, '' AS T_ID_COUPLE_MOT, T_SALY, T_BONUSRATE, " & _
    '                " '' AS T_POROVIDEN_DDT, '' AS	T_DED_EMP_PER, '' AS T_DED_COM_PER, T_PALW, T_SALW1, T_MALW1, T_FALW1, T_TELW1, T_OTHER, T_SPEC1,'' AS T_HELP1, T_MDED1, " & _
    '                " T_ODED1,'' AS T_TRCOM, '' AS T_TRCODE, '' AS T_TROPE, '' AS	T_TRBRH, '' AS	T_TRORG, '' AS T_TRSHF, '' AS	T_TRDATE, T_SLDED,'' AS T_SLBAL, '' AS T_GSB, " & _
    '                " T_LLDED, '' AS T_LLBAL, T_HARW1, T_HOUW1, T_SPAW1, T_OINC1, T_VEHW1, '' AS T_PEMWF,	'' AS T_PCPWF, '' AS T_HEALTHY, '' AS T_PEMPV,	'' AS T_PCPPV, " & _
    '                " T_BKBRNAME, T_BKBRNAME,	'' AS T_YINC,	'' AS T_YAINC1,	'' AS T_YAINC2,	'' AS T_YAINC3,	'' AS T_YTAX,	'' AS T_YATAX1,	'' AS T_YATAX2,	'' AS T_YATAX3, " & _
    '                " '' AS T_YPALW, '' AS T_YSALW,	'' AS T_YMALW,	'' AS T_YFALW,	'' AS T_YHARW,	'' AS T_YHOUW,	'' AS T_YTELW,	'' AS T_YSPAW,	'' AS T_YOINC, " & _
    '                " '' AS T_YMDED, '' AS T_YODED,	'' AS T_YOT, '' AS T_YRW,	'' AS T_YLATE, '' AS T_YSLDED,	'' AS T_YLLDED,	'' AS T_YLVDED,	'' AS T_YADJ,	'' AS T_YEMWF, " & _
    '                " '' AS T_YCPWF, '' AS T_YEMPV, '' AS T_YCPPV, '' AS T_YHEALTHY, HARMFUL, WORKHOUR, NID, WORKDAY_MONTH " & _
    '                "    FROM(SELECT  A.EMPL_CLASS AS T_TYP, A.EMPLFLAG AS T_FUNC, A.COMPANY AS T_COM, A.PAYROLLID AS T_CODE,A.EMPL_TITLE AS T_TITLE,    " & _
    '                "        A.EMPL_NAME AS T_NAME, A.BONUS AS T_BONUSRATE,  " & _
    '                "        TO_CHAR(A.ADDRESS1) AS T_ADD1,   " & _
    '                "        TO_CHAR(A.ADDRESS2) AS T_ADD2,   " & _
    '                "          (select split_text(A.PAYORGCODE,2,'.') from dual) AS T_OPE,   " & _
    '                "          (select split_text(A.PAYORGCODE,3,'.') from dual) AS T_BRH,    " & _
    '                "          (select split_text(A.PAYORGCODE,4,'.') from dual) AS T_ORG,    " & _
    '                "          A.EMPL_SEX AS T_SEX, A.EMP_MARRYSTATUS AS MAR, " & _
    '                "          A.EMPLID AS T_IDNO, " & _
    '                "          A.STATUS AS T_STA,   " & _
    '                "        TO_CHAR(A.BIRTHDATE,'YYYYMMDD') AS T_BDATE, TO_CHAR(A.HIRE_DT,'YYYYMMDD') AS T_EDATE, TO_CHAR(A.PROBATION_DT,'YYYYMMDD') AS T_FDATE, TO_CHAR(A.TERMINATION_DT,'YYYYMMDD') AS T_RDATE, A.GRADE AS T_PC, A.BANKCODE AS T_BNO,   " & _
    '                "        A.ACCOUNTID AS T_BTY, A.BANKACCOUNT AS T_BAC, A.CALTAXMETHOD AS T_TAXCALMETHOD,A.BANK_BRANCH AS T_BKBRNAME,b.COMP_RATECD,b.COMPENSATION_RATE, " & _
    '                "        A.HARMFUL,A.WORKHOUR,A.NID,A.SCNO AS T_SCNO,A.TAXID AS T_TAXID, A.WORKDAY_MONTH " & _
    '                " FROM INTERFACE_EE A LEFT OUTER JOIN INTERFACE_COMPEN B ON A.EMPLID = B.EMPLID AND A.EMPL_RCD = B.EMPL_RCD AND A.EFFDT = b.EFFDT AND A.EFFSEQ = B.EFFSEQ " & _
    '                "WHERE A.EMPL_CLASS = NVL (:C_EMPLCLASS, A.EMPL_CLASS) AND A.COMPANY = :C_COMP AND A.PAYORGCODE like NVL(:PAYORGCODE || '%',A.PAYORGCODE) AND A.EMPLID = NVL (:C_EMPLID, A.EMPLID) AND A.MANAGER_LEVEL = NVL(:MANAGERLVL,A.MANAGER_LEVEL)  " & _
    '                "  AND A.PERIODMASTID = :PERIODMASTID AND A.EFFDT BETWEEN :C_STDATE AND :C_EDDATE AND A.EMPLFLAG = (CASE WHEN :EMPLFLAG = 'A' THEN A.EMPLFLAG ELSE NVL(:EMPLFLAG,A.EMPLFLAG)END) {0})  " & _
    '                "PIVOT  (SUM(COMPENSATION_RATE) FOR (COMP_RATECD) IN ( 'AGSALY' AS T_SALY,'AGSPPS' AS T_PALW,'AGUPCY' AS T_SALW1,'AGMEAL' AS T_MALW1,'AGHARM' AS T_FALW1, " & _
    '                "                                                                  'AGTELE' AS T_TELW1,'NONE' AS T_OTHER, 'NONE' AS T_SPEC1, 'AGDMEL' AS T_MDED1,'NONE' AS T_ODED1, " & _
    '                "                                                                  'AGDLOA' AS T_SLDED,'AGDBGH' AS T_LLDED,'AGHSHP' AS T_HARW1,'AGRSDT' AS T_HOUW1,'AGSPIC' AS T_SPAW1, " & _
    '                "                                                                  'AGOTHC' AS T_OINC1, 'AGVHLE' AS T_VEHW1)) "

    '    cmd.Parameters.Clear()

    '    cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_STDATE", .Value = _criteria.StartDate})
    '    cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_EDDATE", .Value = _criteria.EndDate})
    '    cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_EMPLCLASS", .Value = If(String.IsNullOrEmpty(_criteria.EmplClass), "", _criteria.EmplClass)})
    '    cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_COMP", .Value = _criteria.Company})
    '    cmd.Parameters.Add(New OracleParameter With {.ParameterName = "PAYORGCODE", .Value = If(_criteria.Org Is Nothing, "", _criteria.Org.Relateid)})
    '    cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_EMPLID", .Value = If(String.IsNullOrEmpty(_criteria.Emplid), "", _criteria.Emplid)})
    '    cmd.Parameters.Add(New OracleParameter With {.ParameterName = "EMPLFLAG", .Value = _criteria.EmplFlag})
    '    cmd.Parameters.Add(New OracleParameter With {.ParameterName = "MANAGERLVL", .Value = If(String.IsNullOrEmpty(_criteria.Managerlvl), "", _criteria.Managerlvl)})
    '    cmd.Parameters.Add(New OracleParameter With {.ParameterName = "PERIODMASTID", .Value = _criteria.PeriodMastId.Periodmastid})
    '    'cmd.Parameters.Add(New OracleParameter With {.ParameterName = "LANG", .Value = param.Lang})
    '    'cmd.Parameters.Add(New OracleParameter With {.ParameterName = "LOCAL", .Value = param.LocalLang})

    '    Dim authenstr = String.Format("AND A.DEPTID IN ({0}) ", dpBp.StringAuthenDepartment(param.UserName, "A.DEPTID", col, Today))

    '    cmd.CommandText = String.Format(sqlText, authenstr)


    '    'Update  A.ISINTERFACE = 'N' In table INTERFACETEMP_EE
    '    Dim cmdEE As OracleCommand = con.CreateCommand
    '    sqlText = "UPDATE INTERFACETEMP_EE A SET A.ISINTERFACE = 'N' " & _
    '              "WHERE A.ISINTERFACE = 'Y' AND A.EMPLFLAG = NVL (:EMPLFLAG, A.EMPLFLAG) AND A.MANAGER_LEVEL = NVL (:MANAGERLVL, A.MANAGER_LEVEL) AND A.EMPL_CLASS = NVL (:EMPLCLASS, A.EMPL_CLASS)   " & _
    '              "AND A.COMPANY = :COMP AND A.PAYORGCODE like NVL(:PAYORGCODE || '%',A.PAYORGCODE) AND A.EFFDT BETWEEN :C_STDATE AND :C_EDDATE AND A.EMPLID = NVL (:EMPLID, A.EMPLID) AND A.PERIODMASTID = :PERIODMASTID  {0} "
    '    cmdEE.Parameters.Clear()
    '    cmdEE.Parameters.Add(New OracleParameter With {.ParameterName = "EMPLFLAG", .Value = If(_criteria.EmplFlag = "A", "", _criteria.EmplFlag)})
    '    cmdEE.Parameters.Add(New OracleParameter With {.ParameterName = "MANAGERLVL", .Value = If(String.IsNullOrEmpty(_criteria.Managerlvl), String.Empty, _criteria.Managerlvl)})
    '    cmdEE.Parameters.Add(New OracleParameter With {.ParameterName = "EMPLCLASS", .Value = If(String.IsNullOrEmpty(_criteria.EmplClass), String.Empty, _criteria.EmplClass)})
    '    cmdEE.Parameters.Add(New OracleParameter With {.ParameterName = "COMP", .Value = _criteria.Company})
    '    cmdEE.Parameters.Add(New OracleParameter With {.ParameterName = "PAYORGCODE", .Value = If(_criteria.Org Is Nothing, "", _criteria.Org.Relateid)})
    '    cmdEE.Parameters.Add(New OracleParameter With {.ParameterName = "C_STDATE", .Value = _criteria.StartDate})
    '    cmdEE.Parameters.Add(New OracleParameter With {.ParameterName = "C_EDDATE", .Value = _criteria.EndDate})
    '    cmdEE.Parameters.Add(New OracleParameter With {.ParameterName = "EMPLID", .Value = If(String.IsNullOrEmpty(_criteria.Emplid), String.Empty, _criteria.Emplid)})
    '    cmdEE.Parameters.Add(New OracleParameter With {.ParameterName = "PERIODMASTID", .Value = _criteria.PeriodMastId.Periodmastid})
    '    cmdEE.CommandText = String.Format(sqlText, authenstr)


    '    'Update  A.ISINTERFACE = 'N' In table INTERFACE_COMPEN 
    '    Dim cmdCompen As OracleCommand = con.CreateCommand
    '    sqlText = "UPDATE INTERFACETEMP_COMPEN A  SET A.ISINTERFACE = 'N'" & _
    '                      "WHERE  A.ISINTERFACE = 'Y' AND A.EMPLFLAG = NVL (:EMPLFLAG, A.EMPLFLAG) AND A.MANAGER_LEVEL = NVL (:MANAGERLVL, A.MANAGER_LEVEL) AND A.EMPL_CLASS = NVL (:EMPLCLASS, A.EMPL_CLASS)   " & _
    '                      "AND A.COMPANY = :COMP AND A.EFFDT BETWEEN :C_STDATE AND :C_EDDATE AND A.EMPLID = NVL (:EMPLID, A.EMPLID) AND A.PERIODMASTID = :PERIODMASTID {0} "
    '    cmdCompen.Parameters.Clear()
    '    cmdCompen.Parameters.Add(New OracleParameter With {.ParameterName = "EMPLFLAG", .Value = If(_criteria.EmplFlag = "A", "", _criteria.EmplFlag)})
    '    cmdCompen.Parameters.Add(New OracleParameter With {.ParameterName = "MANAGERLVL", .Value = If(String.IsNullOrEmpty(_criteria.Managerlvl), String.Empty, _criteria.Managerlvl)})
    '    cmdCompen.Parameters.Add(New OracleParameter With {.ParameterName = "EMPLCLASS", .Value = If(String.IsNullOrEmpty(_criteria.EmplClass), String.Empty, _criteria.EmplClass)})
    '    cmdCompen.Parameters.Add(New OracleParameter With {.ParameterName = "COMP", .Value = _criteria.Company})
    '    cmdCompen.Parameters.Add(New OracleParameter With {.ParameterName = "C_STDATE", .Value = _criteria.StartDate})
    '    cmdCompen.Parameters.Add(New OracleParameter With {.ParameterName = "C_EDDATE", .Value = _criteria.EndDate})
    '    cmdCompen.Parameters.Add(New OracleParameter With {.ParameterName = "EMPLID", .Value = If(String.IsNullOrEmpty(_criteria.Emplid), String.Empty, _criteria.Emplid)})
    '    cmdCompen.Parameters.Add(New OracleParameter With {.ParameterName = "PERIODMASTID", .Value = _criteria.PeriodMastId.Periodmastid})
    '    cmdCompen.CommandText = String.Format(sqlText, authenstr)



    '    Try

    '        If con.State = ConnectionState.Closed Then
    '            con.Open()
    '        End If

    '        Dim trn As System.Data.Common.DbTransaction = con.BeginTransaction()
    '        Try
    '            'Dim rdr As System.Data.Common.DbDataReader = cmd.ExecuteReader()

    '            'Do While (rdr.Read)
    '            '    Dim m As New Model.ModelInterfaceEE
    '            '     m.RetrieveFromDataReader(rdr)
    '            '    dt.Add(m)
    '            'Loop
    '            If TypeOf (dt) Is DataTable Then
    '                Dim da As New OracleDataAdapter(cmd)
    '                ' Dim aa As New DataTable
    '                'da.Fill(aa)
    '                da.Fill(dt)

    '            End If

    '            'Update  A.ISINTERFACE = 'N' In table INTERFACETEMP_EE
    '            cmdEE.Transaction = trn
    '            cmdEE.ExecuteNonQuery()

    '            'Update  A.ISINTERFACE = 'N' In table INTERFACE_COMPEN 
    '            cmdCompen.Transaction = trn
    '            cmdCompen.ExecuteNonQuery()

    '            trn.Commit()
    '        Catch ex As Exception
    '            trn.Rollback()
    '            Throw ex
    '        End Try
    '    Catch ex As Exception
    '        Throw ex
    '    Finally
    '        con.Close()
    '    End Try

    '    Dim nameFile As String = String.Empty

    '    '1/6/2016 Kitinan J. อ่าน Path จาก Confix FwInit
    '    Dim curPath As String = pathServer
    '    If curPath(curPath.Length - 1) <> "\" Then
    '        curPath = curPath & "\"
    '    End If

    '    ' Dim fileName As String = Me.GetType.Name.Replace("Service", "") & "_" & Guid.NewGuid.ToString '& ".prn"
    '    Dim fileName As String = ""

    '    If dt IsNot Nothing AndAlso dt.Rows.Count > 0 Then
    '        ' nameFile = "MS" & Period.Month & Period.Year & _cri.Company & ".prn"
    '        'fileName = "MS" & Today.Day & Today.Month & Today.Year & _criteria.Company & _criteria.Org.Relateid & ".prn"
    '        If _criteria.Org IsNot Nothing AndAlso (_criteria.Org.Relateid IsNot Nothing OrElse Not String.IsNullOrEmpty(_criteria.Org.Relateid)) Then
    '            Dim op = _criteria.Org.Relateid.Split(".")
    '            'fileName = "MS" & Today.ToString("dd") & Today.ToString("MM") & Today.ToString("yyyy") & _criteria.Company & op(1) & ".prn"
    '            'Payroll Vietnam 2019
    '            fileName = "MS" & _criteria.Month & Today.ToString("yyyy") & _criteria.Company & op(1) & ".prn"
    '        Else
    '            'fileName = "MS" & Today.ToString("dd") & Today.ToString("MM") & Today.ToString("yyyy") & _criteria.Company & ".prn"
    '            'Payroll Vietnam 2019
    '            fileName = "MS" & _criteria.Month & Today.ToString("yyyy") & _criteria.Company & ".prn"
    '        End If
    '        Dim dtData As New System.Text.StringBuilder

    '        Dim i = 1
    '        For Each r In dt.Rows
    '            Dim st As New System.Text.StringBuilder
    '            For Each c In dt.Columns
    '                If IsDBNull(r(c.columnName).ToString.Trim) OrElse String.IsNullOrEmpty(r(c.columnName).ToString.Trim) Then
    '                    st.Append(";")
    '                Else
    '                    If TypeOf (r(c.columnName)) Is Date Then
    '                        st.Append(String.Format(r(c.columnName).ToString.Trim, formatdt).Trim & ";")
    '                    ElseIf TypeOf (r(c.columnName)) Is Integer OrElse TypeOf (r(c.columnName)) Is Long OrElse TypeOf (r(c.columnName)) Is Double Then
    '                        st.Append(r(c.columnName).ToString("#0.00") & ";")
    '                    Else
    '                        st.Append(r(c).ToString & ";")
    '                    End If
    '                End If
    '            Next
    '            Right(st.ToString, 1)

    '            If i = dt.Rows.Count Then
    '                dtData.Append(st.ToString)
    '            Else
    '                dtData.AppendLine(st.ToString)
    '            End If

    '            i += 1
    '        Next

    '        Dim oFile As System.IO.FileInfo
    '        oFile = New System.IO.FileInfo(curPath & fileName.ToString)

    '        ' For Each u In dtDataLst
    '        If System.IO.File.Exists(curPath & fileName.ToString) = False Then
    '            Try
    '                Using writer As New System.IO.StreamWriter(curPath & fileName, True, System.Text.Encoding.UTF8)
    '                    writer.WriteLine(dtData.ToString)
    '                End Using
    '            Catch ex As Exception
    '                ret = "Export text file Uncompleted"
    '            End Try
    '        Else
    '            ret = "Payroll have not received text file yet"
    '        End If

    '        ' System.IO.File.Delete(curPath & fileName.ToString)

    '    Else
    '        ret = "No data found"
    '    End If


    '    Return ret

    'End Function

    Public Function NewDate(ByVal newDt As Date, ByVal param As SsCommon.ServiceParam) As String
        ' Dim newDat As String = ""
        Dim Year As String = ""
        If param.Lang = "th-TH" Then
            Year = newDt.Year + 543
        Else
            Year = newDt.Year
        End If


        NewDate = Year.Substring(Year.Length - 2) & If(newDt.Month.ToString.Length < 2, "0" & newDt.Month, newDt.Month) & If(newDt.Day.ToString.Length < 2, "0" & newDt.Day, newDt.Day)

        Return NewDate
    End Function

#End Region


    'Add PrintExcel,GetExcelByte,DataTableToExcel by Chanchira L. on 12/06/2017 
    'Public Function PrintExcel(ByVal param As SsCommon.ServiceParam, ByVal _cri As Model.ModelCriteriaList, ByVal headers As Dictionary(Of String, String)) As Byte()
    <OperationContract()> _
    Public Function PrintExcel(ByVal param As SsCommon.ServiceParam, ByVal _cri As Model.ModelCriteriaList, ByVal headers As Dictionary(Of String, String), ByRef EmplidList As List(Of Model.ModelInterfacetempEe)) As Byte()
        Dim ret As Byte() = Nothing
        Dim dt As New DataTable("Export")
        Me.ExecuteShowData(param, _cri, dt, headers, EmplidList)

        'If headers IsNot Nothing Then
        '    Dim cols As New List(Of DataColumn)
        '    For Each o In dt.Columns
        '        cols.Add(o)
        '    Next
        '    Dim i = 0
        '    For Each col As DataColumn In cols
        '        If headers.ContainsKey(col.ColumnName) Then
        '            If i = 151 Then
        '                col.ColumnName = headers.Item(col.ColumnName)
        '            Else
        '                col.ColumnName = headers.Item(col.ColumnName)
        '            End If
        '        Else
        '            dt.Columns.Remove(col)
        '        End If
        '        i += 1
        '    Next
        'End If

        If dt.Rows.Count > 0 Then
            ret = GetExcelByte(dt)
        End If

        Return ret
    End Function

    Private Function GetExcelByte(ByVal dta As DataTable) As Byte()
        Dim ret As Byte() = Nothing
        Dim curPath As String = AppDomain.CurrentDomain.BaseDirectory
        If curPath(curPath.Length - 1) <> "\" Then
            curPath = curPath & "\"
        End If
        curPath = curPath & "Temp\"
        Dim fileName As String = Me.GetType.Name.Replace("Service", "") & "_" & Guid.NewGuid.ToString

        DataTableToExcel(dta, curPath & fileName)

        'Set Break point here and go to 'Temp' directory you will see temporay file
        Dim oFile As System.IO.FileInfo
        oFile = New System.IO.FileInfo(curPath & fileName)
        Using oFileStream As System.IO.FileStream = oFile.OpenRead()
            Dim lBytes As Long = oFileStream.Length
            If (lBytes > 0) Then
                Dim fileData(lBytes - 1) As Byte
                ' Read the file into a byte array
                oFileStream.Read(fileData, 0, lBytes)
                oFileStream.Close()
                ret = fileData
            End If
        End Using
        System.IO.File.Delete(curPath & fileName)
        Return ret
    End Function

    Public Shared Sub DataTableToExcel(ByVal source As System.Data.DataTable, ByVal fileName As String)

        Using excelDoc = New System.IO.StreamWriter(fileName)
            Const startExcelXML As String = "<xml version=""1.0""?>" & vbCr & vbLf & "<Workbook " & "xmlns=""urn:schemas-microsoft-com:office:spreadsheet""" & vbCr & vbLf & " xmlns:o=""urn:schemas-microsoft-com:office:office""" & vbCr & vbLf & " " & "xmlns:x=""urn:schemas-    microsoft-com:office:" & "excel""" & vbCr & vbLf & " xmlns:ss=""urn:schemas-microsoft-com:" & "office:spreadsheet"">" & vbCr & vbLf & " <Styles>" & vbCr & vbLf & " " & "<Style ss:ID=""Default"" ss:Name=""Normal"">" & vbCr & vbLf & " " & "<Alignment ss:Vertical=""Bottom""/>" & vbCr & vbLf & " <Borders/>" & vbCr & vbLf & " <Font/>" & vbCr & vbLf & " <Interior/>" & vbCr & vbLf & " <NumberFormat/>" & vbCr & vbLf & " <Protection/>" & vbCr & vbLf & " </Style>" & vbCr & vbLf & " " & "<Style ss:ID=""BoldColumn"">" & vbCr & vbLf & " <Font " & "x:Family=""Swiss"" ss:Bold=""1""/>" & vbCr & vbLf & " </Style>" & vbCr & vbLf & " " & "<Style     ss:ID=""StringLiteral"">" & vbCr & vbLf & " <NumberFormat" & " ss:Format=""@""/>" & vbCr & vbLf & " </Style>" & vbCr & vbLf & " <Style " & "ss:ID=""Decimal"">" & vbCr & vbLf & " <NumberFormat " & "ss:Format=""0.00""/>" & vbCr & vbLf & " </Style>" & vbCr & vbLf & " " & "<Style ss:ID=""Integer"">" & vbCr & vbLf & " <NumberFormat " & "ss:Format=""0""/>" & vbCr & vbLf & " </Style>" & vbCr & vbLf & " <Style " & "ss:ID=""DateLiteral"">" & vbCr & vbLf & " <NumberFormat " & "ss:Format=""mm/dd/yyyy;@""/>" & vbCr & vbLf & " </Style>" & vbCr & vbLf & " <Style " & "ss:ID=""DateTimeLiteral"">" & vbCr & vbLf & " <NumberFormat " & "ss:Format=""mm/dd/yyyy hh:mm:ss AM/PM ;@""/>" & vbCr & vbLf & " </Style>" & vbCr & vbLf & " " & "</Styles>" & vbCr & vbLf & " "
            Const endExcelXML As String = "</Workbook>"

            Dim rowCount As Integer = 0
            Dim sheetCount As Integer = 1

            excelDoc.Write(startExcelXML)
            excelDoc.Write("<Worksheet ss:Name=""Sheet" & sheetCount & """>")
            excelDoc.Write("<Table>")
            excelDoc.Write("<Row>")
            For x As Integer = 0 To source.Columns.Count - 1
                excelDoc.Write("<Cell ss:StyleID=""BoldColumn""><Data ss:Type=""String"">")
                excelDoc.Write(source.Columns(x).Caption)
                excelDoc.Write("</Data></Cell>")
            Next
            excelDoc.Write("</Row>")
            For Each x As DataRow In source.Rows
                rowCount += 1
                'if the number of rows is > 64000 create a new page to continue output

                If rowCount = 64000 Then
                    rowCount = 0
                    sheetCount += 1
                    excelDoc.Write("</Table>")
                    excelDoc.Write(" </Worksheet>")
                    excelDoc.Write("<Worksheet ss:Name=""Sheet" & sheetCount & """>")
                    excelDoc.Write("<Table>")
                End If
                excelDoc.Write("<Row>")
                'ID=" + rowCount + "
                For y As Integer = 0 To source.Columns.Count - 1
                    Dim rowType As System.Type
                    rowType = x(y).[GetType]()
                    Select Case rowType.ToString()
                        Case "System.String"
                            Dim XMLstring As String = x(y).ToString()
                            XMLstring = XMLstring.Trim()
                            XMLstring = XMLstring.Replace("&", "&")
                            XMLstring = XMLstring.Replace(">", ">")
                            XMLstring = XMLstring.Replace("<", "<")
                            excelDoc.Write("<Cell ss:StyleID=""StringLiteral"">" & "<Data ss:Type=""String"">")
                            excelDoc.Write(XMLstring)
                            excelDoc.Write("</Data></Cell>")
                            Exit Select
                        Case "System.DateTime"
                            If IsDBNull(x(y)) Then
                                excelDoc.Write("<Cell ss:StyleID=""DateLiteral"" />")
                            Else
                                Dim XMLDate As DateTime = DirectCast(x(y), DateTime)
                                Dim XMLDatetoString As String = ""
                                'Excel Converted Date
                                XMLDatetoString = XMLDate.Year.ToString() & "-" & (If(XMLDate.Month < 10, "0" & XMLDate.Month.ToString(), XMLDate.Month.ToString())) & "-" & (If(XMLDate.Day < 10, "0" & XMLDate.Day.ToString(), XMLDate.Day.ToString())) & "T" & (If(XMLDate.Hour < 10, "0" & XMLDate.Hour.ToString(), XMLDate.Hour.ToString())) & ":" & (If(XMLDate.Minute < 10, "0" & XMLDate.Minute.ToString(), XMLDate.Minute.ToString())) & ":" & (If(XMLDate.Second < 10, "0" & XMLDate.Second.ToString(), XMLDate.Second.ToString())) & ".000"
                                Dim timeSt = Right(XMLDatetoString, 12)
                                If timeSt = "00:00:00.000" Then
                                    excelDoc.Write("<Cell ss:StyleID=""DateLiteral"">" & "<Data ss:Type=""DateTime"">")
                                Else
                                    excelDoc.Write("<Cell ss:StyleID=""DateTimeLiteral"">" & "<Data ss:Type=""DateTime"">")
                                End If
                                excelDoc.Write(XMLDatetoString)
                                excelDoc.Write("</Data></Cell>")
                            End If

                            Exit Select
                        Case "System.Boolean"
                            excelDoc.Write("<Cell ss:StyleID=""StringLiteral"">" & "<Data ss:Type=""String"">")
                            excelDoc.Write(x(y).ToString())
                            excelDoc.Write("</Data></Cell>")
                            Exit Select
                        Case "System.Int16", "System.Int32", "System.Int64", "System.Byte", "System.Single"
                            If x(y).ToString.Contains(".") Then
                                excelDoc.Write("<Cell ss:StyleID=""Decimal"">" & "<Data ss:Type=""Number"">")
                                excelDoc.Write(x(y).ToString())
                                excelDoc.Write("</Data></Cell>")
                                Exit Select
                            Else
                                excelDoc.Write("<Cell ss:StyleID=""Integer"">" & "<Data ss:Type=""Number"">")
                                excelDoc.Write(x(y).ToString())
                                excelDoc.Write("</Data></Cell>")
                                Exit Select
                            End If
                        Case "System.Decimal", "System.Double"
                            excelDoc.Write("<Cell ss:StyleID=""Decimal"">" & "<Data ss:Type=""Number"">")
                            excelDoc.Write(x(y).ToString())
                            excelDoc.Write("</Data></Cell>")
                            Exit Select
                        Case "System.DBNull"
                            excelDoc.Write("<Cell ss:StyleID=""StringLiteral"">" & "<Data ss:Type=""String"">")
                            excelDoc.Write("")
                            excelDoc.Write("</Data></Cell>")
                            Exit Select
                        Case Else
                            Throw (New Exception(rowType.ToString() & " not handled."))
                    End Select
                Next
                excelDoc.Write("</Row>")
            Next
            excelDoc.Write("</Table>")
            excelDoc.Write(" </Worksheet>")
            excelDoc.Write(endExcelXML)
            excelDoc.Close()
        End Using
    End Sub

#Region "Payroll 2019"
    Private Sub FindEmployeeForInterface(ByVal param As SsCommon.ServiceParam, ByVal _criteria As Model.ModelCriteriaList, ByRef sqlInsEmployee As String)   'dtoTempEE As List(Of Model.ModelInterfacetempEe))
        Dim dpBp As New SsHrCommonService
        Dim con As System.Data.Common.DbConnection = New OracleConnection(Util.GetOraConnectionString)
        Dim cmd As OracleCommand = con.CreateCommand
        cmd.BindByName = True
        Dim col As String = "A.Grade"
        Dim sqlText As String = String.Empty


        If authen IsNot Nothing AndAlso authen.Count > 0 Then
            Dim val = (From x In authen Where x.Dtcode = "HR" Select x.Localdescription).FirstOrDefault
            If val = "M" Then
                col = "A.manager_level"
            End If
        End If

        cmd.CommandType = CommandType.Text
        
        sqlText = "select x.EMPLID,x.EMPL_RCD,x.EFFDT,x.EFFSEQ,x.ACTION,x.ACTION_DT,x.ACTION_REASON,x.PER_ORG,x.DEPTID,x.JOBCODE,x.POSITION_NBR,x.POSITION_LEVEL,x.REPORT_TO," & _
                  "       x.POSN_OVRD,x.HR_STATUS,x.EMPL_STATUS,x.LOCATION,x.JOB_ENTRY_DT,x.DEPT_ENTRY_DT,x.POSITION_ENTRY_DT,x.POSNLEVEL_ENTRY_DT,x.SHIFT,x.REG_TEMP,x.FULL_PART_TIME, " & _
                  "       x.COMPANY, x.PAYGROUP, x.POITYPE, x.EMPLGROUP, x.EMPLIDCODE, x.HOLIDAY_SCHEDULE, x.STD_HOURS, x.STD_HRS_FREQUENCY, x.OFFICER_CD, x.EMPL_CLASS, x.GRADE, x.GRADE_ENTRY_DT, x.COMP_FREQUENCY, " & _
                  "       x.COMPRATE, x.CHANGE_AMT, x.CHANGE_PCT, x.CURRENCY_CD, x.BUSINESS_UNIT, x.SETID_DEPT, x.SETID_JOBCODE, x.HIRE_DT, x.LAST_HIRE_DT, x.TERMINATION_DT, x.ASGN_START_DT, x.LST_ASGN_START_DT, " & _
                  "       x.ASGN_END_DT, x.LAST_DATE_WORKED, x.EXPECTED_RETURN_DT, x.EXPECTED_END_DATE, x.PC_DATE_CPG, x.PROBATION_DT, x.PROBATION, x.PROBATION_TYPE, x.SOCIALWELF_PREFIX, x.CALSOCIALWELF, " & _
                  "       x.SOCIALWELFBEFYN, x.SOCIALWELFID, x.PERCENTSOCIALWELF, x.SOCIAL_BRANCH_CPG, x.ISCOMPSOCIALWELF, x.PAYORGCODE, x.PERIODMASTID, x.BONUS, x.CALTAXMETHOD, x.CCA_CPG, x.JOB_INDICATOR, x.JOBOPEN_NO, " & _
                  "       x.PAYROLLID, x.MANAGER_LEVEL, x.SAL_ADMIN_PLAN, x.EMPLFLAG, x.ISINTERFACE, x.PRE_COMPANY, x.CREATEUSER, x.CREATEDATE, x.MODIFYDATE, x.PROGRAMCODE, " & _
                  "      (x.rateid_remark || decode(x.payorgcode_remark,null,'',',' || x.payorgcode_remark) || decode(x.periodmastid_remark,null,'',',' || periodmastid_remark) || decode(x.marrystatus_remark,null,'',',' || marrystatus_remark) || decode(x.bankcode_remark,null,'',',' || bankcode_remark) || decode(x.accountid_remark,null,'',',' || accountid_remark) || decode(x.bankaccount_remark,null,'',',' || bankaccount_remark) || decode(x.payrollid_remark,null,'',',' || payrollid_remark) || decode(x.sat_work_remark,null,'',',' || sat_work_remark) || decode(x.count_bank,1,'',',' || 'No data found in Table PYCOMBANK. (Company = ' || x.company || ', Emplid = ' || x.emplid)) remarks, " & _
                  "       x.RATEID, x.RATEQTY, x.AMOUNT, x.INCEXPCODE, x.INCEXPAMT, x.INCEXPQTY, " & _
                  "       x.EMPL_TITLE, x.EMPL_NAME, x.EMPL_SEX, x.EMP_MARRYSTATUS, x.STATUS, x.BIRTHDATE, x.BANKCODE, x.ACCOUNTID, x.BANKACCOUNT, x.BANK_BRANCH, x.HARMFUL, x.WORKHOUR, x.NID, x.ADDRESS1, x.ADDRESS2, x.SCNO, x.TAXID, x.WORKDAY_MONTH " & _
                  "from ( " & _
                  "      SELECT M.EMPLID, M.EMPL_RCD, M.EFFDT, M.EFFSEQ, M.ACTION, M.ACTION_DT, M.ACTION_REASON, M.PER_ORG, M.DEPTID, M.JOBCODE, M.POSITION_NBR, M.POSITION_LEVEL, M.REPORT_TO, M.POSN_OVRD, M.HR_STATUS, M.EMPL_STATUS, M.LOCATION, M.JOB_ENTRY_DT, M.DEPT_ENTRY_DT, M.POSITION_ENTRY_DT, M.POSNLEVEL_ENTRY_DT, M.SHIFT, M.REG_TEMP, M.FULL_PART_TIME, M.COMPANY, M.PAYGROUP, M.POITYPE, M.EMPLGROUP, M.EMPLIDCODE, M.HOLIDAY_SCHEDULE, M.STD_HOURS, M.STD_HRS_FREQUENCY, M.OFFICER_CD, M.EMPL_CLASS, M.GRADE, M.GRADE_ENTRY_DT, M.COMP_FREQUENCY, M.COMPRATE, M.CHANGE_AMT, M.CHANGE_PCT, M.CURRENCY_CD, M.BUSINESS_UNIT, M.SETID_DEPT, M.SETID_JOBCODE, M.HIRE_DT, M.LAST_HIRE_DT, M.TERMINATION_DT, M.ASGN_START_DT, M.LST_ASGN_START_DT, M.ASGN_END_DT, M.LAST_DATE_WORKED, M.EXPECTED_RETURN_DT, M.EXPECTED_END_DATE, M.PC_DATE_CPG, M.PROBATION_DT, M.PROBATION, M.PROBATION_TYPE, M.SOCIALWELF_PREFIX, M.CALSOCIALWELF, M.SOCIALWELFBEFYN, M.SOCIALWELFID, M.PERCENTSOCIALWELF, M.SOCIAL_BRANCH_CPG, M.ISCOMPSOCIALWELF, M.PAYORGCODE, M.PERIODMASTID, M.BONUS, M.CALTAXMETHOD, M.CCA_CPG, M.JOB_INDICATOR, M.JOBOPEN_NO, M.PAYROLLID, M.MANAGER_LEVEL, M.SAL_ADMIN_PLAN, M.EMPLFLAG, M.PRE_COMPANY, M.CREATEUSER, M.CREATEDATE, M.MODIFYDATE, M.PROGRAMCODE, ( CASE WHEN BISINTERFACE = 'Y' THEN 'Y' ELSE CASE WHEN PROBATION       <> BPROBATION OR PROBATION_TYPE    <> BPROBATION_TYPE OR SOCIALWELF_PREFIX <> BSOCIALWELF_PREFIX OR CALSOCIALWELF     <> BCALSOCIALWELF OR SOCIALWELFBEFYN   <> BSOCIALWELFBEFYN OR SOCIALWELFID      <> BSOCIALWELFID OR PERCENTSOCIALWELF <>BPERCENTSOCIALWELF OR SOCIAL_BRANCH_CPG <> BSOCIAL_BRANCH_CPG OR ISCOMPSOCIALWELF  <> BISCOMPSOCIALWELF OR PAYORGCODE        <> BPAYORGCODE OR PERIODMASTID      <> BPERIODMASTID OR BONUS             <> BBONUS OR CALTAXMETHOD      <> BCALTAXMETHOD OR CCA_CPG           <> BCCA_CPG OR PAYROLLID         <> BPAYROLLID OR (BEMPLFLAG        IS NOT NULL AND EMPLFLAG         <> BEMPLFLAG) THEN 'N' ELSE 'Y' END END) ISINTERFACE, RATEID, RATEQTY, AMOUNT, INCEXPCODE, INCEXPQTY, INCEXPAMT, M.EMPL_TITLE, M.EMPL_NAME, M.ADDRESS1, M.ADDRESS2, ( CASE WHEN M.EMPL_SEX = 'F' THEN '0' WHEN M.EMPL_SEX = 'M' THEN '1' END) AS EMPL_SEX, M.EMP_MARRYSTATUS, M.STATUS, M.BIRTHDATE, M.BANKCODE, M.ACCOUNTID, M.BANKACCOUNT, M.BANK_BRANCH, M.HARMFUL, M.WORKHOUR, M.NID, M.SCNO, M.TAXID, M.SAT_WORK, " & _
                  "      decode(rateid,null, 'Salary cannot be null.','') rateid_remark,decode(m.payorgcode,null,'Payorgcode cannot be null.','') payorgcode_remark,decode(m.periodmastid,null,'Periodmaster Id. cannot be null.','') periodmastid_remark,decode(m.emp_marrystatus,null,'MarryStatus cannot be null.','') marrystatus_remark,decode(m.bankcode,null,'Bankcode cannot be null.','') bankcode_remark,decode(m.accountid,null,'Accountid cannot be null.','') accountid_remark,decode(m.bankaccount,null,'Bankaccount cannot be null.','') bankaccount_remark,decode(m.payrollid,null,'National ID. cannot be null.','') payrollid_remark,decode(m.sat_work,null,'Job Data --> Saturday Work cannot be null.','') sat_work_remark,(select count(*) from pycombank a where a.company = m.company and a.bankid = (select b.bank_cd from pers_bank b where b.emplid = m.emplid and account_id = '1')) count_bank, decode(m.sat_work,'Y',m.sat_workday,m.nosat_workday) workday_month " & _
                  "      FROM (SELECT c.sat_workday,c.nosat_workday,A.*, ( CASE WHEN A.ACTION = 'HIR' AND A.EFFDT BETWEEN :C_STDATE AND :C_EDDATE THEN 'N' WHEN A.ACTION = 'LOA' AND A.EFFDT BETWEEN :C_STDATE AND :C_EDDATE THEN 'L' WHEN A.HR_STATUS = 'I' THEN 'R' ELSE 'U' END) EMPLFLAG, NVL ( (SELECT company FROM job_ee O WHERE O.emplid = a.emplid AND O.empl_rcd = a.empl_rcd AND O.effdt    = (SELECT MAX (t.effdt) FROM job_ee t WHERE t.emplid = O.emplid AND t.empl_rcd = O.empl_rcd AND t.effdt    < a.effdt ) AND O.EFFSEQ = (SELECT MAX (t.EFFSEQ) FROM job_ee t WHERE t.emplid = O.emplid AND t.empl_rcd = O.empl_rcd AND t.effdt    = a.effdt ) ), A.company) PRE_COMPANY, NVL (A.MODIFYDATE, A.CREATEDATE) DATEOFREOCRD, NVL(B.PROBATION,0) BPROBATION, NVL(B.PROBATION_TYPE,'') BPROBATION_TYPE , NVL(B.SOCIALWELF_PREFIX,'') BSOCIALWELF_PREFIX, NVL(B.CALSOCIALWELF,'') BCALSOCIALWELF, NVL(B.SOCIALWELFBEFYN,'') BSOCIALWELFBEFYN, NVL(B.SOCIALWELFID,'') BSOCIALWELFID, NVL(B.PERCENTSOCIALWELF,0) BPERCENTSOCIALWELF , NVL(B.SOCIAL_BRANCH_CPG,'') BSOCIAL_BRANCH_CPG, NVL(B.ISCOMPSOCIALWELF,'') BISCOMPSOCIALWELF, NVL(B.PAYORGCODE,'') BPAYORGCODE, NVL(B.PERIODMASTID,0) BPERIODMASTID, NVL(B.BONUS,0) BBONUS, NVL(B.CALTAXMETHOD,'') BCALTAXMETHOD, NVL(B.CCA_CPG,'') BCCA_CPG, NVL(B.PAYROLLID,'') BPAYROLLID, NVL(B.ISINTERFACE,'N' ) BISINTERFACE, B.EMPLFLAG AS BEMPLFLAG, (SELECT wm_concat (g.COMP_RATECD) FROM compensation_tbl g LEFT OUTER JOIN pyincomeexpense i ON G.COMP_RATECD = I.INCEXPCODE WHERE g.emplid   = a.emplid AND g.EMPL_RCD   = a.EMPL_RCD AND g.EFFDT      = a.EFFDT AND g.EFFSEQ     = a.EFFSEQ AND I.BASESALARY = 'Y' GROUP BY I.BASESALARY ) RATEID, (SELECT wm_concat (g.PAYQTY) FROM compensation_tbl g LEFT OUTER JOIN pyincomeexpense i ON G.COMP_RATECD = I.INCEXPCODE WHERE g.emplid   = a.emplid AND g.EMPL_RCD   = a.EMPL_RCD AND g.EFFDT      = a.EFFDT AND g.EFFSEQ     = a.EFFSEQ AND I.BASESALARY = 'Y' GROUP BY I.BASESALARY ) RATEQTY, (SELECT wm_concat (g.COMPENSATION_RATE) FROM compensation_tbl g LEFT OUTER JOIN pyincomeexpense i ON G.COMP_RATECD = I.INCEXPCODE WHERE g.emplid   = a.emplid AND g.EMPL_RCD   = a.EMPL_RCD AND g.EFFDT      = a.EFFDT AND g.EFFSEQ     = a.EFFSEQ AND I.BASESALARY = 'Y' GROUP BY I.BASESALARY ) AMOUNT, (SELECT wm_concat (g.COMP_RATECD) FROM compensation_tbl g LEFT OUTER JOIN pyincomeexpense i ON G.COMP_RATECD = I.INCEXPCODE WHERE g.emplid   = a.emplid AND g.EMPL_RCD   = a.EMPL_RCD AND g.EFFDT      = a.EFFDT AND g.EFFSEQ     = a.EFFSEQ AND I.BASESALARY = 'N' GROUP BY I.BASESALARY ) INCEXPCODE, (SELECT wm_concat (g.PAYQTY) FROM compensation_tbl g LEFT OUTER JOIN pyincomeexpense i ON G.COMP_RATECD = I.INCEXPCODE WHERE g.emplid   = a.emplid AND g.EMPL_RCD   = a.EMPL_RCD AND g.EFFDT      = a.EFFDT AND g.EFFSEQ     = a.EFFSEQ AND I.BASESALARY = 'N' GROUP BY I.BASESALARY ) INCEXPQTY, (SELECT wm_concat (g.COMPENSATION_RATE) FROM compensation_tbl g LEFT OUTER JOIN pyincomeexpense i ON G.COMP_RATECD = I.INCEXPCODE WHERE g.emplid   = a.emplid AND g.EMPL_RCD   = a.EMPL_RCD AND g.EFFDT      = a.EFFDT AND g.EFFSEQ     = a.EFFSEQ AND I.BASESALARY = 'N' GROUP BY I.BASESALARY ) INCEXPAMT, P.EMPLTITLE AS EMPL_TITLE, ( CASE WHEN :LANG = :LOCAL THEN P.EMPLFIRSTNAME || ' ' || P.EMPLLASTNAME ELSE P.EMPLENGFIRSTNAME || ' ' || P.EMPLENGLASTNAME END) EMPL_NAME, (P.BUILDING || ' ' || ( CASE WHEN P.ADDRESS IS NOT NULL THEN 'No. ' || P.ADDRESS ELSE '' END) || ' ' || P.STREET || ' ' || ( CASE WHEN P.TAMBOL IS NOT NULL THEN P.TAMBOL || ' Sub - District' ELSE '' END)) || ' ' ||( ( CASE WHEN P.AMPHUR IS NOT NULL THEN P.AMPHUR || ' District' ELSE '' END) || ' ' || (SELECT CASE WHEN :LANG = :LOCAL THEN GD.LOCALDESCRIPTION ELSE NVL (GD.ENGDESCRIPTION, GD.LOCALDESCRIPTION) END FROM PYGENERALDT GD WHERE GD.GDCODE = 'City' AND GD.DTCODE   = P.CITY ) || ' ' || (SELECT CASE WHEN :LANG = :LOCAL THEN GD.LOCALDESCRIPTION ELSE NVL (GD.ENGDESCRIPTION, GD.LOCALDESCRIPTION) END FROM PYGENERALDT GD WHERE GD.GDCODE = 'Province' AND GD.DTCODE   = P.STATE ) || ' ' || P.POSTAL || ' ' || (SELECT CASE WHEN :LANG = :LOCAL THEN GD.LOCALDESCRIPTION ELSE NVL (GD.ENGDESCRIPTION, GD.LOCALDESCRIPTION) END FROM PYGENERALDT GD WHERE GD.GDCODE = 'Country' AND GD.DTCODE   = P.COUNTRY )) AS ADDRESS1, (P.BUILDING || ' ' || ( CASE WHEN P.ADDRESS IS NOT NULL THEN 'No. ' || P.ADDRESS ELSE '' END) || ' ' || P.STREET || ' ' || ( CASE WHEN P.TAMBOL IS NOT NULL THEN P.TAMBOL || ' Sub - District' ELSE '' END)) || ' ' ||( ( CASE WHEN P.AMPHUR IS NOT NULL THEN P.AMPHUR || ' District' ELSE '' END) || ' ' || (SELECT CASE WHEN :LANG = :LOCAL THEN GD.LOCALDESCRIPTION ELSE NVL (GD.ENGDESCRIPTION, GD.LOCALDESCRIPTION) END FROM PYGENERALDT GD WHERE GD.GDCODE = 'City' AND GD.DTCODE   = P.CITY ) || ' ' || (SELECT CASE WHEN :LANG = :LOCAL THEN GD.LOCALDESCRIPTION ELSE NVL (GD.ENGDESCRIPTION, GD.LOCALDESCRIPTION) END FROM PYGENERALDT GD WHERE GD.GDCODE = 'Province' AND GD.DTCODE   = P.STATE ) || ' ' || P.POSTAL || ' ' || (SELECT CASE WHEN :LANG = :LOCAL THEN GD.LOCALDESCRIPTION ELSE NVL (GD.ENGDESCRIPTION, GD.LOCALDESCRIPTION) END FROM PYGENERALDT GD WHERE GD.GDCODE = 'Country' AND GD.DTCODE   = P.COUNTRY ))            AS ADDRESS2, P.SEX         AS EMPL_SEX, P.MARRYSTATUS AS EMP_MARRYSTATUS, (SELECT ( CASE WHEN AC.EMPL_STATUS = 'T' THEN 3 ELSE 1 END) FROM Action_Tbl AC WHERE AC.ACTION = A.ACTION AND AC.EFFDT    = (SELECT MAX(ACT.EFFDT) FROM Action_Tbl ACT WHERE ACT.ACTION = AC.ACTION ) )                 AS STATUS, P.BIRTHDATE       AS BIRTHDATE, PK.BANKCODE       AS BANKCODE, K.ACCOUNT_ID      AS ACCOUNTID, K.BANK_ACCOUNT    AS BANKACCOUNT, K.BANK_BRANCH     AS BANK_BRANCH, A.HARMFUL_PERCENT AS HARMFUL, a.workdayhour AS WORKHOUR, (SELECT PN.NATIONAL_ID FROM PERSON_NID PN WHERE A.EMPLID          = PN.EMPLID AND P.COUNTRY           = PN.COUNTRY AND PN.NATIONAL_ID_TYPE = 'PID' ) AS NID, (SELECT PN.NATIONAL_ID FROM PERSON_NID PN WHERE A.EMPLID          = PN.EMPLID AND P.COUNTRY           = PN.COUNTRY AND PN.NATIONAL_ID_TYPE = 'SSOID' ) AS SCNO, (SELECT PN.NATIONAL_ID FROM PERSON_NID PN WHERE A.EMPLID          = PN.EMPLID AND P.COUNTRY           = PN.COUNTRY AND PN.NATIONAL_ID_TYPE = 'TAXID' ) AS TAXID FROM ( SELECT A.* FROM JOB_EE A WHERE A.HR_STATUS = 'A' and a.empl_rcd = 0 AND A.EFFDT       = (SELECT MAX(JT.EFFDT) FROM JOB_EE JT WHERE JT.EMPLID = A.EMPLID AND JT.EMPL_RCD = A.EMPL_RCD AND JT.EFFDT   <= :C_EDDATE ) AND A.EFFSEQ = (SELECT MAX(JT.EFFSEQ) FROM JOB_EE JT WHERE JT.EMPLID = A.EMPLID AND JT.EMPL_RCD = A.EMPL_RCD AND JT.EFFDT    = A.EFFDT ) UNION ALL SELECT A.* FROM JOB_EE A WHERE A.HR_STATUS = 'I' AND A.EFFDT       = (SELECT MAX(JT.EFFDT) FROM JOB_EE JT WHERE JT.EMPLID = A.EMPLID AND JT.EMPL_RCD = A.EMPL_RCD AND JT.EFFDT BETWEEN :C_STDATE AND :C_EDDATE ) AND A.EFFSEQ = (SELECT MAX(JT.EFFSEQ) FROM JOB_EE JT WHERE JT.EMPLID = A.EMPLID AND JT.EMPL_RCD = A.EMPL_RCD AND JT.EFFDT    = A.EFFDT ) ) A LEFT OUTER JOIN INTERFACE_EE B ON A.EMPLID    = B.EMPLID AND A.EMPL_RCD = B.EMPL_RCD AND A.EFFDT    = B.EFFDT AND A.EFFSEQ   = B.EFFSEQ AND NVL(B.ISINTERFACE,'N') = 'N' LEFT OUTER JOIN PERSON_TBL P ON A.EMPLID = P.EMPLID LEFT OUTER JOIN PERS_BANK K ON A.EMPLID      = K.EMPLID AND K.ACCOUNT_ID = 1 LEFT OUTER JOIN PYBANK PK ON K.BANK_CD      = PK.BANKID left outer join (select a.* from company_dtl a where a.effdt = (select max(b.effdt) from company_dtl b where b.company = a.company and b.month = a.month and b.effdt <= sysdate)) c on c.company = a.company and c.month = to_char(:C_EDDATE,'mm') WHERE A.EMPL_CLASS = NVL (:C_EMPLCLASS, A.EMPL_CLASS) AND A.COMPANY      = :C_COMP AND A.PAYORGCODE LIKE NVL(:PAYORGCODE || '%',A.PAYORGCODE) AND A.EMPLID              = NVL (:C_EMPLID, A.EMPLID) AND A.MANAGER_LEVEL       = NVL(:MANAGERLVL,A.MANAGER_LEVEL) AND NVL(A.PERIODMASTID,7) = DECODE(:PERIODMASTID,NULL,NVL(A.PERIODMASTID,7),:PERIODMASTID) AND NVL(P.FOREIGNER,'N') = 'N' {0} ORDER BY A.EMPLID, A.EFFDT ) M WHERE M.EMPLFLAG = NVL(:EMPLFLAG,M.EMPLFLAG) " & _
                  "     ) x "

        '----------------------------------------------------------------------------

        Dim authenstr = String.Format("AND A.DEPTID IN ({0}) ", dpBp.StringAuthenDepartment(param.UserName, "A.DEPTID", col, Today))

        cmd.CommandText = String.Format(sqlText, authenstr)

        'Add by Chanchira L. on 19/04/2021
        sqlInsEmployee = cmd.CommandText

    End Sub

    Private Sub FindCompenForInterface(param As SsCommon.ServiceParam, _criteria As Model.ModelCriteriaList, ByRef sqlInsCompen As String)   ' dtoTempComp As List(Of Model.ModelInterfacetempCompen))
        Dim dpBp As New SsHrCommonService
        Dim con As System.Data.Common.DbConnection = New OracleConnection(Util.GetOraConnectionString)
        Dim cmd As OracleCommand = con.CreateCommand
        cmd.BindByName = True
        Dim col As String = "A.Grade"
        Dim sqlText As String = String.Empty


        If authen IsNot Nothing AndAlso authen.Count > 0 Then
            Dim val = (From x In authen Where x.Dtcode = "HR" Select x.Localdescription).FirstOrDefault
            If val = "M" Then
                col = "A.manager_level"
            End If
        End If

        cmd.CommandType = CommandType.Text
        '-- Watinee 05/06/2013
        'Select Job_ee data Between Start and End of period follow Criteria and If there are already Record and column below not change ISINTERFACE = 'N' ELSE 'Y'
        'M.PROBATION, M.PROBATION_TYPE, M.SOCIALWELF_PREFIX, M.CALSOCIALWELF, M.SOCIALWELFBEFYN, M.SOCIALWELFID, M.PERCENTSOCIALWELF, M.SOCIAL_BRANCH_CPG, M.ISCOMPSOCIALWELF,
        'M.PAYORGCODE, M.PERIODMASTID, M.BONUS, M.CALTAXMETHOD, M.CCA_CPG, M.PAYROLLID

        '--Kitinan 27/8/2014 max EFFDT Job_ee and compen
        'sqlText = "SELECT M.* ,(CASE WHEN N.ISINTERFACE = 'Y' THEN 'Y' ELSE CASE WHEN M.COMPENSATION_RATE <> NVL(N.COMPENSATION_RATE,0) OR M.PAYQTY <> NVL(N.PAYQTY,0) OR  M.CHANGE_AMT <> NVL(N.CHANGE_AMT,0) THEN 'Y' ELSE 'N' END END) ISINTERFACE FROM  " & _
        '            "(SELECT  A.COMPANY, B.EMPLID,B.EMPL_RCD,B.EFFDT,B.EFFSEQ,B.COMP_RATECD,B.INCEXPTYPE,B.PAYQTY,B.COMPENSATION_RATE,B.CHANGE_AMT,B.FREQUENCY,B.CREATEUSER,B.CREATEDATE, " & _
        '            "         B.MODIFYDATE,B.PROGRAMCODE, (CASE WHEN A.ACTION = 'HIR'  AND A.EFFDT BETWEEN :C_STDATE AND :C_EDDATE THEN 'N' WHEN A.ACTION = 'LOA' AND A.EFFDT BETWEEN :C_STDATE AND :C_EDDATE THEN 'L' WHEN A.HR_STATUS = 'I' THEN 'R' ELSE 'U'  END) EMPLFLAG, " & _
        '            "         A.MANAGER_LEVEL,A.EMPL_CLASS,A.GRADE,A.DEPTID,A.PERIODMASTID " & _
        '            "FROM JOB_EE A LEFT OUTER JOIN COMPENSATION_TBL B ON A.EMPLID = B.EMPLID AND A.EMPL_RCD = B.EMPL_RCD AND A.EFFDT = B.EFFDT  AND A.EFFSEQ = B.EFFSEQ   " & _
        '            "WHERE  A.EFFDT BETWEEN :C_STDATE AND :C_EDDATE   AND   A.EMPL_CLASS = NVL (:C_EMPLCLASS, A.EMPL_CLASS) AND A.COMPANY = :C_COMP  " & _
        '            "AND A.EMPLID = NVL (:C_EMPLID, A.EMPLID) AND A.MANAGER_LEVEL = NVL(:MANAGERLVL,A.MANAGER_LEVEL) AND A.PERIODMASTID = :PERIODMASTID  {0}  ) M          " & _
        '            "LEFT OUTER JOIN INTERFACE_COMPEN N ON  M.EMPLID = N.EMPLID AND M.EMPL_RCD = N.EMPL_RCD AND M.EFFDT = N.EFFDT AND M.EFFSEQ = N.EFFSEQ AND M.COMP_RATECD = N.COMP_RATECD " & _
        '            "WHERE M.EMPLFLAG = NVL(:EMPLFLAG,M.EMPLFLAG)  "

        'sqlText = "SELECT M.* ,(CASE WHEN N.ISINTERFACE = 'Y' THEN 'Y' ELSE CASE WHEN M.COMPENSATION_RATE <> NVL(N.COMPENSATION_RATE,0) OR M.PAYQTY <> NVL(N.PAYQTY,0) OR  M.CHANGE_AMT <> NVL(N.CHANGE_AMT,0) OR (N.EMPLFLAG IS NOT NULL AND M.EMPLFLAG <> N.EMPLFLAG) THEN 'Y' ELSE 'N' END END) ISINTERFACE FROM    " & _
        '            "                    (SELECT  A.COMPANY, B.EMPLID,B.EMPL_RCD,B.EFFDT,B.EFFSEQ,B.COMP_RATECD,B.INCEXPTYPE,B.PAYQTY,B.COMPENSATION_RATE,B.CHANGE_AMT,B.FREQUENCY,B.CREATEUSER,B.CREATEDATE,   " & _
        '            "                             B.MODIFYDATE,B.PROGRAMCODE, (CASE WHEN A.ACTION = 'HIR'  AND A.EFFDT BETWEEN :C_STDATE AND :C_EDDATE THEN 'N' WHEN A.ACTION = 'LOA' AND A.EFFDT BETWEEN :C_STDATE AND :C_EDDATE THEN 'L' WHEN A.HR_STATUS = 'I' THEN 'R' ELSE 'U'  END) EMPLFLAG,   " & _
        '            "                             A.MANAGER_LEVEL,A.EMPL_CLASS,A.GRADE,A.DEPTID,A.PERIODMASTID   " & _
        '            "                    FROM JOB_EE A LEFT OUTER JOIN COMPENSATION_TBL B ON A.EMPLID = B.EMPLID AND A.EMPL_RCD = B.EMPL_RCD AND A.EFFDT = B.EFFDT  AND A.EFFSEQ = B.EFFSEQ     " & _
        '            "                    WHERE A.EFFDT = (SELECT MAX(JT.EFFDT) FROM JOB_EE JT WHERE JT.EMPLID = A.EMPLID " & _
        '            "                          AND JT.EMPL_RCD = A.EMPL_RCD AND JT.EFFDT  BETWEEN  :C_STDATE AND :C_EDDATE ) AND A.EFFSEQ = (SELECT MAX(JT.EFFSEQ) FROM JOB_EE JT WHERE JT.EMPLID = A.EMPLID " & _
        '            "                          AND JT.EMPL_RCD = A.EMPL_RCD AND JT.EFFDT = A.EFFDT)   AND   A.EMPL_CLASS = NVL (:C_EMPLCLASS, A.EMPL_CLASS) AND A.COMPANY = :C_COMP AND A.PAYORGCODE like NVL(:PAYORGCODE || '%',A.PAYORGCODE)   " & _
        '            "                    AND A.EMPLID = NVL (:C_EMPLID, A.EMPLID) AND A.MANAGER_LEVEL = NVL(:MANAGERLVL,A.MANAGER_LEVEL) AND NVL(A.PERIODMASTID,0) = DECODE(A.PERIODMASTID,NULL,0,:PERIODMASTID)  {0}  ) M            " & _
        '            "                    LEFT OUTER JOIN INTERFACE_COMPEN N ON  M.EMPLID = N.EMPLID AND M.EMPL_RCD = N.EMPL_RCD AND M.EFFDT = N.EFFDT AND M.EFFSEQ = N.EFFSEQ AND M.COMP_RATECD = N.COMP_RATECD   " & _
        '            "                    WHERE M.EMPLFLAG = NVL(:EMPLFLAG,M.EMPLFLAG) "

        'ปรับใหม่เพื่อส่งให้อั้ม  
        '13/03/2020 เปลี่ยนค่า isinterface --> THEN 'Y' ELSE 'N' END END) ISINTERFACE เป็น  THEN 'N' ELSE 'Y' END END) ISINTERFACE 
        'sqlText = "SELECT M.* ,(CASE WHEN N.ISINTERFACE = 'Y' THEN 'Y' ELSE CASE WHEN M.COMPENSATION_RATE <> NVL(N.COMPENSATION_RATE,0) OR M.PAYQTY <> NVL(N.PAYQTY,0) OR  M.CHANGE_AMT <> NVL(N.CHANGE_AMT,0) OR (N.EMPLFLAG IS NOT NULL AND M.EMPLFLAG <> N.EMPLFLAG) THEN 'N' ELSE 'Y' END END) ISINTERFACE FROM    " & _
        '     "                    (SELECT B.* FROM (SELECT  A.COMPANY, B.EMPLID,B.EMPL_RCD,B.EFFDT,B.EFFSEQ,B.COMP_RATECD,B.INCEXPTYPE,B.PAYQTY,B.COMPENSATION_RATE,B.CHANGE_AMT,B.FREQUENCY,B.CREATEUSER,B.CREATEDATE,   " & _
        '     "                             B.MODIFYDATE,B.PROGRAMCODE, (CASE WHEN A.ACTION = 'HIR'  AND A.EFFDT BETWEEN :C_STDATE AND :C_EDDATE THEN 'N' WHEN A.ACTION = 'LOA' AND A.EFFDT BETWEEN :C_STDATE AND :C_EDDATE THEN 'L' WHEN A.HR_STATUS = 'I' THEN 'R' ELSE 'U'  END) EMPLFLAG,   " & _
        '     "                             A.MANAGER_LEVEL,A.EMPL_CLASS,A.GRADE,A.DEPTID,A.PERIODMASTID,a.payorgcode    " & _
        '     "                    FROM JOB_EE A LEFT OUTER JOIN COMPENSATION_TBL B ON A.EMPLID = B.EMPLID AND A.EMPL_RCD = B.EMPL_RCD AND A.EFFDT = B.EFFDT  AND A.EFFSEQ = B.EFFSEQ     " & _
        '     "                    WHERE A.EFFDT = (SELECT MAX(JT.EFFDT) FROM JOB_EE JT WHERE JT.EMPLID = A.EMPLID " & _
        '     "                          AND JT.EMPL_RCD = A.EMPL_RCD AND JT.EFFDT <= :C_EDDATE ) AND A.EFFSEQ = (SELECT MAX(JT.EFFSEQ) FROM JOB_EE JT WHERE JT.EMPLID = A.EMPLID " & _
        '     "                          AND JT.EMPL_RCD = A.EMPL_RCD AND JT.EFFDT = A.EFFDT ) AND A.HR_STATUS = 'A' AND A.EMPL_RCD = 0" & _
        '     " UNION ALL " & _
        '     " SELECT  A.COMPANY, B.EMPLID,B.EMPL_RCD,B.EFFDT,B.EFFSEQ,B.COMP_RATECD,B.INCEXPTYPE,B.PAYQTY,B.COMPENSATION_RATE,B.CHANGE_AMT,B.FREQUENCY,B.CREATEUSER,B.CREATEDATE,   " & _
        '     "                             B.MODIFYDATE,B.PROGRAMCODE, (CASE WHEN A.ACTION = 'HIR'  AND A.EFFDT BETWEEN :C_STDATE AND :C_EDDATE THEN 'N' WHEN A.ACTION = 'LOA' AND A.EFFDT BETWEEN :C_STDATE AND :C_EDDATE THEN 'L' WHEN A.HR_STATUS = 'I' THEN 'R' ELSE 'U'  END) EMPLFLAG,   " & _
        '     "                             A.MANAGER_LEVEL,A.EMPL_CLASS,A.GRADE,A.DEPTID,A.PERIODMASTID,a.payorgcode    " & _
        '     "                    FROM JOB_EE A LEFT OUTER JOIN COMPENSATION_TBL B ON A.EMPLID = B.EMPLID AND A.EMPL_RCD = B.EMPL_RCD AND A.EFFDT = B.EFFDT  AND A.EFFSEQ = B.EFFSEQ     " & _
        '     "                    WHERE A.EFFDT = (SELECT MAX(JT.EFFDT) FROM JOB_EE JT WHERE JT.EMPLID = A.EMPLID " & _
        '     "                          AND JT.EMPL_RCD = A.EMPL_RCD AND JT.EFFDT BETWEEN :C_STDATE AND :C_EDDATE ) AND A.EFFSEQ = (SELECT MAX(JT.EFFSEQ) FROM JOB_EE JT WHERE JT.EMPLID = A.EMPLID " & _
        '     "                          AND JT.EMPL_RCD = A.EMPL_RCD AND JT.EFFDT = A.EFFDT ) AND A.HR_STATUS = 'I' ) B " & _
        '     "  WHERE B.COMPANY = :C_COMP AND   B.EMPL_CLASS = NVL (:C_EMPLCLASS, B.EMPL_CLASS) " & _
        '     "  AND B.PAYORGCODE like NVL(:PAYORGCODE || '%',B.PAYORGCODE)  " & _
        '     "  AND B.EMPLID = NVL (:C_EMPLID, B.EMPLID) AND B.MANAGER_LEVEL = NVL(:MANAGERLVL,B.MANAGER_LEVEL) " & _
        '     "  AND NVL(B.PERIODMASTID,7) = DECODE(:PERIODMASTID,NULL,NVL(B.PERIODMASTID,7),:PERIODMASTID) {0}  ) M            " & _
        '     "                    LEFT OUTER JOIN INTERFACE_COMPEN N ON  M.EMPLID = N.EMPLID AND M.EMPL_RCD = N.EMPL_RCD AND M.EFFDT = N.EFFDT AND M.EFFSEQ = N.EFFSEQ AND M.COMP_RATECD = N.COMP_RATECD AND NVL(N.ISINTERFACE,'N') = 'N' " & _
        '     "                    WHERE M.EMPLFLAG = NVL(:EMPLFLAG,M.EMPLFLAG) "
        'Changed by Chanchira L. on 15/09/2020 เพิ่มเงื่อนไข nvl(person_tbl.foreigner,'N') = 'N'
        'sqlText = "SELECT M.* ,(CASE WHEN N.ISINTERFACE = 'Y' THEN 'Y' ELSE CASE WHEN M.COMPENSATION_RATE <> NVL(N.COMPENSATION_RATE,0) OR M.PAYQTY <> NVL(N.PAYQTY,0) OR  M.CHANGE_AMT <> NVL(N.CHANGE_AMT,0) OR (N.EMPLFLAG IS NOT NULL AND M.EMPLFLAG <> N.EMPLFLAG) THEN 'N' ELSE 'Y' END END) ISINTERFACE FROM    " & _
        '     "                    (SELECT B.* FROM (SELECT  A.COMPANY, B.EMPLID,B.EMPL_RCD,B.EFFDT,B.EFFSEQ,B.COMP_RATECD,B.INCEXPTYPE,B.PAYQTY,B.COMPENSATION_RATE,B.CHANGE_AMT,B.FREQUENCY,B.CREATEUSER,B.CREATEDATE,   " & _
        '     "                             B.MODIFYDATE,B.PROGRAMCODE, (CASE WHEN A.ACTION = 'HIR'  AND A.EFFDT BETWEEN :C_STDATE AND :C_EDDATE THEN 'N' WHEN A.ACTION = 'LOA' AND A.EFFDT BETWEEN :C_STDATE AND :C_EDDATE THEN 'L' WHEN A.HR_STATUS = 'I' THEN 'R' ELSE 'U'  END) EMPLFLAG,   " & _
        '     "                             A.MANAGER_LEVEL,A.EMPL_CLASS,A.GRADE,A.DEPTID,A.PERIODMASTID,a.payorgcode    " & _
        '     "                    FROM JOB_EE A LEFT OUTER JOIN COMPENSATION_TBL B ON A.EMPLID = B.EMPLID AND A.EMPL_RCD = B.EMPL_RCD AND A.EFFDT = B.EFFDT  AND A.EFFSEQ = B.EFFSEQ     " & _
        '     "                    WHERE A.EFFDT = (SELECT MAX(JT.EFFDT) FROM JOB_EE JT WHERE JT.EMPLID = A.EMPLID " & _
        '     "                          AND JT.EMPL_RCD = A.EMPL_RCD AND JT.EFFDT <= :C_EDDATE ) AND A.EFFSEQ = (SELECT MAX(JT.EFFSEQ) FROM JOB_EE JT WHERE JT.EMPLID = A.EMPLID " & _
        '     "                          AND JT.EMPL_RCD = A.EMPL_RCD AND JT.EFFDT = A.EFFDT ) AND A.HR_STATUS = 'A' AND A.EMPL_RCD = 0" & _
        '     " UNION ALL " & _
        '     " SELECT  A.COMPANY, B.EMPLID,B.EMPL_RCD,B.EFFDT,B.EFFSEQ,B.COMP_RATECD,B.INCEXPTYPE,B.PAYQTY,B.COMPENSATION_RATE,B.CHANGE_AMT,B.FREQUENCY,B.CREATEUSER,B.CREATEDATE,   " & _
        '     "                             B.MODIFYDATE,B.PROGRAMCODE, (CASE WHEN A.ACTION = 'HIR'  AND A.EFFDT BETWEEN :C_STDATE AND :C_EDDATE THEN 'N' WHEN A.ACTION = 'LOA' AND A.EFFDT BETWEEN :C_STDATE AND :C_EDDATE THEN 'L' WHEN A.HR_STATUS = 'I' THEN 'R' ELSE 'U'  END) EMPLFLAG,   " & _
        '     "                             A.MANAGER_LEVEL,A.EMPL_CLASS,A.GRADE,A.DEPTID,A.PERIODMASTID,a.payorgcode    " & _
        '     "                    FROM JOB_EE A LEFT OUTER JOIN COMPENSATION_TBL B ON A.EMPLID = B.EMPLID AND A.EMPL_RCD = B.EMPL_RCD AND A.EFFDT = B.EFFDT  AND A.EFFSEQ = B.EFFSEQ     " & _
        '     "                    WHERE A.EFFDT = (SELECT MAX(JT.EFFDT) FROM JOB_EE JT WHERE JT.EMPLID = A.EMPLID " & _
        '     "                          AND JT.EMPL_RCD = A.EMPL_RCD AND JT.EFFDT BETWEEN :C_STDATE AND :C_EDDATE ) AND A.EFFSEQ = (SELECT MAX(JT.EFFSEQ) FROM JOB_EE JT WHERE JT.EMPLID = A.EMPLID " & _
        '     "                          AND JT.EMPL_RCD = A.EMPL_RCD AND JT.EFFDT = A.EFFDT ) AND A.HR_STATUS = 'I' ) B " & _
        '     "  WHERE B.COMPANY = :C_COMP AND   B.EMPL_CLASS = NVL (:C_EMPLCLASS, B.EMPL_CLASS) " & _
        '     "  AND B.PAYORGCODE like NVL(:PAYORGCODE || '%',B.PAYORGCODE)  " & _
        '     "  AND B.EMPLID = NVL (:C_EMPLID, B.EMPLID) AND B.MANAGER_LEVEL = NVL(:MANAGERLVL,B.MANAGER_LEVEL) " & _
        '     "  AND NVL(B.PERIODMASTID,7) = DECODE(:PERIODMASTID,NULL,NVL(B.PERIODMASTID,7),:PERIODMASTID) {0}  ) M            " & _
        '     "                    LEFT OUTER JOIN INTERFACE_COMPEN N ON  M.EMPLID = N.EMPLID AND M.EMPL_RCD = N.EMPL_RCD AND M.EFFDT = N.EFFDT AND M.EFFSEQ = N.EFFSEQ AND M.COMP_RATECD = N.COMP_RATECD AND NVL(N.ISINTERFACE,'N') = 'N' " & _
        '     "  ,PERSON_TBL P " & _
        '     "  WHERE M.EMPLFLAG = NVL(:EMPLFLAG,M.EMPLFLAG) " & _
        '     "  AND M.EMPLID = P.EMPLID AND NVL(P.FOREIGNER,'N') = 'N' "

        'cmd.Parameters.Clear()

        'cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_STDATE", .Value = _criteria.StartDate})
        'cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_EDDATE", .Value = _criteria.EndDate})
        'cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_EMPLCLASS", .Value = If(String.IsNullOrEmpty(_criteria.EmplClass), "", _criteria.EmplClass)})
        'cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_COMP", .Value = _criteria.Company})
        'cmd.Parameters.Add(New OracleParameter With {.ParameterName = "PAYORGCODE", .Value = If(_criteria.Org Is Nothing, "", _criteria.Org.Relateid)})
        'cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_EMPLID", .Value = If(String.IsNullOrEmpty(_criteria.Emplid), "", _criteria.Emplid)})
        'cmd.Parameters.Add(New OracleParameter With {.ParameterName = "EMPLFLAG", .Value = _criteria.EmplFlag})
        'cmd.Parameters.Add(New OracleParameter With {.ParameterName = "MANAGERLVL", .Value = If(String.IsNullOrEmpty(_criteria.Managerlvl), "", _criteria.Managerlvl)})
        'cmd.Parameters.Add(New OracleParameter With {.ParameterName = "PERIODMASTID", .Value = _criteria.PeriodMastId.Periodmastid})

        'Changed by Chanchira L. on 19/04/2021
        sqlText = "SELECT m.emplid,m.empl_rcd,m.effdt,m.effseq,m.comp_ratecd,m.incexptype,m.payqty,m.compensation_rate,m.change_amt,m.frequency,m.emplflag," & _
             "(CASE WHEN N.ISINTERFACE = 'Y' THEN 'Y' ELSE CASE WHEN M.COMPENSATION_RATE <> NVL(N.COMPENSATION_RATE,0) OR M.PAYQTY <> NVL(N.PAYQTY,0) OR  M.CHANGE_AMT <> NVL(N.CHANGE_AMT,0) OR (N.EMPLFLAG IS NOT NULL AND M.EMPLFLAG <> N.EMPLFLAG) THEN 'N' ELSE 'Y' END END) ISINTERFACE, " & _
             " m.createuser,m.createdate,m.modifydate,m.programcode,m.grade,m.manager_level,m.company,m.deptid,m.empl_class,m.periodmastid " & _
             "FROM    " & _
             "                    (SELECT B.* FROM (SELECT  A.COMPANY, B.EMPLID,B.EMPL_RCD,B.EFFDT,B.EFFSEQ,B.COMP_RATECD,B.INCEXPTYPE,B.PAYQTY,B.COMPENSATION_RATE,B.CHANGE_AMT,B.FREQUENCY,B.CREATEUSER,B.CREATEDATE,   " & _
             "                             B.MODIFYDATE,B.PROGRAMCODE, (CASE WHEN A.ACTION = 'HIR'  AND A.EFFDT BETWEEN :C_STDATE AND :C_EDDATE THEN 'N' WHEN A.ACTION = 'LOA' AND A.EFFDT BETWEEN :C_STDATE AND :C_EDDATE THEN 'L' WHEN A.HR_STATUS = 'I' THEN 'R' ELSE 'U'  END) EMPLFLAG,   " & _
             "                             A.MANAGER_LEVEL,A.EMPL_CLASS,A.GRADE,A.DEPTID,A.PERIODMASTID,a.payorgcode    " & _
             "                    FROM JOB_EE A LEFT OUTER JOIN COMPENSATION_TBL B ON A.EMPLID = B.EMPLID AND A.EMPL_RCD = B.EMPL_RCD AND A.EFFDT = B.EFFDT  AND A.EFFSEQ = B.EFFSEQ     " & _
             "                    WHERE A.EFFDT = (SELECT MAX(JT.EFFDT) FROM JOB_EE JT WHERE JT.EMPLID = A.EMPLID " & _
             "                          AND JT.EMPL_RCD = A.EMPL_RCD AND JT.EFFDT <= :C_EDDATE ) AND A.EFFSEQ = (SELECT MAX(JT.EFFSEQ) FROM JOB_EE JT WHERE JT.EMPLID = A.EMPLID " & _
             "                          AND JT.EMPL_RCD = A.EMPL_RCD AND JT.EFFDT = A.EFFDT ) AND A.HR_STATUS = 'A' AND A.EMPL_RCD = 0" & _
             " UNION ALL " & _
             " SELECT  A.COMPANY, B.EMPLID,B.EMPL_RCD,B.EFFDT,B.EFFSEQ,B.COMP_RATECD,B.INCEXPTYPE,B.PAYQTY,B.COMPENSATION_RATE,B.CHANGE_AMT,B.FREQUENCY,B.CREATEUSER,B.CREATEDATE,   " & _
             "                             B.MODIFYDATE,B.PROGRAMCODE, (CASE WHEN A.ACTION = 'HIR'  AND A.EFFDT BETWEEN :C_STDATE AND :C_EDDATE THEN 'N' WHEN A.ACTION = 'LOA' AND A.EFFDT BETWEEN :C_STDATE AND :C_EDDATE THEN 'L' WHEN A.HR_STATUS = 'I' THEN 'R' ELSE 'U'  END) EMPLFLAG,   " & _
             "                             A.MANAGER_LEVEL,A.EMPL_CLASS,A.GRADE,A.DEPTID,A.PERIODMASTID,a.payorgcode    " & _
             "                    FROM JOB_EE A LEFT OUTER JOIN COMPENSATION_TBL B ON A.EMPLID = B.EMPLID AND A.EMPL_RCD = B.EMPL_RCD AND A.EFFDT = B.EFFDT  AND A.EFFSEQ = B.EFFSEQ     " & _
             "                    WHERE A.EFFDT = (SELECT MAX(JT.EFFDT) FROM JOB_EE JT WHERE JT.EMPLID = A.EMPLID " & _
             "                          AND JT.EMPL_RCD = A.EMPL_RCD AND JT.EFFDT BETWEEN :C_STDATE AND :C_EDDATE ) AND A.EFFSEQ = (SELECT MAX(JT.EFFSEQ) FROM JOB_EE JT WHERE JT.EMPLID = A.EMPLID " & _
             "                          AND JT.EMPL_RCD = A.EMPL_RCD AND JT.EFFDT = A.EFFDT ) AND A.HR_STATUS = 'I' ) B " & _
             "  WHERE B.COMPANY = :C_COMP AND   B.EMPL_CLASS = NVL (:C_EMPLCLASS, B.EMPL_CLASS) " & _
             "  AND B.PAYORGCODE like NVL(:PAYORGCODE || '%',B.PAYORGCODE)  " & _
             "  AND B.EMPLID = NVL (:C_EMPLID, B.EMPLID) AND B.MANAGER_LEVEL = NVL(:MANAGERLVL,B.MANAGER_LEVEL) " & _
             "  AND NVL(B.PERIODMASTID,7) = DECODE(:PERIODMASTID,NULL,NVL(B.PERIODMASTID,7),:PERIODMASTID) {0}  ) M            " & _
             "                    LEFT OUTER JOIN INTERFACE_COMPEN N ON  M.EMPLID = N.EMPLID AND M.EMPL_RCD = N.EMPL_RCD AND M.EFFDT = N.EFFDT AND M.EFFSEQ = N.EFFSEQ AND M.COMP_RATECD = N.COMP_RATECD AND NVL(N.ISINTERFACE,'N') = 'N' " & _
             "  ,PERSON_TBL P " & _
             "  WHERE M.EMPLFLAG = NVL(:EMPLFLAG,M.EMPLFLAG) " & _
             "  AND M.EMPLID = P.EMPLID AND NVL(P.FOREIGNER,'N') = 'N' "
        '----------------------------------------------------------------------------------------------------------------

        Dim authenstr = String.Format("AND B.DEPTID IN ({0}) ", dpBp.StringAuthenDepartment(param.UserName, "B.DEPTID", col, Today))
        authenstr = Replace(authenstr, "A.MANAGER_LEVEL", "B.MANAGER_LEVEL")

        cmd.CommandText = String.Format(sqlText, authenstr)

        '-------------------------------------------------
        'Changed by Chanchira L. 19/04/2021
        sqlInsCompen = cmd.CommandText
        'Try
        '    If cmd.Connection.State = ConnectionState.Closed Then
        '        cmd.Connection.Open()
        '    End If
        '    Dim rdr1 As System.Data.Common.DbDataReader = cmd.ExecuteReader()
        '    While rdr1.Read
        '        Dim m As New Model.ModelInterfacetempCompen
        '        m.RetrieveFromDataReader(rdr1)
        '        dtoTempComp.Add(m)
        '    End While
        'Catch ex As Exception
        '    Throw ex
        'Finally
        '    con.Close()
        'End Try
        '-------------------------------------------------

    End Sub

    'Public Function GenerateTextInToServer(ByVal param As SsCommon.ServiceParam, ByVal _criteria As Model.ModelCriteriaList, ByVal formatdt As String, ByVal pathServer As String) As String
    <OperationContract()> _
    Public Function GenerateTextInToServer(ByVal param As SsCommon.ServiceParam, ByVal _criteria As Model.ModelCriteriaList, ByVal formatdt As String, ByVal pathServer As String, ByRef EmplidList As List(Of Model.ModelInterfacetempEe)) As String
        Dim ret As String = "Export text file Completed"
        Dim dpBp As New SsHrCommonService
        Dim col As String = "A.Grade"
        Dim dt As New DataTable
        Dim con As System.Data.Common.DbConnection = New OracleConnection(Util.GetOraConnectionString)
        Dim cmd As OracleCommand = con.CreateCommand
        cmd.BindByName = True
        Dim sqlText As String = String.Empty

        'Add by Chanchira L. on 15/10/2020 check all record or select item
        '--------------------------------------------------
        Dim Criteria_Emplid As String = ""
        If Not _criteria.chkAll Then
            Dim Emplid = (From y In EmplidList Where y.chk = True Order By y.Emplid Select y)
            For Each o In Emplid
                Criteria_Emplid += IIf(Criteria_Emplid <> "", ",", "") & "'" & o.Emplid & "'"
            Next
        Else
            Criteria_Emplid = If(String.IsNullOrEmpty(_criteria.Emplid), String.Empty, _criteria.Emplid)
        End If
        '--------------------------------------------------

        'sqlText = "SELECT * FROM(SELECT  A.EMPL_CLASS AS T_TYP, A.EMPLFLAG AS T_FUNC, A.COMPANY AS T_COM, A.PAYROLLID AS T_CODE,A.EMPL_TITLE AS T_TITLE,    " & _
        '            "        A.EMPL_NAME AS T_NAME,   " & _
        '            "        TO_CHAR(A.ADDRESS1) AS T_ADD1,   " & _
        '            "        TO_CHAR(A.ADDRESS2) AS T_ADD2,   " & _
        '            "          (select split_text(A.PAYORGCODE,2,'.') from dual) AS T_OPE,   " & _
        '            "          (select split_text(A.PAYORGCODE,3,'.') from dual) AS T_BRH,    " & _
        '            "          (select split_text(A.PAYORGCODE,4,'.') from dual) AS T_ORG,    " & _
        '            "          A.EMPL_SEX AS T_SEX, A.EMP_MARRYSTATUS AS MAR, A.EMPLID AS T_IDNO,    " & _
        '            "          A.STATUS AS T_STA,   " & _
        '            "        A.BIRTHDATE AS T_BDATE, A.HIRE_DT AS T_EDATE, A.PROBATION_DT AS T_FDATE, A.EXPECTED_END_DATE AS T_RDATE,A.GRADE AS T_PC, A.BANKCODE AS T_BNO,   " & _
        '            "        A.ACCOUNTID AS T_BTY, A.BANKACCOUNT AS T_BAC, A.CALTAXMETHOD AS T_TAXCALMETHOD,A.BANK_BRANCH AS T_BKBRNAME,b.COMP_RATECD,b.COMPENSATION_RATE, " & _
        '            "        A.HARMFUL,A.WORKHOUR,A.NID " & _
        '            " FROM INTERFACE_EE A LEFT OUTER JOIN INTERFACE_COMPEN B ON A.EMPLID = B.EMPLID AND A.EMPL_RCD = B.EMPL_RCD AND A.EFFDT = b.EFFDT AND A.EFFSEQ = B.EFFSEQ " & _
        '            "WHERE A.EMPL_CLASS = NVL (:C_EMPLCLASS, A.EMPL_CLASS) AND A.COMPANY = :C_COMP AND A.PAYORGCODE like NVL(:PAYORGCODE || '%',A.PAYORGCODE) AND A.EMPLID = NVL (:C_EMPLID, A.EMPLID) AND A.MANAGER_LEVEL = NVL(:MANAGERLVL,A.MANAGER_LEVEL)  " & _
        '            "  AND A.PERIODMASTID = :PERIODMASTID AND A.EFFDT BETWEEN :C_STDATE AND :C_EDDATE AND A.EMPLFLAG = (CASE WHEN :EMPLFLAG = 'A' THEN A.EMPLFLAG ELSE NVL(:EMPLFLAG,A.EMPLFLAG)END) {0})  " & _
        '            "PIVOT  (SUM(COMPENSATION_RATE) FOR (COMP_RATECD) IN ( 'AGSALY' AS T_SALY,'AGSPPS' AS T_PALW,'AGUPCY' AS T_SALW1,'AGMEAL' AS T_MALW1,'AGHARM' AS T_FALW1, " & _
        '            "                                                                  'AGTELE' AS T_TELW1,'NONE' AS T_OTHER, 'NONE' AS T_SPEC1, 'AGDMEL' AS T_MDED1,'NONE' AS T_ODED1, " & _
        '            "                                                                  'AGDLOA' AS T_SLDED,'AGDBGH' AS T_LLDED,'AGHSHP' AS T_HARW1,'AGRSDT' AS T_HOUW1,'AGSPIC' AS T_SPAW1, " & _
        '            "                                                                  'AGOTHC' AS T_OINC1, 'AGVHLE' AS T_VEHW1)) "
        'Changed by Chanchira L. on 15/10/2020 check all record or select item
        '--------------------------------------------------
        'sqlText = "SELECT T_TYP, T_FUNC, T_COM, T_CODE, T_TITLE, T_NAME, T_ADD1, T_ADD2, T_OPE, T_BRH, T_ORG, '' AS T_SHF, T_TAXID, T_SEX, MAR,'' AS T_CHILDTOTAL, " & _
        '            " '' AS T_CHILDSCHOLL, '' AS T_DEDINSURANCE, '' AS T_DEDHOMEINTEREST, '' AS T_DEDPROVIDENCE, '' AS	T_DEDDONATION, '' AS T_DEDDONATION2, " & _
        '            " T_IDNO, T_SCNO, T_STA, T_BDATE, T_EDATE, T_FDATE, '' AS T_TDATE, T_RDATE, '' AS T_POS, T_PC, T_BNO, T_BTY, T_BAC,T_TAXCALMETHOD, " & _
        '            " '' AS T_NOCALSOCIAL, '' AS T_DEDLTF, '' AS T_DEDRMF, ''	AS T_DEDFATHER, ''	AS T_FATHERID, ''	AS T_MOTHERID, '' AS T_TITLE_COUPLE, '' AS T_NAME_COUPLE, " & _
        '            " '' AS T_SURN_COUPLE, ''	AS T_BDATE_COUPLE, ''	AS T_ID_COUPLE, '' AS T_ID_COUPLE_FAT, '' AS T_ID_COUPLE_MOT, T_SALY, T_BONUSRATE, " & _
        '            " '' AS T_POROVIDEN_DDT, '' AS	T_DED_EMP_PER, '' AS T_DED_COM_PER, T_PALW, T_SALW1, T_MALW1, T_FALW1, T_TELW1, T_OTHER, T_SPEC1,'' AS T_HELP1, T_MDED1, " & _
        '            " T_ODED1,'' AS T_TRCOM, '' AS T_TRCODE, '' AS T_TROPE, '' AS	T_TRBRH, '' AS	T_TRORG, '' AS T_TRSHF, '' AS	T_TRDATE, T_SLDED,'' AS T_SLBAL, '' AS T_GSB, " & _
        '            " T_LLDED, '' AS T_LLBAL, T_HARW1, T_HOUW1, T_SPAW1, T_OINC1, T_VEHW1, '' AS T_PEMWF,	'' AS T_PCPWF, '' AS T_HEALTHY, '' AS T_PEMPV,	'' AS T_PCPPV, " & _
        '            " T_BKBRNAME, T_BKBRNAME,	'' AS T_YINC,	'' AS T_YAINC1,	'' AS T_YAINC2,	'' AS T_YAINC3,	'' AS T_YTAX,	'' AS T_YATAX1,	'' AS T_YATAX2,	'' AS T_YATAX3, " & _
        '            " '' AS T_YPALW, '' AS T_YSALW,	'' AS T_YMALW,	'' AS T_YFALW,	'' AS T_YHARW,	'' AS T_YHOUW,	'' AS T_YTELW,	'' AS T_YSPAW,	'' AS T_YOINC, " & _
        '            " '' AS T_YMDED, '' AS T_YODED,	'' AS T_YOT, '' AS T_YRW,	'' AS T_YLATE, '' AS T_YSLDED,	'' AS T_YLLDED,	'' AS T_YLVDED,	'' AS T_YADJ,	'' AS T_YEMWF, " & _
        '            " '' AS T_YCPWF, '' AS T_YEMPV, '' AS T_YCPPV, '' AS T_YHEALTHY, HARMFUL, WORKHOUR, NID, WORKDAY_MONTH " & _
        '            "    FROM(SELECT  A.EMPL_CLASS AS T_TYP, A.EMPLFLAG AS T_FUNC, A.COMPANY AS T_COM, A.PAYROLLID AS T_CODE,A.EMPL_TITLE AS T_TITLE,    " & _
        '            "        A.EMPL_NAME AS T_NAME, A.BONUS AS T_BONUSRATE,  " & _
        '            "        TO_CHAR(A.ADDRESS1) AS T_ADD1,   " & _
        '            "        TO_CHAR(A.ADDRESS2) AS T_ADD2,   " & _
        '            "          (select split_text(A.PAYORGCODE,2,'.') from dual) AS T_OPE,   " & _
        '            "          (select split_text(A.PAYORGCODE,3,'.') from dual) AS T_BRH,    " & _
        '            "          (select split_text(A.PAYORGCODE,4,'.') from dual) AS T_ORG,    " & _
        '            "          A.EMPL_SEX AS T_SEX, A.EMP_MARRYSTATUS AS MAR, " & _
        '            "          A.EMPLID AS T_IDNO, " & _
        '            "          A.STATUS AS T_STA,   " & _
        '            "        TO_CHAR(A.BIRTHDATE,'YYYYMMDD') AS T_BDATE, TO_CHAR(A.HIRE_DT,'YYYYMMDD') AS T_EDATE, TO_CHAR(A.PROBATION_DT,'YYYYMMDD') AS T_FDATE, TO_CHAR(A.TERMINATION_DT,'YYYYMMDD') AS T_RDATE, A.GRADE AS T_PC, A.BANKCODE AS T_BNO,   " & _
        '            "        A.ACCOUNTID AS T_BTY, A.BANKACCOUNT AS T_BAC, A.CALTAXMETHOD AS T_TAXCALMETHOD,A.BANK_BRANCH AS T_BKBRNAME,b.COMP_RATECD,b.COMPENSATION_RATE, " & _
        '            "        A.HARMFUL,A.WORKHOUR,A.NID,A.SCNO AS T_SCNO,A.TAXID AS T_TAXID, A.WORKDAY_MONTH " & _
        '            " FROM INTERFACETEMP_EE A LEFT OUTER JOIN INTERFACETEMP_COMPEN B ON A.EMPLID = B.EMPLID AND A.EMPL_RCD = B.EMPL_RCD AND A.EFFDT = b.EFFDT AND A.EFFSEQ = B.EFFSEQ " & _
        '            "WHERE A.EMPL_CLASS = NVL (:C_EMPLCLASS, A.EMPL_CLASS) AND A.COMPANY = :C_COMP " & _
        '            "AND A.PAYORGCODE like NVL(:PAYORGCODE || '%',A.PAYORGCODE) " & _
        '            "AND A.EMPLID = NVL (:C_EMPLID, A.EMPLID) AND A.MANAGER_LEVEL = NVL(:MANAGERLVL,A.MANAGER_LEVEL)  " & _
        '            "  AND A.PERIODMASTID = :PERIODMASTID AND A.EMPLFLAG = (CASE WHEN :EMPLFLAG = 'A' THEN A.EMPLFLAG ELSE NVL(:EMPLFLAG,A.EMPLFLAG)END) {0})  " & _
        '            "PIVOT  (SUM(COMPENSATION_RATE) FOR (COMP_RATECD) IN ( 'AGSALY' AS T_SALY,'AGSPPS' AS T_PALW,'AGUPCY' AS T_SALW1,'AGMEAL' AS T_MALW1,'AGHARM' AS T_FALW1, " & _
        '            "                                                                  'AGTELE' AS T_TELW1,'NONE' AS T_OTHER, 'NONE' AS T_SPEC1, 'AGDMEL' AS T_MDED1,'NONE' AS T_ODED1, " & _
        '            "                                                                  'AGDLOA' AS T_SLDED,'AGDBGH' AS T_LLDED,'AGHSHP' AS T_HARW1,'AGRSDT' AS T_HOUW1,'AGSPIC' AS T_SPAW1, " & _
        '            "                                                                  'AGOTHC' AS T_OINC1, 'AGVHLE' AS T_VEHW1)) "
        'Changed by Chanchira L. on 21/02/2021 ส่ง pre_company เพิ่ม
        'sqlText = "SELECT T_TYP, T_FUNC, T_COM, T_CODE, T_TITLE, T_NAME, T_ADD1, T_ADD2, T_OPE, T_BRH, T_ORG, '' AS T_SHF, T_TAXID, T_SEX, MAR,'' AS T_CHILDTOTAL, " & _
        '            " '' AS T_CHILDSCHOLL, '' AS T_DEDINSURANCE, '' AS T_DEDHOMEINTEREST, '' AS T_DEDPROVIDENCE, '' AS	T_DEDDONATION, '' AS T_DEDDONATION2, " & _
        '            " T_IDNO, T_SCNO, T_STA, T_BDATE, T_EDATE, T_FDATE, '' AS T_TDATE, T_RDATE, '' AS T_POS, T_PC, T_BNO, T_BTY, T_BAC,T_TAXCALMETHOD, " & _
        '            " '' AS T_NOCALSOCIAL, '' AS T_DEDLTF, '' AS T_DEDRMF, ''	AS T_DEDFATHER, ''	AS T_FATHERID, ''	AS T_MOTHERID, '' AS T_TITLE_COUPLE, '' AS T_NAME_COUPLE, " & _
        '            " '' AS T_SURN_COUPLE, ''	AS T_BDATE_COUPLE, ''	AS T_ID_COUPLE, '' AS T_ID_COUPLE_FAT, '' AS T_ID_COUPLE_MOT, T_SALY, T_BONUSRATE, " & _
        '            " '' AS T_POROVIDEN_DDT, '' AS	T_DED_EMP_PER, '' AS T_DED_COM_PER, T_PALW, T_SALW1, T_MALW1, T_FALW1, T_TELW1, T_OTHER, T_SPEC1,'' AS T_HELP1, T_MDED1, " & _
        '            " T_ODED1,'' AS T_TRCOM, '' AS T_TRCODE, '' AS T_TROPE, '' AS	T_TRBRH, '' AS	T_TRORG, '' AS T_TRSHF, '' AS	T_TRDATE, T_SLDED,'' AS T_SLBAL, '' AS T_GSB, " & _
        '            " T_LLDED, '' AS T_LLBAL, T_HARW1, T_HOUW1, T_SPAW1, T_OINC1, T_VEHW1, '' AS T_PEMWF,	'' AS T_PCPWF, '' AS T_HEALTHY, '' AS T_PEMPV,	'' AS T_PCPPV, " & _
        '            " T_BKBRNAME, T_BKBRNAME,	'' AS T_YINC,	'' AS T_YAINC1,	'' AS T_YAINC2,	'' AS T_YAINC3,	'' AS T_YTAX,	'' AS T_YATAX1,	'' AS T_YATAX2,	'' AS T_YATAX3, " & _
        '            " '' AS T_YPALW, '' AS T_YSALW,	'' AS T_YMALW,	'' AS T_YFALW,	'' AS T_YHARW,	'' AS T_YHOUW,	'' AS T_YTELW,	'' AS T_YSPAW,	'' AS T_YOINC, " & _
        '            " '' AS T_YMDED, '' AS T_YODED,	'' AS T_YOT, '' AS T_YRW,	'' AS T_YLATE, '' AS T_YSLDED,	'' AS T_YLLDED,	'' AS T_YLVDED,	'' AS T_YADJ,	'' AS T_YEMWF, " & _
        '            " '' AS T_YCPWF, '' AS T_YEMPV, '' AS T_YCPPV, '' AS T_YHEALTHY, HARMFUL, WORKHOUR, NID, WORKDAY_MONTH " & _
        '            "    FROM(SELECT  A.EMPL_CLASS AS T_TYP, A.EMPLFLAG AS T_FUNC, A.COMPANY AS T_COM, A.PAYROLLID AS T_CODE,A.EMPL_TITLE AS T_TITLE,    " & _
        '            "        A.EMPL_NAME AS T_NAME, A.BONUS AS T_BONUSRATE,  " & _
        '            "        TO_CHAR(A.ADDRESS1) AS T_ADD1,   " & _
        '            "        TO_CHAR(A.ADDRESS2) AS T_ADD2,   " & _
        '            "          (select split_text(A.PAYORGCODE,2,'.') from dual) AS T_OPE,   " & _
        '            "          (select split_text(A.PAYORGCODE,3,'.') from dual) AS T_BRH,    " & _
        '            "          (select split_text(A.PAYORGCODE,4,'.') from dual) AS T_ORG,    " & _
        '            "          A.EMPL_SEX AS T_SEX, A.EMP_MARRYSTATUS AS MAR, " & _
        '            "          A.EMPLID AS T_IDNO, " & _
        '            "          A.STATUS AS T_STA,   " & _
        '            "        TO_CHAR(A.BIRTHDATE,'YYYYMMDD') AS T_BDATE, TO_CHAR(A.HIRE_DT,'YYYYMMDD') AS T_EDATE, TO_CHAR(A.PROBATION_DT,'YYYYMMDD') AS T_FDATE, TO_CHAR(A.TERMINATION_DT,'YYYYMMDD') AS T_RDATE, A.GRADE AS T_PC, A.BANKCODE AS T_BNO,   " & _
        '            "        A.ACCOUNTID AS T_BTY, A.BANKACCOUNT AS T_BAC, A.CALTAXMETHOD AS T_TAXCALMETHOD,A.BANK_BRANCH AS T_BKBRNAME,b.COMP_RATECD,b.COMPENSATION_RATE, " & _
        '            "        A.HARMFUL,A.WORKHOUR,A.NID,A.SCNO AS T_SCNO,A.TAXID AS T_TAXID, A.WORKDAY_MONTH " & _
        '            " FROM INTERFACETEMP_EE A LEFT OUTER JOIN INTERFACETEMP_COMPEN B ON A.EMPLID = B.EMPLID AND A.EMPL_RCD = B.EMPL_RCD AND A.EFFDT = b.EFFDT AND A.EFFSEQ = B.EFFSEQ " & _
        '            "WHERE A.EMPL_CLASS = NVL (:C_EMPLCLASS, A.EMPL_CLASS) AND A.COMPANY = :C_COMP " & _
        '            "AND A.PAYORGCODE like NVL(:PAYORGCODE || '%',A.PAYORGCODE) " & _
        '            "AND A.MANAGER_LEVEL = NVL(:MANAGERLVL,A.MANAGER_LEVEL)  " & _
        '            "  AND A.PERIODMASTID = :PERIODMASTID AND A.EMPLFLAG = (CASE WHEN :EMPLFLAG = 'A' THEN A.EMPLFLAG ELSE NVL(:EMPLFLAG,A.EMPLFLAG)END) {0}  "
        sqlText = "SELECT T_TYP, T_FUNC, T_COM, T_CODE, T_TITLE, T_NAME, T_ADD1, T_ADD2, T_OPE, T_BRH, T_ORG, '' AS T_SHF, T_TAXID, T_SEX, MAR,'' AS T_CHILDTOTAL, " & _
                     " '' AS T_CHILDSCHOLL, '' AS T_DEDINSURANCE, '' AS T_DEDHOMEINTEREST, '' AS T_DEDPROVIDENCE, '' AS	T_DEDDONATION, '' AS T_DEDDONATION2, " & _
                     " T_IDNO, T_SCNO, T_STA, T_BDATE, T_EDATE, T_FDATE, '' AS T_TDATE, T_RDATE, '' AS T_POS, T_PC, T_BNO, T_BTY, T_BAC,T_TAXCALMETHOD, " & _
                     " '' AS T_NOCALSOCIAL, '' AS T_DEDLTF, '' AS T_DEDRMF, ''	AS T_DEDFATHER, ''	AS T_FATHERID, ''	AS T_MOTHERID, '' AS T_TITLE_COUPLE, '' AS T_NAME_COUPLE, " & _
                     " '' AS T_SURN_COUPLE, ''	AS T_BDATE_COUPLE, ''	AS T_ID_COUPLE, '' AS T_ID_COUPLE_FAT, '' AS T_ID_COUPLE_MOT, T_SALY, T_BONUSRATE, " & _
                     " '' AS T_POROVIDEN_DDT, '' AS	T_DED_EMP_PER, '' AS T_DED_COM_PER, T_PALW, T_SALW1, T_MALW1, T_FALW1, T_TELW1, T_OTHER, T_SPEC1,'' AS T_HELP1, T_MDED1, " & _
                     " T_ODED1,'' AS T_TRCOM, '' AS T_TRCODE, '' AS T_TROPE, '' AS	T_TRBRH, '' AS	T_TRORG, '' AS T_TRSHF, '' AS	T_TRDATE, T_SLDED,'' AS T_SLBAL, '' AS T_GSB, " & _
                     " T_LLDED, '' AS T_LLBAL, T_HARW1, T_HOUW1, T_SPAW1, T_OINC1, T_VEHW1, '' AS T_PEMWF,	'' AS T_PCPWF, '' AS T_HEALTHY, '' AS T_PEMPV,	'' AS T_PCPPV, " & _
                     " T_BKBRNAME, T_BKBRNAME,	'' AS T_YINC,	'' AS T_YAINC1,	'' AS T_YAINC2,	'' AS T_YAINC3,	'' AS T_YTAX,	'' AS T_YATAX1,	'' AS T_YATAX2,	'' AS T_YATAX3, " & _
                     " '' AS T_YPALW, '' AS T_YSALW,	'' AS T_YMALW,	'' AS T_YFALW,	'' AS T_YHARW,	'' AS T_YHOUW,	'' AS T_YTELW,	'' AS T_YSPAW,	'' AS T_YOINC, " & _
                     " '' AS T_YMDED, '' AS T_YODED,	'' AS T_YOT, '' AS T_YRW,	'' AS T_YLATE, '' AS T_YSLDED,	'' AS T_YLLDED,	'' AS T_YLVDED,	'' AS T_YADJ,	'' AS T_YEMWF, " & _
                     " '' AS T_YCPWF, '' AS T_YEMPV, '' AS T_YCPPV, '' AS T_YHEALTHY, HARMFUL, WORKHOUR, NID, WORKDAY_MONTH, PRE_COMPANY " & _
                     "    FROM(SELECT  A.EMPL_CLASS AS T_TYP, A.EMPLFLAG AS T_FUNC, A.COMPANY AS T_COM, A.PAYROLLID AS T_CODE,A.EMPL_TITLE AS T_TITLE,    " & _
                     "        A.EMPL_NAME AS T_NAME, A.BONUS AS T_BONUSRATE,  " & _
                     "        TO_CHAR(A.ADDRESS1) AS T_ADD1,   " & _
                     "        TO_CHAR(A.ADDRESS2) AS T_ADD2,   " & _
                     "          (select split_text(A.PAYORGCODE,2,'.') from dual) AS T_OPE,   " & _
                     "          (select split_text(A.PAYORGCODE,3,'.') from dual) AS T_BRH,    " & _
                     "          (select split_text(A.PAYORGCODE,4,'.') from dual) AS T_ORG,    " & _
                     "          A.EMPL_SEX AS T_SEX, A.EMP_MARRYSTATUS AS MAR, " & _
                     "          A.EMPLID AS T_IDNO, " & _
                     "          A.STATUS AS T_STA,   " & _
                     "        TO_CHAR(A.BIRTHDATE,'YYYYMMDD') AS T_BDATE, TO_CHAR(A.HIRE_DT,'YYYYMMDD') AS T_EDATE, TO_CHAR(A.PROBATION_DT,'YYYYMMDD') AS T_FDATE, TO_CHAR(A.TERMINATION_DT,'YYYYMMDD') AS T_RDATE, A.GRADE AS T_PC, A.BANKCODE AS T_BNO,   " & _
                     "        A.ACCOUNTID AS T_BTY, A.BANKACCOUNT AS T_BAC, A.CALTAXMETHOD AS T_TAXCALMETHOD,A.BANK_BRANCH AS T_BKBRNAME,b.COMP_RATECD,b.COMPENSATION_RATE, " & _
                     "        A.HARMFUL,A.WORKHOUR,A.NID,A.SCNO AS T_SCNO,A.TAXID AS T_TAXID, A.WORKDAY_MONTH, A.PRE_COMPANY " & _
                     " FROM INTERFACETEMP_EE A LEFT OUTER JOIN INTERFACETEMP_COMPEN B ON A.EMPLID = B.EMPLID AND A.EMPL_RCD = B.EMPL_RCD AND A.EFFDT = b.EFFDT AND A.EFFSEQ = B.EFFSEQ " & _
                     "WHERE A.EMPL_CLASS = NVL (:C_EMPLCLASS, A.EMPL_CLASS) AND A.COMPANY = :C_COMP " & _
                     "AND A.PAYORGCODE like NVL(:PAYORGCODE || '%',A.PAYORGCODE) " & _
                     "AND A.MANAGER_LEVEL = NVL(:MANAGERLVL,A.MANAGER_LEVEL)  " & _
                     "  AND A.PERIODMASTID = :PERIODMASTID AND A.EMPLFLAG = (CASE WHEN :EMPLFLAG = 'A' THEN A.EMPLFLAG ELSE NVL(:EMPLFLAG,A.EMPLFLAG)END) {0}  "
        If _criteria.chkAll Then
            sqlText += " AND A.EMPLID = NVL ('" & Criteria_Emplid & "', A.EMPLID) "
        Else
            sqlText += " AND A.EMPLID IN (" & Criteria_Emplid & ") "
        End If
        sqlText += ") PIVOT  (SUM(COMPENSATION_RATE) FOR (COMP_RATECD) IN ( 'AGSALY' AS T_SALY,'AGSPPS' AS T_PALW,'AGUPCY' AS T_SALW1,'AGMEAL' AS T_MALW1,'AGHARM' AS T_FALW1, " & _
                   "                                                                  'AGTELE' AS T_TELW1,'NONE' AS T_OTHER, 'NONE' AS T_SPEC1, 'AGDMEL' AS T_MDED1,'NONE' AS T_ODED1, " & _
                   "                                                                  'AGDLOA' AS T_SLDED,'AGDBGH' AS T_LLDED,'AGHSHP' AS T_HARW1,'AGRSDT' AS T_HOUW1,'AGSPIC' AS T_SPAW1, " & _
                   "                                                                  'AGOTHC' AS T_OINC1, 'AGVHLE' AS T_VEHW1)) "
        '--------------------------------------------------

        cmd.Parameters.Clear()

        'cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_STDATE", .Value = _criteria.StartDate})
        'cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_EDDATE", .Value = _criteria.EndDate})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_EMPLCLASS", .Value = If(String.IsNullOrEmpty(_criteria.EmplClass), "", _criteria.EmplClass)})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_COMP", .Value = _criteria.Company})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "PAYORGCODE", .Value = If(_criteria.Org Is Nothing, "", _criteria.Org.Relateid)})
        'Marked by Chanchira L. on 15/10/2020
        'cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_EMPLID", .Value = If(String.IsNullOrEmpty(_criteria.Emplid), "", _criteria.Emplid)})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "EMPLFLAG", .Value = _criteria.EmplFlag})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "MANAGERLVL", .Value = If(String.IsNullOrEmpty(_criteria.Managerlvl), "", _criteria.Managerlvl)})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "PERIODMASTID", .Value = _criteria.PeriodMastId.Periodmastid})
        'cmd.Parameters.Add(New OracleParameter With {.ParameterName = "LANG", .Value = param.Lang})
        'cmd.Parameters.Add(New OracleParameter With {.ParameterName = "LOCAL", .Value = param.LocalLang})

        Dim authenstr = String.Format("AND A.DEPTID IN ({0}) ", dpBp.StringAuthenDepartment(param.UserName, "A.DEPTID", col, Today))

        cmd.CommandText = String.Format(sqlText, authenstr)

        'Changed by Chanchira L. on 13/03/2020
        'เปลี่ยน update a.isinterface = 'Y'
        'Update  A.ISINTERFACE = 'N' In table INTERFACETEMP_EE
        Dim cmdEE As OracleCommand = con.CreateCommand
        'sqlText = "UPDATE INTERFACETEMP_EE A SET A.ISINTERFACE = 'N' " & _
        '          "WHERE A.ISINTERFACE = 'Y' AND A.EMPLFLAG = NVL (:EMPLFLAG, A.EMPLFLAG) AND A.MANAGER_LEVEL = NVL (:MANAGERLVL, A.MANAGER_LEVEL) AND A.EMPL_CLASS = NVL (:EMPLCLASS, A.EMPL_CLASS)   " & _
        '          "AND A.COMPANY = :COMP AND A.PAYORGCODE like NVL(:PAYORGCODE || '%',A.PAYORGCODE) AND A.EMPLID = NVL (:EMPLID, A.EMPLID) AND A.PERIODMASTID = :PERIODMASTID  {0} "

        'Changed by Chanchira L. on 15/10/2020 check all record or select item
        '--------------------------------------------
        'sqlText = "UPDATE INTERFACETEMP_EE A SET A.ISINTERFACE = 'Y' " & _
        '          "WHERE A.ISINTERFACE = 'N' AND A.EMPLFLAG = NVL (:EMPLFLAG, A.EMPLFLAG) AND A.MANAGER_LEVEL = NVL (:MANAGERLVL, A.MANAGER_LEVEL) AND A.EMPL_CLASS = NVL (:EMPLCLASS, A.EMPL_CLASS)   " & _
        '          "AND A.COMPANY = :COMP AND A.PAYORGCODE like NVL(:PAYORGCODE || '%',A.PAYORGCODE) AND A.EMPLID = NVL (:EMPLID, A.EMPLID) AND A.PERIODMASTID = :PERIODMASTID  {0} "
        sqlText = "UPDATE INTERFACETEMP_EE A SET A.ISINTERFACE = 'Y' " & _
                        "WHERE A.ISINTERFACE = 'N' AND A.EMPLFLAG = NVL (:EMPLFLAG, A.EMPLFLAG) AND A.MANAGER_LEVEL = NVL (:MANAGERLVL, A.MANAGER_LEVEL) AND A.EMPL_CLASS = NVL (:EMPLCLASS, A.EMPL_CLASS)   " & _
                        "AND A.COMPANY = :COMP AND A.PAYORGCODE like NVL(:PAYORGCODE || '%',A.PAYORGCODE) AND A.PERIODMASTID = :PERIODMASTID  {0} "
        If _criteria.chkAll Then
            sqlText += " AND A.EMPLID = NVL ('" & Criteria_Emplid & "', A.EMPLID) "
        Else
            sqlText += " AND A.EMPLID IN (" & Criteria_Emplid & ")"
        End If
        '--------------------------------------------

        cmdEE.Parameters.Clear()
        cmdEE.Parameters.Add(New OracleParameter With {.ParameterName = "EMPLFLAG", .Value = If(_criteria.EmplFlag = "A", "", _criteria.EmplFlag)})
        cmdEE.Parameters.Add(New OracleParameter With {.ParameterName = "MANAGERLVL", .Value = If(String.IsNullOrEmpty(_criteria.Managerlvl), String.Empty, _criteria.Managerlvl)})
        cmdEE.Parameters.Add(New OracleParameter With {.ParameterName = "EMPLCLASS", .Value = If(String.IsNullOrEmpty(_criteria.EmplClass), String.Empty, _criteria.EmplClass)})
        cmdEE.Parameters.Add(New OracleParameter With {.ParameterName = "COMP", .Value = _criteria.Company})
        cmdEE.Parameters.Add(New OracleParameter With {.ParameterName = "PAYORGCODE", .Value = If(_criteria.Org Is Nothing, "", _criteria.Org.Relateid)})
        'cmdEE.Parameters.Add(New OracleParameter With {.ParameterName = "C_STDATE", .Value = _criteria.StartDate})
        'cmdEE.Parameters.Add(New OracleParameter With {.ParameterName = "C_EDDATE", .Value = _criteria.EndDate})
        'Marked by Chanchira L. on 15/10/2020
        'cmdEE.Parameters.Add(New OracleParameter With {.ParameterName = "EMPLID", .Value = If(String.IsNullOrEmpty(_criteria.Emplid), String.Empty, _criteria.Emplid)})
        cmdEE.Parameters.Add(New OracleParameter With {.ParameterName = "PERIODMASTID", .Value = _criteria.PeriodMastId.Periodmastid})
        cmdEE.CommandText = String.Format(sqlText, authenstr)


        'Changed by Chanchira L. on 13/03/2020
        'เปลี่ยน update a.isinterface = 'Y'
        'Update  A.ISINTERFACE = 'N' In table INTERFACE_COMPEN 
        Dim cmdCompen As OracleCommand = con.CreateCommand
        'Changed by Chanchira L. on 15/10/2020 check all record or select item
        '------------------------------------------
        'sqlText = "UPDATE INTERFACETEMP_COMPEN A  SET A.ISINTERFACE = 'N'" & _
        '                  "WHERE  A.ISINTERFACE = 'Y' AND A.EMPLFLAG = NVL (:EMPLFLAG, A.EMPLFLAG) AND A.MANAGER_LEVEL = NVL (:MANAGERLVL, A.MANAGER_LEVEL) AND A.EMPL_CLASS = NVL (:EMPLCLASS, A.EMPL_CLASS)   " & _
        '                  "AND A.COMPANY = :COMP AND A.EMPLID = NVL (:EMPLID, A.EMPLID) AND A.PERIODMASTID = :PERIODMASTID {0} "

        sqlText = "UPDATE INTERFACETEMP_COMPEN A  SET A.ISINTERFACE = 'Y'" & _
                  "WHERE  A.ISINTERFACE = 'N' AND A.EMPLFLAG = NVL (:EMPLFLAG, A.EMPLFLAG) AND A.MANAGER_LEVEL = NVL (:MANAGERLVL, A.MANAGER_LEVEL) AND A.EMPL_CLASS = NVL (:EMPLCLASS, A.EMPL_CLASS)   " & _
                  "AND A.COMPANY = :COMP AND A.PERIODMASTID = :PERIODMASTID {0} "
        If _criteria.chkAll Then
            sqlText += " AND A.EMPLID = NVL ('" & Criteria_Emplid & "', A.EMPLID) "
        Else
            sqlText += " AND A.EMPLID IN (" & Criteria_Emplid & ")"
        End If
        '------------------------------------------

        cmdCompen.Parameters.Clear()
        cmdCompen.Parameters.Add(New OracleParameter With {.ParameterName = "EMPLFLAG", .Value = If(_criteria.EmplFlag = "A", "", _criteria.EmplFlag)})
        cmdCompen.Parameters.Add(New OracleParameter With {.ParameterName = "MANAGERLVL", .Value = If(String.IsNullOrEmpty(_criteria.Managerlvl), String.Empty, _criteria.Managerlvl)})
        cmdCompen.Parameters.Add(New OracleParameter With {.ParameterName = "EMPLCLASS", .Value = If(String.IsNullOrEmpty(_criteria.EmplClass), String.Empty, _criteria.EmplClass)})
        cmdCompen.Parameters.Add(New OracleParameter With {.ParameterName = "COMP", .Value = _criteria.Company})
        'cmdCompen.Parameters.Add(New OracleParameter With {.ParameterName = "C_STDATE", .Value = _criteria.StartDate})
        'cmdCompen.Parameters.Add(New OracleParameter With {.ParameterName = "C_EDDATE", .Value = _criteria.EndDate})
        'Marked by Chanchira L. on 15/10/2020
        'cmdCompen.Parameters.Add(New OracleParameter With {.ParameterName = "EMPLID", .Value = If(String.IsNullOrEmpty(_criteria.Emplid), String.Empty, _criteria.Emplid)})
        cmdCompen.Parameters.Add(New OracleParameter With {.ParameterName = "PERIODMASTID", .Value = _criteria.PeriodMastId.Periodmastid})
        cmdCompen.CommandText = String.Format(sqlText, authenstr)

        'Add by Chanchira L. on 13/03/2020
        'เพิ่ม update isinterface ที่ interface_ee, interface_compen
        Dim cmdInterfaceEE As OracleCommand = con.CreateCommand
        'Changed by Chanchira L. on 15/10/2020 check all record or select item
        '-----------------------------------------------
        'sqlText = "UPDATE INTERFACE_EE A SET A.ISINTERFACE = 'Y' " & _
        '          "WHERE A.ISINTERFACE = 'N' AND A.EMPLFLAG = NVL (:EMPLFLAG, A.EMPLFLAG) AND A.MANAGER_LEVEL = NVL (:MANAGERLVL, A.MANAGER_LEVEL) AND A.EMPL_CLASS = NVL (:EMPLCLASS, A.EMPL_CLASS)   " & _
        '          "AND A.COMPANY = :COMP AND A.PAYORGCODE like NVL(:PAYORGCODE || '%',A.PAYORGCODE) AND A.EMPLID = NVL (:EMPLID, A.EMPLID) AND A.PERIODMASTID = :PERIODMASTID  {0} "
        sqlText = "UPDATE INTERFACE_EE A SET A.ISINTERFACE = 'Y' " & _
                  "WHERE A.ISINTERFACE = 'N' AND A.EMPLFLAG = NVL (:EMPLFLAG, A.EMPLFLAG) AND A.MANAGER_LEVEL = NVL (:MANAGERLVL, A.MANAGER_LEVEL) AND A.EMPL_CLASS = NVL (:EMPLCLASS, A.EMPL_CLASS)   " & _
                  "AND A.COMPANY = :COMP AND A.PAYORGCODE like NVL(:PAYORGCODE || '%',A.PAYORGCODE) AND A.PERIODMASTID = :PERIODMASTID  {0} "
        If _criteria.chkAll Then
            sqlText += " AND A.EMPLID = NVL ('" & Criteria_Emplid & "', A.EMPLID) "
        Else
            sqlText += " AND A.EMPLID IN (" & Criteria_Emplid & ") "
        End If
        '-----------------------------------------------
        cmdInterfaceEE.Parameters.Clear()
        cmdInterfaceEE.Parameters.Add(New OracleParameter With {.ParameterName = "EMPLFLAG", .Value = If(_criteria.EmplFlag = "A", "", _criteria.EmplFlag)})
        cmdInterfaceEE.Parameters.Add(New OracleParameter With {.ParameterName = "MANAGERLVL", .Value = If(String.IsNullOrEmpty(_criteria.Managerlvl), String.Empty, _criteria.Managerlvl)})
        cmdInterfaceEE.Parameters.Add(New OracleParameter With {.ParameterName = "EMPLCLASS", .Value = If(String.IsNullOrEmpty(_criteria.EmplClass), String.Empty, _criteria.EmplClass)})
        cmdInterfaceEE.Parameters.Add(New OracleParameter With {.ParameterName = "COMP", .Value = _criteria.Company})
        cmdInterfaceEE.Parameters.Add(New OracleParameter With {.ParameterName = "PAYORGCODE", .Value = If(_criteria.Org Is Nothing, "", _criteria.Org.Relateid)})
        'Marked by Chanchira L. on 15/10/2020
        'cmdInterfaceEE.Parameters.Add(New OracleParameter With {.ParameterName = "EMPLID", .Value = If(String.IsNullOrEmpty(_criteria.Emplid), String.Empty, _criteria.Emplid)})
        cmdInterfaceEE.Parameters.Add(New OracleParameter With {.ParameterName = "PERIODMASTID", .Value = _criteria.PeriodMastId.Periodmastid})
        cmdInterfaceEE.CommandText = String.Format(sqlText, authenstr)

        Dim cmdInterfaceCompen As OracleCommand = con.CreateCommand
        'Changed by Chanchira L. on 15/10/2020 check all record or select item
        '-----------------------------------------------
        'sqlText = "UPDATE INTERFACE_COMPEN B  SET B.ISINTERFACE = 'Y'" & _
        '  "WHERE  B.ISINTERFACE = 'N' " & _
        '  "AND EXISTS " & _
        '  "(SELECT 1 FROM INTERFACETEMP_COMPEN A " & _
        '  " WHERE A.ISINTERFACE = 'N' AND A.EMPLFLAG = NVL (:EMPLFLAG, A.EMPLFLAG) " & _
        '  " AND A.MANAGER_LEVEL = NVL (:MANAGERLVL, A.MANAGER_LEVEL) AND A.EMPL_CLASS = NVL (:EMPLCLASS, A.EMPL_CLASS)   " & _
        '          "AND A.COMPANY = :COMP AND A.EMPLID = NVL (:EMPLID, A.EMPLID) AND A.PERIODMASTID = :PERIODMASTID {0} )"
        sqlText = "UPDATE INTERFACE_COMPEN B  SET B.ISINTERFACE = 'Y'" & _
                  "WHERE  B.ISINTERFACE = 'N' " & _
                  "AND EXISTS " & _
                  "(SELECT 1 FROM INTERFACETEMP_COMPEN A " & _
                  " WHERE A.ISINTERFACE = 'N' AND A.EMPLFLAG = NVL (:EMPLFLAG, A.EMPLFLAG) " & _
                  " AND A.MANAGER_LEVEL = NVL (:MANAGERLVL, A.MANAGER_LEVEL) AND A.EMPL_CLASS = NVL (:EMPLCLASS, A.EMPL_CLASS)   " & _
                  " AND A.COMPANY = :COMP AND A.PERIODMASTID = :PERIODMASTID {0}  "
        If _criteria.chkAll Then
            sqlText += " AND A.EMPLID = NVL ('" & Criteria_Emplid & "', A.EMPLID) "
        Else
            sqlText += " AND A.EMPLID IN (" & Criteria_Emplid & ")"
        End If
        sqlText += " ) "
        '-----------------------------------------------

        cmdInterfaceCompen.Parameters.Clear()
        cmdInterfaceCompen.Parameters.Add(New OracleParameter With {.ParameterName = "EMPLFLAG", .Value = If(_criteria.EmplFlag = "A", "", _criteria.EmplFlag)})
        cmdInterfaceCompen.Parameters.Add(New OracleParameter With {.ParameterName = "MANAGERLVL", .Value = If(String.IsNullOrEmpty(_criteria.Managerlvl), String.Empty, _criteria.Managerlvl)})
        cmdInterfaceCompen.Parameters.Add(New OracleParameter With {.ParameterName = "EMPLCLASS", .Value = If(String.IsNullOrEmpty(_criteria.EmplClass), String.Empty, _criteria.EmplClass)})
        cmdInterfaceCompen.Parameters.Add(New OracleParameter With {.ParameterName = "COMP", .Value = _criteria.Company})
        'Marked by Chanchira L. on 15/10/2020
        'cmdInterfaceCompen.Parameters.Add(New OracleParameter With {.ParameterName = "EMPLID", .Value = If(String.IsNullOrEmpty(_criteria.Emplid), String.Empty, _criteria.Emplid)})
        cmdInterfaceCompen.Parameters.Add(New OracleParameter With {.ParameterName = "PERIODMASTID", .Value = _criteria.PeriodMastId.Periodmastid})
        cmdInterfaceCompen.CommandText = String.Format(sqlText, authenstr)

        Try

            If con.State = ConnectionState.Closed Then
                con.Open()
            End If

            Dim trn As System.Data.Common.DbTransaction = con.BeginTransaction()
            Try
                'Dim rdr As System.Data.Common.DbDataReader = cmd.ExecuteReader()

                'Do While (rdr.Read)
                '    Dim m As New Model.ModelInterfaceEE
                '     m.RetrieveFromDataReader(rdr)
                '    dt.Add(m)
                'Loop
                If TypeOf (dt) Is DataTable Then
                    Dim da As New OracleDataAdapter(cmd)
                    ' Dim aa As New DataTable
                    'da.Fill(aa)
                    da.Fill(dt)

                End If

                'Add by Chanchira L. on 13/03/2020
                '-----------------------------
                'Updatae A.ISINTERFACE = 'Y' In table INTERFACE_EE
                cmdInterfaceEE.Transaction = trn
                cmdInterfaceEE.ExecuteNonQuery()

                'Updatae A.ISINTERFACE = 'Y' In table INTERFACE_COMPEN
                cmdInterfaceCompen.Transaction = trn
                cmdInterfaceCompen.ExecuteNonQuery()
                '------------------------------

                'Update  A.ISINTERFACE = 'N' In table INTERFACETEMP_EE
                cmdEE.Transaction = trn
                cmdEE.ExecuteNonQuery()

                'Update  A.ISINTERFACE = 'N' In table INTERFACETEMP_COMPEN 
                cmdCompen.Transaction = trn
                cmdCompen.ExecuteNonQuery()

                trn.Commit()
            Catch ex As Exception
                trn.Rollback()
                Throw ex
            End Try
        Catch ex As Exception
            Throw ex
        Finally
            con.Close()
        End Try

        Dim nameFile As String = String.Empty

        '1/6/2016 Kitinan J. อ่าน Path จาก Confix FwInit
        Dim curPath As String = pathServer
        If curPath(curPath.Length - 1) <> "\" Then
            curPath = curPath & "\"
        End If

        ' Dim fileName As String = Me.GetType.Name.Replace("Service", "") & "_" & Guid.NewGuid.ToString '& ".prn"
        Dim fileName As String = ""

        If dt IsNot Nothing AndAlso dt.Rows.Count > 0 Then
            '' nameFile = "MS" & Period.Month & Period.Year & _cri.Company & ".prn"
            ''fileName = "MS" & Today.Day & Today.Month & Today.Year & _criteria.Company & _criteria.Org.Relateid & ".prn"
            'If _criteria.Org IsNot Nothing AndAlso (_criteria.Org.Relateid IsNot Nothing OrElse Not String.IsNullOrEmpty(_criteria.Org.Relateid)) Then
            '    Dim op = _criteria.Org.Relateid.Split(".")
            '    fileName = "MS" & Today.ToString("dd") & Today.ToString("MM") & Today.ToString("yyyy") & _criteria.Company & op(1) & ".prn"
            'Else
            '    fileName = "MS" & Today.ToString("dd") & Today.ToString("MM") & Today.ToString("yyyy") & _criteria.Company & ".prn"
            'End If

            If _criteria.Org IsNot Nothing AndAlso (_criteria.Org.Relateid IsNot Nothing OrElse Not String.IsNullOrEmpty(_criteria.Org.Relateid)) Then
                Dim op = _criteria.Org.Relateid.Split(".")
                'fileName = "MS" & Today.ToString("dd") & Today.ToString("MM") & Today.ToString("yyyy") & _criteria.Company & op(1) & ".prn"
                'Payroll Vietnam 2019
                fileName = "MS" & _criteria.Month & Today.ToString("yyyy") & _criteria.Company & op(1) & ".prn"
            Else
                'fileName = "MS" & Today.ToString("dd") & Today.ToString("MM") & Today.ToString("yyyy") & _criteria.Company & ".prn"
                'Payroll Vietnam 2019
                fileName = "MS" & _criteria.Month & Today.ToString("yyyy") & _criteria.Company & ".prn"
            End If

            Dim dtData As New System.Text.StringBuilder

            Dim i = 1
            For Each r In dt.Rows
                Dim st As New System.Text.StringBuilder
                For Each c In dt.Columns
                    If IsDBNull(r(c.columnName).ToString.Trim) OrElse String.IsNullOrEmpty(r(c.columnName).ToString.Trim) Then
                        st.Append(";")
                    Else
                        If TypeOf (r(c.columnName)) Is Date Then
                            st.Append(String.Format(r(c.columnName).ToString.Trim, formatdt).Trim & ";")
                        ElseIf TypeOf (r(c.columnName)) Is Integer OrElse TypeOf (r(c.columnName)) Is Long OrElse TypeOf (r(c.columnName)) Is Double Then
                            st.Append(r(c.columnName).ToString("#0.00") & ";")
                        Else
                            st.Append(r(c).ToString & ";")
                        End If
                    End If
                Next
                Right(st.ToString, 1)

                If i = dt.Rows.Count Then
                    dtData.Append(st.ToString)
                Else
                    dtData.AppendLine(st.ToString)
                End If

                i += 1
            Next

            Dim oFile As System.IO.FileInfo
            oFile = New System.IO.FileInfo(curPath & fileName.ToString)

            ' For Each u In dtDataLst
            If System.IO.File.Exists(curPath & fileName.ToString) = False Then
                Try
                    Using writer As New System.IO.StreamWriter(curPath & fileName, True, System.Text.Encoding.UTF8)
                        writer.WriteLine(dtData.ToString)
                    End Using
                Catch ex As Exception
                    ret = "Export text file Uncompleted"
                End Try
            Else
                ret = "Payroll have not received text file yet"
            End If

            ' System.IO.File.Delete(curPath & fileName.ToString)

        Else
            ret = "No data found"
        End If

        Return ret

    End Function

    'Public Sub ExecuteShowData(ByVal param As SsCommon.ServiceParam, ByVal _cri As Model.ModelCriteriaList, ByRef ret As DataTable, ByRef headers As Dictionary(Of String, String))
    Public Sub ExecuteShowData(ByVal param As SsCommon.ServiceParam, ByVal _cri As Model.ModelCriteriaList, ByRef ret As DataTable, ByRef headers As Dictionary(Of String, String), ByRef EmplidList As List(Of Model.ModelInterfacetempEe))
        Dim con As System.Data.Common.DbConnection = New OracleConnection(Util.GetOraConnectionString)
        Dim cmd As OracleCommand = con.CreateCommand
        Dim ss As New SsHrCommon.SsHrCommonService
        cmd.BindByName = True
        Dim treeId = ss.GetCurrentTreeId(_cri.StartDate)
        Dim sqlText As String = ""
        Dim cri As String = String.Empty
        Dim cntSearch As New SsCommon.ServiceResult(Of Model.ModelCountRecord, Model.ModelInterfacetempEe)
        Dim dpBp As New SsHrCommonService
        Dim col As String = "A.Grade"

        'Add by Chanchira L. on 15/10/2020 check all record or select item
        '--------------------------------------------------
        Dim Criteria_Emplid As String = ""
        If Not _cri.chkAll Then
            Dim Emplid = (From y In EmplidList Where y.chk = True Order By y.Emplid Select y)
            For Each o In Emplid
                Criteria_Emplid += IIf(Criteria_Emplid <> "", ",", "") & "'" & o.Emplid & "'"
            Next
        Else
            Criteria_Emplid = If(String.IsNullOrEmpty(_cri.Emplid), String.Empty, _cri.Emplid)
        End If
        '--------------------------------------------------
        'Add by Chanchira L. on 19/04/2021  
        If _cri.EmplFlag = "A" Then
            _cri.EmplFlag = ""
        Else
            _cri.EmplFlag = _cri.EmplFlag
        End If
        '--------------------------------------------------

        Try
            If con.State = ConnectionState.Closed Then
                con.Open()
            End If

            'Changed by Chanchira L. on 18/03/2020
            'cntSearch.Result = CountSearch(param, con, _cri)

            'Changed by Chanchira L. 15/09/2020 เพิ่มเงื่อนไข nvl(person_tbl.foreigner,'N') = 'N'
            '---------------------------------------------------------------------------------
            'sqlText = "SELECT M.EMPLID, M.EMPL_RCD, M.EFFDT, M.EFFSEQ, M.ACTION, M.ACTION_DT, M.ACTION_REASON, M.PER_ORG, M.DEPTID, M.JOBCODE, M.POSITION_NBR, M.POSITION_LEVEL, M.REPORT_TO, M.POSN_OVRD, M.HR_STATUS, M.EMPL_STATUS, M.LOCATION, M.JOB_ENTRY_DT, M.DEPT_ENTRY_DT, M.POSITION_ENTRY_DT, M.POSNLEVEL_ENTRY_DT, M.SHIFT, M.REG_TEMP, M.FULL_PART_TIME, M.COMPANY, M.PAYGROUP, M.POITYPE, M.EMPLGROUP, M.EMPLIDCODE, M.HOLIDAY_SCHEDULE, M.STD_HOURS, M.STD_HRS_FREQUENCY, M.OFFICER_CD, M.EMPL_CLASS, M.GRADE, M.GRADE_ENTRY_DT, M.COMP_FREQUENCY, M.COMPRATE, M.CHANGE_AMT, M.CHANGE_PCT, M.CURRENCY_CD, M.BUSINESS_UNIT, M.SETID_DEPT, M.SETID_JOBCODE, M.HIRE_DT, M.LAST_HIRE_DT, M.TERMINATION_DT, M.ASGN_START_DT, M.LST_ASGN_START_DT, M.ASGN_END_DT, M.LAST_DATE_WORKED, M.EXPECTED_RETURN_DT, M.EXPECTED_END_DATE, M.PC_DATE_CPG, M.PROBATION_DT, M.PROBATION, M.PROBATION_TYPE, M.SOCIALWELF_PREFIX, M.CALSOCIALWELF, M.SOCIALWELFBEFYN, M.SOCIALWELFID, M.PERCENTSOCIALWELF, M.SOCIAL_BRANCH_CPG, M.ISCOMPSOCIALWELF, M.PAYORGCODE, M.PERIODMASTID, M.BONUS, M.CALTAXMETHOD, M.CCA_CPG, M.JOB_INDICATOR, M.JOBOPEN_NO, M.PAYROLLID, M.MANAGER_LEVEL, M.SAL_ADMIN_PLAN, M.EMPLFLAG, M.PRE_COMPANY, M.CREATEUSER, M.CREATEDATE, M.MODIFYDATE, M.PROGRAMCODE, ( CASE WHEN BISINTERFACE = 'Y' THEN 'Y' ELSE CASE WHEN PROBATION       <> BPROBATION OR PROBATION_TYPE    <> BPROBATION_TYPE OR SOCIALWELF_PREFIX <> BSOCIALWELF_PREFIX OR CALSOCIALWELF     <> BCALSOCIALWELF OR SOCIALWELFBEFYN   <> BSOCIALWELFBEFYN OR SOCIALWELFID      <> BSOCIALWELFID OR PERCENTSOCIALWELF <>BPERCENTSOCIALWELF OR SOCIAL_BRANCH_CPG <> BSOCIAL_BRANCH_CPG OR ISCOMPSOCIALWELF  <> BISCOMPSOCIALWELF OR PAYORGCODE        <> BPAYORGCODE OR PERIODMASTID      <> BPERIODMASTID OR BONUS             <> BBONUS OR CALTAXMETHOD      <> BCALTAXMETHOD OR CCA_CPG           <> BCCA_CPG OR PAYROLLID         <> BPAYROLLID OR (BEMPLFLAG        IS NOT NULL AND EMPLFLAG         <> BEMPLFLAG) THEN 'N' ELSE 'Y' END END) ISINTERFACE, RATEID, RATEQTY, AMOUNT, INCEXPCODE, INCEXPQTY, INCEXPAMT, M.EMPL_TITLE, M.EMPL_NAME, M.ADDRESS1, M.ADDRESS2, ( CASE WHEN M.EMPL_SEX = 'F' THEN '0' WHEN M.EMPL_SEX = 'M' THEN '1' END) AS EMPL_SEX, M.EMP_MARRYSTATUS, M.STATUS, M.BIRTHDATE, M.BANKCODE, M.ACCOUNTID, M.BANKACCOUNT, M.BANK_BRANCH, M.HARMFUL, M.WORKHOUR, M.NID, M.SCNO, M.TAXID, M.SAT_WORK FROM (SELECT A.*, ( CASE WHEN A.ACTION = 'HIR' AND A.EFFDT BETWEEN :C_STDATE AND :C_EDDATE THEN 'N' WHEN A.ACTION = 'LOA' AND A.EFFDT BETWEEN :C_STDATE AND :C_EDDATE THEN 'L' WHEN A.HR_STATUS = 'I' THEN 'R' ELSE 'U' END) EMPLFLAG, NVL ( (SELECT company FROM job_ee O WHERE O.emplid = a.emplid AND O.empl_rcd = a.empl_rcd AND O.effdt    = (SELECT MAX (t.effdt) FROM job_ee t WHERE t.emplid = O.emplid AND t.empl_rcd = O.empl_rcd AND t.effdt    < a.effdt ) AND O.EFFSEQ = (SELECT MAX (t.EFFSEQ) FROM job_ee t WHERE t.emplid = O.emplid AND t.empl_rcd = O.empl_rcd AND t.effdt    = a.effdt ) ), A.company) PRE_COMPANY, NVL (A.MODIFYDATE, A.CREATEDATE) DATEOFREOCRD, NVL(B.PROBATION,0) BPROBATION, NVL(B.PROBATION_TYPE,'') BPROBATION_TYPE , NVL(B.SOCIALWELF_PREFIX,'') BSOCIALWELF_PREFIX, NVL(B.CALSOCIALWELF,'') BCALSOCIALWELF, NVL(B.SOCIALWELFBEFYN,'') BSOCIALWELFBEFYN, NVL(B.SOCIALWELFID,'') BSOCIALWELFID, NVL(B.PERCENTSOCIALWELF,0) BPERCENTSOCIALWELF , NVL(B.SOCIAL_BRANCH_CPG,'') BSOCIAL_BRANCH_CPG, NVL(B.ISCOMPSOCIALWELF,'') BISCOMPSOCIALWELF, NVL(B.PAYORGCODE,'') BPAYORGCODE, NVL(B.PERIODMASTID,0) BPERIODMASTID, NVL(B.BONUS,0) BBONUS, NVL(B.CALTAXMETHOD,'') BCALTAXMETHOD, NVL(B.CCA_CPG,'') BCCA_CPG, NVL(B.PAYROLLID,'') BPAYROLLID, NVL(B.ISINTERFACE,'N' ) BISINTERFACE, B.EMPLFLAG AS BEMPLFLAG, (SELECT wm_concat (g.COMP_RATECD) FROM compensation_tbl g LEFT OUTER JOIN pyincomeexpense i ON G.COMP_RATECD = I.INCEXPCODE WHERE g.emplid   = a.emplid AND g.EMPL_RCD   = a.EMPL_RCD AND g.EFFDT      = a.EFFDT AND g.EFFSEQ     = a.EFFSEQ AND I.BASESALARY = 'Y' GROUP BY I.BASESALARY ) RATEID, (SELECT wm_concat (g.PAYQTY) FROM compensation_tbl g LEFT OUTER JOIN pyincomeexpense i ON G.COMP_RATECD = I.INCEXPCODE WHERE g.emplid   = a.emplid AND g.EMPL_RCD   = a.EMPL_RCD AND g.EFFDT      = a.EFFDT AND g.EFFSEQ     = a.EFFSEQ AND I.BASESALARY = 'Y' GROUP BY I.BASESALARY ) RATEQTY, (SELECT wm_concat (g.COMPENSATION_RATE) FROM compensation_tbl g LEFT OUTER JOIN pyincomeexpense i ON G.COMP_RATECD = I.INCEXPCODE WHERE g.emplid   = a.emplid AND g.EMPL_RCD   = a.EMPL_RCD AND g.EFFDT      = a.EFFDT AND g.EFFSEQ     = a.EFFSEQ AND I.BASESALARY = 'Y' GROUP BY I.BASESALARY ) AMOUNT, (SELECT wm_concat (g.COMP_RATECD) FROM compensation_tbl g LEFT OUTER JOIN pyincomeexpense i ON G.COMP_RATECD = I.INCEXPCODE WHERE g.emplid   = a.emplid AND g.EMPL_RCD   = a.EMPL_RCD AND g.EFFDT      = a.EFFDT AND g.EFFSEQ     = a.EFFSEQ AND I.BASESALARY = 'N' GROUP BY I.BASESALARY ) INCEXPCODE, (SELECT wm_concat (g.PAYQTY) FROM compensation_tbl g LEFT OUTER JOIN pyincomeexpense i ON G.COMP_RATECD = I.INCEXPCODE WHERE g.emplid   = a.emplid AND g.EMPL_RCD   = a.EMPL_RCD AND g.EFFDT      = a.EFFDT AND g.EFFSEQ     = a.EFFSEQ AND I.BASESALARY = 'N' GROUP BY I.BASESALARY ) INCEXPQTY, (SELECT wm_concat (g.COMPENSATION_RATE) FROM compensation_tbl g LEFT OUTER JOIN pyincomeexpense i ON G.COMP_RATECD = I.INCEXPCODE WHERE g.emplid   = a.emplid AND g.EMPL_RCD   = a.EMPL_RCD AND g.EFFDT      = a.EFFDT AND g.EFFSEQ     = a.EFFSEQ AND I.BASESALARY = 'N' GROUP BY I.BASESALARY ) INCEXPAMT, P.EMPLTITLE AS EMPL_TITLE, ( CASE WHEN :LANG = :LOCAL THEN P.EMPLFIRSTNAME || ' ' || P.EMPLLASTNAME ELSE P.EMPLENGFIRSTNAME || ' ' || P.EMPLENGLASTNAME END) EMPL_NAME, (P.BUILDING || ' ' || ( CASE WHEN P.ADDRESS IS NOT NULL THEN 'No. ' || P.ADDRESS ELSE '' END) || ' ' || P.STREET || ' ' || ( CASE WHEN P.TAMBOL IS NOT NULL THEN P.TAMBOL || ' Sub - District' ELSE '' END)) || ' ' ||( ( CASE WHEN P.AMPHUR IS NOT NULL THEN P.AMPHUR || ' District' ELSE '' END) || ' ' || (SELECT CASE WHEN :LANG = :LOCAL THEN GD.LOCALDESCRIPTION ELSE NVL (GD.ENGDESCRIPTION, GD.LOCALDESCRIPTION) END FROM PYGENERALDT GD WHERE GD.GDCODE = 'City' AND GD.DTCODE   = P.CITY ) || ' ' || (SELECT CASE WHEN :LANG = :LOCAL THEN GD.LOCALDESCRIPTION ELSE NVL (GD.ENGDESCRIPTION, GD.LOCALDESCRIPTION) END FROM PYGENERALDT GD WHERE GD.GDCODE = 'Province' AND GD.DTCODE   = P.STATE ) || ' ' || P.POSTAL || ' ' || (SELECT CASE WHEN :LANG = :LOCAL THEN GD.LOCALDESCRIPTION ELSE NVL (GD.ENGDESCRIPTION, GD.LOCALDESCRIPTION) END FROM PYGENERALDT GD WHERE GD.GDCODE = 'Country' AND GD.DTCODE   = P.COUNTRY )) AS ADDRESS1, (P.BUILDING || ' ' || ( CASE WHEN P.ADDRESS IS NOT NULL THEN 'No. ' || P.ADDRESS ELSE '' END) || ' ' || P.STREET || ' ' || ( CASE WHEN P.TAMBOL IS NOT NULL THEN P.TAMBOL || ' Sub - District' ELSE '' END)) || ' ' ||( ( CASE WHEN P.AMPHUR IS NOT NULL THEN P.AMPHUR || ' District' ELSE '' END) || ' ' || (SELECT CASE WHEN :LANG = :LOCAL THEN GD.LOCALDESCRIPTION ELSE NVL (GD.ENGDESCRIPTION, GD.LOCALDESCRIPTION) END FROM PYGENERALDT GD WHERE GD.GDCODE = 'City' AND GD.DTCODE   = P.CITY ) || ' ' || (SELECT CASE WHEN :LANG = :LOCAL THEN GD.LOCALDESCRIPTION ELSE NVL (GD.ENGDESCRIPTION, GD.LOCALDESCRIPTION) END FROM PYGENERALDT GD WHERE GD.GDCODE = 'Province' AND GD.DTCODE   = P.STATE ) || ' ' || P.POSTAL || ' ' || (SELECT CASE WHEN :LANG = :LOCAL THEN GD.LOCALDESCRIPTION ELSE NVL (GD.ENGDESCRIPTION, GD.LOCALDESCRIPTION) END FROM PYGENERALDT GD WHERE GD.GDCODE = 'Country' AND GD.DTCODE   = P.COUNTRY ))            AS ADDRESS2, P.SEX         AS EMPL_SEX, P.MARRYSTATUS AS EMP_MARRYSTATUS, (SELECT ( CASE WHEN AC.EMPL_STATUS = 'T' THEN 3 ELSE 1 END) FROM Action_Tbl AC WHERE AC.ACTION = A.ACTION AND AC.EFFDT    = (SELECT MAX(ACT.EFFDT) FROM Action_Tbl ACT WHERE ACT.ACTION = AC.ACTION ) )                 AS STATUS, P.BIRTHDATE       AS BIRTHDATE, PK.BANKCODE       AS BANKCODE, K.ACCOUNT_ID      AS ACCOUNTID, K.BANK_ACCOUNT    AS BANKACCOUNT, K.BANK_BRANCH     AS BANK_BRANCH, A.HARMFUL_PERCENT AS HARMFUL, (SELECT VALUE FROM FW_INIT WHERE PROGRAM_ID = 'N/A' AND KEY_NAME = (CASE WHEN A.SAT_WORK = 'Y' THEN 'SAT_WORK' ELSE 'SAT_NOWORK' END)) AS WORKHOUR, (SELECT PN.NATIONAL_ID FROM PERSON_NID PN WHERE A.EMPLID          = PN.EMPLID AND P.COUNTRY           = PN.COUNTRY AND PN.NATIONAL_ID_TYPE = 'PID' ) AS NID, (SELECT PN.NATIONAL_ID FROM PERSON_NID PN WHERE A.EMPLID          = PN.EMPLID AND P.COUNTRY           = PN.COUNTRY AND PN.NATIONAL_ID_TYPE = 'SSOID' ) AS SCNO, (SELECT PN.NATIONAL_ID FROM PERSON_NID PN WHERE A.EMPLID          = PN.EMPLID AND P.COUNTRY           = PN.COUNTRY AND PN.NATIONAL_ID_TYPE = 'TAXID' ) AS TAXID FROM ( SELECT A.* FROM JOB_EE A WHERE A.HR_STATUS = 'A' and a.empl_rcd = 0 AND A.EFFDT       = (SELECT MAX(JT.EFFDT) FROM JOB_EE JT WHERE JT.EMPLID = A.EMPLID AND JT.EMPL_RCD = A.EMPL_RCD AND JT.EFFDT   <= :C_EDDATE ) AND A.EFFSEQ = (SELECT MAX(JT.EFFSEQ) FROM JOB_EE JT WHERE JT.EMPLID = A.EMPLID AND JT.EMPL_RCD = A.EMPL_RCD AND JT.EFFDT    = A.EFFDT ) UNION ALL SELECT A.* FROM JOB_EE A WHERE A.HR_STATUS = 'I' AND A.EFFDT       = (SELECT MAX(JT.EFFDT) FROM JOB_EE JT WHERE JT.EMPLID = A.EMPLID AND JT.EMPL_RCD = A.EMPL_RCD AND JT.EFFDT BETWEEN :C_STDATE AND :C_EDDATE ) AND A.EFFSEQ = (SELECT MAX(JT.EFFSEQ) FROM JOB_EE JT WHERE JT.EMPLID = A.EMPLID AND JT.EMPL_RCD = A.EMPL_RCD AND JT.EFFDT    = A.EFFDT ) ) A LEFT OUTER JOIN INTERFACE_EE B ON A.EMPLID    = B.EMPLID AND A.EMPL_RCD = B.EMPL_RCD AND A.EFFDT    = B.EFFDT AND A.EFFSEQ   = B.EFFSEQ AND NVL(B.ISINTERFACE,'N') = 'N' LEFT OUTER JOIN PERSON_TBL P ON A.EMPLID = P.EMPLID LEFT OUTER JOIN PERS_BANK K ON A.EMPLID      = K.EMPLID AND K.ACCOUNT_ID = 1 LEFT OUTER JOIN PYBANK PK ON K.BANK_CD      = PK.BANKID WHERE A.EMPL_CLASS = NVL (:C_EMPLCLASS, A.EMPL_CLASS) AND A.COMPANY      = :C_COMP AND A.PAYORGCODE LIKE NVL(:PAYORGCODE || '%',A.PAYORGCODE) AND A.EMPLID              = NVL (:C_EMPLID, A.EMPLID) AND A.MANAGER_LEVEL       = NVL(:MANAGERLVL,A.MANAGER_LEVEL) AND NVL(A.PERIODMASTID,7) = DECODE(:PERIODMASTID,NULL,NVL(A.PERIODMASTID,7),:PERIODMASTID) {0} ORDER BY A.EMPLID, A.EFFDT ) M WHERE M.EMPLFLAG = NVL(:EMPLFLAG,M.EMPLFLAG)"
            'Changed by Chanchira L. on 15/10/2020 check all record or select item
            'sqlText = "SELECT M.EMPLID, M.EMPL_RCD, M.EFFDT, M.EFFSEQ, M.ACTION, M.ACTION_DT, M.ACTION_REASON, M.PER_ORG, M.DEPTID, M.JOBCODE, M.POSITION_NBR, M.POSITION_LEVEL, M.REPORT_TO, M.POSN_OVRD, M.HR_STATUS, M.EMPL_STATUS, M.LOCATION, M.JOB_ENTRY_DT, M.DEPT_ENTRY_DT, M.POSITION_ENTRY_DT, M.POSNLEVEL_ENTRY_DT, M.SHIFT, M.REG_TEMP, M.FULL_PART_TIME, M.COMPANY, M.PAYGROUP, M.POITYPE, M.EMPLGROUP, M.EMPLIDCODE, M.HOLIDAY_SCHEDULE, M.STD_HOURS, M.STD_HRS_FREQUENCY, M.OFFICER_CD, M.EMPL_CLASS, M.GRADE, M.GRADE_ENTRY_DT, M.COMP_FREQUENCY, M.COMPRATE, M.CHANGE_AMT, M.CHANGE_PCT, M.CURRENCY_CD, M.BUSINESS_UNIT, M.SETID_DEPT, M.SETID_JOBCODE, M.HIRE_DT, M.LAST_HIRE_DT, M.TERMINATION_DT, M.ASGN_START_DT, M.LST_ASGN_START_DT, M.ASGN_END_DT, M.LAST_DATE_WORKED, M.EXPECTED_RETURN_DT, M.EXPECTED_END_DATE, M.PC_DATE_CPG, M.PROBATION_DT, M.PROBATION, M.PROBATION_TYPE, M.SOCIALWELF_PREFIX, M.CALSOCIALWELF, M.SOCIALWELFBEFYN, M.SOCIALWELFID, M.PERCENTSOCIALWELF, M.SOCIAL_BRANCH_CPG, M.ISCOMPSOCIALWELF, M.PAYORGCODE, M.PERIODMASTID, M.BONUS, M.CALTAXMETHOD, M.CCA_CPG, M.JOB_INDICATOR, M.JOBOPEN_NO, M.PAYROLLID, M.MANAGER_LEVEL, M.SAL_ADMIN_PLAN, M.EMPLFLAG, M.PRE_COMPANY, M.CREATEUSER, M.CREATEDATE, M.MODIFYDATE, M.PROGRAMCODE, ( CASE WHEN BISINTERFACE = 'Y' THEN 'Y' ELSE CASE WHEN PROBATION       <> BPROBATION OR PROBATION_TYPE    <> BPROBATION_TYPE OR SOCIALWELF_PREFIX <> BSOCIALWELF_PREFIX OR CALSOCIALWELF     <> BCALSOCIALWELF OR SOCIALWELFBEFYN   <> BSOCIALWELFBEFYN OR SOCIALWELFID      <> BSOCIALWELFID OR PERCENTSOCIALWELF <>BPERCENTSOCIALWELF OR SOCIAL_BRANCH_CPG <> BSOCIAL_BRANCH_CPG OR ISCOMPSOCIALWELF  <> BISCOMPSOCIALWELF OR PAYORGCODE        <> BPAYORGCODE OR PERIODMASTID      <> BPERIODMASTID OR BONUS             <> BBONUS OR CALTAXMETHOD      <> BCALTAXMETHOD OR CCA_CPG           <> BCCA_CPG OR PAYROLLID         <> BPAYROLLID OR (BEMPLFLAG        IS NOT NULL AND EMPLFLAG         <> BEMPLFLAG) THEN 'N' ELSE 'Y' END END) ISINTERFACE, RATEID, RATEQTY, AMOUNT, INCEXPCODE, INCEXPQTY, INCEXPAMT, M.EMPL_TITLE, M.EMPL_NAME, M.ADDRESS1, M.ADDRESS2, ( CASE WHEN M.EMPL_SEX = 'F' THEN '0' WHEN M.EMPL_SEX = 'M' THEN '1' END) AS EMPL_SEX, M.EMP_MARRYSTATUS, M.STATUS, M.BIRTHDATE, M.BANKCODE, M.ACCOUNTID, M.BANKACCOUNT, M.BANK_BRANCH, M.HARMFUL, M.WORKHOUR, M.NID, M.SCNO, M.TAXID, M.SAT_WORK FROM (SELECT A.*, ( CASE WHEN A.ACTION = 'HIR' AND A.EFFDT BETWEEN :C_STDATE AND :C_EDDATE THEN 'N' WHEN A.ACTION = 'LOA' AND A.EFFDT BETWEEN :C_STDATE AND :C_EDDATE THEN 'L' WHEN A.HR_STATUS = 'I' THEN 'R' ELSE 'U' END) EMPLFLAG, NVL ( (SELECT company FROM job_ee O WHERE O.emplid = a.emplid AND O.empl_rcd = a.empl_rcd AND O.effdt    = (SELECT MAX (t.effdt) FROM job_ee t WHERE t.emplid = O.emplid AND t.empl_rcd = O.empl_rcd AND t.effdt    < a.effdt ) AND O.EFFSEQ = (SELECT MAX (t.EFFSEQ) FROM job_ee t WHERE t.emplid = O.emplid AND t.empl_rcd = O.empl_rcd AND t.effdt    = a.effdt ) ), A.company) PRE_COMPANY, NVL (A.MODIFYDATE, A.CREATEDATE) DATEOFREOCRD, NVL(B.PROBATION,0) BPROBATION, NVL(B.PROBATION_TYPE,'') BPROBATION_TYPE , NVL(B.SOCIALWELF_PREFIX,'') BSOCIALWELF_PREFIX, NVL(B.CALSOCIALWELF,'') BCALSOCIALWELF, NVL(B.SOCIALWELFBEFYN,'') BSOCIALWELFBEFYN, NVL(B.SOCIALWELFID,'') BSOCIALWELFID, NVL(B.PERCENTSOCIALWELF,0) BPERCENTSOCIALWELF , NVL(B.SOCIAL_BRANCH_CPG,'') BSOCIAL_BRANCH_CPG, NVL(B.ISCOMPSOCIALWELF,'') BISCOMPSOCIALWELF, NVL(B.PAYORGCODE,'') BPAYORGCODE, NVL(B.PERIODMASTID,0) BPERIODMASTID, NVL(B.BONUS,0) BBONUS, NVL(B.CALTAXMETHOD,'') BCALTAXMETHOD, NVL(B.CCA_CPG,'') BCCA_CPG, NVL(B.PAYROLLID,'') BPAYROLLID, NVL(B.ISINTERFACE,'N' ) BISINTERFACE, B.EMPLFLAG AS BEMPLFLAG, (SELECT wm_concat (g.COMP_RATECD) FROM compensation_tbl g LEFT OUTER JOIN pyincomeexpense i ON G.COMP_RATECD = I.INCEXPCODE WHERE g.emplid   = a.emplid AND g.EMPL_RCD   = a.EMPL_RCD AND g.EFFDT      = a.EFFDT AND g.EFFSEQ     = a.EFFSEQ AND I.BASESALARY = 'Y' GROUP BY I.BASESALARY ) RATEID, (SELECT wm_concat (g.PAYQTY) FROM compensation_tbl g LEFT OUTER JOIN pyincomeexpense i ON G.COMP_RATECD = I.INCEXPCODE WHERE g.emplid   = a.emplid AND g.EMPL_RCD   = a.EMPL_RCD AND g.EFFDT      = a.EFFDT AND g.EFFSEQ     = a.EFFSEQ AND I.BASESALARY = 'Y' GROUP BY I.BASESALARY ) RATEQTY, (SELECT wm_concat (g.COMPENSATION_RATE) FROM compensation_tbl g LEFT OUTER JOIN pyincomeexpense i ON G.COMP_RATECD = I.INCEXPCODE WHERE g.emplid   = a.emplid AND g.EMPL_RCD   = a.EMPL_RCD AND g.EFFDT      = a.EFFDT AND g.EFFSEQ     = a.EFFSEQ AND I.BASESALARY = 'Y' GROUP BY I.BASESALARY ) AMOUNT, (SELECT wm_concat (g.COMP_RATECD) FROM compensation_tbl g LEFT OUTER JOIN pyincomeexpense i ON G.COMP_RATECD = I.INCEXPCODE WHERE g.emplid   = a.emplid AND g.EMPL_RCD   = a.EMPL_RCD AND g.EFFDT      = a.EFFDT AND g.EFFSEQ     = a.EFFSEQ AND I.BASESALARY = 'N' GROUP BY I.BASESALARY ) INCEXPCODE, (SELECT wm_concat (g.PAYQTY) FROM compensation_tbl g LEFT OUTER JOIN pyincomeexpense i ON G.COMP_RATECD = I.INCEXPCODE WHERE g.emplid   = a.emplid AND g.EMPL_RCD   = a.EMPL_RCD AND g.EFFDT      = a.EFFDT AND g.EFFSEQ     = a.EFFSEQ AND I.BASESALARY = 'N' GROUP BY I.BASESALARY ) INCEXPQTY, (SELECT wm_concat (g.COMPENSATION_RATE) FROM compensation_tbl g LEFT OUTER JOIN pyincomeexpense i ON G.COMP_RATECD = I.INCEXPCODE WHERE g.emplid   = a.emplid AND g.EMPL_RCD   = a.EMPL_RCD AND g.EFFDT      = a.EFFDT AND g.EFFSEQ     = a.EFFSEQ AND I.BASESALARY = 'N' GROUP BY I.BASESALARY ) INCEXPAMT, P.EMPLTITLE AS EMPL_TITLE, ( CASE WHEN :LANG = :LOCAL THEN P.EMPLFIRSTNAME || ' ' || P.EMPLLASTNAME ELSE P.EMPLENGFIRSTNAME || ' ' || P.EMPLENGLASTNAME END) EMPL_NAME, (P.BUILDING || ' ' || ( CASE WHEN P.ADDRESS IS NOT NULL THEN 'No. ' || P.ADDRESS ELSE '' END) || ' ' || P.STREET || ' ' || ( CASE WHEN P.TAMBOL IS NOT NULL THEN P.TAMBOL || ' Sub - District' ELSE '' END)) || ' ' ||( ( CASE WHEN P.AMPHUR IS NOT NULL THEN P.AMPHUR || ' District' ELSE '' END) || ' ' || (SELECT CASE WHEN :LANG = :LOCAL THEN GD.LOCALDESCRIPTION ELSE NVL (GD.ENGDESCRIPTION, GD.LOCALDESCRIPTION) END FROM PYGENERALDT GD WHERE GD.GDCODE = 'City' AND GD.DTCODE   = P.CITY ) || ' ' || (SELECT CASE WHEN :LANG = :LOCAL THEN GD.LOCALDESCRIPTION ELSE NVL (GD.ENGDESCRIPTION, GD.LOCALDESCRIPTION) END FROM PYGENERALDT GD WHERE GD.GDCODE = 'Province' AND GD.DTCODE   = P.STATE ) || ' ' || P.POSTAL || ' ' || (SELECT CASE WHEN :LANG = :LOCAL THEN GD.LOCALDESCRIPTION ELSE NVL (GD.ENGDESCRIPTION, GD.LOCALDESCRIPTION) END FROM PYGENERALDT GD WHERE GD.GDCODE = 'Country' AND GD.DTCODE   = P.COUNTRY )) AS ADDRESS1, (P.BUILDING || ' ' || ( CASE WHEN P.ADDRESS IS NOT NULL THEN 'No. ' || P.ADDRESS ELSE '' END) || ' ' || P.STREET || ' ' || ( CASE WHEN P.TAMBOL IS NOT NULL THEN P.TAMBOL || ' Sub - District' ELSE '' END)) || ' ' ||( ( CASE WHEN P.AMPHUR IS NOT NULL THEN P.AMPHUR || ' District' ELSE '' END) || ' ' || (SELECT CASE WHEN :LANG = :LOCAL THEN GD.LOCALDESCRIPTION ELSE NVL (GD.ENGDESCRIPTION, GD.LOCALDESCRIPTION) END FROM PYGENERALDT GD WHERE GD.GDCODE = 'City' AND GD.DTCODE   = P.CITY ) || ' ' || (SELECT CASE WHEN :LANG = :LOCAL THEN GD.LOCALDESCRIPTION ELSE NVL (GD.ENGDESCRIPTION, GD.LOCALDESCRIPTION) END FROM PYGENERALDT GD WHERE GD.GDCODE = 'Province' AND GD.DTCODE   = P.STATE ) || ' ' || P.POSTAL || ' ' || (SELECT CASE WHEN :LANG = :LOCAL THEN GD.LOCALDESCRIPTION ELSE NVL (GD.ENGDESCRIPTION, GD.LOCALDESCRIPTION) END FROM PYGENERALDT GD WHERE GD.GDCODE = 'Country' AND GD.DTCODE   = P.COUNTRY ))            AS ADDRESS2, P.SEX         AS EMPL_SEX, P.MARRYSTATUS AS EMP_MARRYSTATUS, (SELECT ( CASE WHEN AC.EMPL_STATUS = 'T' THEN 3 ELSE 1 END) FROM Action_Tbl AC WHERE AC.ACTION = A.ACTION AND AC.EFFDT    = (SELECT MAX(ACT.EFFDT) FROM Action_Tbl ACT WHERE ACT.ACTION = AC.ACTION ) )                 AS STATUS, P.BIRTHDATE       AS BIRTHDATE, PK.BANKCODE       AS BANKCODE, K.ACCOUNT_ID      AS ACCOUNTID, K.BANK_ACCOUNT    AS BANKACCOUNT, K.BANK_BRANCH     AS BANK_BRANCH, A.HARMFUL_PERCENT AS HARMFUL, (SELECT VALUE FROM FW_INIT WHERE PROGRAM_ID = 'N/A' AND KEY_NAME = (CASE WHEN A.SAT_WORK = 'Y' THEN 'SAT_WORK' ELSE 'SAT_NOWORK' END)) AS WORKHOUR, (SELECT PN.NATIONAL_ID FROM PERSON_NID PN WHERE A.EMPLID          = PN.EMPLID AND P.COUNTRY           = PN.COUNTRY AND PN.NATIONAL_ID_TYPE = 'PID' ) AS NID, (SELECT PN.NATIONAL_ID FROM PERSON_NID PN WHERE A.EMPLID          = PN.EMPLID AND P.COUNTRY           = PN.COUNTRY AND PN.NATIONAL_ID_TYPE = 'SSOID' ) AS SCNO, (SELECT PN.NATIONAL_ID FROM PERSON_NID PN WHERE A.EMPLID          = PN.EMPLID AND P.COUNTRY           = PN.COUNTRY AND PN.NATIONAL_ID_TYPE = 'TAXID' ) AS TAXID FROM ( SELECT A.* FROM JOB_EE A WHERE A.HR_STATUS = 'A' and a.empl_rcd = 0 AND A.EFFDT       = (SELECT MAX(JT.EFFDT) FROM JOB_EE JT WHERE JT.EMPLID = A.EMPLID AND JT.EMPL_RCD = A.EMPL_RCD AND JT.EFFDT   <= :C_EDDATE ) AND A.EFFSEQ = (SELECT MAX(JT.EFFSEQ) FROM JOB_EE JT WHERE JT.EMPLID = A.EMPLID AND JT.EMPL_RCD = A.EMPL_RCD AND JT.EFFDT    = A.EFFDT ) UNION ALL SELECT A.* FROM JOB_EE A WHERE A.HR_STATUS = 'I' AND A.EFFDT       = (SELECT MAX(JT.EFFDT) FROM JOB_EE JT WHERE JT.EMPLID = A.EMPLID AND JT.EMPL_RCD = A.EMPL_RCD AND JT.EFFDT BETWEEN :C_STDATE AND :C_EDDATE ) AND A.EFFSEQ = (SELECT MAX(JT.EFFSEQ) FROM JOB_EE JT WHERE JT.EMPLID = A.EMPLID AND JT.EMPL_RCD = A.EMPL_RCD AND JT.EFFDT    = A.EFFDT ) ) A LEFT OUTER JOIN INTERFACE_EE B ON A.EMPLID    = B.EMPLID AND A.EMPL_RCD = B.EMPL_RCD AND A.EFFDT    = B.EFFDT AND A.EFFSEQ   = B.EFFSEQ AND NVL(B.ISINTERFACE,'N') = 'N' LEFT OUTER JOIN PERSON_TBL P ON A.EMPLID = P.EMPLID LEFT OUTER JOIN PERS_BANK K ON A.EMPLID      = K.EMPLID AND K.ACCOUNT_ID = 1 LEFT OUTER JOIN PYBANK PK ON K.BANK_CD      = PK.BANKID WHERE A.EMPL_CLASS = NVL (:C_EMPLCLASS, A.EMPL_CLASS) AND A.COMPANY      = :C_COMP AND A.PAYORGCODE LIKE NVL(:PAYORGCODE || '%',A.PAYORGCODE) AND A.EMPLID              = NVL (:C_EMPLID, A.EMPLID) AND A.MANAGER_LEVEL       = NVL(:MANAGERLVL,A.MANAGER_LEVEL) AND NVL(A.PERIODMASTID,7) = DECODE(:PERIODMASTID,NULL,NVL(A.PERIODMASTID,7),:PERIODMASTID) AND NVL(P.FOREIGNER,'N') = 'N' {0} ORDER BY A.EMPLID, A.EFFDT ) M WHERE M.EMPLFLAG = NVL(:EMPLFLAG,M.EMPLFLAG)"
            sqlText = "SELECT M.EMPLID, M.EMPL_RCD, M.EFFDT, M.EFFSEQ, M.ACTION, M.ACTION_DT, M.ACTION_REASON, M.PER_ORG, M.DEPTID, M.JOBCODE, M.POSITION_NBR, M.POSITION_LEVEL, M.REPORT_TO, M.POSN_OVRD, M.HR_STATUS, M.EMPL_STATUS, M.LOCATION, M.JOB_ENTRY_DT, M.DEPT_ENTRY_DT, M.POSITION_ENTRY_DT, M.POSNLEVEL_ENTRY_DT, M.SHIFT, M.REG_TEMP, M.FULL_PART_TIME, M.COMPANY, M.PAYGROUP, M.POITYPE, M.EMPLGROUP, M.EMPLIDCODE, M.HOLIDAY_SCHEDULE, M.STD_HOURS, M.STD_HRS_FREQUENCY, M.OFFICER_CD, M.EMPL_CLASS, M.GRADE, M.GRADE_ENTRY_DT, M.COMP_FREQUENCY, M.COMPRATE, M.CHANGE_AMT, M.CHANGE_PCT, M.CURRENCY_CD, M.BUSINESS_UNIT, M.SETID_DEPT, M.SETID_JOBCODE, M.HIRE_DT, M.LAST_HIRE_DT, M.TERMINATION_DT, M.ASGN_START_DT, M.LST_ASGN_START_DT, M.ASGN_END_DT, M.LAST_DATE_WORKED, M.EXPECTED_RETURN_DT, M.EXPECTED_END_DATE, M.PC_DATE_CPG, M.PROBATION_DT, M.PROBATION, M.PROBATION_TYPE, M.SOCIALWELF_PREFIX, M.CALSOCIALWELF, M.SOCIALWELFBEFYN, M.SOCIALWELFID, M.PERCENTSOCIALWELF, M.SOCIAL_BRANCH_CPG, M.ISCOMPSOCIALWELF, M.PAYORGCODE, M.PERIODMASTID, M.BONUS, M.CALTAXMETHOD, M.CCA_CPG, M.JOB_INDICATOR, M.JOBOPEN_NO, M.PAYROLLID, M.MANAGER_LEVEL, M.SAL_ADMIN_PLAN, M.EMPLFLAG, M.PRE_COMPANY, M.CREATEUSER, M.CREATEDATE, M.MODIFYDATE, M.PROGRAMCODE, ( CASE WHEN BISINTERFACE = 'Y' THEN 'Y' ELSE CASE WHEN PROBATION       <> BPROBATION OR PROBATION_TYPE    <> BPROBATION_TYPE OR SOCIALWELF_PREFIX <> BSOCIALWELF_PREFIX OR CALSOCIALWELF     <> BCALSOCIALWELF OR SOCIALWELFBEFYN   <> BSOCIALWELFBEFYN OR SOCIALWELFID      <> BSOCIALWELFID OR PERCENTSOCIALWELF <>BPERCENTSOCIALWELF OR SOCIAL_BRANCH_CPG <> BSOCIAL_BRANCH_CPG OR ISCOMPSOCIALWELF  <> BISCOMPSOCIALWELF OR PAYORGCODE        <> BPAYORGCODE OR PERIODMASTID      <> BPERIODMASTID OR BONUS             <> BBONUS OR CALTAXMETHOD      <> BCALTAXMETHOD OR CCA_CPG           <> BCCA_CPG OR PAYROLLID         <> BPAYROLLID OR (BEMPLFLAG        IS NOT NULL AND EMPLFLAG         <> BEMPLFLAG) THEN 'N' ELSE 'Y' END END) ISINTERFACE, RATEID, RATEQTY, AMOUNT, INCEXPCODE, INCEXPQTY, INCEXPAMT, M.EMPL_TITLE, M.EMPL_NAME, M.ADDRESS1, M.ADDRESS2, ( CASE WHEN M.EMPL_SEX = 'F' THEN '0' WHEN M.EMPL_SEX = 'M' THEN '1' END) AS EMPL_SEX, M.EMP_MARRYSTATUS, M.STATUS, M.BIRTHDATE, M.BANKCODE, M.ACCOUNTID, M.BANKACCOUNT, M.BANK_BRANCH, M.HARMFUL, M.WORKHOUR, M.NID, M.SCNO, M.TAXID, M.SAT_WORK FROM (SELECT A.*, ( CASE WHEN A.ACTION = 'HIR' AND A.EFFDT BETWEEN :C_STDATE AND :C_EDDATE THEN 'N' WHEN A.ACTION = 'LOA' AND A.EFFDT BETWEEN :C_STDATE AND :C_EDDATE THEN 'L' WHEN A.HR_STATUS = 'I' THEN 'R' ELSE 'U' END) EMPLFLAG, NVL ( (SELECT company FROM job_ee O WHERE O.emplid = a.emplid AND O.empl_rcd = a.empl_rcd AND O.effdt    = (SELECT MAX (t.effdt) FROM job_ee t WHERE t.emplid = O.emplid AND t.empl_rcd = O.empl_rcd AND t.effdt    < a.effdt ) AND O.EFFSEQ = (SELECT MAX (t.EFFSEQ) FROM job_ee t WHERE t.emplid = O.emplid AND t.empl_rcd = O.empl_rcd AND t.effdt    = a.effdt ) ), A.company) PRE_COMPANY, NVL (A.MODIFYDATE, A.CREATEDATE) DATEOFREOCRD, NVL(B.PROBATION,0) BPROBATION, NVL(B.PROBATION_TYPE,'') BPROBATION_TYPE , NVL(B.SOCIALWELF_PREFIX,'') BSOCIALWELF_PREFIX, NVL(B.CALSOCIALWELF,'') BCALSOCIALWELF, NVL(B.SOCIALWELFBEFYN,'') BSOCIALWELFBEFYN, NVL(B.SOCIALWELFID,'') BSOCIALWELFID, NVL(B.PERCENTSOCIALWELF,0) BPERCENTSOCIALWELF , NVL(B.SOCIAL_BRANCH_CPG,'') BSOCIAL_BRANCH_CPG, NVL(B.ISCOMPSOCIALWELF,'') BISCOMPSOCIALWELF, NVL(B.PAYORGCODE,'') BPAYORGCODE, NVL(B.PERIODMASTID,0) BPERIODMASTID, NVL(B.BONUS,0) BBONUS, NVL(B.CALTAXMETHOD,'') BCALTAXMETHOD, NVL(B.CCA_CPG,'') BCCA_CPG, NVL(B.PAYROLLID,'') BPAYROLLID, NVL(B.ISINTERFACE,'N' ) BISINTERFACE, B.EMPLFLAG AS BEMPLFLAG, (SELECT wm_concat (g.COMP_RATECD) FROM compensation_tbl g LEFT OUTER JOIN pyincomeexpense i ON G.COMP_RATECD = I.INCEXPCODE WHERE g.emplid   = a.emplid AND g.EMPL_RCD   = a.EMPL_RCD AND g.EFFDT      = a.EFFDT AND g.EFFSEQ     = a.EFFSEQ AND I.BASESALARY = 'Y' GROUP BY I.BASESALARY ) RATEID, (SELECT wm_concat (g.PAYQTY) FROM compensation_tbl g LEFT OUTER JOIN pyincomeexpense i ON G.COMP_RATECD = I.INCEXPCODE WHERE g.emplid   = a.emplid AND g.EMPL_RCD   = a.EMPL_RCD AND g.EFFDT      = a.EFFDT AND g.EFFSEQ     = a.EFFSEQ AND I.BASESALARY = 'Y' GROUP BY I.BASESALARY ) RATEQTY, (SELECT wm_concat (g.COMPENSATION_RATE) FROM compensation_tbl g LEFT OUTER JOIN pyincomeexpense i ON G.COMP_RATECD = I.INCEXPCODE WHERE g.emplid   = a.emplid AND g.EMPL_RCD   = a.EMPL_RCD AND g.EFFDT      = a.EFFDT AND g.EFFSEQ     = a.EFFSEQ AND I.BASESALARY = 'Y' GROUP BY I.BASESALARY ) AMOUNT, (SELECT wm_concat (g.COMP_RATECD) FROM compensation_tbl g LEFT OUTER JOIN pyincomeexpense i ON G.COMP_RATECD = I.INCEXPCODE WHERE g.emplid   = a.emplid AND g.EMPL_RCD   = a.EMPL_RCD AND g.EFFDT      = a.EFFDT AND g.EFFSEQ     = a.EFFSEQ AND I.BASESALARY = 'N' GROUP BY I.BASESALARY ) INCEXPCODE, (SELECT wm_concat (g.PAYQTY) FROM compensation_tbl g LEFT OUTER JOIN pyincomeexpense i ON G.COMP_RATECD = I.INCEXPCODE WHERE g.emplid   = a.emplid AND g.EMPL_RCD   = a.EMPL_RCD AND g.EFFDT      = a.EFFDT AND g.EFFSEQ     = a.EFFSEQ AND I.BASESALARY = 'N' GROUP BY I.BASESALARY ) INCEXPQTY, (SELECT wm_concat (g.COMPENSATION_RATE) FROM compensation_tbl g LEFT OUTER JOIN pyincomeexpense i ON G.COMP_RATECD = I.INCEXPCODE WHERE g.emplid   = a.emplid AND g.EMPL_RCD   = a.EMPL_RCD AND g.EFFDT      = a.EFFDT AND g.EFFSEQ     = a.EFFSEQ AND I.BASESALARY = 'N' GROUP BY I.BASESALARY ) INCEXPAMT, P.EMPLTITLE AS EMPL_TITLE, ( CASE WHEN :LANG = :LOCAL THEN P.EMPLFIRSTNAME || ' ' || P.EMPLLASTNAME ELSE P.EMPLENGFIRSTNAME || ' ' || P.EMPLENGLASTNAME END) EMPL_NAME, (P.BUILDING || ' ' || ( CASE WHEN P.ADDRESS IS NOT NULL THEN 'No. ' || P.ADDRESS ELSE '' END) || ' ' || P.STREET || ' ' || ( CASE WHEN P.TAMBOL IS NOT NULL THEN P.TAMBOL || ' Sub - District' ELSE '' END)) || ' ' ||( ( CASE WHEN P.AMPHUR IS NOT NULL THEN P.AMPHUR || ' District' ELSE '' END) || ' ' || (SELECT CASE WHEN :LANG = :LOCAL THEN GD.LOCALDESCRIPTION ELSE NVL (GD.ENGDESCRIPTION, GD.LOCALDESCRIPTION) END FROM PYGENERALDT GD WHERE GD.GDCODE = 'City' AND GD.DTCODE   = P.CITY ) || ' ' || (SELECT CASE WHEN :LANG = :LOCAL THEN GD.LOCALDESCRIPTION ELSE NVL (GD.ENGDESCRIPTION, GD.LOCALDESCRIPTION) END FROM PYGENERALDT GD WHERE GD.GDCODE = 'Province' AND GD.DTCODE   = P.STATE ) || ' ' || P.POSTAL || ' ' || (SELECT CASE WHEN :LANG = :LOCAL THEN GD.LOCALDESCRIPTION ELSE NVL (GD.ENGDESCRIPTION, GD.LOCALDESCRIPTION) END FROM PYGENERALDT GD WHERE GD.GDCODE = 'Country' AND GD.DTCODE   = P.COUNTRY )) AS ADDRESS1, (P.BUILDING || ' ' || ( CASE WHEN P.ADDRESS IS NOT NULL THEN 'No. ' || P.ADDRESS ELSE '' END) || ' ' || P.STREET || ' ' || ( CASE WHEN P.TAMBOL IS NOT NULL THEN P.TAMBOL || ' Sub - District' ELSE '' END)) || ' ' ||( ( CASE WHEN P.AMPHUR IS NOT NULL THEN P.AMPHUR || ' District' ELSE '' END) || ' ' || (SELECT CASE WHEN :LANG = :LOCAL THEN GD.LOCALDESCRIPTION ELSE NVL (GD.ENGDESCRIPTION, GD.LOCALDESCRIPTION) END FROM PYGENERALDT GD WHERE GD.GDCODE = 'City' AND GD.DTCODE   = P.CITY ) || ' ' || (SELECT CASE WHEN :LANG = :LOCAL THEN GD.LOCALDESCRIPTION ELSE NVL (GD.ENGDESCRIPTION, GD.LOCALDESCRIPTION) END FROM PYGENERALDT GD WHERE GD.GDCODE = 'Province' AND GD.DTCODE   = P.STATE ) || ' ' || P.POSTAL || ' ' || (SELECT CASE WHEN :LANG = :LOCAL THEN GD.LOCALDESCRIPTION ELSE NVL (GD.ENGDESCRIPTION, GD.LOCALDESCRIPTION) END FROM PYGENERALDT GD WHERE GD.GDCODE = 'Country' AND GD.DTCODE   = P.COUNTRY ))            AS ADDRESS2, P.SEX         AS EMPL_SEX, P.MARRYSTATUS AS EMP_MARRYSTATUS, (SELECT ( CASE WHEN AC.EMPL_STATUS = 'T' THEN 3 ELSE 1 END) FROM Action_Tbl AC WHERE AC.ACTION = A.ACTION AND AC.EFFDT    = (SELECT MAX(ACT.EFFDT) FROM Action_Tbl ACT WHERE ACT.ACTION = AC.ACTION ) )                 AS STATUS, P.BIRTHDATE       AS BIRTHDATE, PK.BANKCODE       AS BANKCODE, K.ACCOUNT_ID      AS ACCOUNTID, K.BANK_ACCOUNT    AS BANKACCOUNT, K.BANK_BRANCH     AS BANK_BRANCH, A.HARMFUL_PERCENT AS HARMFUL, (SELECT VALUE FROM FW_INIT WHERE PROGRAM_ID = 'N/A' AND KEY_NAME = (CASE WHEN A.SAT_WORK = 'Y' THEN 'SAT_WORK' ELSE 'SAT_NOWORK' END)) AS WORKHOUR, (SELECT PN.NATIONAL_ID FROM PERSON_NID PN WHERE A.EMPLID          = PN.EMPLID AND P.COUNTRY           = PN.COUNTRY AND PN.NATIONAL_ID_TYPE = 'PID' ) AS NID, (SELECT PN.NATIONAL_ID FROM PERSON_NID PN WHERE A.EMPLID          = PN.EMPLID AND P.COUNTRY           = PN.COUNTRY AND PN.NATIONAL_ID_TYPE = 'SSOID' ) AS SCNO, (SELECT PN.NATIONAL_ID FROM PERSON_NID PN WHERE A.EMPLID          = PN.EMPLID AND P.COUNTRY           = PN.COUNTRY AND PN.NATIONAL_ID_TYPE = 'TAXID' ) AS TAXID FROM ( SELECT A.* FROM JOB_EE A WHERE A.HR_STATUS = 'A' and a.empl_rcd = 0 AND A.EFFDT       = (SELECT MAX(JT.EFFDT) FROM JOB_EE JT WHERE JT.EMPLID = A.EMPLID AND JT.EMPL_RCD = A.EMPL_RCD AND JT.EFFDT   <= :C_EDDATE ) AND A.EFFSEQ = (SELECT MAX(JT.EFFSEQ) FROM JOB_EE JT WHERE JT.EMPLID = A.EMPLID AND JT.EMPL_RCD = A.EMPL_RCD AND JT.EFFDT    = A.EFFDT ) UNION ALL SELECT A.* FROM JOB_EE A WHERE A.HR_STATUS = 'I' AND A.EFFDT       = (SELECT MAX(JT.EFFDT) FROM JOB_EE JT WHERE JT.EMPLID = A.EMPLID AND JT.EMPL_RCD = A.EMPL_RCD AND JT.EFFDT BETWEEN :C_STDATE AND :C_EDDATE ) AND A.EFFSEQ = (SELECT MAX(JT.EFFSEQ) FROM JOB_EE JT WHERE JT.EMPLID = A.EMPLID AND JT.EMPL_RCD = A.EMPL_RCD AND JT.EFFDT    = A.EFFDT ) ) A LEFT OUTER JOIN INTERFACE_EE B ON A.EMPLID    = B.EMPLID AND A.EMPL_RCD = B.EMPL_RCD AND A.EFFDT    = B.EFFDT AND A.EFFSEQ   = B.EFFSEQ AND NVL(B.ISINTERFACE,'N') = 'N' LEFT OUTER JOIN PERSON_TBL P ON A.EMPLID = P.EMPLID LEFT OUTER JOIN PERS_BANK K ON A.EMPLID      = K.EMPLID AND K.ACCOUNT_ID = 1 LEFT OUTER JOIN PYBANK PK ON K.BANK_CD      = PK.BANKID WHERE A.EMPL_CLASS = NVL (:C_EMPLCLASS, A.EMPL_CLASS) AND A.COMPANY      = :C_COMP AND A.PAYORGCODE LIKE NVL(:PAYORGCODE || '%',A.PAYORGCODE) AND A.MANAGER_LEVEL       = NVL(:MANAGERLVL,A.MANAGER_LEVEL) AND NVL(A.PERIODMASTID,7) = DECODE(:PERIODMASTID,NULL,NVL(A.PERIODMASTID,7),:PERIODMASTID) AND NVL(P.FOREIGNER,'N') = 'N' {0} "
            If _cri.chkAll Then
                sqlText += " AND A.EMPLID = NVL ('" & Criteria_Emplid & "', A.EMPLID) "
            Else
                sqlText += " AND A.EMPLID IN (" & Criteria_Emplid & ") "
            End If
            sqlText += " ORDER BY A.EMPLID, A.EFFDT ) M WHERE M.EMPLFLAG = NVL(:EMPLFLAG,M.EMPLFLAG)"
            '---------------------------------------------------------------------------------

            cmd.Parameters.Clear()

            cmd.Parameters.Add(New OracleParameter With {.ParameterName = "LANG", .Value = param.Lang})
            cmd.Parameters.Add(New OracleParameter With {.ParameterName = "LOCAL", .Value = param.LocalLang})
            cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_STDATE", .Value = _cri.StartDate})
            cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_EDDATE", .Value = _cri.EndDate})
            cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_EMPLCLASS", .Value = If(String.IsNullOrEmpty(_cri.EmplClass), "", _cri.EmplClass)})
            cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_COMP", .Value = _cri.Company})
            cmd.Parameters.Add(New OracleParameter With {.ParameterName = "PAYORGCODE", .Value = If(_cri.Org Is Nothing, "", _cri.Org.Relateid)})
            'Marked by Chanchira L. on 15/10/2020
            'cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_EMPLID", .Value = If(String.IsNullOrEmpty(_cri.Emplid), "", _cri.Emplid)})
            cmd.Parameters.Add(New OracleParameter With {.ParameterName = "EMPLFLAG", .Value = _cri.EmplFlag})
            cmd.Parameters.Add(New OracleParameter With {.ParameterName = "MANAGERLVL", .Value = If(String.IsNullOrEmpty(_cri.Managerlvl), "", _cri.Managerlvl)})
            cmd.Parameters.Add(New OracleParameter With {.ParameterName = "PERIODMASTID", .Value = _cri.PeriodMastId.Periodmastid})
            Dim authenstr = String.Format("AND A.DEPTID IN ({0}) ", dpBp.StringAuthenDepartment(param.UserName, "A.DEPTID", col, Today))

            cmd.CommandText = String.Format(sqlText, authenstr)


            'If cntSearch.Result.CntMaster > 0 Then

            'sqlText = "SELECT A.EMPLID,EMPL_RCD,EFFDT,EFFSEQ,ACTION,ACTION_DT,ACTION_REASON,A.PER_ORG,DEPTID,JOBCODE,POSITION_NBR,POSITION_LEVEL,REPORT_TO, " & _
            '                  "   POSN_OVRD,HR_STATUS,EMPL_STATUS,LOCATION,JOB_ENTRY_DT,DEPT_ENTRY_DT,POSITION_ENTRY_DT,POSNLEVEL_ENTRY_DT,SHIFT,REG_TEMP,FULL_PART_TIME,  " & _
            '                  "   COMPANY,PAYGROUP,POITYPE,EMPLGROUP,A.EMPLIDCODE,HOLIDAY_SCHEDULE,STD_HOURS,STD_HRS_FREQUENCY,OFFICER_CD,EMPL_CLASS,GRADE,GRADE_ENTRY_DT,COMP_FREQUENCY, " & _
            '                  "   COMPRATE,CHANGE_AMT,CHANGE_PCT,CURRENCY_CD,BUSINESS_UNIT,SETID_DEPT,SETID_JOBCODE,HIRE_DT,LAST_HIRE_DT,TERMINATION_DT,ASGN_START_DT,LST_ASGN_START_DT, " & _
            '                  "   ASGN_END_DT,LAST_DATE_WORKED,EXPECTED_RETURN_DT,EXPECTED_END_DATE,PC_DATE_CPG,PROBATION_DT,PROBATION,PROBATION_TYPE,B.SOCIALWELF_PREFIX,CALSOCIALWELF, " & _
            '                  "   SOCIALWELFBEFYN,SOCIALWELFID,PERCENTSOCIALWELF,SOCIAL_BRANCH_CPG,ISCOMPSOCIALWELF,PAYORGCODE,PERIODMASTID,BONUS,CALTAXMETHOD,CCA_CPG,JOB_INDICATOR,A.JOBOPEN_NO, " & _
            '                  "   PAYROLLID,MANAGER_LEVEL,SAL_ADMIN_PLAN,EMPLFLAG,ISINTERFACE,PRE_COMPANY,A.CREATEUSER,A.CREATEDATE,A.MODIFYDATE,A.PROGRAMCODE,REMARKS,RATEID,RATEQTY,AMOUNT,INCEXPCODE,INCEXPAMT,INCEXPQTY, " & _
            '                  "   ( CASE WHEN :LANG = :LOCAL THEN B.EMPLFIRSTNAME || ' ' || B.EMPLLASTNAME ELSE  B.EMPLENGFIRSTNAME || ' ' || B.EMPLENGLASTNAME END) EMPL_NAME, " & _
            '                  "   (SELECT CASE WHEN :LANG <> :LOCAL THEN NVL(Y.DESCR,X.DESCR) ELSE X.DESCR END FROM DEPARTMENT_TBL X LEFT OUTER JOIN DEPARTMENT_LANG Y  " & _
            '                  "                 ON X.DEPTID = Y.DEPTID AND X.SETID = Y.SETID AND X.EFFDT = Y.EFFDT AND Y.LANGUAGE_CD = :LANG WHERE X.DEPTID = A.DEPTID AND X.SETID = A.SETID_DEPT  " & _
            '                  "                 AND X.EFFDT = (SELECT MAX(Z.EFFDT) FROM DEPARTMENT_TBL Z WHERE Z.DEPTID = X.DEPTID AND Z.SETID = X.SETID AND Z.EFFDT <= A.EFFDT)  ) DEPT_NAME, " & _
            '                  "                  (SELECT CASE WHEN :LANG <> :LOCAL THEN NVL(Y.DESCR,X.DESCR) ELSE X.DESCR END FROM POSITION_TBL X LEFT OUTER JOIN POSITION_LANG Y  " & _
            '                  "                 ON X.POSITION_NBR = Y.POSITION_NBR AND X.EFFDT = Y.EFFDT AND Y.LANGUAGE_CD = :LANG WHERE X.POSITION_NBR = A.POSITION_NBR  " & _
            '                  "                 AND X.EFFDT = (SELECT MAX(Z.EFFDT) FROM POSITION_TBL Z WHERE Z.POSITION_NBR = X.POSITION_NBR AND Z.EFFDT <= A.EFFDT)  ) POSITION_NAME " & _
            '                  "FROM INTERFACETEMP_EE A ,PERSON_TBL B " & _
            '                  "WHERE A.ISINTERFACE = 'N'  AND A.EMPLID = B.EMPLID AND A.EMPL_CLASS = NVL (:C_EMPLCLASS, A.EMPL_CLASS) AND A.COMPANY = :C_COMP AND A.PAYORGCODE like NVL(:PAYORGCODE || '%',A.PAYORGCODE) AND A.EMPLID = NVL (:C_EMPLID, A.EMPLID) AND A.MANAGER_LEVEL = NVL(:MANAGERLVL,A.MANAGER_LEVEL)  " & _
            '                  "AND A.PERIODMASTID =  :PERIODMASTID  AND A.EMPLID = NVL(:C_EMPLID,A.EMPLID) AND A.EMPLFLAG = NVL(:EMPLFLAG,A.EMPLFLAG) AND A.EMPLFLAG <> 'D' {0} ORDER by EMPLID"

            'Dim authenstr = String.Format("AND A.DEPTID IN ({0}) ", dpBp.StringAuthenDepartment(param.UserName, "A.DEPTID", col, Today))

            'sqlText = String.Format(sqlText, authenstr)
            'sqlText = String.Format(sqlText, cri)

            'cmd.Parameters.Clear()
            'cmd.Parameters.Add(New OracleParameter With {.ParameterName = "LANG", .Value = param.Lang})
            'cmd.Parameters.Add(New OracleParameter With {.ParameterName = "LOCAL", .Value = param.LocalLang})
            'cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_EMPLCLASS", .Value = _cri.EmplClass})
            'cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_COMP", .Value = _cri.Company})
            'cmd.Parameters.Add(New OracleParameter With {.ParameterName = "PAYORGCODE", .Value = If(_cri.Org Is Nothing, "", _cri.Org.Relateid)})
            'cmd.Parameters.Add(New OracleParameter With {.ParameterName = "MANAGERLVL", .Value = If(String.IsNullOrEmpty(_cri.Managerlvl), "", _cri.Managerlvl)})
            ''cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_STDATE", .Value = _cri.StartDate})
            ''cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_EDDATE", .Value = _cri.EndDate})
            'cmd.Parameters.Add(New OracleParameter With {.ParameterName = "PERIODMASTID", .Value = _cri.PeriodMastId.Periodmastid})
            'cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_EMPLID", .Value = If(String.IsNullOrEmpty(_cri.Emplid), "", _cri.Emplid)})
            'cmd.Parameters.Add(New OracleParameter With {.ParameterName = "EMPLFLAG", .Value = _cri.EmplFlag})

            'sqlText = String.Format(sqlText, String.Empty)
            'cmd.CommandText = sqlText
            cmd.CommandType = CommandType.Text

            If TypeOf (ret) Is DataTable Then
                Dim da As New OracleDataAdapter(cmd)
                da.Fill(ret)
            End If
            'End If

        Catch ex As Exception
            Throw ex
        Finally
            con.Close()
        End Try
    End Sub

    '<OperationContract()> _
    'Public Function GenerateEmployeeForInterface(param As SsCommon.ServiceParam, _criteria As Model.ModelCriteriaList) As Boolean
    '    Dim status = True
    '    Dim dpBp As New SsHrCommonService
    '    Dim con As System.Data.Common.DbConnection = New OracleConnection(Util.GetOraConnectionString)
    '    Dim cmd As OracleCommand = con.CreateCommand
    '    cmd.BindByName = True
    '    authen = GetGdDetails("HRAUTHEN")
    '    Dim d As Date = Now
    '    Dim sqlText As String = String.Empty
    '    Dim col As String = "A.Grade"
    '    Dim authenstr = String.Format("AND A.DEPTID IN ({0}) ", dpBp.StringAuthenDepartment(param.UserName, "A.DEPTID", col, Today))

    '    If authen IsNot Nothing AndAlso authen.Count > 0 Then
    '        Dim val = (From x In authen Where x.Dtcode = "HR" Select x.Localdescription).FirstOrDefault
    '        If val = "M" Then
    '            col = "A.manager_level"
    '        End If
    '    End If

    '    If _criteria.EmplFlag = "A" Then
    '        _criteria.EmplFlag = ""
    '    Else
    '        _criteria.EmplFlag = _criteria.EmplFlag
    '    End If

    '    Try
    '        If cmd.Connection.State = ConnectionState.Closed Then
    '            cmd.Connection.Open()
    '        End If
    '        Dim trn As System.Data.Common.DbTransaction = con.BeginTransaction()
    '        Try
    '            'Delect
    '            sqlText = "Delete From INTERFACE_EE A " & _
    '                      "WHERE  1=1 {0}"
    '            cmd.Parameters.Clear()
    '            cmd.CommandText = String.Format(sqlText, authenstr)
    '            cmd.Transaction = trn
    '            cmd.ExecuteNonQuery()

    '            sqlText = "Delete From INTERFACE_COMPEN C " & _
    '                            "WHERE EXISTS (SELECT 1 FROM INTERFACE_COMPEN B ,JOB_EE A  WHERE B.EMPLID = A.EMPLID AND B.EMPL_RCD = A.EMPL_RCD AND B.EFFDT = A.EFFDT AND B.EFFSEQ = A.EFFSEQ " & _
    '                            "AND C.EMPLID = B.EMPLID AND C.EMPL_RCD = B.EMPL_RCD AND C.EFFDT = B.EFFDT AND C.EFFSEQ = B.EFFSEQ AND C.COMP_RATECD = B.COMP_RATECD " & _
    '                            "{0}) "
    '            cmd.Parameters.Clear()
    '            cmd.CommandText = String.Format(sqlText, authenstr)
    '            cmd.Transaction = trn
    '            cmd.ExecuteNonQuery()


    '            'Insert
    '            sqlText = "INSERT INTO INTERFACE_EE ( EMPLID, EMPL_RCD, EFFDT, EFFSEQ, ACTION, ACTION_DT, ACTION_REASON, PER_ORG, DEPTID, JOBCODE, POSITION_NBR, POSITION_LEVEL, REPORT_TO, POSN_OVRD, HR_STATUS, EMPL_STATUS, LOCATION, JOB_ENTRY_DT, DEPT_ENTRY_DT, POSITION_ENTRY_DT, POSNLEVEL_ENTRY_DT, SHIFT, REG_TEMP, FULL_PART_TIME, COMPANY, PAYGROUP, POITYPE, EMPLGROUP, EMPLIDCODE, HOLIDAY_SCHEDULE, STD_HOURS, STD_HRS_FREQUENCY, OFFICER_CD, EMPL_CLASS, GRADE, GRADE_ENTRY_DT, COMP_FREQUENCY, COMPRATE, CHANGE_AMT, CHANGE_PCT, CURRENCY_CD, BUSINESS_UNIT, SETID_DEPT, SETID_JOBCODE, HIRE_DT, LAST_HIRE_DT, TERMINATION_DT, ASGN_START_DT, LST_ASGN_START_DT, ASGN_END_DT, LAST_DATE_WORKED, EXPECTED_RETURN_DT, EXPECTED_END_DATE, PC_DATE_CPG, PROBATION_DT, PROBATION, PROBATION_TYPE, SOCIALWELF_PREFIX, CALSOCIALWELF, SOCIALWELFBEFYN, SOCIALWELFID, PERCENTSOCIALWELF, SOCIAL_BRANCH_CPG, ISCOMPSOCIALWELF, PAYORGCODE, PERIODMASTID, BONUS, CALTAXMETHOD, CCA_CPG, JOB_INDICATOR, JOBOPEN_NO, PAYROLLID, MANAGER_LEVEL, SAL_ADMIN_PLAN, EMPLFLAG, ISINTERFACE, PRE_COMPANY, CREATEUSER, CREATEDATE, MODIFYDATE, PROGRAMCODE, DATEOFINTERFACE, USERINTERFACE, EMPL_TITLE, EMPL_NAME, EMPL_SEX, EMP_MARRYSTATUS, STATUS, BIRTHDATE, BANKCODE, ACCOUNTID, BANKACCOUNT, BANK_BRANCH, HARMFUL, WORKHOUR, NID, ADDRESS1, ADDRESS2, SCNO, TAXID, WORKDAY_MONTH ) SELECT x.* FROM (SELECT j.EMPLID, j.EMPL_RCD, j.EFFDT, j.EFFSEQ, j.ACTION, j.ACTION_DT, j.ACTION_REASON, j.PER_ORG, j.DEPTID, j.JOBCODE, j.POSITION_NBR, j.POSITION_LEVEL, j.REPORT_TO, j.POSN_OVRD, j.HR_STATUS, j.EMPL_STATUS, j.LOCATION, j.JOB_ENTRY_DT, j.DEPT_ENTRY_DT, j.POSITION_ENTRY_DT, j.POSNLEVEL_ENTRY_DT, j.SHIFT, j.REG_TEMP, j.FULL_PART_TIME, j.COMPANY, j.PAYGROUP, j.POITYPE, j.EMPLGROUP, j.EMPLIDCODE, j.HOLIDAY_SCHEDULE, j.STD_HOURS, j.STD_HRS_FREQUENCY, j.OFFICER_CD, j.EMPL_CLASS, j.GRADE, j.GRADE_ENTRY_DT, j.COMP_FREQUENCY, (SELECT SUM(g.COMPENSATION_RATE) FROM compensation_tbl g WHERE g.emplid = J.emplid AND g.EMPL_RCD = J.EMPL_RCD AND g.EFFDT    = J.EFFDT AND g.EFFSEQ   = J.EFFSEQ ) AS COMPRATE, j.CHANGE_AMT, j.CHANGE_PCT, j.CURRENCY_CD, j.BUSINESS_UNIT, j.SETID_DEPT, j.SETID_JOBCODE, j.HIRE_DT, j.LAST_HIRE_DT, j.TERMINATION_DT, j.ASGN_START_DT, j.LST_ASGN_START_DT, j.ASGN_END_DT, j.LAST_DATE_WORKED, j.EXPECTED_RETURN_DT, j.EXPECTED_END_DATE, j.PC_DATE_CPG, j.PROBATION_DT, j.PROBATION, j.PROBATION_TYPE, j.SOCIALWELF_PREFIX, j.CALSOCIALWELF, j.SOCIALWELFBEFYN, j.SOCIALWELFID, j.PERCENTSOCIALWELF, j.SOCIAL_BRANCH_CPG, j.ISCOMPSOCIALWELF, j.PAYORGCODE, j.PERIODMASTID, j.BONUS, j.CALTAXMETHOD, j.CCA_CPG, j.JOB_INDICATOR, j.JOBOPEN_NO, j.PAYROLLID, j.MANAGER_LEVEL, j.SAL_ADMIN_PLAN, ( CASE WHEN j.ACTION = 'HIR' AND j.EFFDT BETWEEN :C_STDATE AND :C_EDDATE THEN 'N' WHEN j.ACTION = 'LOA' AND j.EFFDT BETWEEN :C_STDATE AND :C_EDDATE THEN 'L' WHEN j.HR_STATUS = 'I' THEN 'R' ELSE 'U' END) AS EMPLFLAG, 'Y'  AS ISINTERFACE, NVL ( (SELECT company FROM job_ee O WHERE O.emplid = j.emplid AND O.empl_rcd = j.empl_rcd AND O.effdt    = (SELECT MAX (t.effdt) FROM job_ee t WHERE t.emplid = O.emplid AND t.empl_rcd = O.empl_rcd AND t.effdt    < j.effdt ) AND O.EFFSEQ = (SELECT MAX (t.EFFSEQ) FROM job_ee t WHERE t.emplid = O.emplid AND t.empl_rcd = O.empl_rcd AND t.effdt    = j.effdt ) ), j.company) AS PRE_COMPANY, j.CREATEUSER, j.createdate, j.MODIFYDATE, :PROGRAMCODE, :DATEOFINTERFACE, :USERINTERFACE, P.EMPLTITLE AS EMPL_TITLE, ( CASE WHEN :LANG = :LOCAL THEN P.EMPLFIRSTNAME || ' ' || P.EMPLLASTNAME ELSE P.EMPLENGFIRSTNAME || ' ' || P.EMPLENGLASTNAME END) AS EMPL_NAME, ( CASE WHEN P.SEX = 'F' THEN '0' WHEN P.SEX = 'M' THEN '1' END)          AS EMPL_SEX, P.MARRYSTATUS AS EMP_MARRYSTATUS, (SELECT ( CASE WHEN AC.EMPL_STATUS = 'T' THEN 3 ELSE 1 END) FROM Action_Tbl AC WHERE AC.ACTION = j.ACTION AND AC.EFFDT    = (SELECT MAX(ACT.EFFDT) FROM Action_Tbl ACT WHERE ACT.ACTION = AC.ACTION ) )                 AS STATUS, P.BIRTHDATE       AS BIRTHDATE, PK.BANKCODE       AS BANKCODE, K.ACCOUNT_ID      AS ACCOUNTID, K.BANK_ACCOUNT    AS BANKACCOUNT, K.BANK_BRANCH     AS BANK_BRANCH, j.HARMFUL_PERCENT AS HARMFUL, j.WORKDAYHOUR     AS WORKHOUR, (SELECT PN.NATIONAL_ID FROM PERSON_NID PN WHERE j.EMPLID          = PN.EMPLID AND P.COUNTRY           = PN.COUNTRY AND PN.NATIONAL_ID_TYPE = 'PID' ) AS NID, (P.BUILDING || ' ' || ( CASE WHEN P.ADDRESS IS NOT NULL THEN 'No. ' || P.ADDRESS ELSE '' END) || ' ' || P.STREET || ' ' || ( CASE WHEN P.TAMBOL IS NOT NULL THEN P.TAMBOL || ' Sub - District' ELSE '' END)) || ' ' ||( ( CASE WHEN P.AMPHUR IS NOT NULL THEN P.AMPHUR || ' District' ELSE '' END) || ' ' || (SELECT CASE WHEN :LANG = :LOCAL THEN GD.LOCALDESCRIPTION ELSE NVL (GD.ENGDESCRIPTION, GD.LOCALDESCRIPTION) END FROM PYGENERALDT GD WHERE GD.GDCODE = 'City' AND GD.DTCODE   = P.CITY ) || ' ' || (SELECT CASE WHEN :LANG = :LOCAL THEN GD.LOCALDESCRIPTION ELSE NVL (GD.ENGDESCRIPTION, GD.LOCALDESCRIPTION) END FROM PYGENERALDT GD WHERE GD.GDCODE = 'Province' AND GD.DTCODE   = P.STATE ) || ' ' || P.POSTAL || ' ' || (SELECT CASE WHEN :LANG = :LOCAL THEN GD.LOCALDESCRIPTION ELSE NVL (GD.ENGDESCRIPTION, GD.LOCALDESCRIPTION) END FROM PYGENERALDT GD WHERE GD.GDCODE = 'Country' AND GD.DTCODE   = P.COUNTRY )) AS ADDRESS1, (P.BUILDING || ' ' || ( CASE WHEN P.ADDRESS IS NOT NULL THEN 'No. ' || P.ADDRESS ELSE '' END) || ' ' || P.STREET || ' ' || ( CASE WHEN P.TAMBOL IS NOT NULL THEN P.TAMBOL || ' Sub - District' ELSE '' END)) || ' ' ||( ( CASE WHEN P.AMPHUR IS NOT NULL THEN P.AMPHUR || ' District' ELSE '' END) || ' ' || (SELECT CASE WHEN :LANG = :LOCAL THEN GD.LOCALDESCRIPTION ELSE NVL (GD.ENGDESCRIPTION, GD.LOCALDESCRIPTION) END FROM PYGENERALDT GD WHERE GD.GDCODE = 'City' AND GD.DTCODE   = P.CITY ) || ' ' || (SELECT CASE WHEN :LANG = :LOCAL THEN GD.LOCALDESCRIPTION ELSE NVL (GD.ENGDESCRIPTION, GD.LOCALDESCRIPTION) END FROM PYGENERALDT GD WHERE GD.GDCODE = 'Province' AND GD.DTCODE   = P.STATE ) || ' ' || P.POSTAL || ' ' || (SELECT CASE WHEN :LANG = :LOCAL THEN GD.LOCALDESCRIPTION ELSE NVL (GD.ENGDESCRIPTION, GD.LOCALDESCRIPTION) END FROM PYGENERALDT GD WHERE GD.GDCODE = 'Country' AND GD.DTCODE   = P.COUNTRY )) AS ADDRESS2, (SELECT PN.NATIONAL_ID FROM PERSON_NID PN WHERE j.EMPLID          = PN.EMPLID AND P.COUNTRY           = PN.COUNTRY AND PN.NATIONAL_ID_TYPE = 'SSOID' ) AS SCNO, (SELECT PN.NATIONAL_ID FROM PERSON_NID PN WHERE j.EMPLID          = PN.EMPLID AND P.COUNTRY           = PN.COUNTRY AND PN.NATIONAL_ID_TYPE = 'TAXID' ) AS TAXID, ( CASE WHEN j.sat_work = 'Y' THEN c.SAT_WORKDAY ELSE c.nosat_workday END) AS WORKDAY_MONTH FROM job_ee j LEFT OUTER JOIN (SELECT c1.* FROM company_dtl c1 WHERE c1.effdt = (SELECT MAX(c2.effdt) FROM company_dtl c2 WHERE c2.company = c1.company AND c2.month     = c1.month ) ) c ON c.company = j.company AND c.month  = :C_MONTH LEFT OUTER JOIN PERS_BANK K ON j.EMPLID      = K.EMPLID AND K.ACCOUNT_ID = 1 LEFT OUTER JOIN PYBANK PK ON K.BANK_CD = PK.BANKID , person_tbl p WHERE j.effdt = (SELECT MAX(j1.effdt) FROM job_ee j1 WHERE j1.hr_status = 'A' AND j1.emplid      = j.emplid AND j1.effdt      <= :C_EDDATE ) AND j.effseq = (SELECT MAX(j1.effseq) FROM job_ee j1 WHERE j1.hr_status = 'A' AND j1.emplid      = j.emplid AND j1.effdt       = j.effdt ) AND j.hr_status = 'A' AND j.emplid    = p.emplid UNION ALL SELECT j.EMPLID, j.EMPL_RCD, j.EFFDT, j.EFFSEQ, j.ACTION, j.ACTION_DT, j.ACTION_REASON, j.PER_ORG, j.DEPTID, j.JOBCODE, j.POSITION_NBR, j.POSITION_LEVEL, j.REPORT_TO, j.POSN_OVRD, j.HR_STATUS, j.EMPL_STATUS, j.LOCATION, j.JOB_ENTRY_DT, j.DEPT_ENTRY_DT, j.POSITION_ENTRY_DT, j.POSNLEVEL_ENTRY_DT, j.SHIFT, j.REG_TEMP, j.FULL_PART_TIME, j.COMPANY, j.PAYGROUP, j.POITYPE, j.EMPLGROUP, j.EMPLIDCODE, j.HOLIDAY_SCHEDULE, j.STD_HOURS, j.STD_HRS_FREQUENCY, j.OFFICER_CD, j.EMPL_CLASS, j.GRADE, j.GRADE_ENTRY_DT, j.COMP_FREQUENCY, (SELECT SUM(g.COMPENSATION_RATE) FROM compensation_tbl g WHERE g.emplid = J.emplid AND g.EMPL_RCD = J.EMPL_RCD AND g.EFFDT    = J.EFFDT AND g.EFFSEQ   = J.EFFSEQ ) AS COMPRATE, j.CHANGE_AMT, j.CHANGE_PCT, j.CURRENCY_CD, j.BUSINESS_UNIT, j.SETID_DEPT, j.SETID_JOBCODE, j.HIRE_DT, j.LAST_HIRE_DT, j.TERMINATION_DT, j.ASGN_START_DT, j.LST_ASGN_START_DT, j.ASGN_END_DT, j.LAST_DATE_WORKED, j.EXPECTED_RETURN_DT, j.EXPECTED_END_DATE, j.PC_DATE_CPG, j.PROBATION_DT, j.PROBATION, j.PROBATION_TYPE, j.SOCIALWELF_PREFIX, j.CALSOCIALWELF, j.SOCIALWELFBEFYN, j.SOCIALWELFID, j.PERCENTSOCIALWELF, j.SOCIAL_BRANCH_CPG, j.ISCOMPSOCIALWELF, j.PAYORGCODE, j.PERIODMASTID, j.BONUS, j.CALTAXMETHOD, j.CCA_CPG, j.JOB_INDICATOR, j.JOBOPEN_NO, j.PAYROLLID, j.MANAGER_LEVEL, j.SAL_ADMIN_PLAN, ( CASE WHEN j.ACTION = 'HIR' AND j.EFFDT BETWEEN :C_STDATE AND :C_EDDATE THEN 'N' WHEN j.ACTION = 'LOA' AND j.EFFDT BETWEEN :C_STDATE AND :C_EDDATE THEN 'L' WHEN j.HR_STATUS = 'I' THEN 'R' ELSE 'U' END) AS EMPLFLAG, 'Y'  AS ISINTERFACE, NVL ( (SELECT company FROM job_ee O WHERE O.emplid = j.emplid AND O.empl_rcd = j.empl_rcd AND O.effdt    = (SELECT MAX (t.effdt) FROM job_ee t WHERE t.emplid = O.emplid AND t.empl_rcd = O.empl_rcd AND t.effdt    < j.effdt ) AND O.EFFSEQ = (SELECT MAX (t.EFFSEQ) FROM job_ee t WHERE t.emplid = O.emplid AND t.empl_rcd = O.empl_rcd AND t.effdt    = j.effdt ) ), j.company) AS PRE_COMPANY, j.CREATEUSER, j.createdate, j.MODIFYDATE, :PROGRAMCODE, :DATEOFINTERFACE, :USERINTERFACE, P.EMPLTITLE AS EMPL_TITLE, ( CASE WHEN :LANG = :LOCAL THEN P.EMPLFIRSTNAME || ' ' || P.EMPLLASTNAME ELSE P.EMPLENGFIRSTNAME || ' ' || P.EMPLENGLASTNAME END) AS EMPL_NAME, ( CASE WHEN P.SEX = 'F' THEN '0' WHEN P.SEX = 'M' THEN '1' END)          AS EMPL_SEX, P.MARRYSTATUS AS EMP_MARRYSTATUS, (SELECT ( CASE WHEN AC.EMPL_STATUS = 'T' THEN 3 ELSE 1 END) FROM Action_Tbl AC WHERE AC.ACTION = j.ACTION AND AC.EFFDT    = (SELECT MAX(ACT.EFFDT) FROM Action_Tbl ACT WHERE ACT.ACTION = AC.ACTION ) )                 AS STATUS, P.BIRTHDATE       AS BIRTHDATE, PK.BANKCODE       AS BANKCODE, K.ACCOUNT_ID      AS ACCOUNTID, K.BANK_ACCOUNT    AS BANKACCOUNT, K.BANK_BRANCH     AS BANK_BRANCH, j.HARMFUL_PERCENT AS HARMFUL, j.WORKDAYHOUR     AS WORKHOUR, (SELECT PN.NATIONAL_ID FROM PERSON_NID PN WHERE j.EMPLID          = PN.EMPLID AND P.COUNTRY           = PN.COUNTRY AND PN.NATIONAL_ID_TYPE = 'PID' ) AS NID, (P.BUILDING || ' ' || ( CASE WHEN P.ADDRESS IS NOT NULL THEN 'No. ' || P.ADDRESS ELSE '' END) || ' ' || P.STREET || ' ' || ( CASE WHEN P.TAMBOL IS NOT NULL THEN P.TAMBOL || ' Sub - District' ELSE '' END)) || ' ' ||( ( CASE WHEN P.AMPHUR IS NOT NULL THEN P.AMPHUR || ' District' ELSE '' END) || ' ' || (SELECT CASE WHEN :LANG = :LOCAL THEN GD.LOCALDESCRIPTION ELSE NVL (GD.ENGDESCRIPTION, GD.LOCALDESCRIPTION) END FROM PYGENERALDT GD WHERE GD.GDCODE = 'City' AND GD.DTCODE   = P.CITY ) || ' ' || (SELECT CASE WHEN :LANG = :LOCAL THEN GD.LOCALDESCRIPTION ELSE NVL (GD.ENGDESCRIPTION, GD.LOCALDESCRIPTION) END FROM PYGENERALDT GD WHERE GD.GDCODE = 'Province' AND GD.DTCODE   = P.STATE ) || ' ' || P.POSTAL || ' ' || (SELECT CASE WHEN :LANG = :LOCAL THEN GD.LOCALDESCRIPTION ELSE NVL (GD.ENGDESCRIPTION, GD.LOCALDESCRIPTION) END FROM PYGENERALDT GD WHERE GD.GDCODE = 'Country' AND GD.DTCODE   = P.COUNTRY )) AS ADDRESS1, (P.BUILDING || ' ' || ( CASE WHEN P.ADDRESS IS NOT NULL THEN 'No. ' || P.ADDRESS ELSE '' END) || ' ' || P.STREET || ' ' || ( CASE WHEN P.TAMBOL IS NOT NULL THEN P.TAMBOL || ' Sub - District' ELSE '' END)) || ' ' ||( ( CASE WHEN P.AMPHUR IS NOT NULL THEN P.AMPHUR || ' District' ELSE '' END) || ' ' || (SELECT CASE WHEN :LANG = :LOCAL THEN GD.LOCALDESCRIPTION ELSE NVL (GD.ENGDESCRIPTION, GD.LOCALDESCRIPTION) END FROM PYGENERALDT GD WHERE GD.GDCODE = 'City' AND GD.DTCODE   = P.CITY ) || ' ' || (SELECT CASE WHEN :LANG = :LOCAL THEN GD.LOCALDESCRIPTION ELSE NVL (GD.ENGDESCRIPTION, GD.LOCALDESCRIPTION) END FROM PYGENERALDT GD WHERE GD.GDCODE = 'Province' AND GD.DTCODE   = P.STATE ) || ' ' || P.POSTAL || ' ' || (SELECT CASE WHEN :LANG = :LOCAL THEN GD.LOCALDESCRIPTION ELSE NVL (GD.ENGDESCRIPTION, GD.LOCALDESCRIPTION) END FROM PYGENERALDT GD WHERE GD.GDCODE = 'Country' AND GD.DTCODE   = P.COUNTRY )) AS ADDRESS2, (SELECT PN.NATIONAL_ID FROM PERSON_NID PN WHERE j.EMPLID          = PN.EMPLID AND P.COUNTRY           = PN.COUNTRY AND PN.NATIONAL_ID_TYPE = 'SSOID' ) AS SCNO, (SELECT PN.NATIONAL_ID FROM PERSON_NID PN WHERE j.EMPLID          = PN.EMPLID AND P.COUNTRY           = PN.COUNTRY AND PN.NATIONAL_ID_TYPE = 'TAXID' ) AS TAXID, ( CASE WHEN j.sat_work = 'Y' THEN c.SAT_WORKDAY ELSE c.nosat_workday END) AS WORKDAY_MONTH FROM job_ee j LEFT OUTER JOIN (SELECT c1.* FROM company_dtl c1 WHERE c1.effdt = (SELECT MAX(c2.effdt) FROM company_dtl c2 WHERE c2.company = c1.company AND c2.month     = c1.month ) ) c ON c.company = j.company AND c.month  = :C_MONTH LEFT OUTER JOIN PERS_BANK K ON j.EMPLID      = K.EMPLID AND K.ACCOUNT_ID = 1 LEFT OUTER JOIN PYBANK PK ON K.BANK_CD = PK.BANKID , person_tbl p WHERE j.effdt = (SELECT MAX(j1.effdt) FROM job_ee j1 WHERE j1.emplid = j.emplid AND j1.effdt BETWEEN :C_STDATE AND :C_EDDATE ) AND j.effseq = (SELECT MAX(j1.effseq) FROM job_ee j1 WHERE j1.emplid = j.emplid AND j1.effdt    = j.effdt ) AND j.emplid    = p.emplid AND J.HR_STATUS = 'I' ) x WHERE x.EMPL_CLASS = NVL (:C_EMPLCLASS, x.EMPL_CLASS) AND x.COMPANY      = :C_COMP AND (x.PAYORGCODE LIKE NVL(:PAYORGCODE || '%',x.PAYORGCODE) OR x.payorgcode    IS NULL) AND x.MANAGER_LEVEL = NVL(:MANAGERLVL,x.MANAGER_LEVEL) AND x.EMPLID        = NVL(:C_EMPLID,x.EMPLID) AND (x.PERIODMASTID = NVL(:PERIODMASTID,x.PERIODMASTID) OR x.PERIODMASTID  IS NULL)   {0}"

    '            cmd.Parameters.Clear()
    '            cmd.Parameters.Add(New OracleParameter With {.ParameterName = "PROGRAMCODE", .Value = param.ProgramCode})
    '            cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_MONTH", .Value = _criteria.Month})
    '            cmd.Parameters.Add(New OracleParameter With {.ParameterName = "MANAGERLVL", .Value = If(String.IsNullOrEmpty(_criteria.Managerlvl), String.Empty, _criteria.Managerlvl)})
    '            cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_EMPLID", .Value = If(String.IsNullOrEmpty(_criteria.Emplid), String.Empty, _criteria.Emplid)})
    '            cmd.Parameters.Add(New OracleParameter With {.ParameterName = "PERIODMASTID", .Value = _criteria.PeriodMastId.Periodmastid})
    '            cmd.Parameters.Add(New OracleParameter With {.ParameterName = "PAYORGCODE", .Value = If(_criteria.Org Is Nothing, "", _criteria.Org.Relateid)})
    '            cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_STDATE", .Value = _criteria.StartDate})
    '            cmd.Parameters.Add(New OracleParameter With {.ParameterName = "USERINTERFACE", .Value = param.UserName})
    '            cmd.Parameters.Add(New OracleParameter With {.ParameterName = "DATEOFINTERFACE", .Value = d})
    '            cmd.Parameters.Add(New OracleParameter With {.ParameterName = "LOCAL", .Value = param.LocalLang})
    '            cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_COMP", .Value = _criteria.Company})
    '            cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_EDDATE", .Value = _criteria.EndDate})
    '            cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_EMPLCLASS", .Value = If(String.IsNullOrEmpty(_criteria.EmplClass), String.Empty, _criteria.EmplClass)})
    '            cmd.Parameters.Add(New OracleParameter With {.ParameterName = "LANG", .Value = param.Lang})

    '            cmd.CommandText = String.Format(sqlText, authenstr)
    '            cmd.Transaction = trn
    '            cmd.ExecuteNonQuery()


    '            sqlText = "INSERT INTO INTERFACE_COMPEN(EMPLID,EMPL_RCD,EFFDT,EFFSEQ,COMP_RATECD,INCEXPTYPE,PAYQTY,COMPENSATION_RATE,CHANGE_AMT,FREQUENCY,EMPLFLAG,ISINTERFACE," & _
    '                                               "CREATEUSER,CREATEDATE,MODIFYDATE,PROGRAMCODE,DATEOFINTERFACE,USERINTERFACE) " & _
    '                      "SELECT EMPLID,EMPL_RCD,EFFDT,EFFSEQ,COMP_RATECD,INCEXPTYPE,PAYQTY,COMPENSATION_RATE,CHANGE_AMT,FREQUENCY,EMPLFLAG,ISINTERFACE,CREATEUSER, " & _
    '                      "       CREATEDATE,MODIFYDATE,PROGRAMCODE,:DATEOFINTERFACE,:USERINTERFACE  " & _
    '                     "FROM INTERFACETEMP_COMPEN A " & _
    '                      "WHERE A.ISINTERFACE = 'Y' AND  A.EMPL_CLASS = NVL (:C_EMPLCLASS, A.EMPL_CLASS) AND A.COMPANY = :C_COMP AND A.MANAGER_LEVEL = NVL(:MANAGERLVL,A.MANAGER_LEVEL)   " & _
    '                      "AND A.EFFDT BETWEEN :C_STDATE AND :C_EDDATE AND A.EMPLID = NVL(:C_EMPLID,A.EMPLID) AND A.EMPLFLAG = NVL(:EMPLFLAG,A.EMPLFLAG) AND A.PERIODMASTID = :PERIODMASTID  {0}"


    '            cmd.Parameters.Clear()
    '            cmd.Parameters.Add(New OracleParameter With {.ParameterName = "EMPLFLAG", .Value = _criteria.EmplFlag})
    '            cmd.Parameters.Add(New OracleParameter With {.ParameterName = "MANAGERLVL", .Value = If(String.IsNullOrEmpty(_criteria.Managerlvl), String.Empty, _criteria.Managerlvl)})
    '            cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_EMPLCLASS", .Value = If(String.IsNullOrEmpty(_criteria.EmplClass), String.Empty, _criteria.EmplClass)})
    '            cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_COMP", .Value = _criteria.Company})
    '            cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_STDATE", .Value = _criteria.StartDate})
    '            cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_EDDATE", .Value = _criteria.EndDate})
    '            cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_EMPLID", .Value = If(String.IsNullOrEmpty(_criteria.Emplid), String.Empty, _criteria.Emplid)})
    '            cmd.Parameters.Add(New OracleParameter With {.ParameterName = "DATEOFINTERFACE", .Value = d})
    '            cmd.Parameters.Add(New OracleParameter With {.ParameterName = "USERINTERFACE", .Value = param.UserName})
    '            cmd.Parameters.Add(New OracleParameter With {.ParameterName = "PERIODMASTID", .Value = _criteria.PeriodMastId.Periodmastid})
    '            cmd.CommandText = String.Format(sqlText, authenstr)
    '            cmd.Transaction = trn
    '            cmd.ExecuteNonQuery()


    '            trn.Commit()
    '        Catch ex As Exception
    '            trn.Rollback()
    '            Throw ex
    '        End Try
    '    Catch ex As Exception
    '        status = False
    '        Throw ex
    '    Finally
    '        con.Close()
    '    End Try

    '    Return status
    'End Function
#End Region

#Region "Combobox"

    <OperationContract()> _
    Public Function FindPayBranch(ByVal param As SsCommon.ServiceParam, ByVal Company As String, ByVal Org As String, ByVal IsSmartPay As String) As List(Of Model.ModelComboLst)
        Dim ret As New List(Of Model.ModelComboLst)
        Dim con As System.Data.Common.DbConnection = New OracleConnection(Util.GetOraConnectionString)

        Dim cmdBranch As OracleCommand = con.CreateCommand
        cmdBranch.CommandType = CommandType.Text
        cmdBranch.BindByName = True

        If Org IsNot Nothing Then
            'PYORGANIZE (BR)
            If IsSmartPay Is Nothing OrElse String.IsNullOrEmpty(IsSmartPay) Then
                cmdBranch.CommandText = "SELECT ORGCODE AS KEY,ORGCODE AS NAME FROM PYORGANIZE where LEVELID = '3' and COMPANYID Like :COM || '%'  order by RELATEID"
            Else
                If IsSmartPay = "Y" Then
                    cmdBranch.CommandText = "SELECT ORGCODE AS KEY,ORGCODE AS NAME FROM PYORGANIZE where LEVELID = '3' and COMPANYID Like :COM || '%' and relateid like :COM || '.' || :ORG || '%' order by RELATEID"
                ElseIf IsSmartPay = "N" Then
                    cmdBranch.CommandText = "SELECT ORGCODE AS KEY,ORGCODE AS NAME FROM PYORGANIZE where LEVELID = '3' and COMPANY Like :COM || '%' and relateid like :COM || '.' || :ORG || '%'  AND TREENO = (SELECT P.TREENO FROM PYORGTREE P WHERE P.TREENO  = (SELECT MAX(TO_NUMBER(O.TREENO)) FROM PYORGTREE O WHERE O.EFFDT <= SYSDATE)) order by RELATEID"
                End If
            End If
            cmdBranch.Parameters.Clear()
            cmdBranch.Parameters.Add(New OracleParameter With {.ParameterName = "COM", .Value = Company})
            cmdBranch.Parameters.Add(New OracleParameter With {.ParameterName = "ORG", .Value = Org})

            Try
                If con.State = ConnectionState.Closed Then
                    con.Open()
                End If

                Dim rdrB As System.Data.Common.DbDataReader = cmdBranch.ExecuteReader()
                While (rdrB.Read)
                    Dim m As New Model.ModelComboLst
                    m.RetrieveFromDataReader(rdrB)
                    ret.Add(m)
                End While

            Catch ex As Exception
                Throw ex
            Finally
                con.Close()
            End Try

        End If

        Return ret
    End Function

    <OperationContract()> _
    Public Function FindPayOR(ByVal param As SsCommon.ServiceParam, ByVal Company As String, ByVal Org As String, ByVal Branch As String, ByVal IsSmartPay As String) As List(Of Model.ModelComboLst)
        Dim ret As New List(Of Model.ModelComboLst)
        Dim con As System.Data.Common.DbConnection = New OracleConnection(Util.GetOraConnectionString)

        Dim cmdOR As OracleCommand = con.CreateCommand
        cmdOR.CommandType = CommandType.Text
        cmdOR.BindByName = True

        If Branch IsNot Nothing Then
            'PYORGANIZE (OR)
            If IsSmartPay Is Nothing OrElse String.IsNullOrEmpty(IsSmartPay) Then
                cmdOR.CommandText = "SELECT ORGCODE AS KEY,ORGCODE AS NAME FROM PYORGANIZE where LEVELID = '4' and COMPANYID Like :COM || '%'  order by RELATEID"
            Else
                If IsSmartPay = "Y" Then
                    cmdOR.CommandText = "SELECT ORGCODE AS KEY,ORGCODE AS NAME FROM PYORGANIZE where LEVELID = '4' and COMPANYID Like :COM || '%' and relateid like :COM || '.' || :ORG || '.' || :BRANCH || '%' order by RELATEID"
                ElseIf IsSmartPay = "N" Then
                    cmdOR.CommandText = "SELECT ORGCODE AS KEY,ORGCODE AS NAME FROM PYORGANIZE where LEVELID = '4' and COMPANY Like :COM || '%' and relateid like :COM || '.' || :ORG || '.' || :BRANCH || '%'  AND TREENO = (SELECT P.TREENO FROM PYORGTREE P WHERE P.TREENO  = (SELECT MAX(TO_NUMBER(O.TREENO)) FROM PYORGTREE O WHERE O.EFFDT <= SYSDATE)) order by RELATEID"
                End If
            End If
            cmdOR.Parameters.Clear()
            cmdOR.Parameters.Add(New OracleParameter With {.ParameterName = "COM", .Value = Company})
            cmdOR.Parameters.Add(New OracleParameter With {.ParameterName = "ORG", .Value = Org})
            cmdOR.Parameters.Add(New OracleParameter With {.ParameterName = "BRANCH", .Value = Branch})

            Try
                If con.State = ConnectionState.Closed Then
                    con.Open()
                End If

                Dim rdr As System.Data.Common.DbDataReader = cmdOR.ExecuteReader()
                While (rdr.Read)
                    Dim m As New Model.ModelComboLst
                    m.RetrieveFromDataReader(rdr)
                    ret.Add(m)
                End While

            Catch ex As Exception
                Throw ex
            Finally
                con.Close()
            End Try

        End If

        Return ret
    End Function


#End Region

#Region "Generate Text"
    <OperationContract()> _
    Public Function NewGenerateText(param As SsCommon.ServiceParam, _criteria As Model.ModelCriteriaList, ByRef EmplidList As List(Of Model.ModelInterfacetempEe), ByVal formatdt As String, ByVal pathServer As String) As SsCommon.ServiceResult(Of String, String)
        Dim ret As New SsCommon.ServiceResult(Of String, String)

        'ret.IsSuccess = NewInsertDataInternal(param, _criteria, EmplidList)
        'If ret.IsSuccess Then
        '    ret.Result = NewGenerateTextInternal(param, _criteria, formatdt, pathServer, EmplidList)
        'Else
        '    ret.Result = "Cannot Save Data!. Generate Text will not be processed."
        'End If

        'Dim strText As String = File.ReadAllText(curPath & fileName & "\" & fileName & ".txt")

        Dim curPath As String = pathServer
        If curPath(curPath.Length - 1) <> "\" Then
            curPath = curPath & "\"
        End If

        Dim fileName As String = "MS" & _criteria.Month & _criteria.Year & _criteria.Company

        Try
            Dim strText As String = File.ReadAllText(curPath & fileName & "\" & fileName & ".txt")
            Dim dtEmpList As DataTable = Newtonsoft.Json.JsonConvert.DeserializeObject(Of DataTable)(strText)

            If dtEmpList IsNot Nothing AndAlso dtEmpList.Rows.Count > 0 Then
                ret.Result = CallToGenerateText(_criteria, formatdt, pathServer, fileName, dtEmpList)
            End If
        Catch ex As Exception
            ret.Result = "Not found text file " & fileName & ".txt " & "press GetData and GenText."
        End Try
        
        Return ret
    End Function

    Public Function NewInsertDataInternal(param As SsCommon.ServiceParam, _criteria As Model.ModelCriteriaList, ByRef EmplidList As List(Of Model.ModelInterfacetempEe)) As Boolean
        Dim status = True
        Dim dpBp As New SsHrCommonService
        Dim ret As New List(Of Model.ModelInterfaceLog)
        Dim con As System.Data.Common.DbConnection = New OracleConnection(Util.GetOraConnectionString)
        Dim cmd As OracleCommand = con.CreateCommand
        cmd.BindByName = True
        authen = GetGdDetails("HRAUTHEN")
        Dim d As Date = Now
        Dim sqlText As String = String.Empty
        Dim col As String = "A.Grade"
        Dim authenstr = String.Format("AND A.DEPTID IN ({0}) ", dpBp.StringAuthenDepartment(param.UserName, "A.DEPTID", col, Today))

        If authen IsNot Nothing AndAlso authen.Count > 0 Then
            Dim val = (From x In authen Where x.Dtcode = "HR" Select x.Localdescription).FirstOrDefault
            If val = "M" Then
                col = "A.manager_level"
            End If
        End If

        If _criteria.EmplFlag = "A" Then
            _criteria.EmplFlag = ""
        Else
            _criteria.EmplFlag = _criteria.EmplFlag
        End If

        Dim Criteria_Emplid As String = ""
        If Not _criteria.chkAll Then
            For Each o In EmplidList
                'If o.chk Then
                '    Criteria_Emplid += IIf(Criteria_Emplid <> "", ",", "") & "'" & o.Emplid & "'"
                'End If
                Criteria_Emplid += IIf(Criteria_Emplid <> "", ",", "") & "'" & o.Emplid & "'"
                o._IsEnable = False
            Next
        Else
            Criteria_Emplid = If(String.IsNullOrEmpty(_criteria.Emplid), String.Empty, _criteria.Emplid)
            For Each o In EmplidList
                o._IsEnable = False
            Next
        End If

        Try
            If cmd.Connection.State = ConnectionState.Closed Then
                cmd.Connection.Open()
            End If
            Dim trn As System.Data.Common.DbTransaction = con.BeginTransaction()
            Try
                sqlText = "delete from interface_ee a " & _
                           "WHERE A.ISINTERFACE = 'N' AND A.EMPL_CLASS = NVL (:C_EMPLCLASS, A.EMPL_CLASS) AND A.COMPANY = :C_COMP AND A.PAYORGCODE like NVL(:PAYORGCODE || '%',A.PAYORGCODE) AND A.MANAGER_LEVEL = NVL(:MANAGERLVL,A.MANAGER_LEVEL)   " & _
                           "AND A.EMPLFLAG = NVL(:EMPLFLAG,A.EMPLFLAG) AND A.PERIODMASTID = :PERIODMASTID  {0} "
                If _criteria.chkAll Then
                    sqlText += " AND A.EMPLID = NVL('" & Criteria_Emplid & "',A.EMPLID) "
                Else
                    sqlText += " AND A.EMPLID IN (" & Criteria_Emplid & ")"
                End If

                cmd.Parameters.Clear()
                If _criteria.EndDate IsNot Nothing Then
                    sqlText = String.Format(sqlText, "AND A.EFFDT <= :DateTo {0}")
                    cmd.Parameters.Add(New OracleParameter With {.ParameterName = "DateTo", .Value = _criteria.EndDate})
                End If
                'If _criteria.StartDate IsNot Nothing AndAlso _criteria.EndDate IsNot Nothing Then
                '    sqlText = String.Format(sqlText, "AND A.EFFDT BETWEEN :DateFrom AND :DateTo {0}")
                '    cmd.Parameters.Add(New OracleParameter With {.ParameterName = "DateFrom", .Value = _criteria.StartDate})
                '    cmd.Parameters.Add(New OracleParameter With {.ParameterName = "DateTo", .Value = _criteria.EndDate})
                'ElseIf _criteria.StartDate IsNot Nothing Then
                '    sqlText = String.Format(sqlText, "AND A.EFFDT >= :DateFrom {0}")
                '    cmd.Parameters.Add(New OracleParameter With {.ParameterName = "DateFrom", .Value = _criteria.StartDate})
                'ElseIf _criteria.EndDate IsNot Nothing Then
                '    sqlText = String.Format(sqlText, "AND A.EFFDT <= :DateTo {0}")
                '    cmd.Parameters.Add(New OracleParameter With {.ParameterName = "DateTo", .Value = _criteria.EndDate})
                'End If

                'cmd.Parameters.Clear()
                cmd.Parameters.Add(New OracleParameter With {.ParameterName = "EMPLFLAG", .Value = _criteria.EmplFlag})
                cmd.Parameters.Add(New OracleParameter With {.ParameterName = "MANAGERLVL", .Value = If(String.IsNullOrEmpty(_criteria.Managerlvl), String.Empty, _criteria.Managerlvl)})
                cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_EMPLCLASS", .Value = If(String.IsNullOrEmpty(_criteria.EmplClass), String.Empty, _criteria.EmplClass)})
                cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_COMP", .Value = _criteria.Company})
                cmd.Parameters.Add(New OracleParameter With {.ParameterName = "PAYORGCODE", .Value = If(_criteria.Org Is Nothing, "", _criteria.Org.Relateid)})
                cmd.Parameters.Add(New OracleParameter With {.ParameterName = "PERIODMASTID", .Value = _criteria.PeriodMastId.Periodmastid})
                cmd.CommandText = String.Format(sqlText, authenstr)
                cmd.Transaction = trn
                cmd.ExecuteNonQuery()


                sqlText = "delete from interface_compen a " & _
                          "WHERE NVL(A.ISINTERFACE,'N') = 'N' " & _
                          "AND EXISTS (SELECT 1 FROM INTERFACE_COMPEN B ,JOB_EE A  WHERE B.EMPLID = A.EMPLID AND B.EMPL_RCD = A.EMPL_RCD AND B.EFFDT = A.EFFDT AND B.EFFSEQ = A.EFFSEQ AND A.PERIODMASTID = :PERIODMASTID " & _
                          "AND b.EMPLFLAG  = NVL(:EMPLFLAG,b.EMPLFLAG) AND A.EMPL_CLASS = NVL (:C_EMPLCLASS, A.EMPL_CLASS) AND A.COMPANY = :C_COMP AND A.PAYORGCODE like NVL(:PAYORGCODE || '%',A.PAYORGCODE) " & _
                          "AND A.MANAGER_LEVEL = NVL(:MANAGERLVL,A.MANAGER_LEVEL)  {0}) " & _
                          "AND EMPLFLAG <> 'D' "

                If _criteria.chkAll Then
                    sqlText += " AND A.EMPLID = NVL('" & Criteria_Emplid & "',A.EMPLID) "
                Else
                    sqlText += " AND A.EMPLID IN (" & Criteria_Emplid & ")"
                End If

                cmd.Parameters.Clear()
                If _criteria.EndDate IsNot Nothing Then
                    sqlText = String.Format(sqlText, "AND A.EFFDT <= :DateTo {0}")
                    cmd.Parameters.Add(New OracleParameter With {.ParameterName = "DateTo", .Value = _criteria.EndDate})
                End If
                'If _criteria.StartDate IsNot Nothing AndAlso _criteria.EndDate IsNot Nothing Then
                '    sqlText = String.Format(sqlText, "AND A.EFFDT BETWEEN :DateFrom AND :DateTo {0}")
                '    cmd.Parameters.Add(New OracleParameter With {.ParameterName = "DateFrom", .Value = _criteria.StartDate})
                '    cmd.Parameters.Add(New OracleParameter With {.ParameterName = "DateTo", .Value = _criteria.EndDate})
                'ElseIf _criteria.StartDate IsNot Nothing Then
                '    sqlText = String.Format(sqlText, "AND A.EFFDT >= :DateFrom {0}")
                '    cmd.Parameters.Add(New OracleParameter With {.ParameterName = "DateFrom", .Value = _criteria.StartDate})
                'ElseIf _criteria.EndDate IsNot Nothing Then
                '    sqlText = String.Format(sqlText, "AND A.EFFDT <= :DateTo {0}")
                '    cmd.Parameters.Add(New OracleParameter With {.ParameterName = "DateTo", .Value = _criteria.EndDate})
                'End If

                'cmd.Parameters.Clear()
                cmd.Parameters.Add(New OracleParameter With {.ParameterName = "EMPLFLAG", .Value = _criteria.EmplFlag})
                cmd.Parameters.Add(New OracleParameter With {.ParameterName = "MANAGERLVL", .Value = If(String.IsNullOrEmpty(_criteria.Managerlvl), String.Empty, _criteria.Managerlvl)})
                cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_EMPLCLASS", .Value = If(String.IsNullOrEmpty(_criteria.EmplClass), String.Empty, _criteria.EmplClass)})
                cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_COMP", .Value = _criteria.Company})
                cmd.Parameters.Add(New OracleParameter With {.ParameterName = "PAYORGCODE", .Value = If(_criteria.Org Is Nothing, "", _criteria.Org.Relateid)})
                cmd.Parameters.Add(New OracleParameter With {.ParameterName = "PERIODMASTID", .Value = _criteria.PeriodMastId.Periodmastid})
                cmd.CommandText = String.Format(sqlText, authenstr)
                cmd.Transaction = trn
                cmd.ExecuteNonQuery()


                sqlText = "INSERT INTO INTERFACE_EE(EMPLID,EMPL_RCD,EFFDT,EFFSEQ,ACTION,ACTION_DT,ACTION_REASON,PER_ORG,DEPTID,JOBCODE,POSITION_NBR,POSITION_LEVEL," & _
                                                               "REPORT_TO,POSN_OVRD,HR_STATUS,EMPL_STATUS,LOCATION,JOB_ENTRY_DT,DEPT_ENTRY_DT,POSITION_ENTRY_DT,POSNLEVEL_ENTRY_DT,SHIFT," & _
                                                               "REG_TEMP,FULL_PART_TIME,COMPANY,PAYGROUP,POITYPE,EMPLGROUP,EMPLIDCODE,HOLIDAY_SCHEDULE,STD_HOURS,STD_HRS_FREQUENCY,OFFICER_CD," & _
                                                               "EMPL_CLASS,GRADE,GRADE_ENTRY_DT,COMP_FREQUENCY,COMPRATE,CHANGE_AMT,CHANGE_PCT,CURRENCY_CD,BUSINESS_UNIT,SETID_DEPT,SETID_JOBCODE," & _
                                                               "HIRE_DT,LAST_HIRE_DT,TERMINATION_DT,ASGN_START_DT,LST_ASGN_START_DT,ASGN_END_DT,LAST_DATE_WORKED,EXPECTED_RETURN_DT,EXPECTED_END_DATE," & _
                                                               "PC_DATE_CPG,PROBATION_DT,PROBATION,PROBATION_TYPE,SOCIALWELF_PREFIX,CALSOCIALWELF,SOCIALWELFBEFYN,SOCIALWELFID,PERCENTSOCIALWELF," & _
                                                               "SOCIAL_BRANCH_CPG,ISCOMPSOCIALWELF,PAYORGCODE,PERIODMASTID,BONUS,CALTAXMETHOD,CCA_CPG,JOB_INDICATOR,JOBOPEN_NO,PAYROLLID,MANAGER_LEVEL," & _
                                                               "SAL_ADMIN_PLAN,EMPLFLAG,ISINTERFACE,PRE_COMPANY,CREATEUSER,CREATEDATE,MODIFYDATE,PROGRAMCODE,DATEOFINTERFACE,USERINTERFACE," & _
                                                               "EMPL_TITLE,EMPL_NAME,EMPL_SEX,EMP_MARRYSTATUS,STATUS,BIRTHDATE,BANKCODE,ACCOUNTID,BANKACCOUNT,BANK_BRANCH,HARMFUL,WORKHOUR,NID,ADDRESS1,ADDRESS2,SCNO,TAXID,WORKDAY_MONTH) " & _
                                   "SELECT EMPLID,EMPL_RCD,EFFDT,EFFSEQ,ACTION,ACTION_DT,ACTION_REASON,PER_ORG,DEPTID,JOBCODE,POSITION_NBR,POSITION_LEVEL,REPORT_TO,POSN_OVRD,HR_STATUS, " & _
                                    "       EMPL_STATUS,LOCATION,JOB_ENTRY_DT,DEPT_ENTRY_DT,POSITION_ENTRY_DT,POSNLEVEL_ENTRY_DT,SHIFT,REG_TEMP,FULL_PART_TIME,COMPANY,PAYGROUP,POITYPE,EMPLGROUP, " & _
                                    "       EMPLIDCODE,HOLIDAY_SCHEDULE,STD_HOURS,STD_HRS_FREQUENCY,OFFICER_CD,EMPL_CLASS,GRADE,GRADE_ENTRY_DT,COMP_FREQUENCY,COMPRATE,CHANGE_AMT,CHANGE_PCT, " & _
                                    "       CURRENCY_CD,BUSINESS_UNIT,SETID_DEPT,SETID_JOBCODE,HIRE_DT,LAST_HIRE_DT,TERMINATION_DT,ASGN_START_DT,LST_ASGN_START_DT,ASGN_END_DT,LAST_DATE_WORKED, " & _
                                    "       EXPECTED_RETURN_DT,EXPECTED_END_DATE,PC_DATE_CPG,PROBATION_DT,PROBATION,PROBATION_TYPE,SOCIALWELF_PREFIX,CALSOCIALWELF,SOCIALWELFBEFYN,SOCIALWELFID, " & _
                                    "       PERCENTSOCIALWELF,SOCIAL_BRANCH_CPG,ISCOMPSOCIALWELF,PAYORGCODE,PERIODMASTID,BONUS,CALTAXMETHOD,CCA_CPG,JOB_INDICATOR,JOBOPEN_NO,PAYROLLID, " & _
                                    "       MANAGER_LEVEL,SAL_ADMIN_PLAN,EMPLFLAG,ISINTERFACE,PRE_COMPANY,CREATEUSER,CREATEDATE,MODIFYDATE,PROGRAMCODE,:DATEOFINTERFACE,:USERINTERFACE, " & _
                                    "       EMPL_TITLE,EMPL_NAME,EMPL_SEX,EMP_MARRYSTATUS,STATUS,BIRTHDATE,BANKCODE,ACCOUNTID,BANKACCOUNT,BANK_BRANCH,HARMFUL,WORKHOUR,NID,ADDRESS1,ADDRESS2,SCNO,TAXID,WORKDAY_MONTH " & _
                                    "FROM INTERFACETEMP_EE A " & _
                                    "WHERE A.ISINTERFACE = 'N' AND A.EMPL_CLASS = NVL (:C_EMPLCLASS, A.EMPL_CLASS) AND A.COMPANY = :C_COMP AND A.PAYORGCODE like NVL(:PAYORGCODE || '%',A.PAYORGCODE) AND A.MANAGER_LEVEL = NVL(:MANAGERLVL,A.MANAGER_LEVEL)   " & _
                                    " AND A.EMPLFLAG = NVL(:EMPLFLAG,A.EMPLFLAG) AND A.PERIODMASTID = :PERIODMASTID  {0} "
                If _criteria.chkAll Then
                    sqlText += " AND A.EMPLID = NVL('" & Criteria_Emplid & "',A.EMPLID) "
                Else
                    sqlText += " AND A.EMPLID IN (" & Criteria_Emplid & ")"
                End If

                cmd.Parameters.Clear()
                If _criteria.EndDate IsNot Nothing Then
                    sqlText = String.Format(sqlText, "AND A.EFFDT <= :DateTo {0}")
                    cmd.Parameters.Add(New OracleParameter With {.ParameterName = "DateTo", .Value = _criteria.EndDate})
                End If
                'If _criteria.StartDate IsNot Nothing AndAlso _criteria.EndDate IsNot Nothing Then
                '    sqlText = String.Format(sqlText, "AND A.EFFDT BETWEEN :DateFrom AND :DateTo {0}")
                '    cmd.Parameters.Add(New OracleParameter With {.ParameterName = "DateFrom", .Value = _criteria.StartDate})
                '    cmd.Parameters.Add(New OracleParameter With {.ParameterName = "DateTo", .Value = _criteria.EndDate})
                'ElseIf _criteria.StartDate IsNot Nothing Then
                '    sqlText = String.Format(sqlText, "AND A.EFFDT >= :DateFrom {0}")
                '    cmd.Parameters.Add(New OracleParameter With {.ParameterName = "DateFrom", .Value = _criteria.StartDate})
                'ElseIf _criteria.EndDate IsNot Nothing Then
                '    sqlText = String.Format(sqlText, "AND A.EFFDT <= :DateTo {0}")
                '    cmd.Parameters.Add(New OracleParameter With {.ParameterName = "DateTo", .Value = _criteria.EndDate})
                'End If

                'cmd.Parameters.Clear()
                cmd.Parameters.Add(New OracleParameter With {.ParameterName = "EMPLFLAG", .Value = _criteria.EmplFlag})
                cmd.Parameters.Add(New OracleParameter With {.ParameterName = "MANAGERLVL", .Value = If(String.IsNullOrEmpty(_criteria.Managerlvl), String.Empty, _criteria.Managerlvl)})
                cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_EMPLCLASS", .Value = If(String.IsNullOrEmpty(_criteria.EmplClass), String.Empty, _criteria.EmplClass)})
                cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_COMP", .Value = _criteria.Company})
                cmd.Parameters.Add(New OracleParameter With {.ParameterName = "PAYORGCODE", .Value = If(_criteria.Org Is Nothing, "", _criteria.Org.Relateid)})
                cmd.Parameters.Add(New OracleParameter With {.ParameterName = "DATEOFINTERFACE", .Value = d})
                cmd.Parameters.Add(New OracleParameter With {.ParameterName = "PERIODMASTID", .Value = _criteria.PeriodMastId.Periodmastid})
                cmd.Parameters.Add(New OracleParameter With {.ParameterName = "USERINTERFACE", .Value = param.UserName})
                cmd.CommandText = String.Format(sqlText, authenstr)
                cmd.Transaction = trn
                cmd.ExecuteNonQuery()


                sqlText = "INSERT INTO INTERFACE_COMPEN(EMPLID,EMPL_RCD,EFFDT,EFFSEQ,COMP_RATECD,INCEXPTYPE,PAYQTY,COMPENSATION_RATE,CHANGE_AMT,FREQUENCY,EMPLFLAG,ISINTERFACE," & _
                                                   "CREATEUSER,CREATEDATE,MODIFYDATE,PROGRAMCODE,DATEOFINTERFACE,USERINTERFACE) " & _
                          "SELECT EMPLID,EMPL_RCD,EFFDT,EFFSEQ,COMP_RATECD,INCEXPTYPE,PAYQTY,COMPENSATION_RATE,CHANGE_AMT,FREQUENCY,EMPLFLAG,ISINTERFACE,CREATEUSER, " & _
                          "       CREATEDATE,MODIFYDATE,PROGRAMCODE,:DATEOFINTERFACE,:USERINTERFACE  " & _
                         "FROM INTERFACETEMP_COMPEN A " & _
                          "WHERE A.EMPL_CLASS = NVL (:C_EMPLCLASS, A.EMPL_CLASS) AND A.COMPANY = :C_COMP AND A.MANAGER_LEVEL = NVL(:MANAGERLVL,A.MANAGER_LEVEL)   " & _
                          "AND A.EMPLFLAG = NVL(:EMPLFLAG,A.EMPLFLAG) AND A.PERIODMASTID = :PERIODMASTID  {0}"
                If _criteria.chkAll Then
                    sqlText += "AND A.EMPLID = NVL('" & Criteria_Emplid & "',A.EMPLID) "
                Else
                    sqlText += " AND A.EMPLID IN (" & Criteria_Emplid & ")"
                End If

                cmd.Parameters.Clear()
                If _criteria.EndDate IsNot Nothing Then
                    sqlText = String.Format(sqlText, "AND A.EFFDT <= :DateTo {0}")
                    cmd.Parameters.Add(New OracleParameter With {.ParameterName = "DateTo", .Value = _criteria.EndDate})
                End If
                'If _criteria.StartDate IsNot Nothing AndAlso _criteria.EndDate IsNot Nothing Then
                '    sqlText = String.Format(sqlText, "AND A.EFFDT BETWEEN :DateFrom AND :DateTo {0}")
                '    cmd.Parameters.Add(New OracleParameter With {.ParameterName = "DateFrom", .Value = _criteria.StartDate})
                '    cmd.Parameters.Add(New OracleParameter With {.ParameterName = "DateTo", .Value = _criteria.EndDate})
                'ElseIf _criteria.StartDate IsNot Nothing Then
                '    sqlText = String.Format(sqlText, "AND A.EFFDT >= :DateFrom {0}")
                '    cmd.Parameters.Add(New OracleParameter With {.ParameterName = "DateFrom", .Value = _criteria.StartDate})
                'ElseIf _criteria.EndDate IsNot Nothing Then
                '    sqlText = String.Format(sqlText, "AND A.EFFDT <= :DateTo {0}")
                '    cmd.Parameters.Add(New OracleParameter With {.ParameterName = "DateTo", .Value = _criteria.EndDate})
                'End If

                'cmd.Parameters.Clear()
                cmd.Parameters.Add(New OracleParameter With {.ParameterName = "EMPLFLAG", .Value = _criteria.EmplFlag})
                cmd.Parameters.Add(New OracleParameter With {.ParameterName = "MANAGERLVL", .Value = If(String.IsNullOrEmpty(_criteria.Managerlvl), String.Empty, _criteria.Managerlvl)})
                cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_EMPLCLASS", .Value = If(String.IsNullOrEmpty(_criteria.EmplClass), String.Empty, _criteria.EmplClass)})
                cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_COMP", .Value = _criteria.Company})
                cmd.Parameters.Add(New OracleParameter With {.ParameterName = "DATEOFINTERFACE", .Value = d})
                cmd.Parameters.Add(New OracleParameter With {.ParameterName = "USERINTERFACE", .Value = param.UserName})
                cmd.Parameters.Add(New OracleParameter With {.ParameterName = "PERIODMASTID", .Value = _criteria.PeriodMastId.Periodmastid})
                cmd.CommandText = String.Format(sqlText, authenstr)
                cmd.Transaction = trn
                cmd.ExecuteNonQuery()

                '-----------------------------
                'Changed by Chanchira L. on 19/04/2021
                'ret = FindHrPayrollEmpl(cmd, _criteria, authenstr, d, Criteria_Emplid)
                'InsertHrPayrollLog(cmd, trn, _criteria, param, ret)
                Dim strSql As String = NewFindHrPayrollEmpl(cmd, _criteria, authenstr, d, Criteria_Emplid)
                InsertHrPayrollLog(cmd, trn, _criteria, param, d, strSql)
                '-----------------------------
                trn.Commit()

                For Each j In EmplidList
                    j._IsEnable = False
                Next

            Catch ex As Exception
                trn.Rollback()
                status = False
                Throw ex
            End Try
        Catch ex As Exception
            status = False
            Throw ex
        Finally
            con.Close()
        End Try

        Return status

    End Function

    Public Function NewGenerateTextInternal(ByVal param As SsCommon.ServiceParam, ByVal _criteria As Model.ModelCriteriaList, ByVal formatdt As String, ByVal pathServer As String, ByRef EmplidList As List(Of Model.ModelInterfacetempEe)) As String
        Dim ret As String = "Export text file Completed"
        Dim dpBp As New SsHrCommonService
        Dim dt As New DataTable
        Dim col As String = "A.Grade"

        Dim con As System.Data.Common.DbConnection = New OracleConnection(Util.GetOraConnectionString)
        Dim cmd As OracleCommand = con.CreateCommand
        cmd.BindByName = True
        Dim sqlText As String = String.Empty

        Dim Criteria_Emplid As String = ""
        If Not _criteria.chkAll Then
            'Dim Emplid = (From y In EmplidList Where y.chk = True Order By y.Emplid Select y)
            'For Each o In Emplid
            '    Criteria_Emplid += IIf(Criteria_Emplid <> "", ",", "") & "'" & o.Emplid & "'"
            'Next
            For Each o In EmplidList
                Criteria_Emplid += IIf(Criteria_Emplid <> "", ",", "") & "'" & o.Emplid & "'"
            Next
        Else
            Criteria_Emplid = If(String.IsNullOrEmpty(_criteria.Emplid), String.Empty, _criteria.Emplid)
        End If
        '----------------------------------
        'Changed by Chanchira L. on 21/05/2021  add 1 to job_ee.termanate_date (T_RDATE)
        'Changed by Chanchira L. on 26/03/2021  change interfacetemp_compen to compensation_tbl
        sqlText = "SELECT T_TYP, T_FUNC, T_COM, T_CODE, T_TITLE, T_NAME, T_ADD1, T_ADD2, T_OPE, T_BRH, T_ORG, '' AS T_SHF, T_TAXID, T_SEX, MAR,'' AS T_CHILDTOTAL, " & _
                     " '' AS T_CHILDSCHOLL, '' AS T_DEDINSURANCE, '' AS T_DEDHOMEINTEREST, '' AS T_DEDPROVIDENCE, '' AS	T_DEDDONATION, '' AS T_DEDDONATION2, " & _
                     " T_IDNO, T_SCNO, T_STA, T_BDATE, T_EDATE, T_FDATE, '' AS T_TDATE, T_RDATE, '' AS T_POS, T_PC, T_BNO, T_BTY, T_BAC,T_TAXCALMETHOD, " & _
                     " '' AS T_NOCALSOCIAL, '' AS T_DEDLTF, '' AS T_DEDRMF, ''	AS T_DEDFATHER, ''	AS T_FATHERID, ''	AS T_MOTHERID, '' AS T_TITLE_COUPLE, '' AS T_NAME_COUPLE, " & _
                     " '' AS T_SURN_COUPLE, ''	AS T_BDATE_COUPLE, ''	AS T_ID_COUPLE, '' AS T_ID_COUPLE_FAT, '' AS T_ID_COUPLE_MOT, T_SALY, T_BONUSRATE, " & _
                     " '' AS T_POROVIDEN_DDT, '' AS	T_DED_EMP_PER, '' AS T_DED_COM_PER, T_PALW, T_SALW1, T_MALW1, T_FALW1, T_TELW1, T_OTHER, T_SPEC1,'' AS T_HELP1, T_MDED1, " & _
                     " T_ODED1,'' AS T_TRCOM, '' AS T_TRCODE, '' AS T_TROPE, '' AS	T_TRBRH, '' AS	T_TRORG, '' AS T_TRSHF, '' AS	T_TRDATE, T_SLDED,'' AS T_SLBAL, '' AS T_GSB, " & _
                     " T_LLDED, '' AS T_LLBAL, T_HARW1, T_HOUW1, T_SPAW1, T_OINC1, T_VEHW1, '' AS T_PEMWF,	'' AS T_PCPWF, '' AS T_HEALTHY, '' AS T_PEMPV,	'' AS T_PCPPV, " & _
                     " T_BKBRNAME, T_BKBRNAME,	'' AS T_YINC,	'' AS T_YAINC1,	'' AS T_YAINC2,	'' AS T_YAINC3,	'' AS T_YTAX,	'' AS T_YATAX1,	'' AS T_YATAX2,	'' AS T_YATAX3, " & _
                     " '' AS T_YPALW, '' AS T_YSALW,	'' AS T_YMALW,	'' AS T_YFALW,	'' AS T_YHARW,	'' AS T_YHOUW,	'' AS T_YTELW,	'' AS T_YSPAW,	'' AS T_YOINC, " & _
                     " '' AS T_YMDED, '' AS T_YODED,	'' AS T_YOT, '' AS T_YRW,	'' AS T_YLATE, '' AS T_YSLDED,	'' AS T_YLLDED,	'' AS T_YLVDED,	'' AS T_YADJ,	'' AS T_YEMWF, " & _
                     " '' AS T_YCPWF, '' AS T_YEMPV, '' AS T_YCPPV, '' AS T_YHEALTHY, HARMFUL, WORKHOUR, NID, WORKDAY_MONTH, PRE_COMPANY " & _
                     "    FROM(SELECT  A.EMPL_CLASS AS T_TYP, A.EMPLFLAG AS T_FUNC, A.COMPANY AS T_COM, A.PAYROLLID AS T_CODE,A.EMPL_TITLE AS T_TITLE,    " & _
                     "        A.EMPL_NAME AS T_NAME, A.BONUS AS T_BONUSRATE,  " & _
                     "        TO_CHAR(A.ADDRESS1) AS T_ADD1,   " & _
                     "        TO_CHAR(A.ADDRESS2) AS T_ADD2,   " & _
                     "          (select split_text(A.PAYORGCODE,2,'.') from dual) AS T_OPE,   " & _
                     "          (select split_text(A.PAYORGCODE,3,'.') from dual) AS T_BRH,    " & _
                     "          (select split_text(A.PAYORGCODE,4,'.') from dual) AS T_ORG,    " & _
                     "          A.EMPL_SEX AS T_SEX, A.EMP_MARRYSTATUS AS MAR, " & _
                     "          A.EMPLID AS T_IDNO, " & _
                     "          A.STATUS AS T_STA,   " & _
                     "        TO_CHAR(A.BIRTHDATE,'YYYYMMDD') AS T_BDATE, TO_CHAR(A.HIRE_DT,'YYYYMMDD') AS T_EDATE, TO_CHAR(A.PROBATION_DT,'YYYYMMDD') AS T_FDATE, TO_CHAR(TRUNC(A.TERMINATION_DT) + 1,'YYYYMMDD') AS T_RDATE, A.GRADE AS T_PC, A.BANKCODE AS T_BNO,   " & _
                     "        A.ACCOUNTID AS T_BTY, A.BANKACCOUNT AS T_BAC, A.CALTAXMETHOD AS T_TAXCALMETHOD,A.BANK_BRANCH AS T_BKBRNAME,b.COMP_RATECD,b.COMPENSATION_RATE, " & _
                     "        A.HARMFUL,A.WORKHOUR,A.NID,A.SCNO AS T_SCNO,A.TAXID AS T_TAXID, A.WORKDAY_MONTH, A.PRE_COMPANY " & _
                     " FROM INTERFACETEMP_EE A LEFT OUTER JOIN COMPENSATION_TBL B ON A.EMPLID = B.EMPLID AND A.EMPL_RCD = B.EMPL_RCD AND A.EFFDT = b.EFFDT AND A.EFFSEQ = B.EFFSEQ " & _
                     "WHERE A.EMPL_CLASS = NVL (:C_EMPLCLASS, A.EMPL_CLASS) AND A.COMPANY = :C_COMP " & _
                     "AND A.PAYORGCODE like NVL(:PAYORGCODE || '%',A.PAYORGCODE) " & _
                     "AND A.MANAGER_LEVEL = NVL(:MANAGERLVL,A.MANAGER_LEVEL)  " & _
                     "  AND A.PERIODMASTID = :PERIODMASTID AND A.EMPLFLAG = (CASE WHEN :EMPLFLAG = 'A' THEN A.EMPLFLAG ELSE NVL(:EMPLFLAG,A.EMPLFLAG)END) {0}  "
        '" FROM INTERFACETEMP_EE A LEFT OUTER JOIN INTERFACETEMP_COMPEN B ON A.EMPLID = B.EMPLID AND A.EMPL_RCD = B.EMPL_RCD AND A.EFFDT = b.EFFDT AND A.EFFSEQ = B.EFFSEQ " & _
        '"        TO_CHAR(A.BIRTHDATE,'YYYYMMDD') AS T_BDATE, TO_CHAR(A.HIRE_DT,'YYYYMMDD') AS T_EDATE, TO_CHAR(A.PROBATION_DT,'YYYYMMDD') AS T_FDATE, TO_CHAR(A.TERMINATION_DT,'YYYYMMDD') AS T_RDATE, A.GRADE AS T_PC, A.BANKCODE AS T_BNO,   " & _
        If _criteria.chkAll Then
            sqlText += " AND A.EMPLID = NVL ('" & Criteria_Emplid & "', A.EMPLID) "
        Else
            sqlText += " AND A.EMPLID IN (" & Criteria_Emplid & ") "
        End If
        sqlText += ") PIVOT  (SUM(COMPENSATION_RATE) FOR (COMP_RATECD) IN ( 'AGSALY' AS T_SALY,'AGSPPS' AS T_PALW,'AGUPCY' AS T_SALW1,'AGMEAL' AS T_MALW1,'AGHARM' AS T_FALW1, " & _
                   "                                                                  'AGTELE' AS T_TELW1,'NONE' AS T_OTHER, 'NONE' AS T_SPEC1, 'AGDMEL' AS T_MDED1,'NONE' AS T_ODED1, " & _
                   "                                                                  'AGDLOA' AS T_SLDED,'AGDBGH' AS T_LLDED,'AGHSHP' AS T_HARW1,'AGRSDT' AS T_HOUW1,'AGSPIC' AS T_SPAW1, " & _
                   "                                                                  'AGOTHC' AS T_OINC1, 'AGVHLE' AS T_VEHW1)) "


        cmd.Parameters.Clear()
        If _criteria.EndDate IsNot Nothing Then
            sqlText = String.Format(sqlText, "AND A.EFFDT <= :DateTo {0}")
            cmd.Parameters.Add(New OracleParameter With {.ParameterName = "DateTo", .Value = _criteria.EndDate})
        End If
        'If _criteria.StartDate IsNot Nothing AndAlso _criteria.EndDate IsNot Nothing Then
        '    sqlText = String.Format(sqlText, "AND A.EFFDT BETWEEN :DateFrom AND :DateTo {0}")
        '    cmd.Parameters.Add(New OracleParameter With {.ParameterName = "DateFrom", .Value = _criteria.StartDate})
        '    cmd.Parameters.Add(New OracleParameter With {.ParameterName = "DateTo", .Value = _criteria.EndDate})
        'ElseIf _criteria.StartDate IsNot Nothing Then
        '    sqlText = String.Format(sqlText, "AND A.EFFDT >= :DateFrom {0}")
        '    cmd.Parameters.Add(New OracleParameter With {.ParameterName = "DateFrom", .Value = _criteria.StartDate})
        'ElseIf _criteria.EndDate IsNot Nothing Then
        '    sqlText = String.Format(sqlText, "AND A.EFFDT <= :DateTo {0}")
        '    cmd.Parameters.Add(New OracleParameter With {.ParameterName = "DateTo", .Value = _criteria.EndDate})
        'End If

        'cmd.Parameters.Clear()
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_EMPLCLASS", .Value = If(String.IsNullOrEmpty(_criteria.EmplClass), "", _criteria.EmplClass)})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "C_COMP", .Value = _criteria.Company})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "PAYORGCODE", .Value = If(_criteria.Org Is Nothing, "", _criteria.Org.Relateid)})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "EMPLFLAG", .Value = _criteria.EmplFlag})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "MANAGERLVL", .Value = If(String.IsNullOrEmpty(_criteria.Managerlvl), "", _criteria.Managerlvl)})
        cmd.Parameters.Add(New OracleParameter With {.ParameterName = "PERIODMASTID", .Value = _criteria.PeriodMastId.Periodmastid})

        Dim authenstr = String.Format("AND A.DEPTID IN ({0}) ", dpBp.StringAuthenDepartment(param.UserName, "A.DEPTID", col, Today))
        cmd.CommandText = String.Format(sqlText, authenstr)


        Dim cmdEE As OracleCommand = con.CreateCommand
        cmdEE.BindByName = True
        sqlText = "UPDATE INTERFACETEMP_EE A SET A.ISINTERFACE = 'Y' " & _
                        "WHERE A.ISINTERFACE = 'N' AND A.EMPLFLAG = NVL (:EMPLFLAG, A.EMPLFLAG) AND A.MANAGER_LEVEL = NVL (:MANAGERLVL, A.MANAGER_LEVEL) AND A.EMPL_CLASS = NVL (:EMPLCLASS, A.EMPL_CLASS)   " & _
                        "AND A.COMPANY = :COMP AND A.PAYORGCODE like NVL(:PAYORGCODE || '%',A.PAYORGCODE) AND A.PERIODMASTID = :PERIODMASTID  {0} "
        If _criteria.chkAll Then
            sqlText += " AND A.EMPLID = NVL ('" & Criteria_Emplid & "', A.EMPLID) "
        Else
            sqlText += " AND A.EMPLID IN (" & Criteria_Emplid & ")"
        End If

        cmdEE.Parameters.Clear()
        If _criteria.EndDate IsNot Nothing Then
            sqlText = String.Format(sqlText, "AND A.EFFDT <= :DateTo {0}")
            cmdEE.Parameters.Add(New OracleParameter With {.ParameterName = "DateTo", .Value = _criteria.EndDate})
        End If
        'If _criteria.StartDate IsNot Nothing AndAlso _criteria.EndDate IsNot Nothing Then
        '    sqlText = String.Format(sqlText, "AND A.EFFDT BETWEEN :DateFrom AND :DateTo {0}")
        '    cmdEE.Parameters.Add(New OracleParameter With {.ParameterName = "DateFrom", .Value = _criteria.StartDate})
        '    cmdEE.Parameters.Add(New OracleParameter With {.ParameterName = "DateTo", .Value = _criteria.EndDate})
        'ElseIf _criteria.StartDate IsNot Nothing Then
        '    sqlText = String.Format(sqlText, "AND A.EFFDT >= :DateFrom {0}")
        '    cmdEE.Parameters.Add(New OracleParameter With {.ParameterName = "DateFrom", .Value = _criteria.StartDate})
        'ElseIf _criteria.EndDate IsNot Nothing Then
        '    sqlText = String.Format(sqlText, "AND A.EFFDT <= :DateTo {0}")
        '    cmdEE.Parameters.Add(New OracleParameter With {.ParameterName = "DateTo", .Value = _criteria.EndDate})
        'End If

        'cmdEE.Parameters.Clear()
        cmdEE.Parameters.Add(New OracleParameter With {.ParameterName = "EMPLFLAG", .Value = If(_criteria.EmplFlag = "A", "", _criteria.EmplFlag)})
        cmdEE.Parameters.Add(New OracleParameter With {.ParameterName = "MANAGERLVL", .Value = If(String.IsNullOrEmpty(_criteria.Managerlvl), String.Empty, _criteria.Managerlvl)})
        cmdEE.Parameters.Add(New OracleParameter With {.ParameterName = "EMPLCLASS", .Value = If(String.IsNullOrEmpty(_criteria.EmplClass), String.Empty, _criteria.EmplClass)})
        cmdEE.Parameters.Add(New OracleParameter With {.ParameterName = "COMP", .Value = _criteria.Company})
        cmdEE.Parameters.Add(New OracleParameter With {.ParameterName = "PAYORGCODE", .Value = If(_criteria.Org Is Nothing, "", _criteria.Org.Relateid)})
        cmdEE.Parameters.Add(New OracleParameter With {.ParameterName = "PERIODMASTID", .Value = _criteria.PeriodMastId.Periodmastid})
        cmdEE.CommandText = String.Format(sqlText, authenstr)

        Dim cmdCompen As OracleCommand = con.CreateCommand
        cmdCompen.BindByName = True
        sqlText = "UPDATE INTERFACETEMP_COMPEN A  SET A.ISINTERFACE = 'Y'" & _
                  "WHERE  A.ISINTERFACE = 'N' AND A.EMPLFLAG = NVL (:EMPLFLAG, A.EMPLFLAG) AND A.MANAGER_LEVEL = NVL (:MANAGERLVL, A.MANAGER_LEVEL) AND A.EMPL_CLASS = NVL (:EMPLCLASS, A.EMPL_CLASS)   " & _
                  "AND A.COMPANY = :COMP AND A.PERIODMASTID = :PERIODMASTID {0} "
        If _criteria.chkAll Then
            sqlText += " AND A.EMPLID = NVL ('" & Criteria_Emplid & "', A.EMPLID) "
        Else
            sqlText += " AND A.EMPLID IN (" & Criteria_Emplid & ")"
        End If

        cmdCompen.Parameters.Clear()
        If _criteria.EndDate IsNot Nothing Then
            sqlText = String.Format(sqlText, "AND A.EFFDT <= :DateTo {0}")
            cmdCompen.Parameters.Add(New OracleParameter With {.ParameterName = "DateTo", .Value = _criteria.EndDate})
        End If
        'If _criteria.StartDate IsNot Nothing AndAlso _criteria.EndDate IsNot Nothing Then
        '    sqlText = String.Format(sqlText, "AND A.EFFDT BETWEEN :DateFrom AND :DateTo {0}")
        '    cmdCompen.Parameters.Add(New OracleParameter With {.ParameterName = "DateFrom", .Value = _criteria.StartDate})
        '    cmdCompen.Parameters.Add(New OracleParameter With {.ParameterName = "DateTo", .Value = _criteria.EndDate})
        'ElseIf _criteria.StartDate IsNot Nothing Then
        '    sqlText = String.Format(sqlText, "AND A.EFFDT >= :DateFrom {0}")
        '    cmdCompen.Parameters.Add(New OracleParameter With {.ParameterName = "DateFrom", .Value = _criteria.StartDate})
        'ElseIf _criteria.EndDate IsNot Nothing Then
        '    sqlText = String.Format(sqlText, "AND A.EFFDT <= :DateTo {0}")
        '    cmdCompen.Parameters.Add(New OracleParameter With {.ParameterName = "DateTo", .Value = _criteria.EndDate})
        'End If

        'cmdCompen.Parameters.Clear()
        cmdCompen.Parameters.Add(New OracleParameter With {.ParameterName = "EMPLFLAG", .Value = If(_criteria.EmplFlag = "A", "", _criteria.EmplFlag)})
        cmdCompen.Parameters.Add(New OracleParameter With {.ParameterName = "MANAGERLVL", .Value = If(String.IsNullOrEmpty(_criteria.Managerlvl), String.Empty, _criteria.Managerlvl)})
        cmdCompen.Parameters.Add(New OracleParameter With {.ParameterName = "EMPLCLASS", .Value = If(String.IsNullOrEmpty(_criteria.EmplClass), String.Empty, _criteria.EmplClass)})
        cmdCompen.Parameters.Add(New OracleParameter With {.ParameterName = "COMP", .Value = _criteria.Company})
        cmdCompen.Parameters.Add(New OracleParameter With {.ParameterName = "PERIODMASTID", .Value = _criteria.PeriodMastId.Periodmastid})
        cmdCompen.CommandText = String.Format(sqlText, authenstr)


        Dim cmdInterfaceEE As OracleCommand = con.CreateCommand
        cmdInterfaceEE.BindByName = True
        sqlText = "UPDATE INTERFACE_EE A SET A.ISINTERFACE = 'Y' " & _
                  "WHERE A.ISINTERFACE = 'N' AND A.EMPLFLAG = NVL (:EMPLFLAG, A.EMPLFLAG) AND A.MANAGER_LEVEL = NVL (:MANAGERLVL, A.MANAGER_LEVEL) AND A.EMPL_CLASS = NVL (:EMPLCLASS, A.EMPL_CLASS)   " & _
                  "AND A.COMPANY = :COMP AND A.PAYORGCODE like NVL(:PAYORGCODE || '%',A.PAYORGCODE) AND A.PERIODMASTID = :PERIODMASTID  {0} "
        If _criteria.chkAll Then
            sqlText += " AND A.EMPLID = NVL ('" & Criteria_Emplid & "', A.EMPLID) "
        Else
            sqlText += " AND A.EMPLID IN (" & Criteria_Emplid & ") "
        End If

        cmdInterfaceEE.Parameters.Clear()
        If _criteria.EndDate IsNot Nothing Then
            sqlText = String.Format(sqlText, "AND A.EFFDT <= :DateTo {0}")
            cmdInterfaceEE.Parameters.Add(New OracleParameter With {.ParameterName = "DateTo", .Value = _criteria.EndDate})
        End If
        'If _criteria.StartDate IsNot Nothing AndAlso _criteria.EndDate IsNot Nothing Then
        '    sqlText = String.Format(sqlText, "AND A.EFFDT BETWEEN :DateFrom AND :DateTo {0}")
        '    cmdInterfaceEE.Parameters.Add(New OracleParameter With {.ParameterName = "DateFrom", .Value = _criteria.StartDate})
        '    cmdInterfaceEE.Parameters.Add(New OracleParameter With {.ParameterName = "DateTo", .Value = _criteria.EndDate})
        'ElseIf _criteria.StartDate IsNot Nothing Then
        '    sqlText = String.Format(sqlText, "AND A.EFFDT >= :DateFrom {0}")
        '    cmdInterfaceEE.Parameters.Add(New OracleParameter With {.ParameterName = "DateFrom", .Value = _criteria.StartDate})
        'ElseIf _criteria.EndDate IsNot Nothing Then
        '    sqlText = String.Format(sqlText, "AND A.EFFDT <= :DateTo {0}")
        '    cmdInterfaceEE.Parameters.Add(New OracleParameter With {.ParameterName = "DateTo", .Value = _criteria.EndDate})
        'End If

        'cmdInterfaceEE.Parameters.Clear()
        cmdInterfaceEE.Parameters.Add(New OracleParameter With {.ParameterName = "EMPLFLAG", .Value = If(_criteria.EmplFlag = "A", "", _criteria.EmplFlag)})
        cmdInterfaceEE.Parameters.Add(New OracleParameter With {.ParameterName = "MANAGERLVL", .Value = If(String.IsNullOrEmpty(_criteria.Managerlvl), String.Empty, _criteria.Managerlvl)})
        cmdInterfaceEE.Parameters.Add(New OracleParameter With {.ParameterName = "EMPLCLASS", .Value = If(String.IsNullOrEmpty(_criteria.EmplClass), String.Empty, _criteria.EmplClass)})
        cmdInterfaceEE.Parameters.Add(New OracleParameter With {.ParameterName = "COMP", .Value = _criteria.Company})
        cmdInterfaceEE.Parameters.Add(New OracleParameter With {.ParameterName = "PAYORGCODE", .Value = If(_criteria.Org Is Nothing, "", _criteria.Org.Relateid)})
        cmdInterfaceEE.Parameters.Add(New OracleParameter With {.ParameterName = "PERIODMASTID", .Value = _criteria.PeriodMastId.Periodmastid})
        cmdInterfaceEE.CommandText = String.Format(sqlText, authenstr)


        Dim cmdInterfaceCompen As OracleCommand = con.CreateCommand
        cmdInterfaceCompen.BindByName = True
        sqlText = "UPDATE INTERFACE_COMPEN B  SET B.ISINTERFACE = 'Y'" & _
                  "WHERE  B.ISINTERFACE = 'N' " & _
                  "AND EXISTS " & _
                  "(SELECT 1 FROM INTERFACETEMP_COMPEN A " & _
                  " WHERE A.ISINTERFACE = 'N' AND A.EMPLFLAG = NVL (:EMPLFLAG, A.EMPLFLAG) " & _
                  " AND A.MANAGER_LEVEL = NVL (:MANAGERLVL, A.MANAGER_LEVEL) AND A.EMPL_CLASS = NVL (:EMPLCLASS, A.EMPL_CLASS)   " & _
                  " AND A.COMPANY = :COMP AND A.PERIODMASTID = :PERIODMASTID {0}  "
        If _criteria.chkAll Then
            sqlText += " AND A.EMPLID = NVL ('" & Criteria_Emplid & "', A.EMPLID) "
        Else
            sqlText += " AND A.EMPLID IN (" & Criteria_Emplid & ")"
        End If
        sqlText += " ) "

        cmdInterfaceCompen.Parameters.Clear()
        If _criteria.EndDate IsNot Nothing Then
            sqlText = String.Format(sqlText, "AND A.EFFDT <= :DateTo {0}")
            cmdInterfaceCompen.Parameters.Add(New OracleParameter With {.ParameterName = "DateTo", .Value = _criteria.EndDate})
        End If
        'If _criteria.StartDate IsNot Nothing AndAlso _criteria.EndDate IsNot Nothing Then
        '    sqlText = String.Format(sqlText, "AND A.EFFDT BETWEEN :DateFrom AND :DateTo {0}")
        '    cmdInterfaceCompen.Parameters.Add(New OracleParameter With {.ParameterName = "DateFrom", .Value = _criteria.StartDate})
        '    cmdInterfaceCompen.Parameters.Add(New OracleParameter With {.ParameterName = "DateTo", .Value = _criteria.EndDate})
        'ElseIf _criteria.StartDate IsNot Nothing Then
        '    sqlText = String.Format(sqlText, "AND A.EFFDT >= :DateFrom {0}")
        '    cmdInterfaceCompen.Parameters.Add(New OracleParameter With {.ParameterName = "DateFrom", .Value = _criteria.StartDate})
        'ElseIf _criteria.EndDate IsNot Nothing Then
        '    sqlText = String.Format(sqlText, "AND A.EFFDT <= :DateTo {0}")
        '    cmdInterfaceCompen.Parameters.Add(New OracleParameter With {.ParameterName = "DateTo", .Value = _criteria.EndDate})
        'End If

        'cmdInterfaceCompen.Parameters.Clear()
        cmdInterfaceCompen.Parameters.Add(New OracleParameter With {.ParameterName = "EMPLFLAG", .Value = If(_criteria.EmplFlag = "A", "", _criteria.EmplFlag)})
        cmdInterfaceCompen.Parameters.Add(New OracleParameter With {.ParameterName = "MANAGERLVL", .Value = If(String.IsNullOrEmpty(_criteria.Managerlvl), String.Empty, _criteria.Managerlvl)})
        cmdInterfaceCompen.Parameters.Add(New OracleParameter With {.ParameterName = "EMPLCLASS", .Value = If(String.IsNullOrEmpty(_criteria.EmplClass), String.Empty, _criteria.EmplClass)})
        cmdInterfaceCompen.Parameters.Add(New OracleParameter With {.ParameterName = "COMP", .Value = _criteria.Company})
        cmdInterfaceCompen.Parameters.Add(New OracleParameter With {.ParameterName = "PERIODMASTID", .Value = _criteria.PeriodMastId.Periodmastid})
        cmdInterfaceCompen.CommandText = String.Format(sqlText, authenstr)

        Try
            If con.State = ConnectionState.Closed Then
                con.Open()
            End If
            Dim trn As System.Data.Common.DbTransaction = con.BeginTransaction()
            Try
                If TypeOf (dt) Is DataTable Then
                    Dim da As New OracleDataAdapter(cmd)
                    da.Fill(dt)
                End If

                cmdInterfaceEE.Transaction = trn
                cmdInterfaceEE.ExecuteNonQuery()

                cmdInterfaceCompen.Transaction = trn
                cmdInterfaceCompen.ExecuteNonQuery()
                cmdEE.Transaction = trn
                cmdEE.ExecuteNonQuery()

                cmdCompen.Transaction = trn
                cmdCompen.ExecuteNonQuery()

                trn.Commit()
            Catch ex As Exception
                trn.Rollback()
                Throw ex
            End Try
        Catch ex As Exception
            Throw ex
        Finally
            con.Close()
        End Try

        Dim nameFile As String = String.Empty

        Dim curPath As String = pathServer
        If curPath(curPath.Length - 1) <> "\" Then
            curPath = curPath & "\"
        End If

        Dim fileName As String = ""
        If dt IsNot Nothing AndAlso dt.Rows.Count > 0 Then
            '--------------------------------------------
            If _criteria.Org IsNot Nothing AndAlso (_criteria.Org.Relateid IsNot Nothing OrElse Not String.IsNullOrEmpty(_criteria.Org.Relateid)) Then
                '----------------------------------
                'Changed by Chanchira L. on 21/05/2021
                'Dim op = _criteria.Org.Relateid.Split(".")
                'fileName = "MS" & _criteria.Month & Today.ToString("yyyy") & _criteria.Company & op(1) & ".prn"
                If InStr(_criteria.Org.Relateid, ".") = 0 Then
                    fileName = "MS" & _criteria.Month & Today.ToString("yyyy") & _criteria.Company & ".prn"
                Else
                    Dim op = _criteria.Org.Relateid.Split(".")
                    fileName = "MS" & _criteria.Month & Today.ToString("yyyy") & _criteria.Company & op(1) & ".prn"
                End If
                '----------------------------------
            Else
                fileName = "MS" & _criteria.Month & Today.ToString("yyyy") & _criteria.Company & ".prn"
            End If

            Dim dtData As New System.Text.StringBuilder
            Dim i = 1
            For Each r In dt.Rows
                Dim st As New System.Text.StringBuilder
                For Each c In dt.Columns
                    If IsDBNull(r(c.columnName).ToString.Trim) OrElse String.IsNullOrEmpty(r(c.columnName).ToString.Trim) Then
                        st.Append(";")
                    Else
                        If TypeOf (r(c.columnName)) Is Date Then
                            st.Append(String.Format(r(c.columnName).ToString.Trim, formatdt).Trim & ";")
                        ElseIf TypeOf (r(c.columnName)) Is Integer OrElse TypeOf (r(c.columnName)) Is Long OrElse TypeOf (r(c.columnName)) Is Double Then
                            st.Append(r(c.columnName).ToString("#0.00") & ";")
                        Else
                            st.Append(r(c).ToString & ";")
                        End If
                    End If
                Next
                Right(st.ToString, 1)

                If i = dt.Rows.Count Then
                    dtData.Append(st.ToString)
                Else
                    dtData.AppendLine(st.ToString)
                End If

                i += 1
            Next

            Dim oFile As System.IO.FileInfo
            oFile = New System.IO.FileInfo(curPath & fileName.ToString)

            If System.IO.File.Exists(curPath & fileName.ToString) = False Then
                Try
                    Using writer As New System.IO.StreamWriter(curPath & fileName, True, System.Text.Encoding.UTF8)
                        writer.WriteLine(dtData.ToString)
                    End Using
                Catch ex As Exception
                    ret = "Export text file Uncompleted"
                End Try
            Else
                ret = "Payroll have not received text file yet"
            End If
        Else
            ret = "No data found"
        End If

        Return ret
    End Function

    Public Function CallToGenerateText(_criteria As Model.ModelCriteriaList, ByVal formatdt As String, ByVal pathServer As String, fileName As String, dtEmpList As DataTable) As String
        Dim ret As String = "Export text file Completed"
        Dim nameFile As String = String.Empty
        Dim dt As New DataTable
        Dim curPath As String = pathServer
        dt = dtEmpList

        If curPath(curPath.Length - 1) <> "\" Then
            curPath = curPath & "\"
        End If

        Dim TempName As String = fileName
        fileName = fileName & ".prn"

        'ลบไฟล์ .prn เดิมก่อน
        If System.IO.Directory.Exists(curPath & fileName) Then
            Dim aryItems() As String = Directory.GetFileSystemEntries(curPath & fileName)
            For Each strItem In aryItems
                If File.Exists(strItem) Then
                    File.Delete(strItem)
                End If
            Next
            System.IO.Directory.Delete(curPath & fileName)
        End If

        If dt IsNot Nothing AndAlso dt.Rows.Count > 0 Then

            Dim dtData As New System.Text.StringBuilder
            Dim i = 1
            For Each r In dt.Rows
                Dim st As New System.Text.StringBuilder
                For Each c In dt.Columns
                    If IsDBNull(r(c.columnName).ToString.Trim) OrElse String.IsNullOrEmpty(r(c.columnName).ToString.Trim) Then
                        st.Append(";")
                    Else
                        If TypeOf (r(c.columnName)) Is Date Then
                            st.Append(String.Format(r(c.columnName).ToString.Trim, formatdt).Trim & ";")
                        ElseIf TypeOf (r(c.columnName)) Is Integer OrElse TypeOf (r(c.columnName)) Is Long OrElse TypeOf (r(c.columnName)) Is Double Then
                            st.Append(r(c.columnName).ToString() & ";")
                        Else
                            st.Append(r(c).ToString & ";")
                        End If
                    End If
                Next
                Right(st.ToString, 1)

                If i = dt.Rows.Count Then
                    dtData.Append(st.ToString)
                Else
                    dtData.AppendLine(st.ToString)
                End If

                i += 1
            Next

            Dim oFile As System.IO.FileInfo
            oFile = New System.IO.FileInfo(curPath & fileName.ToString)

            If System.IO.File.Exists(curPath & fileName.ToString) = False Then
                Try
                    Using writer As New System.IO.StreamWriter(curPath & fileName, True, System.Text.Encoding.UTF8)
                        writer.WriteLine(dtData.ToString)
                    End Using
                Catch ex As Exception
                    ret = "Export text file Uncompleted"
                End Try
            Else
                ret = "Payroll have not received text file yet"
            End If
        Else
            ret = "No data found"
        End If

        Return ret
    End Function

    Private Sub ConvertBaseToFileAndWrite(responseBF64 As ModelBaseFileApi, ByVal pathServer As String)
        Try
            Dim TempName As String = responseBF64.parameter.RequestId
            Dim bytes As Byte() = Convert.FromBase64String(responseBF64.data)

            Dim curPath As String = pathServer
            If curPath(curPath.Length - 1) <> "\" Then
                curPath = curPath & "\"
            End If

            'ลบไฟล์เดิมก่อน
            If System.IO.Directory.Exists(curPath & TempName) Then
                Dim aryItems() As String = Directory.GetFileSystemEntries(curPath & TempName)
                For Each strItem In aryItems
                    If File.Exists(strItem) Then
                        File.Delete(strItem)
                    End If
                Next
                System.IO.Directory.Delete(curPath & TempName)
            End If

            If System.IO.File.Exists(curPath & TempName & ".zip") Then
                System.IO.File.Delete(curPath & TempName & ".zip")
            End If

            File.WriteAllBytes(curPath & TempName & ".zip", bytes)

            Dim zf = ZipFile.Read(curPath & TempName & ".zip")
            zf.ExtractAll(curPath & TempName)
            zf.Dispose()

            If System.IO.File.Exists(curPath & TempName & ".zip") Then
                System.IO.File.Delete(curPath & TempName & ".zip")
            End If

        Catch ex As Exception
            Throw ex
        End Try
        'Return dtEmpList
    End Sub

#End Region

End Class
