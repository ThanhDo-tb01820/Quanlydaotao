import sys, io, os
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

import pandas as pd
import numpy as np
import warnings
warnings.filterwarnings('ignore')

file = "1. CME Đào tạo liên tục.xlsx"
df = pd.read_excel(file, sheet_name='CME', header=6)

# Chon cac cot can thiet
cols = {
    'ma_ho': 'Mã HO',
    'full_name': 'HỌ VÀ TÊN',
    'phong_ban': 'PHÒNG BAN',
    'chuc_danh': 'CHỨC DANH',
    'join_date': 'NGÀY VÀO CÔNG TY',
    'dob': 'NGÀY SINH',
    'bang_cap': 'Bằng cấp',
    'ten_khoa_hoc': 'TÊN HỘI THẢO / CHƯƠNG TRÌNH',
    'ngay_bat_dau': 'Ngày bắt đầu đào tạo',
    'ngay_ket_thuc': 'Ngày kết thúc đào tạo',
    'ngay_cap': 'Ngày cấp',
    'don_vi': 'ĐƠN VỊ\nTỔ CHỨC',
    'so_tiet': 'SỐ TIẾT ĐÀO TẠO\n(ĐÃ QUY ĐỔI)',
    'gio_tin_chi': 'GIỜ TÍN CHỈ\n',
    'het_han': 'NGÀY HẾT HẠN',
    'ma_dao_tao': 'Mã đào tạo',
    'nghi_viec': 'Chech nghỉ việc',
}

# Loc bo dong trong
df = df[df['HỌ VÀ TÊN'].notna() & df['HỌ VÀ TÊN'].astype(str).str.strip().ne('')]
df = df[df['TÊN HỘI THẢO / CHƯƠNG TRÌNH'].notna()]
print(f"Tong so ban ghi: {len(df)}")

def safe_str(v):
    if pd.isna(v) or str(v).strip() in ['nan', 'NaN', '']:
        return None
    return str(v).strip().replace("'", "''")

def safe_date(v):
    if pd.isna(v) or str(v).strip() in ['nan', 'NaN', '']:
        return 'NULL'
    try:
        d = pd.to_datetime(v)
        return f"'{d.strftime('%Y-%m-%d')}'"
    except:
        return 'NULL'

def safe_int(v):
    if pd.isna(v) or str(v).strip() in ['nan', 'NaN', '']:
        return 0
    try:
        s = str(v).replace(',', '.').strip()
        return int(float(s))
    except:
        return 0

sql_lines = []

# === 1. DEPARTMENTS ===
depts = df['PHÒNG BAN'].dropna().unique()
depts = [d.strip() for d in depts if str(d).strip()]
dept_map = {}

sql_lines.append("-- =============================================")
sql_lines.append("-- 1. DEPARTMENTS")
sql_lines.append("-- =============================================")
for i, dept in enumerate(sorted(set(depts)), 1):
    dept_esc = dept.replace("'", "''")
    dept_map[dept] = i
    sql_lines.append(f"INSERT INTO Departments (DepartmentCode, DepartmentName) VALUES (N'DEPT{i:03d}', N'{dept_esc}');")

# === 2. EMPLOYEES (unique by Mã HO) ===
emp_df = df.drop_duplicates(subset=['Mã HO']).copy()
emp_map = {}  # ma_ho -> row_index

sql_lines.append("\n-- =============================================")
sql_lines.append("-- 2. EMPLOYEES")
sql_lines.append("-- =============================================")

for _, row in emp_df.iterrows():
    ma_ho = safe_str(row.get('Mã HO'))
    if not ma_ho:
        continue
    full_name = safe_str(row.get('HỌ VÀ TÊN')) or 'Không rõ'
    phong_ban = safe_str(row.get('PHÒNG BAN'))
    chuc_danh = safe_str(row.get('CHỨC DANH'))
    join_date = safe_date(row.get('NGÀY VÀO CÔNG TY'))
    dob = safe_date(row.get('NGÀY SINH'))
    nghi_viec = safe_str(row.get('Chech nghỉ việc'))
    is_active = 0 if nghi_viec and nghi_viec.upper() in ['X', '1', 'TRUE'] else 1

    dept_lookup = phong_ban or ''
    dept_id_sql = "(SELECT TOP 1 DepartmentId FROM Departments WHERE DepartmentName = N'" + dept_lookup.replace("'","''") + "')"

    sql_lines.append(
        f"INSERT INTO Employees (EmployeeCode, FullName, Gender, DateOfBirth, DepartmentId, Position, JoinDate, IsActive, IsDeleted) "
        f"VALUES (N'{ma_ho}', N'{full_name}', N'Không rõ', {dob}, {dept_id_sql}, "
        f"{'N''' + chuc_danh.replace(chr(39), chr(39)+chr(39)) + chr(39) if chuc_danh else 'NULL'}, {join_date}, {is_active}, 0);"
    )

