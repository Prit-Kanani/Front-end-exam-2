using Front_end_exam.Models;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Front_end_exam.Services;

public sealed record ApiResult<T>(bool Success, T? Data, string? Error, HttpStatusCode StatusCode)
{
    public static ApiResult<T> SuccessResult(T data, HttpStatusCode statusCode) => new(true, data, null, statusCode);
    public static ApiResult<T> FailureResult(string error, HttpStatusCode statusCode) => new(false, default, error, statusCode);
}

#region API Client Interface
public interface IClinicCmsApiClient
{
    Task<ApiResult<LoginResponse>> LoginAsync(LoginRequest request);
    Task<ApiResult<ClinicInfo>> GetClinicAsync(string token);
    Task<ApiResult<IReadOnlyList<ClinicUser>>> GetUsersAsync(string token);
    Task<ApiResult<ClinicUser>> CreateUserAsync(string token, AdminCreateUserRequest request);
    Task<ApiResult<IReadOnlyList<QueueEntry>>> GetQueueAsync(string token, string date);
    Task<ApiResult<QueueEntry>> UpdateQueueStatusAsync(string token, int queueId, QueueStatusUpdateRequest request);
    Task<ApiResult<PatientAppointment>> BookAppointmentAsync(string token, BookAppointmentRequest request);
    Task<ApiResult<IReadOnlyList<PatientAppointment>>> GetMyAppointmentsAsync(string token);
    Task<ApiResult<PatientAppointmentDetail>> GetAppointmentDetailAsync(string token, int appointmentId);
    Task<ApiResult<IReadOnlyList<PatientPrescription>>> GetMyPrescriptionsAsync(string token);
    Task<ApiResult<IReadOnlyList<PatientReport>>> GetMyReportsAsync(string token);
}

#endregion

#region API Client Implementation
public sealed class ClinicCmsApiClient : IClinicCmsApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
    private readonly HttpClient _httpClient;

    public ClinicCmsApiClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(configuration.GetConnectionString("BackendLink") ?? "https://cmsback.sampaarsh.cloud/");
        _httpClient.Timeout = TimeSpan.FromSeconds(20);
    }

    public async Task<ApiResult<LoginResponse>> LoginAsync(LoginRequest request)
    {
        var response = await _httpClient.PostAsync("auth/login", CreateJsonContent(request));
        return await ReadResponseAsync<LoginResponse>(response);
    }

    public async Task<ApiResult<ClinicInfo>> GetClinicAsync(string token)
    {
        var request = CreateAuthorizedRequest(HttpMethod.Get, "admin/clinic", token);
        var response = await _httpClient.SendAsync(request);
        return await ReadResponseAsync<ClinicInfo>(response);
    }

    public async Task<ApiResult<IReadOnlyList<ClinicUser>>> GetUsersAsync(string token)
    {
        var request = CreateAuthorizedRequest(HttpMethod.Get, "admin/users", token);
        var response = await _httpClient.SendAsync(request);
        return await ReadResponseAsync<IReadOnlyList<ClinicUser>>(response);
    }

    public async Task<ApiResult<ClinicUser>> CreateUserAsync(string token, AdminCreateUserRequest request)
    {
        var httpRequest = CreateAuthorizedRequest(HttpMethod.Post, "admin/users", token);
        httpRequest.Content = CreateJsonContent(request);
        var response = await _httpClient.SendAsync(httpRequest);
        return await ReadResponseAsync<ClinicUser>(response);
    }

    public async Task<ApiResult<IReadOnlyList<QueueEntry>>> GetQueueAsync(string token, string date)
    {
        var request = CreateAuthorizedRequest(HttpMethod.Get, $"queue?date={Uri.EscapeDataString(date)}", token);
        var response = await _httpClient.SendAsync(request);
        return await ReadResponseAsync<IReadOnlyList<QueueEntry>>(response);
    }

    public async Task<ApiResult<QueueEntry>> UpdateQueueStatusAsync(string token, int queueId, QueueStatusUpdateRequest request)
    {
        var httpRequest = CreateAuthorizedRequest(HttpMethod.Patch, $"queue/{queueId}", token);
        httpRequest.Content = CreateJsonContent(request);
        var response = await _httpClient.SendAsync(httpRequest);
        return await ReadResponseAsync<QueueEntry>(response);
    }

    public async Task<ApiResult<PatientAppointment>> BookAppointmentAsync(string token, BookAppointmentRequest request)
    {
        var httpRequest = CreateAuthorizedRequest(HttpMethod.Post, "appointments", token);
        httpRequest.Content = CreateJsonContent(request);
        var response = await _httpClient.SendAsync(httpRequest);
        return await ReadResponseAsync<PatientAppointment>(response);
    }

    public async Task<ApiResult<IReadOnlyList<PatientAppointment>>> GetMyAppointmentsAsync(string token)
    {
        var request = CreateAuthorizedRequest(HttpMethod.Get, "appointments/my", token);
        var response = await _httpClient.SendAsync(request);
        return await ReadResponseAsync<IReadOnlyList<PatientAppointment>>(response);
    }

    public async Task<ApiResult<PatientAppointmentDetail>> GetAppointmentDetailAsync(string token, int appointmentId)
    {
        var request = CreateAuthorizedRequest(HttpMethod.Get, $"appointments/{appointmentId}", token);
        var response = await _httpClient.SendAsync(request);
        return await ReadResponseAsync<PatientAppointmentDetail>(response);
    }

    public async Task<ApiResult<IReadOnlyList<PatientPrescription>>> GetMyPrescriptionsAsync(string token)
    {
        var request = CreateAuthorizedRequest(HttpMethod.Get, "prescriptions/my", token);
        var response = await _httpClient.SendAsync(request);
        return await ReadResponseAsync<IReadOnlyList<PatientPrescription>>(response);
    }

    public async Task<ApiResult<IReadOnlyList<PatientReport>>> GetMyReportsAsync(string token)
    {
        var request = CreateAuthorizedRequest(HttpMethod.Get, "reports/my", token);
        var response = await _httpClient.SendAsync(request);
        return await ReadResponseAsync<IReadOnlyList<PatientReport>>(response);
    }

    private static HttpRequestMessage CreateAuthorizedRequest(HttpMethod method, string url, string token)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    private static StringContent CreateJsonContent<T>(T value)
    {
        return new StringContent(JsonSerializer.Serialize(value, JsonOptions), Encoding.UTF8, "application/json");
    }

    private static async Task<ApiResult<T>> ReadResponseAsync<T>(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            var data = JsonSerializer.Deserialize<T>(body, JsonOptions);
            if (data is null)
            {
                return ApiResult<T>.FailureResult("Empty response from server.", response.StatusCode);
            }

            return ApiResult<T>.SuccessResult(data, response.StatusCode);
        }

        var error = TryReadError(body) ?? response.ReasonPhrase ?? "Request failed.";
        return ApiResult<T>.FailureResult(error, response.StatusCode);
    }

    private static string? TryReadError(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        try
        {
            var error = JsonSerializer.Deserialize<ApiError>(body, JsonOptions);
            return error?.Error;
        }
        catch
        {
            return null;
        }
    }

    private sealed class ApiError
    {
        public string? Error { get; set; }
    }
}
#endregion
