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

WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 5000

# Environment variables (có thể override bằng docker run hoặc docker-compose)
ENV ASPNETCORE_URLS=http://+:5000
ENV ConnectionStrings__PostgresConnection=Host=db.adempxrijpegevyseoef.supabase.co;Port=5432;Username=postgres;Password=Jowl@158091;Database=postgres
ENV Jwt__Issuer=QuanLyKhachSan
ENV Jwt__Audience=QuanLyKhachSanAPI
ENV Jwt__Key=your_very_long_secure_key_1234567890abcdef1234567890abcdef
ENV EmailSettings__SmtpServer=smtp.gmail.com
ENV EmailSettings__SmtpPort=587
ENV EmailSettings__SenderEmail=ducthinh25042003@gmail.com
ENV EmailSettings__SenderPassword=fwcgokjijwcpyssk
ENV EmailSettings__SenderName=Khách Sạn Đức Thịnh

# Health check (cần thêm endpoint /health nếu muốn sử dụng)
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 CMD curl --fail http://localhost:5000/health || exit 1

# Run the app
ENTRYPOINT ["dotnet", "KhachSan.Api.dll"]