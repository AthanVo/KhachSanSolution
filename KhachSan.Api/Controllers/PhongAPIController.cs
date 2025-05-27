using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KhachSan.Api.Data;
using KhachSan.Api.Models;

namespace KhachSan.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize(Roles = "Nhân viên, Quản trị")]
    public class PhongAPIController : ControllerBase
    {
        private readonly ApplicationDBContext _context;
        private readonly ILogger<PhongAPIController> _logger;

        public PhongAPIController(ApplicationDBContext context, ILogger<PhongAPIController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetRooms(int page = 1)
        {
            try
            {
                // Số phòng trên mỗi trang
                const int pageSize = 16;

                // Tính tổng số phòng
                var totalRooms = await _context.Phong.CountAsync();

                // Tính tổng số trang
                var totalPages = (int)Math.Ceiling((double)totalRooms / pageSize);

                // Đảm bảo page hợp lệ
                page = page < 1 ? 1 : page;
                page = page > totalPages ? totalPages : page;

                // Load dữ liệu phòng từ CSDL với phân trang
                var rooms = await _context.Phong
                    .Include(p => p.LoaiPhong)
                    .Include(p => p.DatPhong.Where(dp => dp.TrangThai == "Đã nhận phòng" || dp.TrangThai == "Đã trả phòng"))
                        .ThenInclude(dp => dp.KhachHangLuuTru)
                    .Include(p => p.DatPhong)
                        .ThenInclude(dp => dp.NhanVien)
                    .Include(p => p.DatPhong)
                        .ThenInclude(dp => dp.HoaDon)
                    .Select(p => new ModelViewPhong
                    {
                        MaPhong = p.MaPhong,
                        SoPhong = p.SoPhong,
                        LoaiPhong = p.LoaiPhong.TenLoaiPhong,
                        GiaTheoGio = p.LoaiPhong.GiaTheoGio,
                        GiaTheoNgay = p.LoaiPhong.GiaTheoNgay,
                        DangSuDung = p.DangSuDung,
                        MoTa = p.MoTa,
                        TrangThai = p.DangSuDung ? "Đang sử dụng" : "Trống",
                        MaDatPhong = p.DangSuDung ? p.DatPhong
                            .Where(dp => dp.MaPhong == p.MaPhong && dp.TrangThai == "Đã nhận phòng")
                            .Select(dp => (int?)dp.MaDatPhong)
                            .FirstOrDefault() : null,
                        KhachHang = p.DangSuDung ? p.DatPhong
                            .Where(dp => dp.MaPhong == p.MaPhong && dp.TrangThai == "Đã nhận phòng")
                            .Select(dp => dp.MaKhachHangLuuTru != null ? dp.KhachHangLuuTru.HoTen : "")
                            .FirstOrDefault() : "",
                        NhanVien = p.DangSuDung ? p.DatPhong
                            .Where(dp => dp.MaPhong == p.MaPhong && dp.TrangThai == "Đã nhận phòng")
                            .Select(dp => dp.MaNhanVien != null ? dp.NhanVien.HoTen : "")
                            .FirstOrDefault() : "",
                        MaNhanVien = p.DangSuDung ? p.DatPhong
                            .Where(dp => dp.MaPhong == p.MaPhong && dp.TrangThai == "Đã nhận phòng")
                            .Select(dp => (int?)dp.MaNhanVien)
                            .FirstOrDefault() : null,
                        NgayNhanPhong = p.DangSuDung ? p.DatPhong
                            .Where(dp => dp.MaPhong == p.MaPhong && dp.TrangThai == "Đã nhận phòng")
                            .Select(dp => (DateTime?)dp.NgayNhanPhong)
                            .FirstOrDefault() : null,
                        MaHoaDon = p.DangSuDung ? p.DatPhong
                            .Where(dp => dp.MaPhong == p.MaPhong && dp.TrangThai == "Đã nhận phòng")
                            .Select(dp => dp.HoaDon != null && dp.HoaDon.Any() ? (int?)dp.HoaDon.FirstOrDefault().MaHoaDon : null)
                            .FirstOrDefault() : null,
                        HienTrang = p.MoTa,
                        ThoiGianTraPhongCuoi = !p.DangSuDung ? p.DatPhong
                            .Where(dp => dp.MaPhong == p.MaPhong && dp.TrangThai == "Đã trả phòng")
                            .OrderByDescending(dp => dp.NgayTraPhong)
                            .Select(dp => (DateTime?)dp.NgayTraPhong)
                            .FirstOrDefault() : null
                    })
                    .OrderBy(p => p.SoPhong)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // Trả về dữ liệu dưới dạng JSON
                return Ok(new
                {
                    success = true,
                    rooms = rooms,
                    currentPage = page,
                    totalPages = totalPages,
                    pageSize = pageSize,
                    totalRooms = totalRooms
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Có lỗi xảy ra khi tải danh sách phòng qua API.");
                return StatusCode(500, new { success = false, message = "Có lỗi xảy ra khi tải danh sách phòng. Vui lòng thử lại sau." });
            }
        }
    }
}