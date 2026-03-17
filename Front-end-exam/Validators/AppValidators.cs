using FluentValidation;
using Front_end_exam.Models;
using System.Globalization;

namespace Front_end_exam.Validators;

public class LoginViewModelValidator : AbstractValidator<LoginViewModel>
{
    public LoginViewModelValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Enter a valid email address.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.");
    }
}

public class AdminCreateUserViewModelValidator : AbstractValidator<AdminCreateUserViewModel>
{
    public AdminCreateUserViewModelValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MinimumLength(3).WithMessage("Name must be at least 3 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Enter a valid email address.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters.");

        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("Role is required.")
            .Must(role => role is "receptionist" or "doctor" or "patient")
            .WithMessage("Select a valid role.");
    }
}

public class QueueStatusUpdateViewModelValidator : AbstractValidator<QueueStatusUpdateViewModel>
{
    public QueueStatusUpdateViewModelValidator()
    {
        RuleFor(x => x.QueueId)
            .GreaterThan(0).WithMessage("Invalid queue entry.");

        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Status is required.")
            .Must(status => status is "in-progress" or "done" or "skipped")
            .WithMessage("Invalid queue status.");

        RuleFor(x => x.Date)
            .NotEmpty().WithMessage("Date is required.");
    }
}

public class BookAppointmentViewModelValidator : AbstractValidator<BookAppointmentViewModel>
{
    private static readonly string[] AllowedSlots =
    [
        "10:00-10:15",
        "10:15-10:30",
        "10:30-10:45",
        "10:45-11:00",
        "11:00-11:15"
    ];

    public BookAppointmentViewModelValidator()
    {
        RuleFor(x => x.AppointmentDate)
            .NotEmpty().WithMessage("Appointment date is required.")
            .Must(BeFutureDate).WithMessage("Appointment date must be in the future.")
            .When(x => !string.IsNullOrWhiteSpace(x.AppointmentDate));

        RuleFor(x => x.TimeSlot)
            .NotEmpty().WithMessage("Time slot is required.")
            .Must(slot => AllowedSlots.Contains(slot)).WithMessage("Select a valid time slot.");
    }

    private static bool BeFutureDate(string value)
    {
        return TryParseAppointmentDate(value, out var date)
            && date > DateOnly.FromDateTime(DateTime.Today);
    }

    private static bool TryParseAppointmentDate(string value, out DateOnly date)
    {
        return DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date)
            || DateOnly.TryParseExact(value, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
    }
}
