using Microsoft.EntityFrameworkCore;

namespace KhachSan.Api.Data
{
    public partial class ApplicationDBContext : DbContext
    {
        public ApplicationDBContext()
        {
        }

        public ApplicationDBContext(DbContextOptions<ApplicationDBContext> options)
            : base(options)
        {
        }

        public virtual DbSet<CaLamViec> CaLamViec { get; set; }
        public virtual DbSet<ChiTietDichVu> ChiTietDichVu { get; set; }
        public virtual DbSet<DatPhong> DatPhong { get; set; }
        public virtual DbSet<DichVu> DichVu { get; set; }
        public virtual DbSet<GiamGia> GiamGia { get; set; }
        public virtual DbSet<HoaDon> HoaDon { get; set; }
        public virtual DbSet<KhachHangLuuTru> KhachHangLuuTru { get; set; }
        public virtual DbSet<LichSuThaoTac> LichSuThaoTac { get; set; }
        public virtual DbSet<LoaiPhong> LoaiPhong { get; set; }
        public virtual DbSet<NguoiDung> NguoiDung { get; set; }
        public virtual DbSet<NhomDatPhong> NhomDatPhong { get; set; }
        public virtual DbSet<Phong> Phong { get; set; }
        public virtual DbSet<ThongBao> ThongBao { get; set; }
        public DbSet<NhomPhong> NhomPhong { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                throw new InvalidOperationException("DbContext has not been configured. Ensure the connection string is set in appsettings.json and registered in Program.cs.");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<NhomPhong>()
                .HasKey(np => new { np.MaNhomDatPhong, np.MaPhong });

            modelBuilder.Entity<NhomPhong>()
                .HasOne(np => np.NhomDatPhong)
                .WithMany()
                .HasForeignKey(np => np.MaNhomDatPhong);

            modelBuilder.Entity<NhomPhong>()
                .HasOne(np => np.Phong)
                .WithMany()
                .HasForeignKey(np => np.MaPhong);

            modelBuilder.Entity<CaLamViec>(entity =>
            {
                entity.HasKey(e => e.MaCaLamViec).HasName("PK__CaLamVie__E545F625FDEC358D");

                entity.Property(e => e.ThoiGianBatDau).HasColumnType("datetime");
                entity.Property(e => e.ThoiGianKetThuc).HasColumnType("datetime");
                entity.Property(e => e.TongTienChuyenGiao).HasColumnType("decimal(10, 2)");
                entity.Property(e => e.TongTienTrongCa).HasColumnType("decimal(10, 2)");
                entity.Property(e => e.TrangThai)
                    .HasMaxLength(20)
                    .HasDefaultValue("Đang làm việc");

                entity.HasOne(d => d.NhanVien)
                    .WithMany(p => p.CaLamViecNhanVien)
                    .HasForeignKey(d => d.MaNhanVien)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__CaLamViec__MaNha__628FA481");

                entity.HasOne(d => d.NhanVienCaTiepTheo)
                    .WithMany(p => p.CaLamViecNhanVienCaTiepTheo)
                    .HasForeignKey(d => d.MaNhanVienCaTiepTheo)
                    .HasConstraintName("FK__CaLamViec__MaNha__6383C8BA");
            });

            modelBuilder.Entity<ChiTietDichVu>(entity =>
            {
                entity.HasKey(e => e.MaChiTietDichVu).HasName("PK__ChiTietD__11EFCA671558A745");

                entity.ToTable(tb => tb.HasTrigger("TR_CapNhat_TongTienDichVu"));

                entity.Property(e => e.DonGia).HasColumnType("decimal(10, 2)");
                entity.Property(e => e.NgayTao)
                    .HasDefaultValueSql("(getdate())")
                    .HasColumnType("datetime");
                entity.Property(e => e.ThanhTien).HasColumnType("decimal(10, 2)");

                entity.HasOne(d => d.DatPhong)
                    .WithMany(p => p.ChiTietDichVu)
                    .HasForeignKey(d => d.MaDatPhong)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__ChiTietDi__MaDat__76969D2E");

                entity.HasOne(d => d.DichVu)
                    .WithMany(p => p.ChiTietDichVu)
                    .HasForeignKey(d => d.MaDichVu)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__ChiTietDi__MaDic__778AC167");
            });

            modelBuilder.Entity<DatPhong>(entity =>
            {
                entity.HasKey(e => e.MaDatPhong).HasName("PK__DatPhong__6344ADEAC48CFCB8");

                entity.ToTable(tb => tb.HasTrigger("UpdatePhong_DangSuDung"));

                entity.Property(e => e.NgayCapNhat)
                    .HasDefaultValueSql("(getdate())")
                    .HasColumnType("datetime");
                entity.Property(e => e.NgayNhanPhong).HasColumnType("datetime");
                entity.Property(e => e.NgayTao)
                    .HasDefaultValueSql("(getdate())")
                    .HasColumnType("datetime");
                entity.Property(e => e.NgayTraPhong).HasColumnType("datetime");
                entity.Property(e => e.SoTienGiam).HasColumnType("decimal(10, 2)");
                entity.Property(e => e.TongTienDichVu).HasColumnType("decimal(10, 2)");
                entity.Property(e => e.TongTienTheoThoiGian).HasColumnType("decimal(10, 2)");
                entity.Property(e => e.TrangThai)
                    .HasMaxLength(20)
                    .HasDefaultValue("Chờ xác nhận");
                entity.Property(e => e.TrangThaiBaoCaoTamTru)
                    .HasMaxLength(20)
                    .HasDefaultValue("Chưa báo cáo");
                entity.Property(e => e.TrangThaiThanhToan)
                    .HasMaxLength(20)
                    .HasDefaultValue("Chưa thanh toán");

                entity.HasOne(d => d.GiamGia)
                    .WithMany(p => p.DatPhong)
                    .HasForeignKey(d => d.MaGiamGia)
                    .HasConstraintName("FK__DatPhong__MaGiam__72C60C4A");

                entity.HasOne(d => d.KhachHangLuuTru)
                    .WithMany(p => p.DatPhong)
                    .HasForeignKey(d => d.MaKhachHangLuuTru)
                    .HasConstraintName("FK__DatPhong__MaKhac__71D1E811");

                entity.HasOne(d => d.NguoiDung)
                    .WithMany(p => p.DatPhongNguoiDung)
                    .HasForeignKey(d => d.MaNguoiDung)
                    .HasConstraintName("FK__DatPhong__MaNguo__6EF57B66");

                entity.HasOne(d => d.NhanVien)
                    .WithMany(p => p.DatPhongNhanVien)
                    .HasForeignKey(d => d.MaNhanVien)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__DatPhong__MaNhan__6FE99F9F");

                entity.HasOne(d => d.NhomDatPhong)
                    .WithMany(p => p.DatPhong)
                    .HasForeignKey(d => d.MaNhomDatPhong)
                    .HasConstraintName("FK__DatPhong__MaNhom__6E01572D");

                entity.HasOne(d => d.Phong)
                    .WithMany(p => p.DatPhong)
                    .HasForeignKey(d => d.MaPhong)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__DatPhong__MaPhon__70DDC3D8");
            });

            modelBuilder.Entity<DichVu>(entity =>
            {
                entity.HasKey(e => e.MaDichVu).HasName("PK__DichVu__C0E6DE8FE6EFC6C0");

                entity.Property(e => e.Gia).HasColumnType("decimal(10, 2)");
                entity.Property(e => e.TenDichVu).HasMaxLength(100);
                entity.Property(e => e.TrangThai)
                    .HasMaxLength(20)
                    .HasDefaultValue("Hoạt động");
            });

            modelBuilder.Entity<GiamGia>(entity =>
            {
                entity.HasKey(e => e.MaGiamGiaId).HasName("PK__GiamGia__C28471F31FC63FF9");

                entity.HasIndex(e => e.MaGiamGia, "UQ__GiamGia__EF9458E5C657FF5F").IsUnique();

                entity.Property(e => e.GiaTriGiam).HasColumnType("decimal(10, 2)");
                entity.Property(e => e.MaGiamGia).HasMaxLength(50);
                entity.Property(e => e.MoTa).HasMaxLength(50);
                entity.Property(e => e.SoLuongDaDung).HasDefaultValue(0);
                entity.Property(e => e.SoTienDatToiThieu).HasColumnType("decimal(10, 2)");
                entity.Property(e => e.TenMaGiamGia).HasMaxLength(50);
                entity.Property(e => e.TrangThai)
                    .HasMaxLength(20)
                    .HasDefaultValue("Hoạt động");
            });

            modelBuilder.Entity<HoaDon>(entity =>
            {
                entity.HasKey(e => e.MaHoaDon).HasName("PK__HoaDon__835ED13BC44285B8");

                entity.Property(e => e.LoaiHoaDon).HasMaxLength(20);
                entity.Property(e => e.NgayXuat)
                    .HasDefaultValueSql("(getdate())")
                    .HasColumnType("datetime");
                entity.Property(e => e.PhuongThucThanhToan).HasMaxLength(20);
                entity.Property(e => e.TongTien).HasColumnType("decimal(10, 2)");
                entity.Property(e => e.TrangThaiThanhToan)
                    .HasMaxLength(20)
                    .HasDefaultValue("Chưa thanh toán");

                entity.HasOne(d => d.CaLamViec)
                    .WithMany(p => p.HoaDon)
                    .HasForeignKey(d => d.MaCaLamViec)
                    .HasConstraintName("FK__HoaDon__MaCaLamV__7F2BE32F");

                entity.HasOne(d => d.DatPhong)
                    .WithMany(p => p.HoaDon)
                    .HasForeignKey(d => d.MaDatPhong)
                    .HasConstraintName("FK__HoaDon__MaDatPho__01142BA1");

                entity.HasOne(d => d.NhomDatPhong)
                    .WithMany(p => p.HoaDon)
                    .HasForeignKey(d => d.MaNhomDatPhong)
                    .HasConstraintName("FK__HoaDon__MaNhomDa__00200768");
            });

            modelBuilder.Entity<KhachHangLuuTru>(entity =>
            {
                entity.HasKey(e => e.MaKhachHangLuuTru).HasName("PK__KhachHan__7FE4FD2A50E5FFCD");

                entity.HasIndex(e => e.SoGiayTo, "UQ__KhachHan__04338D25F26395F9").IsUnique();

                entity.Property(e => e.GioiTinh).HasMaxLength(10);
                entity.Property(e => e.HoTen).HasMaxLength(100);
                entity.Property(e => e.LoaiGiayTo).HasMaxLength(20);
                entity.Property(e => e.NgayCapNhat)
                    .HasDefaultValueSql("(getdate())")
                    .HasColumnType("datetime");
                entity.Property(e => e.NgayTao)
                    .HasDefaultValueSql("(getdate())")
                    .HasColumnType("datetime");
                entity.Property(e => e.NoiCapGiayTo).HasMaxLength(100);
                entity.Property(e => e.QuocTich).HasMaxLength(50);
                entity.Property(e => e.SoGiayTo).HasMaxLength(20);

                entity.HasOne(d => d.NguoiDung)
                    .WithMany(p => p.KhachHangLuuTru)
                    .HasForeignKey(d => d.MaNguoiDung)
                    .HasConstraintName("FK__KhachHang__MaNgu__5441852A");
            });

            modelBuilder.Entity<LichSuThaoTac>(entity =>
            {
                entity.HasKey(e => e.MaThaoTac).HasName("PK__LichSuTh__32ACE293D87272B6");

                entity.Property(e => e.LoaiThaoTac).HasMaxLength(20);
                entity.Property(e => e.ThoiGian)
                    .HasDefaultValueSql("(getdate())")
                    .HasColumnType("datetime");

                entity.HasOne(d => d.CaLamViec)
                    .WithMany(p => p.LichSuThaoTac)
                    .HasForeignKey(d => d.MaCaLamViec)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__LichSuTha__MaCaL__05D8E0BE");

                entity.HasOne(d => d.NhanVien)
                    .WithMany(p => p.LichSuThaoTac)
                    .HasForeignKey(d => d.MaNhanVien)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__LichSuTha__MaNha__06CD04F7");
            });

            modelBuilder.Entity<LoaiPhong>(entity =>
            {
                entity.HasKey(e => e.MaLoaiPhong).HasName("PK__LoaiPhon__2302121717C25051");

                entity.Property(e => e.GiaTheoGio).HasColumnType("decimal(10, 2)");
                entity.Property(e => e.GiaTheoNgay).HasColumnType("decimal(10, 2)");
                entity.Property(e => e.TenLoaiPhong).HasMaxLength(20);
                entity.Property(e => e.TrangThai)
                    .HasMaxLength(20)
                    .HasDefaultValue("Hoạt động");
            });

            modelBuilder.Entity<NguoiDung>(entity =>
            {
                entity.HasKey(e => e.MaNguoiDung).HasName("PK__NguoiDun__C539D76229599F06");

                entity.HasIndex(e => e.TenDangNhap, "UQ__NguoiDun__55F68FC026BE0A59").IsUnique();

                entity.Property(e => e.Email).HasMaxLength(100);
                entity.Property(e => e.HoTen).HasMaxLength(100);
                entity.Property(e => e.MaPIN).HasMaxLength(10);
                entity.Property(e => e.MatKhau).HasMaxLength(255);
                entity.Property(e => e.NgayCapNhat)
                    .HasDefaultValueSql("(getdate())")
                    .HasColumnType("datetime");
                entity.Property(e => e.NgayTao)
                    .HasDefaultValueSql("(getdate())")
                    .HasColumnType("datetime");
                entity.Property(e => e.SoDienThoai).HasMaxLength(15);
                entity.Property(e => e.TenDangNhap).HasMaxLength(50);
                entity.Property(e => e.TrangThai)
                    .HasMaxLength(20)
                    .HasDefaultValue("Hoạt động");
                entity.Property(e => e.VaiTro).HasMaxLength(20);
            });

            modelBuilder.Entity<NhomDatPhong>(entity =>
            {
                entity.HasKey(e => e.MaNhomDatPhong).HasName("PK__NhomDatP__F91B37A1C2829A17");

                entity.Property(e => e.HoTenNguoiDaiDien).HasMaxLength(100);
                entity.Property(e => e.NgayTao)
                    .HasDefaultValueSql("(getdate())")
                    .HasColumnType("datetime");
                entity.Property(e => e.SoDienThoaiNguoiDaiDien).HasMaxLength(15);
                entity.Property(e => e.TenNhom).HasMaxLength(100);
                entity.Property(e => e.TrangThai)
                    .HasMaxLength(20)
                    .HasDefaultValue("Đang xử lý");

                entity.HasOne(d => d.NguoiDaiDien)
                    .WithMany(p => p.NhomDatPhongNguoiDaiDien)
                    .HasForeignKey(d => d.MaNguoiDaiDien)
                    .HasConstraintName("FK__NhomDatPh__MaNgu__4BAC3F29");

                entity.HasOne(d => d.NhanVien)
                    .WithMany(p => p.NhomDatPhongNhanVien)
                    .HasForeignKey(d => d.MaNhanVien)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__NhomDatPh__MaNha__4CA06362");
            });

            modelBuilder.Entity<Phong>(entity =>
            {
                entity.HasKey(e => e.MaPhong).HasName("PK__Phong__20BD5E5B0F308B5B");

                entity.HasIndex(e => e.SoPhong, "UQ__Phong__7C736CA12A9456E4").IsUnique();

                entity.Property(e => e.DangSuDung).HasDefaultValue(false);
                entity.Property(e => e.SoPhong).HasMaxLength(10);
                entity.Property(e => e.ThoiGianTraPhongCuoi).HasColumnType("datetime");

                entity.HasOne(d => d.LoaiPhong)
                    .WithMany(p => p.Phong)
                    .HasForeignKey(d => d.MaLoaiPhong)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__Phong__MaLoaiPho__45F365D3");
            });

            modelBuilder.Entity<ThongBao>(entity =>
            {
                entity.HasKey(e => e.MaThongBao).HasName("PK__ThongBao__04DEB54E7BC64C60");

                entity.Property(e => e.LoaiThongBao).HasMaxLength(20);
                entity.Property(e => e.ThoiGianGui)
                    .HasDefaultValueSql("(getdate())")
                    .HasColumnType("datetime");
                entity.Property(e => e.TieuDe).HasMaxLength(100);
                entity.Property(e => e.TrangThai)
                    .HasMaxLength(20)
                    .HasDefaultValue("Chưa đọc");

                entity.HasOne(d => d.NguoiGui)
                    .WithMany(p => p.ThongBaoNguoiGui)
                    .HasForeignKey(d => d.MaNguoiGui)
                    .HasConstraintName("FK__ThongBao__MaNguo__0D7A0286");

                entity.HasOne(d => d.NguoiNhan)
                    .WithMany(p => p.ThongBaoNguoiNhan)
                    .HasForeignKey(d => d.MaNguoiNhan)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__ThongBao__MaNguo__0E6E26BF");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}