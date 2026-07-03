/**
 * data.js — CME Smart Tracker
 * Dữ liệu mẫu cho hệ thống quản lý đào tạo liên tục
 * Bệnh viện Hoàn Mỹ Đồng Nai
 *
 * Cấu trúc dữ liệu (Database Schema):
 *  - employees: { id, fullName, gender, dob, department, role, joinDate, status }
 *  - trainings:  { id, employeeId, courseName, organizer, hours, issueDate, expiryDate }
 *  - departments: [string]
 *  - cmeRequirements: { 1: 24, 2: 48, 5: 120 }
 *  - systemSettings: { warn30, warn60, req1y, req2y, req5y }
 */

// ─── Phòng Ban ──────────────────────────────────────────────
const defaultDepartments = [
  'Khoa Ngoại Tổng hợp',
  'Khoa Nội Tổng hợp',
  'Khoa Sản',
  'Khoa Nhi',
  'Khoa Cấp cứu',
  'Khoa Gây mê Hồi sức',
  'Khoa Xét nghiệm',
  'Khoa Chẩn đoán hình ảnh',
  'Khoa Dược',
  'Phòng Nhân sự',
  'Phòng Kế toán',
  'Phòng Hành chính',
];

// ─── Cài đặt hệ thống mặc định ──────────────────────────────
const defaultSettings = {
  warn30: 30,    // Ngưỡng cảnh báo khẩn (ngày cam)
  warn60: 60,    // Ngưỡng cảnh báo sớm  (ngày vàng)
  req1y: 24,     // Yêu cầu 1 năm (tiết)
  req2y: 48,     // Yêu cầu 2 năm (tiết)
  req5y: 120,    // Yêu cầu 5 năm (tiết)
};

