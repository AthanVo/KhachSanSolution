namespace KhachSan.Api.Data
{
    public class LichSuThaoTac
    {
        public int MaThaoTac { get; set; }
        public int MaCaLamViec { get; set; }
        public int MaNhanVien { get; set; }
        public string LoaiThaoTac { get; set; }
        public string ChiTiet { get; set; }
        public DateTime ThoiGian { get; set; }

        public CaLamViec CaLamViec { get; set; }
        public NguoiDung NhanVien { get; set; }
    }
}