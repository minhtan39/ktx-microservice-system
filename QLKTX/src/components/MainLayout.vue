<template>
  <div class="app-shell">
    <aside class="sidebar">
      <div class="sidebar-top">
        <router-link to="/" class="brand">
          <div class="brand-mark">
            <span class="mdi mdi-home-city-outline"></span>
          </div>
          <div>
            <div class="brand-name">DormManager</div>
            <span class="brand-subtitle">Smart Residence</span>
          </div>
        </router-link>

        <div class="system-pill">
          <span class="live-dot"></span>
          <span>{{ roleKey === 'Student' ? 'Không gian sinh viên' : 'Bảng điều hành' }}</span>
        </div>
      </div>

      <nav class="nav">
        <p v-if="overviewItems.length" class="nav-section">{{ overviewSectionTitle }}</p>

        <router-link
          v-for="item in overviewItems"
          :key="item.label"
          :to="item.to"
          class="nav-item"
          :class="{ active: item.names.includes(route.name) }"
        >
          <span :class="['mdi', item.icon]"></span>
          <span>{{ item.label }}</span>
        </router-link>

        <p v-if="rubricItems.length" class="nav-section">Nội trú sinh viên</p>

        <router-link
          v-for="item in rubricItems"
          :key="item.label"
          :to="item.to"
          class="nav-item rubric-item"
          :class="{ active: item.names.includes(route.name) }"
        >
          <b>{{ item.step }}</b>
          <span :class="['mdi', item.icon]"></span>
          <span>{{ item.label }}</span>
        </router-link>

        <p v-if="serviceItems.length" class="nav-section">Vận hành liên thông</p>

        <router-link
          v-for="item in serviceItems"
          :key="item.label"
          :to="item.to"
          class="nav-item"
          :class="{ active: item.names.includes(route.name) }"
        >
          <span :class="['mdi', item.icon]"></span>
          <span>{{ item.label }}</span>
        </router-link>
      </nav>

      <div v-if="!isStudent" class="sidebar-note">
        <span class="mdi mdi-clipboard-check-outline"></span>
        <div>
          <strong>Ưu tiên hôm nay</strong>
          <p>Duyệt đơn chờ, xếp phòng còn giường và theo dõi hợp đồng mới.</p>
        </div>
      </div>

      <div class="account-box">
        <div class="account-avatar">{{ userInitial }}</div>
        <div class="account-meta">
          <strong>{{ fullName }}</strong>
          <span>{{ userRole }}</span>
        </div>
        <v-btn icon="mdi-logout" variant="text" color="error" density="comfortable" @click="logout" />
      </div>
    </aside>

    <main class="main-panel">
      <header class="topbar">
        <div class="topbar-title">
          <div class="breadcrumb-line">
            <span>{{ serviceLabel }}</span>
            <i></i>
            <strong>{{ routeBadge }}</strong>
          </div>
          <h1>{{ appTitle }}</h1>
          <p>{{ pageTitle }}</p>
        </div>

        <div class="topbar-actions">
          <div class="term-chip">
            <span class="mdi mdi-calendar-check-outline"></span>
            <div>
              <strong>Kỳ nội trú</strong>
              <small>2026 - 2027</small>
            </div>
          </div>
          <div class="user-chip">
            <div>
              <strong>{{ fullName }}</strong>
              <span>{{ userRole }}</span>
            </div>
            <div class="user-avatar">{{ userInitial }}</div>
          </div>
        </div>
      </header>

      <div class="page-body">
        <router-view />
      </div>
    </main>
  </div>
</template>

