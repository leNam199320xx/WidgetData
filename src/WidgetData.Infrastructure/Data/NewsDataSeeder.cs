using Microsoft.Data.Sqlite;

namespace WidgetData.Infrastructure.Data;

/// <summary>
/// Generates a SQLite news-portal database to simulate a Vietnamese online news site (VietNews).
/// Tables: categories, authors, articles, readers, article_views, comments
/// </summary>
public static class NewsDataSeeder
{
    private static readonly string[] FirstNames =
    [
        "Nguyễn", "Trần", "Lê", "Phạm", "Hoàng", "Huỳnh", "Phan", "Vũ", "Võ", "Đặng",
        "Bùi", "Đỗ", "Hồ", "Ngô", "Dương", "Lý", "Đinh", "Tô", "Mai", "Cao"
    ];
    private static readonly string[] MiddleNames =
    [
        "Văn", "Thị", "Hữu", "Quốc", "Minh", "Thanh", "Tuấn", "Anh", "Thu", "Hoài"
    ];
    private static readonly string[] LastNames =
    [
        "An", "Bình", "Cường", "Dũng", "Đức", "Giang", "Hoa", "Hùng", "Lan", "Long",
        "Mai", "Nam", "Ngọc", "Phúc", "Quân", "Sơn", "Thành", "Tùng", "Việt", "Xuân"
    ];
    private static readonly string[] Cities =
    [
        "Hà Nội", "TP. Hồ Chí Minh", "Đà Nẵng", "Cần Thơ", "Hải Phòng",
        "Biên Hòa", "Huế", "Nha Trang", "Vũng Tàu", "Quy Nhơn"
    ];

    public static void EnsureNewsDatabase(string dbPath)
    {
        if (File.Exists(dbPath)) return;

        using var conn = new SqliteConnection($"Data Source={dbPath}");
        conn.Open();

        CreateSchema(conn);
        SeedCategories(conn);
        SeedAuthors(conn);
        SeedArticles(conn);
        SeedReaders(conn);
        SeedArticleViews(conn);
        SeedComments(conn);
    }

