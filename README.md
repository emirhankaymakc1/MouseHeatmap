# 🖱️ Mouse Heatmap Recorder

> Mouse hareketlerinizi arka planda kaydedin; ısı haritaları, kullanım istatistikleri ve yapay zeka destekli içgörülerle alışkanlıklarınızı keşfedin.

![C#](https://img.shields.io/badge/C%23-.NET%208-512BD4?logo=csharp&logoColor=white)
![Avalonia](https://img.shields.io/badge/UI-Avalonia%2011-9B59B6?logo=avalonia&logoColor=white)
![Platform](https://img.shields.io/badge/Platform-Windows-0078d4?logo=windows&logoColor=white)
![License](https://img.shields.io/badge/License-MIT-green)

**Mouse Heatmap Recorder**, bilgisayar kullanımınız sırasında mouse hareketlerini, tıklamaları ve scroll olaylarını düşük kaynak tüketimiyle kaydeden; bu verilerden **ısı haritaları**, **detaylı istatistikler** ve **yapay zeka destekli davranış analizi** üreten açık kaynak bir masaüstü uygulamasıdır.

---

## 🔒 Gizlilik Taahhüdü

Bu proje **tamamen analiz ve görselleştirme amaçlıdır**:

- ⛔ **Klavye KESİNLİKLE izlenmez** — keylogger yoktur, klavye hook'u (`WH_KEYBOARD_LL`) kod tabanında hiç kurulmaz.
- ✅ Yalnızca mouse koordinatları, tıklama ve scroll olayları kaydedilir (`WH_MOUSE_LL`).
- 💻 Tüm veriler **yalnızca kendi bilgisayarınızda** (yerel SQLite dosyasında) saklanır.
- 🌐 Hiçbir veri internete gönderilmez; uygulama ağ bağlantısı kurmaz.
- 🤖 Yapay zeka analizi tamamen **yerelde**, mouse dinamiklerinden çalışır — hangi uygulamanın açık olduğuna asla bakılmaz.

---

## ✨ Özellikler

### Temel
- 🔥 **Profesyonel ısı haritası** — mavi → yeşil → sarı → turuncu → kırmızı renk geçişi, logaritmik yoğunluk ölçeği
- 📊 **Dashboard** — bugünkü/haftalık kullanım, tıklama, mesafe, CPU/RAM göstergeleri
- 🔴 **Canlı kayıt akışı** — olayları gerçek zamanlı izleyin
- 📈 **İstatistikler** — saatlik / günlük / haftalık yoğunluk grafikleri, en yoğun saat/gün/bölge
- 📤 **Rapor dışa aktarma** — PNG, PDF, CSV, JSON
- 🧠 **Akıllı kayıt filtresi** — her piksel değil, anlamlı hareketler kaydedilir (≥8 px veya ≥50 ms)
- 💾 **Çökmeye dayanıklı** — SQLite WAL modu + kapanışta kanal boşaltma sayesinde veri kaybı yaşanmaz
- 🚀 **Otomatik başlatma** — Windows açılışında isteğe bağlı çalışır
- 🖥️ **Sistem tepsisi desteği** — pencere kapatılınca kayıt arka planda sürer

### Gelişmiş (bonus)
- 🖥️🖥️ **Çoklu monitör desteği** — Win32 `EnumDisplayMonitors` ile tüm ekranlar sanal masaüstü koordinatlarında ayrı ayrı izlenir
- ⚡ **Gerçek zamanlı heatmap** — canlı gelen olaylardan sönümlü ızgara ile ~4 FPS akan ısı haritası
- 🎮💼 **Oyun / Çalışma modu analizi** — 60 saniyelik pencerelerde k-means kümeleme ile davranış sınıflandırması (uygulama adına bakılmaz, yalnızca mouse dinamikleri)
- 🚀 **Mouse hız analizi** — ortalama/medyan/tepe hız, saatlik hız trendi, histogram
- 🎯 **Verimlilik skoru (0-100)** — aktiflik oranı, çalışma oranı, hareket verimliliği ve tutarlılığın ağırlıklı bileşimi
- 🤖 **Yapay zeka içgörüleri** — istatistik, hız, mod ve skor verilerinden doğal dilde (Türkçe) kişiselleştirilmiş bulgular

---

## 📸 Ekran Görüntüleri

> Ekran görüntüleri `assets/` klasörüne eklenecektir.

| Dashboard | Heatmap | Yapay Zeka Analizi |
|---|---|---|
| *yakında* | *yakında* | *yakında* |

---

## 🚀 Kurulum

### Gereksinimler

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) veya üzeri
- Windows 10/11 (mouse hook ve kayıt defteri erişimi Windows'a özeldir)

### Adımlar

```bash
# Depoyu klonlayın
git clone https://github.com/emirhankaymakc1/MouseHeatmap.git
cd MouseHeatmap

# Bağımlılıkları geri yükleyin ve derleyin
dotnet build MouseHeatmap.sln

# Uygulamayı çalıştırın
dotnet run --project src/MouseHeatmap.App/MouseHeatmap.App.csproj
```

Yayınlamak (tek dosya, bağımsız çalıştırılabilir) için:

```bash
dotnet publish src/MouseHeatmap.App/MouseHeatmap.App.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```

---

## 📖 Kullanım

1. **Kayıt** — Uygulama açılır açılmaz kayda başlar (Ayarlar'dan değiştirilebilir). Dashboard'daki düğmeyle istediğiniz an durdurup başlatabilirsiniz.
2. **Arka plan** — Pencereyi kapattığınızda uygulama sistem tepsisine küçülür ve kayda devam eder. Tamamen çıkmak için tepsi menüsündeki **Çıkış**'ı kullanın.
3. **Heatmap** — Dönem, monitör ve veri türünü seçip **Heatmap Oluştur**'a basın; isterseniz **Gerçek Zamanlı** anahtarını açarak canlı ısı haritasını izleyin.
4. **Yapay Zeka Analizi** — Dönem seçip **Analiz Et**'e basın: verimlilik skorunuzu, oyun/çalışma modu dağılımınızı, mouse hızınızı ve kişiselleştirilmiş içgörülerinizi görün.
5. **Raporlar** — İstatistikler sekmesinden CSV / JSON / PDF raporları `reports/` klasörüne aktarın.

---

## 🏗️ Mimari

```
Win32 WH_MOUSE_LL hook (kendi mesaj pompası thread'i)
        │
        ▼
  Akıllı Filtre  ──  ≥8 px hareket VEYA ≥50 ms geçtiyse kabul
        │
        ▼
  System.Threading.Channels (sınırsız kanal)
        │
        ▼
  EventWriter (arka plan Task)  ──  toplu INSERT, 2 sn'de bir flush
        │
        ▼
   SQLite (WAL modu)  ──  çökmeye dayanıklı yerel depolama
        │
        ▼
  Analytics katmanı  ──  İstatistik · Hız · Mod sınıflandırma (k-means) · Verimlilik skoru · İçgörü motoru
```

### Proje Yapısı

```
MouseHeatmap/
├── MouseHeatmap.sln
├── src/
│   ├── MouseHeatmap.Core/            # Platform bağımsız iş mantığı (class library)
│   │   ├── Models/                   # MouseEvent, MonitorInfo, analiz modelleri
│   │   ├── Data/                     # SQLite yönetimi, repository, arka plan yazıcı
│   │   ├── Tracking/                 # Win32 mouse hook, akıllı filtre, monitör algılama
│   │   ├── Analytics/                # İstatistik, hız analizi, mod sınıflandırma,
│   │   │                             # verimlilik skoru, yapay zeka içgörü motoru, rapor
│   │   └── Heatmap/                  # SkiaSharp tabanlı ısı haritası motoru
│   └── MouseHeatmap.App/             # Avalonia UI masaüstü uygulaması (MVVM)
│       ├── ViewModels/                # CommunityToolkit.Mvvm ile sekme görünüm modelleri
│       ├── Views/                     # AXAML arayüzleri
│       └── Services/                  # DI kökü, otomatik başlatma, sistem kaynakları
├── data/                              # SQLite veritabanı (git'e girmez)
└── reports/                           # Üretilen raporlar (git'e girmez)
```

### Tasarım Kararları

| Karar | Neden |
|---|---|
| `MouseHeatmap.Core` / `MouseHeatmap.App` ayrımı | İş mantığı UI'dan bağımsız; ileride farklı arayüzlerle (CLI, web) yeniden kullanılabilir |
| `System.Threading.Channels` | Hook thread'i hiçbir zaman disk I/O beklemez; üretici-tüketici deseni güvenli ve hızlı |
| Toplu yazma (200'lük batch, 2 sn flush) | Tek tek INSERT yerine toplu yazma → düşük CPU/disk |
| SQLite WAL modu | Çökme anında bile veri bütünlüğü korunur |
| Sanal masaüstü koordinatları | Çoklu monitörde negatif/geniş koordinatlar doğru işlenir |
| k-means + kural tabanlı geri düşüş | Az veri varken kümeleme güvenilmez; kural tabanlı sınıflandırmaya geri düşülür |
| SkiaSharp (matplotlib yerine) | .NET native, hızlı, hem statik hem gerçek zamanlı çizim için uygun |
| CommunityToolkit.Mvvm | Kod tekrarını azaltan kaynak üretici tabanlı MVVM, gözlemlenebilir property'ler |

---

## 🛠️ Teknolojiler

| Teknoloji | Kullanım Amacı |
|---|---|
| [.NET 8](https://dotnet.microsoft.com/) | Çalışma zamanı ve dil (C# 12) |
| [Avalonia UI](https://avaloniaui.net/) | Modern, XAML tabanlı masaüstü arayüzü |
| [SkiaSharp](https://github.com/mono/SkiaSharp) | Isı haritası çizimi ve PDF rapor üretimi |
| [Microsoft.Data.Sqlite](https://learn.microsoft.com/dotnet/standard/data/sqlite/) | Yerel, sıfır kurulum veritabanı |
| [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) | MVVM altyapısı (ObservableObject, RelayCommand) |
| Win32 API (`user32.dll`) | Düşük seviyeli mouse hook, çoklu monitör algılama, DPI farkındalığı |

---

## 🗺️ Yol Haritası

- [ ] Linux/macOS için alternatif mouse dinleme katmanı
- [ ] Isı haritasını canlı ekran görüntüsü üzerine bindirme
- [ ] Haftalık otomatik e-posta/rapor özeti
- [ ] Çoklu kullanıcı profili desteği

---

## 🤝 Katkıda Bulunma

Katkılarınızı bekliyoruz! Lütfen bir *issue* açın veya *pull request* gönderin.

## 📄 Lisans

Bu proje [MIT Lisansı](LICENSE) ile lisanslanmıştır.

## 👤 Author

Built by **Emirhan Kaymakçıoğlu**

🔗 Follow me on [LinkedIn](https://www.linkedin.com/in/emirhankaymakc1/) for more builds like this.

🌐 [turquartz.com](https://turquartz.com)

