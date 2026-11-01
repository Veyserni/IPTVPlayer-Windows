# IPTV Player Windows

Windows 10/11 x64 için masaüstü IPTV oynatıcı.

## Özellikler

- Xtream Codes ile giriş
- Başarılı hesabı yerel olarak kaydetme ve hızlı giriş
- M3U Plus listesini indirme ve ayrıştırma
- Spor / Canlı Yayın / Filmler / Diziler / Tümü filtreleri
- beIN Sports, S Sport, Tivibu Spor, Tabii Spor, Exxen Spor, TRT Spor, A Spor ve Eurosport hızlı filtreleri
- Kanal arama
- LibVLC tabanlı dahili oynatıcı
- Çift tıklama veya Enter ile kanal açma
- Space ile oynat/duraklat
- F11 ile tam ekran

## GitHub üzerinden derleme

Bu repoda `.github/workflows/build-windows.yml` hazırdır. Dosyalar `main` branch'e yüklendiğinde GitHub Actions otomatik çalışır.

Build başarılı olduğunda:

1. GitHub'da **Actions** sekmesine girin.
2. Son yeşil **Build Windows App** çalışmasını açın.
3. Sayfanın altındaki **Artifacts** bölümünden `IPTVPlayer-Windows-x64` dosyasını indirin.
4. İndirilen ZIP'i çıkarın ve içindeki `IPTVPlayer.Windows.exe` dosyasını çalıştırın.

> Uygulama yalnızca kullanıcının erişim yetkisine sahip olduğu yayınlarla kullanılmalıdır.
