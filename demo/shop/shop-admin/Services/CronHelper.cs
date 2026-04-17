namespace ShopAdmin.Services;

/// <summary>
/// Chuyển đổi cron expression 5 trường (phút giờ ngày tháng thứ)
/// thành mô tả tiếng Việt dễ hiểu.
/// </summary>
public static class CronHelper
{
    private static readonly string[] DayNames =
        ["Chủ Nhật", "Thứ Hai", "Thứ Ba", "Thứ Tư", "Thứ Năm", "Thứ Sáu", "Thứ Bảy"];

    private static readonly string[] MonthNames =
        ["", "Tháng 1", "Tháng 2", "Tháng 3", "Tháng 4", "Tháng 5", "Tháng 6",
              "Tháng 7", "Tháng 8", "Tháng 9", "Tháng 10", "Tháng 11", "Tháng 12"];

    /// <summary>
    /// Trả về mô tả tiếng Việt. Nếu không parse được trả về chuỗi gốc.
    /// </summary>
    public static string Describe(string? cron)
    {
        if (string.IsNullOrWhiteSpace(cron)) return "(chưa đặt lịch)";

        var parts = cron.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 5) return cron;

        var (min, hour, dom, month, dow) = (parts[0], parts[1], parts[2], parts[3], parts[4]);

        // --- Mỗi phút ---
        if (min == "*" && hour == "*" && dom == "*" && month == "*" && dow == "*")
            return "Mỗi phút";

        // --- */N phút ---
        if (min.StartsWith("*/") && hour == "*" && dom == "*" && month == "*" && dow == "*")
        {
            if (int.TryParse(min[2..], out int n))
                return n == 1 ? "Mỗi phút" : $"Mỗi {n} phút";
        }

        // --- Mỗi giờ (0 * * * *) ---
        if (hour == "*" && dom == "*" && month == "*" && dow == "*" && IsFixed(min, out int atMin))
            return $"Mỗi giờ vào phút :{atMin:D2}";

        // --- */N giờ ---
        if (hour.StartsWith("*/") && dom == "*" && month == "*" && dow == "*" && IsFixed(min, out int m2))
        {
            if (int.TryParse(hour[2..], out int h))
                return h == 1 ? $"Mỗi giờ vào phút :{m2:D2}" : $"Mỗi {h} giờ lúc xx:{m2:D2}";
        }

        // --- Hàng ngày (0 6 * * *) ---
        if (dom == "*" && month == "*" && dow == "*"
            && IsFixed(min, out int dailyMin) && IsFixed(hour, out int dailyHour))
            return $"Mỗi ngày lúc {dailyHour:D2}:{dailyMin:D2}";

        // --- Các ngày trong tuần (0 8 * * 1-5) ---
        if (dom == "*" && month == "*" && dow == "1-5"
            && IsFixed(min, out int wdMin) && IsFixed(hour, out int wdHour))
            return $"Thứ Hai – Thứ Sáu lúc {wdHour:D2}:{wdMin:D2}";

        if (dom == "*" && month == "*" && dow == "1-7"
            && IsFixed(min, out int wdMin2) && IsFixed(hour, out int wdHour2))
            return $"Mỗi ngày lúc {wdHour2:D2}:{wdMin2:D2}";

        // --- Một ngày cụ thể trong tuần (0 8 * * 1) ---
        if (dom == "*" && month == "*" && IsFixed(dow, out int dayNum)
            && IsFixed(min, out int sdMin) && IsFixed(hour, out int sdHour)
            && dayNum >= 0 && dayNum <= 6)
            return $"{DayNames[dayNum]} hằng tuần lúc {sdHour:D2}:{sdMin:D2}";

        // --- Nhiều ngày trong tuần (0 8 * * 1,3,5) ---
        if (dom == "*" && month == "*" && dow.Contains(',')
            && IsFixed(min, out int mdMin) && IsFixed(hour, out int mdHour))
        {
            var days = ParseList(dow, 0, 6);
            if (days != null)
            {
                var dayLabels = string.Join(", ", days.Select(d => DayNames[d]));
                return $"{dayLabels} lúc {mdHour:D2}:{mdMin:D2}";
            }
        }

        // --- Ngày đầu tháng (0 0 1 * *) ---
        if (dom == "1" && month == "*" && dow == "*"
            && IsFixed(min, out int bomMin) && IsFixed(hour, out int bomHour))
            return $"Ngày đầu mỗi tháng lúc {bomHour:D2}:{bomMin:D2}";

        // --- Ngày cuối tháng (0 0 L * *) – not common but handle gracefully ---

        // --- Ngày cụ thể trong tháng (0 8 15 * *) ---
        if (month == "*" && dow == "*"
            && IsFixed(dom, out int dayOfMon) && IsFixed(min, out int domMin) && IsFixed(hour, out int domHour))
            return $"Ngày {dayOfMon} hằng tháng lúc {domHour:D2}:{domMin:D2}";

        // --- Tháng cụ thể (0 0 1 6 *) ---
        if (dow == "*" && IsFixed(month, out int fixMonth) && IsFixed(dom, out int fixDom)
            && IsFixed(min, out int ymMin) && IsFixed(hour, out int ymHour)
            && fixMonth >= 1 && fixMonth <= 12)
            return $"Ngày {fixDom} {MonthNames[fixMonth]} hằng năm lúc {ymHour:D2}:{ymMin:D2}";

        // --- Fallback: trả về bản dịch từng phần ---
        return BuildFallback(min, hour, dom, month, dow);
    }

    // ---- helpers ----

    private static bool IsFixed(string field, out int value)
    {
        value = 0;
        return field != "*" && !field.Contains('/') && !field.Contains(',') && !field.Contains('-')
               && int.TryParse(field, out value);
    }

    private static List<int>? ParseList(string field, int min, int max)
    {
        var parts = field.Split(',');
        var list = new List<int>();
        foreach (var p in parts)
        {
            if (!int.TryParse(p.Trim(), out int v) || v < min || v > max) return null;
            list.Add(v);
        }
        list.Sort();
        return list;
    }

    private static string BuildFallback(string min, string hour, string dom, string month, string dow)
    {
        var sb = new System.Text.StringBuilder();

        // time part
        if (IsFixed(hour, out int h) && IsFixed(min, out int m))
            sb.Append($"lúc {h:D2}:{m:D2}");
        else if (hour == "*" && IsFixed(min, out int m2))
            sb.Append($"phút :{m2:D2} mỗi giờ");
        else if (min.StartsWith("*/") && int.TryParse(min[2..], out int ev))
            sb.Append($"mỗi {ev} phút");

        // day-of-week part
        if (dow != "*")
        {
            if (dow.Contains('-') && dow.Split('-').Length == 2)
            {
                var r = dow.Split('-');
                if (int.TryParse(r[0], out int d1) && int.TryParse(r[1], out int d2) && d1 >= 0 && d2 <= 6)
                    sb.Insert(0, $"{DayNames[d1]}–{DayNames[d2]} ");
            }
        }

        // dom part
        if (dom != "*" && IsFixed(dom, out int d))
            sb.Insert(0, $"ngày {d} hằng tháng ");

        // month part
        if (month != "*" && IsFixed(month, out int mo) && mo >= 1 && mo <= 12)
            sb.Insert(0, $"{MonthNames[mo]} ");

        return sb.Length > 0 ? sb.ToString().Trim() : $"Lịch tùy chỉnh ({min} {hour} {dom} {month} {dow})";
    }
}
