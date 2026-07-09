using MouseHeatmap.Core.Models;

namespace MouseHeatmap.Core.Analytics;

public static class InsightsEngine
{
    public static List<Insight> Generate(
        UsageSummary summary,
        SpeedStats speed,
        IReadOnlyList<ActivityWindow> windows,
        ProductivityScore score)
    {
        var insights = new List<Insight>();
        if (summary.TotalEvents == 0)
        {
            insights.Add(new Insight("💤", "Henüz veri yok",
                "Analiz için biraz kayıt biriktirin; birkaç saatlik kullanım yeterli."));
            return insights;
        }

        AddPeakHourInsight(insights, summary);
        AddModeInsights(insights, windows);
        AddSpeedInsights(insights, speed, windows);
        AddEfficiencyInsight(insights, score);
        AddClickPatternInsight(insights, summary);
        AddDistanceInsight(insights, summary);

        return insights;
    }

    private static void AddPeakHourInsight(List<Insight> list, UsageSummary s)
    {
        if (s.BusiestHour is not int hour) return;

        var period = hour switch
        {
            >= 5 and < 12 => "sabah saatlerinde",
            >= 12 and < 17 => "öğleden sonra",
            >= 17 and < 22 => "akşam saatlerinde",
            _ => "gece geç saatlerde"
        };
        list.Add(new Insight("⏰", $"En yoğun saatiniz {hour}:00",
            $"Bilgisayar etkinliğinizin zirvesi {period}. " +
            "Önemli işleri bu aralığa planlamak verimliliğinizi artırabilir."));

        if (hour is >= 0 and < 5)
            list.Add(new Insight("🌙", "Gece kullanımı tespit edildi",
                "Gece 00:00-05:00 arası belirgin etkinlik var. " +
                "Uyku düzeninize dikkat etmek isteyebilirsiniz."));
    }

    private static void AddModeInsights(List<Insight> list, IReadOnlyList<ActivityWindow> windows)
    {
        if (windows.Count == 0) return;

        var work = windows.Count(w => w.Mode == ActivityMode.Work);
        var gaming = windows.Count(w => w.Mode == ActivityMode.Gaming);
        var active = work + gaming;
        if (active == 0) return;

        var gamingRatio = (double)gaming / active;
        if (gamingRatio > 0.5)
            list.Add(new Insight("🎮", "Oyun ağırlıklı kullanım",
                $"Aktif sürenizin %{gamingRatio * 100:F0} kadarı oyun benzeri yoğun " +
                "davranış deseninde (yüksek hız + sık tıklama)."));
        else if (gamingRatio > 0.15)
            list.Add(new Insight("⚖️", "Dengeli kullanım profili",
                $"Çalışma %{(1 - gamingRatio) * 100:F0}, oyun benzeri etkinlik " +
                $"%{gamingRatio * 100:F0}. Sağlıklı bir denge görünüyor."));
        else
            list.Add(new Insight("💼", "Çalışma odaklı kullanım",
                $"Aktif sürenizin %{(1 - gamingRatio) * 100:F0} kadarı düzenli " +
                "çalışma deseninde geçiyor."));
    }

    private static void AddSpeedInsights(
        List<Insight> list, SpeedStats speed, IReadOnlyList<ActivityWindow> windows)
    {
        if (speed.AvgSpeedPxPerSec <= 0) return;

        var description = speed.AvgSpeedPxPerSec switch
        {
            < 250 => "sakin ve kontrollü",
            < 600 => "ortalama tempoda",
            < 1100 => "hızlı ve enerjik",
            _ => "çok yüksek tempolu"
        };
        list.Add(new Insight("🚀", $"Mouse temponuz: {description}",
            $"Ortalama hızınız {speed.AvgSpeedPxPerSec:F0} px/sn, " +
            $"tepe hızınız {speed.MaxSpeedPxPerSec:F0} px/sn."));

        var gamingSpeeds = windows.Where(w => w.Mode == ActivityMode.Gaming)
            .Select(w => w.AvgSpeedPxPerSec).Where(v => v > 0).ToList();
        var workSpeeds = windows.Where(w => w.Mode == ActivityMode.Work)
            .Select(w => w.AvgSpeedPxPerSec).Where(v => v > 0).ToList();
        if (gamingSpeeds.Count > 0 && workSpeeds.Count > 0)
        {
            var ratio = gamingSpeeds.Average() / Math.Max(workSpeeds.Average(), 1);
            if (ratio > 1.3)
                list.Add(new Insight("📊", "Modlar arası hız farkı",
                    $"Oyun modunda mouse hızınız çalışma modunun {ratio:F1} katı. " +
                    "Refleksleriniz oyunda belirgin şekilde devreye giriyor."));
        }
    }

    private static void AddEfficiencyInsight(List<Insight> list, ProductivityScore score)
    {
        if (score.Total <= 0) return;

        if (score.MovementEfficiency < 40)
            list.Add(new Insight("🧭", "Hareket verimliliği geliştirilebilir",
                $"Mouse yolunuzun yalnızca %{score.MovementEfficiency:F0} kadarı hedefe " +
                "doğrudan gidiyor. İmleç hassasiyetini artırmak veya kısayol tuşları " +
                "kullanmak yolu kısaltabilir."));
        else if (score.MovementEfficiency > 70)
            list.Add(new Insight("🎯", "Keskin nişancı hareketleri",
                $"%{score.MovementEfficiency:F0} hareket verimliliği: imleciniz hedefe " +
                "neredeyse dümdüz gidiyor. Etkileyici!"));

        if (score.Consistency > 75)
            list.Add(new Insight("📈", "İstikrarlı çalışma ritmi",
                "Etkinliğiniz dakikadan dakikaya dengeli dağılıyor; " +
                "odaklı çalışma göstergesi."));
    }

    private static void AddClickPatternInsight(List<Insight> list, UsageSummary s)
    {
        if (s.TotalClicks < 20) return;

        var rightRatio = (double)s.RightClicks / s.TotalClicks;
        if (rightRatio > 0.30)
            list.Add(new Insight("🖱️", "Yoğun sağ tık kullanımı",
                $"Tıklamalarınızın %{rightRatio * 100:F0} kadarı sağ tık. Bağlam menülerini " +
                "sık kullanıyorsunuz; klavye kısayolları zaman kazandırabilir."));

        if (s.ActiveTimeSec > 0)
        {
            var clicksPerMinute = s.TotalClicks / (s.ActiveTimeSec / 60);
            if (clicksPerMinute > 30)
                list.Add(new Insight("⚡", "Yüksek tıklama temposu",
                    $"Dakikada ortalama {clicksPerMinute:F0} tıklama yapıyorsunuz. " +
                    "El sağlığınız için düzenli molalar önerilir."));
        }
    }

    private static void AddDistanceInsight(List<Insight> list, UsageSummary s)
    {
        var meters = s.TotalDistanceMeters;
        if (meters < 10) return;

        var comparison = meters switch
        {
            < 100 => $"yaklaşık {meters / 25:F0} basketbol sahası boyu",
            < 1000 => $"yaklaşık {meters / 105:F1} futbol sahası boyu",
            _ => $"yaklaşık {meters / 1000:F1} kilometre — neredeyse bir koşu parkuru"
        };
        list.Add(new Insight("📏", $"Mouse'unuz {meters:F0} metre yol katetti",
            $"Bu, {comparison}!"));
    }
}
