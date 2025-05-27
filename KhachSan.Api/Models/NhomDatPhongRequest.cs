namespace KhachSan.Api.Models
{
    public class NhomDatPhongRequest
    {
        public int? MaNhomDatPhong { get; set; }
        public string TenNhom { get; set; }
        public string HoTenNguoiDaiDien { get; set; }
        public string SoDienThoaiNguoiDaiDien { get; set; }
        public int[] MaPhong { get; set; }
        public DateTime? NgayNhanPhong { get; set; }
        public DateTime? NgayTraPhong { get; set; }
    }
}
