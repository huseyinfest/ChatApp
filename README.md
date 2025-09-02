# ChatApp - Real-time Chat Application

Bu proje, ASP.NET Core ve SignalR kullanılarak geliştirilmiş gerçek zamanlı bir chat uygulamasıdır.

## Özellikler

- ✅ **Real-time Communication**: SignalR hub ile gerçek zamanlı mesajlaşma
- ✅ **User Authentication**: JWT token tabanlı kimlik doğrulama
- ✅ **User Registration**: E-posta ve kullanıcı adı ile üyelik sistemi
- ✅ **Real-time Messaging**: Anlık mesaj gönderimi ve alımı
- ✅ **User Status**: Çevrimiçi/çevrimdışı durumu
- ✅ **Unread Message Count**: Okunmamış mesaj sayısı
- ✅ **Typing Indicators**: Yazıyor göstergesi
- ✅ **Message Persistence**: Mesajların veritabanında saklanması
- ✅ **Modern UI**: Bootstrap ve FontAwesome ile modern arayüz

## Teknolojiler

- **Backend**: ASP.NET Core 9.0
- **Real-time Communication**: SignalR
- **Database**: Entity Framework Core + SQL Server
- **Authentication**: JWT Bearer Tokens
- **Frontend**: HTML5, CSS3, JavaScript (Vanilla)
- **UI Framework**: Bootstrap 5.3
- **Icons**: FontAwesome 6.0

## Kurulum

### Gereksinimler

- .NET 9.0 SDK
- SQL Server (LocalDB veya SQL Server Express)
- Git

### Adımlar

1. **Projeyi klonlayın**
   ```bash
   git clone <repository-url>
   cd ChatApp
   ```

2. **Bağımlılıkları yükleyin**
   ```bash
   dotnet restore
   ```

3. **Veritabanı bağlantı dizesini yapılandırın**
   `appsettings.json` dosyasında connection string'i güncelleyin:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=ChatAppDb;Trusted_Connection=true;MultipleActiveResultSets=true"
   }
   ```

4. **Veritabanını oluşturun**
   ```bash
   dotnet ef database update
   ```

5. **Uygulamayı çalıştırın**
   ```bash
   dotnet run
   ```

6. **Tarayıcıda açın**
   ```
   https://localhost:7000
   ```

## API Endpoints

### Authentication
- `POST /api/user/register` - Kullanıcı kaydı
- `POST /api/user/login` - Kullanıcı girişi
- `POST /api/user/logout` - Kullanıcı çıkışı
- `GET /api/user/profile` - Kullanıcı profili
- `GET /api/user/users` - Tüm kullanıcılar

### Messages
- `POST /api/message/send` - Mesaj gönderimi
- `GET /api/message/conversation/{userId}` - Sohbet geçmişi
- `POST /api/message/read/{messageId}` - Mesajı okundu olarak işaretle
- `GET /api/message/conversations` - Kullanıcı sohbetleri

### SignalR Hub
- `/chatHub` - Real-time communication hub

## Proje Yapısı

```
ChatApp/
├── Controllers/          # API Controllers
├── Data/                # Entity Framework Context
├── Hubs/                # SignalR Hubs
├── Models/              # Entity Models
│   └── DTOs/           # Data Transfer Objects
├── Services/            # Business Logic Services
├── wwwroot/             # Static Files
│   ├── js/             # JavaScript files
│   └── index.html      # Main HTML page
├── Program.cs           # Application entry point
└── appsettings.json    # Configuration
```

## Kullanım

1. **Kayıt Ol**: Yeni kullanıcı hesabı oluşturun
2. **Giriş Yap**: E-posta ve şifre ile giriş yapın
3. **Kullanıcı Seçin**: Sol panelden sohbet etmek istediğiniz kullanıcıyı seçin
4. **Mesaj Gönderin**: Sağ panelde mesajınızı yazın ve gönderin
5. **Real-time Chat**: SignalR ile gerçek zamanlı mesajlaşma yapın

## Özellikler Detayı

### Real-time Communication
- SignalR hub ile anlık mesaj gönderimi
- Kullanıcı çevrimiçi/çevrimdışı durumu
- Yazıyor göstergesi
- Mesaj okundu bildirimi

### User Management
- Benzersiz kullanıcı adı ve e-posta kontrolü
- Güvenli şifre hashleme (SHA256)
- JWT token tabanlı kimlik doğrulama
- Kullanıcı profil yönetimi

### Message System
- Kalıcı mesaj saklama
- Okunmamış mesaj sayısı
- Sohbet geçmişi
- Mesaj durumu takibi

## Güvenlik

- JWT token tabanlı kimlik doğrulama
- Şifre hashleme (SHA256)
- CORS yapılandırması
- API endpoint koruması

## Lisans

Bu proje MIT lisansı altında lisanslanmıştır.

## Katkıda Bulunma

1. Fork yapın
2. Feature branch oluşturun (`git checkout -b feature/AmazingFeature`)
3. Commit yapın (`git commit -m 'Add some AmazingFeature'`)
4. Push yapın (`git branch -u origin/feature/AmazingFeature`)
5. Pull Request oluşturun

## İletişim

Proje hakkında sorularınız için issue açabilir veya pull request gönderebilirsiniz.
