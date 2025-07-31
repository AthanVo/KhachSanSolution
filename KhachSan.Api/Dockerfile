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

# Tạo thư mục app
WORKDIR /app

# Copy ứng dụng từ build stage
COPY --from=build /app/publish .

# Expose HTTP port only
EXPOSE 5000

# Environment variables
ENV ASPNETCORE_ENVIRONMENT=Production

# Chỉ dùng HTTP để tránh lỗi SSL certificate trong Docker
ENV ASPNETCORE_URLS=http://+:5000

# Tắt HTTPS redirect và certificate requirements
ENV ASPNETCORE_HTTPS_PORT=""
ENV ASPNETCORE_HTTPS_PORTS=""
ENV ASPNETCORE_FORWARDEDHEADERS_ENABLED=true

# Database connection
ENV ConnectionStrings__PostgresConnection="Host=db.adempxrijpegevyseoef.supabase.co;Port=5432;Username=postgres;Password=Jowl@158091;Database=postgres"

# JWT Configuration
ENV Jwt__Issuer=QuanLyKhachSan
ENV Jwt__Audience=QuanLyKhachSanAPI
ENV Jwt__Key="your_very_long_secure_key_1234567890abcdef1234567890abcdef"

# Email Configuration
ENV EmailSettings__SmtpServer=smtp.gmail.com
ENV EmailSettings__SmtpPort=587
ENV EmailSettings__SenderEmail=ducthinh25042003@gmail.com
ENV EmailSettings__SenderPassword=fwcgokjijwcpyssk

# Health check - sử dụng HTTP
HEALTHCHECK --interval=30s --timeout=10s --start-period=30s --retries=3 \
    CMD curl --fail --connect-timeout 5 http://localhost:5000/health || exit 1

# Run the app
ENTRYPOINT ["dotnet", "KhachSan.Api.dll"]