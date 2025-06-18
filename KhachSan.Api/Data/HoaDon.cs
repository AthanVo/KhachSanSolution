namespace KhachSan.Api.Data
{
    public class HoaDon
    {
        public int MaHoaDon { get; set; }
        public int? MaCaLamViec { get; set; }
        public int? MaDatPhong { get; set; }
        public int? MaNhomDatPhong { get; set; }
        public DateTime NgayXuat { get; set; }
        public decimal? TongTien { get; set; }
        public string PhuongThucThanhToan { get; set; }
        public string TrangThaiThanhToan { get; set; }
        public string LoaiHoaDon { get; set; }
        public string GhiChu { get; set; }

        public CaLamViec CaLamViec { get; set; }
        public DatPhong DatPhong { get; set; }
        public NhomDatPhong NhomDatPhong { get; set; }
    }
}