# === 3. TRAINING COURSES (unique by course name) ===
courses_df = df[['TÊN HỘI THẢO / CHƯƠNG TRÌNH', 'ĐƠN VỊ\nTỔ CHỨC', 'Mã đào tạo', 'SỐ TIẾT ĐÀO TẠO\n(ĐÃ QUY ĐỔI)', 'GIỜ TÍN CHỈ\n']].copy()
courses_df = courses_df.drop_duplicates(subset=['TÊN HỘI THẢO / CHƯƠNG TRÌNH'])

sql_lines.append("\n-- =============================================")
sql_lines.append("-- 3. TRAINING COURSES")
sql_lines.append("-- =============================================")

for _, row in courses_df.iterrows():
    course_name = safe_str(row['TÊN HỘI THẢO / CHƯƠNG TRÌNH'])
    if not course_name:
        continue
    organizer = safe_str(row.get('ĐƠN VỊ\nTỔ CHỨC'))
    course_code = safe_str(row.get('Mã đào tạo'))
    hours = safe_int(row.get('SỐ TIẾT ĐÀO TẠO\n(ĐÃ QUY ĐỔI)'))
    if hours == 0:
        hours = safe_int(row.get('GIỜ TÍN CHỈ\n'))

    organizer_sql = f"N'{organizer}'" if organizer else 'NULL'
    code_sql = f"N'{course_code}'" if course_code else 'NULL'

    sql_lines.append(
        f"INSERT INTO TrainingCourses (CourseCode, CourseName, Organizer, DefaultHours) "
        f"VALUES ({code_sql}, N'{course_name}', {organizer_sql}, {hours});"
    )

# === 4. EMPLOYEE TRAININGS ===
sql_lines.append("\n-- =============================================")
sql_lines.append("-- 4. EMPLOYEE TRAININGS")
sql_lines.append("-- =============================================")

for _, row in df.iterrows():
    ma_ho = safe_str(row.get('Mã HO'))
    course_name = safe_str(row.get('TÊN HỘI THẢO / CHƯƠNG TRÌNH'))
    if not ma_ho or not course_name:
        continue

    issue_date = safe_date(row.get('Ngày cấp'))
    if issue_date == 'NULL':
        issue_date = safe_date(row.get('Ngày bắt đầu đào tạo'))
        
    expiry_date = safe_date(row.get('NGÀY HẾT HẠN'))
    hours = safe_int(row.get('SỐ TIẾT ĐÀO TẠO\n(ĐÃ QUY ĐỔI)'))
    if hours == 0:
        hours = safe_int(row.get('GIỜ TÍN CHỈ\n'))
    bang_cap = safe_str(row.get('Bằng cấp'))
    notes_sql = f"N'{bang_cap}'" if bang_cap else 'NULL'

    emp_id_sql = f"(SELECT TOP 1 EmployeeId FROM Employees WHERE EmployeeCode = N'{ma_ho}')"
    course_id_sql = f"(SELECT TOP 1 CourseId FROM TrainingCourses WHERE CourseName = N'{course_name}')"

    sql_lines.append(
        f"INSERT INTO EmployeeTrainings (EmployeeId, CourseId, TrainingHours, ActualHours, IssueDate, ExpiryDate, Notes) "
        f"VALUES ({emp_id_sql}, {course_id_sql}, {hours}, {hours}, {issue_date}, {expiry_date}, {notes_sql});"
    )

# Ghi ra file SQL
out_file = "import_data.sql"
with open(out_file, 'w', encoding='utf-8-sig') as f:
    f.write("-- AUTO-GENERATED IMPORT SCRIPT\n")
    f.write("-- Database: QLDAOTAO\n\n")
    f.write("SET NOCOUNT ON;\n\n")
    f.write('\n'.join(sql_lines))

print(f"Xuat ra file: {out_file}")
print(f"So Departments: {len(depts)}")
print(f"So Employees: {len(emp_df)}")
print(f"So Training Courses: {len(courses_df)}")
print(f"So Employee Trainings: {len(df)}")
