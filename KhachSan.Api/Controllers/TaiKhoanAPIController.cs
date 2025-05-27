using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using KhachSan.Api.Data;
using KhachSan.Api.Models;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace KhachSan.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDBContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;

        public AuthController(ApplicationDBContext context, IConfiguration configuration, ILogger<AuthController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Đăng nhập người dùng và tạo JWT token.
        /// </summary>
        /// <param name="model">Thông tin đăng nhập (tên đăng nhập, mật khẩu).</param>
        /// <returns>Token JWT nếu đăng nhập thành công.</returns>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] DangNhapViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ", errors });
            }

            try
            {
                // Tìm người dùng theo tên đăng nhập
                var user = await _context.NguoiDung
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.TenDangNhap == model.TenDN);

                // Kiểm tra người dùng và mật khẩu
                if (user == null)
                {
                    _logger.LogWarning("Đăng nhập thất bại: Tên đăng nhập không tồn tại - {TenDN}", model.TenDN);
                    return Unauthorized(new { success = false, message = "Tên đăng nhập không tồn tại" });
                }

                if (!VerifyPassword(model.MatKhau, user.MatKhau))
                {
                    _logger.LogWarning("Đăng nhập thất bại: Mật khẩu sai cho người dùng {TenDN}", model.TenDN);
                    return Unauthorized(new { success = false, message = "Mật khẩu không đúng" });
                }

                // Kiểm tra vai trò của người dùng
                if (user.VaiTro != "Nhân viên" && user.VaiTro != "Quản trị")
                {
                    _logger.LogWarning("Đăng nhập thất bại: Vai trò không được phép - {TenDN}, VaiTro: {VaiTro}", model.TenDN, user.VaiTro);
                    return Unauthorized(new { success = false, message = "Chỉ nhân viên hoặc quản trị viên mới được phép đăng nhập vào hệ thống này." });
                }

                // Tạo JWT token
                var token = GenerateJwtToken(user);
                _logger.LogInformation("Đăng nhập thành công cho người dùng {TenDN}", user.TenDangNhap);

                return Ok(new
                {
                    success = true,
                    token,
                    username = user.TenDangNhap,
                    vaitro = user.VaiTro,
                    remember = model.RememberMe
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi đăng nhập cho {TenDN}", model.TenDN);
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống. Vui lòng thử lại sau." });
            }
        }

        /// <summary>
        /// Đăng ký người dùng mới.
        /// </summary>
        /// <param name="model">Thông tin đăng ký (tên đăng nhập, mật khẩu, email, v.v.).</param>
        /// <returns>Thông báo đăng ký thành công hoặc lỗi.</returns>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] DangKyViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ", errors });
            }

            try
            {
                // Kiểm tra trùng lặp tên đăng nhập và email
                if (await _context.NguoiDung.AnyAsync(u => u.TenDangNhap == model.TenDN))
                {
                    return BadRequest(new { success = false, message = "Tên đăng nhập đã tồn tại" });
                }

                if (await _context.NguoiDung.AnyAsync(u => u.Email == model.Email))
                {
                    return BadRequest(new { success = false, message = "Email đã tồn tại" });
                }

                // Validate email
                try
                {
                    var emailAddress = new System.Net.Mail.MailAddress(model.Email);
                }
                catch (FormatException)
                {
                    return BadRequest(new { success = false, message = "Email không đúng định dạng" });
                }

                // Validate mật khẩu
                if (model.MatKhau != model.Matkhaunhaplai)
                {
                    return BadRequest(new { success = false, message = "Mật khẩu nhập lại không khớp" });
                }

                if (model.MatKhau.Length < 8)
                {
                    return BadRequest(new { success = false, message = "Mật khẩu phải dài ít nhất 8 ký tự" });
                }

                // Validate điện thoại
                if (!System.Text.RegularExpressions.Regex.IsMatch(model.Dienthoai, @"^\d{10,11}$"))
                {
                    return BadRequest(new { success = false, message = "Số điện thoại phải là 10 hoặc 11 chữ số" });
                }

                // Tạo người dùng mới
                var user = new NguoiDung
                {
                    HoTen = model.HotenKH?.Trim(),
                    TenDangNhap = model.TenDN?.Trim(),
                    MatKhau = BCrypt.Net.BCrypt.HashPassword(model.MatKhau),
                    Email = model.Email?.Trim(),
                    SoDienThoai = model.Dienthoai?.Trim(),
                    NgayTao = DateTime.UtcNow,
                    VaiTro = "Khách hàng",
                    TrangThai = "Hoạt động"
                };

                _context.NguoiDung.Add(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Đăng ký thành công cho người dùng {TenDN}", user.TenDangNhap);
                return Ok(new { success = true, message = "Đăng ký thành công! Vui lòng đăng nhập." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi đăng ký người dùng {TenDN}", model.TenDN);
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống. Vui lòng thử lại sau." });
            }
        }

        /// <summary>
        /// Đăng xuất người dùng.
        /// </summary>
        /// <returns>Thông báo đăng xuất thành công hoặc lỗi.</returns>
        [HttpPost("logout")]
        public IActionResult Logout()
        {
            _logger.LogInformation("User logged out successfully.");
            return Ok(new { success = true, message = "Đăng xuất thành công" });
        }

        /// <summary>
        /// Tạo JWT token cho người dùng.
        /// </summary>
        /// <param name="user">Thông tin người dùng.</param>
        /// <returns>JWT token dưới dạng chuỗi.</returns>
        private string GenerateJwtToken(NguoiDung user)
        {
            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.MaNguoiDung.ToString()), // Thêm claim NameIdentifier
        new Claim("UserId", user.MaNguoiDung.ToString()), // Giữ lại claim UserId nếu cần
        new Claim("UserName", user.TenDangNhap),
        new Claim(ClaimTypes.Role, user.VaiTro ?? "NhanVien"),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
    };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddDays(1),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /// <summary>
        /// Kiểm tra mật khẩu người dùng.
        /// </summary>
        /// <param name="inputPassword">Mật khẩu nhập vào.</param>
        /// <param name="storedPassword">Mật khẩu đã mã hóa trong cơ sở dữ liệu.</param>
        /// <returns>True nếu mật khẩu khớp, False nếu không khớp.</returns>
        private bool VerifyPassword(string inputPassword, string storedPassword)
        {
            if (string.IsNullOrEmpty(inputPassword) || string.IsNullOrEmpty(storedPassword))
            {
                return false;
            }

            // Nếu mật khẩu trong cơ sở dữ liệu không được mã hóa bằng BCrypt, so sánh trực tiếp
            if (!storedPassword.StartsWith("$2a$") && !storedPassword.StartsWith("$2b$"))
            {
                return inputPassword == storedPassword;
            }

            try
            {
                return BCrypt.Net.BCrypt.Verify(inputPassword, storedPassword);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi kiểm tra mật khẩu");
                return false;
            }
        }
    }
}