/**
 * app.js — CME Smart Tracker (Full-Stack Version)
 * Giao tiếp với ASP.NET Core API thay vì dùng data.js tĩnh
 * Bệnh viện Hoàn Mỹ Đồng Nai
 */

// ─────────────────────────────────────────────────────────────
//  CẤU HÌNH API
// ─────────────────────────────────────────────────────────────
const API_BASE = window.location.origin.startsWith('file://')
  ? 'http://localhost:5183/api/v1'
  : `${window.location.origin}/api/v1`;

// ─────────────────────────────────────────────────────────────
//  STATE
// ─────────────────────────────────────────────────────────────
let currentUser = null;
let currentToken = null;
let currentDetailEmpId = null;
let currentAlertFilter = 'all';
let departments        = [];
let courses            = [];
let allUsersList       = [];
let settings           = { expiryWarningDays: 60, urgentWarningDays: 30, requiredHours2Years: 48 };

// ─────────────────────────────────────────────────────────────
//  API HELPER — Fetch wrapper với xử lý lỗi
// ─────────────────────────────────────────────────────────────
async function api(path, options = {}) {
  const headers = { 'Content-Type': 'application/json', ...options.headers };
  const token = currentToken || localStorage.getItem('token') || sessionStorage.getItem('token');
  if (token) {
    headers['Authorization'] = `Bearer ${token}`;
  }
  
  try {
    const res = await fetch(`${API_BASE}${path}`, {
      headers,
      ...options,
    });
    if (res.status === 401 || res.status === 403) {
      handleLogout();
      throw new Error('Phiên đăng nhập đã hết hạn hoặc bạn không có quyền!');
    }
    if (!res.ok) {
      const err = await res.json().catch(() => ({ message: res.statusText }));
      throw new Error(err.message || `HTTP ${res.status}`);
    }
    if (res.status === 204) return null;
    return await res.json();
  } catch (e) {
    if (e.message.includes('fetch') || e.message.includes('Failed')) {
      showToast('❌ Không kết nối được API. Hãy chắc chắn backend đang chạy!', 'error');
    } else {
      showToast(`❌ Lỗi: ${e.message}`, 'error');
    }
    throw e;
  }
}

// ─────────────────────────────────────────────────────────────
//  UTILITIES
// ─────────────────────────────────────────────────────────────
function formatDate(dateStr) {
  if (!dateStr) return '—';
  const [y, m, d] = dateStr.split('T')[0].split('-');
  return `${d}/${m}/${y}`;
}

function genId(prefix) {
  return prefix + Date.now().toString(36).toUpperCase();
}

function showToast(msg, type = 'success') {
  const t = document.getElementById('toast');
  t.textContent = msg;
  t.className = `toast ${type} show`;
  setTimeout(() => t.classList.remove('show'), 3500);
}

function showLoading(containerId) {
  const el = document.getElementById(containerId);
  if (el) el.innerHTML = `
    <div style="text-align:center;padding:40px;color:var(--text-muted);">
      <div class="spinner"></div>
      <p style="margin-top:12px;font-size:13px;">Đang tải dữ liệu...</p>
    </div>`;
}

async function updateAlertBadge() {
  try {
    const alerts = await api('/dashboard/alerts');
    const urgent = alerts.filter(a => a.alertType === 'red' || a.alertType === 'orange').length;
    document.getElementById('alertBadge').textContent = urgent;
    document.getElementById('notifCount').textContent  = urgent;
  } catch (_) {}
}

function setCurrentDate() {
  const now  = new Date();
  const opts = { weekday: 'long', day: '2-digit', month: '2-digit', year: 'numeric' };
  document.getElementById('topbarDate').textContent = now.toLocaleDateString('vi-VN', opts);
}

// ─────────────────────────────────────────────────────────────
//  NAVIGATION
// ─────────────────────────────────────────────────────────────
const pageConfigs = {
  dashboard:          { breadcrumb: 'Dashboard' },
  alerts:             { breadcrumb: 'Cảnh báo' },
  employees:          { breadcrumb: 'Nhân viên' },
  'employee-detail':  { breadcrumb: 'Nhân viên → Chi tiết' },
  trainings:          { breadcrumb: 'Đào tạo' },
  settings:           { breadcrumb: 'Cài đặt' },
  users:              { breadcrumb: 'Phân quyền & Tài khoản' },
};

// ─────────────────────────────────────────────────────────────
//  MOBILE TABLE HELPER: gán data-label vào mỗi <td>
//  để CSS card layout có thể hiển thị tên cột trên mobile
// ─────────────────────────────────────────────────────────────
function applyMobileDataLabels(tableId) {
  const table = document.getElementById(tableId);
  if (!table) return;
  const headers = [...table.querySelectorAll('thead th')].map(th => th.textContent.trim());
  table.querySelectorAll('tbody tr').forEach(tr => {
    [...tr.querySelectorAll('td')].forEach((td, i) => {
      td.setAttribute('data-label', headers[i] || '');
    });
  });
}

function showPage(pageId) {
  document.querySelectorAll('.page').forEach(p => p.classList.remove('active'));
  const page = document.getElementById(`page-${pageId}`);
  if (page) page.classList.add('active');

  document.querySelectorAll('.nav-item').forEach(n => n.classList.remove('active'));
  const navEl = document.getElementById(`nav-${pageId}`) ||
                document.getElementById('nav-employees');
  if (navEl) navEl.classList.add('active');

  // Sync mobile bottom nav
  document.querySelectorAll('.mobile-nav-btn').forEach(b => b.classList.remove('active'));
  const mnavEl = document.getElementById(`mnav-${pageId}`) ||
                 document.getElementById('mnav-employees');
  if (mnavEl) mnavEl.classList.add('active');

  document.getElementById('breadcrumb').textContent =
    pageConfigs[pageId]?.breadcrumb || pageId;

  switch (pageId) {
    case 'dashboard':       renderDashboard();  break;
    case 'alerts':          renderAlerts();     break;
    case 'employees':       renderEmployees();  break;
    case 'trainings':       renderTrainings();  break;
    case 'settings':        renderSettings();   break;
    case 'users':           renderUsers();      break;
  }

  closeMobileSidebar();
  window.scrollTo({ top: 0, behavior: 'smooth' });
}


// ─────────────────────────────────────────────────────────────
//  RENDER: DASHBOARD
// ─────────────────────────────────────────────────────────────
async function renderDashboard() {
  try {
    const [summary, alerts] = await Promise.all([
      api('/dashboard/summary'),
      api('/dashboard/alerts'),
    ]);

    // Thống kê khẩn cấp và hiển thị lên Alert Widget
    const widget = document.getElementById('emergencyAlertWidget');
    const widgetText = document.getElementById('emergencyAlertText');
    if (widget && widgetText) {
      const urgentCerts = summary.urgentCertificates || 0;
      const expiredCerts = summary.expiredCertificates || 0;
      const nonCme = summary.nonCompliantEmployees || 0;
      
      if (urgentCerts > 0 || expiredCerts > 0 || nonCme > 0) {
        widgetText.innerHTML = `Hôm nay có <b>${expiredCerts}</b> chứng chỉ đã hết hạn, <b>${urgentCerts}</b> chứng chỉ sắp hết hạn và <b>${nonCme}</b> nhân viên chưa đủ tiết CME.`;
        widget.style.display = 'flex';
      } else {
        widget.style.display = 'none';
      }
    }

    // Summary Cards
    document.getElementById('dashboardCards').innerHTML = `
      ${statCard('blue',   '👥', summary.totalEmployees,        'Tổng nhân viên',       'Đang hoạt động')}
      ${statCard('green',  '✅', summary.compliantEmployees,    'Đạt yêu cầu CME',      `≥ ${settings.requiredHours2Years} tiết / 2 năm`)}
      ${statCard('red',    '❌', summary.nonCompliantEmployees, 'Chưa đạt yêu cầu',     `< ${settings.requiredHours2Years} tiết`)}
      ${statCard('orange', '⚠️', summary.expiringCertificates, 'Sắp hết hạn',           `Trong ${settings.expiryWarningDays} ngày tới`)}
      ${statCard('red',    '🔴', summary.expiredCertificates,  'Chứng chỉ hết hạn',     'Cần gia hạn ngay')}
    `;

    renderDonutChart(
      summary.compliantEmployees,
      summary.nonCompliantEmployees,
      summary.expiredCertificates,
      summary.urgentCertificates
    );
    await renderDeptBars();
    renderDashboardAlerts(alerts);
    renderDeptDashboard(); // Đào tạo theo phòng ban (async, không block)
    renderNotificationDropdown(); // Dropdown cảnh báo

  } catch (e) {
    document.getElementById('dashboardCards').innerHTML =
      `<div class="empty-state" style="grid-column:1/-1"><div class="empty-icon">⚠️</div><p>Không thể tải dữ liệu. Backend có đang chạy không?</p></div>`;
  }
}

function statCard(color, icon, value, label, trend) {
  return `
  <div class="stat-card ${color}">
    <div class="stat-card-icon">${icon}</div>
    <div class="stat-value">${value ?? 0}</div>
    <div class="stat-label">${label}</div>
    <div class="stat-trend">${trend}</div>
  </div>`;
}

function renderDonutChart(compliant, nonComp, expired, expiring) {
  const canvas = document.getElementById('donutChart');
  if (!canvas) return;
  const ctx = canvas.getContext('2d');
  const cx = 110, cy = 110, radius = 88, innerR = 56;

  const data = [
    { val: compliant, color: '#10b981', label: 'Đạt CME' },
    { val: nonComp,   color: '#f59e0b', label: 'Chưa đạt CME' },
    { val: expired,   color: '#ef4444', label: 'Hết hạn CC' },
    { val: expiring,  color: '#f97316', label: 'Sắp hết hạn CC' },
  ].filter(d => d.val > 0);

  const total = data.reduce((s, d) => s + d.val, 0) || 1;
  ctx.clearRect(0, 0, 220, 220);

  let startAngle = -Math.PI / 2;
  data.forEach(d => {
    const slice = (d.val / total) * 2 * Math.PI;
    ctx.beginPath();
    ctx.moveTo(cx, cy);
    ctx.arc(cx, cy, radius, startAngle, startAngle + slice);
    ctx.closePath();
    ctx.fillStyle = d.color;
    ctx.fill();
    startAngle += slice;
  });

  ctx.beginPath();
  ctx.arc(cx, cy, innerR, 0, 2 * Math.PI);
  ctx.fillStyle = '#fff';
  ctx.fill();

  ctx.fillStyle = '#0f172a';
  ctx.font = 'bold 26px Inter, sans-serif';
  ctx.textAlign = 'center';
  ctx.textBaseline = 'middle';
  ctx.fillText(total, cx, cy - 8);
  ctx.font = '11px Inter, sans-serif';
  ctx.fillStyle = '#94a3b8';
  ctx.fillText('nhân viên', cx, cy + 12);

  document.getElementById('donutLegend').innerHTML = data.map(d => `
    <div class="legend-item">
      <div class="legend-dot" style="background:${d.color}"></div>
      <span class="legend-label">${d.label}</span>
      <span class="legend-val">${d.val}</span>
    </div>
  `).join('');
}

async function renderDeptBars() {
  const deptEl = document.getElementById('deptBars');
  try {
    const employees = await api('/employees');
    const deptMap = {};
    employees.forEach(emp => {
      if (!deptMap[emp.departmentName]) deptMap[emp.departmentName] = { total: 0, nonComp: 0 };
      deptMap[emp.departmentName].total++;
      if (!emp.isCompliant) deptMap[emp.departmentName].nonComp++;
    });

    const sorted = Object.entries(deptMap)
      .filter(([, v]) => v.nonComp > 0)
      .sort((a, b) => b[1].nonComp - a[1].nonComp)
      .slice(0, 6);

    if (!sorted.length) {
      deptEl.innerHTML = '<div class="empty-state"><p>🎉 Tất cả phòng ban đều đạt yêu cầu!</p></div>';
      return;
    }

    const maxNonComp = Math.max(...sorted.map(([, v]) => v.nonComp));
    deptEl.innerHTML = sorted.map(([dept, v]) => {
      const pct   = Math.round((v.nonComp / maxNonComp) * 100);
      const color = pct > 66 ? '#ef4444' : pct > 33 ? '#f97316' : '#f59e0b';
      return `
      <div class="dept-bar-item">
        <div class="dept-bar-label">
          <span>${dept}</span>
          <span>${v.nonComp}/${v.total} chưa đạt</span>
        </div>
        <div class="dept-bar-track">
          <div class="dept-bar-fill" style="width:${pct}%;background:${color};"></div>
        </div>
      </div>`;
    }).join('');
  } catch (_) {}
}

