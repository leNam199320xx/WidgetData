using Microsoft.Data.Sqlite;

namespace WidgetData.Infrastructure.Data;

/// <summary>
/// Generates a SQLite learning-management database to simulate an online course platform (EduViet).
/// Tables: categories, instructors, courses, students, enrollments, lessons, lesson_progress, course_payments, reviews
/// </summary>
public static class CourseDataSeeder
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

    public static void EnsureCourseDatabase(string dbPath)
    {
        if (File.Exists(dbPath)) return;

        using var conn = new SqliteConnection($"Data Source={dbPath}");
        conn.Open();

        CreateSchema(conn);
        SeedCategories(conn);
        SeedInstructors(conn);
        SeedCourses(conn);
        SeedStudents(conn);
        SeedLessons(conn);
        SeedEnrollments(conn);
        SeedPayments(conn);
        SeedReviews(conn);
    }

    private static void CreateSchema(SqliteConnection conn)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
PRAGMA journal_mode=WAL;

CREATE TABLE IF NOT EXISTS categories (
    id          INTEGER PRIMARY KEY AUTOINCREMENT,
    name        TEXT NOT NULL,
    slug        TEXT NOT NULL UNIQUE,
    description TEXT,
    icon        TEXT,
    sort_order  INTEGER NOT NULL DEFAULT 0,
    created_at  TEXT DEFAULT (datetime('now'))
);

CREATE TABLE IF NOT EXISTS instructors (
    id             INTEGER PRIMARY KEY AUTOINCREMENT,
    full_name      TEXT NOT NULL,
    email          TEXT NOT NULL UNIQUE,
    bio            TEXT,
    specialization TEXT,
    rating         REAL NOT NULL DEFAULT 0,
    total_students INTEGER NOT NULL DEFAULT 0,
    total_courses  INTEGER NOT NULL DEFAULT 0,
    is_active      INTEGER NOT NULL DEFAULT 1,
    joined_at      TEXT DEFAULT (datetime('now'))
);

CREATE TABLE IF NOT EXISTS courses (
    id                INTEGER PRIMARY KEY AUTOINCREMENT,
    category_id       INTEGER NOT NULL,
    instructor_id     INTEGER NOT NULL,
    title             TEXT NOT NULL,
    slug              TEXT NOT NULL UNIQUE,
    description       TEXT,
    level             TEXT NOT NULL DEFAULT 'beginner',
    price             REAL NOT NULL DEFAULT 0,
    original_price    REAL NOT NULL DEFAULT 0,
    duration_hours    REAL NOT NULL DEFAULT 0,
    total_lessons     INTEGER NOT NULL DEFAULT 0,
    is_published      INTEGER NOT NULL DEFAULT 1,
    is_featured       INTEGER NOT NULL DEFAULT 0,
    rating            REAL NOT NULL DEFAULT 0,
    total_ratings     INTEGER NOT NULL DEFAULT 0,
    total_enrollments INTEGER NOT NULL DEFAULT 0,
    language          TEXT NOT NULL DEFAULT 'vi',
    created_at        TEXT DEFAULT (datetime('now')),
    published_at      TEXT,
    FOREIGN KEY (category_id) REFERENCES categories(id),
    FOREIGN KEY (instructor_id) REFERENCES instructors(id)
);

CREATE TABLE IF NOT EXISTS students (
    id           INTEGER PRIMARY KEY AUTOINCREMENT,
    full_name    TEXT NOT NULL,
    email        TEXT NOT NULL UNIQUE,
    phone        TEXT,
    city         TEXT,
    is_active    INTEGER NOT NULL DEFAULT 1,
    registered_at TEXT DEFAULT (datetime('now'))
);

CREATE TABLE IF NOT EXISTS enrollments (
    id               INTEGER PRIMARY KEY AUTOINCREMENT,
    course_id        INTEGER NOT NULL,
    student_id       INTEGER NOT NULL,
    status           TEXT NOT NULL DEFAULT 'active',
    progress_percent REAL NOT NULL DEFAULT 0,
    enrolled_at      TEXT DEFAULT (datetime('now')),
    completed_at     TEXT,
    FOREIGN KEY (course_id) REFERENCES courses(id),
    FOREIGN KEY (student_id) REFERENCES students(id)
);

CREATE TABLE IF NOT EXISTS lessons (
    id              INTEGER PRIMARY KEY AUTOINCREMENT,
    course_id       INTEGER NOT NULL,
    section_title   TEXT,
    title           TEXT NOT NULL,
    order_num       INTEGER NOT NULL DEFAULT 1,
    duration_minutes INTEGER NOT NULL DEFAULT 10,
    lesson_type     TEXT NOT NULL DEFAULT 'video',
    is_free         INTEGER NOT NULL DEFAULT 0,
    is_published    INTEGER NOT NULL DEFAULT 1,
    FOREIGN KEY (course_id) REFERENCES courses(id)
);

CREATE TABLE IF NOT EXISTS lesson_progress (
    id              INTEGER PRIMARY KEY AUTOINCREMENT,
    enrollment_id   INTEGER NOT NULL,
    lesson_id       INTEGER NOT NULL,
    is_completed    INTEGER NOT NULL DEFAULT 0,
    watched_seconds INTEGER NOT NULL DEFAULT 0,
    completed_at    TEXT,
    FOREIGN KEY (enrollment_id) REFERENCES enrollments(id),
    FOREIGN KEY (lesson_id) REFERENCES lessons(id)
);

