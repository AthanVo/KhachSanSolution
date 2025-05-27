namespace KhachSan.Api.Data
{
    public class ThongBao
    {
        public int MaThongBao { get; set; }
        public int? MaNguoiGui { get; set; }
        public int MaNguoiNhan { get; set; }
        public string TieuDe { get; set; }
        public string NoiDung { get; set; }
        public string LoaiThongBao { get; set; }
        public DateTime ThoiGianGui { get; set; }
        public string TrangThai { get; set; }

        public NguoiDung NguoiGui { get; set; }
        public NguoiDung NguoiNhan { get; set; }
    }
}