function renderDashboardAlerts(alerts) {
  const el = document.getElementById('dashboardAlerts');
  const top8 = (alerts || []).slice(0, 8);
  if (!top8.length) {
    el.innerHTML = '<div class="empty-state"><div class="empty-icon">✅</div><p>Không có cảnh báo nào!</p></div>';
    return;
  }
  el.innerHTML = top8.map(a => {
    const icon     = a.alertType === 'red' ? '🔴' : a.alertType === 'orange' ? '🟠' : a.alertType === 'amber' ? '🟡' : '⚠️';
    const cls      = a.alertType === 'missing' ? 'amber' : (a.alertType || 'amber');
    const daysText = a.daysLeft !== null && a.daysLeft !== undefined
      ? (a.daysLeft < 0
          ? `Đã hết hạn ${Math.abs(a.daysLeft)} ngày`
          : `Còn ${a.daysLeft} ngày`)
      : '';
    return `
    <div class="alert-row" onclick="viewEmployeeByCode('${a.employeeCode}')" title="Xem chi tiết">
      <div class="alert-icon ${cls}">${icon}</div>
      <div class="alert-content">
        <div class="alert-name">${a.employeeName}</div>
        <div class="alert-desc">${a.courseName}</div>
      </div>
      <div class="alert-meta">
        <div class="alert-days ${cls}">${daysText}</div>
        <div class="alert-date">${formatDate(a.expiryDate)}</div>
      </div>
    </div>`;
  }).join('');
}

// ─────────────────────────────────────────────────────────────
//  RENDER: ALERTS PAGE
// ─────────────────────────────────────────────────────────────
async function renderAlerts() {
  await filterAlerts(currentAlertFilter, null);
  await updateAlertBadge();
}

async function filterAlerts(filter, btnEl) {
  currentAlertFilter = filter;

  if (btnEl) {
    document.querySelectorAll('#alertFilterTabs .tab-btn').forEach(b => b.classList.remove('active'));
    btnEl.classList.add('active');
  }

  const tbody = document.getElementById('alertsTableBody');
  tbody.innerHTML = `<tr><td colspan="7"><div style="text-align:center;padding:30px;color:var(--text-muted);">Đang tải...</div></td></tr>`;

  try {
    let alerts = await api('/dashboard/alerts');

    if (filter === 'expired')     alerts = alerts.filter(a => a.alertType === 'red');
    else if (filter === 'expiring30') alerts = alerts.filter(a => a.alertType === 'orange');
    else if (filter === 'expiring60') alerts = alerts.filter(a => a.alertType === 'amber');
    else if (filter === 'missing')    alerts = alerts.filter(a => a.alertKind === 'missing');

    if (!alerts.length) {
      tbody.innerHTML = `<tr><td colspan="7" class="empty-state"><p>✅ Không có cảnh báo trong danh mục này</p></td></tr>`;
      return;
    }

    tbody.innerHTML = alerts.map(a => {
      const daysText = a.daysLeft !== null && a.daysLeft !== undefined
        ? (a.daysLeft < 0
            ? `<span style="color:var(--danger);font-weight:700;">Quá ${Math.abs(a.daysLeft)} ngày</span>`
            : `<span style="font-weight:600;">${a.daysLeft} ngày</span>`)
        : '—';
      return `
      <tr>
        <td><code style="font-size:12px;color:var(--text-secondary);">${a.employeeCode}</code></td>
        <td><a class="emp-link" onclick="viewEmployeeByCode('${a.employeeCode}')">${a.employeeName}</a></td>
        <td><span class="badge badge-gray">${a.department}</span></td>
        <td>${a.courseName}</td>
        <td>${formatDate(a.expiryDate)}</td>
        <td>${daysText}</td>
        <td><span class="badge ${a.badgeClass}">${a.statusLabel}</span></td>
      </tr>`;
    }).join('');
    applyMobileDataLabels('alertsTable');
  } catch (_) {}
}

// ─────────────────────────────────────────────────────────────
//  RENDER: EMPLOYEES PAGE
// ─────────────────────────────────────────────────────────────
async function renderEmployees() {
  // Load departments cho filter
  try {
    departments = await api('/departments');
    const deptSel = document.getElementById('deptFilter');
    const current = deptSel.value;
    deptSel.innerHTML = '<option value="">Tất cả phòng ban</option>' +
      departments.map(d => `<option value="${d.departmentId}">${d.departmentName}</option>`).join('');
    deptSel.value = current;
  } catch (_) {}

  await filterEmployees();
}

let filterTimeout = null;
function debounceFilterEmployees() {
  clearTimeout(filterTimeout);
  filterTimeout = setTimeout(() => {
    filterEmployees();
  }, 300);
}

let allEmployeesList = [];
let currentEmployeesPage = 1;
const employeesPerPage = 10;

async function filterEmployees() {
  const search    = document.getElementById('empSearch')?.value || '';
  const deptId    = document.getElementById('deptFilter')?.value || '';
  const statusVal = document.getElementById('statusFilter')?.value || '';

  const tbody = document.getElementById('employeesTableBody');
  tbody.innerHTML = `<tr><td colspan="8"><div style="text-align:center;padding:30px;color:var(--text-muted);">Đang tải...</div></td></tr>`;

  try {
    const params = new URLSearchParams();
    if (search)    params.set('search', search);
    if (deptId)    params.set('deptId', deptId);
    if (statusVal) params.set('compliance', statusVal);

    allEmployeesList = await api(`/employees?${params}`);
    currentEmployeesPage = 1;

    renderEmployeesTable();
  } catch (_) {}
}

function renderEmployeesTable() {
  const tbody = document.getElementById('employeesTableBody');
  
  if (!allEmployeesList || !allEmployeesList.length) {
    tbody.innerHTML = `<tr><td colspan="8" class="empty-state"><p>Không tìm thấy nhân viên phù hợp</p></td></tr>`;
    document.getElementById('employeesPagination').style.display = 'none';
    return;
  }

  document.getElementById('employeesPagination').style.display = 'flex';

  const totalPages = Math.ceil(allEmployeesList.length / employeesPerPage);
  if (currentEmployeesPage < 1) currentEmployeesPage = 1;
  if (currentEmployeesPage > totalPages) currentEmployeesPage = totalPages;

  const startIndex = (currentEmployeesPage - 1) * employeesPerPage;
  const endIndex = Math.min(startIndex + employeesPerPage, allEmployeesList.length);
  const pageData = allEmployeesList.slice(startIndex, endIndex);

  tbody.innerHTML = pageData.map(emp => `
    <tr>
      <td><code style="font-size:12px;color:var(--text-muted);">${emp.employeeCode}</code></td>
      <td><a class="emp-link" onclick="viewEmployee(${emp.employeeId})">${emp.fullName}</a></td>
      <td><span class="badge badge-gray">${emp.departmentName}</span></td>
      <td>${emp.position}</td>
      <td>
        <strong>${emp.totalHours}</strong>
        <span style="color:var(--text-muted);font-size:12px;"> / ${settings.requiredHours2Years} tiết</span>
      </td>
      <td>
        <span class="badge ${emp.isCompliant ? 'badge-green' : 'badge-red'}">
          ${emp.isCompliant ? '✅ Đạt' : `❌ Thiếu ${emp.missingHours} tiết`}
        </span>
      </td>
      <td>
        ${emp.certWarnings > 0
          ? `<span class="badge badge-orange">⚠️ ${emp.certWarnings} CC cần chú ý</span>`
          : `<span class="badge badge-green">✓ Bình thường</span>`}
      </td>
      <td>
        <button class="btn-icon" onclick="viewEmployee(${emp.employeeId})" title="Xem chi tiết">
          <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"/><circle cx="12" cy="12" r="3"/>
          </svg>
        </button>
      </td>
    </tr>`).join('');
  applyMobileDataLabels('employeesTable');


  document.getElementById('employeesPageInfo').textContent = `Đang hiển thị ${startIndex + 1} - ${endIndex} / ${allEmployeesList.length} bản ghi`;
  document.getElementById('employeesPageButtons').innerHTML = generatePaginationButtons(currentEmployeesPage, totalPages, 'goToEmployeesPage');
}

window.goToEmployeesPage = function(page) {
  currentEmployeesPage = page;
  renderEmployeesTable();
};

// ─────────────────────────────────────────────────────────────
//  EMPLOYEE DETAIL
// ─────────────────────────────────────────────────────────────
async function viewEmployee(empId) {
  currentDetailEmpId = empId;
  showPage('employee-detail');

  document.getElementById('detailEmpName').textContent = 'Đang tải...';
  document.getElementById('detailProfile').innerHTML   = '';
  document.getElementById('detailCME').innerHTML       = '';
  document.getElementById('detailTrainingsBody').innerHTML =
    `<tr><td colspan="6"><div style="text-align:center;padding:20px;color:var(--text-muted);">Đang tải...</div></td></tr>`;

  try {
    const emp = await api(`/employees/${empId}`);

    document.getElementById('detailEmpName').textContent = emp.fullName;
    document.getElementById('detailEmpDept').textContent = `${emp.departmentName} — ${emp.position}`;

    const initials = emp.fullName.split(' ').map(w => w[0]).join('').slice(-2).toUpperCase();
    const pct      = Math.min(100, Math.round((emp.totalHours / settings.requiredHours2Years) * 100));
    const barColor = emp.isCompliant ? '#10b981' : emp.totalHours > settings.requiredHours2Years * 0.5 ? '#f59e0b' : '#ef4444';

    document.getElementById('detailProfile').innerHTML = `
      <div class="profile-avatar-lg">${initials}</div>
      <div class="profile-field"><label>Mã nhân viên</label><span>${emp.employeeCode}</span></div>
      <div class="profile-field"><label>Họ và tên</label><span>${emp.fullName}</span></div>
      <div class="profile-field"><label>Giới tính</label><span>${emp.gender}</span></div>
      <div class="profile-field"><label>Ngày sinh</label><span>${formatDate(emp.dateOfBirth)}</span></div>
      <div class="profile-field"><label>Phòng ban</label><span>${emp.departmentName}</span></div>
      <div class="profile-field"><label>Chức danh</label><span>${emp.position}</span></div>
      <div class="profile-field"><label>Ngày vào làm</label><span>${formatDate(emp.joinDate)}</span></div>
      <div class="profile-field">
        <label>Trạng thái</label>
        <span><span class="badge ${emp.isActive ? 'badge-green' : 'badge-gray'}">${emp.isActive ? '✓ Đang làm việc' : '✗ Đã nghỉ'}</span></span>
      </div>
    `;

    document.getElementById('detailCME').innerHTML = `
      <h3 style="font-size:15px;font-weight:700;margin-bottom:16px;">📊 Tổng quan CME (2 năm)</h3>
      <div class="cme-progress-wrap">
        <div class="cme-bar-label">
          <span>Tiến độ đạt yêu cầu</span>
          <span><strong>${emp.totalHours}</strong> / ${settings.requiredHours2Years} tiết (${pct}%)</span>
        </div>
        <div class="cme-bar-track">
          <div class="cme-bar-fill" style="width:${pct}%;background:${barColor};"></div>
        </div>
      </div>
      <div class="cme-stat-grid">
        <div class="cme-stat-box">
          <div class="num" style="color:${barColor}">${emp.totalHours}</div>
          <div class="lbl">Tổng tiết</div>
        </div>
        <div class="cme-stat-box">
          <div class="num" style="color:${emp.missingHours > 0 ? '#ef4444' : '#10b981'}">${emp.missingHours}</div>
          <div class="lbl">Còn thiếu</div>
        </div>
        <div class="cme-stat-box">
          <div class="num" style="color:${emp.isCompliant ? '#10b981' : '#ef4444'}">${emp.isCompliant ? '✅' : '❌'}</div>
          <div class="lbl">${emp.isCompliant ? 'Đạt yêu cầu' : 'Chưa đạt'}</div>
        </div>
      </div>
      <div style="margin-top:16px;padding:12px;background:var(--surface-2);border-radius:8px;font-size:13px;color:var(--text-secondary);">
        <strong>Quy định:</strong> 1 năm = ${settings.requiredHours1Year} tiết &nbsp;|&nbsp; 2 năm = ${settings.requiredHours2Years} tiết &nbsp;|&nbsp; 5 năm = ${settings.requiredHours5Years} tiết
      </div>
    `;

    const tbody = document.getElementById('detailTrainingsBody');
    if (!emp.trainings?.length) {
      tbody.innerHTML = `<tr><td colspan="8" class="empty-state"><p>Chưa có khóa đào tạo nào được ghi nhận</p></td></tr>`;
    } else {
      tbody.innerHTML = emp.trainings.map(tr => `
        <tr>
          <td>${tr.courseName}</td>
          <td>${tr.organizer}</td>
          <td>
            ${tr.isLifetime
              ? `<span class="badge badge-lifetime">♾️ Vĩnh viễn</span>`
              : tr.needsRenewal
                ? `<span class="badge badge-expiry">⏳ Hạn ${tr.renewalAfterYears}năm</span>`
                : `<span class="badge badge-gray">⏳ Có hạn</span>`}
          </td>
          <td><strong>${tr.actualHours}</strong> <span style="font-size:12px;color:var(--text-muted);">/ ${tr.trainingHours}</span></td>
          <td><strong>${formatDate(tr.issueDate)}</strong></td>
          <td>${tr.isLifetime ? '<span style="color:var(--primary);font-weight:600;">♾️ Mãi mãi</span>' : `<strong>${formatDate(tr.expiryDate)}</strong>${tr.needsRenewal ? ` <small style="color:var(--warning);">(Học lại sau ${tr.renewalAfterYears} năm)</small>` : ''}`}</td>
          <td>${tr.hasEvidence ? `<a href="${tr.certificateUrl}" target="_blank" class="badge badge-green" style="text-decoration:none;">📎 Xem ảnh</a>` : `<span class="badge badge-red">⚠️ Chưa có</span>`}</td>
          <td><span class="badge ${tr.badgeClass}">${tr.statusLabel}</span></td>
        </tr>`).join('');
    }
  } catch (_) {}
}

