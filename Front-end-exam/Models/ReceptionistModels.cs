namespace Front_end_exam.Models;

public class ReceptionistDashboardViewModel
{
    public string UserName { get; set; } = string.Empty;
    public string ClinicName { get; set; } = string.Empty;
    public string SelectedDate { get; set; } = string.Empty;
    public IReadOnlyList<QueueEntry> Entries { get; set; } = [];
    public string? StatusMessage { get; set; }
    public string? ErrorMessage { get; set; }
}

public class QueueEntry
{
    public int Id { get; set; }
    public int TokenNumber { get; set; }
    public string Status { get; set; } = string.Empty;
    public string QueueDate { get; set; } = string.Empty;
    public int AppointmentId { get; set; }
    public QueueAppointment Appointment { get; set; } = new();
}

public class QueueAppointment
{
    public QueuePatient Patient { get; set; } = new();
}

public class QueuePatient
{
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
}

public class QueueStatusUpdateViewModel
{
    public int QueueId { get; set; }

    public string Status { get; set; } = string.Empty;

    public string Date { get; set; } = string.Empty;
}

public class QueueStatusUpdateRequest
{
    public string Status { get; set; } = string.Empty;
}
