# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["KhachSan.Api/KhachSan.Api.csproj", "KhachSan.Api/"]
RUN dotnet restore "KhachSan.Api/KhachSan.Api.csproj"
COPY . .
WORKDIR "/src/KhachSan.Api"
RUN dotnet publish "KhachSan.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

# Cài đặt curl cho health check
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Tạo thư mục app và set permissions
WORKDIR /app

# Sao chép chứng chỉ SSL (nếu có)
COPY cert.pem /app/cert.pem
COPY key.pem /app/key.pem

# Copy ứng dụng từ build stage
COPY --from=build /app/publish .

# Expose cả HTTP và HTTPS ports
EXPOSE 5000
EXPOSE 5001

# Environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=https://+:5001;http://+:5000
ENV ASPNETCORE_Kestrel__Certificates__Default__Path=/app/cert.pem
ENV ASPNETCORE_Kestrel__Certificates__Default__KeyPath=/app/key.pem

# Database connection
ENV ConnectionStrings__PostgresConnection=Host=db.adempxrijpegevyseoef.supabase.co;Port=5432;Username=postgres;Password=Jowl@158091;Database=postgres

# JWT Configuration
ENV Jwt__Issuer=QuanLyKhachSan
ENV Jwt__Audience=QuanLyKhachSanAPI
ENV Jwt__Key=your_very_long_secure_key_1234567890abcdef1234567890abcdef

# Email Configuration
ENV EmailSettings__SmtpServer=smtp.gmail.com
ENV EmailSettings__SmtpPort=587
ENV EmailSettings__SenderEmail=ducthinh25042003@gmail.com
ENV EmailSettings__SenderPassword=fwcgokjijwcpyssk

# Health check - sử dụng HTTP thay vì HTTPS để tránh lỗi certificate
HEALTHCHECK --interval=30s --timeout=10s --start-period=15s --retries=3 \
    CMD curl --fail http://localhost:5000/health || exit 1

# Run the app
ENTRYPOINT ["dotnet", "KhachSan.Api.dll"]