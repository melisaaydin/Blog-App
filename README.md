
<img width="250" height="300" alt="Ekran görüntüsü 2025-09-10 174857" src="https://github.com/user-attachments/assets/4dac16d5-311e-43ee-bf2f-75b32069789b" />
<img width="250" height="300" alt="Ekran görüntüsü 2025-09-10 181306" src="https://github.com/user-attachments/assets/04e0fd26-4be5-4859-b42e-1950e488800a" />
<img width="250" height="300" alt="Ekran görüntüsü 2025-09-10 181315" src="https://github.com/user-attachments/assets/f5b51e93-f0be-4ffe-88d3-7005d3567ffd" />
<img width="250" height="300" alt="Ekran görüntüsü 2025-09-10 181353" src="https://github.com/user-attachments/assets/1e4a0f19-d86f-4d4c-9732-bebd847a2f75" />
<img width="250" height="300" alt="Ekran görüntüsü 2025-09-10 181537" src="https://github.com/user-attachments/assets/092872a4-b573-41af-b6d4-6dd9c3fcc167" />
<img width="250" height="300" alt="Ekran görüntüsü 2025-09-10 181601" src="https://github.com/user-attachments/assets/719c5769-3536-4b5c-b3b6-a76eb8769c57" />
<img width="250" height="300" alt="Ekran görüntüsü 2025-09-10 181700" src="https://github.com/user-attachments/assets/6ad1f23f-9b4c-4501-b817-ebff40d2c70b" />
<img width="250" height="300" alt="Ekran görüntüsü 2025-09-10 181854" src="https://github.com/user-attachments/assets/77895fd4-7655-4c49-8c27-0c17b1f223f2" />



📘 BlogApp
📋 Proje Hakkında

BlogApp, kullanıcıların makale oluşturup yayımlayabileceği, diğer kullanıcılarla etkileşim kurabileceği ve içeriklerini yönetebileceği tam özellikli bir blog platformudur.
Uygulama; güçlü kullanıcı kimlik doğrulama, yetkilendirme ve yönetim sistemine sahiptir. Modern arayüz, gelişmiş içerik yönetimi ve sosyal etkileşim özellikleriyle kullanıcı dostu bir deneyim sunar.

✨ Temel Özellikler

👤 Kullanıcı Yönetimi

E-posta onayıyla hesap oluşturma ve giriş yapma

Şifre sıfırlama ve profil düzenleme

Profil resmi yükleme ve değiştirme

📝 Makale (Post) İşlemleri

Yeni makale oluşturma, düzenleme ve silme

Makale modeli: Başlık, Açıklama, URL, İçerik

Etiket desteği ile kategorilendirme

Meta veriler: yazar bilgisi, tarih, görüntülenme sayısı

Gelişmiş zengin metin editörü (Quill.js) ile içerik girişi

💬 Sosyal Etkileşimler

Yorumlar: Makalelere yorum yapma ve yanıtlama (AJAX ile dinamik ekleme)

Beğeniler: Makaleleri beğenme ve beğeniyi geri çekme

Takip: Kullanıcıları takip etme / takibi bırakma

Özel Mesajlaşma: Karşılıklı takipte olan kullanıcılar arasında

📂 Koleksiyonlar

Makalelerden özel koleksiyonlar oluşturma

Koleksiyonları herkese açık veya gizli yapabilme

🔔 Bildirim Sistemi

Yorum, beğeni, takip gibi etkileşimler için bildirimler

Okunmamış bildirim sayısı ve detaylı listeleme

🛠️ Yönetim Paneli

Yalnızca admin kullanıcılar erişebilir

Kullanıcı listeleme ve rol düzenleme

Makale aktiflik durumunu yönetme

⚙️ Uygulama Yapısı ve Teknolojiler
Kullanılan Teknolojiler

Backend: C#, ASP.NET Core MVC, Entity Framework Core

Veritabanı: SQLite

Kimlik Yönetimi: ASP.NET Core Identity

Önyüz: HTML, CSS, Bootstrap, JavaScript, jQuery

Ek Kütüphaneler

Quill.js: Zengin metin editörü

SweetAlert2: Özelleştirilebilir uyarılar

Toastr: Geçici bildirim mesajları

Bootstrap Icons: İkon seti

Tema Desteği: Açık & Koyu tema

📂 Proje Yapısı
BlogApp/
│
├── Controllers/         # Denetleyiciler (AdminController, PostController, UsersController, ...)
├── Data/                # Veri erişim katmanı (BlogContext, EF Repository sınıfları)
├── Entity/              # Veritabanı tabloları (Post, User, Comment, Collection, Message)
├── Models/              # ViewModel sınıfları
├── ViewComponents/      # Tekrar kullanılabilir UI bileşenleri
├── Views/               # Razor View dosyaları (.cshtml)
└── wwwroot/             # Statik dosyalar (CSS, JS, görseller)

🚀 Kurulum ve Çalıştırma
Gereksinimler

.NET 6 SDK veya üzeri

SQLite (EF Core ile otomatik yönetilir)

Visual Studio / Visual Studio Code

Adımlar

Repoyu klonlayın:

git clone https://github.com/melisaaydin/Blog-App.git
cd BlogApp


Bağımlılıkları yükleyin:

dotnet restore


Veritabanını oluşturun:

dotnet ef database update


Uygulamayı çalıştırın:

dotnet run


veya Visual Studio’da F5 ile çalıştırabilirsiniz.
