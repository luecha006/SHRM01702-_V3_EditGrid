﻿#ExternalChecksum("D:\CPF\dev\SHRM01702 _V3_EditGrid\SHRM01702\DetailView.xaml","{406ea660-64cf-4c82-b6f0-42d48172a799}","1F2E754B2A7EFD6995E5C71DBABDAAEE")
'------------------------------------------------------------------------------
' <auto-generated>
'     This code was generated by a tool.
'     Runtime Version:4.0.30319.42000
'
'     Changes to this file may cause incorrect behavior and will be lost if
'     the code is regenerated.
' </auto-generated>
'------------------------------------------------------------------------------

Option Strict Off
Option Explicit On

Imports SsHrCommon
Imports SsSilverlight
Imports System
Imports System.Windows
Imports System.Windows.Automation
Imports System.Windows.Automation.Peers
Imports System.Windows.Automation.Provider
Imports System.Windows.Controls
Imports System.Windows.Controls.Primitives
Imports System.Windows.Data
Imports System.Windows.Documents
Imports System.Windows.Ink
Imports System.Windows.Input
Imports System.Windows.Interop
Imports System.Windows.Markup
Imports System.Windows.Media
Imports System.Windows.Media.Animation
Imports System.Windows.Media.Imaging
Imports System.Windows.Resources
Imports System.Windows.Shapes
Imports System.Windows.Threading



<Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>  _
Partial Public Class DetailView
    Inherits System.Windows.Controls.UserControl
    
    Friend WithEvents Uc As System.Windows.Controls.UserControl
    
    Friend WithEvents busyIndicator As Telerik.Windows.Controls.RadBusyIndicator
    
    Friend WithEvents btnSearch As Telerik.Windows.Controls.RadButton
    
    Friend WithEvents btnNewGenText As Telerik.Windows.Controls.RadButton
    
    Friend WithEvents btnSave As Telerik.Windows.Controls.RadButton
    
    Friend WithEvents btnGenText As Telerik.Windows.Controls.RadButton
    
    Friend WithEvents btnExcel As System.Windows.Controls.Button
    
    Friend WithEvents btnCancel As Telerik.Windows.Controls.RadButton
    
    Friend WithEvents btnClose As Telerik.Windows.Controls.RadButton
    
    Friend WithEvents expMain As Telerik.Windows.Controls.RadExpander
    
    Friend WithEvents stpLine2 As System.Windows.Controls.StackPanel
    
    Friend WithEvents lblYear As System.Windows.Controls.Label
    
    Friend WithEvents txtYear As System.Windows.Controls.TextBox
    
    Friend WithEvents lblMonth As System.Windows.Controls.Label
    
    Friend WithEvents cboMonth As SsHrCommon.GDComboBox
    
    Friend WithEvents lblPayDate As System.Windows.Controls.Label
    
    Friend WithEvents cboPayDate As Telerik.Windows.Controls.RadComboBox
    
    Friend WithEvents stpSpecDate As System.Windows.Controls.StackPanel
    
    Friend WithEvents lblFromDate As System.Windows.Controls.Label
    
    Friend WithEvents dpkFromdt As SsSilverlight.CDatePicker
    
    Friend WithEvents lblToDate As System.Windows.Controls.Label
    
    Friend WithEvents dpkTodt As SsSilverlight.CDatePicker
    
    Friend WithEvents stpLine11 As System.Windows.Controls.StackPanel
    
    Friend WithEvents lblCompany As System.Windows.Controls.Label
    
    Friend WithEvents lkCompany As SsHrCommon.CompanyLookUp
    
    Friend WithEvents lblCompDescr As System.Windows.Controls.Label
    
    Friend WithEvents stpLine12 As System.Windows.Controls.StackPanel
    
    Friend WithEvents lblPeriodMastid As System.Windows.Controls.Label
    
    Friend WithEvents cboPeriodMastId As Telerik.Windows.Controls.RadComboBox
    
    Friend WithEvents stpLine31 As System.Windows.Controls.StackPanel
    
    Friend WithEvents lblEmplFlag As System.Windows.Controls.Label
    
    Friend WithEvents cboEmplFlag As SsHrCommon.GDComboBox
    
    Friend WithEvents stpLine41 As System.Windows.Controls.StackPanel
    
    Friend WithEvents lblEmpl As System.Windows.Controls.Label
    
    Friend WithEvents lkEmployee As SsHrCommon.JobEeLookUp
    
    Friend WithEvents txbEmplName As System.Windows.Controls.TextBlock
    
    Friend WithEvents stpNewEmpl As System.Windows.Controls.StackPanel
    
    Friend WithEvents lblRemark As System.Windows.Controls.Label
    
    Friend WithEvents lblCountRemark As System.Windows.Controls.Label
    
    Friend WithEvents lblRemarkRecord As System.Windows.Controls.Label
    
    Friend WithEvents gridShowGrid As System.Windows.Controls.Grid
    
    Friend WithEvents spShowMsg As System.Windows.Controls.StackPanel
    
    Friend WithEvents txbStr1 As System.Windows.Controls.TextBlock
    
    Friend WithEvents txbStr2 As System.Windows.Controls.TextBlock
    
    Friend WithEvents dgEmpl As Telerik.Windows.Controls.RadGridView
    
    Friend WithEvents colEmplID As Telerik.Windows.Controls.GridViewDataColumn
    
    Private _contentLoaded As Boolean
    
    '''<summary>
    '''InitializeComponent
    '''</summary>
    <System.Diagnostics.DebuggerNonUserCodeAttribute()>  _
    Public Sub InitializeComponent()
        If _contentLoaded Then
            Return
        End If
        _contentLoaded = true
        System.Windows.Application.LoadComponent(Me, New System.Uri("/SHRM01702;component/DetailView.xaml", System.UriKind.Relative))
        Me.Uc = CType(Me.FindName("Uc"),System.Windows.Controls.UserControl)
        Me.busyIndicator = CType(Me.FindName("busyIndicator"),Telerik.Windows.Controls.RadBusyIndicator)
        Me.btnSearch = CType(Me.FindName("btnSearch"),Telerik.Windows.Controls.RadButton)
        Me.btnNewGenText = CType(Me.FindName("btnNewGenText"),Telerik.Windows.Controls.RadButton)
        Me.btnSave = CType(Me.FindName("btnSave"),Telerik.Windows.Controls.RadButton)
        Me.btnGenText = CType(Me.FindName("btnGenText"),Telerik.Windows.Controls.RadButton)
        Me.btnExcel = CType(Me.FindName("btnExcel"),System.Windows.Controls.Button)
        Me.btnCancel = CType(Me.FindName("btnCancel"),Telerik.Windows.Controls.RadButton)
        Me.btnClose = CType(Me.FindName("btnClose"),Telerik.Windows.Controls.RadButton)
        Me.expMain = CType(Me.FindName("expMain"),Telerik.Windows.Controls.RadExpander)
        Me.stpLine2 = CType(Me.FindName("stpLine2"),System.Windows.Controls.StackPanel)
        Me.lblYear = CType(Me.FindName("lblYear"),System.Windows.Controls.Label)
        Me.txtYear = CType(Me.FindName("txtYear"),System.Windows.Controls.TextBox)
        Me.lblMonth = CType(Me.FindName("lblMonth"),System.Windows.Controls.Label)
        Me.cboMonth = CType(Me.FindName("cboMonth"),SsHrCommon.GDComboBox)
        Me.lblPayDate = CType(Me.FindName("lblPayDate"),System.Windows.Controls.Label)
        Me.cboPayDate = CType(Me.FindName("cboPayDate"),Telerik.Windows.Controls.RadComboBox)
        Me.stpSpecDate = CType(Me.FindName("stpSpecDate"),System.Windows.Controls.StackPanel)
        Me.lblFromDate = CType(Me.FindName("lblFromDate"),System.Windows.Controls.Label)
        Me.dpkFromdt = CType(Me.FindName("dpkFromdt"),SsSilverlight.CDatePicker)
        Me.lblToDate = CType(Me.FindName("lblToDate"),System.Windows.Controls.Label)
        Me.dpkTodt = CType(Me.FindName("dpkTodt"),SsSilverlight.CDatePicker)
        Me.stpLine11 = CType(Me.FindName("stpLine11"),System.Windows.Controls.StackPanel)
        Me.lblCompany = CType(Me.FindName("lblCompany"),System.Windows.Controls.Label)
        Me.lkCompany = CType(Me.FindName("lkCompany"),SsHrCommon.CompanyLookUp)
        Me.lblCompDescr = CType(Me.FindName("lblCompDescr"),System.Windows.Controls.Label)
        Me.stpLine12 = CType(Me.FindName("stpLine12"),System.Windows.Controls.StackPanel)
        Me.lblPeriodMastid = CType(Me.FindName("lblPeriodMastid"),System.Windows.Controls.Label)
        Me.cboPeriodMastId = CType(Me.FindName("cboPeriodMastId"),Telerik.Windows.Controls.RadComboBox)
        Me.stpLine31 = CType(Me.FindName("stpLine31"),System.Windows.Controls.StackPanel)
        Me.lblEmplFlag = CType(Me.FindName("lblEmplFlag"),System.Windows.Controls.Label)
        Me.cboEmplFlag = CType(Me.FindName("cboEmplFlag"),SsHrCommon.GDComboBox)
        Me.stpLine41 = CType(Me.FindName("stpLine41"),System.Windows.Controls.StackPanel)
        Me.lblEmpl = CType(Me.FindName("lblEmpl"),System.Windows.Controls.Label)
        Me.lkEmployee = CType(Me.FindName("lkEmployee"),SsHrCommon.JobEeLookUp)
        Me.txbEmplName = CType(Me.FindName("txbEmplName"),System.Windows.Controls.TextBlock)
        Me.stpNewEmpl = CType(Me.FindName("stpNewEmpl"),System.Windows.Controls.StackPanel)
        Me.lblRemark = CType(Me.FindName("lblRemark"),System.Windows.Controls.Label)
        Me.lblCountRemark = CType(Me.FindName("lblCountRemark"),System.Windows.Controls.Label)
        Me.lblRemarkRecord = CType(Me.FindName("lblRemarkRecord"),System.Windows.Controls.Label)
        Me.gridShowGrid = CType(Me.FindName("gridShowGrid"),System.Windows.Controls.Grid)
        Me.spShowMsg = CType(Me.FindName("spShowMsg"),System.Windows.Controls.StackPanel)
        Me.txbStr1 = CType(Me.FindName("txbStr1"),System.Windows.Controls.TextBlock)
        Me.txbStr2 = CType(Me.FindName("txbStr2"),System.Windows.Controls.TextBlock)
        Me.dgEmpl = CType(Me.FindName("dgEmpl"),Telerik.Windows.Controls.RadGridView)
        Me.colEmplID = CType(Me.FindName("colEmplID"),Telerik.Windows.Controls.GridViewDataColumn)
    End Sub
End Class
