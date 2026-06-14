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
          <span>{{ workspaceLabel }}</span>
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
          <p>{{ isStaff ? 'Xử lý công việc được giao và lịch bảo trì sắp đến hạn.' : 'Duyệt đơn chờ, phân công sửa chữa và theo dõi vận hành.' }}</p>
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
import { getPermissions, normalizeRole } from '@/utils/auth'

const route = useRoute()
const router = useRouter()
const userRole = ref(localStorage.getItem('user_role') || 'N2 Admin')
const fullName = ref(localStorage.getItem('fullName') || 'demo_admin')

const roleKey = computed(() => normalizeRole(userRole.value))
const isStudent = computed(() => roleKey.value === 'Student')
const isStaff = computed(() => roleKey.value === 'Staff')
const permissions = ref(getPermissions())
const can = (permission) => roleKey.value === 'Admin' || permissions.value.includes(permission)
const canAccessItem = (item) =>
  (!item.adminOnly || roleKey.value === 'Admin') &&
  (!item.permission || can(item.permission)) &&
  (!item.permissionsAny || item.permissionsAny.some((permission) => can(permission)))

const titleByRoute = {
  StudentPortal: 'Theo dõi hồ sơ, đăng ký nội trú và hợp đồng của bạn',
  ChangePassword: 'Xác nhận mật khẩu hiện tại và bảo vệ tài khoản của bạn',
  StudentPayments: 'Theo dõi hóa đơn, quét QR và lịch sử thanh toán nội trú',
  EmployeeDashboard: 'Theo dõi công việc được giao, yêu cầu quá hạn và lịch bảo trì',
  StudentServiceDashboard: 'Nắm nhanh tình hình phòng ở, đơn đăng ký và hợp đồng',
  StudentManage: 'Quản lý hồ sơ sinh viên, lớp, khoa và lịch sử lưu trú',
  RoomRegistrationCreate: 'Tiếp nhận đăng ký nội trú trực tuyến',
  RoomRegistrationApproval: 'Chọn tòa, chọn phòng và duyệt đơn nội trú',
  ContractList: 'Tra cứu hợp đồng đã tạo từ đơn được duyệt',
  ContractManage: 'Theo dõi hiệu lực và xử lý trạng thái hợp đồng',
  RoomDashboard: 'Sơ đồ phòng, số giường và trạng thái vận hành',
  IncidentManage: 'Tiếp nhận sự cố phòng ở và theo dõi xử lý',
  BillingManagement: 'Nhập chỉ số điện nước, phát hành phiếu và đối soát thanh toán',
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
  {
    to: '/student/change-password',
    names: ['ChangePassword'],
    icon: 'mdi-lock-reset',
    label: 'Đổi mật khẩu',
  },
  {
    to: '/student/payments',
    names: ['StudentPayments'],
    icon: 'mdi-credit-card-check-outline',
    label: 'Thanh toán',
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

const staffOverviewItems = [
  {
    to: '/employee/dashboard',
    names: ['EmployeeDashboard'],
    icon: 'mdi-clipboard-account-outline',
    label: 'Công việc của tôi',
  },
]

const adminRubricItems = [
  {
    step: '5',
    to: '/student-service/students',
    names: ['StudentManage'],
    icon: 'mdi-account-school-outline',
    label: 'Hồ sơ sinh viên',
    permission: 'view_students',
  },
  {
    step: '6',
    to: '/student-service/registrations',
    names: ['RoomRegistrationCreate'],
    icon: 'mdi-form-select',
    label: 'Đăng ký nội trú',
    adminOnly: true,
  },
  {
    step: '7',
    to: '/student-service/registrations/approval',
    names: ['RoomRegistrationApproval'],
    icon: 'mdi-clipboard-check-outline',
    label: 'Duyệt xếp phòng',
    permission: 'approve_registrations',
  },
  {
    step: '8',
    to: '/student-service/contracts',
    names: ['ContractList'],
    icon: 'mdi-file-document-outline',
    label: 'Hợp đồng thuê phòng',
    permission: 'manage_contracts',
  },
  {
    step: '+',
    to: '/student-service/contracts/manage',
    names: ['ContractManage'],
    icon: 'mdi-file-cog-outline',
    label: 'Vận hành hợp đồng',
    permission: 'manage_contracts',
  },
]

const adminServiceItems = [
  {
    to: '/facility/rooms',
    names: ['RoomDashboard'],
    icon: 'mdi-door-open',
    label: 'Room & Building',
    permission: 'view_rooms',
  },
  {
    to: '/finance/incidents',
    names: ['IncidentManage'],
    icon: 'mdi-tools',
    label: 'Sửa chữa & bảo trì',
    permissionsAny: ['manage_incidents', 'manage_maintenance'],
  },
  {
    to: '/finance/billing',
    names: ['BillingManagement'],
    icon: 'mdi-receipt-text-check-outline',
    label: 'Hóa đơn & thanh toán',
    permission: 'issue_billing',
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
    adminOnly: true,
  },
]

const overviewItems = computed(() =>
  isStudent.value ? studentOverviewItems : isStaff.value ? staffOverviewItems : adminOverviewItems)

const rubricItems = computed(() =>
  isStudent.value
    ? []
    : adminRubricItems.filter(canAccessItem))

const serviceItems = computed(() =>
  isStudent.value
    ? []
    : adminServiceItems.filter(canAccessItem))

const overviewSectionTitle = computed(() =>
  isStudent.value ? 'Tài khoản sinh viên' : isStaff.value ? 'Không gian nhân viên' : 'Tổng quan hệ thống')

const serviceLabel = computed(() =>
  isStudent.value ? 'Student Portal' : isStaff.value ? 'Employee Operations' : 'Residence Operations')

const appTitle = computed(() =>
  isStudent.value
    ? 'Cổng sinh viên ký túc xá'
    : isStaff.value ? 'Điều hành công việc' : 'Quản lý ký túc xá')

const workspaceLabel = computed(() =>
  isStudent.value ? 'Không gian sinh viên' : isStaff.value ? 'Không gian nhân viên' : 'Bảng điều hành')

const pageTitle = computed(() =>
  titleByRoute[route.name] || 'Nghiệp vụ Contract & Student Service')

const routeBadge = computed(() => {
  if (isStudent.value) return 'Sinh viên'
  if (isStaff.value) return 'Nhân viên'
  if (['RoomDashboard'].includes(route.name)) return 'Nhóm 1'
  if (['IncidentManage', 'BillingManagement', 'SystemLogs', 'AccountManage'].includes(route.name)) return 'Nhóm 3'
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
  localStorage.removeItem('user_permissions')
  localStorage.removeItem('employee_code')
  localStorage.removeItem('employee_department')
  localStorage.removeItem('employee_area')
  await router.push('/')
}
</script>

<style scoped>
.app-shell {
  display: grid;
  grid-template-columns: 260px minmax(0, 1fr);
  width: 100%;
  min-height: 100vh;
  overflow-x: hidden;
  background: var(--app-bg);
  color: var(--ink);
}

.sidebar {
  position: sticky;
  top: 0;
  display: flex;
  flex-direction: column;
  height: 100vh;
  padding: 0 12px 14px;
  background: var(--ant-sider);
  color: #f5f7f8;
  box-shadow: none;
}

.sidebar-top {
  display: grid;
  gap: 12px;
  margin: 0 -12px 10px;
  padding: 14px 12px 12px;
  border-bottom: 1px solid rgba(255, 255, 255, 0.08);
  background: var(--ant-sider-soft);
}

.brand {
  display: flex;
  align-items: center;
  gap: 12px;
  min-height: 46px;
  color: inherit;
  text-decoration: none;
}

.brand-mark {
  display: grid;
  place-items: center;
  width: 44px;
  height: 44px;
  border: 0;
  border-radius: 8px;
  background: #1677ff;
  color: #ffffff;
  font-size: 28px;
}

.brand-name {
  color: #ffffff;
  font-family: var(--font-heading);
  font-size: 20px;
  font-weight: 900;
  line-height: 1;
}

.brand-subtitle {
  display: block;
  margin-top: 5px;
  color: rgba(255, 255, 255, 0.62);
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
  border: 1px solid rgba(255, 255, 255, 0.10);
  border-radius: 6px;
  background: rgba(255, 255, 255, 0.08);
  color: rgba(255, 255, 255, 0.72);
  font-size: 12px;
  font-weight: 800;
}

.live-dot {
  width: 8px;
  height: 8px;
  border-radius: 999px;
  background: var(--ant-success);
  box-shadow: 0 0 0 4px rgba(82, 196, 26, 0.18);
}

.nav {
  flex: 1;
  min-height: 0;
  overflow-y: auto;
  padding: 8px 0;
}

.nav-section {
  margin: 18px 12px 8px;
  color: rgba(255, 255, 255, 0.42);
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
  min-height: 42px;
  width: 100%;
  margin: 2px 0;
  padding: 9px 12px;
  border: 1px solid transparent;
  border-radius: 6px;
  background: transparent;
  color: rgba(255, 255, 255, 0.72);
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
  background: rgba(255, 255, 255, 0.10);
  color: rgba(255, 255, 255, 0.76);
  font-size: 12px;
  font-weight: 900;
}

.nav-item .mdi {
  color: rgba(255, 255, 255, 0.58);
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
  border-color: transparent;
  background: #1677ff;
  color: #ffffff;
}

.nav-item.active .mdi,
.nav-item.active span:last-child {
  color: #ffffff;
}

.nav-item.active b {
  background: rgba(255, 255, 255, 0.20);
  color: #ffffff;
}

.sidebar-note {
  display: grid;
  grid-template-columns: 32px minmax(0, 1fr);
  gap: 10px;
  margin: 12px 0 0;
  padding: 14px;
  border: 1px solid rgba(255, 255, 255, 0.10);
  border-radius: 8px;
  background: rgba(255, 255, 255, 0.06);
}

.sidebar-note .mdi {
  color: #69b1ff;
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
  padding: 14px 0 0;
  border-top: 1px solid rgba(255, 255, 255, 0.08);
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
  overflow-x: clip;
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
  min-height: 92px;
  padding: 16px 28px;
  border-bottom: 1px solid var(--line);
  background: #ffffff;
  backdrop-filter: none;
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
  background: var(--ant-primary);
}

.breadcrumb-line strong {
  color: var(--ant-primary);
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
  border: 1px solid var(--line);
  border-radius: 8px;
  background: #ffffff;
}

.term-chip .mdi {
  color: var(--ant-primary);
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
  border: 1px solid var(--line);
  border-radius: 8px;
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
  width: auto;
  min-width: 0;
  max-width: 1500px;
  margin: 0 auto;
  padding: 24px 28px 48px;
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
    grid-template-columns: minmax(0, 1fr);
  }

  .sidebar {
    position: static;
    min-width: 0;
    width: 100%;
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
    width: 100%;
    padding: 18px;
  }
}
</style>
