using Front_end_exam.Models;
using Front_end_exam.Services;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Globalization;

namespace Front_end_exam.Controllers;

public class HomeController(IClinicCmsApiClient apiClient) : Controller
{
    private readonly IClinicCmsApiClient _apiClient = apiClient;

    #region Auth

    [HttpGet]
    public IActionResult Index()
    {
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult Login()
    {
        if (IsLoggedIn())
        {
            return RedirectToRoleHome();
        }

        return View("Index", new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View("Index", model);
        }

        var loginResult = await _apiClient.LoginAsync(new LoginRequest
        {
            Email = model.Email,
            Password = model.Password
        });

        if (!loginResult.Success || loginResult.Data is null)
        {
            model.ErrorMessage = loginResult.Error ?? "Unable to sign in.";
            return View("Index", model);
        }

        HttpContext.Session.SetString(SessionKeys.AuthToken, loginResult.Data.Token);
        HttpContext.Session.SetString(SessionKeys.UserName, loginResult.Data.User.Name);
        HttpContext.Session.SetString(SessionKeys.UserRole, loginResult.Data.User.Role);
        HttpContext.Session.SetString(SessionKeys.ClinicName, loginResult.Data.User.ClinicName);
        HttpContext.Session.SetString(SessionKeys.ClinicCode, loginResult.Data.User.ClinicCode);

        return RedirectToRoleHome();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction(nameof(Login));
    }

    #endregion

    #region Admin

    [HttpGet]
    public async Task<IActionResult> Admin()
    {
        if (!IsLoggedIn())
        {
            return RedirectToAction(nameof(Login));
        }

        if (!IsInRole("admin"))
        {
            TempData["AuthError"] = "You do not have access to the admin section.";
            return RedirectToRoleHome();
        }

        var token = HttpContext.Session.GetString(SessionKeys.AuthToken)!;
        var clinicTask = _apiClient.GetClinicAsync(token);
        var usersTask = _apiClient.GetUsersAsync(token);
        await Task.WhenAll(clinicTask, usersTask);

        if (!clinicTask.Result.Success || !usersTask.Result.Success || clinicTask.Result.Data is null)
        {
            HttpContext.Session.Clear();
            TempData["AuthError"] = clinicTask.Result.Error ?? usersTask.Result.Error ?? "Session expired. Sign in again.";
            return RedirectToAction(nameof(Login));
        }

        var viewModel = new AdminDashboardViewModel
        {
            UserName = HttpContext.Session.GetString(SessionKeys.UserName) ?? "Admin",
            ClinicName = clinicTask.Result.Data.Name,
            ClinicCode = clinicTask.Result.Data.Code,
            Clinic = clinicTask.Result.Data,
            Users = usersTask.Result.Data ?? [],
            NewUser = new AdminCreateUserViewModel(),
            StatusMessage = TempData["AdminMessage"] as string,
            ErrorMessage = TempData["AdminError"] as string
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateUser(AdminDashboardViewModel model)
    {
        var token = HttpContext.Session.GetString(SessionKeys.AuthToken);
        if (string.IsNullOrWhiteSpace(token))
        {
            TempData["AuthError"] = "Sign in again to continue.";
            return RedirectToAction(nameof(Login));
        }

        if (!IsInRole("admin"))
        {
            TempData["AuthError"] = "You do not have access to the admin section.";
            return RedirectToRoleHome();
        }

        if (!ModelState.IsValid)
        {
            return await BuildAdminViewModelAsync(model.NewUser, "Please correct the highlighted values.");
        }

        var result = await _apiClient.CreateUserAsync(token, new AdminCreateUserRequest
        {
            Name = model.NewUser.Name,
            Email = model.NewUser.Email,
            Password = model.NewUser.Password,
            Role = model.NewUser.Role,
            Phone = model.NewUser.Phone
        });

        if (!result.Success)
        {
            return await BuildAdminViewModelAsync(model.NewUser, result.Error ?? "Unable to create user.");
        }

        TempData["AdminMessage"] = "User created successfully.";
        return RedirectToAction(nameof(Admin));
    }

    #endregion

    #region Receptionist

    [HttpGet]
    public async Task<IActionResult> Receptionist(string? date)
    {
        if (!IsLoggedIn())
        {
            return RedirectToAction(nameof(Login));
        }

        if (!IsInRole("receptionist"))
        {
            TempData["AuthError"] = "You do not have access to the receptionist section.";
            return RedirectToRoleHome();
        }

        var token = HttpContext.Session.GetString(SessionKeys.AuthToken)!;
        var selectedDate = string.IsNullOrWhiteSpace(date) ? DateTime.Today.ToString("yyyy-MM-dd") : date;
        var queueResult = await _apiClient.GetQueueAsync(token, selectedDate);

        return View(new ReceptionistDashboardViewModel
        {
            UserName = HttpContext.Session.GetString(SessionKeys.UserName) ?? "Receptionist",
            ClinicName = HttpContext.Session.GetString(SessionKeys.ClinicName) ?? "Clinic",
            SelectedDate = selectedDate,
            Entries = queueResult.Success ? queueResult.Data ?? [] : [],
            StatusMessage = TempData["ReceptionistMessage"] as string,
            ErrorMessage = TempData["ReceptionistError"] as string ?? (!queueResult.Success ? queueResult.Error : null)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateQueueStatus(QueueStatusUpdateViewModel model)
    {
        var token = HttpContext.Session.GetString(SessionKeys.AuthToken);
        if (string.IsNullOrWhiteSpace(token))
        {
            TempData["AuthError"] = "Sign in again to continue.";
            return RedirectToAction(nameof(Login));
        }

        if (!IsInRole("receptionist"))
        {
            TempData["AuthError"] = "You do not have access to the receptionist section.";
            return RedirectToRoleHome();
        }

        if (!ModelState.IsValid)
        {
            TempData["ReceptionistError"] = "Invalid queue update request.";
            return RedirectToAction(nameof(Receptionist), new { date = model.Date });
        }

        var result = await _apiClient.UpdateQueueStatusAsync(token, model.QueueId, new QueueStatusUpdateRequest
        {
            Status = model.Status
        });

        TempData[result.Success ? "ReceptionistMessage" : "ReceptionistError"] =
            result.Success ? "Queue status updated." : result.Error ?? "Unable to update queue status.";

        return RedirectToAction(nameof(Receptionist), new { date = model.Date });
    }

    #endregion

    #region Patient

    [HttpGet]
    public async Task<IActionResult> Patient(int? appointmentId)
    {
        if (!IsLoggedIn())
        {
            return RedirectToAction(nameof(Login));
        }

        if (!IsInRole("patient"))
        {
            TempData["AuthError"] = "You do not have access to the patient section.";
            return RedirectToRoleHome();
        }

        return await BuildPatientViewModelAsync(appointmentId);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BookAppointment([Bind(Prefix = "Booking")] BookAppointmentViewModel model)
    {
        var token = HttpContext.Session.GetString(SessionKeys.AuthToken);
        if (string.IsNullOrWhiteSpace(token))
        {
            TempData["AuthError"] = "Sign in again to continue.";
            return RedirectToAction(nameof(Login));
        }

        if (!IsInRole("patient"))
        {
            TempData["AuthError"] = "You do not have access to the patient section.";
            return RedirectToRoleHome();
        }

        model.AppointmentDate = NormalizeAppointmentDate(model.AppointmentDate);

        if (!ModelState.IsValid)
        {
            return await BuildPatientViewModelAsync(null, model, "Please enter a valid appointment date and time slot.");
        }

        var result = await _apiClient.BookAppointmentAsync(token, new BookAppointmentRequest
        {
            AppointmentDate = model.AppointmentDate,
            TimeSlot = model.TimeSlot
        });

        if (!result.Success)
        {
            return await BuildPatientViewModelAsync(null, model, result.Error ?? "Unable to book appointment.");
        }

        TempData["PatientMessage"] = "Appointment booked successfully.";
        return RedirectToAction(nameof(Patient), new { appointmentId = result.Data?.Id });
    }

    #endregion

    #region System

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    #endregion

    #region Helpers

    private async Task<IActionResult> BuildAdminViewModelAsync(AdminCreateUserViewModel newUser, string errorMessage)
    {
        var token = HttpContext.Session.GetString(SessionKeys.AuthToken);
        if (string.IsNullOrWhiteSpace(token))
        {
            TempData["AuthError"] = "Sign in again to continue.";
            return RedirectToAction(nameof(Login));
        }

        if (!IsInRole("admin"))
        {
            TempData["AuthError"] = "You do not have access to the admin section.";
            return RedirectToRoleHome();
        }

        var clinicTask = _apiClient.GetClinicAsync(token);
        var usersTask = _apiClient.GetUsersAsync(token);
        await Task.WhenAll(clinicTask, usersTask);

        if (!clinicTask.Result.Success || !usersTask.Result.Success || clinicTask.Result.Data is null)
        {
            HttpContext.Session.Clear();
            TempData["AuthError"] = clinicTask.Result.Error ?? usersTask.Result.Error ?? "Session expired. Sign in again.";
            return RedirectToAction(nameof(Login));
        }

        return View("Admin", new AdminDashboardViewModel
        {
            UserName = HttpContext.Session.GetString(SessionKeys.UserName) ?? "Admin",
            ClinicName = clinicTask.Result.Data.Name,
            ClinicCode = clinicTask.Result.Data.Code,
            Clinic = clinicTask.Result.Data,
            Users = usersTask.Result.Data ?? [],
            NewUser = newUser,
            ErrorMessage = errorMessage
        });
    }

    private async Task<IActionResult> BuildPatientViewModelAsync(int? appointmentId, BookAppointmentViewModel? booking = null, string? errorMessage = null)
    {
        var token = HttpContext.Session.GetString(SessionKeys.AuthToken);
        if (string.IsNullOrWhiteSpace(token))
        {
            TempData["AuthError"] = "Sign in again to continue.";
            return RedirectToAction(nameof(Login));
        }

        var appointmentsTask = _apiClient.GetMyAppointmentsAsync(token);
        var prescriptionsTask = _apiClient.GetMyPrescriptionsAsync(token);
        var reportsTask = _apiClient.GetMyReportsAsync(token);
        await Task.WhenAll(appointmentsTask, prescriptionsTask, reportsTask);

        PatientAppointmentDetail? detail = null;
        if (appointmentId.HasValue)
        {
            var detailResult = await _apiClient.GetAppointmentDetailAsync(token, appointmentId.Value);
            if (detailResult.Success)
            {
                detail = detailResult.Data;
            }
        }

        return View("Patient", new PatientDashboardViewModel
        {
            UserName = HttpContext.Session.GetString(SessionKeys.UserName) ?? "Patient",
            ClinicName = HttpContext.Session.GetString(SessionKeys.ClinicName) ?? "Clinic",
            Booking = booking ?? new BookAppointmentViewModel
            {
                AppointmentDate = DateTime.Today.AddDays(1).ToString("yyyy-MM-dd"),
                TimeSlot = "10:00-10:15"
            },
            Appointments = appointmentsTask.Result.Success ? appointmentsTask.Result.Data ?? [] : [],
            SelectedAppointment = detail,
            Prescriptions = prescriptionsTask.Result.Success ? prescriptionsTask.Result.Data ?? [] : [],
            Reports = reportsTask.Result.Success ? reportsTask.Result.Data ?? [] : [],
            StatusMessage = TempData["PatientMessage"] as string,
            ErrorMessage = errorMessage ?? appointmentsTask.Result.Error ?? prescriptionsTask.Result.Error ?? reportsTask.Result.Error
        });
    }

    private bool IsLoggedIn()
    {
        return !string.IsNullOrWhiteSpace(HttpContext.Session.GetString(SessionKeys.AuthToken));
    }

    private bool IsInRole(string role)
    {
        var currentRole = HttpContext.Session.GetString(SessionKeys.UserRole);
        return string.Equals(currentRole, role, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeAppointmentDate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        if (DateOnly.TryParseExact(value, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            return date.ToString("yyyy-MM-dd");
        }

        return value;
    }

    private IActionResult RedirectToRoleHome()
    {
        var role = HttpContext.Session.GetString(SessionKeys.UserRole);

        if (string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase))
        {
            return RedirectToAction(nameof(Admin));
        }

        if (string.Equals(role, "receptionist", StringComparison.OrdinalIgnoreCase))
        {
            return RedirectToAction(nameof(Receptionist));
        }

        if (string.Equals(role, "patient", StringComparison.OrdinalIgnoreCase))
        {
            return RedirectToAction(nameof(Patient));
        }

        HttpContext.Session.Clear();
        TempData["AuthError"] = "This role is not available in the project yet.";
        return RedirectToAction(nameof(Login));
    }

    #endregion
}
