namespace KhachSan.Api.Data
{
    public class NhomPhong
    {
        public int MaNhomDatPhong { get; set; }
        public int MaPhong { get; set; }

        public NhomDatPhong NhomDatPhong { get; set; }
        public Phong Phong { get; set; }
    }
}