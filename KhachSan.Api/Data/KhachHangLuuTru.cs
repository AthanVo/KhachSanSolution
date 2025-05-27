namespace KhachSan.Api.Data
{
    public class KhachHangLuuTru
    {
        public int MaKhachHangLuuTru { get; set; }
        public string? LoaiGiayTo { get; set; }
        public string? SoGiayTo { get; set; }
        public string HoTen { get; set; } = null!;
        public DateTime? NgaySinh { get; set; }
        public string? GioiTinh { get; set; }
        public string? DiaChi { get; set; }
        public string? QuocTich { get; set; }
        public DateTime? NgayCapGiayTo { get; set; }
        public string? NoiCapGiayTo { get; set; }
        public int? MaNguoiDung { get; set; }
        public DateTime NgayTao { get; set; }
        public DateTime NgayCapNhat { get; set; }

        // Navigation properties
        public virtual NguoiDung? NguoiDung { get; set; }
        public virtual ICollection<DatPhong> DatPhong { get; set; } = new List<DatPhong>();
    }
}