async function viewEmployeeByCode(code) {
  try {
    const list = await api(`/employees?search=${encodeURIComponent(code)}`);
    if (list.length > 0) viewEmployee(list[0].employeeId);
  } catch (_) {}
}

// ─────────────────────────────────────────────────────────────
//  RENDER: TRAININGS PAGE
// ─────────────────────────────────────────────────────────────
async function renderTrainings() {
  await filterTrainings();
}

let allTrainingsList = [];
let currentTrainingsPage = 1;
const trainingsPerPage = 10;

async function filterTrainings() {
  const search    = document.getElementById('trainSearch')?.value || '';
  const statusVal = document.getElementById('trainStatusFilter')?.value || '';

  const tbody = document.getElementById('trainingsTableBody');
  tbody.innerHTML = `<tr><td colspan="9"><div style="text-align:center;padding:30px;color:var(--text-muted);">Đang tải...</div></td></tr>`;

  try {
    const params = new URLSearchParams();
    if (search)    params.set('search', search);
    if (statusVal) params.set('status', statusVal);

    allTrainingsList = await api(`/trainings?${params}`);
    currentTrainingsPage = 1;

    renderTrainingsTable();
  } catch (_) {}
}

function renderTrainingsTable() {
  const tbody = document.getElementById('trainingsTableBody');
  
  if (!allTrainingsList || !allTrainingsList.length) {
    tbody.innerHTML = `<tr><td colspan="9" class="empty-state"><p>Không tìm thấy kết quả phù hợp</p></td></tr>`;
    document.getElementById('trainingsPagination').style.display = 'none';
    return;
  }

  document.getElementById('trainingsPagination').style.display = 'flex';

  const totalPages = Math.ceil(allTrainingsList.length / trainingsPerPage);
  if (currentTrainingsPage < 1) currentTrainingsPage = 1;
  if (currentTrainingsPage > totalPages) currentTrainingsPage = totalPages;

  const startIndex = (currentTrainingsPage - 1) * trainingsPerPage;
  const endIndex = Math.min(startIndex + trainingsPerPage, allTrainingsList.length);
  const pageData = allTrainingsList.slice(startIndex, endIndex);

  tbody.innerHTML = pageData.map(tr => `
    <tr>
      <td><a class="emp-link" onclick="viewEmployee(${tr.employeeId})">${tr.employeeName}</a></td>
      <td><span class="badge badge-gray">${tr.departmentName}</span></td>
      <td>${tr.courseName}</td>
      <td>${tr.organizer}</td>
      <td>
        ${tr.isLifetime
          ? `<span class="badge badge-lifetime">♾️ Vĩnh viễn</span>`
          : tr.needsRenewal
            ? `<span class="badge badge-expiry">⏳ Hạn ${tr.renewalAfterYears} năm</span>`
            : `<span class="badge badge-gray">⏳ Có thời hạn</span>`}
      </td>
      <td><strong>${tr.actualHours}</strong> <span style="font-size:12px;color:var(--text-muted);">/ ${tr.trainingHours}</span></td>
      <td><strong>${formatDate(tr.issueDate)}</strong></td>
      <td><strong>${tr.isLifetime ? '♾️ Mãi mãi' : formatDate(tr.expiryDate)}</strong></td>
      <td>${tr.hasEvidence ? `<a href="${tr.certificateUrl}" target="_blank" class="badge badge-green" style="text-decoration:none;">📎 Xem ảnh</a>` : `<span class="badge badge-red">⚠️ Chưa có</span>`}</td>
      <td><span class="badge ${tr.badgeClass}">${tr.statusLabel}</span></td>
    </tr>`).join('');
  applyMobileDataLabels('trainingsTable');


  document.getElementById('trainingsPageInfo').textContent = `Đang hiển thị ${startIndex + 1} - ${endIndex} / ${allTrainingsList.length} bản ghi`;
  document.getElementById('trainingsPageButtons').innerHTML = generatePaginationButtons(currentTrainingsPage, totalPages, 'goToTrainingsPage');
}

window.goToTrainingsPage = function(page) {
  currentTrainingsPage = page;
  renderTrainingsTable();
};

function generatePaginationButtons(currentPage, totalPages, goToPageFuncName) {
  let html = '';
  // Prev button
  html += `<button class="btn-page" onclick="${goToPageFuncName}(${currentPage - 1})" ${currentPage === 1 ? 'disabled' : ''}>Trước</button>`;

  // Calculate range of pages to show
  let startPage = Math.max(1, currentPage - 2);
  let endPage = Math.min(totalPages, currentPage + 2);

  if (startPage > 1) {
    html += `<button class="btn-page" onclick="${goToPageFuncName}(1)">1</button>`;
    if (startPage > 2) html += `<span class="btn-page" style="pointer-events:none;background:transparent;border:none;">...</span>`;
  }

  for (let i = startPage; i <= endPage; i++) {
    const activeClass = i === currentPage ? 'active' : '';
    const style = i === currentPage ? 'background: var(--primary); color: white; border-color: var(--primary);' : '';
    html += `<button class="btn-page" style="${style}" onclick="${goToPageFuncName}(${i})">${i}</button>`;
  }

  if (endPage < totalPages) {
    if (endPage < totalPages - 1) html += `<span class="btn-page" style="pointer-events:none;background:transparent;border:none;">...</span>`;
    html += `<button class="btn-page" onclick="${goToPageFuncName}(${totalPages})">${totalPages}</button>`;
  }

  // Next button
  html += `<button class="btn-page" onclick="${goToPageFuncName}(${currentPage + 1})" ${currentPage === totalPages ? 'disabled' : ''}>Sau</button>`;
  
  return html;
}

// ─────────────────────────────────────────────────────────────
//  RENDER: SETTINGS
// ─────────────────────────────────────────────────────────────
async function renderSettings() {
  try {
    const s = await api('/settings');
    document.getElementById('warn30').value = s.urgentWarningDays;
    document.getElementById('warn60').value = s.expiryWarningDays;
    document.getElementById('req1y').value  = s.requiredHours1Year;
    document.getElementById('req2y').value  = s.requiredHours2Years;
    document.getElementById('req5y').value  = s.requiredHours5Years;
    settings = {
      urgentWarningDays:  s.urgentWarningDays,
      expiryWarningDays:  s.expiryWarningDays,
      requiredHours1Year: s.requiredHours1Year,
      requiredHours2Years:s.requiredHours2Years,
      requiredHours5Years:s.requiredHours5Years,
    };
  } catch (_) {}

  try {
    departments = await api('/departments');
    renderDeptList();
  } catch (_) {}
}

function renderDeptList() {
  const el = document.getElementById('deptList');
  if (!el) return;
  el.innerHTML = departments.map(d => `
    <span class="dept-tag">
      ${d.departmentName}
      <span style="font-size:11px;color:rgba(15,118,110,.5);">(${d.employeeCount})</span>
    </span>
  `).join('');
}

async function saveSettings() {
  try {
    await api('/settings', {
      method: 'PUT',
      body: JSON.stringify({
        urgentWarningDays:  parseInt(document.getElementById('warn30').value) || 30,
        expiryWarningDays:  parseInt(document.getElementById('warn60').value) || 60,
        requiredHours1Year: parseInt(document.getElementById('req1y').value)  || 24,
        requiredHours2Years:parseInt(document.getElementById('req2y').value)  || 48,
        requiredHours5Years:parseInt(document.getElementById('req5y').value)  || 120,
      }),
    });
    await renderSettings();
    await updateAlertBadge();
    showToast('✅ Đã lưu cài đặt thành công!');
  } catch (_) {}
}

async function resetSettings() {
  document.getElementById('warn30').value = 30;
  document.getElementById('warn60').value = 60;
  document.getElementById('req1y').value  = 24;
  document.getElementById('req2y').value  = 48;
  document.getElementById('req5y').value  = 120;
  await saveSettings();
  showToast('Đã khôi phục cài đặt mặc định', 'warning');
}

