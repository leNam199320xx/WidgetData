-- =============================================================================
-- course-test.sql  —  Dữ liệu test cho course.db (EduViet - học trực tuyến)
-- =============================================================================
-- Mục đích : Tạo dữ liệu tối thiểu đủ để test tất cả widget queries của
--            demo course-front (Learning Analytics Dashboard).
-- Cách dùng:
--   sqlite3 course.db < course-test.sql
--   # hoặc trong DB Browser for SQLite: File → Import → Database from SQL file
-- Lưu ý    : Script sẽ xóa và tạo lại toàn bộ bảng (idempotent).
-- =============================================================================

PRAGMA journal_mode=WAL;
PRAGMA foreign_keys=OFF;

-- ─── Drop tables ─────────────────────────────────────────────────────────────
DROP TABLE IF EXISTS reviews;
DROP TABLE IF EXISTS course_payments;
DROP TABLE IF EXISTS lesson_progress;
DROP TABLE IF EXISTS lessons;
DROP TABLE IF EXISTS enrollments;
DROP TABLE IF EXISTS students;
DROP TABLE IF EXISTS courses;
DROP TABLE IF EXISTS instructors;
DROP TABLE IF EXISTS categories;

-- ─── Schema ───────────────────────────────────────────────────────────────────
CREATE TABLE categories (
    id          INTEGER PRIMARY KEY AUTOINCREMENT,
    name        TEXT NOT NULL,
    slug        TEXT NOT NULL UNIQUE,
    description TEXT,
    icon        TEXT,
    sort_order  INTEGER NOT NULL DEFAULT 0,
    created_at  TEXT DEFAULT (datetime('now'))
);

