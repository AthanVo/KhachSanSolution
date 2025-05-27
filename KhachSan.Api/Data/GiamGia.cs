namespace KhachSan.Api.Data
{
    public class GiamGia
    {
        public int MaGiamGiaId { get; set; }
        public string MaGiamGia { get; set; }
        public string TenMaGiamGia { get; set; }
        public string MoTa { get; set; }
        public decimal GiaTriGiam { get; set; }
        public decimal SoTienDatToiThieu { get; set; }
        public int SoLuong { get; set; }
        public int SoLuongDaDung { get; set; }
        public string TrangThai { get; set; }

        public ICollection<DatPhong> DatPhong { get; set; }
    }
}