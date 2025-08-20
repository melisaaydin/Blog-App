<img width="400" height="280" alt="image" src="https://github.com/user-attachments/assets/5914ee3a-3054-4dca-b918-ce182dcd3c3f" />
<img width="400" height="250" alt="image" src="https://github.com/user-attachments/assets/640a9755-7918-4c11-a3c7-d8b0d33415e7" />
<img width="400" height="250" alt="image" src="https://github.com/user-attachments/assets/f543bfc5-3eea-482d-97b3-c46c8a4a3d92" />
<img width="400" height="250" alt="image" src="https://github.com/user-attachments/assets/170d825b-9dcb-468f-9d09-52fa3dc92582" />
<img width="400" height="250" alt="image" src="https://github.com/user-attachments/assets/8a96e060-390c-466d-a6b8-490a4ad5746d" />
<img width="400" height="250" alt="image" src="https://github.com/user-attachments/assets/73eb1cff-5d13-4957-aa33-3936787e16bc" />

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