// ─────────────────────────────────────────────────────────────
//  MODALS
// ─────────────────────────────────────────────────────────────
async function openModal(type) {
  const overlay = document.getElementById('modalOverlay');
  const title   = document.getElementById('modalTitle');
  const body    = document.getElementById('modalBody');
  overlay.classList.add('open');

  if (type === 'addEmployee') {
    // Load departments
    let deptOptions = '';
    try {
      const depts = await api('/departments');
      deptOptions = depts.map(d =>
        `<option value="${d.departmentId}">${d.departmentName}</option>`).join('');
    } catch (_) {}

    title.textContent = 'Thêm Nhân viên mới';
    body.innerHTML = `
      <div class="form-grid">
        <div class="form-group">
          <label>Mã nhân viên *</label>
          <input type="text" id="fEmpCode" placeholder="Vd: NV026" />
        </div>
        <div class="form-group">
          <label>Giới tính</label>
          <select id="fGender"><option>Nam</option><option>Nữ</option></select>
        </div>
      </div>
      <div class="form-group">
        <label>Họ và tên *</label>
        <input type="text" id="fFullName" placeholder="Nguyễn Văn A" />
      </div>
      <div class="form-grid">
        <div class="form-group">
          <label>Ngày sinh</label>
          <input type="date" id="fDob" />
        </div>
        <div class="form-group">
          <label>Ngày vào làm</label>
          <input type="date" id="fJoinDate" />
        </div>
      </div>
      <div class="form-group">
        <label>Phòng ban *</label>
        <select id="fDept">${deptOptions}</select>
      </div>
      <div class="form-group">
        <label>Chức danh *</label>
        <input type="text" id="fRole" placeholder="Vd: Bác sĩ, Điều dưỡng..." />
      </div>
      <div class="form-actions">
        <button class="btn-secondary" onclick="closeModal()">Hủy</button>
        <button class="btn-primary" onclick="saveEmployee()">💾 Lưu nhân viên</button>
      </div>
    `;
  }
  else if (type === 'addTraining' || type === 'addTrainingForEmployee') {
    // Load employees + courses
    let empOptions = '', courseOptions = '';
    try {
      const [emps, crs] = await Promise.all([api('/employees'), api('/courses')]);
      empOptions = emps.map(e =>
        `<option value="${e.employeeId}" ${e.employeeId === currentDetailEmpId ? 'selected' : ''}>${e.fullName} (${e.employeeCode})</option>`).join('');
      courseOptions = crs.map(c =>
        `<option value="${c.courseId}" data-hours="${c.defaultHours}" data-organizer="${c.organizer}" data-is-lifetime="${c.isLifetime}" data-renewal-years="${c.requiresRenewalAfterYears || 0}">${c.courseName}${c.isLifetime ? ' ♾️' : c.requiresRenewalAfterYears ? ` (${c.requiresRenewalAfterYears}n)` : ''}</option>`).join('');
    } catch (_) {}

    title.textContent = 'Thêm Chứng chỉ Đào tạo';
    body.innerHTML = `
      <div class="form-group">
        <label>Nhân viên *</label>
        <select id="fTrainEmp">
          <option value="">— Chọn nhân viên —</option>
          ${empOptions}
        </select>
      </div>
      <div class="form-group">
        <label>Chọn khóa học (hoặc nhập tự do bên dưới)</label>
        <select id="fCourse" onchange="onCourseSelect()">
          <option value="0">— Chọn khóa học —</option>
          ${courseOptions}
        </select>
      </div>
      <!-- Loại chứng chỉ -->
      <div class="cert-type-selector">
        <label class="cert-type-label">Loại chứng chỉ</label>
        <div class="cert-type-options">
          <label class="cert-type-option" id="certTypeExpiry">
            <input type="radio" name="certType" value="expiry" checked onchange="onCertTypeChange()" />
            <span class="cert-type-icon">⏳</span>
            <span>Có thời hạn</span>
          </label>
          <label class="cert-type-option" id="certTypeLifetime">
            <input type="radio" name="certType" value="lifetime" onchange="onCertTypeChange()" />
            <span class="cert-type-icon">♾️</span>
            <span>Vĩnh viễn (Không cần học lại)</span>
          </label>
        </div>
        <!-- Cảnh báo học lại -->
        <div class="renewal-warning" id="renewalWarning" style="display:none;">
          ⚠️ Chứng chỉ này có thời hạn - cần đào tạo lại sau <strong id="renewalYearsText">N</strong> năm
        </div>
      </div>
      <div class="form-group">
        <label>Tên khóa học / Chứng chỉ *</label>
        <input type="text" id="fCourseName" placeholder="Vd: Cấp cứu tim mạch nâng cao (ACLS)" />
      </div>
      <div class="form-group">
        <label>Đơn vị tổ chức *</label>
        <input type="text" id="fOrganizer" placeholder="Vd: Hội Tim mạch Việt Nam" />
      </div>
      <div class="form-grid">
        <div class="form-group">
          <label>Số tiết quy định *</label>
          <input type="number" id="fHours" placeholder="24" min="1" />
        </div>
        <div class="form-group">
          <label>Số tiết thực tế *</label>
          <input type="number" id="fActualHours" placeholder="24" min="0" />
        </div>
      </div>
      <div class="form-grid" id="dateSection">
        <div class="form-group">
          <label>Ngày cấp *</label>
          <input type="date" id="fIssue" onchange="onIssueDateChange()" />
        </div>
        <div class="form-group" id="expiryGroup">
          <label>Ngày hết hạn</label>
          <input type="date" id="fExpiry" placeholder="Tự động tính nếu có số năm học lại" />
        </div>
      </div>
      <div class="form-group">
        <label>File minh chứng (Ảnh/PDF)</label>
        <div class="upload-zone" onclick="document.getElementById('fEvidenceFile').click()" style="border: 2px dashed var(--border); border-radius: 8px; padding: 20px; text-align: center; cursor: pointer; background: var(--surface-2); margin-top: 8px; transition: all 0.2s;">
          <input type="file" id="fEvidenceFile" accept=".jpg,.jpeg,.png,.pdf,.webp" style="display:none" onchange="previewEvidence(this)" />
          <div id="uploadPreview" style="color: var(--text-muted);">
             <div style="font-size: 24px; margin-bottom: 8px;">📁</div>
             <span>Kéo thả file hoặc <b>Click</b> để chọn ảnh/PDF</span>
          </div>
        </div>
      </div>
      <div class="form-actions" style="margin-top: 24px;">
        <button class="btn-secondary" onclick="closeModal()">Hủy</button>
        <button class="btn-primary" onclick="saveTraining()">💾 Lưu chứng chỉ</button>
      </div>
    `;

    // Initialize searchable select
    setTimeout(() => {
      if (document.getElementById('fTrainEmp')) {
        new TomSelect('#fTrainEmp', {
          create: false,
          sortField: { field: "text", direction: "asc" }
        });
      }
    }, 10);
  }
}

function onCourseSelect() {
  const sel = document.getElementById('fCourse');
  const opt = sel.options[sel.selectedIndex];
  if (opt && opt.value !== '0') {
    document.getElementById('fCourseName').value  = opt.text;
    document.getElementById('fOrganizer').value   = opt.dataset.organizer || '';
    document.getElementById('fHours').value       = opt.dataset.hours || '';
    if (document.getElementById('fActualHours')) document.getElementById('fActualHours').value = opt.dataset.hours || '';
    // Cập nhật loại chứng chỉ nếu course có IsLifetime
    const isLtm = opt.dataset.isLifetime === 'true';
    const renewalYears = opt.dataset.renewalYears;
    if (isLtm) {
      document.querySelector('input[name="certType"][value="lifetime"]').checked = true;
    } else {
      document.querySelector('input[name="certType"][value="expiry"]').checked = true;
    }
    onCertTypeChange(renewalYears ? parseInt(renewalYears) : null);
  }
}

// Khi thay đổi loại chứng chỉ
function onCertTypeChange(renewalYears) {
  const isLifetime = document.querySelector('input[name="certType"]:checked')?.value === 'lifetime';
  const expiryGroup = document.getElementById('expiryGroup');
  const renewalWarn = document.getElementById('renewalWarning');
  if (isLifetime) {
    if (expiryGroup) expiryGroup.style.display = 'none';
    if (renewalWarn) renewalWarn.style.display = 'none';
    const fExpiry = document.getElementById('fExpiry');
    if (fExpiry) fExpiry.value = '';
  } else {
    if (expiryGroup) expiryGroup.style.display = '';
    if (renewalWarn && renewalYears) {
      document.getElementById('renewalYearsText').textContent = renewalYears;
      renewalWarn.style.display = 'block';
    } else if (renewalWarn) {
      renewalWarn.style.display = 'none';
    }
  }
}

// Tự động tính ExpiryDate khi có IssueDate + số năm học lại
function onIssueDateChange() {
  const isLifetime = document.querySelector('input[name="certType"]:checked')?.value === 'lifetime';
  if (isLifetime) return;
  const issueVal = document.getElementById('fIssue')?.value;
  const sel = document.getElementById('fCourse');
  const opt = sel ? sel.options[sel.selectedIndex] : null;
  const renewalYears = opt ? parseInt(opt.dataset.renewalYears) : 0;
  if (issueVal && renewalYears > 0) {
    const issueDate = new Date(issueVal);
    issueDate.setFullYear(issueDate.getFullYear() + renewalYears);
    const yyyy = issueDate.getFullYear();
    const mm   = String(issueDate.getMonth() + 1).padStart(2, '0');
    const dd   = String(issueDate.getDate()).padStart(2, '0');
    document.getElementById('fExpiry').value = `${yyyy}-${mm}-${dd}`;
  }
}

function closeModal() {
  document.getElementById('modalOverlay').classList.remove('open');
}

// ─────────────────────────────────────────────────────────────
//  DASHBOARD THEO PHÒNG BAN
// ─────────────────────────────────────────────────────────────
async function renderDeptDashboard() {
  const grid = document.getElementById('deptDashboardGrid');
  if (!grid) return;
  try {
    const depts = await api('/dashboard/by-department');
    if (!depts || !depts.length) {
      grid.innerHTML = '<div style="text-align:center;padding:40px;color:var(--text-muted);grid-column:1/-1">✅ Tất cả phòng ban đều đạt yêu cầu!</div>';
      return;
    }
    grid.innerHTML = depts.map(dept => {
      const alertColors = { green: '#10b981', amber: '#f59e0b', orange: '#f97316', red: '#ef4444' };
      const color = alertColors[dept.alertLevel] || '#10b981';
      const pct = dept.compliancePercent;
      return `
      <div class="dept-card" style="--dept-color:${color}" id="deptCard_${dept.departmentId}">
        <div class="dept-card-header" onclick="toggleDeptExpand(${dept.departmentId})">
          <div class="dept-card-title">
            <span class="dept-alert-dot" style="background:${color}"></span>
            <strong>${dept.departmentName}</strong>
          </div>
          <div class="dept-card-stats">
            <span class="dept-stat compliant">✅ ${dept.compliantEmployees}</span>
            <span class="dept-stat total">${dept.totalEmployees} NV</span>
          </div>
          <div class="dept-compliance-bar">
            <div class="dept-bar-fill" style="width:${pct}%;background:${color}"></div>
          </div>
          <div class="dept-card-footer">
            <span class="dept-pct" style="color:${color}">${pct}% đạt</span>
            ${dept.expiredCertificates > 0 ? `<span class="dept-warn-badge">🔴 ${dept.expiredCertificates} hết hạn</span>` : ''}
            ${dept.expiringCertificates > 0 ? `<span class="dept-warn-badge amber">🟡 ${dept.expiringCertificates} sắp hết</span>` : ''}
          </div>
          <div class="dept-expand-icon" id="deptExpandIcon_${dept.departmentId}">▼</div>
        </div>
        <div class="dept-emp-list" id="deptEmpList_${dept.departmentId}" style="display:none;">
          ${dept.employees.map(emp => `
            <div class="dept-emp-row ${emp.isCompliant ? '' : 'non-compliant'}">
              <div class="dept-emp-info">
                <div class="dept-emp-name">${emp.fullName}</div>
                <div class="dept-emp-pos">${emp.position}</div>
              </div>
              <div class="dept-emp-status">
                <span class="badge ${emp.isCompliant ? 'badge-green' : 'badge-red'}">
                  ${emp.isCompliant ? '✅ Đạt' : `❌ Thiếu ${emp.missingHours} tiết`}
                </span>
                ${emp.expiredCerts > 0 ? `<span class="badge badge-red">🔴 ${emp.expiredCerts} CC hết hạn</span>` : ''}
                ${emp.expiringCerts > 0 ? `<span class="badge badge-orange">🟠 ${emp.expiringCerts} sắp hết</span>` : ''}
              </div>
              <button class="btn-view-emp" onclick="viewEmployee(${emp.employeeId})" title="Xem chi tiết">
                <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"/><circle cx="12" cy="12" r="3"/></svg>
                Xem
              </button>
            </div>`).join('')}
        </div>
      </div>`;
    }).join('');
  } catch (e) {
    if (grid) grid.innerHTML = '<div style="text-align:center;padding:20px;color:var(--text-muted);grid-column:1/-1">⚠️ Không tải được thống kê phòng ban</div>';
  }
}

function toggleDeptExpand(deptId) {
  const list = document.getElementById(`deptEmpList_${deptId}`);
  const icon = document.getElementById(`deptExpandIcon_${deptId}`);
  if (!list) return;
  if (list.style.display === 'none') {
    list.style.display = 'block';
    if (icon) icon.textContent = '▲';
  } else {
    list.style.display = 'none';
    if (icon) icon.textContent = '▼';
  }
}

// ─────────────────────────────────────────────────────────────
//  NOTIFICATION DROPDOWN
// ─────────────────────────────────────────────────────────────
function toggleNotifDropdown(e) {
  e.stopPropagation();
  const dd = document.getElementById('notifDropdown');
  if (!dd) return;
  const isOpen = dd.classList.contains('open');
  if (!isOpen) {
    dd.classList.add('open');
    renderNotificationDropdown();
  } else {
    dd.classList.remove('open');
  }
}

function closeNotifDropdown() {
  const dd = document.getElementById('notifDropdown');
  if (dd) dd.classList.remove('open');
}

// Đóng dropdown khi click ra ngoài
document.addEventListener('click', function(e) {
  const wrapper = document.getElementById('notifWrapper');
  if (wrapper && !wrapper.contains(e.target)) closeNotifDropdown();
});

