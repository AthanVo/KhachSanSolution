namespace KhachSan.Api.Data
{
    public class NhomDatPhong
    {
        public int MaNhomDatPhong { get; set; }
        public string TenNhom { get; set; }
        public string HoTenNguoiDaiDien { get; set; }
        public string SoDienThoaiNguoiDaiDien { get; set; }
        public int MaNhanVien { get; set; }
        public int? MaNguoiDaiDien { get; set; }
        public DateTime NgayTao { get; set; }
        public DateTime? NgayNhanPhong { get; set; }
        public DateTime? NgayTraPhong { get; set; }
        public string TrangThai { get; set; }

        public NguoiDung NhanVien { get; set; }
        public NguoiDung NguoiDaiDien { get; set; }
        public ICollection<DatPhong> DatPhong { get; set; }
        public ICollection<HoaDon> HoaDon { get; set; }
    }
}