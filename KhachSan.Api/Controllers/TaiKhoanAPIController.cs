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
using KhachSan.Models;

namespace KhachSan.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDBContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;
        private string? userIdClaim;

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
                var user = await _context.NguoiDung
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.TenDangNhap == model.TenDN);

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

                if (user.VaiTro != "Nhân viên" && user.VaiTro != "Khách hàng" && user.VaiTro != "Quản trị")
                {
                    _logger.LogWarning("Đăng nhập thất bại: Vai trò không được phép - {TenDN}, VaiTro: {VaiTro}", model.TenDN, user.VaiTro);
                    return Unauthorized(new { success = false, message = "Chỉ nhân viên, khách hàng hoặc quản trị viên mới được phép đăng nhập vào hệ thống này." });
                }

                var token = GenerateJwtToken(user);
                _logger.LogInformation("Đăng nhập thành công cho người dùng {TenDN}", user.TenDangNhap);

                return Ok(new
                {
                    success = true,
                    token,
                    username = user.TenDangNhap,
                    vaitro = user.VaiTro,
                    hoTen = user.HoTen,
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
                if (await _context.NguoiDung.AnyAsync(u => u.TenDangNhap == model.TenDN))
                {
                    return BadRequest(new { success = false, message = "Tên đăng nhập đã tồn tại" });
                }

                if (await _context.NguoiDung.AnyAsync(u => u.Email == model.Email))
                {
                    return BadRequest(new { success = false, message = "Email đã tồn tại" });
                }

                try
                {
                    var emailAddress = new System.Net.Mail.MailAddress(model.Email);
                }
                catch (FormatException)
                {
                    return BadRequest(new { success = false, message = "Email không đúng định dạng" });
                }

                if (model.MatKhau != model.Matkhaunhaplai)
                {
                    return BadRequest(new { success = false, message = "Mật khẩu nhập lại không khớp" });
                }

                if (model.MatKhau.Length < 8)
                {
                    return BadRequest(new { success = false, message = "Mật khẩu phải dài ít nhất 8 ký tự" });
                }

                if (!System.Text.RegularExpressions.Regex.IsMatch(model.Dienthoai, @"^\d{10,11}$"))
                {
                    return BadRequest(new { success = false, message = "Số điện thoại phải là 10 hoặc 11 chữ số" });
                }

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
        /// Đổi mật khẩu của người dùng hiện tại.
        /// </summary>
        /// <param name="model">Thông tin mật khẩu hiện tại, mật khẩu mới và xác nhận mật khẩu mới.</param>
        /// <returns>Thông báo đổi mật khẩu thành công hoặc lỗi.</returns>
        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] DoiMatKhauViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ", errors });
            }

            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    _logger.LogWarning("Không thể xác định người dùng từ token JWT.");
                    return Unauthorized(new { success = false, message = "Không thể xác định người dùng. Vui lòng đăng nhập lại." });
                }

                var user = await _context.NguoiDung
                    .FirstOrDefaultAsync(u => u.MaNguoiDung == userId);
                if (user == null)
                {
                    _logger.LogWarning("Người dùng không tồn tại: MaNguoiDung = {UserId}", userId);
                    return NotFound(new { success = false, message = "Người dùng không tồn tại." });
                }

                if (!VerifyPassword(model.MatKhauHienTai, user.MatKhau))
                {
                    _logger.LogWarning("Mật khẩu hiện tại không đúng cho người dùng: {TenDN}", user.TenDangNhap);
                    return BadRequest(new { success = false, message = "Mật khẩu hiện tại không đúng." });
                }

                if (VerifyPassword(model.MatKhauMoi, user.MatKhau))
                {
                    _logger.LogWarning("Mật khẩu mới trùng với mật khẩu hiện tại cho người dùng: {TenDN}", user.TenDangNhap);
                    return BadRequest(new { success = false, message = "Mật khẩu mới không được trùng với mật khẩu hiện tại." });
                }

                user.MatKhau = BCrypt.Net.BCrypt.HashPassword(model.MatKhauMoi);
                _context.NguoiDung.Update(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Đổi mật khẩu thành công cho người dùng: {TenDN}", user.TenDangNhap);
                return Ok(new { success = true, message = "Đổi mật khẩu thành công! Vui lòng đăng nhập lại." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi đổi mật khẩu cho người dùng với MaNguoiDung = {UserId}", userIdClaim);
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống. Vui lòng thử lại sau." });
            }
        }

        /// <summary>
        /// Lấy thông tin người dùng hiện tại dựa trên JWT token.
        /// </summary>
        /// <returns>Thông tin người dùng nếu xác thực thành công.</returns>
        [HttpGet("user")]
        [Authorize]
        public async Task<IActionResult> GetUserInfo()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    _logger.LogWarning("Không thể xác định người dùng từ token JWT.");
                    return Unauthorized(new { success = false, message = "Không thể xác định người dùng. Vui lòng đăng nhập lại." });
                }

                var user = await _context.NguoiDung
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.MaNguoiDung == userId);
                if (user == null)
                {
                    _logger.LogWarning("Người dùng không tồn tại: MaNguoiDung = {UserId}", userId);
                    return NotFound(new { success = false, message = "Người dùng không tồn tại." });
                }

                _logger.LogInformation("Lấy thông tin thành công cho người dùng: {TenDN}", user.TenDangNhap);
                return Ok(new
                {
                    success = true,
                    user = new
                    {
                        maNguoiDung = user.MaNguoiDung,
                        hoTen = user.HoTen,
                        tenDangNhap = user.TenDangNhap,
                        email = user.Email,
                        soDienThoai = user.SoDienThoai,
                        vaiTro = user.VaiTro,
                        trangThai = user.TrangThai
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy thông tin người dùng với MaNguoiDung = {UserId}", userIdClaim);
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
                new Claim(ClaimTypes.NameIdentifier, user.MaNguoiDung.ToString()),
                new Claim("UserId", user.MaNguoiDung.ToString()),
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