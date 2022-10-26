Imports System
Imports System.Net.Http
Imports Newtonsoft.Json
Imports System.Net.Http.Headers
Imports System.Globalization
Imports SHRM01702Service.Model

Public Class Controller
    Public Function Call_BaseFile64(ByVal _criteria As Model.ModelCriteriaList, ByVal access_token As String)
        Try
            Dim requestId = "MS" & _criteria.Month & _criteria.Year & _criteria.Company

            Dim url As UriBuilder = New UriBuilder("https://apigwcldsh-uat.cpf.co.th/datanodeuat/test_smartpay")
            'url.Query = "RequestId=" & requestId & "&companyCode=" & _criteria.Company & "&StartPeriod=" & FormatDateTime(_criteria.StartDate) &
            '    "&EndPeriod=" & FormatDateTime(_criteria.EndDate) & "&&EmployeeID=" & _criteria.Emplid & "OP=&Address=Y&BookBank=Y&SSN=Y&BankBranch=Y&TaxID=Y"

            url.Query = "RequestId=" & requestId & "&companyCode=" & _criteria.Company & "&StartPeriod=2022-09-01&EndPeriod=2022-09-30&EmployeeID=10103001&OP=&Address=Y&BookBank=Y&SSN=Y&BankBranch=Y&TaxID=Y"

            Using httpClient As HttpClient = New HttpClient()
                httpClient.DefaultRequestHeaders.Authorization = New AuthenticationHeaderValue(access_token)

                Using httpContent As HttpResponseMessage = httpClient.GetAsync(url.ToString).Result
                    Using response As HttpContent = httpContent.Content
                        If httpContent.StatusCode = System.Net.HttpStatusCode.OK Then
                            Dim responseBody As String = response.ReadAsStringAsync().Result
                            Dim responseData As ModelBaseFileApi = JsonConvert.DeserializeObject(Of ModelBaseFileApi)(responseBody)

                            Return responseData
                        Else
                            Return Nothing
                        End If
                    End Using
                End Using
            End Using
        Catch ex As Exception
            Throw ex

        End Try
    End Function

    Public Function Call_AccessToken() As ModelAccessToken
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
                        Dim responseBody = response.Content.ReadAsStringAsync().Result ' Save the token for further requests.
                        'Console.WriteLine(token)
                        Dim responseObject As ModelAccessToken = JsonConvert.DeserializeObject(Of ModelAccessToken)(responseBody)
                        'Console.WriteLine(emp.access_token)
                        Return responseObject
                    Else
                        Return Nothing
                    End If
                End Using
            End Using

        Catch ex As Exception
            Throw ex
        End Try
    End Function

    Public Function FormatDateTime(ByVal strDate As Date) As String
        Return strDate.ToString("yyyy-MM-dd", New CultureInfo("en-US"))
    End Function

End Class