    private static void CreateSchema(SqliteConnection conn)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
PRAGMA journal_mode=WAL;

CREATE TABLE IF NOT EXISTS categories (
    id            INTEGER PRIMARY KEY AUTOINCREMENT,
    name          TEXT NOT NULL,
    slug          TEXT NOT NULL UNIQUE,
    description   TEXT,
    color         TEXT NOT NULL DEFAULT '#3182ce',
    is_active     INTEGER NOT NULL DEFAULT 1,
    sort_order    INTEGER NOT NULL DEFAULT 0,
    article_count INTEGER NOT NULL DEFAULT 0,
    created_at    TEXT DEFAULT (datetime('now'))
);

CREATE TABLE IF NOT EXISTS authors (
    id             INTEGER PRIMARY KEY AUTOINCREMENT,
    full_name      TEXT NOT NULL,
    email          TEXT NOT NULL UNIQUE,
    bio            TEXT,
    avatar_url     TEXT,
    total_articles INTEGER NOT NULL DEFAULT 0,
    total_views    INTEGER NOT NULL DEFAULT 0,
    is_active      INTEGER NOT NULL DEFAULT 1,
    joined_at      TEXT DEFAULT (datetime('now'))
);

CREATE TABLE IF NOT EXISTS articles (
    id                  INTEGER PRIMARY KEY AUTOINCREMENT,
    category_id         INTEGER NOT NULL,
    author_id           INTEGER NOT NULL,
    title               TEXT NOT NULL,
    slug                TEXT NOT NULL UNIQUE,
    excerpt             TEXT,
    word_count          INTEGER NOT NULL DEFAULT 0,
    status              TEXT NOT NULL DEFAULT 'published',
    is_featured         INTEGER NOT NULL DEFAULT 0,
    view_count          INTEGER NOT NULL DEFAULT 0,
    comment_count       INTEGER NOT NULL DEFAULT 0,
    share_count         INTEGER NOT NULL DEFAULT 0,
    read_time_minutes   INTEGER NOT NULL DEFAULT 3,
    published_at        TEXT,
    created_at          TEXT DEFAULT (datetime('now')),
    updated_at          TEXT DEFAULT (datetime('now')),
    FOREIGN KEY (category_id) REFERENCES categories(id),
    FOREIGN KEY (author_id)   REFERENCES authors(id)
);

CREATE TABLE IF NOT EXISTS readers (
    id             INTEGER PRIMARY KEY AUTOINCREMENT,
    full_name      TEXT NOT NULL,
    email          TEXT NOT NULL UNIQUE,
    city           TEXT,
    is_subscribed  INTEGER NOT NULL DEFAULT 0,
    total_reads    INTEGER NOT NULL DEFAULT 0,
    total_comments INTEGER NOT NULL DEFAULT 0,
    registered_at  TEXT DEFAULT (datetime('now'))
);

CREATE TABLE IF NOT EXISTS article_views (
    id                    INTEGER PRIMARY KEY AUTOINCREMENT,
    article_id            INTEGER NOT NULL,
    reader_id             INTEGER,
    source                TEXT NOT NULL DEFAULT 'direct',
    device                TEXT NOT NULL DEFAULT 'desktop',
    country               TEXT NOT NULL DEFAULT 'VN',
    read_completion_percent REAL NOT NULL DEFAULT 0,
    viewed_at             TEXT DEFAULT (datetime('now')),
    FOREIGN KEY (article_id) REFERENCES articles(id),
    FOREIGN KEY (reader_id)  REFERENCES readers(id)
);

CREATE TABLE IF NOT EXISTS comments (
    id          INTEGER PRIMARY KEY AUTOINCREMENT,
    article_id  INTEGER NOT NULL,
    reader_id   INTEGER NOT NULL,
    content     TEXT NOT NULL,
    is_approved INTEGER NOT NULL DEFAULT 1,
    created_at  TEXT DEFAULT (datetime('now')),
    FOREIGN KEY (article_id) REFERENCES articles(id),
    FOREIGN KEY (reader_id)  REFERENCES readers(id)
);

CREATE INDEX IF NOT EXISTS idx_articles_category    ON articles(category_id);
CREATE INDEX IF NOT EXISTS idx_articles_published   ON articles(published_at);
CREATE INDEX IF NOT EXISTS idx_article_views_article ON article_views(article_id);
CREATE INDEX IF NOT EXISTS idx_article_views_viewed  ON article_views(viewed_at);
CREATE INDEX IF NOT EXISTS idx_comments_article     ON comments(article_id);
";
        cmd.ExecuteNonQuery();
    }

    private static void SeedCategories(SqliteConnection conn)
    {
        var categories = new[]
        {
            ("Công nghệ",     "cong-nghe",     "Tin tức về công nghệ, AI, phần mềm, thiết bị điện tử", "#3182ce", 1, 0),
            ("Kinh tế",       "kinh-te",       "Thị trường tài chính, bất động sản, doanh nghiệp",      "#38a169", 2, 0),
            ("Thể thao",      "the-thao",      "Bóng đá, quần vợt, SEA Games và các môn thể thao",      "#e53e3e", 3, 0),
            ("Giải trí",      "giai-tri",      "Âm nhạc, điện ảnh, nghệ thuật, người nổi tiếng",        "#d69e2e", 4, 0),
            ("Xã hội",        "xa-hoi",        "Tin tức xã hội, đời sống, cộng đồng",                   "#805ad5", 5, 0),
            ("Sức khỏe",      "suc-khoe",      "Y tế, dinh dưỡng, lối sống lành mạnh",                  "#dd6b20", 6, 0),
            ("Du lịch",       "du-lich",       "Điểm đến, kinh nghiệm, cẩm nang du lịch",               "#319795", 7, 0),
            ("Giáo dục",      "giao-duc",      "Tuyển sinh, học bổng, chính sách giáo dục",             "#2b6cb0", 8, 0),
        };

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO categories (name, slug, description, color, sort_order, article_count) VALUES (@n, @s, @d, @c, @o, @ac)";
        var pN  = cmd.Parameters.Add("@n",  SqliteType.Text);
        var pS  = cmd.Parameters.Add("@s",  SqliteType.Text);
        var pD  = cmd.Parameters.Add("@d",  SqliteType.Text);
        var pC  = cmd.Parameters.Add("@c",  SqliteType.Text);
        var pO  = cmd.Parameters.Add("@o",  SqliteType.Integer);
        var pAc = cmd.Parameters.Add("@ac", SqliteType.Integer);

        foreach (var (name, slug, desc, color, sort, ac) in categories)
        {
            pN.Value = name; pS.Value = slug; pD.Value = desc; pC.Value = color; pO.Value = sort; pAc.Value = ac;
            cmd.ExecuteNonQuery();
        }
    }

    private static void SeedAuthors(SqliteConnection conn)
    {
        var authors = new[]
        {
            ("Nguyễn Minh Tuấn",  "tuan.nguyen@vietnews.vn",  "Phóng viên công nghệ với 8 năm kinh nghiệm tại các tờ báo lớn.",          20000),
            ("Trần Thị Hoa",      "hoa.tran@vietnews.vn",     "Chuyên gia kinh tế, cựu giám đốc ngân hàng.",                             18000),
            ("Lê Văn Hùng",       "hung.le@vietnews.vn",      "Phóng viên thể thao, chuyên theo dõi bóng đá Việt Nam và quốc tế.",       25000),
            ("Phạm Thị Lan",      "lan.pham@vietnews.vn",     "Biên tập viên giải trí, chuyên về điện ảnh và âm nhạc.",                  15000),
            ("Hoàng Văn Đức",     "duc.hoang@vietnews.vn",    "Nhà báo xã hội, tập trung vào các vấn đề cộng đồng và phúc lợi.",         12000),
            ("Vũ Thị Mai",        "mai.vu@vietnews.vn",       "Bác sĩ kiêm nhà báo, chuyên về sức khỏe và y tế cộng đồng.",             14000),
            ("Đặng Quang Vinh",   "vinh.dang@vietnews.vn",    "Phóng viên du lịch, đã đặt chân đến 50+ quốc gia.",                      11000),
            ("Ngô Thị Phương",    "phuong.ngo@vietnews.vn",   "Nhà báo giáo dục, chuyên theo dõi chính sách tuyển sinh đại học.",       10000),
            ("Bùi Thanh Nam",     "nam.bui@vietnews.vn",      "Phóng viên đa lĩnh vực với 5 năm kinh nghiệm.",                          8000),
            ("Đinh Thị Ngọc",     "ngoc.dinh@vietnews.vn",    "Biên tập viên kiêm phóng viên chuyên mảng kinh doanh.",                   9000),
        };

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"INSERT INTO authors (full_name, email, bio, total_views, is_active, joined_at)
                            VALUES (@fn, @em, @bio, @tv, 1, datetime('now','-'||@days||' months'))";
        var fn   = cmd.Parameters.Add("@fn",   SqliteType.Text);
        var em   = cmd.Parameters.Add("@em",   SqliteType.Text);
        var bio  = cmd.Parameters.Add("@bio",  SqliteType.Text);
        var tv   = cmd.Parameters.Add("@tv",   SqliteType.Integer);
        var days = cmd.Parameters.Add("@days", SqliteType.Integer);

        int i = 0;
        foreach (var (name, email, bioText, totalViews) in authors)
        {
            fn.Value   = name;
            em.Value   = email;
            bio.Value  = bioText;
            tv.Value   = totalViews;
            days.Value = 12 + i * 3;
            cmd.ExecuteNonQuery();
            i++;
        }
    }

    private static void SeedArticles(SqliteConnection conn)
    {
        // Titles grouped by category (catId, authorId, title, words, readTime, views, comments, shares, featured, daysAgo)
        var articles = new List<(int cat, int auth, string title, int words, int readTime, int views, int comments, int shares, int featured, int daysAgo)>
        {
            // Công nghệ (cat=1)
            (1,1,"ChatGPT-5 chính thức ra mắt — thay đổi cuộc chơi AI toàn cầu",1200,5,45200,234,1820,1,1),
            (1,1,"Apple Intelligence: Những tính năng AI sẽ đổ bộ iPhone 17",980,4,38500,189,1540,0,3),
            (1,1,"Việt Nam tăng tốc phát triển AI quốc gia — đặt mục tiêu top 50 thế giới",1100,5,29400,156,1100,1,5),
            (1,1,"5G phủ sóng toàn quốc năm 2026 — cơ hội và thách thức",900,4,22100,98,880,0,7),
            (1,9,"Top 10 laptop cho lập trình viên 2026 — so sánh chi tiết",1800,8,18700,76,720,0,10),
            (1,1,"Gemini Ultra 2 vs GPT-5: AI nào thông minh hơn?",1400,6,31200,198,1250,0,4),
            (1,1,"Blockchain và tương lai của tài chính phi tập trung tại Việt Nam",1100,5,15400,67,560,0,14),
            (1,9,"Hướng dẫn bảo mật tài khoản mạng xã hội trong thời đại AI deepfake",950,4,12800,88,490,0,8),
            (1,1,"Thiết bị AI đeo tay thế hệ mới — wearable tech 2026",800,3,9600,45,380,0,15),
            (1,1,"Điện toán lượng tử: Khi nào máy tính lượng tử sẽ thay thế máy tính thường?",1300,6,11200,59,430,1,20),
            // Kinh tế (cat=2)
            (2,2,"VN-Index phá đỉnh lịch sử 1.600 điểm — chứng khoán Việt Nam bứt phá",1000,4,42000,312,1680,1,2),
            (2,2,"Giá vàng SJC lập đỉnh mới — nên mua hay chờ?",850,4,38700,287,1540,1,1),
            (2,2,"Bất động sản Hà Nội và TP.HCM — xu hướng giá 2026",1200,5,28900,178,1120,0,6),
            (2,10,"10 doanh nghiệp Việt Nam lọt top 500 doanh nghiệp lớn nhất Đông Nam Á",1100,5,21400,134,854,0,9),
            (2,2,"Lạm phát và bài toán điều hành lãi suất của Ngân hàng Nhà nước",1300,6,15600,89,620,0,12),
            (2,2,"Đầu tư FDI vào Việt Nam tăng kỷ lục — những ngành nào hưởng lợi?",1000,4,18900,98,760,0,7),
            (2,10,"Khởi nghiệp công nghệ Việt 2026 — làn sóng mới sau giai đoạn sàng lọc",900,4,12400,67,490,0,15),
            (2,2,"Tiền đồng mất giá — ảnh hưởng đến doanh nghiệp xuất nhập khẩu thế nào?",1100,5,9800,54,380,0,20),
            // Thể thao (cat=3)
            (3,3,"Đội tuyển Việt Nam vào bán kết AFF Cup 2026 — chiến thắng lịch sử",800,3,68900,567,2750,1,1),
            (3,3,"SEA Games 34: Đoàn Việt Nam dẫn đầu bảng tổng sắp huy chương",700,3,52300,423,2100,1,3),
            (3,3,"Nguyễn Thùy Linh giành HCV giải cầu lông châu Á — kỳ tích mới",750,3,41200,345,1640,1,5),
            (3,3,"Premier League 2025/26 — cuộc đua vô địch hấp dẫn nhất thập kỷ",950,4,38700,289,1540,0,4),
            (3,3,"World Cup 2026 tại Mỹ-Canada-Mexico — mọi thứ bạn cần biết",1400,6,35600,234,1420,1,10),
            (3,3,"Formula 1 tại Việt Nam — thông tin mới nhất về Hà Nội GP",900,4,28900,178,1156,0,8),
            (3,9,"Ronaldo hay Messi — ai vĩ đại hơn? Phân tích số liệu toàn diện",1200,5,31400,456,1890,0,15),
            (3,3,"VPro League — mùa giải 2026 chính thức khởi tranh với nhiều kỷ lục",800,3,18900,123,760,0,12),
            // Giải trí (cat=4)
            (4,4,"BTS ra mắt album comeback sau 2 năm nghĩa vụ quân sự — kỷ lục streaming",750,3,48900,389,1960,1,2),
            (4,4,"Phim Việt 'Đất Rừng Phương Nam 2' phá kỷ lục phòng vé nội địa",800,3,42100,312,1680,1,4),
            (4,4,"BlackPink world tour 2026 — Hà Nội và TP.HCM có tên trong danh sách",700,3,38700,298,1548,1,3),
            (4,4,"Oscar 2026 — những bộ phim Việt Nam đầu tiên được đề cử",950,4,29400,234,1176,0,7),
            (4,4,"Taylor Swift Eras Tour Việt Nam — giá vé và cách mua",600,3,45600,567,2280,1,1),
            (4,4,"Trấn Thành — hành trình từ diễn viên đến nhà sản xuất phim nghìn tỷ",1000,4,21400,156,856,0,10),
            (4,9,"Ngành âm nhạc Việt Nam 2026 — xu hướng và nghệ sĩ đột phá",850,4,14200,98,568,0,14),
            // Xã hội (cat=5)
            (5,5,"Thống kê dân số Việt Nam 2026 — già hóa dân số và những thách thức",1100,5,22400,167,896,0,8),
            (5,5,"Bão số 5 đổ bộ miền Trung — thống kê thiệt hại và công tác cứu trợ",900,4,38700,298,1548,1,2),
            (5,5,"Cháy chung cư tại Hà Nội — 5 nạn nhân được cứu thoát an toàn",700,3,45600,423,1824,1,1),
            (5,5,"Người Việt sử dụng mạng xã hội nhiều nhất Đông Nam Á — nghiên cứu mới",950,4,18900,134,756,0,6),
            (5,5,"Thanh niên Việt Nam và xu hướng 'nghỉ hưu sớm' FIRE — thực tế thế nào?",1200,5,15400,112,616,0,10),
            (5,9,"Tình trạng tắc đường tại TP.HCM — giải pháp nào cho tương lai?",1000,4,12400,89,496,0,12),
            // Sức khỏe (cat=6)
            (6,6,"Bệnh tay chân miệng bùng phát tại các tỉnh phía Nam — cảnh báo khẩn",800,3,38700,289,1548,1,3),
            (6,6,"Vắc-xin phòng ung thư cổ tử cung — cách tiêm và đối tượng ưu tiên",950,4,28900,198,1156,0,5),
            (6,6,"Chế độ ăn intermittent fasting — hiệu quả và những điều cần biết",1100,5,22400,167,896,0,7),
            (6,6,"Sức khỏe tâm thần sau đại dịch — Việt Nam cần làm gì?",1200,5,18700,145,748,0,9),
            (6,6,"Top 10 thực phẩm tốt nhất cho não bộ theo nghiên cứu mới",850,4,15400,112,616,0,11),
            (6,6,"Yoga mỗi ngày — những thay đổi kỳ diệu sau 30 ngày kiên trì",900,4,24600,189,984,0,6),
            // Du lịch (cat=7)
            (7,7,"Top 10 điểm du lịch Việt Nam không thể bỏ qua năm 2026",1400,6,34500,245,1380,1,5),
            (7,7,"Visa miễn phí 45 ngày tại Việt Nam — thủ tục và hướng dẫn cập nhật",1000,4,28900,198,1156,0,4),
            (7,7,"Phú Quốc 2026 — điểm du lịch hàng đầu châu Á theo Travel+Leisure",850,4,24600,178,984,0,8),
            (7,7,"Hành trình khám phá Tây Bắc mùa lúa chín — cẩm nang chi tiết",1200,5,18900,134,756,0,12),
            (7,7,"Du lịch Nhật Bản tự túc 2026 — chi phí thực tế từ người đã đi",1800,8,22400,167,896,0,10),
            (7,7,"Sa Pa và Mù Căng Chải — so sánh để chọn điểm đến hoàn hảo",1000,4,15400,112,616,0,15),
            // Giáo dục (cat=8)
            (8,8,"Kỳ thi tốt nghiệp THPT 2026 — những thay đổi quan trọng thí sinh cần biết",1100,5,42100,312,1684,1,2),
            (8,8,"Học phí đại học 2026 — danh sách trường tăng và giảm học phí",1000,4,38700,287,1548,1,1),
            (8,8,"Du học Nhật Bản 2026 — học bổng và điều kiện xét tuyển",1200,5,28900,198,1156,0,5),
            (8,8,"AI thay thế giáo viên — mối lo ngại hay cơ hội đổi mới giáo dục?",1300,6,18700,145,748,0,8),
            (8,8,"Chương trình STEM tại trường phổ thông — hiệu quả sau 3 năm triển khai",1000,4,12400,89,496,0,12),
            (8,9,"Kỹ năng cần có để cạnh tranh thị trường lao động 2026",950,4,15400,112,616,0,10),
        };

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"INSERT INTO articles
            (category_id, author_id, title, slug, excerpt, word_count, status, is_featured,
             view_count, comment_count, share_count, read_time_minutes, published_at, created_at, updated_at)
            VALUES (@cat, @aut, @tit, @slu, @exc, @wrd, 'published', @fea,
                    @vie, @com, @sha, @rtm,
                    datetime('now','-'||@ago||' days'),
                    datetime('now','-'||@ago||' days'),
                    datetime('now','-'||@ago||' days'))";

        var pars = new Dictionary<string, SqliteParameter>
        {
            ["@cat"] = cmd.Parameters.Add("@cat", SqliteType.Integer),
            ["@aut"] = cmd.Parameters.Add("@aut", SqliteType.Integer),
            ["@tit"] = cmd.Parameters.Add("@tit", SqliteType.Text),
            ["@slu"] = cmd.Parameters.Add("@slu", SqliteType.Text),
            ["@exc"] = cmd.Parameters.Add("@exc", SqliteType.Text),
            ["@wrd"] = cmd.Parameters.Add("@wrd", SqliteType.Integer),
            ["@fea"] = cmd.Parameters.Add("@fea", SqliteType.Integer),
            ["@vie"] = cmd.Parameters.Add("@vie", SqliteType.Integer),
            ["@com"] = cmd.Parameters.Add("@com", SqliteType.Integer),
            ["@sha"] = cmd.Parameters.Add("@sha", SqliteType.Integer),
            ["@rtm"] = cmd.Parameters.Add("@rtm", SqliteType.Integer),
            ["@ago"] = cmd.Parameters.Add("@ago", SqliteType.Integer),
        };

        int idx = 1;
        foreach (var (cat, auth, title, words, readTime, views, comments, shares, featured, daysAgo) in articles)
        {
            pars["@cat"].Value = cat;
            pars["@aut"].Value = auth;
            pars["@tit"].Value = title;
            pars["@slu"].Value = $"bai-{idx:D4}-{cat}-{daysAgo}";
            pars["@exc"].Value = $"{title.Substring(0, Math.Min(80, title.Length))}...";
            pars["@wrd"].Value = words;
            pars["@fea"].Value = featured;
            pars["@vie"].Value = views;
            pars["@com"].Value = comments;
            pars["@sha"].Value = shares;
            pars["@rtm"].Value = readTime;
            pars["@ago"].Value = daysAgo;
            cmd.ExecuteNonQuery();
            idx++;
        }

        // Update category article_count
        using var updateCmd = conn.CreateCommand();
        updateCmd.CommandText = @"UPDATE categories SET article_count = (SELECT COUNT(*) FROM articles WHERE articles.category_id = categories.id)";
        updateCmd.ExecuteNonQuery();

        // Update author total_articles
        using var updateAuth = conn.CreateCommand();
        updateAuth.CommandText = @"UPDATE authors SET total_articles = (SELECT COUNT(*) FROM articles WHERE articles.author_id = authors.id)";
        updateAuth.ExecuteNonQuery();
    }

    private static void SeedReaders(SqliteConnection conn)
    {
        var rand = new Random(42);
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"INSERT INTO readers (full_name, email, city, is_subscribed, total_reads, registered_at)
                            VALUES (@fn, @em, @ci, @sub, @tr, datetime('now','-'||@days||' days'))";
        var fn   = cmd.Parameters.Add("@fn",   SqliteType.Text);
        var em   = cmd.Parameters.Add("@em",   SqliteType.Text);
        var ci   = cmd.Parameters.Add("@ci",   SqliteType.Text);
        var sub  = cmd.Parameters.Add("@sub",  SqliteType.Integer);
        var tr   = cmd.Parameters.Add("@tr",   SqliteType.Integer);
        var days = cmd.Parameters.Add("@days", SqliteType.Integer);

        for (int i = 1; i <= 1200; i++)
        {
            var firstName  = FirstNames[rand.Next(FirstNames.Length)];
            var middleName = MiddleNames[rand.Next(MiddleNames.Length)];
            var lastName   = LastNames[rand.Next(LastNames.Length)];
            fn.Value   = $"{firstName} {middleName} {lastName}";
            em.Value   = $"reader{i:D4}@gmail.com";
            ci.Value   = Cities[rand.Next(Cities.Length)];
            sub.Value  = rand.Next(3) == 0 ? 1 : 0;
            tr.Value   = rand.Next(0, 150);
            days.Value = rand.Next(0, 365);
            cmd.ExecuteNonQuery();
        }
    }

    private static void SeedArticleViews(SqliteConnection conn)
    {
        // Get article IDs and view counts
        using var selectCmd = conn.CreateCommand();
        selectCmd.CommandText = "SELECT id, view_count FROM articles ORDER BY id";
        var articleData = new List<(long id, int viewCount)>();
        using (var r = selectCmd.ExecuteReader())
        {
            while (r.Read())
                articleData.Add((r.GetInt64(0), r.GetInt32(1)));
        }

        var rand    = new Random(55);
        var sources = new[] { "direct", "direct", "google", "google", "social", "social", "email", "referral" };
        var devices = new[] { "mobile", "mobile", "desktop", "desktop", "tablet" };
        long readerCount = 1200;

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"INSERT INTO article_views (article_id, reader_id, source, device, country, read_completion_percent, viewed_at)
                            VALUES (@aid, @rid, @src, @dev, 'VN', @rcp, datetime('now','-'||@hrs||' hours'))";
        var aid = cmd.Parameters.Add("@aid", SqliteType.Integer);
        var rid = cmd.Parameters.Add("@rid", SqliteType.Integer);
        var src = cmd.Parameters.Add("@src", SqliteType.Text);
        var dev = cmd.Parameters.Add("@dev", SqliteType.Text);
        var rcp = cmd.Parameters.Add("@rcp", SqliteType.Real);
        var hrs = cmd.Parameters.Add("@hrs", SqliteType.Integer);

        // Seed a manageable number of views per article
        foreach (var (articleId, viewCount) in articleData)
        {
            int viewsToInsert = Math.Min(viewCount, 60); // cap at 60 rows per article
            for (int v = 0; v < viewsToInsert; v++)
            {
                aid.Value = articleId;
                rid.Value = rand.Next(0, 4) == 0 ? (object)DBNull.Value : (object)rand.Next(1, (int)readerCount + 1);
                src.Value = sources[rand.Next(sources.Length)];
                dev.Value = devices[rand.Next(devices.Length)];
                rcp.Value = Math.Round(rand.NextDouble() * 100, 1);
                hrs.Value = rand.Next(1, 720); // up to 30 days ago
                cmd.ExecuteNonQuery();
            }
        }
    }

    private static void SeedComments(SqliteConnection conn)
    {
        using var selectCmd = conn.CreateCommand();
        selectCmd.CommandText = "SELECT id, comment_count FROM articles ORDER BY id";
        var articleData = new List<(long id, int comments)>();
        using (var r = selectCmd.ExecuteReader())
        {
            while (r.Read())
                articleData.Add((r.GetInt64(0), r.GetInt32(1)));
        }

        var rand = new Random(88);
        var commentTemplates = new[]
        {
            "Bài viết rất hay và bổ ích, cảm ơn tác giả!",
            "Thông tin cần được kiểm chứng thêm, mình thấy còn nhiều điểm chưa rõ.",
            "Cảm ơn tòa soạn đã cập nhật thông tin kịp thời.",
            "Mình đã biết điều này từ lâu nhưng đọc bài này thấy rõ hơn.",
            "Rất mong có thêm bài phân tích chuyên sâu hơn về chủ đề này.",
            "Chia sẻ cho bạn bè đọc luôn, thông tin quan trọng quá!",
            "Góc nhìn của tác giả rất thú vị, mình đồng ý với quan điểm này.",
            "Nội dung cập nhật mới nhất, rất cần thiết trong thời điểm hiện tại.",
            "Bài viết giúp mình hiểu rõ hơn về vấn đề đang được quan tâm.",
            "Tuyệt vời! Đây là thông tin mình đang tìm kiếm từ lâu."
        };

        long readerCount = 1200;
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"INSERT INTO comments (article_id, reader_id, content, is_approved, created_at)
                            VALUES (@aid, @rid, @con, 1, datetime('now','-'||@hrs||' hours'))";
        var aid = cmd.Parameters.Add("@aid", SqliteType.Integer);
        var rid = cmd.Parameters.Add("@rid", SqliteType.Integer);
        var con = cmd.Parameters.Add("@con", SqliteType.Text);
        var hrs = cmd.Parameters.Add("@hrs", SqliteType.Integer);

        foreach (var (articleId, commentCount) in articleData)
        {
            int commentsToInsert = Math.Min(commentCount, 20);
            for (int c = 0; c < commentsToInsert; c++)
            {
                aid.Value = articleId;
                rid.Value = rand.Next(1, (int)readerCount + 1);
                con.Value = commentTemplates[rand.Next(commentTemplates.Length)];
                hrs.Value = rand.Next(1, 480);
                cmd.ExecuteNonQuery();
            }
        }

        // Update reader total_comments
        using var updateCmd = conn.CreateCommand();
        updateCmd.CommandText = @"UPDATE readers SET total_comments = (SELECT COUNT(*) FROM comments WHERE comments.reader_id = readers.id)";
        updateCmd.ExecuteNonQuery();
    }
}
