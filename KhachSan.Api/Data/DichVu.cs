namespace KhachSan.Api.Data
{
    public class DichVu
    {
        public int MaDichVu { get; set; }
        public string TenDichVu { get; set; }
        public decimal Gia { get; set; }
        public string TrangThai { get; set; }

        public ICollection<ChiTietDichVu> ChiTietDichVu { get; set; }
    }
}