async function renderNotificationDropdown() {
  const body = document.getElementById('notifDropdownBody');
  if (!body) return;
  try {
    const alerts = await api('/dashboard/alerts');
    // Chỉ lấy cảnh báo chứng chỉ sắp hết / đã hết hạn
    const certAlerts = alerts.filter(a => a.alertType === 'red' || a.alertType === 'orange' || a.alertType === 'amber');
    const urgent = certAlerts.length;
    document.getElementById('notifCount').textContent = urgent;
    document.getElementById('alertBadge').textContent = urgent;

    if (!certAlerts.length) {
      body.innerHTML = '<div style="text-align:center;padding:20px;font-size:13px;color:var(--text-muted);"><div style="font-size:24px;margin-bottom:8px;">✅</div>Không có cảnh báo mới!</div>';
      return;
    }

    body.innerHTML = certAlerts.slice(0, 10).map(a => {
      const icon = a.alertType === 'red' ? '🔴' : a.alertType === 'orange' ? '🟠' : '🟡';
      const daysText = a.daysLeft < 0
        ? `<span style="color:#ef4444;font-weight:700;">Quá hạn ${Math.abs(a.daysLeft)} ngày</span>`
        : `<span style="color:#f97316;font-weight:600;">Còn ${a.daysLeft} ngày</span>`;
      return `
      <div class="notif-item" onclick="viewEmployee(${a.employeeId});closeNotifDropdown()">
        <div class="notif-item-icon">${icon}</div>
        <div class="notif-item-content">
          <div class="notif-item-name">${a.employeeName}</div>
          <div class="notif-item-course">${a.courseName}</div>
          <div class="notif-item-dates">
            <span>📅 Cấp: <strong>${formatDate(a.issueDate)}</strong></span>
            <span>⏰ Hạn: <strong>${formatDate(a.expiryDate)}</strong></span>
          </div>
          <div>${daysText}</div>
        </div>
      </div>`;
    }).join('');
  } catch (e) {
    if (body) body.innerHTML = '<div style="padding:16px;color:var(--text-muted);font-size:13px;">Không tải được thông báo</div>';
  }
}

async function saveEmployee() {
  const code     = document.getElementById('fEmpCode')?.value.trim();
  const fullName = document.getElementById('fFullName')?.value.trim();
  const role     = document.getElementById('fRole')?.value.trim();
  const deptId   = parseInt(document.getElementById('fDept')?.value);

  if (!code || !fullName || !role || !deptId) {
    showToast('Vui lòng điền đầy đủ thông tin bắt buộc!', 'error');
    return;
  }

  try {
    await api('/employees', {
      method: 'POST',
      body: JSON.stringify({
        employeeCode: code,
        fullName,
        gender:       document.getElementById('fGender')?.value,
        dateOfBirth:  document.getElementById('fDob')?.value || null,
        joinDate:     document.getElementById('fJoinDate')?.value || null,
        departmentId: deptId,
        position:     role,
      }),
    });
    closeModal();
    await renderEmployees();
    await updateAlertBadge();
    showToast(`✅ Đã thêm nhân viên ${fullName}!`);
  } catch (_) {}
}

async function saveTraining() {
  const empId      = parseInt(document.getElementById('fTrainEmp')?.value);
  const courseId   = parseInt(document.getElementById('fCourse')?.value || '0');
  const courseName = document.getElementById('fCourseName')?.value.trim();
  const organizer  = document.getElementById('fOrganizer')?.value.trim();
  const hours      = parseInt(document.getElementById('fHours')?.value);
  const actualH    = parseInt(document.getElementById('fActualHours')?.value);
  const issueDate  = document.getElementById('fIssue')?.value;
  const expiryDate = document.getElementById('fExpiry')?.value;
  const fileInput  = document.getElementById('fEvidenceFile');

  if (!empId || !courseName || !organizer || isNaN(hours) || isNaN(actualH) || !issueDate || !expiryDate) {
    showToast('Vui lòng điền đầy đủ thông tin bắt buộc!', 'error');
    return;
  }

  try {
    const res = await api('/trainings', {
      method: 'POST',
      body: JSON.stringify({ employeeId: empId, courseId, courseName, organizer, trainingHours: hours, actualHours: actualH, issueDate, expiryDate }),
    });

    if (fileInput && fileInput.files.length > 0) {
      const formData = new FormData();
      formData.append('file', fileInput.files[0]);
      try {
        const upRes = await fetch(`${API_BASE}/upload/training/${res.trainingId}`, {
          method: 'POST',
          body: formData
        });
        if (!upRes.ok) throw new Error('Upload failed');
      } catch (err) {
        showToast('⚠️ Lưu chứng chỉ thành công nhưng upload minh chứng bị lỗi!', 'warning');
      }
    }

    closeModal();
    if (currentDetailEmpId === empId) await viewEmployee(empId);
    await renderTrainings();
    await updateAlertBadge();
    showToast('✅ Đã thêm chứng chỉ đào tạo thành công!');
  } catch (_) {}
}

function previewEvidence(input) {
  const preview = document.getElementById('uploadPreview');
  if (input.files && input.files[0]) {
    const file = input.files[0];
    const isImage = file.type.startsWith('image/');
    if (isImage) {
      const reader = new FileReader();
      reader.onload = function(e) {
        preview.innerHTML = `<img src="${e.target.result}" style="max-height: 120px; border-radius: 4px;" /><div style="margin-top: 8px; font-size: 12px;">${file.name}</div>`;
      }
      reader.readAsDataURL(file);
    } else {
      preview.innerHTML = `<div style="font-size: 24px; margin-bottom: 8px;">📄</div><div style="font-weight: 500;">${file.name}</div>`;
    }
  }
}

// ─────────────────────────────────────────────────────────────
//  SIDEBAR TOGGLE
// ─────────────────────────────────────────────────────────────
document.getElementById('sidebarToggle').addEventListener('click', () => {
  document.getElementById('sidebar').classList.toggle('collapsed');
  document.getElementById('mainContent').classList.toggle('sidebar-collapsed');
});

document.getElementById('mobileToggle').addEventListener('click', () => {
  document.getElementById('sidebar').classList.toggle('mobile-open');
  const overlay = document.getElementById('sidebarOverlay');
  if (overlay) overlay.classList.toggle('show');
});

function closeMobileSidebar() {
  document.getElementById('sidebar').classList.remove('mobile-open');
  const overlay = document.getElementById('sidebarOverlay');
  if (overlay) overlay.classList.remove('show');
}


// ─────────────────────────────────────────────────────────────
//  XÁC THỰC & TÀI KHOẢN (AUTHENTICATION & AUTHORIZATION)
// ─────────────────────────────────────────────────────────────
function togglePasswordVisibility(inputId) {
  const input = document.getElementById(inputId);
  if (input) {
    input.type = input.type === 'password' ? 'text' : 'password';
  }
}

function toggleUserMenu(event) {
  event.stopPropagation();
  document.getElementById('userPopupMenu').classList.toggle('show');
}

// Đóng menu tài khoản khi bấm ngoài
document.addEventListener('click', () => {
  const menu = document.getElementById('userPopupMenu');
  if (menu) menu.classList.remove('show');
});

function initAuth() {
  const userStr = localStorage.getItem('user') || sessionStorage.getItem('user');
  const token = localStorage.getItem('token') || sessionStorage.getItem('token');
  
  if (userStr && token) {
    currentUser = JSON.parse(userStr);
    currentToken = token;
    
    document.getElementById('loginPage').style.display = 'none';
    
    document.getElementById('currentUserName').textContent = currentUser.fullName;
    document.getElementById('currentUserRole').textContent = `Vai trò: ${currentUser.role === 'User' ? 'Nhân viên' : currentUser.role}`;
    document.getElementById('currentUserAvatar').textContent = currentUser.fullName.split(' ').pop().slice(0, 2).toUpperCase();
    
    applyRolePermissions(currentUser.role);
    if (currentUser.role === 'User') {
      viewMyProfile();
    } else {
      runInitApis();
    }
  } else {
    document.getElementById('loginPage').style.display = 'flex';
  }
}

function applyRolePermissions(role) {
  // Reset: Ẩn tất cả các thành phần phân quyền
  document.querySelectorAll('.HR-only, .ADMIN-only, .MANAGER-only, .USER-only').forEach(el => el.classList.add('role-hidden'));
  
  if (role === 'Admin') {
    document.querySelectorAll('.ADMIN-only').forEach(el => el.classList.remove('role-hidden'));
  } 
  else if (role === 'HR') {
    document.querySelectorAll('.HR-only').forEach(el => el.classList.remove('role-hidden'));
    
    // HR không được quyền xem cài đặt
    document.getElementById('nav-settings')?.classList.add('role-hidden');
    document.getElementById('mnav-settings')?.classList.add('role-hidden');
  } 
  else if (role === 'Manager') {
    document.querySelectorAll('.MANAGER-only').forEach(el => el.classList.remove('role-hidden'));
    
    // Manager không được quyền xem cài đặt
    document.getElementById('nav-settings')?.classList.add('role-hidden');
    document.getElementById('mnav-settings')?.classList.add('role-hidden');
  } 
  else if (role === 'Viewer') {
    // Viewer chỉ xem được Dashboard
    document.getElementById('nav-dashboard')?.classList.remove('role-hidden');
    document.getElementById('mnav-dashboard')?.classList.remove('role-hidden');
    document.querySelectorAll('.nav-section-label').forEach(el => {
      if(el.textContent.includes('Tổng quan')) el.classList.remove('role-hidden');
    });
  } 
  else if (role === 'User') {
    document.querySelectorAll('.USER-only').forEach(el => el.classList.remove('role-hidden'));
  }

  // Quản lý hiển thị Hồ sơ cá nhân
  if (role !== 'User') {
    document.getElementById('nav-my-profile')?.classList.add('role-hidden');
    document.getElementById('mnav-my-profile')?.classList.add('role-hidden');
  } else {
    document.getElementById('nav-my-profile')?.classList.remove('role-hidden');
    document.getElementById('mnav-my-profile')?.classList.remove('role-hidden');
  }
}

async function handleLogin() {
  const usernameInput = document.getElementById('loginUsername');
  const passwordInput = document.getElementById('loginPassword');
  const rememberMeInput = document.getElementById('loginRememberMe');
  const errorDiv = document.getElementById('loginError');
  
  const username = usernameInput.value.trim();
  const password = passwordInput.value;
  const rememberMe = rememberMeInput.checked;
  
  if (!username || !password) {
    errorDiv.textContent = 'Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu!';
    return;
  }
  
  errorDiv.textContent = '';
  
  try {
    const res = await fetch(`${API_BASE}/auth/login`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ username, password })
    });
    
    if (!res.ok) {
      const err = await res.json().catch(() => ({ message: 'Lỗi đăng nhập!' }));
      throw new Error(err.message || 'Mật khẩu hoặc tài khoản sai!');
    }
    
    const data = await res.json();
    currentUser = { 
      userId: data.userId, 
      username: data.username, 
      fullName: data.fullName, 
      role: data.role, 
      employeeId: data.employeeId,
      requirePasswordChange: data.requirePasswordChange 
    };
    currentToken = data.token;
    
    const storage = rememberMe ? localStorage : sessionStorage;
    storage.setItem('user', JSON.stringify(currentUser));
    storage.setItem('token', currentToken);
    
    document.getElementById('loginPage').style.display = 'none';
    
    document.getElementById('currentUserName').textContent = currentUser.fullName;
    document.getElementById('currentUserRole').textContent = `Vai trò: ${currentUser.role === 'User' ? 'Nhân viên' : currentUser.role}`;
    document.getElementById('currentUserAvatar').textContent = currentUser.fullName.split(' ').pop().slice(0, 2).toUpperCase();
    
    applyRolePermissions(currentUser.role);
    
    usernameInput.value = '';
    passwordInput.value = '';
    
    if (currentUser.role === 'User') {
      showPage('my-dashboard');
      loadMyDashboard();
    } else {
      runInitApis();
    }
    
    showToast('👋 Đăng nhập thành công!');
    
    if (currentUser.requirePasswordChange) {
      setTimeout(() => {
        openChangePasswordModal(null, true);
        showToast('Vui lòng đổi mật khẩu mới ở lần đăng nhập đầu tiên!', 'warning');
      }, 500);
    }
  } catch (err) {
    errorDiv.textContent = err.message;
  }
}

function handleLogout(event) {
  if (event) event.stopPropagation();
  
  currentUser = null;
  currentToken = null;
  
  localStorage.removeItem('user');
  localStorage.removeItem('token');
  sessionStorage.removeItem('user');
  sessionStorage.removeItem('token');
  
  document.getElementById('loginPage').style.display = 'flex';
  showToast('🚪 Đăng xuất thành công!', 'warning');
}