// ─── Nhân viên (25 người) ────────────────────────────────────
const employeesSeedData = [
  { id: 'NV001', fullName: 'Nguyễn Văn An',     gender: 'Nam',  dob: '1985-03-12', department: 'Khoa Ngoại Tổng hợp',       role: 'Bác sĩ',          joinDate: '2010-08-01', status: 'Active' },
  { id: 'NV002', fullName: 'Trần Thị Bình',      gender: 'Nữ',   dob: '1990-07-22', department: 'Khoa Nội Tổng hợp',        role: 'Điều dưỡng',      joinDate: '2015-01-15', status: 'Active' },
  { id: 'NV003', fullName: 'Lê Hoàng Cường',     gender: 'Nam',  dob: '1978-11-05', department: 'Khoa Cấp cứu',             role: 'Bác sĩ',          joinDate: '2005-06-01', status: 'Active' },
  { id: 'NV004', fullName: 'Phạm Thị Dung',      gender: 'Nữ',   dob: '1992-04-18', department: 'Khoa Sản',                 role: 'Hộ sinh',         joinDate: '2018-03-10', status: 'Active' },
  { id: 'NV005', fullName: 'Hoàng Minh Đức',     gender: 'Nam',  dob: '1988-09-30', department: 'Khoa Gây mê Hồi sức',     role: 'Bác sĩ',          joinDate: '2013-05-20', status: 'Active' },
  { id: 'NV006', fullName: 'Ngô Thị Hoa',        gender: 'Nữ',   dob: '1995-02-14', department: 'Khoa Nhi',                 role: 'Điều dưỡng',      joinDate: '2020-07-01', status: 'Active' },
  { id: 'NV007', fullName: 'Vũ Quốc Hùng',       gender: 'Nam',  dob: '1982-06-25', department: 'Khoa Xét nghiệm',         role: 'Kỹ thuật viên',   joinDate: '2008-09-15', status: 'Active' },
  { id: 'NV008', fullName: 'Đặng Thị Lan',       gender: 'Nữ',   dob: '1993-12-08', department: 'Khoa Chẩn đoán hình ảnh', role: 'Kỹ thuật viên',   joinDate: '2017-04-22', status: 'Active' },
  { id: 'NV009', fullName: 'Bùi Văn Minh',       gender: 'Nam',  dob: '1975-08-16', department: 'Khoa Ngoại Tổng hợp',     role: 'Bác sĩ trưởng',   joinDate: '2000-01-10', status: 'Active' },
  { id: 'NV010', fullName: 'Đinh Thị Nga',       gender: 'Nữ',   dob: '1991-05-03', department: 'Khoa Dược',               role: 'Dược sĩ',         joinDate: '2016-08-30', status: 'Active' },
  { id: 'NV011', fullName: 'Trương Văn Phong',   gender: 'Nam',  dob: '1987-01-19', department: 'Khoa Nội Tổng hợp',       role: 'Bác sĩ',          joinDate: '2012-11-05', status: 'Active' },
  { id: 'NV012', fullName: 'Lý Thị Quỳnh',       gender: 'Nữ',   dob: '1996-10-27', department: 'Khoa Nhi',                role: 'Điều dưỡng',      joinDate: '2021-02-14', status: 'Active' },
  { id: 'NV013', fullName: 'Đỗ Minh Quân',       gender: 'Nam',  dob: '1983-07-11', department: 'Khoa Gây mê Hồi sức',    role: 'Bác sĩ',          joinDate: '2009-06-18', status: 'Active' },
  { id: 'NV014', fullName: 'Nguyễn Thị Sang',    gender: 'Nữ',   dob: '1989-03-05', department: 'Khoa Sản',               role: 'Hộ sinh trưởng',  joinDate: '2014-10-01', status: 'Active' },
  { id: 'NV015', fullName: 'Phan Văn Tài',        gender: 'Nam',  dob: '1994-11-22', department: 'Khoa Cấp cứu',           role: 'Điều dưỡng',      joinDate: '2019-05-20', status: 'Active' },
  { id: 'NV016', fullName: 'Hồ Thị Thanh',       gender: 'Nữ',   dob: '1986-08-14', department: 'Khoa Xét nghiệm',        role: 'Kỹ thuật viên',   joinDate: '2011-03-25', status: 'Active' },
  { id: 'NV017', fullName: 'Cao Văn Thịnh',      gender: 'Nam',  dob: '1979-04-02', department: 'Khoa Chẩn đoán hình ảnh',role: 'Bác sĩ',          joinDate: '2006-07-10', status: 'Active' },
  { id: 'NV018', fullName: 'Lưu Thị Thu',        gender: 'Nữ',   dob: '1998-01-30', department: 'Phòng Nhân sự',          role: 'Chuyên viên',     joinDate: '2022-03-01', status: 'Active' },
  { id: 'NV019', fullName: 'Mai Đức Trung',      gender: 'Nam',  dob: '1980-09-17', department: 'Khoa Ngoại Tổng hợp',    role: 'Bác sĩ',          joinDate: '2007-02-14', status: 'Active' },
  { id: 'NV020', fullName: 'Tống Thị Uyên',      gender: 'Nữ',   dob: '1997-06-09', department: 'Khoa Dược',              role: 'Dược sĩ',         joinDate: '2021-09-15', status: 'Active' },
  { id: 'NV021', fullName: 'Trần Quốc Việt',     gender: 'Nam',  dob: '1976-12-28', department: 'Khoa Nội Tổng hợp',      role: 'Bác sĩ trưởng',   joinDate: '2002-04-05', status: 'Active' },
  { id: 'NV022', fullName: 'Nguyễn Thị Xuân',    gender: 'Nữ',   dob: '1993-07-20', department: 'Khoa Cấp cứu',           role: 'Điều dưỡng',      joinDate: '2018-08-12', status: 'Active' },
  { id: 'NV023', fullName: 'Phùng Văn Yên',      gender: 'Nam',  dob: '1985-05-15', department: 'Khoa Sản',               role: 'Bác sĩ',          joinDate: '2011-10-28', status: 'Active' },
  { id: 'NV024', fullName: 'Vương Thị Ý',        gender: 'Nữ',   dob: '1991-02-06', department: 'Phòng Kế toán',          role: 'Kế toán viên',    joinDate: '2016-05-18', status: 'Active' },
  { id: 'NV025', fullName: 'Đinh Công Anh',      gender: 'Nam',  dob: '1988-10-11', department: 'Khoa Gây mê Hồi sức',    role: 'Kỹ thuật viên',   joinDate: '2014-12-03', status: 'Active' },
];

// Hàm tạo ngày offset từ hôm nay
function daysFromToday(days) {
  const d = new Date();
  d.setDate(d.getDate() + days);
  return d.toISOString().split('T')[0];
}

