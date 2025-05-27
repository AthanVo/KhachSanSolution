namespace KhachSan.Models
{
    // Model cho chức năng kết ca
    public class EndShiftModel
    {
        public int MaCaLamViec { get; set; }
        public decimal TongTienTrongCa { get; set; }
        public int? MaNhanVienCaTiepTheo { get; set; }
        public decimal? TongTienChuyenGiao { get; set; }
        public string GhiChu { get; set; }
        public int? MaNhanVien { get; set; } // Thêm trường để quản trị viên chọn nhân viên
    }

    // Model cho chức năng xác nhận nhận ca
    public class ConfirmHandoverModel
    {
        public int ThongBaoId { get; set; }
    }
}
