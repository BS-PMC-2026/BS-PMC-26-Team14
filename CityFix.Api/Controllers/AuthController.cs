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
                PasswordHash = HashPassword(dto.Password),
                IsBlocked = false
            };

            if (!ModelState.IsValid)
                return BadRequest("Invalid data");

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            return Ok(new { message = "התושב נרשם בהצלחה" });
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
                ApprovalStatus = "Pending",
                IsBlocked = false
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
                return NotFound(new { message = "לא נמצא תושב עם האימייל הזה" });

            if (customer.IsBlocked)
                return BadRequest(new { message = "Your account has been blocked by the system administrator" });

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

            if (worker.IsBlocked)
                return BadRequest(new { message = "Your account has been blocked by the system administrator" });

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

            if (admin.IsBlocked)
                return BadRequest(new { message = "Your admin account has been blocked" });

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

        [HttpGet("admin/users")]
        public async Task<IActionResult> GetAdminUsers()
        {
            var customers = await _context.Customers
                .AsNoTracking()
                .Select(c => new
                {
                    id = c.Id,
                    name = c.FullName,
                    email = c.Email,
                    role = "Customer",
                    status = c.IsBlocked ? "Blocked" : "Active",
                    joinDate = c.CreatedAt,
                    reports = _context.Reports.Count(r => r.CustomerEmail == c.Email)
                })
                .ToListAsync();

            var workers = await _context.Workers
                .AsNoTracking()
                .Select(w => new
                {
                    id = w.Id,
                    name = w.FullName,
                    email = w.Email,
                    role = "Worker",
                    status = w.IsBlocked
                        ? "Blocked"
                        : w.ApprovalStatus == "Approved"
                            ? "Active"
                            : w.ApprovalStatus,
                    joinDate = w.CreatedAt,
                    reports = _context.Reports.Count(r => r.AssignedWorkerEmail == w.Email)
                })
                .ToListAsync();

            var admins = await _context.Admins
                .AsNoTracking()
                .Select(a => new
                {
                    id = a.Id,
                    name = a.FullName,
                    email = a.Email,
                    role = "Admin",
                    status = a.IsBlocked ? "Blocked" : "Active",
                    joinDate = a.CreatedAt,
                    reports = 0
                })
                .ToListAsync();

            var users = customers
                .Concat(workers)
                .Concat(admins)
                .OrderByDescending(u => u.joinDate)
                .ToList();

            return Ok(users);
        }

        [HttpPut("admin/users/{role}/{id}/block")]
        public async Task<IActionResult> ToggleUserBlock(string role, int id)
        {
            role = role.Trim().ToLowerInvariant();

            if (role == "customer")
            {
                var customer = await _context.Customers.FindAsync(id);

                if (customer == null)
                    return NotFound(new { message = "Customer not found" });

                customer.IsBlocked = !customer.IsBlocked;
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = customer.IsBlocked ? "Customer blocked successfully" : "Customer unblocked successfully",
                    isBlocked = customer.IsBlocked
                });
            }

            if (role == "worker")
            {
                var worker = await _context.Workers.FindAsync(id);

                if (worker == null)
                    return NotFound(new { message = "Worker not found" });

                worker.IsBlocked = !worker.IsBlocked;
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = worker.IsBlocked ? "Worker blocked successfully" : "Worker unblocked successfully",
                    isBlocked = worker.IsBlocked
                });
            }

            if (role == "admin")
            {
                var admin = await _context.Admins.FindAsync(id);

                if (admin == null)
                    return NotFound(new { message = "Admin not found" });

                admin.IsBlocked = !admin.IsBlocked;
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = admin.IsBlocked ? "Admin blocked successfully" : "Admin unblocked successfully",
                    isBlocked = admin.IsBlocked
                });
            }

            return BadRequest(new { message = "Invalid role" });
        }

        [HttpDelete("admin/users/{role}/{id}")]
        public async Task<IActionResult> DeleteUser(string role, int id)
        {
            role = role.Trim().ToLowerInvariant();

            if (role == "customer")
            {
                var customer = await _context.Customers.FindAsync(id);

                if (customer == null)
                    return NotFound(new { message = "Customer not found" });

                _context.Customers.Remove(customer);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Customer deleted successfully" });
            }

            if (role == "worker")
            {
                var worker = await _context.Workers.FindAsync(id);

                if (worker == null)
                    return NotFound(new { message = "Worker not found" });

                _context.Workers.Remove(worker);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Worker deleted successfully" });
            }

            if (role == "admin")
            {
                var admin = await _context.Admins.FindAsync(id);

                if (admin == null)
                    return NotFound(new { message = "Admin not found" });

                _context.Admins.Remove(admin);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Admin deleted successfully" });
            }

            return BadRequest(new { message = "Invalid role" });
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
                            return BadRequest(new { message = "המשתמש לא נמצא" });

                        customer.PasswordHash = HashPassword(dto.NewPassword);
                        break;
                    }

                case "Worker":
                    {
                        var worker = await _context.Workers.FindAsync(user.Value.UserId);

                        if (worker == null)
                            return BadRequest(new { message = "המשתמש לא נמצא" });

                        worker.PasswordHash = HashPassword(dto.NewPassword);
                        break;
                    }

                case "Admin":
                    {
                        var admin = await _context.Admins.FindAsync(user.Value.UserId);

                        if (admin == null)
                            return BadRequest(new { message = "המשתמש לא נמצא" });

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
                return NotFound(new { message = "התושב לא נמצא" });

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
                return NotFound(new { message = "התושב לא נמצא" });

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

        public class UpdateReportStatusDto
        {
            public string WorkerEmail { get; set; } = "";
            public string NewStatus { get; set; } = "";
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
                Location = dto.Location,
                ImageBase64 = dto.ImageBase64,
                Latitude = dto.Latitude,
                Municipality = dto.Municipality,
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

            if (worker.IsBlocked)
                return BadRequest(new { message = "Worker account is blocked" });

            if (worker.ApprovalStatus != "Approved")
                return BadRequest(new { message = "העובד עדיין לא מאושר במערכת" });

            var report = await _context.Reports
                .FirstOrDefaultAsync(r => r.Id == reportId);

            if (report == null)
                return NotFound(new { message = "הדיווח לא נמצא" });

            if (report.Status != "Open")
                return BadRequest(new { message = "הדיווח כבר נלקח לטיפול או שאינו פתוח" });

var acceptedTime = DateTime.UtcNow;

report.Status = "In Treatment";
report.AssignedWorkerEmail = worker.Email;
report.AcceptedAt = acceptedTime;

_context.ReportStatusHistories.Add(new ReportStatusHistory
{
    ReportId = report.Id,
    OldStatus = "Open",
    NewStatus = "In Treatment",
    ChangedByWorkerEmail = worker.Email,
    ChangedAt = acceptedTime
});
_context.CustomerNotifications.Add(
    new CustomerNotification
    {
        CustomerEmail = report.CustomerEmail,
        ReportId = report.Id,
        Message = $"Your report #{report.Id} is now In Treatment.",
        CreatedAt = DateTime.UtcNow,
        IsRead = false
    });
await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "הדיווח התקבל לטיפול בהצלחה",
                reportId = report.Id,
                status = report.Status,
                assignedWorkerEmail = report.AssignedWorkerEmail,
acceptedAt = acceptedTime            });
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
[HttpPost("decline-report/{reportId}")]
public async Task<IActionResult> DeclineReport(int reportId, [FromBody] AcceptReportDto dto)
{
    if (string.IsNullOrWhiteSpace(dto.WorkerEmail))
        return BadRequest(new { message = "Worker email is required" });

    var workerEmail = dto.WorkerEmail.Trim().ToLower();

    var report = await _context.Reports.FirstOrDefaultAsync(r => r.Id == reportId);

    if (report == null)
        return NotFound(new { message = "Report not found" });

    if (report.AssignedWorkerEmail == null ||
        report.AssignedWorkerEmail.ToLower() != workerEmail)
        return BadRequest(new { message = "Only assigned worker can decline this report" });

    var oldStatus = report.Status;
    var now = DateTime.UtcNow;

    report.Status = "Open";
    report.AssignedWorkerEmail = null;
    report.AcceptedAt = null;

    _context.ReportStatusHistories.Add(new ReportStatusHistory
    {
        ReportId = report.Id,
        OldStatus = oldStatus,
        NewStatus = "Open",
        ChangedByWorkerEmail = dto.WorkerEmail,
        ChangedAt = now
    });
_context.CustomerNotifications.Add(
    new CustomerNotification
    {
        CustomerEmail = report.CustomerEmail,
        ReportId = report.Id,
        Message = $"Your report #{report.Id} has returned to Open status.",
        CreatedAt = DateTime.UtcNow,
        IsRead = false
    });
    await _context.SaveChangesAsync();

    return Ok(new
    {
        message = "Report declined and returned to Open",
        reportId = report.Id,
        status = report.Status
    });
}
        [HttpGet("reports-map")]
        public async Task<IActionResult> GetReportsMap(
            [FromQuery] string? status,
            [FromQuery] string? fromDate,
            [FromQuery] string? toDate,
            [FromQuery] string? workerEmail)
        {
            var query = _context.Reports.AsQueryable();

            if (!string.IsNullOrWhiteSpace(workerEmail))
            {
                var normalizedWorkerEmail = workerEmail.Trim().ToLowerInvariant();

                var worker = await _context.Workers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(w => w.Email.ToLower() == normalizedWorkerEmail);

                if (worker == null)
                    return NotFound(new { message = "Worker not found" });

                if (worker.IsBlocked)
                    return BadRequest(new { message = "Worker account is blocked" });

                if (worker.ApprovalStatus != "Approved")
                    return BadRequest(new { message = "Worker account is not approved" });

                query = query.Where(r =>
                    (
                        r.Status == "Open" ||
                        (r.AssignedWorkerEmail != null && r.AssignedWorkerEmail.ToLower() == normalizedWorkerEmail)
                    )
                );
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                var statuses = status.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .ToList();

                query = query.Where(r => statuses.Contains(r.Status));
            }

            if (DateTime.TryParse(fromDate, out var from))
                query = query.Where(r => r.CreatedAt >= from);

            if (DateTime.TryParse(toDate, out var to))
                query = query.Where(r => r.CreatedAt <= to.AddDays(1));

            var reports = await query
                .AsNoTracking()
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new
                {
                    id = r.Id,
                    customerEmail = r.CustomerEmail,
                    category = r.Category,
                    priority = r.Priority,
                    description = r.Description,
                    notes = r.Notes,
                    status = r.Status,
                    createdAt = r.CreatedAt,
                    latitude = r.Latitude,
                    longitude = r.Longitude,
                    location = r.Location,
                    assignedWorkerEmail = r.AssignedWorkerEmail,
                    acceptedAt = r.AcceptedAt,
                    imageBase64 = r.ImageBase64,
                    workerImageBase64 = r.WorkerImageBase64,
                    workerImageNote = r.WorkerImageNote,
                    workerImageUploadedAt = r.WorkerImageUploadedAt
                })
                .ToListAsync();

            return Ok(reports);
        }

        [HttpDelete("admin/reports/{id}")]
        public async Task<IActionResult> DeleteReport(int id)
        {
            var report = await _context.Reports.FirstOrDefaultAsync(r => r.Id == id);

            if (report == null)
            {
                return NotFound(new { message = "Report not found" });
            }

            var histories = await _context.ReportStatusHistories
                .Where(h => h.ReportId == id)
                .ToListAsync();

            if (histories.Any())
            {
                _context.ReportStatusHistories.RemoveRange(histories);
            }

            _context.Reports.Remove(report);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Report deleted successfully"
            });
        }

        [HttpGet("open-reports")]
        public async Task<IActionResult> GetOpenReports([FromQuery] string? workerEmail)
        {
            if (string.IsNullOrWhiteSpace(workerEmail))
                return BadRequest(new { message = "Worker email is required" });

            var normalizedWorkerEmail = workerEmail.Trim().ToLowerInvariant();

            var worker = await _context.Workers
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.Email.ToLower() == normalizedWorkerEmail);

            if (worker == null)
                return NotFound(new { message = "Worker not found" });

            if (worker.IsBlocked)
                return BadRequest(new { message = "Worker account is blocked" });

            if (worker.ApprovalStatus != "Approved")
                return BadRequest(new { message = "Worker account is not approved" });

            var reports = await _context.Reports
                .AsNoTracking()
                .Where(r =>
                    (
                        r.Status == "Open" ||
                        (
                            r.AssignedWorkerEmail != null &&
                            r.AssignedWorkerEmail.ToLower() == normalizedWorkerEmail
                        )
                    )
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
                    location = r.Location,
                    imageBase64 = r.ImageBase64,
                    latitude = r.Latitude,
                    longitude = r.Longitude,
                    status = r.Status,
                    assignedWorkerEmail = r.AssignedWorkerEmail,
                    acceptedAt = r.AcceptedAt,
                    createdAt = r.CreatedAt,
                    workerImageBase64 = r.WorkerImageBase64,
                    workerImageNote = r.WorkerImageNote,
                    workerImageUploadedAt = r.WorkerImageUploadedAt
                })
                .ToListAsync();

            return Ok(reports);
        }
