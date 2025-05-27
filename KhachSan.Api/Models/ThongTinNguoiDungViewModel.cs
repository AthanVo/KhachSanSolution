namespace KhachSan.Models
{
    public class ThongTinNguoiDungViewModell
    {
        // Thuộc tính cho thông tin người dùng (dùng trong Thongtinnguoidung.cshtml)
        public int NguoiDungId { get; set; }
        public string HoTen { get; set; }
        public string Email { get; set; }
        public string SoDienThoai { get; set; }
        public string DiaChi { get; set; }
        public DateTime? NgayDangKy { get; set; }

       
    }
}