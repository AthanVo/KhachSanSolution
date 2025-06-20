﻿namespace KhachSan.Api.Models
{
    public class BookRoomModel
    {
        public int MaPhong { get; set; }
        public string LoaiGiayTo { get; set; }
        public string SoGiayTo { get; set; }
        public string HoTen { get; set; }
        public string DiaChi { get; set; }
        public string QuocTich { get; set; }
        public string LoaiDatPhong { get; set; }
        public DateTime? NgayNhanPhongDuKien { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("ngayNhanPhong")]
        public DateTime? NgayNhanPhong { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("ngayTraPhong")]
        public DateTime? NgayTraPhong { get; set; }
    }
}