CREATE TABLE IF NOT EXISTS course_payments (
    id              INTEGER PRIMARY KEY AUTOINCREMENT,
    enrollment_id   INTEGER NOT NULL,
    student_id      INTEGER NOT NULL,
    course_id       INTEGER NOT NULL,
    amount          REAL NOT NULL,
    original_price  REAL NOT NULL,
    discount_amount REAL NOT NULL DEFAULT 0,
    coupon_code     TEXT,
    payment_method  TEXT NOT NULL,
    status          TEXT NOT NULL DEFAULT 'success',
    transaction_ref TEXT,
    paid_at         TEXT DEFAULT (datetime('now')),
    FOREIGN KEY (enrollment_id) REFERENCES enrollments(id),
    FOREIGN KEY (student_id) REFERENCES students(id),
    FOREIGN KEY (course_id) REFERENCES courses(id)
);

CREATE TABLE IF NOT EXISTS reviews (
    id           INTEGER PRIMARY KEY AUTOINCREMENT,
    course_id    INTEGER NOT NULL,
    student_id   INTEGER NOT NULL,
    rating       INTEGER NOT NULL DEFAULT 5,
    comment      TEXT,
    is_published INTEGER NOT NULL DEFAULT 1,
    created_at   TEXT DEFAULT (datetime('now')),
    FOREIGN KEY (course_id) REFERENCES courses(id),
    FOREIGN KEY (student_id) REFERENCES students(id)
);

