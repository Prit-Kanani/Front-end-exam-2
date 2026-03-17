namespace Front_end_exam.Models;

public class ClinicInfo
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public int UserCount { get; set; }
    public int AppointmentCount { get; set; }
    public int QueueCount { get; set; }
}

public class ClinicUser
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AdminDashboardViewModel
{
    public string UserName { get; set; } = string.Empty;
    public string ClinicName { get; set; } = string.Empty;
    public string ClinicCode { get; set; } = string.Empty;
    public ClinicInfo Clinic { get; set; } = new();
    public IReadOnlyList<ClinicUser> Users { get; set; } = [];
    public AdminCreateUserViewModel NewUser { get; set; } = new();
    public string? StatusMessage { get; set; }
    public string? ErrorMessage { get; set; }
}

public class AdminCreateUserViewModel
{
    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string Role { get; set; } = "receptionist";

    public string? Phone { get; set; }
}

public class AdminCreateUserRequest
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? Phone { get; set; }
}
