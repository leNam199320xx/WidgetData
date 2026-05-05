-- =============================================================================
-- news-test.sql  —  Dữ liệu test cho news.db (VietNews - cổng tin tức)
-- =============================================================================
-- Mục đích : Tạo dữ liệu tối thiểu đủ để test tất cả widget queries của
--            demo news-front (News Analytics Dashboard).
-- Cách dùng:
--   sqlite3 news.db < news-test.sql
--   # hoặc trong DB Browser for SQLite: File → Import → Database from SQL file
-- Lưu ý    : Script sẽ xóa và tạo lại toàn bộ bảng (idempotent).
-- =============================================================================

PRAGMA journal_mode=WAL;
PRAGMA foreign_keys=OFF;

-- ─── Drop tables ─────────────────────────────────────────────────────────────
DROP TABLE IF EXISTS comments;
DROP TABLE IF EXISTS article_views;
DROP TABLE IF EXISTS readers;
DROP TABLE IF EXISTS articles;
DROP TABLE IF EXISTS authors;
DROP TABLE IF EXISTS categories;

-- ─── Schema ───────────────────────────────────────────────────────────────────
CREATE TABLE categories (
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

CREATE TABLE authors (
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

CREATE TABLE articles (
    id                INTEGER PRIMARY KEY AUTOINCREMENT,
    category_id       INTEGER NOT NULL,
    author_id         INTEGER NOT NULL,
    title             TEXT NOT NULL,
    slug              TEXT NOT NULL UNIQUE,
    excerpt           TEXT,
    word_count        INTEGER NOT NULL DEFAULT 0,
    status            TEXT NOT NULL DEFAULT 'published',
    is_featured       INTEGER NOT NULL DEFAULT 0,
    view_count        INTEGER NOT NULL DEFAULT 0,
    comment_count     INTEGER NOT NULL DEFAULT 0,
    share_count       INTEGER NOT NULL DEFAULT 0,
    read_time_minutes INTEGER NOT NULL DEFAULT 3,
    published_at      TEXT,
    created_at        TEXT DEFAULT (datetime('now')),
    updated_at        TEXT DEFAULT (datetime('now')),
    FOREIGN KEY (category_id) REFERENCES categories(id),
    FOREIGN KEY (author_id)   REFERENCES authors(id)
);

CREATE TABLE readers (
    id             INTEGER PRIMARY KEY AUTOINCREMENT,
    full_name      TEXT NOT NULL,
    email          TEXT NOT NULL UNIQUE,
    city           TEXT,
    is_subscribed  INTEGER NOT NULL DEFAULT 0,
    total_reads    INTEGER NOT NULL DEFAULT 0,
    total_comments INTEGER NOT NULL DEFAULT 0,
    registered_at  TEXT DEFAULT (datetime('now'))
);

CREATE TABLE article_views (
    id                      INTEGER PRIMARY KEY AUTOINCREMENT,
    article_id              INTEGER NOT NULL,
    reader_id               INTEGER,
    source                  TEXT NOT NULL DEFAULT 'direct',
    device                  TEXT NOT NULL DEFAULT 'desktop',
    country                 TEXT NOT NULL DEFAULT 'VN',
    read_completion_percent REAL NOT NULL DEFAULT 0,
    viewed_at               TEXT DEFAULT (datetime('now')),
    FOREIGN KEY (article_id) REFERENCES articles(id),
    FOREIGN KEY (reader_id)  REFERENCES readers(id)
);

CREATE TABLE comments (
    id          INTEGER PRIMARY KEY AUTOINCREMENT,
    article_id  INTEGER NOT NULL,
    reader_id   INTEGER NOT NULL,
    content     TEXT NOT NULL,
    is_approved INTEGER NOT NULL DEFAULT 1,
    created_at  TEXT DEFAULT (datetime('now')),
    FOREIGN KEY (article_id) REFERENCES articles(id),
    FOREIGN KEY (reader_id)  REFERENCES readers(id)
);

CREATE INDEX idx_articles_category     ON articles(category_id);
CREATE INDEX idx_articles_published    ON articles(published_at);
CREATE INDEX idx_article_views_article ON article_views(article_id);
CREATE INDEX idx_article_views_viewed  ON article_views(viewed_at);
CREATE INDEX idx_comments_article      ON comments(article_id);

-- ─── 1. Chuyên mục (categories) ──────────────────────────────────────────────
INSERT INTO categories (name, slug, description, color, sort_order) VALUES
    ('Công nghệ', 'cong-nghe', 'Tin tức về công nghệ, AI, phần mềm, thiết bị điện tử', '#3182ce', 1),
    ('Kinh tế',   'kinh-te',   'Thị trường tài chính, bất động sản, doanh nghiệp',     '#38a169', 2),
    ('Thể thao',  'the-thao',  'Bóng đá, quần vợt, SEA Games và các môn thể thao',     '#e53e3e', 3),
    ('Giải trí',  'giai-tri',  'Âm nhạc, điện ảnh, nghệ thuật, người nổi tiếng',       '#d69e2e', 4),
    ('Xã hội',    'xa-hoi',    'Tin tức xã hội, đời sống, cộng đồng',                  '#805ad5', 5),
    ('Sức khỏe',  'suc-khoe',  'Y tế, dinh dưỡng, lối sống lành mạnh',                '#dd6b20', 6);

-- ─── 2. Tác giả (authors) ────────────────────────────────────────────────────
INSERT INTO authors (full_name, email, bio, total_articles, total_views, joined_at) VALUES
    ('Nguyễn Minh Tuấn', 'tuan.nguyen@vietnews.vn', 'Phóng viên công nghệ 8 năm kinh nghiệm.',            18, 45000, datetime('now', '-36 months')),
    ('Trần Thị Hoa',     'hoa.tran@vietnews.vn',    'Chuyên gia kinh tế, cựu giám đốc ngân hàng.',         14, 38000, datetime('now', '-30 months')),
    ('Lê Văn Hùng',      'hung.le@vietnews.vn',     'Phóng viên thể thao, chuyên bóng đá Việt Nam.',       20, 52000, datetime('now', '-24 months')),
    ('Phạm Thị Lan',     'lan.pham@vietnews.vn',    'Biên tập viên giải trí, chuyên điện ảnh và âm nhạc.',12, 31000, datetime('now', '-18 months')),
    ('Hoàng Văn Đức',    'duc.hoang@vietnews.vn',   'Nhà báo xã hội và phóng viên mảng sức khỏe.',        10, 22000, datetime('now', '-15 months'));

-- ─── 3. Bài viết (articles) ──────────────────────────────────────────────────
INSERT INTO articles (category_id, author_id, title, slug, excerpt, word_count, is_featured, view_count, comment_count, share_count, read_time_minutes, published_at) VALUES
    -- Công nghệ (cat=1)
    (1, 1, 'ChatGPT-5 chính thức ra mắt — thay đổi cuộc chơi AI toàn cầu',
           'bai-0001-cong-nghe-ai', 'ChatGPT-5 với khả năng suy luận vượt trội...', 1200, 1, 45200, 234, 1820, 5, datetime('now', '-1 day')),
    (1, 1, 'Việt Nam tăng tốc phát triển AI quốc gia — mục tiêu top 50 thế giới',
           'bai-0002-vn-ai', 'Chính phủ Việt Nam công bố chiến lược AI...', 1100, 1, 29400, 156, 1100, 5, datetime('now', '-5 days')),
    (1, 1, 'Top 10 laptop cho lập trình viên 2026 — so sánh chi tiết',
           'bai-0003-laptop-dev', 'MacBook M3, Dell XPS, ThinkPad X1 Carbon...', 1800, 0, 18700,  76,  720, 8, datetime('now', '-10 days')),
    (1, 1, 'Gemini Ultra 2 vs GPT-5: AI nào thông minh hơn?',
           'bai-0004-gemini-gpt5', 'Phân tích chi tiết benchmark 2 AI mạnh nhất...', 1400, 0, 31200, 198, 1250, 6, datetime('now', '-4 days')),
    (1, 1, '5G phủ sóng toàn quốc năm 2026 — cơ hội và thách thức',
           'bai-0005-5g-vn', 'Các nhà mạng lớn cam kết phủ sóng 5G...', 900, 0, 22100,  98,  880, 4, datetime('now', '-7 days')),
    -- Kinh tế (cat=2)
    (2, 2, 'VN-Index phá đỉnh lịch sử 1.600 điểm — chứng khoán Việt Nam bứt phá',
           'bai-0006-vnindex-1600', 'Phiên giao dịch hôm nay VN-Index vượt ngưỡng...', 1000, 1, 42000, 312, 1680, 4, datetime('now', '-2 days')),
    (2, 2, 'Giá vàng SJC lập đỉnh mới — nên mua hay chờ?',
           'bai-0007-vang-sjc', 'Vàng SJC chạm mốc 120 triệu đồng/lượng...', 850, 1, 38700, 287, 1540, 4, datetime('now', '-1 day')),
    (2, 2, 'Bất động sản Hà Nội và TP.HCM — xu hướng giá 2026',
           'bai-0008-bds-2026', 'Thị trường BDS đang trong giai đoạn hồi phục...', 1200, 0, 28900, 178, 1120, 5, datetime('now', '-6 days')),
    (2, 2, 'Đầu tư FDI vào Việt Nam tăng kỷ lục — ngành nào hưởng lợi?',
           'bai-0009-fdi-vn', 'FDI đạt 24 tỷ USD trong 4 tháng đầu năm...', 1000, 0, 18900,  98,  760, 4, datetime('now', '-7 days')),
    -- Thể thao (cat=3)
    (3, 3, 'Đội tuyển Việt Nam vào bán kết AFF Cup 2026 — chiến thắng lịch sử',
           'bai-0010-vn-aff-2026', 'Bàn thắng phút 89 đưa Việt Nam vào bán kết...', 800, 1, 68900, 567, 2750, 3, datetime('now', '-1 day')),
    (3, 3, 'SEA Games 34: Đoàn Việt Nam dẫn đầu bảng tổng sắp huy chương',
           'bai-0011-seagames34', 'Sau 5 ngày thi đấu, Việt Nam giành 42 HCV...', 700, 1, 52300, 423, 2100, 3, datetime('now', '-3 days')),
    (3, 3, 'Premier League 2025/26 — cuộc đua vô địch hấp dẫn nhất thập kỷ',
           'bai-0012-pl-2526', 'Arsenal, Man City, Liverpool cách nhau 2 điểm...', 950, 0, 38700, 289, 1540, 4, datetime('now', '-4 days')),
    (3, 3, 'World Cup 2026 — tất cả những gì cần biết',
           'bai-0013-wc2026', 'World Cup đầu tiên tổ chức ở 3 quốc gia...', 1400, 1, 35600, 234, 1420, 6, datetime('now', '-10 days')),
    -- Giải trí (cat=4)
    (4, 4, 'Taylor Swift Eras Tour Việt Nam — giá vé và cách mua',
           'bai-0014-ts-vn', 'Buổi diễn lịch sử tại Mỹ Đình vào tháng 10...', 600, 1, 45600, 567, 2280, 3, datetime('now', '-1 day')),
    (4, 4, 'BTS ra mắt album comeback — kỷ lục streaming mới',
           'bai-0015-bts-album', 'Album mới của BTS đạt 50 triệu stream ngay ngày đầu...', 750, 1, 48900, 389, 1960, 3, datetime('now', '-2 days')),
    (4, 4, 'Phim Việt Đất Rừng Phương Nam 2 phá kỷ lục phòng vé nội địa',
           'bai-0016-drpn2', 'Doanh thu 200 tỷ đồng sau 10 ngày công chiếu...', 800, 0, 42100, 312, 1680, 3, datetime('now', '-4 days')),
    -- Xã hội (cat=5)
    (5, 5, 'Bão số 5 đổ bộ miền Trung — thống kê thiệt hại và cứu trợ',
           'bai-0017-bao-5', 'Bão đổ bộ vào Đà Nẵng lúc 2h sáng với cấp 13...', 900, 1, 38700, 298, 1548, 4, datetime('now', '-2 days')),
    (5, 5, 'Cháy chung cư tại Hà Nội — 5 nạn nhân được cứu thoát',
           'bai-0018-chay-cc', 'Vụ cháy xảy ra vào lúc 3h sáng tại chung cư...', 700, 1, 45600, 423, 1824, 3, datetime('now', '-1 day')),
    -- Sức khỏe (cat=6)
    (6, 5, 'Bệnh tay chân miệng bùng phát tại các tỉnh phía Nam',
           'bai-0019-tcm', 'Số ca mắc tay chân miệng tăng 40% so với cùng kỳ...', 800, 1, 38700, 289, 1548, 3, datetime('now', '-3 days')),
    (6, 5, 'Vắc-xin phòng ung thư cổ tử cung — cách tiêm và đối tượng ưu tiên',
           'bai-0020-vacxin-utctc', 'Bộ Y tế triển khai tiêm miễn phí cho trẻ em 9-14 tuổi...', 950, 0, 28900, 198, 1156, 4, datetime('now', '-5 days')),
    -- Bài đăng HÔM NAY (để test widget "Bài đăng hôm nay")
    (1, 1, 'Apple WWDC 2026 — những tính năng iOS 20 mới nhất',
           'bai-0021-ios20-wwdc', 'Apple giới thiệu iOS 20 với nhiều tính năng AI...', 1000, 0,  500,  12,   80, 4, datetime('now')),
    (3, 3, 'Hà Nội FC vs TP.HCM FC — Siêu cúp quốc gia 2026',
           'bai-0022-hn-hcm-sieu-cup', 'Trận chung kết siêu hấp dẫn tối nay...', 600, 0, 2400,  45,  180, 3, datetime('now')),
    (2, 2, 'Ngân hàng Nhà nước giảm lãi suất điều hành xuống 4%',
           'bai-0023-nhnn-laisuat', 'Quyết định bất ngờ của NHNN trong phiên họp sáng nay...', 850, 0, 1800,  32,  140, 4, datetime('now'));

-- ─── 4. Độc giả (readers) ────────────────────────────────────────────────────
INSERT INTO readers (full_name, email, city, is_subscribed, total_reads, registered_at) VALUES
    ('Nguyễn Văn An',   'reader001@gmail.com', 'Hà Nội',          1, 85, datetime('now', '-300 days')),
    ('Trần Thị Mai',    'reader002@gmail.com', 'TP. Hồ Chí Minh', 1, 67, datetime('now', '-250 days')),
    ('Lê Văn Đức',      'reader003@gmail.com', 'Đà Nẵng',         0, 42, datetime('now', '-200 days')),
    ('Phạm Thị Hoa',    'reader004@gmail.com', 'Cần Thơ',         1, 38, datetime('now', '-180 days')),
    ('Hoàng Văn Minh',  'reader005@gmail.com', 'Hải Phòng',       0, 29, datetime('now', '-150 days')),
    ('Vũ Thị Lan',      'reader006@gmail.com', 'Hà Nội',          1, 55, datetime('now', '-120 days')),
    ('Đặng Quốc Hùng',  'reader007@gmail.com', 'TP. Hồ Chí Minh', 0, 21, datetime('now', '-90 days')),
    ('Ngô Thị Phương',  'reader008@gmail.com', 'Huế',             1, 44, datetime('now', '-60 days')),
    ('Bùi Thanh Nam',   'reader009@gmail.com', 'Nha Trang',       0, 18, datetime('now', '-45 days')),
    ('Đinh Thị Ngọc',   'reader010@gmail.com', 'Hà Nội',          1, 31, datetime('now', '-30 days')),
    -- Đăng ký HÔM NAY (để test widget "Độc giả mới")
    ('Lý Minh Khoa',    'reader011@gmail.com', 'TP. Hồ Chí Minh', 0,  0, datetime('now')),
    ('Phan Thị Thu',    'reader012@gmail.com', 'Hà Nội',          1,  0, datetime('now')),
    ('Dương Văn Quân',  'reader013@gmail.com', 'Đà Nẵng',         0,  0, datetime('now'));

-- ─── 5. Lượt xem bài viết (article_views) ────────────────────────────────────
-- Phân bổ sources: direct, google, social, email, referral
-- Phân bổ devices: mobile, desktop, tablet
-- Bao gồm lượt xem HÔM NAY để test widget "Tổng lượt xem hôm nay"
-- Bao gồm lượt xem 7 ngày để test "Bài viết phổ biến nhất trong tuần"
-- Bao gồm lượt xem nhiều tháng để test "Xu hướng lượt xem theo tháng"

-- Hôm nay
INSERT INTO article_views (article_id, reader_id, source, device, read_completion_percent, viewed_at) VALUES
    (10, 1,    'social',   'mobile',  95.0, datetime('now', '-1 hour')),
    (14, 2,    'google',   'desktop', 82.0, datetime('now', '-1 hour')),
    (18, NULL, 'direct',   'mobile',  100.0,datetime('now', '-2 hours')),
    (21, 3,    'google',   'desktop', 71.0, datetime('now', '-2 hours')),
    (22, 4,    'social',   'mobile',  88.0, datetime('now', '-2 hours')),
    (7,  5,    'direct',   'tablet',  65.0, datetime('now', '-3 hours')),
    (6,  NULL, 'google',   'desktop', 45.0, datetime('now', '-3 hours')),
    (15, 6,    'social',   'mobile',  100.0,datetime('now', '-4 hours')),
    (10, 7,    'referral', 'desktop', 55.0, datetime('now', '-4 hours')),
    (23, 1,    'direct',   'mobile',  78.0, datetime('now', '-5 hours')),
    (1,  8,    'email',    'desktop', 90.0, datetime('now', '-5 hours')),
    (17, NULL, 'social',   'mobile',  100.0,datetime('now', '-6 hours')),
    (7,  9,    'google',   'mobile',  33.0, datetime('now', '-6 hours')),
    (11, 2,    'direct',   'desktop', 85.0, datetime('now', '-7 hours')),
    (21, 10,   'google',   'tablet',  62.0, datetime('now', '-7 hours')),
    (6,  3,    'social',   'mobile',  74.0, datetime('now', '-8 hours')),
    (14, NULL, 'referral', 'desktop', 40.0, datetime('now', '-8 hours')),
    (22, 4,    'google',   'mobile',  91.0, datetime('now', '-9 hours')),
    (1,  5,    'direct',   'mobile',  100.0,datetime('now', '-9 hours')),
    (10, 6,    'social',   'desktop', 100.0,datetime('now','-10 hours'));

-- 7 ngày qua (bài trong tuần — để test top articles)
INSERT INTO article_views (article_id, reader_id, source, device, read_completion_percent, viewed_at) VALUES
    (10, 1, 'social',   'mobile',  100.0, datetime('now', '-1 day')),
    (10, 2, 'google',   'desktop',  92.0, datetime('now', '-1 day')),
    (10, 3, 'direct',   'mobile',   88.0, datetime('now', '-2 days')),
    (10, 4, 'social',   'tablet',   95.0, datetime('now', '-2 days')),
    (10, 5, 'referral', 'desktop',  80.0, datetime('now', '-3 days')),
    (7,  1, 'google',   'mobile',   75.0, datetime('now', '-1 day')),
    (7,  2, 'direct',   'desktop',  68.0, datetime('now', '-1 day')),
    (7,  6, 'social',   'mobile',   90.0, datetime('now', '-2 days')),
    (14, 7, 'google',   'desktop',  85.0, datetime('now', '-1 day')),
    (14, 8, 'social',   'mobile',   78.0, datetime('now', '-2 days')),
    (14, 9, 'email',    'desktop',  100.0,datetime('now', '-3 days')),
    (11,10, 'social',   'mobile',   95.0, datetime('now', '-3 days')),
    (11, 1, 'google',   'tablet',   82.0, datetime('now', '-4 days')),
    (15, 2, 'social',   'mobile',   100.0,datetime('now', '-2 days')),
    (15, 3, 'direct',   'desktop',  71.0, datetime('now', '-3 days')),
    (18, 4, 'google',   'mobile',   93.0, datetime('now', '-1 day')),
    (6,  5, 'google',   'desktop',  55.0, datetime('now', '-4 days')),
    (6,  6, 'direct',   'mobile',   48.0, datetime('now', '-5 days')),
    (1,  7, 'email',    'desktop',  100.0,datetime('now', '-5 days')),
    (4,  8, 'google',   'mobile',   77.0, datetime('now', '-6 days'));

-- 30 ngày qua (tháng trước)
INSERT INTO article_views (article_id, reader_id, source, device, read_completion_percent, viewed_at) VALUES
    (1,  1, 'google', 'desktop', 85.0, datetime('now', '-15 days')),
    (2,  2, 'social', 'mobile',  72.0, datetime('now', '-12 days')),
    (3,  3, 'direct', 'desktop', 68.0, datetime('now', '-20 days')),
    (4,  4, 'google', 'mobile',  91.0, datetime('now', '-18 days')),
    (5,  5, 'email',  'tablet',  55.0, datetime('now', '-25 days')),
    (6,  6, 'social', 'mobile',  80.0, datetime('now', '-22 days')),
    (7,  7, 'google', 'desktop', 64.0, datetime('now', '-28 days')),
    (8,  8, 'direct', 'mobile',  77.0, datetime('now', '-16 days')),
    (9,  9, 'social', 'desktop', 95.0, datetime('now', '-14 days')),
    (10,10, 'google', 'mobile',  100.0,datetime('now', '-11 days')),
    (11, 1, 'direct', 'tablet',  83.0, datetime('now', '-13 days')),
    (12, 2, 'email',  'desktop', 59.0, datetime('now', '-19 days')),
    (13, 3, 'social', 'mobile',  74.0, datetime('now', '-24 days')),
    (14, 4, 'google', 'desktop', 88.0, datetime('now', '-26 days')),
    (15, 5, 'direct', 'mobile',  100.0,datetime('now', '-17 days'));

-- 2 tháng qua
INSERT INTO article_views (article_id, reader_id, source, device, read_completion_percent, viewed_at) VALUES
    (1,  1, 'google', 'desktop', 80.0, datetime('now', '-40 days')),
    (2,  2, 'social', 'mobile',  65.0, datetime('now', '-45 days')),
    (3,  3, 'direct', 'desktop', 70.0, datetime('now', '-50 days')),
    (4,  4, 'email',  'tablet',  90.0, datetime('now', '-55 days')),
    (5,  5, 'google', 'mobile',  75.0, datetime('now', '-60 days')),
    (6,  6, 'social', 'desktop', 85.0, datetime('now', '-35 days')),
    (7,  7, 'direct', 'mobile',  60.0, datetime('now', '-42 days')),
    (8,  8, 'google', 'desktop', 78.0, datetime('now', '-48 days')),
    (9,  9, 'email',  'mobile',  92.0, datetime('now', '-52 days')),
    (10,10, 'social', 'tablet',  100.0,datetime('now', '-38 days'));

-- 3 tháng qua
INSERT INTO article_views (article_id, reader_id, source, device, read_completion_percent, viewed_at) VALUES
    (1,  1, 'google', 'desktop', 82.0, datetime('now', '-75 days')),
    (2,  2, 'direct', 'mobile',  68.0, datetime('now', '-80 days')),
    (3,  3, 'social', 'desktop', 74.0, datetime('now', '-85 days')),
    (4,  4, 'email',  'tablet',  88.0, datetime('now', '-90 days')),
    (5,  5, 'google', 'mobile',  77.0, datetime('now', '-95 days'));

-- ─── 6. Bình luận (comments) ──────────────────────────────────────────────────
INSERT INTO comments (article_id, reader_id, content, created_at) VALUES
    (10, 1, 'Tin tuyệt vời! Hy vọng đội tuyển tiếp tục phát huy.', datetime('now', '-2 hours')),
    (10, 2, 'Bàn thắng đó quá đẹp. Cầu thủ xuất sắc!', datetime('now', '-3 hours')),
    (7,  3, 'Giá vàng tăng quá mạnh, lo ngại lạm phát.', datetime('now', '-4 hours')),
    (7,  4, 'Tôi vừa bán vàng sáng nay, thấy quyết định đúng!', datetime('now', '-4 hours')),
    (1,  5, 'ChatGPT-5 thực sự ấn tượng hơn rất nhiều so với phiên bản cũ.', datetime('now', '-5 hours')),
    (14, 6, 'Mua vé bằng cách nào ạ? Ticketbox đã mở chưa?', datetime('now', '-6 hours')),
    (14, 7, 'Giá vé quá đắt so với thu nhập trung bình ở Việt Nam.', datetime('now', '-6 hours')),
    (6,  8, 'VN-Index 1600 là cột mốc lịch sử. Thị trường đang rất tốt.', datetime('now', '-7 hours')),
    (11, 9, 'Đoàn Thể thao VN đang thi đấu rất xuất sắc tại SEA Games.', datetime('now', '-8 hours')),
    (15,10, 'BTS comeback sau quân ngũ, fan Việt đang rất phấn khích!', datetime('now', '-9 hours')),
    (18, 1, 'Cầu mong các nạn nhân đều bình an.', datetime('now', '-1 hour')),
    (17, 2, 'Bão về, mọi người chú ý giữ an toàn.', datetime('now', '-2 hours')),
    (19, 3, 'Phụ huynh cần chú ý vệ sinh tay chân cho trẻ.', datetime('now', '-3 hours'));

-- ─── 7. Cập nhật tổng hợp ────────────────────────────────────────────────────
UPDATE categories SET article_count = (
    SELECT COUNT(*) FROM articles WHERE articles.category_id = categories.id
);

UPDATE authors SET total_articles = (
    SELECT COUNT(*) FROM articles WHERE articles.author_id = authors.id
);

UPDATE readers SET total_comments = (
    SELECT COUNT(*) FROM comments WHERE comments.reader_id = readers.id
);

UPDATE readers SET total_reads = (
    SELECT COUNT(*) FROM article_views WHERE article_views.reader_id = readers.id
);

-- ─── Kiểm tra nhanh ──────────────────────────────────────────────────────────
-- SELECT COUNT(*) FROM article_views WHERE date(viewed_at)=date('now');   → 20
-- SELECT COUNT(*) FROM articles WHERE date(published_at)=date('now');     → 3
-- SELECT COUNT(*) FROM readers WHERE date(registered_at)=date('now');     → 3
-- SELECT ROUND(AVG(read_completion_percent),1) FROM article_views WHERE viewed_at>=datetime('now','-7 days');
-- SELECT c.name, COUNT(av.id) FROM article_views av JOIN articles a ON av.article_id=a.id JOIN categories c ON a.category_id=c.id GROUP BY c.name ORDER BY COUNT(av.id) DESC;
-- SELECT source, COUNT(*) FROM article_views GROUP BY source ORDER BY COUNT(*) DESC;