[HttpGet("worker-notifications")]
public async Task<IActionResult> GetWorkerNotifications([FromQuery] string workerEmail)
{
    if (string.IsNullOrWhiteSpace(workerEmail))
        return BadRequest(new { message = "Worker email is required" });

    var normalizedWorkerEmail = workerEmail.Trim().ToLowerInvariant();

    var worker = await _context.Workers
        .AsNoTracking()
        .FirstOrDefaultAsync(w => w.Email.ToLower() == normalizedWorkerEmail);

    if (worker == null)
        return NotFound(new { message = "Worker not found" });

    if (worker.IsBlocked)
        return BadRequest(new { message = "Worker account is blocked" });

    if (worker.ApprovalStatus != "Approved")
        return BadRequest(new { message = "Worker account is not approved" });

    var allowedCategories = GetCategoriesForDepartment(worker.Department);
    var workerMunicipality = worker.Municipality.Trim();

    var reports = await _context.Reports
        .AsNoTracking()
        .Where(r =>
            r.Status == "Open" &&
            string.IsNullOrWhiteSpace(r.AssignedWorkerEmail) &&
            allowedCategories.Contains(r.Category) &&
            (
                r.Municipality == workerMunicipality ||
                r.Location.Contains(workerMunicipality)
            )
        )
        .OrderByDescending(r => r.CreatedAt)
        .Select(r => new
        {
            id = r.Id,
            category = r.Category,
            priority = r.Priority,
            description = r.Description,
            location = r.Location,
            municipality = r.Municipality,
            createdAt = r.CreatedAt
        })
        .ToListAsync();

    return Ok(reports);
}


        [HttpGet("report-details/{reportId}")]
        public async Task<IActionResult> GetReportDetails(int reportId, [FromQuery] string? workerEmail)
        {
            var reportEntity = await _context.Reports
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == reportId);

            if (reportEntity == null)
            {
                return NotFound(new { message = "הדיווח לא נמצא" });
            }