<script setup>
import { computed, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'

const route = useRoute()
const router = useRouter()
const userRole = ref(localStorage.getItem('user_role') || 'N2 Admin')
const fullName = ref(localStorage.getItem('fullName') || 'demo_admin')

const normalizeRole = (role) => {
  const normalized = String(role || '').toLowerCase()

  if (normalized === 'student' || normalized === 'sinhvien') return 'Student'
  if (normalized === 'staff' || normalized === 'nhanvien') return 'Staff'
  return 'Admin'
}

const roleKey = computed(() => normalizeRole(userRole.value))
const isStudent = computed(() => roleKey.value === 'Student')

const titleByRoute = {
  StudentPortal: 'Theo dõi hồ sơ, đăng ký nội trú và hợp đồng của bạn',
  StudentServiceDashboard: 'Nắm nhanh tình hình phòng ở, đơn đăng ký và hợp đồng',
  StudentManage: 'Quản lý hồ sơ sinh viên, lớp, khoa và lịch sử lưu trú',
  RoomRegistrationCreate: 'Tiếp nhận đăng ký nội trú trực tuyến',
  RoomRegistrationApproval: 'Chọn tòa, chọn phòng và duyệt đơn nội trú',
  ContractList: 'Tra cứu hợp đồng đã tạo từ đơn được duyệt',
  ContractManage: 'Theo dõi hiệu lực và xử lý trạng thái hợp đồng',
  RoomDashboard: 'Sơ đồ phòng, số giường và trạng thái vận hành',
  IncidentManage: 'Tiếp nhận sự cố phòng ở và theo dõi xử lý',
  AccountManage: 'Quản lý tài khoản đăng nhập của nhân viên và sinh viên',
  SystemLogs: 'Nhật ký hệ thống và kiểm tra vận hành',
}

const studentOverviewItems = [
  {
    to: '/student/portal',
    names: ['StudentPortal'],
    icon: 'mdi-account-school-outline',
    label: 'Cổng sinh viên',
  },
]

const adminOverviewItems = [
  {
    to: '/student-service/dashboard',
    names: ['StudentServiceDashboard'],
    icon: 'mdi-view-dashboard-outline',
    label: 'Bảng tin kết nối',
  },
]

const adminRubricItems = [
  {
    step: '5',
    to: '/student-service/students',
    names: ['StudentManage'],
    icon: 'mdi-account-school-outline',
    label: 'Hồ sơ sinh viên',
  },
  {
    step: '6',
    to: '/student-service/registrations',
    names: ['RoomRegistrationCreate'],
    icon: 'mdi-form-select',
    label: 'Đăng ký nội trú',
  },
  {
    step: '7',
    to: '/student-service/registrations/approval',
    names: ['RoomRegistrationApproval'],
    icon: 'mdi-clipboard-check-outline',
    label: 'Duyệt xếp phòng',
  },
  {
    step: '8',
    to: '/student-service/contracts',
    names: ['ContractList'],
    icon: 'mdi-file-document-outline',
    label: 'Hợp đồng thuê phòng',
  },
  {
    step: '+',
    to: '/student-service/contracts/manage',
    names: ['ContractManage'],
    icon: 'mdi-file-cog-outline',
    label: 'Vận hành hợp đồng',
  },
]

const adminServiceItems = [
  {
    to: '/facility/rooms',
    names: ['RoomDashboard'],
    icon: 'mdi-door-open',
    label: 'Room & Building',
  },
  {
    to: '/finance/incidents',
    names: ['IncidentManage'],
    icon: 'mdi-tools',
    label: 'Billing & Maintenance',
  },
  {
    to: '/auth/accounts',
    names: ['AccountManage'],
    icon: 'mdi-account-key-outline',
    label: 'Quản lý tài khoản',
    adminOnly: true,
  },
  {
    to: '/system/logs',
    names: ['SystemLogs'],
    icon: 'mdi-text-box-search-outline',
    label: 'Nhật ký hệ thống',
  },
]

const overviewItems = computed(() =>
  isStudent.value ? studentOverviewItems : adminOverviewItems)

const rubricItems = computed(() =>
  isStudent.value ? [] : adminRubricItems)

const serviceItems = computed(() =>
  isStudent.value
    ? []
    : adminServiceItems.filter((item) => !item.adminOnly || roleKey.value === 'Admin'))

const overviewSectionTitle = computed(() =>
  isStudent.value ? 'Tài khoản sinh viên' : 'Tổng quan hệ thống')

const serviceLabel = computed(() =>
  isStudent.value ? 'Student Portal' : 'Residence Operations')

const appTitle = computed(() =>
  isStudent.value
    ? 'Cổng sinh viên ký túc xá'
    : 'Quản lý ký túc xá')

const pageTitle = computed(() =>
  titleByRoute[route.name] || 'Nghiệp vụ Contract & Student Service')

const routeBadge = computed(() => {
  if (isStudent.value) return 'Sinh viên'
  if (['RoomDashboard'].includes(route.name)) return 'Nhóm 1'
  if (['IncidentManage', 'SystemLogs', 'AccountManage'].includes(route.name)) return 'Nhóm 3'
  return 'Nhóm 2'
})

const userInitial = computed(() => {
  return (fullName.value || 'U').trim().charAt(0).toUpperCase()
})

const logout = async () => {
  localStorage.removeItem('user_token')
  localStorage.removeItem('user_role')
  localStorage.removeItem('fullName')
  localStorage.removeItem('username')
  localStorage.removeItem('user_home')
  localStorage.removeItem('student_id')
  localStorage.removeItem('student_code')
  await router.push('/login')
}
</script>

<style scoped>
.app-shell {
  display: grid;
  grid-template-columns: 292px minmax(0, 1fr);
  min-height: 100vh;
  background:
    radial-gradient(circle at top left, rgba(20, 157, 113, 0.08), transparent 32%),
    linear-gradient(180deg, #f8faf7 0%, #f3f6f4 100%);
  color: var(--ink);
}

.sidebar {
  position: sticky;
  top: 0;
  display: flex;
  flex-direction: column;
  height: 100vh;
  padding: 22px 18px 18px;
  background:
    linear-gradient(180deg, rgba(20, 157, 113, 0.12), transparent 34%),
    var(--sidebar);
  color: #f5f7f8;
  box-shadow: 12px 0 34px rgba(15, 23, 42, 0.12);
}

.sidebar-top {
  display: grid;
  gap: 14px;
  margin-bottom: 18px;
}

.brand {
  display: flex;
  align-items: center;
  gap: 12px;
  min-height: 54px;
  color: inherit;
  text-decoration: none;
}

.brand-mark {
  display: grid;
  place-items: center;
  width: 44px;
  height: 44px;
  border: 1px solid rgba(120, 242, 189, 0.24);
  border-radius: 14px;
  background: rgba(20, 157, 113, 0.12);
  color: var(--brand);
  font-size: 30px;
}

.brand-name {
  color: #ffffff;
  font-family: var(--font-heading);
  font-size: 21px;
  font-weight: 900;
  line-height: 1;
}

.brand-subtitle {
  display: block;
  margin-top: 5px;
  color: #9aa1a8;
  font-size: 12px;
  font-weight: 800;
  letter-spacing: 0.08em;
  text-transform: uppercase;
}

.system-pill {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  width: fit-content;
  min-height: 30px;
  padding: 0 11px;
  border: 1px solid rgba(255, 255, 255, 0.08);
  border-radius: 999px;
  background: rgba(255, 255, 255, 0.06);
  color: #cbd5d1;
  font-size: 12px;
  font-weight: 800;
}

.live-dot {
  width: 8px;
  height: 8px;
  border-radius: 999px;
  background: #35bb84;
  box-shadow: 0 0 0 4px rgba(53, 187, 132, 0.16);
}

.nav {
  flex: 1;
  min-height: 0;
  overflow-y: auto;
  padding: 4px 2px 8px 0;
}

.nav-section {
  margin: 18px 10px 10px;
  color: #7f8b85;
  font-size: 11px;
  font-weight: 900;
  letter-spacing: 0;
  text-transform: uppercase;
}

.nav-item {
  display: grid;
  grid-template-columns: 26px minmax(0, 1fr);
  gap: 12px;
  align-items: center;
  min-height: 48px;
  width: 100%;
  margin: 5px 0;
  padding: 11px 12px;
  border: 1px solid transparent;
  border-radius: 12px;
  background: transparent;
  color: #dfe6e2;
  cursor: pointer;
  font-size: 14px;
  font-weight: 800;
  line-height: 1.25;
  text-align: left;
  text-decoration: none;
  transition: background-color 0.18s ease, border-color 0.18s ease, color 0.18s ease;
}

.nav-item.rubric-item {
  grid-template-columns: 24px 24px minmax(0, 1fr);
}

.nav-item b {
  display: grid;
  place-items: center;
  width: 24px;
  height: 24px;
  border-radius: 999px;
  background: #202624;
  color: #c8f4df;
  font-size: 12px;
  font-weight: 900;
}

.nav-item .mdi {
  color: #a9b5af;
  font-size: 21px;
}

.nav-item span:last-child {
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.nav-item:hover,
.nav-item.active {
  border-color: rgba(120, 242, 189, 0.22);
  background: rgba(255, 255, 255, 0.09);
}

.nav-item.active .mdi,
.nav-item.active span:last-child {
  color: #ffffff;
}

.nav-item.active b {
  background: var(--brand);
  color: #052e1c;
}

.sidebar-note {
  display: grid;
  grid-template-columns: 32px minmax(0, 1fr);
  gap: 10px;
  margin: 12px 0 0;
  padding: 14px;
  border: 1px solid rgba(120, 242, 189, 0.18);
  border-radius: 16px;
  background: linear-gradient(135deg, rgba(22, 155, 99, 0.18), rgba(255, 255, 255, 0.05));
}

.sidebar-note .mdi {
  color: var(--brand);
  font-size: 24px;
}

.sidebar-note strong {
  display: block;
  color: #ffffff;
  font-size: 13px;
}

.sidebar-note p {
  margin: 4px 0 0;
  color: #c7d5cf;
  font-size: 12px;
  line-height: 1.45;
}

.account-box {
  display: grid;
  grid-template-columns: 42px minmax(0, 1fr) 38px;
  gap: 12px;
  align-items: center;
  margin-top: 16px;
  padding-top: 18px;
  border-top: 1px solid #24282c;
}

.account-avatar,
.user-avatar {
  display: grid;
  place-items: center;
  width: 42px;
  height: 42px;
  border-radius: 50%;
  background: #f4f5f6;
  color: #111827;
  font-weight: 900;
}

.account-meta {
  min-width: 0;
}

.account-meta strong,
.account-meta span {
  display: block;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.account-meta strong {
  color: #ffffff;
  font-size: 14px;
}

.account-meta span {
  margin-top: 2px;
  color: #8e949a;
  font-size: 13px;
}

.main-panel {
  min-width: 0;
  min-height: 100vh;
  background: transparent;
}

.topbar {
  position: sticky;
  top: 0;
  z-index: 5;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 24px;
  min-height: 92px;
  padding: 18px 34px;
  border-bottom: 1px solid rgba(221, 232, 223, 0.78);
  background: rgba(248, 250, 247, 0.88);
  backdrop-filter: blur(18px);
}

.topbar-title {
  min-width: 0;
}

.breadcrumb-line {
  display: flex;
  align-items: center;
  gap: 9px;
  margin-bottom: 6px;
  color: var(--foreground-500);
  font-size: 12px;
  font-weight: 900;
  text-transform: uppercase;
}

.breadcrumb-line i {
  width: 5px;
  height: 5px;
  border-radius: 999px;
  background: var(--primary-400);
}

.breadcrumb-line strong {
  color: var(--primary-700);
}

.topbar h1 {
  margin: 0;
  color: var(--foreground-950);
  font-family: var(--font-heading);
  font-size: 24px;
  font-weight: 900;
  line-height: 1.25;
}

.topbar p {
  margin: 6px 0 0;
  max-width: 760px;
  color: var(--foreground-600);
  font-size: 14px;
  font-weight: 700;
  line-height: 1.45;
}

.topbar-actions {
  display: flex;
  align-items: center;
  gap: 14px;
  flex: 0 0 auto;
}

.term-chip {
  display: grid;
  grid-template-columns: 34px minmax(0, 1fr);
  gap: 10px;
  align-items: center;
  min-width: 154px;
  min-height: 50px;
  padding: 9px 12px;
  border: 1px solid var(--background-200);
  border-radius: 14px;
  background: #ffffff;
}

.term-chip .mdi {
  color: var(--brand-dark);
  font-size: 24px;
}

.term-chip strong,
.term-chip small {
  display: block;
}

.term-chip strong {
  color: var(--ink);
  font-size: 13px;
}

.term-chip small {
  margin-top: 2px;
  color: var(--muted);
  font-size: 12px;
}

.user-chip {
  display: flex;
  align-items: center;
  gap: 12px;
  min-height: 50px;
  padding: 5px 5px 5px 12px;
  border: 1px solid var(--background-200);
  border-radius: 999px;
  background: #ffffff;
}

.user-chip strong,
.user-chip span {
  display: block;
  text-align: right;
}

.user-chip strong {
  color: var(--foreground-950);
  font-size: 15px;
}

.user-chip span {
  margin-top: 2px;
  color: var(--foreground-500);
  font-size: 13px;
}

.page-body {
  width: min(100%, 1500px);
  margin: 0 auto;
  padding: 30px 34px 54px;
}

@media (max-width: 1100px) {
  .app-shell {
    grid-template-columns: 270px minmax(0, 1fr);
  }

  .sidebar {
    padding: 24px 16px;
  }
}

@media (max-width: 860px) {
  .app-shell {
    grid-template-columns: 1fr;
  }

  .sidebar {
    position: static;
    height: auto;
  }

  .brand {
    margin-bottom: 12px;
  }

  .sidebar-top {
    margin-bottom: 12px;
  }

  .nav {
    display: flex;
    gap: 8px;
    overflow-x: auto;
    padding-bottom: 4px;
  }

  .nav-section,
  .sidebar-note,
  .account-box,
  .system-pill {
    display: none;
  }

  .nav-item {
    flex: 0 0 188px;
    margin: 0;
  }

  .topbar {
    position: static;
    align-items: flex-start;
    flex-direction: column;
    padding: 18px;
  }

  .topbar-actions {
    align-items: stretch;
    flex-direction: column;
    width: 100%;
  }

  .user-chip {
    justify-content: flex-start;
  }

  .user-chip strong,
  .user-chip span {
    text-align: left;
  }

  .page-body {
    padding: 18px;
  }
}
</style>
