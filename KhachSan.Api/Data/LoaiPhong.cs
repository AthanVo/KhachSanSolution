namespace KhachSan.Api.Data
{
    public class LoaiPhong
    {
        public int MaLoaiPhong { get; set; }
        public string TenLoaiPhong { get; set; }
        public decimal GiaTheoGio { get; set; }
        public decimal GiaTheoNgay { get; set; }
        public string TrangThai { get; set; }

        public ICollection<Phong> Phong { get; set; }
    }
}