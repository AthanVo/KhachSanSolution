using System.ComponentModel.DataAnnotations;

namespace KhachSan.Api.Models
{
    public class ModelViewPhong
    {
        public int MaPhong { get; set; }

        [StringLength(10, ErrorMessage = "Số phòng không được vượt quá 10 ký tự.")]
        public string SoPhong { get; set; } = string.Empty;

        [StringLength(20, ErrorMessage = "Loại phòng không được vượt quá 20 ký tự.")]
        public string LoaiPhong { get; set; } = string.Empty;

        public decimal GiaTheoGio { get; set; }
        public decimal GiaTheoNgay { get; set; }
        public bool DangSuDung { get; set; }

        [StringLength(20, ErrorMessage = "Trạng thái không được vượt quá 20 ký tự.")]
        public string TrangThai { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "Tên khách hàng không được vượt quá 100 ký tự.")]
        public string KhachHang { get; set; } = string.Empty;

        public int? MaHoaDon { get; set; } // Sửa thành int? để phù hợp với MaHoaDon trong bảng HoaDon

        [StringLength(100, ErrorMessage = "Tên nhân viên không được vượt quá 100 ký tự.")]
        public string NhanVien { get; set; } = string.Empty;

        public DateTime? NgayNhanPhong { get; set; }

        [StringLength(50, ErrorMessage = "Hiện trạng không được vượt quá 50 ký tự.")]
        public string HienTrang { get; set; } = string.Empty; // Đảm bảo phù hợp với yêu cầu nghiệp vụ

        [StringLength(255, ErrorMessage = "Mô tả không được vượt quá 255 ký tự.")]
        public string MoTa { get; set; } = string.Empty;

