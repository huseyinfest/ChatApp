# Base image'den .NET SDK'yı kullanıyoruz.
# Bu, uygulamayı yayınlamak için gerekli araçları içerir.
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

# Dockerfile içindeki çalışma dizinini belirle
WORKDIR /source

# Proje dosyasını kopyala ve bağımlılıkları yükle
COPY *.csproj .
RUN dotnet restore

# Tüm proje dosyalarını kopyala
COPY . .

# Uygulamayı "Release" modunda derle ve yayınla
RUN dotnet publish -c Release -o /app

# Uygulamayı çalıştırmak için SDK imajını kullan
# Bu, "dotnet tool" gibi SDK komutlarının çalışmasına izin verir.
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS final

# Çalışma dizinini ayarla
WORKDIR /app

# Yayınlanan dosyaları bir önceki aşamadan kopyala
COPY --from=build /app .

# Giriş noktası (Entrypoint) komutlarını ayrı ayrı argümanlar olarak tanımla.
# Bu, komutları doğru şekilde çalıştırmanın en güvenilir yoludur.
# Önce veritabanı geçişlerini çalıştır, sonra uygulamayı başlat.
ENTRYPOINT ["/usr/bin/env", "bash", "-c", "dotnet ef database update --no-build --connection '$DATABASE_URL' && dotnet ChatApp.dll"]