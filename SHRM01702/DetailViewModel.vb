Imports System.Collections.ObjectModel
Imports SHRM01702.SHRM01702Ref
Imports SsHrCommon.DTO
Imports SsAppCommon
Imports SsSilverlight

Public Class DetailViewModel
    Inherits ViewModelBase


#Region "Property"

    Private _AsOfDate As Date = Today
    Public Property AsOfDate() As Date
        Get
            Return _AsOfDate
        End Get
        Set(ByVal value As Date)
            _AsOfDate = value
            OnPropertyChanged("AsOfDate")
        End Set
    End Property

    Private _PeriodTimecbo As New List(Of Integer)
    Public ReadOnly Property PeriodTimecbo() As List(Of Integer)
        Get
            _PeriodTimecbo.Add(1)
            _PeriodTimecbo.Add(2)
            _PeriodTimecbo.Add(3)
            _PeriodTimecbo.Add(4)
            Return _PeriodTimecbo
        End Get
    End Property

    Private _modelList As New ObservableCollection(Of ModelInterfacetempEe)
    Public Property ModelList() As ObservableCollection(Of ModelInterfacetempEe)
        Get
            Return _modelList
        End Get
        Set(ByVal value As ObservableCollection(Of ModelInterfacetempEe))
            _modelList = value
            OnPropertyChanged("ModelList")
        End Set
    End Property

    Private _cri As New ModelCriteriaList
    Public Property Criteria() As ModelCriteriaList
        Get
            Return _cri
        End Get
        Set(ByVal value As ModelCriteriaList)
            _cri = value
            OnPropertyChanged("Criteria")
        End Set
    End Property

    Private _NewEmplNum As Integer = 0
    Public Property NewEmplNum() As Integer
        Get
            Return _NewEmplNum
        End Get
        Set(ByVal value As Integer)
            _NewEmplNum = value
            OnPropertyChanged("NewEmplNum")
        End Set
    End Property

    Private _ReEmplNum As Integer = 0
    Public Property ReEmplNum() As Integer
        Get
            Return _ReEmplNum
        End Get
        Set(ByVal value As Integer)
            _ReEmplNum = value
            OnPropertyChanged("ReEmplNum")
        End Set
    End Property
    Private _UpEmplNum As Integer = 0
    Public Property UpEmplNum() As Integer
        Get
            Return _UpEmplNum
        End Get
        Set(ByVal value As Integer)
            _UpEmplNum = value
            OnPropertyChanged("UpEmplNum")
        End Set
    End Property
    Private _TranEmplNum As Integer = 0
    Public Property TranEmplNum() As Integer
        Get
            Return _TranEmplNum
        End Get
        Set(ByVal value As Integer)
            _TranEmplNum = value
            OnPropertyChanged("TranEmplNum")
        End Set
    End Property

    Private _periodMastLst As New ObservableCollection(Of ModelPyperiodmaster)
    Public Property PeriodMastLst() As ObservableCollection(Of ModelPyperiodmaster)
        Get
            Return _periodMastLst
        End Get
        Set(ByVal value As ObservableCollection(Of ModelPyperiodmaster))
            _periodMastLst = value
            OnPropertyChanged("PeriodMastLst")
        End Set
    End Property

    Private _OrgLst As New ObservableCollection(Of SsHrCommon.DTO.Pyorganize)
    Public Property OrgLst() As ObservableCollection(Of SsHrCommon.DTO.Pyorganize)
        Get
            Return _OrgLst
        End Get
        Set(ByVal value As ObservableCollection(Of SsHrCommon.DTO.Pyorganize))
            _OrgLst = value
            OnPropertyChanged("OrgLst")
        End Set
    End Property

    Private _PeriodObj As New SsHrCommon.DTO.Pyperiod
    Public Property PeriodObj() As SsHrCommon.DTO.Pyperiod
        Get
            Return _PeriodObj
        End Get
        Set(ByVal value As SsHrCommon.DTO.Pyperiod)
            _PeriodObj = value
            OnPropertyChanged("PeriodObj")
        End Set
    End Property

    Public Property ErrorMsg() As String

    'Add by Chanchira L. on 10/03/2020
    Private _CountRemark As Integer = 0
    Public Property CountRemark() As Integer
        Get
            Return _CountRemark
        End Get
        Set(ByVal value As Integer)
            _CountRemark = value
            OnPropertyChanged("CountRemark")
        End Set
    End Property

#End Region

End Class