var customer = await _context.Customers
    .AsNoTracking()
    .FirstOrDefaultAsync(c => c.Email.ToLower() == reportEntity.CustomerEmail.ToLower());
            if (!string.IsNullOrWhiteSpace(workerEmail))
            {
                var normalizedWorkerEmail = workerEmail.Trim().ToLowerInvariant();

                var worker = await _context.Workers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(w => w.Email.ToLower() == normalizedWorkerEmail);

                if (worker == null)
                    return NotFound(new { message = "Worker not found" });

                if (worker.IsBlocked)
                    return BadRequest(new { message = "Worker account is blocked" });

                if (worker.ApprovalStatus != "Approved")
                    return BadRequest(new { message = "Worker account is not approved" });

                if (!string.IsNullOrWhiteSpace(reportEntity.AssignedWorkerEmail) &&
                    reportEntity.AssignedWorkerEmail.ToLower() != normalizedWorkerEmail)
                {
                    return Forbid();
                }
            }

            var report = new
            {
                id = reportEntity.Id,
                customerEmail = reportEntity.CustomerEmail,
                category = reportEntity.Category,
                priority = reportEntity.Priority,
                description = reportEntity.Description,
                notes = reportEntity.Notes,
                location = reportEntity.Location,
                imageBase64 = reportEntity.ImageBase64,
                latitude = reportEntity.Latitude,
                longitude = reportEntity.Longitude,
                status = reportEntity.Status,
                assignedWorkerEmail = reportEntity.AssignedWorkerEmail,
                acceptedAt = reportEntity.AcceptedAt,
                createdAt = reportEntity.CreatedAt,
                workerImageBase64 = reportEntity.WorkerImageBase64,
                workerImageNote = reportEntity.WorkerImageNote,
                customerName = customer != null ? customer.FullName : "-",
customerPhone = customer != null ? customer.Phone : "-",
                workerImageUploadedAt = reportEntity.WorkerImageUploadedAt
            };

            return Ok(report);
        }

        [HttpGet("assigned-reports")]
        public async Task<IActionResult> GetAssignedReports([FromQuery] string workerEmail)
        {
            if (string.IsNullOrWhiteSpace(workerEmail))
            {
                return BadRequest(new { message = "Worker email is required" });
            }

            var normalizedWorkerEmail = workerEmail.Trim().ToLowerInvariant();

            var reports = await _context.Reports
                .AsNoTracking()
                .Where(r =>
                    r.AssignedWorkerEmail != null &&
                    r.AssignedWorkerEmail.ToLower() == normalizedWorkerEmail)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new
                {
                    id = r.Id,
                    title = r.Category,
                    category = r.Category,
                    description = r.Description,
                    notes = r.Notes,
                    status = r.Status,
                    priority = r.Priority,
                    createdAt = r.CreatedAt,
                    customerEmail = r.CustomerEmail,
                    location = r.Location,
                    latitude = r.Latitude,
                    longitude = r.Longitude,
                    assignedWorkerEmail = r.AssignedWorkerEmail,
                    acceptedAt = r.AcceptedAt,
                    imageBase64 = r.ImageBase64,
                    workerImageBase64 = r.WorkerImageBase64,
                    workerImageNote = r.WorkerImageNote,
                    workerImageUploadedAt = r.WorkerImageUploadedAt
                })
                .ToListAsync();

            return Ok(reports);
        }

        [HttpPut("update-report-status/{reportId}")]
        public async Task<IActionResult> UpdateReportStatus(
            int reportId,
            [FromBody] UpdateReportStatusDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.WorkerEmail))
                return Unauthorized(new { message = "יש להתחבר כעובד" });

            var worker = await _context.Workers
                .FirstOrDefaultAsync(w =>
                    w.Email.ToLower() == dto.WorkerEmail.ToLower() &&
                    w.ApprovalStatus == "Approved");

            if (worker == null)
                return Unauthorized(new { message = "אין הרשאה לעדכן סטטוס" });

            if (worker.IsBlocked)
                return BadRequest(new { message = "Worker account is blocked" });

  var allowedStatuses = new[]
{
    "In Treatment",
    "Completed"
};

            if (!allowedStatuses.Contains(dto.NewStatus))
                return BadRequest(new { message = "סטטוס לא חוקי" });

            var report = await _context.Reports.FindAsync(reportId);

            if (report == null)
                return NotFound(new { message = "הקריאה לא נמצאה" });

            if (report.AssignedWorkerEmail != null &&
                report.AssignedWorkerEmail.ToLower() != dto.WorkerEmail.ToLower())
            {
                return Forbid();
            }

            var oldStatus = report.Status;

            report.Status = dto.NewStatus;