        public DateTime? ThoiGianTraPhongCuoi { get; set; }
        public int? MaDatPhong { get; set; } // Sửa thành int? để phù hợp với MaDatPhong trong bảng DatPhong
        public int? MaNhanVien { get; set; }
    }

    public class DatPhongViewModel
    {
        [Required(ErrorMessage = "Mã phòng là bắt buộc.")]
        public int MaPhong { get; set; }

        [Required(ErrorMessage = "Số giấy tờ là bắt buộc.")]
        [StringLength(20, ErrorMessage = "Số giấy tờ không được vượt quá 20 ký tự.")]
        public string SoGiayTo { get; set; } = string.Empty;

        [Required(ErrorMessage = "Họ tên là bắt buộc.")]
        [StringLength(100, ErrorMessage = "Họ tên không được vượt quá 100 ký tự.")]
        public string HoTen { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "Địa chỉ không được vượt quá 100 ký tự.")]
        public string DiaChi { get; set; } = string.Empty;

        [Required(ErrorMessage = "Quốc tịch là bắt buộc.")]
        [StringLength(50, ErrorMessage = "Quốc tịch không được vượt quá 50 ký tự.")]
        public string QuocTich { get; set; } = string.Empty;

        [Required(ErrorMessage = "Loại đặt phòng là bắt buộc.")]
        [RegularExpression("^(Theo giờ|Theo ngày|Qua đêm)$", ErrorMessage = "Loại đặt phải là 'Theo giờ', 'Theo ngày' hoặc 'Qua đêm'.")]
        public string LoaiDat { get; set; } = string.Empty;
    }

    public class ThemDichVuViewModel
    {
        [Required(ErrorMessage = "Mã hóa đơn là bắt buộc.")]
        public int MaHoaDon { get; set; }

        [Required(ErrorMessage = "Mã dịch vụ là bắt buộc.")]
        public int MaDichVu { get; set; } // Sửa thành int để phù hợp với MaDichVu trong bảng DichVu

        [Required(ErrorMessage = "Số lượng là bắt buộc.")]
        [Range(1, 100, ErrorMessage = "Số lượng phải từ 1 đến 100.")]
        public int SoLuong { get; set; }
    }

    public class ThanhToanViewModel
    {
        [Required(ErrorMessage = "Mã hóa đơn là bắt buộc.")]
        public int MaHoaDon { get; set; }

        [StringLength(255, ErrorMessage = "Ghi chú không được vượt quá 255 ký tự.")]
        public string GhiChu { get; set; } = string.Empty;

        [Range(0, double.MaxValue, ErrorMessage = "Tiền khuyến mãi không được âm.")]
        public decimal TienKhuyenMai { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Tiền trả trước không được âm.")]
        public decimal TienTraTruoc { get; set; }
    }

    public class TaoNhomViewModel
    {
        [Required(ErrorMessage = "Tên nhóm là bắt buộc.")]
        [StringLength(100, ErrorMessage = "Tên nhóm không được vượt quá 100 ký tự.")]
        public string TenNhom { get; set; } = string.Empty;

        [Required(ErrorMessage = "Người đại diện là bắt buộc.")]
        [StringLength(100, ErrorMessage = "Tên người đại diện không được vượt quá 100 ký tự.")]
        public string NguoiDaiDien { get; set; } = string.Empty;

        [Required(ErrorMessage = "Số điện thoại là bắt buộc.")]
        [RegularExpression(@"^\d{10,15}$", ErrorMessage = "Số điện thoại phải có từ 10 đến 15 chữ số.")]
        public string SoDienThoai { get; set; } = string.Empty;

        [Required(ErrorMessage = "Danh sách phòng không được để trống.")]
        [MinLength(1, ErrorMessage = "Danh sách phòng phải chứa ít nhất một phòng.")]
        public List<int> DanhSachPhong { get; set; } = new List<int>();
    }

    public class GopHoaDonViewModel
    {
        [Required(ErrorMessage = "Mã nhóm là bắt buộc.")]
        public int MaNhom { get; set; }

        [Required(ErrorMessage = "Danh sách phòng không được để trống.")]
        [MinLength(1, ErrorMessage = "Danh sách phòng phải chứa ít nhất một phòng.")]
        public List<int> DanhSachPhong { get; set; } = new List<int>();
    }

    public class KetCaViewModel
    {
        [Required(ErrorMessage = "Mã nhân viên ca sau là bắt buộc.")]
        public int NhanVienCaSau { get; set; }

        [StringLength(255, ErrorMessage = "Ghi chú không được vượt quá 255 ký tự.")]
        public string GhiChu { get; set; } = string.Empty;
    }

    public class ChuyenPhongViewModel
    {
        [Required(ErrorMessage = "Mã hóa đơn là bắt buộc.")]
        public int MaHoaDon { get; set; }

        [Required(ErrorMessage = "Mã phòng cũ là bắt buộc.")]
        public int MaPhongCu { get; set; }

        [Required(ErrorMessage = "Mã phòng mới là bắt buộc.")]
        public int MaPhongMoi { get; set; }

        [StringLength(255, ErrorMessage = "Lý do không được vượt quá 255 ký tự.")]
        public string LyDo { get; set; } = string.Empty;
    }

    public class DongPhongViewModel
    {
        [Required(ErrorMessage = "Mã phòng là bắt buộc.")]
        public int MaPhong { get; set; }

        [Required(ErrorMessage = "Lý do là bắt buộc.")]
        [StringLength(255, ErrorMessage = "Lý do không được vượt quá 255 ký tự.")]
        public string LyDo { get; set; } = string.Empty;
    }

    public class ChiTietHoaDonViewModel
    {
        public int MaHoaDon { get; set; }

        [StringLength(100, ErrorMessage = "Tên khách hàng không được vượt quá 100 ký tự.")]
        public string TenKhachHang { get; set; } = string.Empty;

        [StringLength(10, ErrorMessage = "Số phòng không được vượt quá 10 ký tự.")]
        public string SoPhong { get; set; } = string.Empty;

        public DateTime? NgayNhanPhong { get; set; } // Đã là DateTime? nên giữ nguyên
        public decimal TienPhong { get; set; }
        public List<ChiTietDichVuViewModel> DanhSachDichVu { get; set; } = new List<ChiTietDichVuViewModel>();
        public decimal TongTienDichVu { get; set; }
        public decimal TongTien { get; set; }
        public decimal TienKhuyenMai { get; set; }
        public decimal TienCanTra { get; set; }
        public decimal TienTraTruoc { get; set; }

        [StringLength(100, ErrorMessage = "Tên nhân viên không được vượt quá 100 ký tự.")]
        public string NhanVienMoPhong { get; set; } = string.Empty;
    }

    public class ChiTietDichVuViewModel
    {
        public int MaDichVu { get; set; } // Sửa thành int để phù hợp với MaDichVu trong bảng DichVu

        [StringLength(100, ErrorMessage = "Tên dịch vụ không được vượt quá 100 ký tự.")]
        public string TenDichVu { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0.")]
        public int SoLuong { get; set; }
        public decimal DonGia { get; set; }
        public decimal ChietKhau { get; set; }
        public decimal ThanhTien { get; set; }
    }
}