CREATE TABLE instructors (
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

CREATE TABLE courses (
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
    FOREIGN KEY (category_id)   REFERENCES categories(id),
    FOREIGN KEY (instructor_id) REFERENCES instructors(id)
);

CREATE TABLE students (
    id            INTEGER PRIMARY KEY AUTOINCREMENT,
    full_name     TEXT NOT NULL,
    email         TEXT NOT NULL UNIQUE,
    phone         TEXT,
    city          TEXT,
    is_active     INTEGER NOT NULL DEFAULT 1,
    registered_at TEXT DEFAULT (datetime('now'))
);

CREATE TABLE enrollments (
    id               INTEGER PRIMARY KEY AUTOINCREMENT,
    course_id        INTEGER NOT NULL,
    student_id       INTEGER NOT NULL,
    status           TEXT NOT NULL DEFAULT 'active',
    progress_percent REAL NOT NULL DEFAULT 0,
    enrolled_at      TEXT DEFAULT (datetime('now')),
    completed_at     TEXT,
    FOREIGN KEY (course_id)  REFERENCES courses(id),
    FOREIGN KEY (student_id) REFERENCES students(id)
);

CREATE TABLE lessons (
    id               INTEGER PRIMARY KEY AUTOINCREMENT,
    course_id        INTEGER NOT NULL,
    section_title    TEXT,
    title            TEXT NOT NULL,
    order_num        INTEGER NOT NULL DEFAULT 1,
    duration_minutes INTEGER NOT NULL DEFAULT 10,
    lesson_type      TEXT NOT NULL DEFAULT 'video',
    is_free          INTEGER NOT NULL DEFAULT 0,
    is_published     INTEGER NOT NULL DEFAULT 1,
    FOREIGN KEY (course_id) REFERENCES courses(id)
);

CREATE TABLE lesson_progress (
    id              INTEGER PRIMARY KEY AUTOINCREMENT,
    enrollment_id   INTEGER NOT NULL,
    lesson_id       INTEGER NOT NULL,
    is_completed    INTEGER NOT NULL DEFAULT 0,
    watched_seconds INTEGER NOT NULL DEFAULT 0,
    completed_at    TEXT,
    FOREIGN KEY (enrollment_id) REFERENCES enrollments(id),
    FOREIGN KEY (lesson_id)     REFERENCES lessons(id)
);

CREATE TABLE course_payments (
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
    FOREIGN KEY (student_id)    REFERENCES students(id),
    FOREIGN KEY (course_id)     REFERENCES courses(id)
);

CREATE TABLE reviews (
    id           INTEGER PRIMARY KEY AUTOINCREMENT,
    course_id    INTEGER NOT NULL,
    student_id   INTEGER NOT NULL,
    rating       INTEGER NOT NULL DEFAULT 5,
    comment      TEXT,
    is_published INTEGER NOT NULL DEFAULT 1,
    created_at   TEXT DEFAULT (datetime('now')),
    FOREIGN KEY (course_id)  REFERENCES courses(id),
    FOREIGN KEY (student_id) REFERENCES students(id)
);

CREATE INDEX idx_enrollments_course_id   ON enrollments(course_id);
CREATE INDEX idx_enrollments_student_id  ON enrollments(student_id);
CREATE INDEX idx_enrollments_enrolled_at ON enrollments(enrolled_at);
CREATE INDEX idx_course_payments_paid_at ON course_payments(paid_at);
CREATE INDEX idx_courses_category        ON courses(category_id);

-- ─── 1. Danh mục khóa học (categories) ───────────────────────────────────────
INSERT INTO categories (name, slug, description, icon, sort_order) VALUES
    ('Lập trình & Công nghệ',    'lap-trinh-cong-nghe',    'Từ web development đến AI, cloud, data science', '💻', 1),
    ('Kinh doanh & Khởi nghiệp', 'kinh-doanh-khoi-nghiep', 'Xây dựng và phát triển doanh nghiệp',           '💼', 2),
    ('Marketing & Truyền thông', 'marketing-truyen-thong', 'Digital marketing, content, SEO/SEM, ads',       '📣', 3),
    ('Ngoại ngữ',                'ngoai-ngu',              'Tiếng Anh, Nhật, Hàn, Trung và các ngôn ngữ khác','🌏', 4);

-- ─── 2. Giảng viên (instructors) ─────────────────────────────────────────────
INSERT INTO instructors (full_name, email, bio, specialization, rating, total_students, total_courses, joined_at) VALUES
    ('Nguyễn Văn An',  'an.nguyen@eduviet.vn',  'Kỹ sư phần mềm 10 năm kinh nghiệm, chuyên Python & AI.', 'Python, AI/ML, Backend',       4.9, 8200,  6, datetime('now', '-24 months')),
    ('Trần Minh Khoa', 'khoa.tran@eduviet.vn',  'AWS Certified Solutions Architect Professional.',          'Cloud AWS, DevOps, Docker',     4.8, 4600,  5, datetime('now', '-20 months')),
    ('Lê Thành Đạt',   'dat.le@eduviet.vn',     'Frontend engineer 8 năm, tác giả nhiều OSS libraries.',   'React, TypeScript, Vue.js',     4.9, 6800,  7, datetime('now', '-18 months')),
    ('Nguyễn Thị Cẩm', 'cam.nguyen@eduviet.vn', 'Digital marketing expert, quản lý ngân sách >50 tỷ/năm.','Facebook Ads, TikTok, SEM',     4.6, 9200,  9, datetime('now', '-12 months')),
    ('Trần Thị Hằng',  'hang.tran@eduviet.vn',  'Giáo viên IELTS 8.5, từng giảng British Council & IDP.', 'IELTS, TOEIC, Communication',   4.9, 15600,12, datetime('now', '-30 months'));

-- ─── 3. Khóa học (courses) ────────────────────────────────────────────────────
INSERT INTO courses (category_id, instructor_id, title, slug, description, level, price, original_price, duration_hours, total_lessons, is_featured, rating, total_ratings, total_enrollments, published_at) VALUES
    -- Lập trình (cat=1)
    (1, 1, 'Python nâng cao — Design Patterns & Clean Code',      'python-nang-cao',   'Từ pattern cơ bản đến nâng cao qua 72 bài học thực hành.',   'advanced',      699000, 999000,   36, 72, 1, 4.9, 820,  8200, datetime('now', '-180 days')),
    (1, 2, 'AWS Certified Solutions Architect — tiếng Việt',      'aws-saa',           'Nắm vững AWS SAA qua 96 bài giảng video chất lượng cao.',    'intermediate',  899000, 1299000,  48, 96, 1, 4.8, 460,  4600, datetime('now', '-150 days')),
    (1, 3, 'React 19 + TypeScript — xây dựng SPA chuyên nghiệp',  'react-19-ts',       'Lộ trình học React bài bản: 104 bài học, dự án cuối khóa.',  'intermediate',  749000, 1099000,  52,104, 1, 4.9, 680,  6800, datetime('now', '-120 days')),
    (1, 1, 'Machine Learning với Python — từ cơ bản đến thực chiến','ml-python',        'Khám phá ML toàn diện qua 90 bài học video và bài tập.',     'intermediate',  799000, 1199000,  45, 90, 0, 4.9, 550,  5500, datetime('now', '-80 days')),
    -- Kinh doanh (cat=2)
    (2, 1, 'Growth Hacking — tăng trưởng nhanh không tốn tiền',   'growth-hacking',    'Từ người mới đến chuyên nghiệp với Growth Hacking — 44 bài.','all',           399000, 599000,   22, 44, 1, 4.8, 780,  7800, datetime('now', '-160 days')),
    (2, 2, 'Khởi nghiệp từ 0 — Business Model Canvas',            'khoi-nghiep-bmc',   'Nắm vững BMC & Lean Startup qua 60 bài giảng chuyên sâu.',   'beginner',      499000, 699000,   30, 60, 0, 4.7, 540,  5400, datetime('now', '-140 days')),
    -- Marketing (cat=3)
    (3, 4, 'Facebook & TikTok Ads 2026 — chạy ads hiệu quả',      'fb-tiktok-ads',     'Khóa học FB & TikTok Ads bài bản: 50 bài học, 25 giờ.',      'beginner',      349000, 549000,   25, 50, 1, 4.6, 920,  9200, datetime('now', '-75 days')),
    (3, 4, 'TikTok Shop — bán hàng livestream chuyên nghiệp',      'tiktok-shop',       'Từ mới đến thành thạo TikTok Shop qua 28 bài học video.',    'beginner',      249000, 399000,   14, 28, 0, 4.5, 1200,12000, datetime('now', '-45 days')),
    -- Ngoại ngữ (cat=4)
    (4, 5, 'IELTS 7.0+ — lộ trình học từ 0 đến band 7',           'ielts-70',          'Lộ trình học IELTS bài bản qua 120 bài học, 60 giờ.',        'all',           799000, 1099000,  60,120, 1, 4.9, 1560,15600, datetime('now', '-365 days')),
    (4, 5, 'TOEIC 900+ — chinh phục Reading & Listening',          'toeic-900',         'Nắm vững TOEIC qua 80 bài giảng video chất lượng cao.',      'intermediate',  649000, 899000,   40, 80, 1, 4.9, 920,  9200, datetime('now', '-180 days'));

-- ─── 4. Học viên (students) ───────────────────────────────────────────────────
INSERT INTO students (full_name, email, phone, city, registered_at) VALUES
    ('Nguyễn Văn An',     'st001@eduviet.vn', '0912000001', 'Hà Nội',          datetime('now', '-200 days')),
    ('Trần Thị Mai',      'st002@eduviet.vn', '0912000002', 'TP. Hồ Chí Minh', datetime('now', '-180 days')),
    ('Lê Văn Đức',        'st003@eduviet.vn', '0912000003', 'Đà Nẵng',         datetime('now', '-150 days')),
    ('Phạm Thị Hoa',      'st004@eduviet.vn', '0912000004', 'Cần Thơ',         datetime('now', '-120 days')),
    ('Hoàng Văn Minh',    'st005@eduviet.vn', '0912000005', 'Hải Phòng',       datetime('now', '-90 days')),
    ('Vũ Thị Lan',        'st006@eduviet.vn', '0912000006', 'Hà Nội',          datetime('now', '-60 days')),
    ('Đặng Quốc Hùng',    'st007@eduviet.vn', '0912000007', 'TP. Hồ Chí Minh', datetime('now', '-45 days')),
    ('Ngô Thị Phương',    'st008@eduviet.vn', '0912000008', 'Huế',             datetime('now', '-30 days')),
    ('Bùi Thanh Nam',     'st009@eduviet.vn', '0912000009', 'Nha Trang',       datetime('now', '-15 days')),
    ('Đinh Thị Ngọc',     'st010@eduviet.vn', '0912000010', 'Hà Nội',          datetime('now', '-5 days')),
    ('Cao Văn Thịnh',     'st011@eduviet.vn', '0912000011', 'Đà Nẵng',         datetime('now', '-3 days')),
    ('Lý Minh Khoa',      'st012@eduviet.vn', '0912000012', 'TP. Hồ Chí Minh', datetime('now', '-1 day')),
    ('Phan Thị Thu',      'st013@eduviet.vn', '0912000013', 'Hà Nội',          datetime('now', '-1 day')),
    ('Dương Văn Quân',    'st014@eduviet.vn', '0912000014', 'Cần Thơ',         datetime('now')),          -- đăng ký hôm nay
    ('Trần Hoài Nam',     'st015@eduviet.vn', '0912000015', 'Hải Phòng',       datetime('now'));           -- đăng ký hôm nay

-- ─── 5. Đăng ký khóa học (enrollments) ───────────────────────────────────────
-- Bao gồm: completed, active, paused để test tỷ lệ hoàn thành
INSERT INTO enrollments (course_id, student_id, status, progress_percent, enrolled_at, completed_at) VALUES
    -- Khóa IELTS (id=9) — nhiều đăng ký nhất
    (9, 1, 'completed', 100.0, datetime('now', '-180 days'), datetime('now', '-30 days')),
    (9, 2, 'completed', 100.0, datetime('now', '-170 days'), datetime('now', '-20 days')),
    (9, 3, 'active',     72.0, datetime('now', '-120 days'), NULL),
    (9, 4, 'active',     45.0, datetime('now',  '-90 days'), NULL),
    (9, 5, 'paused',     15.0, datetime('now',  '-60 days'), NULL),
    -- Python nâng cao (id=1)
    (1, 1, 'completed', 100.0, datetime('now', '-160 days'), datetime('now', '-60 days')),
    (1, 6, 'completed', 100.0, datetime('now', '-140 days'), datetime('now', '-40 days')),
    (1, 7, 'active',     58.0, datetime('now',  '-80 days'), NULL),
    (1, 8, 'active',     32.0, datetime('now',  '-50 days'), NULL),
    -- React 19 (id=3)
    (3, 2, 'active',     88.0, datetime('now', '-100 days'), NULL),
    (3, 3, 'active',     61.0, datetime('now',  '-80 days'), NULL),
    (3, 9, 'paused',     25.0, datetime('now',  '-40 days'), NULL),
    -- AWS (id=2)
    (2, 4, 'completed', 100.0, datetime('now', '-120 days'), datetime('now', '-10 days')),
    (2, 5, 'active',     79.0, datetime('now',  '-70 days'), NULL),
    -- Growth Hacking (id=5)
    (5, 6, 'active',     42.0, datetime('now',  '-60 days'), NULL),
    (5, 7, 'completed', 100.0, datetime('now',  '-90 days'), datetime('now', '-15 days')),
    -- TikTok Ads (id=7)
    (7,10, 'active',     18.0, datetime('now',  '-20 days'), NULL),
    (7,11, 'active',      5.0, datetime('now',   '-5 days'), NULL),
    -- TOEIC (id=10)
    (10, 1,'active',     92.0, datetime('now', '-150 days'), NULL),
    (10, 8,'active',     44.0, datetime('now',  '-40 days'), NULL),
    -- ML Python (id=4)
    (4, 9, 'active',     67.0, datetime('now',  '-55 days'), NULL),
    -- TikTok Shop (id=8)
    (8,12, 'active',     30.0, datetime('now',  '-10 days'), NULL),
    -- Đăng ký HÔM NAY (để test widget "Đăng ký mới hôm nay")
    (1,14, 'active',      0.0, datetime('now'), NULL),
    (9,15, 'active',      0.0, datetime('now'), NULL),
    (7,13, 'active',      0.0, datetime('now'), NULL);

-- ─── 6. Bài học mẫu (lessons) — 5 bài đầu của mỗi khóa ──────────────────────
INSERT INTO lessons (course_id, section_title, title, order_num, duration_minutes, lesson_type, is_free) VALUES
    (1, 'Phần 1: Giới thiệu', 'Bài 1: Tổng quan khóa học',         1, 8,  'video', 1),
    (1, 'Phần 1: Giới thiệu', 'Bài 2: Cài đặt môi trường',         2, 12, 'video', 1),
    (1, 'Phần 2: Design Patterns', 'Bài 3: Singleton Pattern',      3, 18, 'video', 0),
    (1, 'Phần 2: Design Patterns', 'Bài 4: Factory Pattern',        4, 20, 'video', 0),
    (1, 'Phần 2: Design Patterns', 'Bài 5: Observer Pattern',       5, 22, 'video', 0),
    (9, 'Phần 1: Listening',  'Bài 1: IELTS Overview',              1, 10, 'video', 1),
    (9, 'Phần 1: Listening',  'Bài 2: Listening Section 1',         2, 15, 'video', 1),
    (9, 'Phần 2: Reading',    'Bài 3: Reading Strategies',          3, 20, 'video', 0),
    (9, 'Phần 2: Reading',    'Bài 4: True/False/Not Given',        4, 18, 'video', 0),
    (9, 'Phần 3: Writing',    'Bài 5: Task 1 — Bar Chart',          5, 25, 'video', 0);

-- ─── 7. Thanh toán khóa học (course_payments) ────────────────────────────────
-- Nhiều tháng để test trend chart
INSERT INTO course_payments (enrollment_id, student_id, course_id, amount, original_price, discount_amount, payment_method, status, transaction_ref, paid_at) VALUES
    (1,  1, 9, 799000, 1099000,       0, 'bank_transfer', 'success', 'EVT-IELTS-001', datetime('now', '-180 days')),
    (2,  2, 9, 799000, 1099000,       0, 'momo',          'success', 'EVT-IELTS-002', datetime('now', '-170 days')),
    (6,  1, 1, 699000,  999000,       0, 'bank_transfer', 'success', 'EVT-PYTH-006', datetime('now', '-160 days')),
    (7,  6, 1, 699000,  999000,       0, 'credit_card',   'success', 'EVT-PYTH-007', datetime('now', '-140 days')),
    (13, 4, 2, 899000, 1299000,       0, 'zalopay',       'success', 'EVT-AWS-013',  datetime('now', '-120 days')),
    (10, 2, 3, 749000, 1099000,       0, 'vnpay',         'success', 'EVT-RCTS-010', datetime('now', '-100 days')),
    (16, 7, 5, 399000,  599000,       0, 'momo',          'success', 'EVT-GH-016',   datetime('now',  '-90 days')),
    (11, 3, 3, 749000, 1099000,       0, 'credit_card',   'success', 'EVT-RCTS-011', datetime('now',  '-80 days')),
    (20, 1,10, 649000,  899000,       0, 'bank_transfer', 'success', 'EVT-TOE-020',  datetime('now',  '-70 days')),
    (21, 9, 4, 799000, 1199000,       0, 'momo',          'success', 'EVT-ML-021',   datetime('now',  '-55 days')),
    (15, 6, 5, 359000,  599000,  40000, 'zalopay',        'success', 'EVT-GH-015',   datetime('now',  '-45 days')), -- có coupon
    (17,10, 7, 349000,  549000,       0, 'vnpay',         'success', 'EVT-FB-017',   datetime('now',  '-20 days')),
    (22,12, 8, 249000,  399000,       0, 'momo',          'success', 'EVT-TTK-022',  datetime('now',  '-10 days')),
    (14, 5, 2, 899000, 1299000,       0, 'credit_card',   'success', 'EVT-AWS-014',  datetime('now',   '-5 days')),
    -- Thanh toán HÔM NAY (để test widget "Doanh thu hôm nay")
    (23,14, 1, 699000,  999000,       0, 'bank_transfer', 'success', 'EVT-PYTH-023', datetime('now')),
    (24,15, 9, 799000, 1099000,       0, 'momo',          'success', 'EVT-IELTS-024',datetime('now')),
    (25,13, 7, 349000,  549000,       0, 'vnpay',         'success', 'EVT-FB-025',   datetime('now'));

-- ─── 8. Đánh giá (reviews) ────────────────────────────────────────────────────
INSERT INTO reviews (course_id, student_id, rating, comment) VALUES
    (9, 1, 5, 'Khóa học rất hay, giảng viên giải thích rõ ràng. Tôi đã đạt IELTS 7.5 sau 3 tháng học.'),
    (9, 2, 5, 'Nội dung chất lượng, học xong có thể áp dụng ngay vào thực tế. Highly recommend!'),
    (1, 1, 5, 'Tốc độ giảng dạy vừa phải, bài tập thực hành rất hữu ích. 5 sao xứng đáng.'),
    (1, 6, 4, 'Nội dung tốt, tuy nhiên có thể cải thiện thêm phần bài tập cuối chương.'),
    (3, 2, 5, 'Giảng viên nhiệt tình, hỗ trợ học viên rất tốt. Học xong tự làm được project React.'),
    (2, 4, 5, 'Khóa AWS rất thực chiến, pass exam lần đầu nhờ khóa này.'),
    (5, 7, 4, 'Góc nhìn của tác giả rất thú vị, nhiều case study thực tế từ Việt Nam.');

-- ─── Kiểm tra nhanh ──────────────────────────────────────────────────────────
-- SELECT COUNT(*) FROM enrollments;                                → 25
-- SELECT COUNT(*) FROM enrollments WHERE date(enrolled_at)=date('now');  → 3 (hôm nay)
-- SELECT COUNT(*) FROM courses WHERE is_published=1;              → 10
-- SELECT ROUND(100.0*SUM(CASE WHEN status='completed' THEN 1 ELSE 0 END)/NULLIF(COUNT(*),0),1) FROM enrollments; → ~28%
-- SELECT COALESCE(ROUND(SUM(amount),0),0) FROM course_payments WHERE status='success' AND date(paid_at)=date('now'); → 1847000
-- SELECT c.name, COUNT(e.id) FROM enrollments e JOIN courses co ON e.course_id=co.id JOIN categories c ON co.category_id=c.id GROUP BY c.name;
