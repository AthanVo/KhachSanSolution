using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using KhachSan.Api.Data;
using KhachSan.Api.Models;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace KhachSan.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Quản trị")]
    public class AdminAPIController : ControllerBase
    {
        private readonly ApplicationDBContext _context;
        private readonly ILogger<AdminAPIController> _logger;
        private readonly IWebHostEnvironment _environment;

        public AdminAPIController(
            ApplicationDBContext context,
            ILogger<AdminAPIController> logger,
            IWebHostEnvironment environment)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        }

        // 1. Quản lý phòng
        [HttpGet("Rooms")]
        public async Task<IActionResult> GetRooms()
        {
            try
            {
                var rooms = await _context.Phong
                    .Include(p => p.LoaiPhong)
                    .Select(p => new
                    {
                        p.MaPhong,
                        p.SoPhong,
                        p.DangSuDung,
                        TrangThai = p.DangSuDung ? "Đang sử dụng" : "Trống",
                        LoaiPhong = p.LoaiPhong.TenLoaiPhong,
                        p.LoaiPhong.GiaTheoGio,
                        p.LoaiPhong.GiaTheoNgay
                    })
                    .ToListAsync();

                return Ok(new { success = true, rooms });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách phòng: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        [HttpPost("Rooms")]
        public async Task<IActionResult> CreateRoom([FromBody] RoomModel model)
        {
            try
            {
                if (model == null || string.IsNullOrEmpty(model.SoPhong) || model.MaLoaiPhong <= 0)
                {
                    return BadRequest(new { success = false, message = "Dữ liệu phòng không hợp lệ." });
                }

                var loaiPhong = await _context.LoaiPhong.FindAsync(model.MaLoaiPhong);
                if (loaiPhong == null)
                {
                    return BadRequest(new { success = false, message = "Loại phòng không tồn tại." });
                }

                var existingRoom = await _context.Phong.AnyAsync(p => p.SoPhong == model.SoPhong);
                if (existingRoom)
                {
                    return BadRequest(new { success = false, message = "Số phòng đã tồn tại." });
                }

                var phong = new Phong
                {
                    SoPhong = model.SoPhong,
                    MoTa = model.MoTa ?? "", // Đảm bảo MoTa không bao giờ là null
                    MaLoaiPhong = model.MaLoaiPhong,
                    DangSuDung = false
                };

                _context.Phong.Add(phong);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Đã thêm phòng mới: SoPhong = {phong.SoPhong}, MaPhong = {phong.MaPhong}");
                return Ok(new { success = true, maPhong = phong.MaPhong });
            }
            catch (DbUpdateException dbEx)
            {
                var errorMessage = dbEx.InnerException != null ? dbEx.InnerException.Message : dbEx.Message;
                _logger.LogError(dbEx, "Lỗi database khi thêm phòng: {Message}, StackTrace: {StackTrace}", errorMessage, dbEx.StackTrace);
                return StatusCode(500, new { success = false, message = $"Lỗi database: {errorMessage}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi thêm phòng: {Message}, StackTrace: {StackTrace}", ex.Message, ex.StackTrace);
                return StatusCode(500, new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        [HttpPut("Rooms/{maPhong}")]
        public async Task<IActionResult> UpdateRoom(int maPhong, [FromBody] RoomModel model)
        {
            try
            {
                if (model == null || string.IsNullOrEmpty(model.SoPhong) || model.MaLoaiPhong <= 0)
                {
                    return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ." });
                }

                var phong = await _context.Phong.FindAsync(maPhong);
                if (phong == null)
                {
                    return NotFound(new { success = false, message = "Phòng không tồn tại." });
                }

                var loaiPhong = await _context.LoaiPhong.FindAsync(model.MaLoaiPhong);
                if (loaiPhong == null)
                {
                    return BadRequest(new { success = false, message = "Loại phòng không tồn tại." });
                }

                var existingRoom = await _context.Phong
                    .AnyAsync(p => p.SoPhong == model.SoPhong && p.MaPhong != maPhong);
                if (existingRoom)
                {
                    return BadRequest(new { success = false, message = "Số phòng đã tồn tại." });
                }

                phong.SoPhong = model.SoPhong;
                phong.MaLoaiPhong = model.MaLoaiPhong;

                _context.Phong.Update(phong);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Đã cập nhật phòng: MaPhong = {maPhong}");
                return Ok(new { success = true });
            }
            catch (DbUpdateException dbEx)
            {
                var errorMessage = dbEx.InnerException != null ? dbEx.InnerException.Message : dbEx.Message;
                _logger.LogError(dbEx, "Lỗi database khi cập nhật phòng: {Message}, StackTrace: {StackTrace}", errorMessage, dbEx.StackTrace);
                return StatusCode(500, new { success = false, message = $"Lỗi database: {errorMessage}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật phòng: {Message}, StackTrace: {StackTrace}", ex.Message, ex.StackTrace);
                return StatusCode(500, new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        [HttpDelete("Rooms/{maPhong}")]
        public async Task<IActionResult> DeleteRoom(int maPhong)
        {
            try
            {
                var phong = await _context.Phong.FindAsync(maPhong);
                if (phong == null)
                {
                    return NotFound(new { success = false, message = "Phòng không tồn tại." });
                }

                var hasBookings = await _context.DatPhong.AnyAsync(dp => dp.MaPhong == maPhong && dp.TrangThai != "Đã hủy");
                if (hasBookings)
                {
                    return BadRequest(new { success = false, message = "Phòng đang có đặt phòng, không thể xóa." });
                }

                _context.Phong.Remove(phong);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Đã xóa phòng: MaPhong = {maPhong}");
                return Ok(new { success = true });
            }
            catch (DbUpdateException dbEx)
            {
                var errorMessage = dbEx.InnerException != null ? dbEx.InnerException.Message : dbEx.Message;
                _logger.LogError(dbEx, "Lỗi database khi xóa phòng: {Message}, StackTrace: {StackTrace}", errorMessage, dbEx.StackTrace);
                return StatusCode(500, new { success = false, message = $"Lỗi database: {errorMessage}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa phòng: {Message}, StackTrace: {StackTrace}", ex.Message, ex.StackTrace);
                return StatusCode(500, new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        // Quản lý loại phòng
        [HttpGet("RoomTypes")]
        public async Task<IActionResult> GetRoomTypes()
        {
            try
            {
                var roomTypes = await _context.LoaiPhong
                    .Select(lp => new
                    {
                        lp.MaLoaiPhong,
                        lp.TenLoaiPhong,
                        lp.GiaTheoGio,
                        lp.GiaTheoNgay
                    })
                    .ToListAsync();

                return Ok(new { success = true, roomTypes });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách loại phòng: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        [HttpPost("RoomTypes")]
        public async Task<IActionResult> CreateRoomType([FromBody] RoomTypeModel model)
        {
            try
            {
                if (model == null || string.IsNullOrEmpty(model.TenLoaiPhong) || model.GiaTheoGio <= 0 || model.GiaTheoNgay <= 0)
                {
                    return BadRequest(new { success = false, message = "Dữ liệu loại phòng không hợp lệ." });
                }

                var loaiPhong = new LoaiPhong
                {
                    TenLoaiPhong = model.TenLoaiPhong,
                    GiaTheoGio = model.GiaTheoGio,
                    GiaTheoNgay = model.GiaTheoNgay
                };

                _context.LoaiPhong.Add(loaiPhong);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Đã thêm loại phòng mới: TenLoaiPhong = {loaiPhong.TenLoaiPhong}");
                return Ok(new { success = true, maLoaiPhong = loaiPhong.MaLoaiPhong });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi thêm loại phòng: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        [HttpPut("RoomTypes/{maLoaiPhong}")]
        public async Task<IActionResult> UpdateRoomType(int maLoaiPhong, [FromBody] RoomTypeModel model)
        {
            try
            {
                var loaiPhong = await _context.LoaiPhong.FindAsync(maLoaiPhong);
                if (loaiPhong == null)
                {
                    return NotFound(new { success = false, message = "Loại phòng không tồn tại." });
                }

                if (!string.IsNullOrEmpty(model.TenLoaiPhong))
                {
                    loaiPhong.TenLoaiPhong = model.TenLoaiPhong;
                }

                if (model.GiaTheoGio > 0)
                {
                    loaiPhong.GiaTheoGio = model.GiaTheoGio;
                }

                if (model.GiaTheoNgay > 0)
                {
                    loaiPhong.GiaTheoNgay = model.GiaTheoNgay;
                }

                _context.LoaiPhong.Update(loaiPhong);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Đã cập nhật loại phòng: MaLoaiPhong = {maLoaiPhong}");
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật loại phòng: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        [HttpDelete("RoomTypes/{maLoaiPhong}")]
        public async Task<IActionResult> DeleteRoomType(int maLoaiPhong)
        {
            try
            {
                var loaiPhong = await _context.LoaiPhong.FindAsync(maLoaiPhong);
                if (loaiPhong == null)
                {
                    return NotFound(new { success = false, message = "Loại phòng không tồn tại." });
                }

                var hasRooms = await _context.Phong.AnyAsync(p => p.MaLoaiPhong == maLoaiPhong);
                if (hasRooms)
                {
                    return BadRequest(new { success = false, message = "Loại phòng đang được sử dụng, không thể xóa." });
                }

                _context.LoaiPhong.Remove(loaiPhong);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Đã xóa loại phòng: MaLoaiPhong = {maLoaiPhong}");
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa loại phòng: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        // 2. Quản lý đặt phòng
        [HttpGet("Bookings")]
        public async Task<IActionResult> GetBookings()
        {
            try
            {
                var bookings = await _context.DatPhong
                    .Include(dp => dp.Phong)
                    .ThenInclude(p => p.LoaiPhong)
                    .Include(dp => dp.KhachHangLuuTru)
                    .Select(dp => new
                    {
                        dp.MaDatPhong,
                        dp.Phong.SoPhong,
                        LoaiPhong = dp.Phong.LoaiPhong.TenLoaiPhong,
                        dp.KhachHangLuuTru.HoTen,
                        dp.NgayNhanPhong,
                        dp.NgayTraPhong,
                        dp.TrangThai,
                        dp.TrangThaiThanhToan,
                        dp.LoaiDatPhong
                    })
                    .ToListAsync();

                return Ok(new { success = true, bookings });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách đặt phòng: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        [HttpPut("Bookings/Cancel/{maDatPhong}")]
        public async Task<IActionResult> CancelBooking(int maDatPhong)
        {
            try
            {
                var datPhong = await _context.DatPhong
                    .Include(dp => dp.Phong)
                    .FirstOrDefaultAsync(dp => dp.MaDatPhong == maDatPhong);

                if (datPhong == null)
                {
                    return NotFound(new { success = false, message = "Đặt phòng không tồn tại." });
                }

                if (datPhong.TrangThai == "Đã hủy")
                {
                    return BadRequest(new { success = false, message = "Đặt phòng đã bị hủy trước đó." });
                }

                datPhong.TrangThai = "Đã hủy";
                if (datPhong.Phong != null)
                {
                    datPhong.Phong.DangSuDung = false;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Đã hủy đặt phòng: MaDatPhong = {maDatPhong}");
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi hủy đặt phòng: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        [HttpPut("Bookings/UpdateStatus/{maDatPhong}")]
        public async Task<IActionResult> UpdateBookingStatus(int maDatPhong, [FromBody] BookingStatusModel model)
        {
            try
            {
                var datPhong = await _context.DatPhong
                    .Include(dp => dp.Phong)
                    .FirstOrDefaultAsync(dp => dp.MaDatPhong == maDatPhong);

                if (datPhong == null)
                {
                    return NotFound(new { success = false, message = "Đặt phòng không tồn tại." });
                }

                if (string.IsNullOrEmpty(model.TrangThai))
                {
                    return BadRequest(new { success = false, message = "Trạng thái không hợp lệ." });
                }

                datPhong.TrangThai = model.TrangThai;
                if (model.TrangThai == "Đã nhận phòng" && datPhong.Phong != null)
                {
                    datPhong.Phong.DangSuDung = true;
                }
                else if (model.TrangThai == "Đã hủy" && datPhong.Phong != null)
                {
                    datPhong.Phong.DangSuDung = false;
                }

                datPhong.NgayCapNhat = DateTime.Now;
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Đã cập nhật trạng thái đặt phòng: MaDatPhong = {maDatPhong}, TrangThai = {model.TrangThai}");
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật trạng thái đặt phòng: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        // 3. Quản lý khách hàng lưu trú
        [HttpGet("Guests")]
        public async Task<IActionResult> GetGuests()
        {
            try
            {
                var guests = await _context.KhachHangLuuTru
                    .Select(kh => new
                    {
                        kh.MaKhachHangLuuTru,
                        kh.HoTen,
                        kh.LoaiGiayTo,
                        kh.SoGiayTo,
                        kh.DiaChi,
                        kh.QuocTich,
                        kh.NgayTao
                    })
                    .ToListAsync();

                return Ok(new { success = true, guests });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách khách hàng lưu trú: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        [HttpPost("Guests")]
        public async Task<IActionResult> CreateGuest([FromBody] GuestModel model)
        {
            try
            {
                if (model == null || string.IsNullOrEmpty(model.HoTen) || string.IsNullOrEmpty(model.SoGiayTo))
                {
                    return BadRequest(new { success = false, message = "Dữ liệu khách hàng không hợp lệ." });
                }

                var existingGuest = await _context.KhachHangLuuTru.AnyAsync(kh => kh.SoGiayTo == model.SoGiayTo);
                if (existingGuest)
                {
                    return BadRequest(new { success = false, message = "Số giấy tờ đã tồn tại." });
                }

                var guest = new KhachHangLuuTru
                {
                    HoTen = model.HoTen,
                    LoaiGiayTo = model.LoaiGiayTo,
                    SoGiayTo = model.SoGiayTo,
                    DiaChi = model.DiaChi,
                    QuocTich = model.QuocTich,
                    NgayTao = DateTime.Now
                };

                _context.KhachHangLuuTru.Add(guest);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Đã thêm khách hàng lưu trú mới: SoGiayTo = {guest.SoGiayTo}");
                return Ok(new { success = true, maKhachHangLuuTru = guest.MaKhachHangLuuTru });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi thêm khách hàng lưu trú: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        [HttpPut("Guests/{maKhachHangLuuTru}")]
        public async Task<IActionResult> UpdateGuest(int maKhachHangLuuTru, [FromBody] GuestModel model)
        {
            try
            {
                var guest = await _context.KhachHangLuuTru.FindAsync(maKhachHangLuuTru);
                if (guest == null)
                {
                    return NotFound(new { success = false, message = "Khách hàng không tồn tại." });
                }

                if (!string.IsNullOrEmpty(model.HoTen))
                {
                    guest.HoTen = model.HoTen;
                }

                if (!string.IsNullOrEmpty(model.LoaiGiayTo))
                {
                    guest.LoaiGiayTo = model.LoaiGiayTo;
                }

                if (!string.IsNullOrEmpty(model.SoGiayTo))
                {
                    var existingGuest = await _context.KhachHangLuuTru.AnyAsync(kh => kh.SoGiayTo == model.SoGiayTo && kh.MaKhachHangLuuTru != maKhachHangLuuTru);
                    if (existingGuest)
                    {
                        return BadRequest(new { success = false, message = "Số giấy tờ đã tồn tại." });
                    }
                    guest.SoGiayTo = model.SoGiayTo;
                }

                guest.DiaChi = model.DiaChi;
                guest.QuocTich = model.QuocTich;

                _context.KhachHangLuuTru.Update(guest);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Đã cập nhật khách hàng lưu trú: MaKhachHangLuuTru = {maKhachHangLuuTru}");
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật khách hàng lưu trú: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        // 4. Quản lý hóa đơn
        [HttpGet("Invoices")]
        public async Task<IActionResult> GetInvoices()
        {
            try
            {
                var invoices = await _context.HoaDon
                    .Include(h => h.DatPhong)
                    .ThenInclude(dp => dp.Phong)
                    .Select(h => new
                    {
                        h.MaHoaDon,
                        h.DatPhong.MaDatPhong,
                        SoPhong = h.DatPhong.Phong.SoPhong,
                        h.NgayXuat,
                        h.TongTien,
                        h.PhuongThucThanhToan,
                        h.TrangThaiThanhToan,
                        h.LoaiHoaDon,
                        h.GhiChu
                    })
                    .ToListAsync();

                return Ok(new { success = true, invoices });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách hóa đơn: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        [HttpPut("Invoices/UpdateStatus/{maHoaDon}")]
        public async Task<IActionResult> UpdateInvoiceStatus(int maHoaDon, [FromBody] InvoiceStatusModel model)
        {
            try
            {
                var hoaDon = await _context.HoaDon
                    .Include(h => h.DatPhong)
                    .FirstOrDefaultAsync(h => h.MaHoaDon == maHoaDon);

                if (hoaDon == null)
                {
                    return NotFound(new { success = false, message = "Hóa đơn không tồn tại." });
                }

                if (string.IsNullOrEmpty(model.TrangThaiThanhToan))
                {
                    return BadRequest(new { success = false, message = "Trạng thái thanh toán không hợp lệ." });
                }

                hoaDon.TrangThaiThanhToan = model.TrangThaiThanhToan;
                if (hoaDon.DatPhong != null)
                {
                    hoaDon.DatPhong.TrangThaiThanhToan = model.TrangThaiThanhToan;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Đã cập nhật trạng thái hóa đơn: MaHoaDon = {maHoaDon}, TrangThaiThanhToan = {model.TrangThaiThanhToan}");
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật trạng thái hóa đơn: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }


        // 5. Quản lý người dùng
        [HttpGet("Users")]
        public async Task<IActionResult> GetUsers()
        {
            try
            {
                var users = await _context.NguoiDung
                    .Select(u => new
                    {
                        u.MaNguoiDung,
                        u.HoTen,
                        u.Email,
                        u.SoDienThoai,
                        u.VaiTro
                    })
                    .ToListAsync();

                return Ok(new { success = true, users });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách người dùng: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        [HttpPost("Users")]
        public async Task<IActionResult> CreateUser([FromBody] UserModel model)
        {
            try
            {
                if (model == null || string.IsNullOrEmpty(model.HoTen) || string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.MatKhau))
                {
                    return BadRequest(new { success = false, message = "Dữ liệu người dùng không hợp lệ." });
                }

                var existingUser = await _context.NguoiDung.AnyAsync(u => u.Email == model.Email);
                if (existingUser)
                {
                    return BadRequest(new { success = false, message = "Email đã tồn tại." });
                }

                var user = new NguoiDung
                {
                    HoTen = model.HoTen,
                    Email = model.Email,
                    MatKhau = BCrypt.Net.BCrypt.HashPassword(model.MatKhau),
                    SoDienThoai = model.SoDienThoai,
                    VaiTro = model.VaiTro ?? "Khách hàng",
                    NgayTao = DateTime.Now
                };

                _context.NguoiDung.Add(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Đã thêm người dùng mới: Email = {user.Email}");
                return Ok(new { success = true, maNguoiDung = user.MaNguoiDung });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi thêm người dùng: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        [HttpPut("Users/{maNguoiDung}")]
        public async Task<IActionResult> UpdateUser(int maNguoiDung, [FromBody] UserModel model)
        {
            try
            {
                var user = await _context.NguoiDung.FindAsync(maNguoiDung);
                if (user == null)
                {
                    return NotFound(new { success = false, message = "Người dùng không tồn tại." });
                }

                if (!string.IsNullOrEmpty(model.HoTen))
                {
                    user.HoTen = model.HoTen;
                }

                if (!string.IsNullOrEmpty(model.Email))
                {
                    var existingUser = await _context.NguoiDung.AnyAsync(u => u.Email == model.Email && u.MaNguoiDung != maNguoiDung);
                    if (existingUser)
                    {
                        return BadRequest(new { success = false, message = "Email đã tồn tại." });
                    }
                    user.Email = model.Email;
                }

                if (!string.IsNullOrEmpty(model.MatKhau))
                {
                    user.MatKhau = BCrypt.Net.BCrypt.HashPassword(model.MatKhau);
                }

                user.SoDienThoai = model.SoDienThoai;
                if (!string.IsNullOrEmpty(model.VaiTro))
                {
                    user.VaiTro = model.VaiTro;
                }

                _context.NguoiDung.Update(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Đã cập nhật người dùng: MaNguoiDung = {maNguoiDung}");
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật người dùng: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        [HttpDelete("Users/{maNguoiDung}")]
        public async Task<IActionResult> DeleteUser(int maNguoiDung)
        {
            try
            {
                var user = await _context.NguoiDung.FindAsync(maNguoiDung);
                if (user == null)
                {
                    return NotFound(new { success = false, message = "Người dùng không tồn tại." });
                }

                var hasBookings = await _context.DatPhong.AnyAsync(dp => dp.MaNguoiDung == maNguoiDung && dp.TrangThai != "Đã hủy");
                if (hasBookings)
                {
                    return BadRequest(new { success = false, message = "Người dùng đang có đặt phòng, không thể xóa." });
                }

                _context.NguoiDung.Remove(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Đã xóa người dùng: MaNguoiDung = {maNguoiDung}");
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa người dùng: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        [HttpGet("Reports/TotalRevenue")]
        public async Task<IActionResult> GetTotalRevenue([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            try
            {
                startDate ??= DateTime.Now.AddDays(-30);
                endDate ??= DateTime.Now;

                var totalRevenue = await _context.HoaDon
                    .Where(h => h.NgayXuat >= startDate && h.NgayXuat <= endDate && h.TrangThaiThanhToan == "Đã thanh toán")
                    .SumAsync(h => h.TongTien);

                return Ok(new { success = true, totalRevenue });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy tổng doanh thu: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        // 6. Báo cáo
        [HttpGet("Reports/Revenue")]
        public async Task<IActionResult> GetRevenueReport([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            try
            {
                startDate ??= DateTime.Now.AddDays(-30);
                endDate ??= DateTime.Now;

                var revenue = await _context.HoaDon
                    .Where(h => h.NgayXuat >= startDate && h.NgayXuat <= endDate && h.TrangThaiThanhToan == "Đã thanh toán")
                    .GroupBy(h => h.NgayXuat.Date)
                    .Select(g => new
                    {
                        Date = g.Key,
                        TotalRevenue = g.Sum(h => h.TongTien)
                    })
                    .ToListAsync();

                return Ok(new { success = true, revenue });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy báo cáo doanh thu: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        [HttpGet("Reports/RoomStatus")]
        public async Task<IActionResult> GetRoomStatusReport()
        {
            try
            {
                var totalRooms = await _context.Phong.CountAsync();
                var occupiedRooms = await _context.Phong.CountAsync(p => p.DangSuDung);
                var availableRooms = totalRooms - occupiedRooms;

                var statusByType = await _context.Phong
                    .Include(p => p.LoaiPhong)
                    .GroupBy(p => p.LoaiPhong.TenLoaiPhong)
                    .Select(g => new
                    {
                        RoomType = g.Key,
                        Total = g.Count(),
                        Occupied = g.Count(p => p.DangSuDung),
                        Available = g.Count(p => !p.DangSuDung)
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    totalRooms,
                    occupiedRooms,
                    availableRooms,
                    statusByType
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy báo cáo tình trạng phòng: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }
    }

    // Models cho các endpoint
    public class RoomModel
    {
        internal string MoTa;

        public string SoPhong { get; set; }
        public int MaLoaiPhong { get; set; }
    }

    public class RoomTypeModel
    {
        public string TenLoaiPhong { get; set; }
        public decimal GiaTheoGio { get; set; }
        public decimal GiaTheoNgay { get; set; }
    }

    public class BookingStatusModel
    {
        public string TrangThai { get; set; }
    }

    public class GuestModel
    {
        public string HoTen { get; set; }
        public string LoaiGiayTo { get; set; }
        public string SoGiayTo { get; set; }
        public string DiaChi { get; set; }
        public string QuocTich { get; set; }
    }

    public class InvoiceStatusModel
    {
        public string TrangThaiThanhToan { get; set; }
    }

    public class UserModel
    {
        public string HoTen { get; set; }
        public string Email { get; set; }
        public string MatKhau { get; set; }
        public string SoDienThoai { get; set; }
        public string VaiTro { get; set; }
    }
}