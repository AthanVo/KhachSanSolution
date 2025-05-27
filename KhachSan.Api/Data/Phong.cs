namespace KhachSan.Api.Data
{
    public class Phong
    {
        public int MaPhong { get; set; }
        public string SoPhong { get; set; }
        public bool DangSuDung { get; set; }
        public int MaLoaiPhong { get; set; }
        public DateTime? ThoiGianTraPhongCuoi { get; set; }

        public LoaiPhong LoaiPhong { get; set; }
        public ICollection<DatPhong> DatPhong { get; set; }
        public string MoTa { get; internal set; }
    }
}