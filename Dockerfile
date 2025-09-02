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

# Uygulamayı çalıştırmak için sadece runtime'ı içeren daha küçük bir imaj kullan
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final

# Çalışma dizinini ayarla
WORKDIR /app

# Yayınlanan dosyaları bir önceki aşamadan kopyala
COPY --from=build /app .

# Giriş noktası (Entrypoint) olarak bir shell script kullan
# Bu script önce veritabanı geçişlerini çalıştırır, sonra uygulamayı başlatır.
ENTRYPOINT ["/bin/bash", "-c", "dotnet ef database update --no-build --no-startup-hook --connection \"$DATABASE_URL\" && dotnet ChatApp.dll"]