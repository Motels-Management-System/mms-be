# Stage 1: Build & Publish
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Sao chép các file dự án để tận dụng layer caching của Docker khi restore
COPY ["src/Mms.Domain/Mms.Domain.csproj", "src/Mms.Domain/"]
COPY ["src/Mms.Application/Mms.Application.csproj", "src/Mms.Application/"]
COPY ["src/Mms.Infrastructure/Mms.Infrastructure.csproj", "src/Mms.Infrastructure/"]
COPY ["src/Mms.Api/Mms.Api.csproj", "src/Mms.Api/"]

RUN dotnet restore "src/Mms.Api/Mms.Api.csproj"

# Sao chép toàn bộ mã nguồn và biên dịch
COPY src/ src/
RUN dotnet publish "src/Mms.Api/Mms.Api.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Thiết lập user non-root để tăng cường bảo mật (tùy chọn với .NET 8/9 chiseled/app user)
USER $APP_UID

COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "Mms.Api.dll"]
