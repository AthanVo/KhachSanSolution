namespace KhachSan.Api.Data
{
    public class DatPhong
    {
        public int MaDatPhong { get; set; }
        public int MaPhong { get; set; }
        public int MaKhachHangLuuTru { get; set; }
        public int MaNhanVien { get; set; }
        public int? MaNguoiDung { get; set; }
        public int? MaNhomDatPhong { get; set; }
        public int? MaGiamGia { get; set; }
        public DateTime NgayNhanPhong { get; set; }
        public DateTime? NgayNhanPhongDuKien { get; set; }
        public DateTime? NgayTraPhong { get; set; }
        public DateTime NgayTao { get; set; }
        public DateTime NgayCapNhat { get; set; }
        public string TrangThai { get; set; }
        public string TrangThaiThanhToan { get; set; }
        public string TrangThaiBaoCaoTamTru { get; set; }
        public string LoaiDatPhong { get; set; }
        public int? TongThoiGian { get; set; }
        public decimal? TongTienTheoThoiGian { get; set; }
        public decimal? TongTienDichVu { get; set; }
        public decimal? SoTienGiam { get; set; }

        public Phong Phong { get; set; }
        public KhachHangLuuTru KhachHangLuuTru { get; set; }
        public NguoiDung NhanVien { get; set; }
        public NguoiDung NguoiDung { get; set; }
        public NhomDatPhong NhomDatPhong { get; set; }
        public GiamGia GiamGia { get; set; }
        public ICollection<ChiTietDichVu> ChiTietDichVu { get; set; }
        public ICollection<HoaDon> HoaDon { get; set; }
    }
}