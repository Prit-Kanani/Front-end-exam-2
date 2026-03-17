namespace Front_end_exam.Models;

public class PatientDashboardViewModel
{
    public string UserName { get; set; } = string.Empty;
    public string ClinicName { get; set; } = string.Empty;
    public BookAppointmentViewModel Booking { get; set; } = new();
    public IReadOnlyList<PatientAppointment> Appointments { get; set; } = [];
    public PatientAppointmentDetail? SelectedAppointment { get; set; }
    public IReadOnlyList<PatientPrescription> Prescriptions { get; set; } = [];
    public IReadOnlyList<PatientReport> Reports { get; set; } = [];
    public string? StatusMessage { get; set; }
    public string? ErrorMessage { get; set; }
}

public class BookAppointmentViewModel
{
    public string AppointmentDate { get; set; } = string.Empty;

    public string TimeSlot { get; set; } = string.Empty;
}

public class BookAppointmentRequest
{
    public string AppointmentDate { get; set; } = string.Empty;
    public string TimeSlot { get; set; } = string.Empty;
}

public class PatientAppointment
{
    public int Id { get; set; }
    public string AppointmentDate { get; set; } = string.Empty;
    public string TimeSlot { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public QueueEntry? QueueEntry { get; set; }
}

public class PatientAppointmentDetail
{
    public int Id { get; set; }
    public string AppointmentDate { get; set; } = string.Empty;
    public string TimeSlot { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public QueueEntry? QueueEntry { get; set; }
    public PatientPrescription? Prescription { get; set; }
    public PatientReport? Report { get; set; }
}

public class PatientPrescription
{
    public int Id { get; set; }
    public List<PrescriptionMedicine> Medicines { get; set; } = [];
    public string? Notes { get; set; }
    public PrescriptionDoctor? Doctor { get; set; }
    public PrescriptionAppointment? Appointment { get; set; }
}

public class PrescriptionMedicine
{
    public string Name { get; set; } = string.Empty;
    public string Dosage { get; set; } = string.Empty;
    public string Duration { get; set; } = string.Empty;
}

public class PrescriptionDoctor
{
    public string Name { get; set; } = string.Empty;
}

public class PrescriptionAppointment
{
    public string AppointmentDate { get; set; } = string.Empty;
    public string TimeSlot { get; set; } = string.Empty;
}

public class PatientReport
{
    public int Id { get; set; }
    public string Diagnosis { get; set; } = string.Empty;
    public string? TestRecommended { get; set; }
    public string? Remarks { get; set; }
    public ReportDoctor? Doctor { get; set; }
    public PrescriptionAppointment? Appointment { get; set; }
}

public class ReportDoctor
{
    public string Name { get; set; } = string.Empty;
}
