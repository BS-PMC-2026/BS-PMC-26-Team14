using System.Security.Cryptography;
using System.Text;
using CityFix.Api.Data;
using CityFix.Api.Models;
using CityFix.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
namespace CityFix.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<AuthController> _logger;

        public AuthController(ApplicationDbContext context, IEmailSender emailSender, ILogger<AuthController> logger)
        {
            _context = context;
            _emailSender = emailSender;
            _logger = logger;
        }

        [HttpPost("register-customer")]
        public async Task<IActionResult> RegisterCustomer([FromBody] CustomerRegisterDto dto)
        {
            if (await _context.Customers.AnyAsync(x => x.Email == dto.Email))
                return BadRequest(new { message = "האימייל כבר קיים במערכת" });

            var customer = new Customer
            {
                FullName = dto.FullName,
                Phone = dto.Phone,
                Email = dto.Email,
                Address = dto.Address,
                PasswordHash = HashPassword(dto.Password)
            };
 if (!ModelState.IsValid)
        return BadRequest("Invalid data");
            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            return Ok(new { message = "הלקוח נרשם בהצלחה" });
        }

        [HttpPost("register-worker")]
        public async Task<IActionResult> RegisterWorker([FromBody] WorkerRegisterDto dto)
        {
            if (await _context.Workers.AnyAsync(x => x.Email == dto.Email))
                return BadRequest(new { message = "האימייל כבר קיים במערכת" });

            var worker = new Worker
            {
                FullName = dto.FullName,
                NationalId = dto.NationalId,
                Phone = dto.Phone,
                Email = dto.Email,
                Department = dto.Department,
                Municipality = dto.Municipality,
                PasswordHash = HashPassword(dto.Password),
                ApprovalStatus = "Pending"
            };
if (!ModelState.IsValid)
    return BadRequest("Invalid data");
            _context.Workers.Add(worker);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "בקשת ההרשמה נשלחה בהצלחה",
                status = "Pending"
            });
        }

        [HttpPost("login-customer")]
        public async Task<IActionResult> LoginCustomer([FromBody] LoginDto dto)
        {
            var customer = await _context.Customers
                .FirstOrDefaultAsync(x => x.Email == dto.Email);

            if (customer == null)
                return NotFound(new { message = "לא נמצא לקוח עם האימייל הזה" });

            if (!VerifyPassword(dto.Password, customer.PasswordHash))
                return Unauthorized(new { message = "סיסמה שגויה" });

         return Ok(new
{
    message = "התחברת בהצלחה",
    role = "Customer",
    fullName = customer.FullName,
    email = customer.Email,
    phone = customer.Phone,
    address = customer.Address
});
        }

        [HttpPost("login-worker")]
        public async Task<IActionResult> LoginWorker([FromBody] LoginDto dto)
        {
            var worker = await _context.Workers
                .FirstOrDefaultAsync(x => x.Email == dto.Email);

            if (worker == null)
                return NotFound(new { message = "האימייל לא קיים במערכת" });

            if (!VerifyPassword(dto.Password, worker.PasswordHash))
                return Unauthorized(new { message = "הסיסמה שגויה" });

            if (worker.ApprovalStatus == "Pending")
                return BadRequest(new { message = "החשבון עדיין ממתין לאישור מנהל" });

            if (worker.ApprovalStatus == "Rejected")
                return BadRequest(new { message = "בקשת ההרשמה נדחתה" });
return Ok(new
{
    message = "התחברת בהצלחה",
    role = "Worker",
    fullName = worker.FullName,
    email = worker.Email,
    phone = worker.Phone,
    municipality = worker.Municipality,
    department = worker.Department
});
        }

        [HttpPost("login-admin")]
        public async Task<IActionResult> LoginAdmin([FromBody] LoginDto dto)
        {
            var admin = await _context.Admins
                .FirstOrDefaultAsync(x => x.Email == dto.Email);

            if (admin == null)
                return NotFound(new { message = "האימייל לא קיים במערכת" });

            if (!VerifyPassword(dto.Password, admin.PasswordHash))
                return Unauthorized(new { message = "הסיסמה שגויה" });

            return Ok(new
            {
                message = "התחברת בהצלחה",
                role = "Admin",
                fullName = admin.FullName,
                email = admin.Email
            });
        }

        [HttpGet("admin-profile")]
        public async Task<IActionResult> GetAdminProfile([FromQuery] string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return BadRequest(new { message = "האימייל נדרש" });

            var normalizedEmail = email.Trim().ToLowerInvariant();
            var admin = await _context.Admins
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Email.ToLower() == normalizedEmail);

            if (admin == null)
                return NotFound(new { message = "המנהל לא נמצא" });

            return Ok(new
            {
                username = admin.FullName,
                email = admin.Email
            });
        }

        [HttpPut("admin-profile")]
        public async Task<IActionResult> UpdateAdminProfile([FromBody] UpdateAdminProfileDto dto)
        {
            var currentEmail = dto.CurrentEmail.Trim().ToLowerInvariant();
            var admin = await _context.Admins
                .FirstOrDefaultAsync(x => x.Email.ToLower() == currentEmail);

            if (admin == null)
                return NotFound(new { message = "המנהל לא נמצא" });

            var newEmail = dto.Email.Trim();
            var normalizedNewEmail = newEmail.ToLowerInvariant();

            var emailTaken = await _context.Admins
                .AnyAsync(x => x.Id != admin.Id && x.Email.ToLower() == normalizedNewEmail);

            if (emailTaken)
                return BadRequest(new { message = "האימייל כבר קיים במערכת" });

            admin.FullName = dto.Username.Trim();
            admin.Email = newEmail;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "שם המשתמש והאימייל עודכנו בהצלחה",
                username = admin.FullName,
                email = admin.Email
            });
        }

        [HttpPut("admin-password")]
        public async Task<IActionResult> ChangeAdminPassword([FromBody] ChangeAdminPasswordDto dto)
        {
            var currentEmail = dto.CurrentEmail.Trim().ToLowerInvariant();
            var admin = await _context.Admins
                .FirstOrDefaultAsync(x => x.Email.ToLower() == currentEmail);

            if (admin == null)
                return NotFound(new { message = "המנהל לא נמצא" });

            if (!VerifyPassword(dto.CurrentPassword, admin.PasswordHash))
                return Unauthorized(new { message = "הסיסמה הנוכחית שגויה" });

            admin.PasswordHash = HashPassword(dto.NewPassword);
            await _context.SaveChangesAsync();

            return Ok(new { message = "הסיסמה עודכנה בהצלחה" });
        }

        [HttpGet("pending-workers")]
        public async Task<IActionResult> GetPendingWorkers()
        {
            var pendingWorkers = await _context.Workers
                .Where(w => w.ApprovalStatus == "Pending")
                .Select(w => new
                {
                    w.Id,
                    w.FullName,
                    w.Email,
                    w.Phone,
                    w.Department,
                    w.Municipality,
                    w.NationalId,
                    w.CreatedAt
                })
                .ToListAsync();

            return Ok(pendingWorkers);
        }

        [HttpPost("approve-worker/{workerId}")]
        public async Task<IActionResult> ApproveWorker(int workerId)
        {
            var worker = await _context.Workers.FindAsync(workerId);

            if (worker == null)
                return NotFound(new { message = "Worker not found" });

            worker.ApprovalStatus = "Approved";
            await _context.SaveChangesAsync();

            return Ok(new { message = "Worker approved successfully" });
        }

        [HttpPost("reject-worker/{workerId}")]
        public async Task<IActionResult> RejectWorker(int workerId)
        {
            var worker = await _context.Workers.FindAsync(workerId);

            if (worker == null)
                return NotFound(new { message = "Worker not found" });

            worker.ApprovalStatus = "Rejected";
            await _context.SaveChangesAsync();

            return Ok(new { message = "Worker rejected" });
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            var email = dto.Email.Trim().ToLowerInvariant();
            var user = await FindUserByEmailAsync(email);

            if (user == null)
            {
                return Ok(new { message = "אם כתובת האימייל קיימת במערכת, נשלח קוד איפוס." });
            }

            var now = DateTime.UtcNow;
            var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
            var expiresAt = now.AddMinutes(10);

            var activeCodes = await _context.PasswordResetCodes
                .Where(x => x.UserType == user.Value.UserType && x.UserId == user.Value.UserId && !x.IsUsed && x.ExpiresAt > now)
                .ToListAsync();

            foreach (var activeCode in activeCodes)
            {
                activeCode.IsUsed = true;
                activeCode.UsedAt = now;
            }

            var passwordResetCode = new PasswordResetCode
            {
                UserType = user.Value.UserType,
                UserId = user.Value.UserId,
                CodeHash = HashPassword(code),
                ExpiresAt = expiresAt,
                CreatedAt = now,
                FailedAttempts = 0,
                IsUsed = false
            };

            _context.PasswordResetCodes.Add(passwordResetCode);
            await _context.SaveChangesAsync();

            var subject = "CityFix - קוד לאיפוס סיסמה";
            var body = $"קוד האימות שלך הוא: {code}\n\nהקוד תקף ל-10 דקות.";

            try
            {
                await _emailSender.SendAsync(email, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send password reset code to {Email}", email);
                return StatusCode(500, new { message = "לא הצלחנו לשלוח אימייל כרגע. נסה שוב מאוחר יותר." });
            }

            return Ok(new { message = "אם כתובת האימייל קיימת במערכת, נשלח קוד איפוס." });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            var email = dto.Email.Trim().ToLowerInvariant();
            var user = await FindUserByEmailAsync(email);

            if (user == null)
            {
                return BadRequest(new { message = "קוד האימות אינו תקין או שפג תוקפו" });
            }

            var now = DateTime.UtcNow;
            var resetCode = await _context.PasswordResetCodes
                .Where(x => x.UserType == user.Value.UserType && x.UserId == user.Value.UserId && !x.IsUsed)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync();

            if (resetCode == null || resetCode.ExpiresAt <= now)
            {
                return BadRequest(new { message = "קוד האימות אינו תקין או שפג תוקפו" });
            }

            if (!VerifyPassword(dto.Code, resetCode.CodeHash))
            {
                await _context.SaveChangesAsync();
                return BadRequest(new { message = "קוד האימות אינו תקין או שפג תוקפו" });
            }

            switch (user.Value.UserType)
            {
                case "Customer":
                {
                    var customer = await _context.Customers.FindAsync(user.Value.UserId);
                    if (customer == null)
                    {
                        return BadRequest(new { message = "המשתמש לא נמצא" });
                    }

                    customer.PasswordHash = HashPassword(dto.NewPassword);
                    break;
                }
                case "Worker":
                {
                    var worker = await _context.Workers.FindAsync(user.Value.UserId);
                    if (worker == null)
                    {
                        return BadRequest(new { message = "המשתמש לא נמצא" });
                    }

                    worker.PasswordHash = HashPassword(dto.NewPassword);
                    break;
                }
                case "Admin":
                {
                    var admin = await _context.Admins.FindAsync(user.Value.UserId);
                    if (admin == null)
                    {
                        return BadRequest(new { message = "המשתמש לא נמצא" });
                    }

                    admin.PasswordHash = HashPassword(dto.NewPassword);
                    break;
                }
            }

            resetCode.IsUsed = true;
            resetCode.UsedAt = now;

            await _context.SaveChangesAsync();

            return Ok(new { message = "הסיסמה אופסה בהצלחה" });
        }
        [HttpGet("customer-profile")]
public async Task<IActionResult> GetCustomerProfile([FromQuery] string email)
{
    if (string.IsNullOrWhiteSpace(email))
        return BadRequest(new { message = "האימייל נדרש" });

    var normalizedEmail = email.Trim().ToLowerInvariant();

    var customer = await _context.Customers
        .AsNoTracking()
        .FirstOrDefaultAsync(x => x.Email.ToLower() == normalizedEmail);

    if (customer == null)
        return NotFound(new { message = "הלקוח לא נמצא" });

    return Ok(new
    {
        fullName = customer.FullName,
        email = customer.Email,
        phone = customer.Phone,
        address = customer.Address
    });
}

public class UpdateCustomerProfileDto
{
    public string Email { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Address { get; set; } = "";
    public string CurrentPassword { get; set; } = "";
    public string NewPassword { get; set; } = "";
}

[HttpPut("update-customer-profile")]
public async Task<IActionResult> UpdateCustomerProfile([FromBody] UpdateCustomerProfileDto dto)
{
    var customer = await _context.Customers
        .FirstOrDefaultAsync(x => x.Email == dto.Email);

    if (customer == null)
        return NotFound(new { message = "הלקוח לא נמצא" });

    customer.FullName = dto.FullName;
    customer.Phone = dto.Phone;
    customer.Address = dto.Address;

    if (!string.IsNullOrWhiteSpace(dto.NewPassword))
    {
        if (string.IsNullOrWhiteSpace(dto.CurrentPassword))
            return BadRequest(new { message = "יש להזין סיסמה נוכחית" });

        if (!VerifyPassword(dto.CurrentPassword, customer.PasswordHash))
            return BadRequest(new { message = "הסיסמה הנוכחית שגויה" });

        customer.PasswordHash = HashPassword(dto.NewPassword);
    }

    await _context.SaveChangesAsync();

    return Ok(new
    {
        message = "הפרופיל עודכן בהצלחה",
        role = "Customer",
        fullName = customer.FullName,
        email = customer.Email,
        phone = customer.Phone,
        address = customer.Address
    });
}

public class UpdateWorkerProfileDto
{
    public string Email { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Municipality { get; set; } = "";
    public string Department { get; set; } = "";
    public string CurrentPassword { get; set; } = "";
    public string NewPassword { get; set; } = "";
}

public class AcceptReportDto
{
    public string WorkerEmail { get; set; } = "";
}

public class WorkerUploadImageDto
{
    public string WorkerEmail { get; set; } = "";
    public string ImageBase64 { get; set; } = "";
    public string Note { get; set; } = "";
}

[HttpGet("worker-profile")]
public async Task<IActionResult> GetWorkerProfile([FromQuery] string email)
{
    if (string.IsNullOrWhiteSpace(email))
        return BadRequest(new { message = "האימייל נדרש" });

    var normalizedEmail = email.Trim().ToLowerInvariant();

    var worker = await _context.Workers
        .AsNoTracking()
        .FirstOrDefaultAsync(x => x.Email.ToLower() == normalizedEmail);

    if (worker == null)
        return NotFound(new { message = "העובד לא נמצא" });

    return Ok(new
    {
        fullName = worker.FullName,
        email = worker.Email,
        phone = worker.Phone,
        municipality = worker.Municipality,
        department = worker.Department
    });
}

[HttpPut("update-worker-profile")]
public async Task<IActionResult> UpdateWorkerProfile([FromBody] UpdateWorkerProfileDto dto)
{
    var worker = await _context.Workers
        .FirstOrDefaultAsync(x => x.Email == dto.Email);

    if (worker == null)
        return NotFound(new { message = "העובד לא נמצא" });

    worker.FullName = dto.FullName;
    worker.Phone = dto.Phone;
    worker.Municipality = dto.Municipality;
    worker.Department = dto.Department;

    if (!string.IsNullOrWhiteSpace(dto.NewPassword))
    {
        if (string.IsNullOrWhiteSpace(dto.CurrentPassword))
            return BadRequest(new { message = "יש להזין סיסמה נוכחית" });

        if (!VerifyPassword(dto.CurrentPassword, worker.PasswordHash))
            return BadRequest(new { message = "הסיסמה הנוכחית שגויה" });

        worker.PasswordHash = HashPassword(dto.NewPassword);
    }

    await _context.SaveChangesAsync();

    return Ok(new
    {
        message = "פרופיל העובד עודכן בהצלחה",
        role = "Worker",
        fullName = worker.FullName,
        email = worker.Email,
        phone = worker.Phone,
        municipality = worker.Municipality,
        department = worker.Department
    });
}
[HttpPost("create-report")]
public async Task<IActionResult> CreateReport([FromBody] CreateReportDto dto)
{
    if (!ModelState.IsValid)
        return BadRequest(new { message = "נתונים לא תקינים" });
if (dto.Latitude == 0 || dto.Longitude == 0)
{
    return BadRequest(new { message = "מיקום לא תקין" });
}

if (dto.Latitude < -90 || dto.Latitude > 90 ||
    dto.Longitude < -180 || dto.Longitude > 180)
{
    return BadRequest(new { message = "קואורדינטות לא חוקיות" });
}

// גבולות ישראל
if (dto.Latitude < 29.45 || dto.Latitude > 33.35 ||
    dto.Longitude < 34.25 || dto.Longitude > 35.65)
{
    return BadRequest(new { message = "המיקום חייב להיות בתוך ישראל" });
}
    var customerExists = await _context.Customers
        .AnyAsync(c => c.Email.ToLower() == dto.CustomerEmail.ToLower());

    if (!customerExists)
        return NotFound(new { message = "הלקוח לא נמצא במערכת" });

var geometryFactory = new GeometryFactory(new PrecisionModel(), 4326);

var report = new Report
{
    CustomerEmail = dto.CustomerEmail,
    Category = dto.Category,
    Priority = dto.Priority,
    Description = dto.Description,
    Notes = dto.Notes,
    ImageBase64 = dto.ImageBase64,
    Latitude = dto.Latitude,
    Longitude = dto.Longitude,
    LocationPoint = geometryFactory.CreatePoint(
    new Coordinate(dto.Longitude, dto.Latitude)
    ),
    Status = "Open",
    CreatedAt = DateTime.UtcNow
};

    _context.Reports.Add(report);
    await _context.SaveChangesAsync();

    return Ok(new
    {
        message = "הדיווח נשמר בהצלחה",
        reportId = report.Id
    });
}


[HttpPost("accept-report/{reportId}")]
public async Task<IActionResult> AcceptReport(int reportId, [FromBody] AcceptReportDto dto)
{
    if (string.IsNullOrWhiteSpace(dto.WorkerEmail))
        return BadRequest(new { message = "אימייל עובד נדרש" });

    var workerEmail = dto.WorkerEmail.Trim().ToLowerInvariant();

    var worker = await _context.Workers
        .FirstOrDefaultAsync(w => w.Email.ToLower() == workerEmail);

    if (worker == null)
        return NotFound(new { message = "העובד לא נמצא במערכת" });

    if (worker.ApprovalStatus != "Approved")
        return BadRequest(new { message = "העובד עדיין לא מאושר במערכת" });

    var report = await _context.Reports
        .FirstOrDefaultAsync(r => r.Id == reportId);

    if (report == null)
        return NotFound(new { message = "הדיווח לא נמצא" });

    if (report.Status != "Open")
        return BadRequest(new { message = "הדיווח כבר נלקח לטיפול או שאינו פתוח" });

    report.Status = "In Treatment";
    report.AssignedWorkerEmail = worker.Email;
    report.AcceptedAt = DateTime.UtcNow;

    await _context.SaveChangesAsync();

    return Ok(new
    {
        message = "הדיווח התקבל לטיפול בהצלחה",
        reportId = report.Id,
        status = report.Status,
        assignedWorkerEmail = report.AssignedWorkerEmail,
        acceptedAt = report.AcceptedAt
    });
}


[HttpPut("worker-upload-image/{reportId}")]
public async Task<IActionResult> WorkerUploadImage(int reportId, [FromBody] WorkerUploadImageDto dto)
{
    if (string.IsNullOrWhiteSpace(dto.WorkerEmail))
        return BadRequest(new { message = "אימייל עובד נדרש" });

    if (string.IsNullOrWhiteSpace(dto.ImageBase64))
        return BadRequest(new { message = "חובה לבחור תמונה" });

    var workerEmail = dto.WorkerEmail.Trim().ToLowerInvariant();

    var report = await _context.Reports.FirstOrDefaultAsync(r => r.Id == reportId);

    if (report == null)
        return NotFound(new { message = "הדיווח לא נמצא" });

    if (report.Status != "In Treatment")
        return BadRequest(new { message = "אפשר להעלות תמונה רק לדיווח שנמצא בטיפול" });

    if (string.IsNullOrWhiteSpace(report.AssignedWorkerEmail))
        return BadRequest(new { message = "הדיווח עדיין לא שויך לעובד" });

    if (report.AssignedWorkerEmail.ToLower() != workerEmail)
        return BadRequest(new { message = "רק העובד שקיבל את הדיווח יכול להעלות תמונה" });

    report.WorkerImageBase64 = dto.ImageBase64;
    report.WorkerImageNote = dto.Note;
    report.WorkerImageUploadedAt = DateTime.UtcNow;

    await _context.SaveChangesAsync();

    return Ok(new
    {
        message = "התמונה נשמרה בהצלחה",
        reportId = report.Id,
        status = report.Status,
        workerImageBase64 = report.WorkerImageBase64,
        workerImageNote = report.WorkerImageNote,
        workerImageUploadedAt = report.WorkerImageUploadedAt
    });
}

[HttpGet("open-reports")]
public async Task<IActionResult> GetOpenReports([FromQuery] string? workerEmail)
{
    workerEmail = workerEmail?.Trim().ToLower();

    var reports = await _context.Reports
        .Where(r =>
            r.Status == "Open" ||
            (!string.IsNullOrEmpty(workerEmail) &&
             r.AssignedWorkerEmail != null &&
             r.AssignedWorkerEmail.ToLower() == workerEmail)
        )
        .OrderByDescending(r => r.CreatedAt)
        .Select(r => new
        {
            id = r.Id,
            customerEmail = r.CustomerEmail,
            category = r.Category,
            priority = r.Priority,
            description = r.Description,
            notes = r.Notes,
            imageBase64 = r.ImageBase64,
            latitude = r.Latitude,
            longitude = r.Longitude,
            status = r.Status,
            assignedWorkerEmail = r.AssignedWorkerEmail,
            acceptedAt = r.AcceptedAt,
            createdAt = r.CreatedAt,
            workerImageBase64 = r.WorkerImageBase64,
            workerImageNote = r.WorkerImageNote,
            workerImageUploadedAt = r.WorkerImageUploadedAt,
        })
        .ToListAsync();

    return Ok(reports);
}


        private async Task<(string UserType, int UserId)?> FindUserByEmailAsync(string email)
        {
            var customer = await _context.Customers
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Email.ToLower() == email);
            if (customer != null)
            {
                return ("Customer", customer.Id);
            }

            var worker = await _context.Workers
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Email.ToLower() == email);
            if (worker != null)
            {
                return ("Worker", worker.Id);
            }

            var admin = await _context.Admins
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Email.ToLower() == email);
            if (admin != null)
            {
                return ("Admin", admin.Id);
            }

            return null;
        }
        

        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }

        private static bool VerifyPassword(string password, string savedHash)
        {
            var hashedPassword = HashPassword(password);
            return hashedPassword == savedHash;
        }
    }
}