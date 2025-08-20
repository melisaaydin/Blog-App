<img width="400" height="280" alt="image" src="https://github.com/user-attachments/assets/5914ee3a-3054-4dca-b918-ce182dcd3c3f" />
<img width="400" height="250" alt="image" src="https://github.com/user-attachments/assets/640a9755-7918-4c11-a3c7-d8b0d33415e7" />
<img width="400" height="250" alt="image" src="https://github.com/user-attachments/assets/f543bfc5-3eea-482d-97b3-c46c8a4a3d92" />
<img width="400" height="250" alt="image" src="https://github.com/user-attachments/assets/170d825b-9dcb-468f-9d09-52fa3dc92582" />
<img width="400" height="250" alt="image" src="https://github.com/user-attachments/assets/8a96e060-390c-466d-a6b8-490a4ad5746d" />
<img width="400" height="250" alt="image" src="https://github.com/user-attachments/assets/73eb1cff-5d13-4957-aa33-3936787e16bc" />

BlogApp
📋 Proje Hakkında

BlogApp, kullanıcıların makaleler oluşturup yayımlayabileceği, diğer kullanıcılarla etkileşimde bulunabileceği ve içeriklerini yönetebileceği tam özellikli bir blog platformudur. Uygulama, güçlü bir kullanıcı kimlik doğrulama, yetkilendirme ve yönetim sistemiyle birlikte gelir.





✨ Temel Özellikler

Kullanıcı Yönetimi: Kullanıcılar, e-posta onayıyla hesap oluşturabilir, giriş yapabilir, şifrelerini sıfırlayabilir ve profillerini düzenleyebilirler. Profil resmi yükleme ve değiştirme gibi işlevler de mevcuttur.





Makale (Post) İşlemleri:

Kullanıcılar yeni makaleler oluşturabilir, düzenleyebilir ve silebilir.



Makaleler, başlık, açıklama, URL ve içerik gibi alanları içeren bir model kullanır.



Makaleler, ilgili oldukları konuları belirtmek için etiketlerle ilişkilendirilebilir.


Makale sayfaları, yazar bilgisi, yayınlanma tarihi ve görüntülenme sayısı gibi meta verileri gösterir.

Gelişmiş bir metin editörü (Quill.js) ile içerik girişi yapılır.


Sosyal Etkileşimler:


Yorumlar: Kullanıcılar makalelere yorum yapabilir ve yorumlara yanıt verebilir. Yorumlar AJAX kullanılarak dinamik olarak eklenir.





Beğeniler: Kullanıcılar makaleleri beğenip beğenilerini geri çekebilir.




Takip Etme: Kullanıcılar diğer kullanıcıları takip edebilir veya takibi bırakabilir. Takip bilgileri kullanıcının profil sayfasında gösterilir.


Özel Mesajlaşma: Karşılıklı takipte olan kullanıcılar birbirleriyle özel olarak mesajlaşabilir.



Koleksiyonlar:

Makalelerden özel koleksiyonlar oluşturulabilir.

Koleksiyonlar herkese açık veya gizli olabilir.



Bildirim Sistemi: Yeni bir yorum, beğeni veya takip gibi etkileşimler için kullanıcılara bildirimler gönderilir. Okunmamış bildirim sayısı ve detayları görüntülenebilir.



Yönetim Paneli:

Sadece 

Admin rolüne sahip kullanıcılar için özel bir yönetim paneli mevcuttur.




Adminler, tüm kullanıcıları listeleyebilir ve rollerini düzenleyebilir.





Adminler, makalelerin aktiflik durumunu yönetebilir ve bir makaleyi pasif durumdan aktif duruma getirebilir.




⚙️ Uygulama Yapısı ve Teknolojiler
Bu uygulama, ASP.NET Core MVC çatısı kullanılarak geliştirilmiştir.

Kullanılan Teknolojiler

Backend: C#, ASP.NET Core MVC, Entity Framework Core.




Veritabanı: SQLite.


Kimlik Yönetimi: ASP.NET Core Identity.


Önyüz: HTML, CSS, Bootstrap, JavaScript, jQuery.





Ek Kütüphaneler:


Quill.js: Makale içeriği için zengin metin editörü.



SweetAlert2: Kullanıcı etkileşimleri için özelleştirilebilir uyarı pencereleri.



Toastr: Geçici bildirim mesajları (toast) için kullanılır.



Bootstrap Icons: Çeşitli ikonlar için kullanılır.



Temalar: Uygulama, CSS değişkenleri kullanılarak açık ve koyu tema arasında geçiş yapma desteğine sahiptir.



Proje Yapısı
Controllers/: Uygulamanın iş mantığını ve HTTP isteklerini yöneten denetleyici sınıflarını içerir (AdminController.cs, PostController.cs, UsersController.cs, MessageController.cs, CollectionController.cs, NotificationsController.cs).

Data/: Veri erişim katmanıdır ve Entity Framework Core ile veritabanı işlemlerini yönetir (BlogContext.cs, EfPostRepository.cs gibi).

Entity/: Veritabanı tablolarını temsil eden sınıfları içerir (Post.cs, User.cs, Comment.cs, Collection.cs, Message.cs).


Models/: Görünümlerin ihtiyaç duyduğu verileri taşımak için kullanılan ViewModel sınıflarıdır.


ViewComponents/: Dinamik ve tekrar kullanılabilir UI bileşenlerini içerir.


Views/: Uygulamanın HTML görünümleri (.cshtml dosyaları) bu klasörde bulunur.



🚀 Kurulum ve Çalıştırma
Gereksinimler:

.NET 6 SDK veya üzeri.

SQLite (Entity Framework Core ile otomatik olarak yönetilir).

Tercihen Visual Studio veya Visual Studio Code.

Veritabanı Ayarları:


appsettings.json dosyasında DefaultConnection bağlantı dizginin SQLite için doğru şekilde ayarlandığından emin olun.

Veritabanı şemasını oluşturmak ve başlangıç verilerini yüklemek için komut satırından dotnet ef database update komutunu çalıştırın. Uygulama, SeedData.cs sınıfı aracılığıyla test verilerini otomatik olarak dolduracaktır.

Uygulamayı Çalıştırma:

Projeyi Visual Studio'da açın ve 

F5 tuşuna basın veya projenin kök dizininde dotnet run komutunu çalıştırın.


