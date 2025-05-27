namespace KhachSan.Api.Data
{
    public partial class NguoiDung
    {
        public int MaNguoiDung { get; set; }
        public string TenDangNhap { get; set; } = null!;
        public string MatKhau { get; set; } = null!;
        public string? MaPIN { get; set; }
        public string VaiTro { get; set; } = null!;
        public string HoTen { get; set; } = null!;
        public string? Email { get; set; }
        public string? SoDienThoai { get; set; }
        public string? DiaChi { get; set; }
        public DateTime NgayTao { get; set; }
        public DateTime NgayCapNhat { get; set; }
        public string? TrangThai { get; set; }

        // Navigation properties
        public virtual ICollection<CaLamViec> CaLamViecNhanVien { get; set; } = new List<CaLamViec>();
        public virtual ICollection<CaLamViec> CaLamViecNhanVienCaTiepTheo { get; set; } = new List<CaLamViec>();
        public virtual ICollection<DatPhong> DatPhongNguoiDung { get; set; } = new List<DatPhong>();
        public virtual ICollection<DatPhong> DatPhongNhanVien { get; set; } = new List<DatPhong>();
        public virtual ICollection<KhachHangLuuTru> KhachHangLuuTru { get; set; } = new List<KhachHangLuuTru>();
        public virtual ICollection<LichSuThaoTac> LichSuThaoTac { get; set; } = new List<LichSuThaoTac>();
        public virtual ICollection<NhomDatPhong> NhomDatPhongNguoiDaiDien { get; set; } = new List<NhomDatPhong>();
        public virtual ICollection<NhomDatPhong> NhomDatPhongNhanVien { get; set; } = new List<NhomDatPhong>();
        public virtual ICollection<ThongBao> ThongBaoNguoiGui { get; set; } = new List<ThongBao>();
        public virtual ICollection<ThongBao> ThongBaoNguoiNhan { get; set; } = new List<ThongBao>();
    }
}