// ─── Chứng chỉ / Khóa học ────────────────────────────────────
// Trạng thái: quá khứ = đã hết hạn, gần = sắp hết hạn, tương lai = còn hiệu lực
const trainingsSeedData = [
  // NV001 — Nguyễn Văn An (Đủ tiết, có chứng chỉ sắp hết hạn 🟠)
  { id: 'TR001', employeeId: 'NV001', courseName: 'Cấp cứu Nhi khoa nâng cao (PALS)',        organizer: 'BV Nhi Đồng 1',          hours: 24, issueDate: '2024-03-10', expiryDate: daysFromToday(20)  },
  { id: 'TR002', employeeId: 'NV001', courseName: 'Kiểm soát nhiễm khuẩn cơ bản',            organizer: 'Sở Y tế Đồng Nai',       hours: 16, issueDate: '2025-01-15', expiryDate: daysFromToday(120) },
  { id: 'TR003', employeeId: 'NV001', courseName: 'Nội soi tiêu hóa cơ bản',                 organizer: 'Hội Nội soi VN',         hours: 8,  issueDate: '2025-06-01', expiryDate: daysFromToday(300) },

  // NV002 — Trần Thị Bình (Chứng chỉ đã hết hạn 🔴)
  { id: 'TR004', employeeId: 'NV002', courseName: 'Chăm sóc vết thương hiện đại',             organizer: 'Sở Y tế Đồng Nai',       hours: 16, issueDate: '2022-09-10', expiryDate: daysFromToday(-45) },
  { id: 'TR005', employeeId: 'NV002', courseName: 'Hồi sức tích cực cơ bản (BLS)',            organizer: 'Hội Tim mạch VN',        hours: 8,  issueDate: '2024-11-20', expiryDate: daysFromToday(200) },

  // NV003 — Lê Hoàng Cường (Đủ tiết, còn hiệu lực)
  { id: 'TR006', employeeId: 'NV003', courseName: 'Hỗ trợ sự sống tim mạch nâng cao (ACLS)', organizer: 'Hội Tim mạch VN',        hours: 24, issueDate: '2025-02-14', expiryDate: daysFromToday(240) },
  { id: 'TR007', employeeId: 'NV003', courseName: 'Quản lý bệnh nhân nặng',                  organizer: 'BV Chợ Rẫy',             hours: 24, issueDate: '2025-04-20', expiryDate: daysFromToday(380) },

  // NV004 — Phạm Thị Dung (Thiếu tiết — chỉ có 8 tiết)
  { id: 'TR008', employeeId: 'NV004', courseName: 'An toàn bà mẹ và trẻ sơ sinh',            organizer: 'Sở Y tế Đồng Nai',       hours: 8,  issueDate: '2025-05-10', expiryDate: daysFromToday(330) },

  // NV005 — Hoàng Minh Đức (Chứng chỉ hết hạn 🔴)
  { id: 'TR009', employeeId: 'NV005', courseName: 'Gây mê nhi khoa',                         organizer: 'BV Nhi Đồng 2',          hours: 24, issueDate: '2022-11-05', expiryDate: daysFromToday(-10) },
  { id: 'TR010', employeeId: 'NV005', courseName: 'An toàn phẫu thuật WHO',                  organizer: 'WHO / BV Từ Dũ',         hours: 16, issueDate: '2025-01-25', expiryDate: daysFromToday(150) },

  // NV006 — Ngô Thị Hoa (Thiếu tiết — 8 tiết, và chứng chỉ sắp hết hạn 🟡)
  { id: 'TR011', employeeId: 'NV006', courseName: 'Chăm sóc trẻ sơ sinh',                    organizer: 'BV Nhi Đồng 1',          hours: 8,  issueDate: '2024-06-15', expiryDate: daysFromToday(45)  },

  // NV007 — Vũ Quốc Hùng (Đủ tiết)
  { id: 'TR012', employeeId: 'NV007', courseName: 'Xét nghiệm vi sinh lâm sàng',             organizer: 'Viện Pasteur TP.HCM',    hours: 24, issueDate: '2024-09-01', expiryDate: daysFromToday(260) },
  { id: 'TR013', employeeId: 'NV007', courseName: 'Kiểm soát chất lượng xét nghiệm',        organizer: 'Sở Y tế Đồng Nai',       hours: 24, issueDate: '2025-03-10', expiryDate: daysFromToday(460) },

  // NV008 — Đặng Thị Lan (Sắp hết hạn 🟡)
  { id: 'TR014', employeeId: 'NV008', courseName: 'Kỹ thuật siêu âm cơ bản',                 organizer: 'Hội CĐHA VN',            hours: 24, issueDate: '2024-05-20', expiryDate: daysFromToday(55)  },
  { id: 'TR015', employeeId: 'NV008', courseName: 'Bảo vệ bức xạ y tế',                      organizer: 'Cục An toàn Bức xạ',     hours: 16, issueDate: '2025-02-01', expiryDate: daysFromToday(400) },

  // NV009 — Bùi Văn Minh (Đủ tiết, còn hiệu lực)
  { id: 'TR016', employeeId: 'NV009', courseName: 'Phẫu thuật nội soi tiêu hóa nâng cao',   organizer: 'Hội Phẫu thuật VN',      hours: 48, issueDate: '2025-01-10', expiryDate: daysFromToday(540) },

  // NV010 — Đinh Thị Nga (Đủ tiết)
  { id: 'TR017', employeeId: 'NV010', courseName: 'Dược lâm sàng nâng cao',                  organizer: 'Hội Dược học VN',        hours: 24, issueDate: '2024-10-15', expiryDate: daysFromToday(360) },
  { id: 'TR018', employeeId: 'NV010', courseName: 'Tư vấn sử dụng thuốc an toàn',            organizer: 'Sở Y tế Đồng Nai',       hours: 16, issueDate: '2025-04-05', expiryDate: daysFromToday(480) },
  { id: 'TR019', employeeId: 'NV010', courseName: 'Hội thảo kháng sinh',                     organizer: 'BV Nhiệt đới',           hours: 8,  issueDate: '2025-06-01', expiryDate: daysFromToday(550) },

  // NV011 — Trương Văn Phong (Đủ tiết)
  { id: 'TR020', employeeId: 'NV011', courseName: 'Quản lý bệnh tim mạch mãn tính',          organizer: 'Hội Tim mạch VN',        hours: 24, issueDate: '2024-12-01', expiryDate: daysFromToday(310) },
  { id: 'TR021', employeeId: 'NV011', courseName: 'Điều trị đái tháo đường type 2',          organizer: 'Hội Nội tiết VN',        hours: 16, issueDate: '2025-02-28', expiryDate: daysFromToday(440) },
  { id: 'TR022', employeeId: 'NV011', courseName: 'Cập nhật phác đồ điều trị 2025',         organizer: 'Bộ Y tế',                hours: 8,  issueDate: '2025-05-20', expiryDate: daysFromToday(500) },

  // NV012 — Lý Thị Quỳnh (Chứng chỉ đã hết hạn 🔴, thiếu tiết)
  { id: 'TR023', employeeId: 'NV012', courseName: 'Kỹ năng tiêm truyền an toàn',             organizer: 'Sở Y tế Đồng Nai',       hours: 8,  issueDate: '2023-01-15', expiryDate: daysFromToday(-90) },

  // NV013 — Đỗ Minh Quân (Đủ tiết)
  { id: 'TR024', employeeId: 'NV013', courseName: 'Gây mê vùng và thủ thuật can thiệp',     organizer: 'Hội Gây mê VN',          hours: 24, issueDate: '2025-03-22', expiryDate: daysFromToday(420) },
  { id: 'TR025', employeeId: 'NV013', courseName: 'Quản lý đường thở khó',                  organizer: 'BV Chợ Rẫy',             hours: 24, issueDate: '2025-01-08', expiryDate: daysFromToday(380) },

  // NV014 — Nguyễn Thị Sang (Đủ tiết)
  { id: 'TR026', employeeId: 'NV014', courseName: 'Hỗ trợ sự sống sản khoa (ALSO)',         organizer: 'BV Từ Dũ',               hours: 32, issueDate: '2024-11-10', expiryDate: daysFromToday(290) },
  { id: 'TR027', employeeId: 'NV014', courseName: 'Quản lý thai kỳ nguy cơ cao',            organizer: 'Sở Y tế Đồng Nai',       hours: 16, issueDate: '2025-04-15', expiryDate: daysFromToday(510) },

  // NV015 — Phan Văn Tài (Sắp hết hạn 🟠 khẩn cấp)
  { id: 'TR028', employeeId: 'NV015', courseName: 'Cấp cứu ban đầu ngoại viện',             organizer: 'Hội Tim mạch VN',        hours: 16, issueDate: '2024-06-20', expiryDate: daysFromToday(12)  },
  { id: 'TR029', employeeId: 'NV015', courseName: 'Phân loại và xử trí cấp cứu đa thương', organizer: 'BV Chấn thương CH',      hours: 16, issueDate: '2025-03-15', expiryDate: daysFromToday(370) },

  // NV016 — Hồ Thị Thanh (Đủ tiết)
  { id: 'TR030', employeeId: 'NV016', courseName: 'Huyết học lâm sàng',                     organizer: 'Viện Huyết học',         hours: 24, issueDate: '2024-08-20', expiryDate: daysFromToday(250) },
  { id: 'TR031', employeeId: 'NV016', courseName: 'Xét nghiệm sinh hóa nâng cao',           organizer: 'Viện Pasteur TP.HCM',    hours: 24, issueDate: '2025-05-01', expiryDate: daysFromToday(520) },

  // NV017 — Cao Văn Thịnh (Chứng chỉ sắp hết hạn 🟡 60 ngày)
  { id: 'TR032', employeeId: 'NV017', courseName: 'Chẩn đoán hình ảnh tim mạch',            organizer: 'Hội CĐHA VN',            hours: 24, issueDate: '2024-04-12', expiryDate: daysFromToday(50)  },
  { id: 'TR033', employeeId: 'NV017', courseName: 'Siêu âm Doppler mạch máu',               organizer: 'BV Trung ương',          hours: 24, issueDate: '2025-02-20', expiryDate: daysFromToday(430) },

  // NV018 — Lưu Thị Thu (Thiếu tiết — không có khóa nào)
  // (không có training records)

  // NV019 — Mai Đức Trung (Đủ tiết)
  { id: 'TR034', employeeId: 'NV019', courseName: 'Phẫu thuật laparoscopic cơ bản',         organizer: 'Hội Phẫu thuật VN',      hours: 32, issueDate: '2025-01-20', expiryDate: daysFromToday(410) },
  { id: 'TR035', employeeId: 'NV019', courseName: 'Kiểm soát nhiễm khuẩn phẫu thuật',      organizer: 'Sở Y tế Đồng Nai',       hours: 16, issueDate: '2025-04-10', expiryDate: daysFromToday(490) },

  // NV020 — Tống Thị Uyên (Thiếu tiết — 16 tiết)
  { id: 'TR036', employeeId: 'NV020', courseName: 'Dược động học lâm sàng',                 organizer: 'Hội Dược học VN',        hours: 16, issueDate: '2025-05-05', expiryDate: daysFromToday(540) },

  // NV021 — Trần Quốc Việt (Đủ tiết)
  { id: 'TR037', employeeId: 'NV021', courseName: 'Tim mạch học can thiệp',                 organizer: 'Hội Tim mạch VN',        hours: 48, issueDate: '2024-10-01', expiryDate: daysFromToday(280) },

  // NV022 — Nguyễn Thị Xuân (Sắp hết hạn 🟡)
  { id: 'TR038', employeeId: 'NV022', courseName: 'Điều dưỡng cấp cứu tích cực',            organizer: 'Hội Điều dưỡng VN',      hours: 24, issueDate: '2024-05-15', expiryDate: daysFromToday(42)  },
  { id: 'TR039', employeeId: 'NV022', courseName: 'Triage và phân loại bệnh nhân',          organizer: 'BV Chợ Rẫy',             hours: 16, issueDate: '2025-03-01', expiryDate: daysFromToday(390) },

  // NV023 — Phùng Văn Yên (Đủ tiết)
  { id: 'TR040', employeeId: 'NV023', courseName: 'Sản khoa can thiệp',                     organizer: 'BV Từ Dũ',               hours: 32, issueDate: '2025-02-10', expiryDate: daysFromToday(450) },
  { id: 'TR041', employeeId: 'NV023', courseName: 'Hỗ trợ sinh sản',                        organizer: 'Sở Y tế Đồng Nai',       hours: 16, issueDate: '2025-05-12', expiryDate: daysFromToday(530) },

  // NV024 — Vương Thị Ý (Nhân viên hành chính — 0 tiết chuyên môn)
  { id: 'TR042', employeeId: 'NV024', courseName: 'Kế toán hành chính sự nghiệp',           organizer: 'Bộ Tài chính',           hours: 8,  issueDate: '2025-01-28', expiryDate: daysFromToday(365) },

  // NV025 — Đinh Công Anh (Đủ tiết)
  { id: 'TR043', employeeId: 'NV025', courseName: 'Kỹ thuật mê xuôi và hồi tỉnh',          organizer: 'Hội Gây mê VN',          hours: 24, issueDate: '2024-12-10', expiryDate: daysFromToday(320) },
  { id: 'TR044', employeeId: 'NV025', courseName: 'Kiểm soát nhiễm khuẩn phòng mổ',        organizer: 'Sở Y tế Đồng Nai',       hours: 24, issueDate: '2025-04-18', expiryDate: daysFromToday(500) },
];