function openChangePasswordModal(event, force = false) {
  if (event) event.stopPropagation();
  document.getElementById('pwOld').value = '';
  document.getElementById('pwNew').value = '';
  document.getElementById('pwConfirm').value = '';
  document.getElementById('changePasswordError').textContent = '';
  
  const cancelBtn = document.querySelector('#changePasswordOverlay .btn-secondary');
  if (cancelBtn) {
    cancelBtn.style.display = force ? 'none' : 'block';
  }
  
  // Prevent closing by clicking outside if forced
  const overlay = document.getElementById('changePasswordOverlay');
  if (force) {
    overlay.onclick = null; 
  } else {
    overlay.onclick = (e) => {
      if (e.target === overlay) closeChangePasswordModal();
    };
  }
  
  overlay.classList.add('open');
}

function closeChangePasswordModal() {
  if (currentUser && currentUser.requirePasswordChange) {
    showToast('Bạn bắt buộc phải đổi mật khẩu!', 'warning');
    return;
  }
  document.getElementById('changePasswordOverlay').classList.remove('open');
}

async function submitChangePassword() {
  const oldPw = document.getElementById('pwOld').value;
  const newPw = document.getElementById('pwNew').value;
  const confirmPw = document.getElementById('pwConfirm').value;
  const errorDiv = document.getElementById('changePasswordError');
  
  if (!oldPw || !newPw || !confirmPw) {
    errorDiv.textContent = 'Vui lòng điền đầy đủ các trường bắt buộc!';
    return;
  }
  
  if (newPw.length < 8) {
    errorDiv.textContent = 'Mật khẩu mới phải có ít nhất 8 ký tự!';
    return;
  }
  
  if (newPw === oldPw) {
    errorDiv.textContent = 'Mật khẩu mới không được trùng với mật khẩu cũ!';
    return;
  }
  
  if (newPw !== confirmPw) {
    errorDiv.textContent = 'Xác nhận mật khẩu mới không khớp!';
    return;
  }
  
  errorDiv.textContent = '';
  
  try {
    await api('/auth/change-password', {
      method: 'POST',
      body: JSON.stringify({
        username: currentUser.username,
        currentPassword: oldPw,
        newPassword: newPw,
        confirmPassword: confirmPw
      })
    });
    
    closeChangePasswordModal();
    showToast('🔑 Đổi mật khẩu thành công! Vui lòng đăng nhập lại.', 'success');
    setTimeout(handleLogout, 1000);
  } catch (err) {
    errorDiv.textContent = err.message;
  }
}

// ─────────────────────────────────────────────────────────────
//  EXCEL IMPORT & EXPORT (SHEETJS INTEGRATION)
// ─────────────────────────────────────────────────────────────
function exportEmployeesExcel() {
  api('/employees').then(list => {
    if (!list || list.length === 0) {
      showToast('Không có dữ liệu để xuất file!', 'warning');
      return;
    }
    
    const data = list.map(emp => ({
      'Mã Nhân Viên': emp.employeeCode,
      'Họ và Tên': emp.fullName,
      'Giới tính': emp.gender,
      'Phòng Ban': emp.departmentName,
      'Chức Danh': emp.position,
      'Ngày Vào Làm': formatDate(emp.joinDate),
      'Tổng Tiết CME (2 năm)': emp.totalHours,
      'Yêu cầu CME': settings.requiredHours2Years,
      'Còn thiếu (tiết)': emp.missingHours,
      'Trạng Thái CME': emp.isCompliant ? 'Đạt' : 'Chưa đạt'
    }));
    
    const ws = XLSX.utils.json_to_sheet(data);
    const wscols = [
      {wch: 15}, {wch: 25}, {wch: 12}, {wch: 25}, {wch: 20},
      {wch: 15}, {wch: 20}, {wch: 15}, {wch: 15}, {wch: 15}
    ];
    ws['!cols'] = wscols;
    
    const wb = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(wb, ws, 'Danh sách CME');
    
    XLSX.writeFile(wb, `Danh_Sach_CME_Hoan_My_${Date.now()}.xlsx`);
    showToast('📥 Xuất file Excel thành công!');
  }).catch(err => {
    showToast('❌ Lỗi khi xuất Excel: ' + err.message, 'error');
  });
}

function exportAlertsExcel() {
  api('/dashboard/alerts').then(alerts => {
    let list = alerts || [];
    if (currentAlertFilter === 'expired')     list = list.filter(a => a.alertType === 'red');
    else if (currentAlertFilter === 'expiring30') list = list.filter(a => a.alertType === 'orange');
    else if (currentAlertFilter === 'expiring60') list = list.filter(a => a.alertType === 'amber');
    else if (currentAlertFilter === 'missing')    list = list.filter(a => a.alertKind === 'missing');

    if (list.length === 0) {
      showToast('Không có dữ liệu để xuất file!', 'warning');
      return;
    }
    
    const data = list.map(a => ({
      'Mã Nhân Viên': a.employeeCode,
      'Họ và Tên': a.employeeName,
      'Phòng Ban': a.department,
      'Khóa Học / Vấn đề': a.courseName,
      'Ngày Hết Hạn': formatDate(a.expiryDate),
      'Còn Lại (ngày)': a.daysLeft !== null && a.daysLeft !== undefined ? a.daysLeft : '',
      'Trạng Thái': a.statusLabel
    }));
    
    const ws = XLSX.utils.json_to_sheet(data);
    const wscols = [ {wch: 15}, {wch: 25}, {wch: 25}, {wch: 40}, {wch: 15}, {wch: 15}, {wch: 20} ];
    ws['!cols'] = wscols;
    
    const wb = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(wb, ws, 'CanhBao');
    
    const d = new Date();
    const dateStr = `${d.getFullYear()}${String(d.getMonth()+1).padStart(2,'0')}${String(d.getDate()).padStart(2,'0')}`;
    XLSX.writeFile(wb, `Danh_Sach_Canh_Bao_CME_${dateStr}.xlsx`);
    showToast('📥 Xuất file Excel thành công!');
  }).catch(err => {
    showToast('❌ Lỗi khi xuất Excel: ' + err.message, 'error');
  });
}

async function exportTrainingsExcel() {
  try {
    // Tải SheetJS động nếu chưa có
    if (typeof XLSX === 'undefined') {
      await new Promise((resolve, reject) => {
        const script = document.createElement('script');
        script.src = 'https://cdn.jsdelivr.net/npm/xlsx@0.18.5/dist/xlsx.full.min.js';
        script.onload = resolve;
        script.onerror = reject;
        document.head.appendChild(script);
      });
    }

    const search    = document.getElementById('trainSearch')?.value || '';
    const statusVal = document.getElementById('trainStatusFilter')?.value || '';
    
    const params = new URLSearchParams();
    if (search)    params.set('search', search);
    if (statusVal) params.set('status', statusVal);

    const list = await api(`/trainings?${params}`);
    if (!list || list.length === 0) {
      showToast('Không có dữ liệu để xuất file!', 'warning');
      return;
    }
    
    const data = list.map((tr, index) => ({
      'STT': index + 1,
      'Mã Nhân Viên': tr.employeeCode || '',
      'Họ và Tên': tr.employeeName || '',
      'Phòng Ban': tr.departmentName || '',
      'Khóa Học': tr.courseName || '',
      'Đơn Vị Tổ Chức': tr.organizer || '',
      'Số Tiết (QĐ)': tr.trainingHours || 0,
      'Số Tiết (TT)': tr.actualHours || 0,
      'Ngày Cấp': tr.issueDate ? formatDate(tr.issueDate) : '',
      'Ngày Hết Hạn': tr.expiryDate ? formatDate(tr.expiryDate) : '',
      'Trạng Thái': tr.statusLabel || ''
    }));
    
    const ws = XLSX.utils.json_to_sheet(data);
    
    // Tự động căn chỉnh độ rộng cột
    const maxCols = Object.keys(data[0]);
    const colWidths = maxCols.map(key => {
      let maxLen = key.length;
      data.forEach(row => {
        const val = String(row[key] || '');
        if (val.length > maxLen) maxLen = val.length;
      });
      return { wch: maxLen + 2 };
    });
    ws['!cols'] = colWidths;
    
    const wb = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(wb, ws, 'DaoTao');
    
    const d = new Date();
    const dateStr = `${d.getFullYear()}${String(d.getMonth()+1).padStart(2,'0')}${String(d.getDate()).padStart(2,'0')}`;
    XLSX.writeFile(wb, `Danh_Sach_Dao_Tao_CME_${dateStr}.xlsx`);
    showToast('📥 Xuất file Excel thành công!');
  } catch (err) {
    console.error("Lỗi xuất Excel:", err);
    showToast('❌ Lỗi khi xuất Excel: ' + (err.message || 'Lỗi không xác định'), 'error');
  }
}

function importEmployeesExcel(input) {
  if (!input.files || input.files.length === 0) return;
  const file = input.files[0];
  
  const reader = new FileReader();
  reader.onload = async function(e) {
    try {
      const data = new Uint8Array(e.target.result);
      const workbook = XLSX.read(data, { type: 'array' });
      const firstSheetName = workbook.SheetNames[0];
      const worksheet = workbook.Sheets[firstSheetName];
      const jsonData = XLSX.utils.sheet_to_json(worksheet);
      
      if (jsonData.length === 0) {
        showToast('File Excel trống hoặc không đúng định dạng!', 'error');
        return;
      }
      
      const depts = await api('/departments');
      const deptMap = {};
      depts.forEach(d => {
        deptMap[d.departmentName.toLowerCase().trim()] = d.departmentId;
      });
      
      let importedCount = 0;
      let errorCount = 0;
      
      for (const row of jsonData) {
        const code = (row['Mã Nhân Viên'] || row['MaNV'] || row['EmployeeCode'] || '').toString().trim();
        const name = (row['Họ và Tên'] || row['HoTen'] || row['FullName'] || '').toString().trim();
        const gender = (row['Giới tính'] || row['GioiTinh'] || row['Gender'] || 'Nam').toString().trim();
        const deptName = (row['Phòng Ban'] || row['PhongBan'] || row['Department'] || '').toString().trim();
        const position = (row['Chức Danh'] || row['ChucDanh'] || row['Position'] || 'Nhân viên').toString().trim();
        const joinDateStr = row['Ngày Vào Làm'] || row['NgayVaoLam'] || row['JoinDate'] || null;
        
        if (!code || !name || !deptName) {
          errorCount++;
          continue;
        }
        
        let deptId = deptMap[deptName.toLowerCase().trim()];
        if (!deptId) {
          deptId = depts[0]?.departmentId || 1;
        }
        
        let joinDate = null;
        if (joinDateStr) {
          if (typeof joinDateStr === 'number') {
            const dateObj = XLSX.SSF.parse_date_code(joinDateStr);
            joinDate = `${dateObj.y}-${String(dateObj.m).padStart(2,'0')}-${String(dateObj.d).padStart(2,'0')}`;
          } else {
            const parts = joinDateStr.toString().split(/[\/\-]/);
            if (parts.length === 3) {
              if (parts[2].length === 4) {
                joinDate = `${parts[2]}-${parts[1].padStart(2,'0')}-${parts[0].padStart(2,'0')}`;
              } else if (parts[0].length === 4) {
                joinDate = `${parts[0]}-${parts[1].padStart(2,'0')}-${parts[2].padStart(2,'0')}`;
              }
            }
          }
        }
        
        try {
          await api('/employees', {
            method: 'POST',
            body: JSON.stringify({
              employeeCode: code,
              fullName: name,
              gender: gender,
              departmentId: deptId,
              position: position,
              joinDate: joinDate
            })
          });
          importedCount++;
        } catch (err) {
          errorCount++;
        }
      }
      
      showToast(`📤 Nhập thành công ${importedCount} nhân viên! (Lỗi: ${errorCount})`, importedCount > 0 ? 'success' : 'error');
      await renderEmployees();
      if (importedCount > 0) filterEmployees();
    } catch (e) {
      showToast('Lỗi khi đọc file: ' + e.message, 'error');
    }
  };
  reader.readAsArrayBuffer(file);
  input.value = '';
}

