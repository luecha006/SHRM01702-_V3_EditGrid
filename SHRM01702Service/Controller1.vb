Imports System.Net
Imports System.Net.Http
Imports System.Net.HttpWebRequest
Imports Newtonsoft.Json
Imports SHRM01702Service.Model
Imports System.Net.Http.Headers
Imports System.Globalization

Public Class Controller
    Public Function CallGet_BaseFile64(_criteria As Model.ModelCriteriaList, ByVal access_token As String)
        Dim url = "https://apigwcldsh-uat.cpf.co.th/datanodeuat/test_smartpay"
        ' ... Use GetAsync to get the page data.

        Dim RequestId As String = "MS" & _criteria.Month & _criteria.Year & _criteria.Company & ".PNR"
        Dim parameterList As Dictionary(Of String, String) = New Dictionary(Of String, String)

        'parameterList.Add("RequestId", RequestId)
        'parameterList.Add("companyCode", _criteria.Company)
        'parameterList.Add("StartPeriod", _criteria.StartDate)
        'parameterList.Add("EndPeriod", _criteria.EndDate)
        'parameterList.Add("EmployeeID", "")
        'parameterList.Add("OP", "")
        'parameterList.Add("Address", "Y")
        'parameterList.Add("BookBank", "Y")
        'parameterList.Add("SSN", "Y")
        'parameterList.Add("BankBranch ", "Y")
        'parameterList.Add("TaxID", "Y")
        Dim StartDate As DateTime = DateTime.ParseExact(_criteria.Year, "dd/MM/yyyy", CultureInfo.InvariantCulture)
        Dim reformatted As String = StartDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)

        Dim builderURLs As UriBuilder = New UriBuilder("https://apigwcldsh-uat.cpf.co.th/datanodeuat/test_smartpay")
        builderURLs.Query = "RequestId=" & RequestId & " & companyCode=" & _criteria.Company & "&StartPeriod=2022-09-01&EndPeriod=2022-09-05&Address=Y&BookBank=Y&SSN=Y&BankBranch=Y&TaxID=Y"

        Dim authorization As New Chilkat.JsonObject
        authorization.AppendString("authorization", access_token)

        Using httpClient As HttpClient = New HttpClient()
            httpClient.DefaultRequestHeaders.Authorization = New AuthenticationHeaderValue(access_token)
            Using response As HttpResponseMessage = httpClient.GetAsync(url).Result
                Using content As HttpContent = response.Content
                    ' Get contents of page as a String.
                    Dim result As String = content.ReadAsStringAsync().Result
                    ' If data exists, print a substring.

                End Using
            End Using
        End Using
    End Function

    Public Function CallPost_AccessToken() As ModelAccessToken
        Try
            Dim url As String = "https://apigwcldsh-uat.cpf.co.th/datanodeuat/oauth2/token"

            Dim requestData As New Chilkat.JsonObject
            requestData.AppendString("client_secret", "SztULvDCYyQ60Fl4FwirkkhGU8Z11thh")
            requestData.AppendString("client_id", "rcrT7kL07jPYP1bGb4MTs79oqVPddTmL")
            requestData.AppendString("grant_type", "client_credentials")

            Dim httpClient As HttpClient = New HttpClient()
            Using httpContent As StringContent = New StringContent(requestData.ToString(), System.Text.Encoding.UTF8, "application/json")
                Using response As HttpResponseMessage = httpClient.PostAsync(url, httpContent).Result
                    If response.StatusCode = System.Net.HttpStatusCode.OK Then
                        Dim token = response.Content.ReadAsStringAsync().Result ' Save the token for further requests.
                        'Console.WriteLine(token)
                        Dim m_access_token As ModelAccessToken = JsonConvert.DeserializeObject(Of ModelAccessToken)(token)
                        'Console.WriteLine(emp.access_token)
                        Return m_access_token
                    Else
                        Dim m_access_token As ModelAccessToken = Nothing
                    End If
                End Using
            End Using
        Catch ex As Exception
            Console.WriteLine(ex.Message)
        End Try
    End Function

End Class
