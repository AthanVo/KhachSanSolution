using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using KhachSan.Api.Data;
using KhachSan.Models;
using KhachSan.Api.Models;

namespace KhachSan.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class KhachSanAPIController : ControllerBase
    {
        private readonly ApplicationDBContext _context;
        private readonly ILogger<KhachSanAPIController> _logger;

        public KhachSanAPIController(ApplicationDBContext context, ILogger<KhachSanAPIController> logger)
        {
            _context = context;
            _logger = logger;
        }


        // Lấy danh sách nhân viên sẵn sàng
        [HttpGet("staff/available")]
        public async Task<IActionResult> GetAvailableStaff()
        {
            try
            {
                // Lấy maNhanVien từ token, nhưng không trả về lỗi nếu không có
                int? maNhanVien = null;
                if (int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int parsedMaNhanVien))
                {
                    maNhanVien = parsedMaNhanVien;
                }

                // Nếu không có maNhanVien, vẫn cho phép lấy danh sách nhân viên
                var staffList = await _context.NguoiDung
                    .Where(n => (n.VaiTro == "Nhân viên" || n.VaiTro == "Quản trị") && n.TrangThai == "Hoạt động")
                    .Select(n => new { MaNguoiDung = n.MaNguoiDung, HoTen = n.HoTen, SoDienThoai = n.SoDienThoai })
                    .ToListAsync();

                // Nếu có maNhanVien, loại bỏ nhân viên hiện tại khỏi danh sách
                if (maNhanVien.HasValue)
                {
                    staffList = staffList.Where(n => n.MaNguoiDung != maNhanVien.Value).ToList();
                }

                return Ok(new { success = true, nhanViens = staffList });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách nhân viên: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = $"Lỗi khi lấy danh sách nhân viên: {ex.Message}" });
            }
        }

        // Lấy thông tin ca làm việc hiện tại
        [HttpGet("current-shift")]
        public async Task<IActionResult> GetCurrentShift()
        {
            try
            {
                // Lấy maNhanVien từ token
                int? maNhanVien = null;
                if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int parsedMaNhanVien))
                {
                    return Unauthorized(new { success = false, message = "Không xác định được nhân viên." });
                }
                maNhanVien = parsedMaNhanVien;

                var caHienTai = await _context.CaLamViec
                    .Where(c => c.MaNhanVien == maNhanVien && c.TrangThai == "Đang làm việc")
                    .OrderByDescending(c => c.ThoiGianBatDau)
                    .Select(c => new
                    {
                        MaCa = c.MaCaLamViec,
                        ThoiGianBatDau = c.ThoiGianBatDau.ToString("dd/MM/yyyy HH:mm:ss"),
                        ThoiGianLamViec = DateTime.Now.Subtract(c.ThoiGianBatDau).TotalHours.ToString("0.00"),
                        TongTienTrongCa = c.TongTienTrongCa ?? 0,
                        TongTienChuyenGiao = c.TongTienChuyenGiao ?? 0
                    })
                    .FirstOrDefaultAsync();

                if (caHienTai == null)
                    return Ok(new { success = false, message = "Bạn chưa bắt đầu ca làm việc!" });

                var nhanViens = await _context.NguoiDung
                    .Where(nv => (nv.VaiTro == "Nhân viên" || nv.VaiTro == "Quản trị")
                              && nv.MaNguoiDung != maNhanVien
                              && nv.TrangThai == "Hoạt động")
                    .Select(nv => new { MaNguoiDung = nv.MaNguoiDung, HoTen = nv.HoTen, SoDienThoai = nv.SoDienThoai })
                    .ToListAsync();

                var tongTienHoaDon = await _context.HoaDon
                    .Where(hd => hd.MaCaLamViec == caHienTai.MaCa && hd.TrangThaiThanhToan == "Đã thanh toán")
                    .SumAsync(hd => hd.TongTien);

                return Ok(new
                {
                    success = true,
                    shift = caHienTai,
                    nhanViens = nhanViens,
                    tongTienHoaDon = tongTienHoaDon,
                    thoiGianHienTai = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy thông tin ca làm việc: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = $"Lỗi khi lấy thông tin ca làm việc: {ex.Message}" });
            }
        }

        // Kết thúc ca làm việc
        [HttpPost("end-shift")]
        public async Task<IActionResult> EndShift([FromBody] EndShiftModel model)
        {
            try
            {
                // Lấy maNhanVien từ token
                if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int maNhanVien))
                    return Unauthorized(new { success = false, message = "Không xác định được nhân viên." });

                if (model == null || model.TongTienTrongCa < 0 || model.TongTienChuyenGiao < 0)
                    return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ." });

                var user = await _context.NguoiDung.FindAsync(maNhanVien);
                if (user == null)
                    return Ok(new { success = false, message = "Không tìm thấy thông tin nhân viên!" });

                CaLamViec caHienTai = null;
                if (user.VaiTro == "Quản trị" && model.MaNhanVien.HasValue)
                {
                    caHienTai = await _context.CaLamViec
                        .Where(c => c.MaNhanVien == model.MaNhanVien.Value && (c.TrangThai == "Đang làm việc" || c.TrangThai == null))
                        .OrderByDescending(c => c.ThoiGianBatDau)
                        .FirstOrDefaultAsync();

                    if (caHienTai == null)
                        return Ok(new { success = false, message = "Không tìm thấy ca làm việc đang hoạt động cho nhân viên được chọn!" });

                    var nhanVien = await _context.NguoiDung.FindAsync(model.MaNhanVien.Value);
                    if (nhanVien == null || (nhanVien.TrangThai != "Hoạt động" && nhanVien.TrangThai != null))
                        return Ok(new { success = false, message = "Nhân viên không ở trạng thái hoạt động!" });
                }
                else
                {
                    caHienTai = await _context.CaLamViec
                        .Where(c => c.MaNhanVien == maNhanVien && (c.TrangThai == "Đang làm việc" || c.TrangThai == null))
                        .OrderByDescending(c => c.ThoiGianBatDau)
                        .FirstOrDefaultAsync();

                    if (caHienTai == null)
                        return Ok(new { success = false, message = "Không tìm thấy ca làm việc đang hoạt động!" });

                    if (user.TrangThai != "Hoạt động" && user.TrangThai != null)
                        return Ok(new { success = false, message = "Nhân viên không ở trạng thái hoạt động!" });
                }

                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        caHienTai.ThoiGianKetThuc = DateTime.Now;
                        caHienTai.TongTienTrongCa = model.TongTienTrongCa;
                        caHienTai.GhiChu = model.GhiChu ?? string.Empty;
                        caHienTai.TrangThai = "Đã kết thúc";

                        if (model.MaNhanVienCaTiepTheo.HasValue)
                        {
                            var nhanVienTiepTheo = await _context.NguoiDung
                                .FirstOrDefaultAsync(nv => nv.MaNguoiDung == model.MaNhanVienCaTiepTheo.Value
                                                        && (nv.VaiTro == "Nhân viên" || nv.VaiTro == "Quản trị")
                                                        && (nv.TrangThai == "Hoạt động" || nv.TrangThai == null));
                            if (nhanVienTiepTheo == null)
                                return Ok(new { success = false, message = "Nhân viên ca tiếp theo không hợp lệ!" });

                            caHienTai.MaNhanVienCaTiepTheo = model.MaNhanVienCaTiepTheo;
                            caHienTai.TongTienChuyenGiao = model.TongTienChuyenGiao;

                            var caMoi = new CaLamViec
                            {
                                MaNhanVien = model.MaNhanVienCaTiepTheo.Value,
                                ThoiGianBatDau = DateTime.Now,
                                TrangThai = "Đang làm việc",
                                TongTienChuyenGiao = model.TongTienChuyenGiao
                            };
                            await _context.CaLamViec.AddAsync(caMoi);

                            var nhanVienHienTai = await _context.NguoiDung.FindAsync(maNhanVien);
                            var thongBao = new ThongBao
                            {
                                MaNguoiGui = maNhanVien,
                                MaNguoiNhan = model.MaNhanVienCaTiepTheo.Value,
                                TieuDe = "Giao ca làm việc",
                                NoiDung = $"Nhân viên {nhanVienHienTai?.HoTen ?? "Không xác định"} đã giao ca cho bạn. Tổng tiền trong ca: {model.TongTienTrongCa:#,##0} VNĐ, số tiền chuyển giao: {model.TongTienChuyenGiao:#,##0} VNĐ",
                                LoaiThongBao = "Giao ca",
                                ThoiGianGui = DateTime.Now,
                                TrangThai = "Chưa đọc"
                            };
                            await _context.ThongBao.AddAsync(thongBao);
                        }

                        var lichSuThaoTac = new LichSuThaoTac
                        {
                            MaCaLamViec = caHienTai.MaCaLamViec,
                            MaNhanVien = maNhanVien,
                            LoaiThaoTac = model.MaNhanVienCaTiepTheo.HasValue ? "Giao ca" : "Kết ca",
                            ChiTiet = model.GhiChu ?? string.Empty,
                            ThoiGian = DateTime.Now
                        };
                        await _context.LichSuThaoTac.AddAsync(lichSuThaoTac);

                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        return Ok(new
                        {
                            success = true,
                            message = model.MaNhanVienCaTiepTheo.HasValue ? "Giao ca thành công!" : "Kết thúc ca làm việc thành công!"
                        });
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        _logger.LogError(ex, "Lỗi khi kết thúc ca: {Message}", ex.Message);
                        return StatusCode(500, new { success = false, message = $"Lỗi khi kết thúc ca: {ex.Message}" });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi kết thúc ca: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = $"Lỗi khi kết thúc ca: {ex.Message}" });
            }
        }

        // Lấy thống kê
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            try
            {
                var totalRevenue = await _context.HoaDon.Where(hd => hd.TrangThaiThanhToan == "Đã thanh toán").SumAsync(hd => hd.TongTien);
                var totalCustomers = await _context.KhachHangLuuTru.CountAsync();
                var totalRooms = await _context.Phong.CountAsync();
                var occupiedRooms = await _context.Phong.CountAsync(p => p.DangSuDung == true);
                var availableRooms = totalRooms - occupiedRooms;
                var pendingPayment = await _context.DatPhong.CountAsync(dp => dp.TrangThaiThanhToan == "Chưa thanh toán");

                var stats = new
                {
                    TotalRevenue = totalRevenue,
                    TotalCustomers = totalCustomers,
                    TotalRooms = totalRooms,
                    OccupiedRooms = occupiedRooms,
                    AvailableRooms = availableRooms,
                    PendingPayment = pendingPayment
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy thống kê: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = $"Lỗi khi lấy thống kê: {ex.Message}" });
            }
        }

        // Lấy thông tin người dùng hiện tại
        [HttpGet("current-user")]
        public async Task<IActionResult> GetCurrentUser()
        {
            try
            {
                // Lấy maNhanVien từ token
                var nameIdentifierClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (nameIdentifierClaim == null)
                {
                    _logger.LogWarning("Không tìm thấy claim 'NameIdentifier' trong token.");
                    return Unauthorized(new { success = false, message = "Không xác định được người dùng: Thiếu claim 'NameIdentifier'" });
                }

                if (!int.TryParse(nameIdentifierClaim.Value, out int maNhanVien))
                {
                    _logger.LogWarning("Claim 'NameIdentifier' không phải là số nguyên hợp lệ: {ClaimValue}", nameIdentifierClaim.Value);
                    return Unauthorized(new { success = false, message = "Không xác định được người dùng: Claim 'NameIdentifier' không hợp lệ" });
                }

                var user = await _context.NguoiDung
                    .Where(n => n.MaNguoiDung == maNhanVien)
                    .Select(n => new { MaNguoiDung = n.MaNguoiDung, HoTen = n.HoTen, VaiTro = n.VaiTro })
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    _logger.LogWarning("Không tìm thấy người dùng với MaNguoiDung: {MaNguoiDung}", maNhanVien);
                    return Ok(new { success = false, message = "Không tìm thấy thông tin người dùng!" });
                }

                _logger.LogInformation("Lấy thông tin người dùng thành công: MaNguoiDung = {MaNguoiDung}, HoTen = {HoTen}", user.MaNguoiDung, user.HoTen);
                return Ok(new { success = true, user });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy thông tin người dùng: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = $"Lỗi khi lấy thông tin người dùng: {ex.Message}" });
            }
        }

        // Kiểm tra các ca làm việc bị kẹt
        [HttpGet("check-stuck-shifts")]
        public async Task<IActionResult> CheckStuckShifts()
        {
            try
            {
                var stuckShifts = await _context.CaLamViec
                    .Where(c => c.TrangThai == "Đang làm việc" && c.ThoiGianBatDau < DateTime.Now.AddHours(-24))
                    .ToListAsync();

                foreach (var shift in stuckShifts)
                {
                    var nhanVien = await _context.NguoiDung.FindAsync(shift.MaNhanVien);
                    var quanTris = await _context.NguoiDung.Where(nv => nv.VaiTro == "Quản trị").ToListAsync();

                    foreach (var quanTri in quanTris)
                    {
                        var thongBaoQuanTri = new ThongBao
                        {
                            MaNguoiGui = shift.MaNhanVien,
                            MaNguoiNhan = quanTri.MaNguoiDung,
                            TieuDe = "Ca làm việc bị kẹt",
                            NoiDung = $"Ca làm việc của nhân viên {nhanVien?.HoTen ?? "Không xác định"} (Mã ca: {shift.MaCaLamViec}) đã ở trạng thái 'Đang làm việc' quá 24 giờ. Vui lòng kiểm tra!",
                            LoaiThongBao = "Cảnh báo",
                            ThoiGianGui = DateTime.Now,
                            TrangThai = "Chưa đọc"
                        };
                        await _context.ThongBao.AddAsync(thongBaoQuanTri);
                    }

                    var thongBaoNhanVien = new ThongBao
                    {
                        MaNguoiGui = null,
                        MaNguoiNhan = shift.MaNhanVien,
                        TieuDe = "Ca làm việc của bạn bị kẹt",
                        NoiDung = $"Ca làm việc của bạn (Mã ca: {shift.MaCaLamViec}) đã ở trạng thái 'Đang làm việc' quá 24 giờ. Vui lòng kết thúc ca!",
                        LoaiThongBao = "Cảnh báo",
                        ThoiGianGui = DateTime.Now,
                        TrangThai = "Chưa đọc"
                    };
                    await _context.ThongBao.AddAsync(thongBaoNhanVien);
                }

                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "Đã kiểm tra các ca bị kẹt!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi kiểm tra ca bị kẹt: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = $"Lỗi khi kiểm tra ca bị kẹt: {ex.Message}" });
            }
        }

        // Lấy danh sách thông báo
        [HttpGet("Notifications")]
        public async Task<IActionResult> GetNotifications()
        {
            try
            {
                // Lấy maNhanVien từ token
                if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int maNhanVien))
                {
                    return Unauthorized(new { success = false, message = "Không xác định được người dùng" });
                }

                var notifications = await _context.ThongBao
                    .Where(tb => tb.MaNguoiNhan == maNhanVien)
                    .OrderByDescending(tb => tb.ThoiGianGui)
                    .Select(tb => new
                    {
                        Id = tb.MaThongBao,
                        Sender = tb.NguoiGui != null ? tb.NguoiGui.HoTen : "Hệ thống",
                        Title = tb.TieuDe,
                        Content = tb.NoiDung,
                        Type = tb.LoaiThongBao,
                        Time = tb.ThoiGianGui.ToString("dd/MM/yyyy HH:mm:ss"),
                        Status = tb.TrangThai
                    })
                    .ToListAsync();

                var unreadCount = notifications.Count(n => n.Status == "Chưa đọc");
                return Ok(new { success = true, notifications, unreadCount });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách thông báo: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = $"Lỗi khi lấy danh sách thông báo: {ex.Message}" });
            }
        }

        // Lấy danh sách thông báo chưa đọc
        [HttpGet("notifications/unread")]
        public async Task<IActionResult> GetUnreadNotifications()
        {
            try
            {
                // Lấy maNhanVien từ token
                if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int maNhanVien))
                {
                    return Unauthorized(new { success = false, message = "Không xác định được người dùng" });
                }

                var notifications = await _context.ThongBao
                    .Where(t => t.MaNguoiNhan == maNhanVien && t.TrangThai == "Chưa đọc")
                    .OrderByDescending(t => t.ThoiGianGui)
                    .Select(t => new
                    {
                        MaThongBao = t.MaThongBao,
                        TieuDe = t.TieuDe,
                        NoiDung = t.NoiDung,
                        LoaiThongBao = t.LoaiThongBao,
                        ThoiGianGui = t.ThoiGianGui.ToString("dd/MM/yyyy HH:mm:ss"),
                        TrangThai = t.TrangThai
                    })
                    .ToListAsync();

                var unreadCount = notifications.Count;
                return Ok(new { success = true, notifications, unreadCount });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy thông báo chưa đọc: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = $"Lỗi khi lấy thông báo chưa đọc: {ex.Message}" });
            }
        }

        // Đánh dấu thông báo là đã đọc
        [HttpPost("notifications/mark-read/{maThongBao}")]
        public async Task<IActionResult> MarkNotificationAsRead(int maThongBao)
        {
            try
            {
                // Lấy maNhanVien từ token
                if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int maNhanVien))
                {
                    return Unauthorized(new { success = false, message = "Không xác định được người dùng" });
                }

                _logger.LogInformation("Đánh dấu đã đọc: maThongBao={MaThongBao}, maNhanVien={MaNhanVien}", maThongBao, maNhanVien);

                var thongBao = await _context.ThongBao
                    .FirstOrDefaultAsync(t => t.MaThongBao == maThongBao && t.MaNguoiNhan == maNhanVien);

                if (thongBao == null)
                    return Ok(new { success = false, message = "Không tìm thấy thông báo!" });

                if (thongBao.TrangThai == "Chưa đọc")
                {
                    thongBao.TrangThai = "Đã đọc";
                    _context.Entry(thongBao).State = EntityState.Modified;
                    await _context.SaveChangesAsync();

                    var unreadCount = await _context.ThongBao
                        .CountAsync(t => t.MaNguoiNhan == maNhanVien && t.TrangThai == "Chưa đọc");

                    return Ok(new
                    {
                        success = true,
                        message = "Đã đánh dấu thông báo là đã đọc!",
                        unreadCount,
                        status = thongBao.TrangThai
                    });
                }

                var unreadCountExisting = await _context.ThongBao
                    .CountAsync(t => t.MaNguoiNhan == maNhanVien && t.TrangThai == "Chưa đọc");

                return Ok(new
                {
                    success = true,
                    message = "Thông báo đã được đánh dấu đọc từ trước!",
                    unreadCount = unreadCountExisting,
                    status = thongBao.TrangThai
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi đánh dấu thông báo: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        // Đặt phòng
        [HttpPost("BookRoom")]
        public async Task<IActionResult> BookRoom([FromBody] BookRoomModel model)
        {
            try
            {
                if (model == null || model.MaPhong <= 0 || string.IsNullOrEmpty(model.LoaiDatPhong))
                    return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ" });

                _logger.LogInformation($"Đang kiểm tra phòng với MaPhong: {model.MaPhong}");

                var phong = await _context.Phong
                    .Where(p => p.MaPhong == model.MaPhong && !p.DangSuDung)
                    .Select(p => new { p.MaPhong, p.DangSuDung })
                    .FirstOrDefaultAsync();
                if (phong == null)
                {
                    _logger.LogWarning($"Phòng không tồn tại hoặc đang sử dụng: MaPhong = {model.MaPhong}");
                    return NotFound(new { success = false, message = "Phòng không tồn tại hoặc đang sử dụng" });
                }

                var khachHang = await _context.KhachHangLuuTru
                    .Where(kh => kh.SoGiayTo == model.SoGiayTo)
                    .FirstOrDefaultAsync();
                if (khachHang == null)
                {
                    khachHang = new KhachHangLuuTru
                    {
                        LoaiGiayTo = model.LoaiGiayTo,
                        SoGiayTo = model.SoGiayTo,
                        HoTen = model.HoTen,
                        DiaChi = model.DiaChi,
                        QuocTich = model.QuocTich,
                        NgayTao = DateTime.Now
                    };
                    _context.KhachHangLuuTru.Add(khachHang);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    khachHang.HoTen = model.HoTen;
                    khachHang.DiaChi = model.DiaChi;
                    khachHang.QuocTich = model.QuocTich;
                    _context.KhachHangLuuTru.Update(khachHang);
                }

                int? maNhanVien = null;
                if (int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int parsedMaNhanVien))
                {
                    maNhanVien = parsedMaNhanVien;
                }

                var datPhong = new DatPhong
                {
                    MaPhong = phong.MaPhong,
                    MaKhachHangLuuTru = khachHang.MaKhachHangLuuTru,
                    MaNhanVien = (int)maNhanVien,
                    NgayNhanPhong = DateTime.Now,
                    NgayNhanPhongDuKien = model.NgayNhanPhongDuKien,
                    TrangThai = "Đã nhận phòng",
                    LoaiDatPhong = model.LoaiDatPhong
                };
                _context.DatPhong.Add(datPhong);

                var phongUpdate = await _context.Phong.FindAsync(model.MaPhong);
                if (phongUpdate == null)
                {
                    _logger.LogError($"Không tìm thấy phòng để cập nhật: MaPhong = {model.MaPhong}");
                    return NotFound(new { success = false, message = "Phòng không tồn tại" });
                }

                if (model.NgayNhanPhongDuKien == null || model.NgayNhanPhongDuKien <= DateTime.Now.Date)
                {
                    phongUpdate.DangSuDung = true;
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation($"Đã thêm đặt phòng: MaDatPhong = {datPhong.MaDatPhong}");

                return Ok(new { success = true, maDatPhong = datPhong.MaDatPhong });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi đặt phòng: {Message}, InnerException: {InnerException}", ex.Message, ex.InnerException?.Message);
                return StatusCode(500, new { success = false, message = $"Lỗi khi đặt phòng: {ex.Message}, Inner: {ex.InnerException?.Message}" });
            }
        }


        // Lấy chi tiết đặt phòng
        [HttpGet("GetBookingDetails/{maDatPhong}")]
        public async Task<IActionResult> GetBookingDetails(int maDatPhong)
        {
            try
            {
                var datPhong = await _context.DatPhong
                    .FirstOrDefaultAsync(dp => dp.MaDatPhong == maDatPhong);
                if (datPhong == null)
                    return NotFound(new { success = false, message = "Không tìm thấy đặt phòng" });

                return Ok(new
                {
                    success = true,
                    maDatPhong = datPhong.MaDatPhong,
                    loaiDatPhong = datPhong.LoaiDatPhong ?? "Theo giờ"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy chi tiết đặt phòng: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = $"Lỗi khi lấy chi tiết đặt phòng: {ex.Message}" });
            }
        }

        // Lấy danh sách dịch vụ
        [HttpGet("GetServices")]
        public async Task<IActionResult> GetServices()
        {
            try
            {
                var services = await _context.DichVu
                    .Where(d => d.TrangThai == "Hoạt động")
                    .Select(d => new { maDichVu = d.MaDichVu, tenDichVu = d.TenDichVu, gia = d.Gia })
                    .ToListAsync();

                return Ok(new { success = true, services });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tải danh sách dịch vụ: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = $"Lỗi khi tải danh sách dịch vụ: {ex.Message}" });
            }
        }

        // Thêm dịch vụ
        [HttpPost("AddService")]
        public async Task<IActionResult> AddService([FromBody] AddServiceModel model)
        {
            try
            {
                if (model == null || model.MaDatPhong <= 0 || model.MaDichVu <= 0 || model.SoLuong <= 0)
                    return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ" });

                var datPhong = await _context.DatPhong
                    .FirstOrDefaultAsync(dp => dp.MaDatPhong == model.MaDatPhong && dp.TrangThai == "Đã nhận phòng");
                if (datPhong == null)
                    return BadRequest(new { success = false, message = $"Không tìm thấy đặt phòng với MaDatPhong = {model.MaDatPhong}" });

                var dichVu = await _context.DichVu
                    .FirstOrDefaultAsync(d => d.MaDichVu == model.MaDichVu && d.TrangThai == "Hoạt động");
                if (dichVu == null)
                    return BadRequest(new { success = false, message = $"Dịch vụ với MaDichVu = {model.MaDichVu} không tồn tại" });

                var chiTietDichVu = new ChiTietDichVu
                {
                    MaDatPhong = model.MaDatPhong,
                    MaDichVu = model.MaDichVu,
                    SoLuong = model.SoLuong,
                    DonGia = dichVu.Gia,
                    ThanhTien = model.SoLuong * dichVu.Gia,
                    NgayTao = DateTime.Now
                };

                _context.ChiTietDichVu.Add(chiTietDichVu);
                await _context.SaveChangesAsync();

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi thêm dịch vụ: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = $"Lỗi khi thêm dịch vụ: {ex.Message}" });
            }
        }

        // Lấy thông tin phòng
        [HttpGet("GetRoom/{maPhong}")]
        public async Task<IActionResult> GetRoom(int maPhong)
        {
            try
            {
                var phong = await _context.Phong
                    .Include(p => p.LoaiPhong)
                    .FirstOrDefaultAsync(p => p.MaPhong == maPhong);
                if (phong == null)
                    return NotFound(new { success = false, message = "Phòng không tồn tại" });

                return Ok(new
                {
                    success = true,
                    maPhong = phong.MaPhong,
                    soPhong = phong.SoPhong,
                    dangSuDung = phong.DangSuDung,
                    giaTheoGio = phong.LoaiPhong.GiaTheoGio,
                    giaTheoNgay = phong.LoaiPhong.GiaTheoNgay
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy thông tin phòng: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = $"Lỗi khi lấy thông tin phòng: {ex.Message}" });
            }
        }

        // Lấy danh sách dịch vụ của phòng
        [HttpGet("GetRoomServices")]
        public async Task<IActionResult> GetRoomServices([FromQuery] int[] maDatPhong)
        {
            try
            {
                if (maDatPhong == null || !maDatPhong.Any())
                    return BadRequest(new { success = false, message = "Danh sách mã đặt phòng không hợp lệ" });

                var services = await _context.ChiTietDichVu
                    .Include(ct => ct.DichVu)
                    .Where(ct => maDatPhong.Contains(ct.MaDatPhong))
                    .Select(ct => new
                    {
                        maDatPhong = ct.MaDatPhong,
                        maDichVu = ct.MaDichVu,
                        tenDichVu = ct.DichVu.TenDichVu,
                        soLuong = ct.SoLuong,
                        donGia = ct.DonGia,
                        thanhTien = ct.ThanhTien
                    })
                    .ToListAsync();

                return Ok(new { success = true, services });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tải danh sách dịch vụ phòng: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = $"Lỗi khi tải danh sách dịch vụ phòng: {ex.Message}" });
            }
        }

        // Lấy giá phòng
        [HttpGet("GetRoomPrice/{maPhong}")]
        public async Task<IActionResult> GetRoomPrice(int maPhong)
        {
            try
            {
                var phong = await _context.Phong
                    .Include(p => p.LoaiPhong)
                    .FirstOrDefaultAsync(p => p.MaPhong == maPhong);

                if (phong == null)
                    return NotFound(new { success = false, message = "Phòng không tồn tại" });

                return Ok(new
                {
                    success = true,
                    maPhong = phong.MaPhong,
                    soPhong = phong.SoPhong,
                    loaiPhong = phong.LoaiPhong.TenLoaiPhong,
                    giaTheoGio = phong.LoaiPhong.GiaTheoGio,
                    giaTheoNgay = phong.LoaiPhong.GiaTheoNgay
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy giá phòng: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = $"Lỗi khi lấy giá phòng: {ex.Message}" });
            }
        }

        // Xử lý thanh toán
        [HttpPost("ProcessPayment")]
        public async Task<IActionResult> ProcessPayment([FromBody] ProcessPaymentModel model)
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

                        datPhong.NgayTraPhong = checkoutDate;
                        datPhong.TongThoiGian = thoiGianO;
                        datPhong.TongTienTheoThoiGian = tongTienPhong;
                        datPhong.TongTienDichVu = tongTienDichVu;
                        datPhong.TrangThai = "Đã trả phòng";
                        datPhong.TrangThaiThanhToan = "Đã thanh toán";
                        datPhong.MaNhomDatPhong = null;
                        datPhong.Phong.DangSuDung = false;

                        var hoaDon = new HoaDon
                        {
                            MaCaLamViec = caHienTai.MaCaLamViec,
                            MaDatPhong = datPhong.MaDatPhong,
                            MaNhomDatPhong = null,
                            NgayXuat = DateTime.Now,
                            TongTien = tongTien,
                            PhuongThucThanhToan = "Tiền mặt",
                            TrangThaiThanhToan = "Đã thanh toán",
                            LoaiHoaDon = "Tiền phòng",
                            GhiChu = model.GhiChu ?? string.Empty
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

        // Thêm nhóm đặt phòng
        [HttpPost("add-group")]
        public async Task<IActionResult> AddGroup([FromBody] NhomDatPhongRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.TenNhom) || string.IsNullOrEmpty(request.HoTenNguoiDaiDien) || string.IsNullOrEmpty(request.SoDienThoaiNguoiDaiDien))
                {
                    return BadRequest(new { success = false, message = "Tên nhóm, người đại diện, và số điện thoại không được để trống." });
                }

                var existingGroup = await _context.NhomDatPhong
                    .FirstOrDefaultAsync(g => g.TenNhom == request.TenNhom && g.TrangThai != "Đã hủy");

                if (existingGroup != null)
                {
                    existingGroup.HoTenNguoiDaiDien = request.HoTenNguoiDaiDien;
                    existingGroup.SoDienThoaiNguoiDaiDien = request.SoDienThoaiNguoiDaiDien;
                    existingGroup.NgayNhanPhong = request.NgayNhanPhong; // Lưu ngày nhận phòng
                    existingGroup.NgayTraPhong = request.NgayTraPhong;   // Lưu ngày trả phòng

                    var existingRooms = _context.NhomPhong.Where(np => np.MaNhomDatPhong == existingGroup.MaNhomDatPhong);
                    _context.NhomPhong.RemoveRange(existingRooms);

                    if (request.MaPhong != null && request.MaPhong.Any())
                    {
                        foreach (var maPhong in request.MaPhong)
                        {
                            _context.NhomPhong.Add(new NhomPhong
                            {
                                MaNhomDatPhong = (int)existingGroup.MaNhomDatPhong,
                                MaPhong = maPhong
                            });
                        }
                    }

                    await _context.SaveChangesAsync();
                    return Ok(new { success = true, maNhomDatPhong = existingGroup.MaNhomDatPhong });
                }

                var entity = new NhomDatPhong
                {
                    TenNhom = request.TenNhom,
                    HoTenNguoiDaiDien = request.HoTenNguoiDaiDien,
                    SoDienThoaiNguoiDaiDien = request.SoDienThoaiNguoiDaiDien,
                    MaNhanVien = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "1"),
                    NgayTao = DateTime.Now,
                    NgayNhanPhong = request.NgayNhanPhong, // Lưu ngày nhận phòng
                    NgayTraPhong = request.NgayTraPhong,   // Lưu ngày trả phòng
                    TrangThai = "Đang xử lý"
                };

                _context.NhomDatPhong.Add(entity);
                await _context.SaveChangesAsync();

                if (request.MaPhong != null && request.MaPhong.Any())
                {
                    foreach (var maPhong in request.MaPhong)
                    {
                        _context.NhomPhong.Add(new NhomPhong
                        {
                            MaNhomDatPhong = (int)entity.MaNhomDatPhong,
                            MaPhong = maPhong
                        });
                    }
                    await _context.SaveChangesAsync();
                }

                return Ok(new { success = true, maNhomDatPhong = entity.MaNhomDatPhong });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi thêm nhóm: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // Lấy danh sách nhóm đặt phòng
        [HttpGet("groups")]
        public async Task<IActionResult> GetGroups()
        {
            try
            {
                var groups = await _context.NhomDatPhong
                    .Select(g => new
                    {
                        id = g.MaNhomDatPhong,
                        name = g.TenNhom,
                        representative = g.HoTenNguoiDaiDien,
                        phone = g.SoDienThoaiNguoiDaiDien,
                        ngayNhanPhong = g.NgayNhanPhong,
                        ngayTraPhong = g.NgayTraPhong,
                        rooms = _context.NhomPhong
                            .Where(np => np.MaNhomDatPhong == g.MaNhomDatPhong)
                            .Select(np => np.MaPhong.ToString())
                            .ToList(),
                        datPhongs = _context.DatPhong
                            .Where(dp => dp.MaNhomDatPhong == g.MaNhomDatPhong)
                            .Select(dp => dp.MaDatPhong)
                            .ToList()
                    })
                    .ToListAsync();
                return Ok(new { success = true, groups });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tải danh sách nhóm: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = $"Lỗi khi tải danh sách nhóm: {ex.Message}" });
            }
        }

        // Gộp hóa đơn cho nhóm
        [HttpPost("merge-bill")]
        public async Task<IActionResult> MergeBill([FromBody] MergeBillModel model)
        {
            try
            {
                if (model == null || !model.MaNhomDatPhong.HasValue)
                    return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ: Mã nhóm đặt phòng bị thiếu hoặc không hợp lệ" });

                // Lấy maNhanVien từ token
                if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int maNhanVien))
                {
                    return Unauthorized(new { success = false, message = "Không xác định được nhân viên. Vui lòng đăng nhập lại." });
                }

                var nhanVien = await _context.NguoiDung.FindAsync(maNhanVien);
                if (nhanVien == null)
                    return BadRequest(new { success = false, message = "Không tìm thấy thông tin nhân viên!" });

                var nhomDatPhong = await _context.NhomDatPhong
                    .AsSplitQuery()
                    .Include(n => n.DatPhong)
                    .ThenInclude(dp => dp.ChiTietDichVu)
                    .Include(n => n.DatPhong)
                    .ThenInclude(dp => dp.Phong)
                    .ThenInclude(p => p.LoaiPhong)
                    .FirstOrDefaultAsync(n => n.MaNhomDatPhong == model.MaNhomDatPhong.Value);
                if (nhomDatPhong == null)
                    return BadRequest(new { success = false, message = $"Không tìm thấy nhóm đặt phòng với MaNhomDatPhong = {model.MaNhomDatPhong.Value}" });

                var datPhong = nhomDatPhong.DatPhong.Where(dp => dp.TrangThai == "Đã nhận phòng").ToList();
                if (datPhong.Count == 0)
                    return BadRequest(new { success = false, message = $"Không có phòng nào trong nhóm {nhomDatPhong.TenNhom} ở trạng thái 'Đã nhận phòng' để thanh toán!" });

                if (!nhomDatPhong.NgayNhanPhong.HasValue || !nhomDatPhong.NgayTraPhong.HasValue)
                    return BadRequest(new { success = false, message = "Ngày nhận phòng hoặc ngày trả phòng của nhóm không được để trống!" });

                decimal totalTongTien = 0;
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        var caHienTai = await _context.CaLamViec
                            .FirstOrDefaultAsync(c => c.MaNhanVien == maNhanVien && c.TrangThai == "Đang làm việc");
                        if (caHienTai == null)
                            return BadRequest(new { success = false, message = "Không tìm thấy ca làm việc đang hoạt động!" });

                        var soNgay = (nhomDatPhong.NgayTraPhong.Value.Date - nhomDatPhong.NgayNhanPhong.Value.Date).Days;
                        if (soNgay < 1) soNgay = 1;

                        foreach (var dp in datPhong)
                        {
                            if (dp.Phong == null || dp.Phong.LoaiPhong == null)
                                return BadRequest(new { success = false, message = $"Phòng hoặc loại phòng không tồn tại cho đặt phòng MaDatPhong = {dp.MaDatPhong}" });

                            var loaiPhong = dp.Phong.LoaiPhong;
                            decimal tongTienPhong = soNgay * loaiPhong.GiaTheoNgay;

                            var tongTienDichVu = dp.ChiTietDichVu?.Sum(ct => ct.ThanhTien) ?? 0;
                            var tongTien = tongTienPhong + tongTienDichVu;
                            totalTongTien += tongTien;

                            dp.NgayTraPhong = nhomDatPhong.NgayTraPhong.Value;
                            dp.TongThoiGian = soNgay;
                            dp.TongTienTheoThoiGian = tongTienPhong;
                            dp.TongTienDichVu = tongTienDichVu;
                            dp.TrangThai = "Đã trả phòng";
                            dp.TrangThaiThanhToan = "Đã thanh toán";
                            dp.MaNhomDatPhong = null;
                            dp.Phong.DangSuDung = false;

                            var hoaDon = new HoaDon
                            {
                                MaCaLamViec = caHienTai.MaCaLamViec,
                                MaDatPhong = dp.MaDatPhong,
                                MaNhomDatPhong = nhomDatPhong.MaNhomDatPhong,
                                NgayXuat = DateTime.Now,
                                TongTien = tongTien,
                                PhuongThucThanhToan = "Tiền mặt",
                                TrangThaiThanhToan = "Đã thanh toán",
                                LoaiHoaDon = "Tiền phòng",
                                GhiChu = model.GhiChu
                            };
                            _context.HoaDon.Add(hoaDon);
                        }

                        var nhomPhongRecords = await _context.NhomPhong
                            .Where(np => np.MaNhomDatPhong == nhomDatPhong.MaNhomDatPhong)
                            .ToListAsync();
                        _context.NhomPhong.RemoveRange(nhomPhongRecords);

                        nhomDatPhong.TrangThai = "Đã thanh toán";
                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        _logger.LogInformation($"Đã gộp hóa đơn cho nhóm: MaNhomDatPhong = {nhomDatPhong.MaNhomDatPhong}");
                        return Ok(new { success = true, message = $"Thanh toán hóa đơn gộp cho nhóm {nhomDatPhong.TenNhom} thành công! Tổng tiền: {totalTongTien:N0} VNĐ" });
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        _logger.LogError(ex, "Lỗi khi gộp hóa đơn: {Message}, InnerException: {InnerException}", ex.Message, ex.InnerException?.Message);
                        return StatusCode(500, new { success = false, message = $"Lỗi khi gộp hóa đơn: {ex.Message}, Nội dung chi tiết: {ex.InnerException?.Message}" });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xử lý gộp hóa đơn: {Message}, InnerException: {InnerException}", ex.Message, ex.InnerException?.Message);
                return StatusCode(500, new { success = false, message = $"Lỗi khi xử lý gộp hóa đơn: {ex.Message}, Nội dung chi tiết: {ex.InnerException?.Message}" });
            }
        }

        // Cập nhật nhóm đặt phòng
        [HttpPost("UpdateDatPhongGroup")]
        public async Task<IActionResult> UpdateDatPhongGroup([FromBody] UpdateDatPhongGroupModel model)
        {
            try
            {
                if (model == null || model.MaDatPhong <= 0 || !model.MaNhomDatPhong.HasValue)
                    return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ: MaDatPhong hoặc MaNhomDatPhong không hợp lệ" });

                var datPhong = await _context.DatPhong.FirstOrDefaultAsync(dp => dp.MaDatPhong == model.MaDatPhong);
                if (datPhong == null)
                    return NotFound(new { success = false, message = $"Không tìm thấy DatPhong với MaDatPhong = {model.MaDatPhong}" });

                datPhong.MaNhomDatPhong = model.MaNhomDatPhong;
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Đã cập nhật MaNhomDatPhong = {model.MaNhomDatPhong} cho DatPhong = {model.MaDatPhong}");
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật DatPhong: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = $"Lỗi khi cập nhật DatPhong: {ex.Message}" });
            }
        }

        // Lấy danh sách phòng
        [HttpGet("GetRooms")]
        public async Task<IActionResult> GetRooms()
        {
            try
            {
                var rooms = await _context.Phong
                    .Include(p => p.LoaiPhong)
                    .Select(p => new
                    {
                        MaPhong = p.MaPhong,
                        SoPhong = p.SoPhong,
                        DangSuDung = p.DangSuDung,
                        TrangThai = p.DangSuDung ? "Đang sử dụng" : "Trống",
                        LoaiPhong = p.LoaiPhong.TenLoaiPhong,
                        GiaTheoGio = p.LoaiPhong.GiaTheoGio,
                        GiaTheoNgay = p.LoaiPhong.GiaTheoNgay
                    })
                    .ToListAsync();
                return Ok(new { success = true, rooms });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tải danh sách phòng: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = $"Lỗi khi tải danh sách phòng: {ex.Message}" });
            }
        }

        [HttpGet("GetRoomTypes")]
        public async Task<IActionResult> GetRoomTypes()
        {
            try
            {
                var roomTypes = await _context.LoaiPhong
                    .Select(lp => new { TenLoaiPhong = lp.TenLoaiPhong })
                    .Distinct()
                    .ToListAsync();
                return Ok(new { success = true, roomTypes });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách loại phòng: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = $"Lỗi khi lấy danh sách loại phòng: {ex.Message}" });
            }
        }

        [HttpGet("GetRoomStatuses")]
        public async Task<IActionResult> GetRoomStatuses()
        {
            try
            {
                var statuses = await _context.Phong
                    .Select(p => p.DangSuDung)
                    .Distinct()
                    .ToListAsync();
                return Ok(new { success = true, statuses });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách trạng thái phòng: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = $"Lỗi khi lấy danh sách trạng thái phòng: {ex.Message}" });
            }
        }

        // Cập nhật trạng thái phòng
        [HttpPost("UpdateRoomStatus")]
        public async Task<IActionResult> UpdateRoomStatus()
        {
            try
            {
                var datPhongs = await _context.DatPhong
                    .Include(dp => dp.Phong)
                    .Where(dp => dp.TrangThai == "Đã nhận phòng" && dp.NgayNhanPhongDuKien.HasValue)
                    .ToListAsync();

                foreach (var dp in datPhongs)
                {
                    if (dp.NgayNhanPhongDuKien <= DateTime.Now.Date && dp.Phong != null)
                    {
                        dp.Phong.DangSuDung = true;
                    }
                }

                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "Cập nhật trạng thái phòng thành công!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật trạng thái phòng: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = $"Lỗi khi cập nhật trạng thái phòng: {ex.Message}" });
            }
        }

        // Lấy chi tiết đặt phòng theo phòng
        [HttpGet("GetBookingDetailsByRoom/{maPhong}")]
        public async Task<IActionResult> GetBookingDetailsByRoom(int maPhong)
        {
            try
            {
                var booking = await _context.DatPhong
                    .Where(dp => dp.MaPhong == maPhong && (dp.TrangThai == "Đã nhận phòng" || dp.TrangThai == "Đã đặt"))
                    .Select(dp => new
                    {
                        maDatPhong = dp.MaDatPhong,
                        maPhong = dp.MaPhong,
                        customer = dp.KhachHangLuuTru.HoTen,
                        ngayNhanPhong = dp.NgayNhanPhong
                    })
                    .FirstOrDefaultAsync();

                if (booking == null)
                    return Ok(new { success = false, message = "Không tìm thấy thông tin đặt phòng" });

                return Ok(new { success = true, maDatPhong = booking.maDatPhong, maPhong = booking.maPhong, customer = booking.customer, ngayNhanPhong = booking.ngayNhanPhong });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy chi tiết đặt phòng theo phòng: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = $"Lỗi khi lấy chi tiết đặt phòng theo phòng: {ex.Message}" });
            }
        }


        public class EndShiftModel
        {
            public int? MaNhanVien { get; set; }
            public decimal TongTienTrongCa { get; set; }
            public decimal TongTienChuyenGiao { get; set; }
            public int? MaNhanVienCaTiepTheo { get; set; }
            public string GhiChu { get; set; }
        }


    }
}