function importTrainingsExcel(input) {
  if (!input.files || input.files.length === 0) return;
  const file = input.files[0];
  
  const reader = new FileReader();
  reader.onload = async function(e) {
    try {
      const data = new Uint8Array(e.target.result);
      const workbook = XLSX.read(data, { type: 'array' });
      const firstSheetName = workbook.SheetNames[0];
      const worksheet = workbook.Sheets[firstSheetName];
      const jsonData = XLSX.utils.sheet_to_json(worksheet);
      
      if (jsonData.length === 0) {
        showToast('File Excel trống!', 'error');
        return;
      }

      // Pre-fetch employees to map code to id
      const emps = await api('/employees');
      const empMap = {};
      emps.forEach(e => empMap[e.employeeCode.toString().toLowerCase().trim()] = e.employeeId);
      
      let importedCount = 0;
      let errorCount = 0;
      
      for (const row of jsonData) {
        const empCode = (row['Mã Nhân Viên'] || row['MaNV'] || row['EmployeeCode'] || row['Mã HO'] || '').toString().trim();
        const courseName = (row['Tên Khóa Học'] || row['TenKhoaHoc'] || row['CourseName'] || row['TÊN HỘI THẢO / CHƯƠNG TRÌNH'] || row['Khóa Đào Tạo'] || '').toString().trim();
        const organizer = (row['Đơn Vị Tổ Chức'] || row['DonViToChuc'] || row['Organizer'] || row['ĐƠN VỊ TỔ CHỨC'] || '').toString().trim();
        
        let issueDateStr = row['Ngày Cấp'] || row['NgayCap'] || row['IssueDate'] || row['Ngày bắt đầu đào tạo'];
        let expiryDateStr = row['Ngày Hết Hạn'] || row['NgayHetHan'] || row['ExpiryDate'] || row['NGÀY HẾT HẠN'] || row['Ngày kết thúc đào tạo'];
        
        // Convert Excel serial date to string if needed
        if (typeof issueDateStr === 'number') {
           const d = new Date((issueDateStr - (25567 + 2)) * 86400 * 1000);
           issueDateStr = d.toISOString().split('T')[0];
        }
        if (typeof expiryDateStr === 'number') {
           const d = new Date((expiryDateStr - (25567 + 2)) * 86400 * 1000);
           expiryDateStr = d.toISOString().split('T')[0];
        }

        const hoursStr = row['Số Tiết'] || row['SoTiet'] || row['Hours'] || row['SỐ TIẾT ĐÀO TẠO (ĐÃ QUY ĐỔI)'] || row['GIỜ TÍN CHỈ'];
        const hours = parseInt(hoursStr) || 0;
        
        if (!empCode || !courseName) {
          errorCount++;
          continue;
        }

        const empId = empMap[empCode.toLowerCase()];
        if (!empId) {
           errorCount++;
           continue; // cannot find employee
        }

        try {
          await api('/trainings', {
            method: 'POST',
            body: JSON.stringify({
              employeeId: empId,
              courseId: 0,
              courseName: courseName,
              organizer: organizer || 'Không rõ',
              issueDate: issueDateStr || new Date().toISOString().split('T')[0],
              expiryDate: expiryDateStr || new Date().toISOString().split('T')[0],
              trainingHours: hours,
              certificateUrl: (row['Minh Chứng'] || row['Link'] || '').toString().trim()
            })
          });
          importedCount++;
        } catch (err) {
          errorCount++;
        }
      }
      
      showToast(`Đã nhập ${importedCount} chứng chỉ. ${errorCount > 0 ? `Bỏ qua ${errorCount} dòng lỗi.` : ''}`, importedCount > 0 ? 'success' : 'error');
      if (importedCount > 0) filterTrainings();
    } catch (e) {
      showToast('Lỗi khi đọc file: ' + e.message, 'error');
    }
  };
  reader.readAsArrayBuffer(file);
  input.value = '';
}

// ─────────────────────────────────────────────────────────────
//  QUẢN LÝ TÀI KHOẢN & PHÂN QUYỀN (USER MANAGEMENT)
// ─────────────────────────────────────────────────────────────
function viewMyProfile() {
  if (currentUser && currentUser.employeeId) {
    viewEmployee(currentUser.employeeId);
  } else {
    showPage('employee-detail');
    document.getElementById('detailEmpName').textContent = 'Tài khoản chưa liên kết';
    document.getElementById('detailEmpDept').textContent = 'Vui lòng liên hệ Admin để liên kết với nhân viên!';
    document.getElementById('detailProfile').innerHTML = '<div class="empty-state"><p>Chưa liên kết với nhân viên nào.</p></div>';
    document.getElementById('detailCME').innerHTML = '';
    document.getElementById('detailTrainingsBody').innerHTML = '';
  }
  closeMobileSidebar();
}

async function renderUsers() {
  const tbody = document.getElementById('usersTableBody');
  if (!tbody) return;
  tbody.innerHTML = `<tr><td colspan="7"><div style="text-align:center;padding:30px;color:var(--text-muted);">Đang tải...</div></td></tr>`;
  
  try {
    allUsersList = await api('/users');
    filterUsers();
  } catch(e) {
    tbody.innerHTML = `<tr><td colspan="7" class="empty-state"><p>Không thể tải danh sách tài khoản.</p></td></tr>`;
  }
}

let currentUsersPage = 1;
const usersPerPage = 10;
let filteredUsersList = [];

function filterUsers() {
  const search = document.getElementById('userSearch')?.value.toLowerCase() || '';
  const role = document.getElementById('userRoleFilter')?.value || '';
  
  filteredUsersList = allUsersList;
  if (search) {
    filteredUsersList = filteredUsersList.filter(u => u.username.toLowerCase().includes(search) || u.fullName.toLowerCase().includes(search));
  }
  if (role) {
    filteredUsersList = filteredUsersList.filter(u => u.role === role);
  }
  
  currentUsersPage = 1; // Reset to page 1 on filter
  displayUsersTable();
}

window.goToUsersPage = function(page) {
  currentUsersPage = page;
  displayUsersTable();
};

function displayUsersTable() {
  const tbody = document.getElementById('usersTableBody');
  if (!tbody) return;
  
  if (!filteredUsersList || filteredUsersList.length === 0) {
    tbody.innerHTML = `<tr><td colspan="7" class="empty-state"><p>Không tìm thấy tài khoản phù hợp</p></td></tr>`;
    document.getElementById('usersPagination').style.display = 'none';
    return;
  }
  
  document.getElementById('usersPagination').style.display = 'flex';
  
  const totalPages = Math.ceil(filteredUsersList.length / usersPerPage);
  if (currentUsersPage < 1) currentUsersPage = 1;
  if (currentUsersPage > totalPages) currentUsersPage = totalPages;
  
  const startIndex = (currentUsersPage - 1) * usersPerPage;
  const endIndex = Math.min(startIndex + usersPerPage, filteredUsersList.length);
  const pageData = filteredUsersList.slice(startIndex, endIndex);
  
  document.getElementById('usersPageInfo').textContent = `Đang hiển thị ${startIndex + 1} - ${endIndex} / ${filteredUsersList.length} bản ghi`;
  document.getElementById('usersPageButtons').innerHTML = generatePaginationButtons(currentUsersPage, totalPages, 'goToUsersPage');
  
  tbody.innerHTML = pageData.map(u => `
    <tr>
      <td><strong style="color:var(--brand);">${u.username}</strong></td>
      <td>${u.fullName}</td>
      <td>
        ${(() => {
          const roleMap = {
            'Admin':   ['badge-role-admin',   '👑 Admin'],
            'HR':      ['badge-role-hr',       '📋 HR'],
            'Manager': ['badge-role-manager',  '📊 Quản lý'],
            'Viewer':  ['badge-role-viewer',   '👁️ Viewer'],
            'User':    ['badge-role-user',     '👤 Nhân viên'],
          };
          const [cls, lbl] = roleMap[u.role] || ['badge-gray', u.role];
          return `<span class="badge ${cls}">${lbl}</span>`;
        })()}
      </td>
      <td>${u.employeeName ? `<code>${u.employeeCode}</code> - ${u.employeeName}` : '<em style="color:var(--text-muted);">Không có</em>'}</td>
      <td>${u.departmentName ? `<span class="badge badge-gray">${u.departmentName}</span>` : '—'}</td>
      <td><span class="badge ${u.isActive ? 'badge-green' : 'badge-gray'}">${u.isActive ? '✓ Hoạt động' : '✗ Khóa'}</span></td>
      <td>
        <div style="display:flex; gap:6px;">
          <button class="btn-icon" onclick="openEditUserModal(${u.userId})" title="Chỉnh sửa tài khoản">
            🔧
          </button>
          <button class="btn-icon" onclick="openResetUserPasswordModal(${u.userId})" title="Đặt lại mật khẩu">
            🔑
          </button>
          <button class="btn-icon" onclick="deleteUser(${u.userId})" title="Xóa tài khoản" style="color:var(--danger);" ${u.username === (currentUser ? currentUser.username : '') ? 'disabled' : ''}>
            🗑️
          </button>
        </div>
      </td>
    </tr>
  `).join('');
  applyMobileDataLabels('usersTable');
}

async function openCreateUserModal() {
  const overlay = document.getElementById('modalOverlay');
  const title   = document.getElementById('modalTitle');
  const body    = document.getElementById('modalBody');
  overlay.classList.add('open');
  
  title.textContent = 'Thêm Tài khoản Mới';
  
  let empOptions = '<option value="">— Không liên kết —</option>';
  try {
    const emps = await api('/employees');
    const linkedEmpIds = usersData.filter(u => u.employeeId).map(u => u.employeeId);
    const unlinkedEmps = emps.filter(e => !linkedEmpIds.includes(e.employeeId));
    empOptions += unlinkedEmps.map(e => `<option value="${e.employeeId}">${e.fullName} (${e.employeeCode})</option>`).join('');
  } catch(_) {}
  
  body.innerHTML = `
    <div class="form-group">
      <label>Tên đăng nhập *</label>
      <input type="text" id="fUsername" placeholder="Nhập tên đăng nhập..." />
    </div>
    <div class="form-group">
      <label>Họ và Tên *</label>
      <input type="text" id="fFullName" placeholder="Nhập họ và tên..." />
    </div>
    <div class="form-group">
      <label>Mật khẩu *</label>
      <input type="password" id="fPassword" placeholder="Tối thiểu 8 ký tự..." />
    </div>
    <div class="form-grid">
      <div class="form-group">
        <label>Vai trò *</label>
        <select id="fRole">
          <option value="Viewer">Viewer</option>
          <option value="Manager">Manager</option>
          <option value="HR">HR</option>
          <option value="Admin">Admin</option>
          <option value="User">Nhân viên (User)</option>
        </select>
      </div>
      <div class="form-group">
        <label>Nhân viên liên kết</label>
        <select id="fUserEmp">${empOptions}</select>
      </div>
    </div>
    <div id="userModalError" style="color:var(--danger);font-size:13px;font-weight:600;margin-bottom:12px;margin-top:8px;"></div>
    <div class="form-actions" style="margin-top: 24px;">
      <button class="btn-secondary" onclick="closeModal()">Hủy</button>
      <button class="btn-primary" onclick="saveUser()">💾 Lưu tài khoản</button>
    </div>
  `;
  
  setTimeout(() => {
    if (document.getElementById('fUserEmp')) {
      new TomSelect('#fUserEmp', {
        create: false,
        sortField: { field: "text", direction: "asc" }
      });
    }
  }, 10);
}

