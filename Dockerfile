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

# Uygulamayı çalıştırmak için daha küçük bir runtime imajı kullan
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final

# Çalışma dizinini ayarla
WORKDIR /app

# Yayınlanan dosyaları bir önceki aşamadan kopyala
COPY --from=build /app .

# Uygulamanın çalışacağı portu belirtir
EXPOSE 8080

# Uygulamayı çalıştırmak için giriş noktası
ENTRYPOINT ["dotnet", "ChatApp.dll"]