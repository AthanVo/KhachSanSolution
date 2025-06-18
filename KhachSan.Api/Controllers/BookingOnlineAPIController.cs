using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using KhachSan.Api.Data;
using KhachSan.Api.Models;
using KhachSan.Api.Services;

namespace KhachSan.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BookingOnlineAPIController : ControllerBase
    {
        private readonly ApplicationDBContext _context;
        private readonly ILogger<BookingOnlineAPIController> _logger;
        private readonly EmailService _emailService;

        public BookingOnlineAPIController(
            ApplicationDBContext context,
            ILogger<BookingOnlineAPIController> logger,
            EmailService emailService)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        }

        [HttpGet("GetRoomsOnline")]
        public async Task<IActionResult> GetRoomsOnline()
        {
            try
            {
                var rooms = await _context.Phong
                    .Include(p => p.LoaiPhong)
                    .Where(p => !p.DangSuDung)
                    .Select(p => new
                    {
                        maPhong = p.MaPhong,
                        soPhong = p.SoPhong,
                        dangSuDung = p.DangSuDung,
                        trangThai = p.DangSuDung ? "Đang sử dụng" : "Trống",
                        loaiPhong = p.LoaiPhong.TenLoaiPhong,
                        giaTheoGio = p.LoaiPhong.GiaTheoGio,
                        giaTheoNgay = p.LoaiPhong.GiaTheoNgay
                    })
                    .ToListAsync();

                if (!rooms.Any())
                {
                    return Ok(new { success = true, rooms = new List<object>(), message = "Hiện tại không có phòng trống." });
                }

                return Ok(new { success = true, rooms });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tải danh sách phòng trống: {Message}, InnerException: {InnerException}", ex.Message, ex.InnerException?.Message);
                return StatusCode(500, new { success = false, message = $"Lỗi khi tải danh sách phòng trống: {ex.Message}" });
            }
        }

        /// <summary>
        /// Đặt phòng online cho khách hàng.
        /// </summary>
        /// <param name="model">Thông tin đặt phòng từ khách hàng.</param>
        /// <returns>Kết quả đặt phòng (thành công hoặc lỗi).</returns>
        [HttpPost("BookRoomOnline")]
        [Authorize(Roles = "Khách hàng,Quản trị")]
        public async Task<IActionResult> BookRoomOnline([FromBody] BookRoomModel model)
        {
            try
            {
                // Log toàn bộ payload để debug
                _logger.LogInformation("Payload nhận được: {@Model}", model);

                // Kiểm tra dữ liệu đầu vào
                if (model == null)
                {
                    _logger.LogWarning("Payload rỗng");
                    return BadRequest(new { success = false, message = "Payload không hợp lệ." });
                }

                if (model.MaPhong <= 0)
                {
                    _logger.LogWarning("MaPhong không hợp lệ: {MaPhong}", model.MaPhong);
                    return BadRequest(new { success = false, message = "Mã phòng không hợp lệ." });
                }

                if (string.IsNullOrEmpty(model.LoaiDatPhong))
                {
                    _logger.LogWarning("LoaiDatPhong rỗng");
                    return BadRequest(new { success = false, message = "Loại đặt phòng không được để trống." });
                }

                if (string.IsNullOrEmpty(model.SoGiayTo))
                {
                    _logger.LogWarning("SoGiayTo rỗng");
                    return BadRequest(new { success = false, message = "Số giấy tờ không được để trống." });
                }

                if (string.IsNullOrEmpty(model.HoTen))
                {
                    _logger.LogWarning("HoTen rỗng");
                    return BadRequest(new { success = false, message = "Họ tên không được để trống." });
                }

                if (!model.NgayNhanPhong.HasValue)
                {
                    _logger.LogWarning("NgayNhanPhong rỗng hoặc không hợp lệ");
                    return BadRequest(new { success = false, message = "Ngày nhận phòng không hợp lệ hoặc không được để trống." });
                }

                if (!model.NgayTraPhong.HasValue)
                {
                    _logger.LogWarning("NgayTraPhong rỗng hoặc không hợp lệ");
                    return BadRequest(new { success = false, message = "Ngày trả phòng không hợp lệ hoặc không được để trống." });
                }

                // Kiểm tra ngày nhận và trả phòng
                if (model.NgayNhanPhong.Value.Date < DateTime.Now.Date)
                {
                    _logger.LogWarning("Ngày nhận phòng không hợp lệ: {NgayNhanPhong}", model.NgayNhanPhong);
                    return BadRequest(new { success = false, message = "Ngày nhận phòng phải từ hôm nay trở đi." });
                }

                if (model.NgayTraPhong.Value.Date <= model.NgayNhanPhong.Value.Date)
                {
                    _logger.LogWarning("Ngày trả phòng không hợp lệ: NgayTraPhong {NgayTraPhong} <= NgayNhanPhong {NgayNhanPhong}",
                        model.NgayTraPhong, model.NgayNhanPhong);
                    return BadRequest(new { success = false, message = "Ngày trả phòng phải sau ngày nhận phòng." });
                }

                _logger.LogInformation($"Đang kiểm tra phòng với MaPhong: {model.MaPhong}");

                // Kiểm tra phòng tồn tại và chưa được sử dụng
                var phong = await _context.Phong
                    .Include(p => p.LoaiPhong) // Đảm bảo Include LoaiPhong để truy cập được
                    .Where(p => p.MaPhong == model.MaPhong && !p.DangSuDung)
                    .Select(p => new { p.MaPhong, p.DangSuDung, p.SoPhong, LoaiPhong = p.LoaiPhong.TenLoaiPhong })
                    .FirstOrDefaultAsync();
                if (phong == null)
                {
                    _logger.LogWarning($"Phòng không tồn tại hoặc đang sử dụng: MaPhong = {model.MaPhong}");
                    return NotFound(new { success = false, message = "Phòng không tồn tại hoặc đang sử dụng." });
                }

                // Kiểm tra lịch sử đặt phòng
                var conflictingBooking = await _context.DatPhong
                    .AnyAsync(dp => dp.MaPhong == model.MaPhong &&
                                    dp.TrangThai != "Đã hủy" &&
                                    ((model.NgayNhanPhong >= dp.NgayNhanPhongDuKien && model.NgayNhanPhong < dp.NgayTraPhong) ||
                                     (model.NgayTraPhong > dp.NgayNhanPhongDuKien && model.NgayTraPhong <= dp.NgayTraPhong) ||
                                     (model.NgayNhanPhong <= dp.NgayNhanPhongDuKien && model.NgayTraPhong >= dp.NgayTraPhong)));
                if (conflictingBooking)
                {
                    _logger.LogWarning($"Phòng đã được đặt trong khoảng thời gian: MaPhong = {model.MaPhong}, NgayNhanPhong = {model.NgayNhanPhong}, NgayTraPhong = {model.NgayTraPhong}");
                    return BadRequest(new { success = false, message = "Phòng đã được đặt trong khoảng thời gian yêu cầu." });
                }

                // Lấy mã người dùng từ token
                if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int maNguoiDung))
                {
                    _logger.LogWarning("Không thể lấy mã người dùng từ token JWT.");
                    return Unauthorized(new { success = false, message = "Không thể xác định người dùng. Vui lòng đăng nhập lại." });
                }

                // Lấy thông tin người dùng để lấy email và số điện thoại
                var nguoiDung = await _context.NguoiDung
                    .Where(u => u.MaNguoiDung == maNguoiDung)
                    .Select(u => new { u.HoTen, u.Email, u.SoDienThoai })
                    .FirstOrDefaultAsync();
                if (nguoiDung == null)
                {
                    _logger.LogWarning($"Người dùng không tồn tại: MaNguoiDung = {maNguoiDung}");
                    return NotFound(new { success = false, message = "Người dùng không tồn tại." });
                }

                // Xử lý thông tin khách hàng lưu trú
                var khachHang = await _context.KhachHangLuuTru
                    .Where(kh => kh.SoGiayTo == model.SoGiayTo)
                    .FirstOrDefaultAsync();
                if (khachHang == null)
                {
                    khachHang = new KhachHangLuuTru
                    {
                        LoaiGiayTo = model.LoaiGiayTo,
                        SoGiayTo = model.SoGiayTo,
                        HoTen = model.HoTen?.Trim(),
                        DiaChi = model.DiaChi?.Trim(),
                        QuocTich = model.QuocTich?.Trim(),
                        NgayTao = DateTime.Now
                    };
                    _context.KhachHangLuuTru.Add(khachHang);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    khachHang.HoTen = model.HoTen?.Trim();
                    khachHang.DiaChi = model.DiaChi?.Trim();
                    khachHang.QuocTich = model.QuocTich?.Trim();
                    _context.KhachHangLuuTru.Update(khachHang);
                }

                // Tạo bản ghi đặt phòng
                var datPhong = new DatPhong
                {
                    MaPhong = phong.MaPhong,
                    MaKhachHangLuuTru = khachHang.MaKhachHangLuuTru,
                    MaNhanVien = 1, // ← THÊM: Gán mã nhân viên mặc định
                    MaNguoiDung = maNguoiDung,
                    NgayNhanPhong = model.NgayNhanPhong.Value, // ← THÊM: Thiếu trường này
                    NgayNhanPhongDuKien = model.NgayNhanPhong.Value,
                    NgayTraPhong = model.NgayTraPhong.Value,
                    NgayTao = DateTime.Now,
                    NgayCapNhat = DateTime.Now,
                    TrangThai = "Đã nhận phòng",
                    TrangThaiThanhToan = "Chưa thanh toán", // ← THÊM: Nếu bắt buộc
                    TrangThaiBaoCaoTamTru = "Chưa báo cáo", // ← THÊM: Nếu bắt buộc  
                    LoaiDatPhong = model.LoaiDatPhong,
                };
                _context.DatPhong.Add(datPhong);

                // Cập nhật trạng thái phòng nếu cần
                var phongUpdate = await _context.Phong.FindAsync(model.MaPhong);
                if (phongUpdate == null)
                {
                    _logger.LogError($"Không tìm thấy phòng để cập nhật: MaPhong = {model.MaPhong}");
                    return NotFound(new { success = false, message = "Phòng không tồn tại." });
                }

                if (model.NgayNhanPhong.Value.Date == DateTime.Now.Date)
                {
                    phongUpdate.DangSuDung = true;
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation($"Đã thêm đặt phòng online: MaDatPhong = {datPhong.MaDatPhong}");

                // Gửi email xác nhận nếu người dùng có email
                if (!string.IsNullOrEmpty(nguoiDung.Email))
                {
                    try
                    {
                        var emailSubject = "Xác nhận đặt phòng tại Khách Sạn Đức Thịnh";
                        // Nội dung HTML từ visual_email.html được nhúng trực tiếp
                        var emailTemplate = @"
<!DOCTYPE html>
<html lang=""vi"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Xác Nhận Đặt Phòng - Khách Sạn Đức Thịnh</title>
    <style>
        body {
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            line-height: 1.6;
            color: #333;
            max-width: 600px;
            margin: 0 auto;
            padding: 20px;
            background-color: #f4f4f4;
        }
        
        .email-container {
            background: white;
            border-radius: 15px;
            overflow: hidden;
            box-shadow: 0 10px 30px rgba(0,0,0,0.1);
        }
        
        .header {
            background: linear-gradient(45deg, #2196F3, #21CBF3);
            color: white;
            padding: 30px 20px;
            text-align: center;
        }
        
        .header h1 {
            margin: 0 0 10px 0;
            font-size: 2.2em;
            font-weight: 700;
        }
        
        .header .hotel-icon {
            font-size: 3em;
            margin-bottom: 10px;
            display: block;
        }
        
        .header .subtitle {
            font-size: 1.1em;
            opacity: 0.9;
            margin: 0;
        }
        
        .content {
            padding: 30px;
        }
        
        .greeting {
            font-size: 16px;
            margin-bottom: 20px;
        }
        
        .greeting strong {
            color: #2196F3;
        }
        
        .info-section {
            background: #f8f9fa;
            padding: 25px;
            border-radius: 12px;
            margin: 25px 0;
            border-left: 5px solid #2196F3;
        }
        
        .info-section h3 {
            color: #333;
            margin: 0 0 20px 0;
            font-size: 1.3em;
            display: flex;
            align-items: center;
        }
        
        .info-section h3 .icon {
            margin-right: 10px;
            font-size: 1.2em;
        }
        
        .info-list {
            list-style: none;
            padding: 0;
            margin: 0;
        }
        
        .info-list li {
            padding: 8px 0;
            border-bottom: 1px solid #e9ecef;
            display: flex;
            justify-content: space-between;
            align-items: center;
        }
        
        .info-list li:last-child {
            border-bottom: none;
        }
        
        .info-list .label {
            font-weight: 600;
            color: #555;
            min-width: 120px;
        }
        
        .info-list .value {
            color: #333;
            font-weight: 500;
        }
        
        .customer-section {
            background: linear-gradient(135deg, #e3f2fd 0%, #bbdefb 100%);
            padding: 25px;
            border-radius: 12px;
            margin: 25px 0;
            border-left: 5px solid #1976d2;
        }
        
        .contact-section {
            background: linear-gradient(135deg, #fff3e0 0%, #ffe0b2 100%);
            padding: 20px;
            border-radius: 12px;
            margin: 25px 0;
            text-align: center;
        }
        
        .contact-section h4 {
            color: #333;
            margin: 0 0 15px 0;
            font-size: 1.2em;
        }
        
        .contact-info {
            display: flex;
            justify-content: space-around;
            flex-wrap: wrap;
            gap: 15px;
        }
        
        .contact-item {
            display: flex;
            align-items: center;
            font-weight: 500;
            color: #333;
        }
        
        .contact-item .icon {
            margin-right: 8px;
            font-size: 1.1em;
        }
        
        .message {
            font-size: 16px;
            margin: 20px 0;
            color: #555;
        }
        
        .footer {
            text-align: center;
            margin-top: 40px;
            padding-top: 20px;
            border-top: 2px solid #e9ecef;
        }
        
        .footer p {
            margin: 0;
            font-size: 16px;
            color: #666;
        }
        
        .footer .hotel-name {
            color: #2196F3;
            font-weight: 700;
            font-size: 1.1em;
        }
        
        .highlight-box {
            background: linear-gradient(135deg, #e8f5e8 0%, #c8e6c9 100%);
            border: 2px solid #4caf50;
            border-radius: 10px;
            padding: 20px;
            margin: 20px 0;
            text-align: center;
        }
        
        .highlight-box .icon {
            font-size: 2em;
            color: #4caf50;
            margin-bottom: 10px;
            display: block;
        }
        
        .highlight-box p {
            margin: 0;
            font-weight: 600;
            color: #2e7d32;
        }
        
        /* Responsive Design */
        @media (max-width: 600px) {
            body {
                padding: 10px;
            }
            
            .header {
                padding: 20px 15px;
            }
            
            .header h1 {
                font-size: 1.8em;
            }
            
            .content {
                padding: 20px 15px;
            }
            
            .contact-info {
                flex-direction: column;
                align-items: center;
            }
            
            .info-list li {
                flex-direction: column;
                align-items: flex-start;
                gap: 5px;
            }
            
            .info-list .label {
                min-width: auto;
            }
        }
    </style>
</head>
<body>
    <div class=""email-container"">
        <!-- Header -->
        <div class=""header"">
            <span class=""hotel-icon"">🏨</span>
            <h1>Khách Sạn Đức Thịnh</h1>
            <p class=""subtitle"">Xác Nhận Đặt Phòng</p>
        </div>
        
        <!-- Content -->
        <div class=""content"">
            <!-- Greeting -->
            <div class=""greeting"">
                <p>Xin chào <strong>{{CustomerName}}</strong>,</p>
                <p>Cảm ơn bạn đã đặt phòng tại khách sạn của chúng tôi. Dưới đây là thông tin đặt phòng của bạn:</p>
            </div>
            
            <!-- Success Notification -->
            <div class=""highlight-box"">
                <span class=""icon"">✅</span>
                <p>Đặt phòng của bạn đã được xác nhận thành công!</p>
            </div>
            
            <!-- Booking Information -->
            <div class=""info-section"">
                <h3>
                    <span class=""icon"">📋</span>
                    Thông Tin Đặt Phòng
                </h3>
                <ul class=""info-list"">
                    <li>
                        <span class=""label"">Mã đặt phòng:</span>
                        <span class=""value"">{{MaDatPhong}}</span>
                    </li>
                    <li>
                        <span class=""label"">Số phòng:</span>
                        <span class=""value"">{{SoPhong}}</span>
                    </li>
                    <li>
                        <span class=""label"">Loại phòng:</span>
                        <span class=""value"">{{LoaiPhong}}</span>
                    </li>
                    <li>
                        <span class=""label"">Ngày nhận phòng:</span>
                        <span class=""value"">{{NgayNhanPhong}}</span>
                    </li>
                    <li>
                        <span class=""label"">Ngày trả phòng:</span>
                        <span class=""value"">{{NgayTraPhong}}</span>
                    </li>
                    <li>
                        <span class=""label"">Loại đặt phòng:</span>
                        <span class=""value"">{{LoaiDatPhong}}</span>
                    </li>
                    <li>
                        <span class=""label"">Ngày đặt:</span>
                        <span class=""value"">{{BookingDate}}</span>
                    </li>
                </ul>
            </div>
            
            <!-- Customer Information -->
            <div class=""customer-section"">
                <h3>
                    <span class=""icon"">👤</span>
                    Thông Tin Khách Hàng
                </h3>
                <ul class=""info-list"">
                    <li>
                        <span class=""label"">Họ và tên:</span>
                        <span class=""value"">{{CustomerName}}</span>
                    </li>
                    <li>
                        <span class=""label"">Số điện thoại:</span>
                        <span class=""value"">{{CustomerPhone}}</span>
                    </li>
                    <li>
                        <span class=""label"">Email:</span>
                        <span class=""value"">{{CustomerEmail}}</span>
                    </li>
                </ul>
            </div>
            
            <!-- Message -->
            <div class=""message"">
                <p>🎉 Chúng tôi rất vui được phục vụ bạn và sẽ liên hệ với bạn sớm nhất có thể để xác nhận thêm chi tiết.</p>
                <p>📞 Nếu bạn có bất kỳ câu hỏi hoặc yêu cầu đặc biệt nào, vui lòng liên hệ với chúng tôi.</p>
            </div>
            
            <!-- Contact Information -->
            <div class=""contact-section"">
                <h4>📞 Thông Tin Liên Hệ</h4>
                <div class=""contact-info"">
                    <div class=""contact-item"">
                        <span class=""icon"">📱</span>
                        <span>0123-456-789</span>
                    </div>
                    <div class=""contact-item"">
                        <span class=""icon"">📧</span>
                        <span>info@ductinh.com</span>
                    </div>
                    <div class=""contact-item"">
                        <span class=""icon"">🏠</span>
                        <span>123 Đường ABC, Q.1, TP.HCM</span>
                    </div>
                </div>
            </div>
            
            <!-- Footer -->
            <div class=""footer"">
                <p>Trân trọng,</p>
                <p class=""hotel-name"">Đội ngũ Khách Sạn Đức Thịnh</p>
                <p style=""margin-top: 15px; font-size: 14px; color: #888;"">
                    🌟 Cảm ơn bạn đã tin tưởng và lựa chọn dịch vụ của chúng tôi!
                </p>
            </div>
        </div>
    </div>
</body>
</html>";

                        // Thay thế các placeholder
                        var emailBody = emailTemplate
                            .Replace("{{CustomerName}}", nguoiDung.HoTen)
                            .Replace("{{MaDatPhong}}", datPhong.MaDatPhong.ToString())
                            .Replace("{{SoPhong}}", phongUpdate.SoPhong)
                            .Replace("{{LoaiPhong}}", phong.LoaiPhong)
                            .Replace("{{NgayNhanPhong}}", model.NgayNhanPhong.Value.ToString("dd/MM/yyyy"))
                            .Replace("{{NgayTraPhong}}", model.NgayTraPhong.Value.ToString("dd/MM/yyyy"))
                            .Replace("{{LoaiDatPhong}}", model.LoaiDatPhong)
                            .Replace("{{BookingDate}}", DateTime.Now.ToString("dd/MM/yyyy"))
                            .Replace("{{CustomerPhone}}", nguoiDung.SoDienThoai ?? "N/A")
                            .Replace("{{CustomerEmail}}", nguoiDung.Email);

                        await _emailService.SendEmailAsync(nguoiDung.Email, emailSubject, emailBody, isHtml: true);
                        _logger.LogInformation($"Đã gửi email xác nhận đến {nguoiDung.Email} cho MaDatPhong = {datPhong.MaDatPhong}");
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogWarning(emailEx, $"Lỗi khi gửi email xác nhận đến {nguoiDung.Email}: {emailEx.Message}");
                    }
                }

                return Ok(new { success = true, maDatPhong = datPhong.MaDatPhong });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi đặt phòng online: {Message}, InnerException: {InnerException}", ex.Message, ex.InnerException?.Message);
                return StatusCode(500, new { success = false, message = $"Lỗi khi đặt phòng: {ex.Message}, Inner: {ex.InnerException?.Message}" });
            }
        }

        // Xử lý thanh toán
        [HttpPost("ProcessPaymentOnline")]
        public async Task<IActionResult> ProcessPayment([FromBody] ProcessPaymentOnlineModel model)
        {
            try
            {
                if (model == null || model.MaDatPhong <= 0)
                    return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ: Mã đặt phòng không hợp lệ" });

                // Lấy maNhanVien từ token
                if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int maNhanVien))
                {
                    return Unauthorized(new { success = false, message = "Không xác định được nhân viên. Vui lòng đăng nhập lại." });
                }

                var nhanVien = await _context.NguoiDung.FindAsync(maNhanVien);
                if (nhanVien == null)
                    return BadRequest(new { success = false, message = "Không tìm thấy thông tin nhân viên!" });

                // Truy vấn DatPhong với kiểm tra NULL an toàn
                var datPhong = await _context.DatPhong
                    .Include(dp => dp.ChiTietDichVu)
                    .Include(dp => dp.Phong)
                    .ThenInclude(p => p.LoaiPhong)
                    .FirstOrDefaultAsync(dp => dp.MaDatPhong == model.MaDatPhong && (dp.TrangThai == "Đã nhận phòng" || dp.TrangThai == null));

                if (datPhong == null)
                    return BadRequest(new { success = false, message = $"Không tìm thấy đặt phòng với MaDatPhong = {model.MaDatPhong} ở trạng thái 'Đã nhận phòng' để thanh toán!" });

                // Kiểm tra các giá trị NULL
                if (datPhong.Phong == null)
                    return BadRequest(new { success = false, message = "Không tìm thấy thông tin phòng!" });

                if (datPhong.Phong.LoaiPhong == null)
                    return BadRequest(new { success = false, message = "Không tìm thấy thông tin loại phòng!" });

                // Đảm bảo LoaiDatPhong không null
                datPhong.LoaiDatPhong = datPhong.LoaiDatPhong ?? "TheoGio";

                var caHienTai = await _context.CaLamViec
                    .FirstOrDefaultAsync(c => c.MaNhanVien == maNhanVien && (c.TrangThai == "Đang làm việc" || c.TrangThai == null));
                if (caHienTai == null)
                    return BadRequest(new { success = false, message = "Không tìm thấy ca làm việc đang hoạt động!" });

                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        var checkinDate = datPhong.NgayNhanPhong;
                        var checkoutDate = DateTime.Now;
                        var thoiGianO = (int)(checkoutDate - checkinDate).TotalHours;
                        if (thoiGianO < 1) thoiGianO = 1;

                        decimal tongTienPhong = 0;
                        if (datPhong.LoaiDatPhong == "TheoNgay")
                        {
                            tongTienPhong = datPhong.Phong.LoaiPhong.GiaTheoNgay;
                        }
                        else
                        {
                            tongTienPhong = thoiGianO * datPhong.Phong.LoaiPhong.GiaTheoGio;
                        }

                        var tongTienDichVu = datPhong.ChiTietDichVu?.Sum(ct => ct.ThanhTien) ?? 0;
                        var tongTien = tongTienPhong + tongTienDichVu;

                        // Cập nhật trạng thái thanh toán
                        datPhong.TongThoiGian = thoiGianO;
                        datPhong.TongTienTheoThoiGian = tongTienPhong;
                        datPhong.TongTienDichVu = tongTienDichVu;
                        datPhong.TrangThaiThanhToan = "Đã thanh toán";
                        datPhong.MaNhomDatPhong = null;
                        // Không cập nhật TrangThai, NgayTraPhong, hoặc DangSuDung
                        // datPhong.TrangThai = "Đã nhận phòng"; // Giữ nguyên
                        // datPhong.NgayTraPhong = null; // Không cập nhật
                        // datPhong.Phong.DangSuDung = true; // Phòng vẫn sử dụng

                        // Tạo hóa đơn
                        var hoaDon = new HoaDon
                        {
                            MaCaLamViec = null,
                            MaDatPhong = datPhong.MaDatPhong,
                            MaNhomDatPhong = null,
                            NgayXuat = DateTime.Now,
                            TongTien = tongTien,
                            PhuongThucThanhToan = "Thẻ tín dụng",
                            TrangThaiThanhToan = "Đã thanh toán",
                            LoaiHoaDon = "Tiền phòng",
                            GhiChu = model.GhiChu ?? "Thanh toán online từ khách hàng"
                        };
                        _context.HoaDon.Add(hoaDon);

                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        _logger.LogInformation($"Đã thanh toán hóa đơn: MaDatPhong = {datPhong.MaDatPhong}");
                        return Ok(new
                        {
                            success = true,
                            message = $"Thanh toán hóa đơn thành công! Tổng tiền: {tongTien:N0} VNĐ",
                            hoaDonTrangThaiThanhToan = hoaDon.TrangThaiThanhToan,
                            datPhongTrangThaiThanhToan = datPhong.TrangThaiThanhToan,
                            tongTienPhong = tongTienPhong,
                            tongTien = tongTien
                        });
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        _logger.LogError(ex, "Lỗi khi thanh toán hóa đơn: {Message}, InnerException: {InnerException}", ex.Message, ex.InnerException?.Message);
                        return StatusCode(500, new { success = false, message = $"Lỗi khi thanh toán hóa đơn: {ex.Message}, Nội dung chi tiết: {ex.InnerException?.Message}" });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xử lý thanh toán: {Message}, InnerException: {InnerException}", ex.Message, ex.InnerException?.Message);
                return StatusCode(500, new { success = false, message = $"Lỗi khi xử lý thanh toán: {ex.Message}, Nội dung chi tiết: {ex.InnerException?.Message}" });
            }
        }
    }

    public class ProcessPaymentOnlineModel
    {
        public int MaDatPhong { get; set; }
        public string SoGiayTo { get; set; }
        public string GhiChu { get; set; }
    }
}