async function openEditUserModal(userId) {
  const user = allUsersList.find(u => u.userId === userId);
  if (!user) return;

  const overlay = document.getElementById('modalOverlay');
  const title   = document.getElementById('modalTitle');
  const body    = document.getElementById('modalBody');
  overlay.classList.add('open');
  
  title.textContent = 'Chỉnh sửa Tài khoản';
  
  let empOptions = '<option value="">— Không liên kết —</option>';
  try {
    const emps = await api('/employees');
    empOptions += emps.map(e => `<option value="${e.employeeId}" ${e.employeeId === user.employeeId ? 'selected' : ''}>${e.fullName} (${e.employeeCode})</option>`).join('');
  } catch(_) {}

  body.innerHTML = `
    <div class="form-group">
      <label>Tên đăng nhập</label>
      <input type="text" id="fUsername" value="${user.username}" disabled style="background:var(--surface-2); cursor:not-allowed;" />
    </div>
    <div class="form-group">
      <label>Họ và Tên *</label>
      <input type="text" id="fFullName" value="${user.fullName}" />
    </div>
    <div class="form-grid">
      <div class="form-group">
        <label>Vai trò *</label>
        <select id="fRole">
          <option value="Viewer" ${user.role === 'Viewer' ? 'selected' : ''}>Viewer</option>
          <option value="Manager" ${user.role === 'Manager' ? 'selected' : ''}>Manager</option>
          <option value="HR" ${user.role === 'HR' ? 'selected' : ''}>HR</option>
          <option value="Admin" ${user.role === 'Admin' ? 'selected' : ''}>Admin</option>
          <option value="User" ${user.role === 'User' ? 'selected' : ''}>Nhân viên (User)</option>
        </select>
      </div>
      <div class="form-group">
        <label>Nhân viên liên kết</label>
        <select id="fUserEmp">${empOptions}</select>
      </div>
    </div>
    <div class="form-group" style="margin-top: 12px;">
      <label style="display:flex; align-items:center; gap:8px; cursor:pointer;">
        <input type="checkbox" id="fIsActive" ${user.isActive ? 'checked' : ''} style="width:auto;" />
        <span>Tài khoản đang hoạt động (Active)</span>
      </label>
    </div>
    <div id="userModalError" style="color:var(--danger);font-size:13px;font-weight:600;margin-bottom:12px;margin-top:8px;"></div>
    <div class="form-actions" style="margin-top: 24px;">
      <button class="btn-secondary" onclick="closeModal()">Hủy</button>
      <button class="btn-primary" onclick="updateUser(${userId})">💾 Lưu thay đổi</button>
    </div>
  `;
  
  setTimeout(() => {
    if (document.getElementById('fUserEmp')) {
      new TomSelect('#fUserEmp', {
        create: false,
        sortField: { field: "text", direction: "asc" }
      });
    }
  }, 10);
}

function openResetUserPasswordModal(userId) {
  const user = allUsersList.find(u => u.userId === userId);
  if (!user) return;

  const overlay = document.getElementById('modalOverlay');
  const title   = document.getElementById('modalTitle');
  const body    = document.getElementById('modalBody');
  overlay.classList.add('open');
  
  title.textContent = `Đặt lại mật khẩu cho: ${user.username}`;
  
  body.innerHTML = `
    <div class="form-group">
      <label>Mật khẩu mới *</label>
      <input type="password" id="fNewPassword" placeholder="Nhập mật khẩu mới (tối thiểu 8 ký tự)..." />
    </div>
    <div class="form-group">
      <label>Xác nhận mật khẩu *</label>
      <input type="password" id="fConfirmPassword" placeholder="Xác nhận mật khẩu mới..." />
    </div>
    <div id="userModalError" style="color:var(--danger);font-size:13px;font-weight:600;margin-bottom:12px;margin-top:8px;"></div>
    <div class="form-actions" style="margin-top: 24px;">
      <button class="btn-secondary" onclick="closeModal()">Hủy</button>
      <button class="btn-primary" onclick="submitResetUserPassword(${userId})">💾 Lưu mật khẩu</button>
    </div>
  `;
}

async function saveUser() {
  const username = document.getElementById('fUsername').value.trim();
  const fullName = document.getElementById('fFullName').value.trim();
  const password = document.getElementById('fPassword').value;
  const role = document.getElementById('fRole').value;
  const empIdVal = document.getElementById('fUserEmp').value;
  const employeeId = empIdVal ? parseInt(empIdVal) : null;
  const errDiv = document.getElementById('userModalError');
  
  if (!username || !fullName || !password) {
    errDiv.textContent = 'Vui lòng nhập đầy đủ các trường bắt buộc!';
    return;
  }
  
  if (password.length < 8) {
    errDiv.textContent = 'Mật khẩu phải có tối thiểu 8 ký tự!';
    return;
  }
  
  errDiv.textContent = '';
  
  try {
    await api('/users', {
      method: 'POST',
      body: JSON.stringify({ username, fullName, password, role, employeeId })
    });
    closeModal();
    showToast(`✅ Tạo tài khoản ${username} thành công!`);
    await renderUsers();
  } catch(err) {
    errDiv.textContent = err.message;
  }
}

async function updateUser(userId) {
  const fullName = document.getElementById('fFullName').value.trim();
  const role = document.getElementById('fRole').value;
  const empIdVal = document.getElementById('fUserEmp').value;
  const employeeId = empIdVal ? parseInt(empIdVal) : null;
  const isActive = document.getElementById('fIsActive').checked;
  const errDiv = document.getElementById('userModalError');
  
  if (!fullName) {
    errDiv.textContent = 'Vui lòng nhập đầy đủ họ và tên!';
    return;
  }
  
  errDiv.textContent = '';
  
  try {
    await api(`/users/${userId}`, {
      method: 'PUT',
      body: JSON.stringify({ fullName, role, employeeId, isActive })
    });
    closeModal();
    showToast(`✅ Cập nhật tài khoản thành công!`);
    await renderUsers();
  } catch(err) {
    errDiv.textContent = err.message;
  }
}

async function submitResetUserPassword(userId) {
  const newPw = document.getElementById('fNewPassword').value;
  const confirmPw = document.getElementById('fConfirmPassword').value;
  const errDiv = document.getElementById('userModalError');
  
  if (!newPw || !confirmPw) {
    errDiv.textContent = 'Vui lòng điền đầy đủ thông tin!';
    return;
  }
  
  if (newPw.length < 8) {
    errDiv.textContent = 'Mật khẩu mới phải có tối thiểu 8 ký tự!';
    return;
  }
  
  if (newPw !== confirmPw) {
    errDiv.textContent = 'Mật khẩu xác nhận không khớp!';
    return;
  }
  
  errDiv.textContent = '';
  
  try {
    await api(`/users/${userId}/reset-password`, {
      method: 'PUT',
      body: JSON.stringify({ newPassword: newPw })
    });
    closeModal();
    showToast(`🔑 Đặt lại mật khẩu thành công!`);
  } catch(err) {
    errDiv.textContent = err.message;
  }
}

async function deleteUser(userId) {
  const user = allUsersList.find(u => u.userId === userId);
  if (!user) return;
  
  if (!confirm(`Bạn có chắc chắn muốn xóa tài khoản "${user.username}"?`)) {
    return;
  }
  
  try {
    await api(`/users/${userId}`, {
      method: 'DELETE'
    });
    showToast(`🗑️ Đã xóa tài khoản ${user.username}!`);
    await renderUsers();
  } catch(err) {
    // API helper handles error toast
  }
}

// ─────────────────────────────────────────────────────────────
//  USER SPECIFIC FUNCTIONS
// ─────────────────────────────────────────────────────────────
async function autoGenerateAccounts() {
  if (!confirm('Hệ thống sẽ tự động tạo tài khoản (mật khẩu mặc định: 123456@Aa) cho TẤT CẢ nhân viên chưa có tài khoản. Bạn có chắc chắn?')) return;
  
  try {
    const res = await api('/users/auto-generate', { method: 'POST' });
    showToast(res.message || 'Tạo tài khoản loạt thành công!', 'success');
    if (document.getElementById('page-users').classList.contains('active')) {
      await renderUsers();
    }
  } catch (err) {
    showToast('Lỗi: ' + err.message, 'error');
  }
}

let myCertsData = [];

async function loadMyDashboard() {
  if (!currentUser || !currentUser.employeeId) return;
  
  try {
    const employee = await api(`/employees/${currentUser.employeeId}`);
    if (!employee) return;
    
    myCertsData = employee.trainings || [];
    
    // Calculate total hours
    const totalHours = myCertsData.reduce((sum, t) => sum + (t.trainingHours || 0), 0);
    const requiredHours = 48; // For now, hardcode 48 for 2 years
    
    document.getElementById('myTotalHours').textContent = `${totalHours} / ${requiredHours}`;
    document.getElementById('myCompletedTrainings').textContent = myCertsData.length;
    
    // Expiring certs
    let expiringCertsCount = 0;
    const now = new Date();
    const warns = [];
    
    myCertsData.forEach(t => {
      if (t.expiryDate) {
        const expDate = new Date(t.expiryDate);
        const diffTime = expDate - now;
        const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24));
        
        if (diffDays <= 60 && diffDays > 0) {
          expiringCertsCount++;
          warns.push(`⚠️ Chứng chỉ <b>${t.courseName}</b> sắp hết hạn sau ${diffDays} ngày.`);
        } else if (diffDays <= 0) {
          expiringCertsCount++;
          warns.push(`🚨 Chứng chỉ <b>${t.courseName}</b> đã hết hạn!`);
        }
      }
    });
    
    document.getElementById('myExpiringCerts').textContent = expiringCertsCount;
    
    const alertsContainer = document.getElementById('myAlertsContainer');
    if (warns.length > 0) {
      alertsContainer.innerHTML = warns.map(w => `<div style="padding: 12px; background: var(--warning-light); border-left: 4px solid var(--warning); border-radius: 4px;">${w}</div>`).join('');
    } else {
      alertsContainer.innerHTML = '<div class="empty-state" style="padding: 20px;">Không có cảnh báo nào</div>';
    }
    
    // Render My Profile Info
    document.getElementById('myProfileInfo').innerHTML = `
      <p style="margin-bottom: 8px;"><strong>Họ và tên:</strong> ${employee.fullName}</p>
      <p style="margin-bottom: 8px;"><strong>Mã NV:</strong> ${employee.employeeCode}</p>
      <p style="margin-bottom: 8px;"><strong>Khoa/Phòng:</strong> ${employee.departmentName || '---'}</p>
      <p style="margin-bottom: 8px;"><strong>Chức danh:</strong> ${employee.position || '---'}</p>
    `;
    
    renderMyCertsTable(myCertsData);
    
  } catch (err) {
    showToast('Lỗi khi tải thông tin cá nhân', 'error');
  }
}

function renderMyCertsTable(data) {
  const tbody = document.getElementById('myCertTableBody');
  if (!data || data.length === 0) {
    tbody.innerHTML = '<tr><td colspan="7" class="empty-state">Chưa có chứng chỉ nào</td></tr>';
    return;
  }
  
  tbody.innerHTML = data.map(t => {
    let statusBadge = '<span class="status-badge status-good">Còn hạn</span>';
    if (t.isLifetime) {
      statusBadge = '<span class="status-badge status-good">Vĩnh viễn</span>';
    } else if (t.expiryDate) {
      const expDate = new Date(t.expiryDate);
      const now = new Date();
      const diffTime = expDate - now;
      const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24));
      
      if (diffDays <= 0) statusBadge = '<span class="status-badge status-danger">Hết hạn</span>';
      else if (diffDays <= 60) statusBadge = `<span class="status-badge status-warning">Sắp hết hạn (${diffDays} ngày)</span>`;
    }
    
    const fileLink = t.certificateUrl 
      ? `<a href="${t.certificateUrl}" target="_blank" style="color:var(--primary)">📄 Tải về</a>`
      : '---';
      
    return `
      <tr>
        <td style="font-weight: 500;">${t.courseName}</td>
        <td>${t.certificateType || '---'}</td>
        <td style="text-align:center">${t.trainingHours || 0}</td>
        <td>${t.issueDate ? new Date(t.issueDate).toLocaleDateString('vi-VN') : '---'}</td>
        <td>${t.expiryDate ? new Date(t.expiryDate).toLocaleDateString('vi-VN') : '---'}</td>
        <td>${statusBadge}</td>
        <td style="text-align:center">${fileLink}</td>
      </tr>
    `;
  }).join('');
  applyMobileDataLabels('myCertTableBody');
}

function filterMyCerts() {
  const term = document.getElementById('myCertSearch').value.toLowerCase();
  const filtered = myCertsData.filter(t => t.courseName.toLowerCase().includes(term));
  renderMyCertsTable(filtered);
}

// ─────────────────────────────────────────────────────────────
//  INIT
// ─────────────────────────────────────────────────────────────
async function runInitApis() {
  try {
    const s = await api('/settings');
    settings = {
      urgentWarningDays:  s.urgentWarningDays  ?? 30,
      expiryWarningDays:  s.expiryWarningDays  ?? 60,
      requiredHours1Year: s.requiredHours1Year ?? 24,
      requiredHours2Years:s.requiredHours2Years ?? 48,
      requiredHours5Years:s.requiredHours5Years ?? 120,
    };
  } catch (_) {}
  await updateAlertBadge();
  showPage('dashboard');
}

async function init() {
  setCurrentDate();
  initAuth();
  setInterval(setCurrentDate, 60000);
}

document.addEventListener('DOMContentLoaded', init);
