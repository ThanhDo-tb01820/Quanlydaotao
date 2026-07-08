import sys, io
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

import pandas as pd
import numpy as np
import pyodbc
import warnings
warnings.filterwarnings('ignore')

# Ket noi database
conn_str = "DRIVER={ODBC Driver 17 for SQL Server};SERVER=localhost;DATABASE=QLDAOTAO;Trusted_Connection=yes;Encrypt=no"
try:
    conn = pyodbc.connect(conn_str)
except:
    conn_str = "DRIVER={SQL Server};SERVER=localhost;DATABASE=QLDAOTAO;Trusted_Connection=yes"
    conn = pyodbc.connect(conn_str)

cursor = conn.cursor()
print("Ket noi database thanh cong!")

# Xoa du lieu cu
cursor.execute("DELETE FROM EmployeeTrainings")
cursor.execute("DELETE FROM Notifications")
cursor.execute("DELETE FROM Employees")
cursor.execute("DELETE FROM TrainingCourses")
cursor.execute("DELETE FROM Departments")
conn.commit()
print("Da xoa du lieu cu")

# Doc file Excel
file = "1. CME Đào tạo liên tục.xlsx"
df = pd.read_excel(file, sheet_name='CME', header=6)

# Loc bo dong trong
df = df[df['HỌ VÀ TÊN'].notna() & df['HỌ VÀ TÊN'].astype(str).str.strip().ne('nan')]
df = df[df['TÊN HỘI THẢO / CHƯƠNG TRÌNH'].notna()]
print(f"Tong ban ghi: {len(df)}")

def v(x):
    """Tra ve None neu NaN, nguoc lai tra ve string"""
    if pd.isna(x) or str(x).strip() in ['nan', 'NaN', '']:
        return None
    return str(x).strip()

def vdate(x):
    """Tra ve date hoac None"""
    if pd.isna(x) or str(x).strip() in ['nan', 'NaN', '']:
        return None
    try:
        return pd.to_datetime(x).date()
    except:
        return None

def vint(x):
    """Tra ve int hoac 0"""
    if pd.isna(x) or str(x).strip() in ['nan', 'NaN', '']:
        return 0
    try:
        return int(float(str(x).replace(',', '.')))
    except:
        return 0

# === 1. DEPARTMENTS ===
depts = sorted(set(d.strip() for d in df['PHÒNG BAN'].dropna() if str(d).strip()))
dept_map = {}
for i, dept in enumerate(depts, 1):
    cursor.execute(
        "INSERT INTO Departments (DepartmentCode, DepartmentName) VALUES (?, ?)",
        (f"DEPT{i:03d}", dept)
    )
    dept_map[dept] = None  # se lay ID sau

conn.commit()
print(f"Departments: {len(depts)} rows")

# Lay department IDs
cursor.execute("SELECT DepartmentId, DepartmentName FROM Departments")
for row in cursor.fetchall():
    dept_map[row.DepartmentName] = row.DepartmentId

# === 2. EMPLOYEES ===
emp_df = df.drop_duplicates(subset=['Mã HO']).copy()
emp_map = {}  # ma_ho -> EmployeeId

for _, row in emp_df.iterrows():
    ma_ho = v(row.get('Mã HO'))
    if not ma_ho:
        continue
    full_name = v(row.get('HỌ VÀ TÊN')) or 'Không rõ'
    phong_ban = v(row.get('PHÒNG BAN'))
    chuc_danh = v(row.get('CHỨC DANH')) or 'Chưa cập nhật'
    join_date = vdate(row.get('NGÀY VÀO CÔNG TY'))
    dob = vdate(row.get('NGÀY SINH'))
    nghi_viec = v(row.get('Chech nghỉ việc'))
    is_active = 0 if nghi_viec and nghi_viec.upper() in ['X', '1', 'TRUE'] else 1
    dept_id = dept_map.get(phong_ban) if phong_ban else None

    cursor.execute(
        "INSERT INTO Employees (EmployeeCode, FullName, Gender, DateOfBirth, DepartmentId, Position, JoinDate, IsActive, IsDeleted) "
        "VALUES (?, ?, ?, ?, ?, ?, ?, ?, 0)",
        (ma_ho, full_name, 'Không rõ', dob, dept_id, chuc_danh, join_date, is_active)
    )

conn.commit()
print(f"Employees: {len(emp_df)} rows")

# Lay employee IDs
cursor.execute("SELECT EmployeeId, EmployeeCode FROM Employees")
for row in cursor.fetchall():
    emp_map[row.EmployeeCode] = row.EmployeeId

# === 3. TRAINING COURSES ===
courses_df = df.drop_duplicates(subset=['TÊN HỘI THẢO / CHƯƠNG TRÌNH']).copy()
course_map = {}

for _, row in courses_df.iterrows():
    course_name = v(row['TÊN HỘI THẢO / CHƯƠNG TRÌNH'])
    if not course_name:
        continue
    course_name = course_name[:254]  # gioi han 255 ky tu
    organizer = v(row.get('ĐƠN VỊ\nTỔ CHỨC')) or 'Không rõ'
    if organizer:
        organizer = organizer[:254]
    course_code = v(row.get('Mã đào tạo')) or 'N/A'
    if course_code:
        course_code = course_code[:49]  # gioi han 50 ky tu
    hours = vint(row.get('SỐ TIẾT ĐÀO TẠO\n(ĐÃ QUY ĐỔI)'))
    if hours == 0:
        hours = vint(row.get('GIỜ TÍN CHỈ\n'))

    cursor.execute(
        "INSERT INTO TrainingCourses (CourseCode, CourseName, Organizer, DefaultHours) VALUES (?, ?, ?, ?)",
        (course_code, course_name, organizer, hours)
    )
    course_map[course_name] = None

conn.commit()
print(f"TrainingCourses: {len(courses_df)} rows")

# Lay course IDs
cursor.execute("SELECT CourseId, CourseName FROM TrainingCourses")
for row in cursor.fetchall():
    course_map[row.CourseName] = row.CourseId

# === 4. EMPLOYEE TRAININGS ===
training_count = 0
training_errors = 0

for _, row in df.iterrows():
    ma_ho = v(row.get('Mã HO'))
    course_name = v(row.get('TÊN HỘI THẢO / CHƯƠNG TRÌNH'))
    if not ma_ho or not course_name:
        continue
    course_name = course_name[:254]

    emp_id = emp_map.get(ma_ho)
    course_id = course_map.get(course_name)
    if not emp_id or not course_id:
        training_errors += 1
        continue

    issue_date = vdate(row.get('Ngày cấp'))
    if not issue_date:
        issue_date = vdate(row.get('Ngày bắt đầu đào tạo'))
        
    expiry_date = vdate(row.get('NGÀY HẾT HẠN'))
    hours = vint(row.get('SỐ TIẾT ĐÀO TẠO\n(ĐÃ QUY ĐỔI)'))
    if hours == 0:
        hours = vint(row.get('GIỜ TÍN CHỈ\n'))
    bang_cap = v(row.get('Bằng cấp')) or ''

    cursor.execute(
        "INSERT INTO EmployeeTrainings (EmployeeId, CourseId, TrainingHours, ActualHours, IssueDate, ExpiryDate, Notes) "
        "VALUES (?, ?, ?, ?, ?, ?, ?)",
        (emp_id, course_id, hours, hours, issue_date, expiry_date, bang_cap)
    )
    training_count += 1

conn.commit()
print(f"EmployeeTrainings: {training_count} rows, Bo qua: {training_errors}")

conn.close()
print("\n=== IMPORT HOAN THANH! ===")