if (dto.NewStatus == "Completed")
{
    _context.CustomerNotifications.Add(
        new CustomerNotification
        {
            CustomerEmail = report.CustomerEmail,
            ReportId = report.Id,
            Message = $"Your report #{report.Id} has been completed and closed.",
            CreatedAt = DateTime.UtcNow,
            IsRead = false
        });
}
            var history = new ReportStatusHistory
            {
                ReportId = report.Id,
                OldStatus = oldStatus,
                NewStatus = dto.NewStatus,
                ChangedByWorkerEmail = dto.WorkerEmail,
                ChangedAt = DateTime.UtcNow
            };

            _context.ReportStatusHistories.Add(history);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "סטטוס הקריאה עודכן בהצלחה",
                reportId = report.Id,
                oldStatus,
                newStatus = report.Status,
                changedBy = dto.WorkerEmail,
                changedAt = history.ChangedAt
            });
        }
[HttpGet("customer-notifications")]
public async Task<IActionResult> CustomerNotifications(string email)
{
    var notifications = await _context.CustomerNotifications
        .Where(n => n.CustomerEmail == email)
        .OrderByDescending(n => n.CreatedAt)
        .ToListAsync();

    return Ok(notifications);
}
        [HttpGet("customer-reports")]
        public async Task<IActionResult> GetCustomerReports([FromQuery] string email){
            if (string.IsNullOrWhiteSpace(email))
                return BadRequest(new { message = "האימייל נדרש" });

            var normalizedEmail = email.Trim().ToLowerInvariant();

            var reports = await _context.Reports
                .AsNoTracking()
                .Where(r => r.CustomerEmail.ToLower() == normalizedEmail)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new
                {
                    id = r.Id,
                    category = r.Category,
                    priority = r.Priority,
                    description = r.Description,
                    notes = r.Notes,
                    status = r.Status,
                    createdAt = r.CreatedAt,
                    location = r.Location,
                    latitude = r.Latitude,
                    longitude = r.Longitude,
                    imageBase64 = r.ImageBase64,
                    assignedWorkerEmail = r.AssignedWorkerEmail,
                    acceptedAt = r.AcceptedAt
                })
                .ToListAsync();

            return Ok(reports);
        }

        [HttpGet("report-status-history/{reportId}")]
        public async Task<IActionResult> GetReportStatusHistory(int reportId)
        {
            var history = await _context.ReportStatusHistories
                .Where(h => h.ReportId == reportId)
                .OrderByDescending(h => h.ChangedAt)
                .ToListAsync();

            return Ok(history);
        }

        [HttpGet("admin/statistics")]
        public async Task<IActionResult> GetAdminStatistics()
        {
            var reports = await _context.Reports.AsNoTracking().ToListAsync();

            var totalReports = reports.Count;
            var openReports = reports.Count(r => r.Status == "Open");
            var inTreatmentReports = reports.Count(r => r.Status == "In Treatment");
            var resolvedReports = reports.Count(r => r.Status == "Completed" || r.Status == "Closed");

            var resolutionRate = totalReports > 0
                ? Math.Round((double)resolvedReports / totalReports * 100, 1)
                : 0;

            var resolvedHistories = await _context.ReportStatusHistories
                .AsNoTracking()
                .Where(h => h.NewStatus == "Completed" || h.NewStatus == "Closed")
                .ToListAsync();

            double averageHandlingDays = 0;
            var resolvedReportsList = reports.Where(r => r.Status == "Completed" || r.Status == "Closed").ToList();
            if (resolvedReportsList.Count > 0)
            {
                var handlingDays = new List<double>();
                foreach (var report in resolvedReportsList)
                {
                    var completedEntry = resolvedHistories
                        .Where(h => h.ReportId == report.Id)
                        .OrderByDescending(h => h.ChangedAt)
                        .FirstOrDefault();

                    if (completedEntry != null)
                        handlingDays.Add(Math.Max(0, (completedEntry.ChangedAt - report.CreatedAt).TotalDays));
                }

                if (handlingDays.Count > 0)
                    averageHandlingDays = Math.Round(handlingDays.Average(), 1);
            }

            var approvedWorkersCount = await _context.Workers.CountAsync(w => w.ApprovalStatus == "Approved");
            var workerPerformance = approvedWorkersCount > 0
                ? Math.Round((double)resolvedReports / approvedWorkersCount, 1)
                : 0;

            var reportsByCategory = reports
                .GroupBy(r => r.Category)
                .Select(g => new { category = g.Key, count = g.Count() })
                .OrderByDescending(x => x.count)
                .ToList<object>();

            var statusDistribution = reports
                .GroupBy(r => r.Status)
                .Select(g => new { status = g.Key, count = g.Count() })
                .ToList<object>();

            var now = DateTime.UtcNow;
            var monthlyTrends = new List<object>();
            for (int i = 5; i >= 0; i--)
            {
                var month = now.AddMonths(-i);
                var monthStart = new DateTime(month.Year, month.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                var monthEnd = monthStart.AddMonths(1);

                monthlyTrends.Add(new
                {
                    month = month.ToString("MMM yyyy"),
                    newReports = reports.Count(r => r.CreatedAt >= monthStart && r.CreatedAt < monthEnd),
                    resolved = resolvedHistories.Count(h => h.ChangedAt >= monthStart && h.ChangedAt < monthEnd)
                });
            }

            var priorityDistribution = reports
                .GroupBy(r => r.Priority)
                .Select(g => new { priority = g.Key, count = g.Count() })
                .ToList<object>();

            return Ok(new
            {
                totalReports,
                openReports,
                inTreatmentReports,
                resolvedReports,
                averageHandlingDays,
                resolutionRate,
                workerPerformance,
                customerSatisfaction = 4.2,
                reportsByCategory,
                statusDistribution,
                monthlyTrends,
                priorityDistribution
            });
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

        private static List<string> GetCategoriesForDepartment(string? department)
        {
            var value = NormalizeDepartmentText(department);

            if (string.IsNullOrWhiteSpace(value))
                return new List<string>();

            if (value.Contains("road") || value.Contains("roads") || value.Contains("street") || value.Contains("כביש") || value.Contains("תשתיות"))
                return new List<string> { "Road Damage", "נזק בכביש" };

            if (value.Contains("light") || value.Contains("lighting") || value.Contains("electric") || value.Contains("electricity") || value.Contains("תאורה") || value.Contains("חשמל"))
                return new List<string> { "Street Lighting", "תאורת רחוב" };

            if (value.Contains("garbage") || value.Contains("sanitation") || value.Contains("waste") || value.Contains("clean") || value.Contains("אשפה") || value.Contains("זבל") || value.Contains("ניקיון"))
                return new List<string> { "Garbage / Sanitation", "אשפה / ניקיון" };

            if (value.Contains("garden") || value.Contains("gardening") || value.Contains("park") || value.Contains("גינון") || value.Contains("גן") || value.Contains("עצים"))
                return new List<string> { "Gardening", "גינון" };

            if (value.Contains("water") || value.Contains("sewage") || value.Contains("sewer") || value.Contains("מים") || value.Contains("ביוב"))
                return new List<string> { "Water / Sewage", "מים / ביוב" };

            if (value.Contains("maintenance") || value.Contains("general") || value.Contains("תחזוקה") || value.Contains("כללי"))
                return new List<string> { "General Maintenance", "תחזוקה כללית" };

            return new List<string>();
        }

        private static bool CanWorkerHandleCategory(string? department, string? category)
        {
            if (string.IsNullOrWhiteSpace(category))
                return false;

            var allowedCategories = GetCategoriesForDepartment(department);
            return allowedCategories.Any(c => string.Equals(c, category.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        private static string NormalizeDepartmentText(string? value)
        {
            return (value ?? "")
                .Trim()
                .ToLowerInvariant()
                .Replace("-", " ")
                .Replace("_", " ");
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