CREATE INDEX IF NOT EXISTS idx_enrollments_course_id  ON enrollments(course_id);
CREATE INDEX IF NOT EXISTS idx_enrollments_student_id ON enrollments(student_id);
CREATE INDEX IF NOT EXISTS idx_enrollments_enrolled_at ON enrollments(enrolled_at);
CREATE INDEX IF NOT EXISTS idx_course_payments_paid_at ON course_payments(paid_at);
CREATE INDEX IF NOT EXISTS idx_lesson_progress_enrollment ON lesson_progress(enrollment_id);
CREATE INDEX IF NOT EXISTS idx_courses_category ON courses(category_id);
";
        cmd.ExecuteNonQuery();
    }

    private static void SeedCategories(SqliteConnection conn)
    {
        var categories = new[]
        {
            ("Lập trình & Công nghệ", "lap-trinh-cong-nghe", "Từ web development đến AI, cloud, data science", "💻", 1),
            ("Kinh doanh & Khởi nghiệp", "kinh-doanh-khoi-nghiep", "Xây dựng và phát triển doanh nghiệp", "💼", 2),
            ("Marketing & Truyền thông", "marketing-truyen-thong", "Digital marketing, content, SEO/SEM, ads", "📣", 3),
            ("Thiết kế & Sáng tạo", "thiet-ke-sang-tao", "UI/UX, đồ họa, video, animation", "🎨", 4),
            ("Ngoại ngữ", "ngoai-ngu", "Tiếng Anh, Nhật, Hàn, Trung và nhiều ngôn ngữ khác", "🌏", 5),
            ("Phát triển bản thân", "phat-trien-ban-than", "Kỹ năng mềm, tư duy, lãnh đạo, quản lý thời gian", "🧠", 6),
            ("Chụp ảnh & Video", "chup-anh-video", "Nhiếp ảnh, quay phim, dựng phim chuyên nghiệp", "📷", 7),
            ("Sức khỏe & Thể thao", "suc-khoe-the-thao", "Yoga, dinh dưỡng, thiền định, thể dục", "🏃", 8)
        };

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO categories (name, slug, description, icon, sort_order) VALUES (@n, @s, @d, @i, @o)";
        var n = cmd.Parameters.Add("@n", SqliteType.Text);
        var s = cmd.Parameters.Add("@s", SqliteType.Text);
        var d = cmd.Parameters.Add("@d", SqliteType.Text);
        var ic = cmd.Parameters.Add("@i", SqliteType.Text);
        var o = cmd.Parameters.Add("@o", SqliteType.Integer);

        foreach (var (name, slug, desc, icon, sort) in categories)
        {
            n.Value = name; s.Value = slug; d.Value = desc; ic.Value = icon; o.Value = sort;
            cmd.ExecuteNonQuery();
        }
    }

    private static void SeedInstructors(SqliteConnection conn)
    {
        var instructors = new[]
        {
            ("Nguyễn Văn An",   "an.nguyen@eduviet.vn",   "Kỹ sư phần mềm 10 năm kinh nghiệm tại các tập đoàn lớn. Đam mê chia sẻ kiến thức lập trình.", "Python, AI/ML, Backend",     4.9, 8200,  6),
            ("Trần Minh Khoa",  "khoa.tran@eduviet.vn",   "AWS Certified Solutions Architect Professional. Chuyên gia về cloud và DevOps.",                 "Cloud AWS, DevOps, Docker",  4.8, 4600,  5),
            ("Lê Thành Đạt",    "dat.le@eduviet.vn",      "Frontend engineer với 8 năm kinh nghiệm. Tác giả nhiều open-source libraries nổi tiếng.",        "React, TypeScript, Vue.js",  4.9, 6800,  7),
            ("Phạm Đức Trung",  "trung.pham@eduviet.vn",  "Chuyên gia an ninh mạng, OSCP certified. Từng làm việc cho các ngân hàng lớn tại Việt Nam.",     "Cybersecurity, Ethical Hacking", 4.7, 3400, 4),
            ("Ngô Thị Mai",     "mai.ngo@eduviet.vn",     "Mobile developer chuyên Flutter/Dart và React Native. 7 năm xây dựng ứng dụng thương mại điện tử.", "Flutter, React Native, iOS", 4.8, 5700, 6),
            ("Hoàng Gia Bảo",   "bao.hoang@eduviet.vn",   "AI researcher và prompt engineering expert. Tốt nghiệp tiến sĩ CNTT tại ĐH Bách Khoa.",           "AI, LLM, Prompt Engineering",4.8, 11200, 8),
            ("Vũ Hoàng Nam",    "nam.vu@eduviet.vn",      "Serial entrepreneur với 3 startup thành công. Mentor startup ecosystem tại Việt Nam.",              "Growth Hacking, Startup",    4.8, 7800,  7),
            ("Đặng Hữu Hiệp",   "hiep.dang@eduviet.vn",   "MBA Harvard. Chuyên gia tư vấn chiến lược cho doanh nghiệp vừa và nhỏ tại Đông Nam Á.",           "Business Strategy, MBA",     4.7, 5400,  6),
            ("Nguyễn Thị Cẩm",  "cam.nguyen@eduviet.vn",  "Digital marketing expert với 9 năm kinh nghiệm. Từng quản lý ngân sách quảng cáo >50 tỷ/năm.",   "Facebook Ads, TikTok, SEM",  4.6, 9200,  9),
            ("Lý Thị Trang",    "trang.ly@eduviet.vn",    "Content strategist và copywriter. Đã viết cho hơn 100 thương hiệu lớn tại Việt Nam.",              "Content Marketing, SEO",     4.8, 6100,  7),
            ("Bùi Văn Hải",     "hai.bui@eduviet.vn",     "UX/UI designer tại Grab và Shopee trong 7 năm. Hiện là giám đốc thiết kế tại startup fintech.",    "UI/UX, Figma, Design System",4.9, 4800,  5),
            ("Trần Thị Hằng",   "hang.tran@eduviet.vn",   "Giáo viên tiếng Anh IELTS 8.5. Từng giảng dạy tại British Council và IDP.",                       "IELTS, TOEIC, Communication",4.9, 15600, 12),
            ("Lê Quang Minh",   "minh.le@eduviet.vn",     "Chuyên gia về thiền định và phát triển bản thân. Tác giả 3 cuốn sách bestseller.",                  "Mindfulness, Leadership",    4.8, 7200,  6),
            ("Đỗ Thị Phương",   "phuong.do@eduviet.vn",   "Nhiếp ảnh gia chuyên nghiệp với hơn 15 năm kinh nghiệm. Ảnh đăng trên National Geographic.",      "Photography, Lightroom",     4.9, 3900,  4),
            ("Cao Văn Thịnh",   "thinh.cao@eduviet.vn",   "Huấn luyện viên yoga và dinh dưỡng thể thao. Chứng chỉ quốc tế RYT-500.",                          "Yoga, Nutrition, Fitness",   4.8, 5300,  5)
        };

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"INSERT INTO instructors (full_name, email, bio, specialization, rating, total_students, total_courses, is_active, joined_at)
                            VALUES (@fn, @em, @bio, @spec, @rat, @ts, @tc, 1, datetime('now', '-' || @days || ' months'))";
        var fn   = cmd.Parameters.Add("@fn",   SqliteType.Text);
        var em   = cmd.Parameters.Add("@em",   SqliteType.Text);
        var bio  = cmd.Parameters.Add("@bio",  SqliteType.Text);
        var spec = cmd.Parameters.Add("@spec", SqliteType.Text);
        var rat  = cmd.Parameters.Add("@rat",  SqliteType.Real);
        var ts   = cmd.Parameters.Add("@ts",   SqliteType.Integer);
        var tc   = cmd.Parameters.Add("@tc",   SqliteType.Integer);
        var days = cmd.Parameters.Add("@days", SqliteType.Integer);

        int i = 0;
        foreach (var (name, email, bioText, specialization, rating, students, courses) in instructors)
        {
            fn.Value = name; em.Value = email; bio.Value = bioText; spec.Value = specialization;
            rat.Value = rating; ts.Value = students; tc.Value = courses;
            days.Value = 6 + i * 2;
            cmd.ExecuteNonQuery();
            i++;
        }
    }

    private static void SeedCourses(SqliteConnection conn)
    {
        // (categoryId, instructorId, title, slug, level, price, originalPrice, durationHours, totalLessons, isFeatured, rating, totalRatings, totalEnrollments, daysAgo)
        var courses = new[]
        {
            // Lập trình & Công nghệ (cat=1)
            (1, 1, "Python nâng cao — Design Patterns & Clean Code", "python-nang-cao-design-patterns", "advanced",   699000.0, 999000.0,  36.0, 72, 1, 4.9, 820,  8200, 180),
            (1, 2, "AWS Certified Solutions Architect — tiếng Việt", "aws-solutions-architect-tieng-viet", "intermediate", 899000.0, 1299000.0, 48.0, 96, 1, 4.8, 460,  4600, 150),
            (1, 3, "React 19 + TypeScript — xây dựng SPA chuyên nghiệp", "react-19-typescript-spa", "intermediate", 749000.0, 1099000.0, 52.0, 104, 1, 4.9, 680, 6800, 120),
            (1, 4, "An ninh mạng — Ethical Hacking cơ bản đến nâng cao", "an-ninh-mang-ethical-hacking", "advanced",   849000.0, 0.0,       44.0, 88, 0, 4.7, 340,  3400, 200),
            (1, 5, "Flutter & Dart — lập trình ứng dụng di động đa nền", "flutter-dart-lap-trinh-di-dong", "all",       649000.0, 949000.0,  38.0, 76, 1, 4.8, 570,  5700, 90),
            (1, 6, "Prompt Engineering — Làm chủ ChatGPT & LLM",        "prompt-engineering-chatgpt-llm",  "beginner",  299000.0, 499000.0,  18.0, 36, 1, 4.8, 1120, 11200,60),
            (1, 1, "Django REST API — xây dựng backend hiện đại",        "django-rest-api-backend",         "intermediate",599000.0, 899000.0, 30.0, 60, 0, 4.7, 410,  4100, 100),
            (1, 3, "Node.js + NestJS — Microservices Architecture",      "nodejs-nestjs-microservices",     "advanced",   799000.0, 1199000.0, 40.0, 80, 0, 4.8, 290,  2900, 130),
            (1, 2, "Docker & Kubernetes — triển khai ứng dụng thực tế",  "docker-kubernetes-thuc-te",       "intermediate",749000.0, 999000.0, 28.0, 56, 0, 4.8, 380,  3800, 110),
            (1, 6, "Machine Learning với Python — từ cơ bản đến thực chiến","ml-python-co-ban-thuc-chien", "intermediate",799000.0, 1199000.0, 45.0, 90, 1, 4.9, 550,  5500, 80),
            // Kinh doanh & Khởi nghiệp (cat=2)
            (2, 7, "Growth Hacking — tăng trưởng nhanh không tốn tiền",  "growth-hacking-tang-truong",      "all",       399000.0, 599000.0,  22.0, 44, 1, 4.8, 780,  7800, 160),
            (2, 8, "Khởi nghiệp từ 0 — Business Model Canvas & Lean Startup","khoi-nghiep-tu-0-bmc",       "beginner",  499000.0, 699000.0,  30.0, 60, 0, 4.7, 540,  5400, 140),
            (2, 7, "Product Management — xây dựng sản phẩm từ ý tưởng đến thị trường","pm-san-pham-thi-truong","intermediate",649000.0, 899000.0,24.0, 48, 0, 4.8, 320,  3200, 100),
            (2, 8, "Quản trị tài chính doanh nghiệp nhỏ",               "quan-tri-tai-chinh-doanh-nghiep", "beginner",  449000.0, 649000.0,  20.0, 40, 0, 4.6, 280,  2800, 120),
            (2, 7, "Đàm phán thương mại — kỹ năng đạt thỏa thuận tốt nhất","dam-phan-thuong-mai",          "all",       349000.0, 549000.0,  16.0, 32, 0, 4.7, 410,  4100, 90),
            // Marketing & Truyền thông (cat=3)
            (3, 9, "Facebook & TikTok Ads 2026 — chạy ads hiệu quả",     "facebook-tiktok-ads-2026",        "beginner",  349000.0, 549000.0,  25.0, 50, 1, 4.6, 920,  9200, 75),
            (3,10, "Content Marketing — viết content triệu view",         "content-marketing-trieu-view",    "all",       299000.0, 0.0,       20.0, 40, 1, 4.8, 610,  6100, 60),
            (3, 9, "Google Ads & SEO — tổng thể digital marketing",       "google-ads-seo-digital",          "intermediate",549000.0, 799000.0, 32.0, 64, 0, 4.7, 380,  3800, 110),
            (3,10, "Email Marketing Automation — nuôi dưỡng khách hàng", "email-marketing-automation",      "intermediate",399000.0, 599000.0, 18.0, 36, 0, 4.7, 260,  2600, 95),
            (3, 9, "TikTok Shop — bán hàng livestream chuyên nghiệp",     "tiktok-shop-ban-hang-livestream", "beginner",  249000.0, 399000.0,  14.0, 28, 0, 4.5, 1200, 12000,45),
            // Thiết kế & Sáng tạo (cat=4)
            (4,11, "UI/UX Design — từ wireframe đến prototype",           "uiux-design-wireframe-prototype",  "all",       599000.0, 899000.0,  36.0, 72, 1, 4.9, 480,  4800, 130),
            (4,11, "Figma Master Class — thiết kế giao diện chuyên nghiệp","figma-master-class",             "intermediate",449000.0, 649000.0, 24.0, 48, 0, 4.8, 320,  3200, 100),
            (4,14, "Nhiếp ảnh chân dung — ánh sáng, bố cục, hậu kỳ",    "nhiep-anh-chan-dung",              "beginner",  549000.0, 799000.0,  20.0, 40, 0, 4.9, 390,  3900, 150),
            (4,11, "Motion Graphics với After Effects",                   "motion-graphics-after-effects",   "advanced",  699000.0, 999000.0,  28.0, 56, 0, 4.8, 180,  1800, 200),
            (4,14, "Lightroom & Photoshop — chỉnh ảnh như chuyên gia",   "lightroom-photoshop-chinh-anh",   "all",       299000.0, 499000.0,  16.0, 32, 0, 4.8, 620,  6200, 80),
            // Ngoại ngữ (cat=5)
            (5,12, "IELTS 7.0+ — lộ trình học từ 0 đến band 7",          "ielts-7-lo-trinh",                "all",       799000.0, 1099000.0, 60.0, 120,1, 4.9, 1560, 15600,365),
            (5,12, "Tiếng Anh giao tiếp thương mại — Business English",  "tieng-anh-giao-tiep-thuong-mai",  "intermediate",499000.0, 699000.0, 30.0, 60, 0, 4.8, 780,  7800, 200),
            (5,12, "TOEIC 900+ — chinh phục bài thi Reading & Listening", "toeic-900-reading-listening",     "intermediate",649000.0, 899000.0, 40.0, 80, 1, 4.9, 920,  9200, 180),
            (5,12, "Tiếng Nhật N3 — từ cơ bản đến thực hành",            "tieng-nhat-n3",                   "beginner",  549000.0, 799000.0,  48.0, 96, 0, 4.7, 480,  4800, 240),
            (5,12, "Tiếng Hàn TOPIK II — lộ trình 6 tháng",              "tieng-han-topik-ii",              "intermediate",599000.0, 849000.0, 45.0, 90, 0, 4.8, 390,  3900, 210),
            // Phát triển bản thân (cat=6)
            (6,13, "Tư duy sáng tạo — khơi nguồn ý tưởng đột phá",       "tu-duy-sang-tao",                 "all",       299000.0, 499000.0,  14.0, 28, 0, 4.8, 720,  7200, 120),
            (6,13, "Lãnh đạo & Quản lý đội nhóm hiệu quả",               "lanh-dao-quan-ly-doi-nhom",       "intermediate",449000.0, 649000.0, 20.0, 40, 1, 4.9, 520,  5200, 150),
            (6,13, "Quản lý thời gian — phương pháp GTD & Pomodoro",      "quan-ly-thoi-gian-gtd",           "beginner",  199000.0, 349000.0,  10.0, 20, 0, 4.7, 980,  9800, 90),
            (6,13, "Kỹ năng thuyết trình chuyên nghiệp",                  "ky-nang-thuyet-trinh",            "all",       349000.0, 499000.0,  16.0, 32, 0, 4.8, 640,  6400, 100),
            (6,13, "Xây dựng thói quen tích cực — The Atomic Habits",     "xay-dung-thoi-quen-atomic-habits","beginner",  249000.0, 399000.0,  12.0, 24, 0, 4.8, 1100, 11000,70),
            // Chụp ảnh & Video (cat=7)
            (7,14, "Quay phim chuyên nghiệp bằng smartphone",            "quay-phim-smartphone",             "beginner",  349000.0, 549000.0,  18.0, 36, 0, 4.8, 520,  5200, 110),
            (7,14, "Premiere Pro — dựng phim từ A đến Z",                "premiere-pro-dung-phim",           "intermediate",499000.0, 749000.0, 28.0, 56, 0, 4.8, 390,  3900, 140),
            (7,14, "YouTube Creator — xây dựng kênh từ 0 lên 100K subs", "youtube-creator-100k",            "all",       449000.0, 649000.0,  22.0, 44, 1, 4.7, 860,  8600, 80),
            // Sức khỏe & Thể thao (cat=8)
            (8,15, "Yoga cơ bản — 30 ngày thay đổi cuộc sống",           "yoga-co-ban-30-ngay",              "beginner",  299000.0, 449000.0,  15.0, 30, 0, 4.9, 860,  8600, 200),
            (8,15, "Dinh dưỡng thể thao — ăn đúng để tập hiệu quả",      "dinh-duong-the-thao",             "all",       349000.0, 549000.0,  20.0, 40, 0, 4.8, 530,  5300, 160),
            (8,15, "Thiền định Vipassana — bình yên trong cuộc sống bận rộn","thien-dinh-vipassana",        "all",       249000.0, 399000.0,  12.0, 24, 0, 4.9, 690,  6900, 130),
        };

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"INSERT INTO courses
            (category_id, instructor_id, title, slug, description, level, price, original_price,
             duration_hours, total_lessons, is_published, is_featured, rating, total_ratings,
             total_enrollments, language, created_at, published_at)
            VALUES (@cat, @ins, @tit, @slu, @desc, @lev, @pri, @ori,
                    @dur, @les, 1, @fea, @rat, @tra,
                    @ten, 'vi', datetime('now','-'||@ago||' days'), datetime('now','-'||@ago||' days'))";

        var p = new Dictionary<string, SqliteParameter>
        {
            ["@cat"]  = cmd.Parameters.Add("@cat",  SqliteType.Integer),
            ["@ins"]  = cmd.Parameters.Add("@ins",  SqliteType.Integer),
            ["@tit"]  = cmd.Parameters.Add("@tit",  SqliteType.Text),
            ["@slu"]  = cmd.Parameters.Add("@slu",  SqliteType.Text),
            ["@desc"] = cmd.Parameters.Add("@desc", SqliteType.Text),
            ["@lev"]  = cmd.Parameters.Add("@lev",  SqliteType.Text),
            ["@pri"]  = cmd.Parameters.Add("@pri",  SqliteType.Real),
            ["@ori"]  = cmd.Parameters.Add("@ori",  SqliteType.Real),
            ["@dur"]  = cmd.Parameters.Add("@dur",  SqliteType.Real),
            ["@les"]  = cmd.Parameters.Add("@les",  SqliteType.Integer),
            ["@fea"]  = cmd.Parameters.Add("@fea",  SqliteType.Integer),
            ["@rat"]  = cmd.Parameters.Add("@rat",  SqliteType.Real),
            ["@tra"]  = cmd.Parameters.Add("@tra",  SqliteType.Integer),
            ["@ten"]  = cmd.Parameters.Add("@ten",  SqliteType.Integer),
            ["@ago"]  = cmd.Parameters.Add("@ago",  SqliteType.Integer),
        };

        string[] descTemplates =
        [
            "Khóa học {0} — kiến thức từ cơ bản đến nâng cao, áp dụng ngay vào thực tế với {1} bài học thực hành.",
            "Nắm vững {0} qua {1} bài giảng video chất lượng cao, bài tập và dự án thực tế từ chuyên gia hàng đầu.",
            "Lộ trình học {0} được thiết kế bài bản: {1} bài học, dự án cuối khóa và chứng chỉ hoàn thành.",
            "Từ người mới đến chuyên nghiệp với {0} — {1} bài học thực chiến, hỗ trợ 1:1 từ giảng viên.",
            "Khám phá {0} toàn diện qua {1} bài học video, quiz kiểm tra và tài liệu đi kèm chi tiết.",
        ];

        int descIdx = 0;
        foreach (var (cat, ins, title, slug, level, price, origPrice, dur, lessons, featured, rating, ratings, enrollments, daysAgo) in courses)
        {
            p["@cat"].Value  = cat;
            p["@ins"].Value  = ins;
            p["@tit"].Value  = title;
            p["@slu"].Value  = slug;
            p["@desc"].Value = string.Format(descTemplates[descIdx % descTemplates.Length], title, lessons);
            descIdx++;
            p["@lev"].Value  = level;
            p["@pri"].Value  = price;
            p["@ori"].Value  = origPrice;
            p["@dur"].Value  = dur;
            p["@les"].Value  = lessons;
            p["@fea"].Value  = featured;
            p["@rat"].Value  = rating;
            p["@tra"].Value  = ratings;
            p["@ten"].Value  = enrollments;
            p["@ago"].Value  = daysAgo;
            cmd.ExecuteNonQuery();
        }
    }

    private static void SeedStudents(SqliteConnection conn)
    {
        var rand = new Random(42);
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"INSERT INTO students (full_name, email, phone, city, is_active, registered_at)
                            VALUES (@fn, @em, @ph, @ci, 1, datetime('now','-'||@days||' days'))";
        var fn   = cmd.Parameters.Add("@fn",   SqliteType.Text);
        var em   = cmd.Parameters.Add("@em",   SqliteType.Text);
        var ph   = cmd.Parameters.Add("@ph",   SqliteType.Text);
        var ci   = cmd.Parameters.Add("@ci",   SqliteType.Text);
        var days = cmd.Parameters.Add("@days", SqliteType.Integer);

        for (int i = 1; i <= 800; i++)
        {
            var firstName  = FirstNames[rand.Next(FirstNames.Length)];
            var middleName = MiddleNames[rand.Next(MiddleNames.Length)];
            var lastName   = LastNames[rand.Next(LastNames.Length)];
            var fullName   = $"{firstName} {middleName} {lastName}";
            fn.Value   = fullName;
            em.Value   = $"student{i:D4}@eduviet.vn";
            ph.Value   = $"09{rand.Next(10000000, 99999999)}";
            ci.Value   = Cities[rand.Next(Cities.Length)];
            days.Value = rand.Next(0, 365);
            cmd.ExecuteNonQuery();
        }
    }

    private static void SeedLessons(SqliteConnection conn)
    {
        // Get course IDs and their total_lessons counts
        using var selectCmd = conn.CreateCommand();
        selectCmd.CommandText = "SELECT id, total_lessons FROM courses ORDER BY id";
        var courseData = new List<(long id, int lessons)>();
        using (var reader = selectCmd.ExecuteReader())
        {
            while (reader.Read())
                courseData.Add((reader.GetInt64(0), reader.GetInt32(1)));
        }

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"INSERT INTO lessons (course_id, section_title, title, order_num, duration_minutes, lesson_type, is_free, is_published)
                            VALUES (@cid, @sec, @tit, @ord, @dur, @typ, @fre, 1)";
        var cid = cmd.Parameters.Add("@cid", SqliteType.Integer);
        var sec = cmd.Parameters.Add("@sec", SqliteType.Text);
        var tit = cmd.Parameters.Add("@tit", SqliteType.Text);
        var ord = cmd.Parameters.Add("@ord", SqliteType.Integer);
        var dur = cmd.Parameters.Add("@dur", SqliteType.Integer);
        var typ = cmd.Parameters.Add("@typ", SqliteType.Text);
        var fre = cmd.Parameters.Add("@fre", SqliteType.Integer);

        string[] lessonTypes = ["video", "video", "video", "quiz", "reading"];

        foreach (var (courseId, totalLessons) in courseData)
        {
            int sections = Math.Max(1, totalLessons / 10);
            for (int i = 1; i <= totalLessons; i++)
            {
                int sectionNum = ((i - 1) / 10) + 1;
                cid.Value = courseId;
                sec.Value = $"Phần {sectionNum}: Chương {sectionNum}";
                tit.Value = $"Bài {i}: Nội dung bài học số {i}";
                ord.Value = i;
                dur.Value = 10 + (i % 20);
                typ.Value = lessonTypes[i % lessonTypes.Length];
                fre.Value = i <= 2 ? 1 : 0;
                cmd.ExecuteNonQuery();
            }
        }
    }

    private static void SeedEnrollments(SqliteConnection conn)
    {
        // Get course IDs and total_lessons
        using var selectCmd = conn.CreateCommand();
        selectCmd.CommandText = "SELECT id, total_lessons FROM courses ORDER BY id";
        var courseData = new Dictionary<long, int>();
        using (var reader = selectCmd.ExecuteReader())
        {
            while (reader.Read())
                courseData[reader.GetInt64(0)] = reader.GetInt32(1);
        }

        var rand     = new Random(99);
        long studentCount = 800;
        var statuses = new[] { "active", "active", "active", "completed", "completed", "paused" };

        using var enrollCmd = conn.CreateCommand();
        enrollCmd.CommandText = @"INSERT INTO enrollments (course_id, student_id, status, progress_percent, enrolled_at, completed_at)
                                   VALUES (@cid, @sid, @st, @prog, datetime('now','-'||@days||' days'), @cdat)";
        var cid  = enrollCmd.Parameters.Add("@cid",  SqliteType.Integer);
        var sid  = enrollCmd.Parameters.Add("@sid",  SqliteType.Integer);
        var st   = enrollCmd.Parameters.Add("@st",   SqliteType.Text);
        var prog = enrollCmd.Parameters.Add("@prog", SqliteType.Real);
        var enDays = enrollCmd.Parameters.Add("@days", SqliteType.Integer);
        var cdat = enrollCmd.Parameters.Add("@cdat", SqliteType.Text);

        // Insert lesson_progress in bulk after enrollments
        using var lpCmd = conn.CreateCommand();
        lpCmd.CommandText = @"INSERT INTO lesson_progress (enrollment_id, lesson_id, is_completed, watched_seconds, completed_at)
                               SELECT e.id, l.id,
                                      CASE WHEN l.order_num <= CAST(@done AS INTEGER) THEN 1 ELSE 0 END,
                                      CASE WHEN l.order_num <= CAST(@done AS INTEGER) THEN l.duration_minutes * 60 ELSE 0 END,
                                      CASE WHEN l.order_num <= CAST(@done AS INTEGER) THEN datetime('now','-1 day') ELSE NULL END
                               FROM enrollments e
                               JOIN lessons l ON l.course_id = e.course_id
                               WHERE e.id = @eid";
        var lpEid  = lpCmd.Parameters.Add("@eid",  SqliteType.Integer);
        var lpDone = lpCmd.Parameters.Add("@done", SqliteType.Real);

        long enrollmentId = 0;
        var seen = new HashSet<(long, long)>();

        foreach (var (courseId, totalLessons) in courseData)
        {
            // Each course has a realistic number of enrollments
            int courseEnrollments = (int)Math.Min(studentCount, rand.Next(30, 120));
            var usedStudents = new HashSet<long>();

            for (int e = 0; e < courseEnrollments; e++)
            {
                long studentId;
                int attempts = 0;
                do { studentId = rand.Next(1, (int)studentCount + 1); attempts++; }
                while (usedStudents.Contains(studentId) && attempts < 20);
                if (usedStudents.Contains(studentId)) continue;
                usedStudents.Add(studentId);

                var key = (courseId, studentId);
                if (seen.Contains(key)) continue;
                seen.Add(key);

                var status   = statuses[rand.Next(statuses.Length)];
                double progressPct = status == "completed" ? 100.0
                                   : status == "paused"    ? rand.Next(5, 60)
                                   : rand.Next(1, 95);
                int daysAgo  = rand.Next(0, 300);
                string? completedDate = status == "completed"
                    ? $"datetime('now','-{rand.Next(1, daysAgo + 1)} days')"
                    : null;

                cid.Value   = courseId;
                sid.Value   = studentId;
                st.Value    = status;
                prog.Value  = progressPct;
                enDays.Value = daysAgo;
                cdat.Value  = (object?)completedDate ?? DBNull.Value;
                enrollCmd.ExecuteNonQuery();
                enrollmentId++;

                // Minimal lesson_progress — only track for a subset
                if (rand.Next(3) == 0)
                {
                    long eid = enrollmentId;
                    lpEid.Value  = eid;
                    lpDone.Value = Math.Round(progressPct / 100.0 * totalLessons);
                    try { lpCmd.ExecuteNonQuery(); } catch { /* skip constraint errors */ }
                }
            }
        }
    }

    private static void SeedPayments(SqliteConnection conn)
    {
        using var selectCmd = conn.CreateCommand();
        selectCmd.CommandText = @"SELECT e.id, e.student_id, e.course_id, e.enrolled_at, c.price, c.original_price
                                   FROM enrollments e JOIN courses c ON e.course_id = c.id
                                   WHERE e.status IN ('active','completed')";
        var data = new List<(long eid, long sid, long cid, string enrolledAt, double price, double orig)>();
        using (var r = selectCmd.ExecuteReader())
        {
            while (r.Read())
                data.Add((r.GetInt64(0), r.GetInt64(1), r.GetInt64(2), r.GetString(3), r.GetDouble(4), r.GetDouble(5)));
        }

        var rand    = new Random(33);
        var methods = new[] { "bank_transfer", "bank_transfer", "credit_card", "momo", "zalopay", "vnpay" };
        var txBase  = 1000000L;

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"INSERT INTO course_payments
            (enrollment_id, student_id, course_id, amount, original_price, discount_amount, coupon_code, payment_method, status, transaction_ref, paid_at)
            VALUES (@eid, @sid, @cid, @amt, @ori, @dis, @cou, @mth, 'success', @ref, @dat)";

        var eid = cmd.Parameters.Add("@eid", SqliteType.Integer);
        var sidP = cmd.Parameters.Add("@sid", SqliteType.Integer);
        var cidP = cmd.Parameters.Add("@cid", SqliteType.Integer);
        var amt = cmd.Parameters.Add("@amt", SqliteType.Real);
        var oriP = cmd.Parameters.Add("@ori", SqliteType.Real);
        var dis = cmd.Parameters.Add("@dis", SqliteType.Real);
        var cou = cmd.Parameters.Add("@cou", SqliteType.Text);
        var mth = cmd.Parameters.Add("@mth", SqliteType.Text);
        var refP = cmd.Parameters.Add("@ref", SqliteType.Text);
        var dat = cmd.Parameters.Add("@dat", SqliteType.Text);

        foreach (var (eId, sId, cId, enrolledAt, price, orig) in data)
        {
            double discount = rand.Next(0, 5) == 0 ? Math.Round(price * 0.1) : 0;
            eid.Value  = eId;
            sidP.Value = sId;
            cidP.Value = cId;
            amt.Value  = price - discount;
            oriP.Value = orig > 0 ? orig : price;
            dis.Value  = discount;
            cou.Value  = discount > 0 ? "SALE10" : (object)DBNull.Value;
            mth.Value  = methods[rand.Next(methods.Length)];
            refP.Value = $"EVT{txBase + eId:D8}";
            dat.Value  = enrolledAt;
            cmd.ExecuteNonQuery();
        }
    }

    private static void SeedReviews(SqliteConnection conn)
    {
        using var selectCmd = conn.CreateCommand();
        selectCmd.CommandText = @"SELECT e.id, e.course_id, e.student_id
                                   FROM enrollments e WHERE e.status='completed' LIMIT 500";
        var data = new List<(long eid, long cid, long sid)>();
        using (var r = selectCmd.ExecuteReader())
        {
            while (r.Read())
                data.Add((r.GetInt64(0), r.GetInt64(1), r.GetInt64(2)));
        }

        var rand     = new Random(77);
        var comments = new[]
        {
            "Khóa học rất hay, giảng viên giải thích rõ ràng và dễ hiểu.",
            "Nội dung chất lượng, học xong có thể áp dụng ngay vào thực tế.",
            "Tốc độ giảng dạy vừa phải, bài tập thực hành rất hữu ích.",
            "Tôi đã học nhiều khóa nhưng đây là một trong những khóa tốt nhất.",
            "Giảng viên nhiệt tình, hỗ trợ học viên rất tốt.",
            "Nội dung cập nhật theo xu hướng mới nhất, rất phù hợp.",
            "Học xong khóa này tôi đã tự tin hơn rất nhiều.",
            "Rất xứng đáng với số tiền bỏ ra. Highly recommend!",
            "Cấu trúc khóa học logic, từng bước được giải thích cặn kẽ.",
            "Có thể cải thiện thêm phần bài tập cuối chương nhưng nhìn chung rất tốt."
        };

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"INSERT OR IGNORE INTO reviews (course_id, student_id, rating, comment, is_published, created_at)
                            VALUES (@cid, @sid, @rat, @com, 1, datetime('now','-'||@days||' days'))";
        var cid  = cmd.Parameters.Add("@cid",  SqliteType.Integer);
        var sid  = cmd.Parameters.Add("@sid",  SqliteType.Integer);
        var rat  = cmd.Parameters.Add("@rat",  SqliteType.Integer);
        var com  = cmd.Parameters.Add("@com",  SqliteType.Text);
        var days = cmd.Parameters.Add("@days", SqliteType.Integer);

        foreach (var (_, courseId, studentId) in data)
        {
            if (rand.Next(3) != 0) continue; // ~33% leave a review
            cid.Value  = courseId;
            sid.Value  = studentId;
            rat.Value  = rand.Next(4, 6); // 4 or 5 stars
            com.Value  = comments[rand.Next(comments.Length)];
            days.Value = rand.Next(1, 30);
            cmd.ExecuteNonQuery();
        }
    }
}
