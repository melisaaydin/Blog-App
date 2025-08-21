<img width="2530" height="1242" alt="image" src="https://github.com/user-attachments/assets/b082bc13-67c7-4a0e-bec0-4d6921c66da6" />

<img width="2559" height="1225" alt="image" src="https://github.com/user-attachments/assets/18f0ae27-35f5-4bdf-867f-009a1c501906" />
<img width="2532" height="1225" alt="image" src="https://github.com/user-attachments/assets/3a39a089-f2e8-41b2-8f5d-7de69edb7000" />
<img width="2559" height="1233" alt="image" src="https://github.com/user-attachments/assets/157fa997-cffe-4064-a7dd-074e9780efdc" />
<img width="2559" height="1229" alt="image" src="https://github.com/user-attachments/assets/75bc3f52-6bf1-40ee-a7d6-e8f6d0764d2e" />
<img width="2529" height="1223" alt="image" src="https://github.com/user-attachments/assets/424c6512-13c7-496d-a66d-9699417c4eaa" />
<img width="2559" height="1235" alt="image" src="https://github.com/user-attachments/assets/d9382fcd-5f43-4cc3-b387-356b9f58b4ed" />



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
