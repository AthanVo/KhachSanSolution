namespace KhachSan.Api.Data
{
    public class ChiTietDichVu
    {
        public int MaChiTietDichVu { get; set; }
        public int MaDatPhong { get; set; }
        public int MaDichVu { get; set; }
        public int SoLuong { get; set; }
        public decimal DonGia { get; set; }
        public decimal ThanhTien { get; set; }
        public DateTime NgayTao { get; set; }

        public DatPhong DatPhong { get; set; }
        public DichVu DichVu { get; set; }
    }
}