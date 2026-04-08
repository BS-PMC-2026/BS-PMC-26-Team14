using System.Security.Cryptography;
using System.Text;
using CityFix.Api.Data;
using CityFix.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CityFix.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        // Admin זמני קבוע בקוד
        private const string AdminEmail = "admin@cityfix.com";
        private const string AdminPassword = "1234";
        private const string AdminFullName = "מנהל מערכת";

        public AuthController(ApplicationDbContext context)
        {
            _context = context;
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
                email = customer.Email
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
        email = worker.Email
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