namespace KhachSan.Api.Data
{
    public class CaLamViec
    {
        public int MaCaLamViec { get; set; }
        public int MaNhanVien { get; set; }
        public DateTime ThoiGianBatDau { get; set; }
        public DateTime? ThoiGianKetThuc { get; set; }
        public string? TrangThai { get; set; }
        public decimal? TongTienTrongCa { get; set; }
        public decimal? TongTienChuyenGiao { get; set; }
        public int? MaNhanVienCaTiepTheo { get; set; }
        public string? GhiChu { get; set; }

        // Navigation properties
        public virtual NguoiDung NhanVien { get; set; } = null!;
        public virtual NguoiDung? NhanVienCaTiepTheo { get; set; }
        public virtual ICollection<HoaDon> HoaDon { get; set; } = new List<HoaDon>();
        public virtual ICollection<LichSuThaoTac> LichSuThaoTac { get; set; } = new List<LichSuThaoTac>();
    }
}