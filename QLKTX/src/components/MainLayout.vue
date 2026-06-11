<template>
  <div class="app-shell">
    <aside class="sidebar">
      <div class="brand">
        <div class="brand-mark">
          <span class="mdi mdi-home-city-outline"></span>
        </div>
        <div>
          <div class="brand-name">DormManager</div>
          <span class="brand-subtitle">Residence Portal</span>
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
      </div>
    </aside>

    <main class="main-panel">
      <header class="topbar">
        <div>
          <span class="service-label">{{ serviceLabel }}</span>
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
          <v-btn icon="mdi-logout" variant="tonal" color="error" @click="logout" />
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
  grid-template-columns: 300px minmax(0, 1fr);
  min-height: 100vh;
  background: var(--app-bg);
  color: var(--ink);
}

.sidebar {
  position: sticky;
  top: 0;
  display: flex;
  flex-direction: column;
  height: 100vh;
  padding: 28px 20px 20px;
  background: var(--sidebar);
  color: #f5f7f8;
}

.brand {
  display: flex;
  align-items: center;
  gap: 12px;
  min-height: 62px;
  margin-bottom: 24px;
}

.brand-mark {
  display: grid;
  place-items: center;
  width: 42px;
  height: 42px;
  color: var(--brand);
  font-size: 34px;
}

.brand-name {
  color: var(--brand);
  font-size: 22px;
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

.nav {
  flex: 1;
  min-height: 0;
  overflow-y: auto;
  padding-right: 2px;
}

.nav-section {
  margin: 18px 10px 12px;
  color: #7b8288;
  font-size: 12px;
  font-weight: 900;
  letter-spacing: 0.08em;
  text-transform: uppercase;
}

.nav-item {
  display: grid;
  grid-template-columns: 26px minmax(0, 1fr);
  gap: 12px;
  align-items: center;
  min-height: 46px;
  width: 100%;
  margin: 6px 0;
  padding: 10px 13px;
  border: 1px solid transparent;
  border-radius: 8px;
  background: transparent;
  color: #f2f5f6;
  cursor: pointer;
  font-size: 15px;
  font-weight: 800;
  line-height: 1.25;
  text-align: left;
  text-decoration: none;
}

.nav-item.rubric-item {
  grid-template-columns: 24px 24px minmax(0, 1fr);
}

.nav-item b {
  display: grid;
  place-items: center;
  width: 24px;
  height: 24px;
  border-radius: 6px;
  background: #24282c;
  color: #c8f4df;
  font-size: 12px;
  font-weight: 900;
}

.nav-item .mdi {
  color: #b8bdc2;
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
  border-color: #2c3033;
  background: var(--sidebar-soft);
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
  margin: 10px 0 0;
  padding: 15px;
  border: 1px solid rgba(22, 155, 99, 0.18);
  border-radius: 8px;
  background: linear-gradient(135deg, rgba(22, 155, 99, 0.18), rgba(255, 255, 255, 0.04));
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
  grid-template-columns: 42px minmax(0, 1fr);
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
  font-weight: 800;
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
  background: var(--app-bg);
}

.topbar {
  position: sticky;
  top: 0;
  z-index: 5;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 24px;
  min-height: 88px;
  padding: 18px 30px;
  border-bottom: 1px solid var(--line);
  background: rgba(255, 255, 255, 0.96);
  backdrop-filter: blur(12px);
}

.service-label {
  display: block;
  margin-bottom: 4px;
  color: var(--brand);
  font-size: 12px;
  font-weight: 900;
  letter-spacing: 0.08em;
  text-transform: uppercase;
}

.topbar h1 {
  margin: 0;
  color: var(--brand-dark);
  font-size: 20px;
  font-weight: 900;
  line-height: 1.25;
}

.topbar p {
  margin: 6px 0 0;
  color: #111827;
  font-size: 15px;
  font-weight: 700;
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
  min-height: 48px;
  padding: 8px 12px;
  border: 1px solid var(--line);
  border-radius: 8px;
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
}

.user-chip strong,
.user-chip span {
  display: block;
  text-align: right;
}

.user-chip strong {
  color: var(--brand-dark);
  font-size: 15px;
}

.user-chip span {
  margin-top: 2px;
  color: #111827;
  font-size: 13px;
}

.page-body {
  padding: 28px 30px 48px;
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

  .nav {
    display: flex;
    gap: 8px;
    overflow-x: auto;
    padding-bottom: 4px;
  }

  .nav-section,
  .sidebar-note,
  .account-box {
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
