using System.Globalization;
using System.Text;
using System.Text.Json;
using MouseHeatmap.Core.Models;
using SkiaSharp;

namespace MouseHeatmap.Core.Analytics;

public sealed class ReportExporter
{
    private readonly StatisticsService _statistics;
    private readonly string _reportsDir;

    public ReportExporter(StatisticsService statistics, string reportsDir)
    {
        _statistics = statistics;
        _reportsDir = reportsDir;
    }

    private string NewPath(string prefix, string extension)
    {
        Directory.CreateDirectory(_reportsDir);
        return Path.Combine(_reportsDir,
            $"{prefix}_{DateTime.Now:yyyyMMdd_HHmmss}.{extension}");
    }

    public string ExportCsv(double? startTs = null, double? endTs = null)
    {
        var events = _statistics.LoadEvents(startTs, endTs);
        var sb = new StringBuilder();
        sb.AppendLine("timestamp,x,y,event_type,button,scroll_x,scroll_y,monitor_index");
        foreach (var e in events)
        {
            sb.Append(e.Timestamp.ToString(CultureInfo.InvariantCulture)).Append(',')
              .Append(e.X).Append(',').Append(e.Y).Append(',')
              .Append(e.Type).Append(',').Append(e.Button).Append(',')
              .Append(e.ScrollX).Append(',').Append(e.ScrollY).Append(',')
              .Append(e.MonitorIndex).AppendLine();
        }
        var path = NewPath("mouse_verileri", "csv");
        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        return path;
    }

    public string ExportJson(double? startTs = null, double? endTs = null)
    {
        var s = _statistics.ComputeSummary(startTs, endTs);
        var payload = new
        {
            olusturulma = DateTime.Now.ToString("s"),
            toplam_olay = s.TotalEvents,
            toplam_hareket = s.TotalMoves,
            toplam_tiklama = s.TotalClicks,
            sol_tiklama = s.LeftClicks,
            sag_tiklama = s.RightClicks,
            orta_tiklama = s.MiddleClicks,
            toplam_scroll = s.TotalScrolls,
            toplam_mesafe_px = Math.Round(s.TotalDistancePx, 1),
            toplam_mesafe_m = Math.Round(s.TotalDistanceMeters, 2),
            aktif_sure_sn = Math.Round(s.ActiveTimeSec, 1),
            en_yogun_saat = s.BusiestHour,
            en_yogun_gun = s.BusiestDay,
            en_yogun_bolge = s.BusiestRegion,
            saatlik_dagilim = s.HourlyCounts
                .OrderBy(kv => kv.Key)
                .ToDictionary(kv => kv.Key.ToString(), kv => kv.Value)
        };
        var path = NewPath("ozet_rapor", "json");
        File.WriteAllText(path, JsonSerializer.Serialize(payload,
            new JsonSerializerOptions { WriteIndented = true }), Encoding.UTF8);
        return path;
    }

    public string ExportPdf(double? startTs = null, double? endTs = null)
    {
        var summary = _statistics.ComputeSummary(startTs, endTs);
        var path = NewPath("kullanim_raporu", "pdf");

        const float pageW = 595, pageH = 842, margin = 40;

        using var stream = File.OpenWrite(path);
        using var document = SKDocument.CreatePdf(stream);
        using var canvas = document.BeginPage(pageW, pageH);

        using var titleFont = new SKFont(SKTypeface.FromFamilyName("Segoe UI",
            SKFontStyle.Bold), 20);
        using var textFont = new SKFont(SKTypeface.FromFamilyName("Segoe UI"), 11);
        using var paint = new SKPaint { Color = SKColors.Black, IsAntialias = true };

        var y = margin + 10;
        canvas.DrawText("Mouse Heatmap Recorder — Kullanım Raporu",
            margin, y, titleFont, paint);
        y += 30;

        var lines = new[]
        {
            $"Oluşturulma: {DateTime.Now:dd.MM.yyyy HH:mm}",
            $"Toplam olay: {summary.TotalEvents:N0}",
            $"Aktif kullanım: {summary.ActiveTimeSec / 3600:F1} saat",
            $"Toplam mesafe: {summary.TotalDistanceMeters:F1} m",
            $"Tıklamalar: {summary.TotalClicks:N0} (Sol {summary.LeftClicks:N0} / " +
                $"Sağ {summary.RightClicks:N0} / Orta {summary.MiddleClicks:N0})",
            $"Scroll: {summary.TotalScrolls:N0}",
            $"En yoğun saat: {(summary.BusiestHour is int h ? $"{h}:00" : "-")}",
            $"En yoğun gün: {summary.BusiestDay ?? "-"}",
            $"En yoğun bölge: {summary.BusiestRegion ?? "-"}"
        };
        foreach (var line in lines)
        {
            canvas.DrawText(line, margin, y, textFont, paint);
            y += 18;
        }
        y += 20;

        var hourly = Enumerable.Range(0, 24)
            .Select(hour => ((double)summary.HourlyCounts.GetValueOrDefault(hour),
                hour.ToString()))
            .ToArray();
        DrawBarChart(canvas, "Saatlik Yoğunluk", hourly,
            margin, y, pageW - margin * 2, 180, new SKColor(0x1E, 0x90, 0xFF));
        y += 220;

        var weekday = Enumerable.Range(0, 7)
            .Select(d => ((double)summary.WeekdayCounts.GetValueOrDefault(d),
                StatisticsService.WeekdayName(d)[..3]))
            .ToArray();
        DrawBarChart(canvas, "Haftalık Yoğunluk", weekday,
            margin, y, pageW - margin * 2, 180, new SKColor(0xE6, 0x7E, 0x22));

        document.EndPage();
        document.Close();
        return path;
    }

    private static void DrawBarChart(
        SKCanvas canvas, string title, (double Value, string Label)[] data,
        float x, float y, float width, float height, SKColor barColor)
    {
        using var titleFont = new SKFont(SKTypeface.FromFamilyName("Segoe UI",
            SKFontStyle.Bold), 13);
        using var labelFont = new SKFont(SKTypeface.FromFamilyName("Segoe UI"), 8);
        using var paint = new SKPaint { IsAntialias = true };

        paint.Color = SKColors.Black;
        canvas.DrawText(title, x, y, titleFont, paint);
        y += 12;

        var max = data.Max(d => d.Value);
        if (max <= 0) max = 1;

        var chartH = height - 30;
        var barWidth = width / data.Length * 0.7f;
        var step = width / data.Length;

        for (var i = 0; i < data.Length; i++)
        {
            var barH = (float)(data[i].Value / max * chartH);
            var barX = x + i * step + (step - barWidth) / 2;

            paint.Color = barColor;
            canvas.DrawRect(barX, y + chartH - barH, barWidth, barH, paint);

            paint.Color = SKColors.DarkGray;
            canvas.DrawText(data[i].Label, barX, y + chartH + 12, labelFont, paint);
        }
    }
}
