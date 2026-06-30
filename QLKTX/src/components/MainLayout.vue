<template>
  <div class="app-shell" :class="shellModeClass">
    <aside class="sidebar">
      <div class="sidebar-top">
        <router-link to="/" class="brand">
          <div class="brand-mark">
            <span class="mdi mdi-home-city-outline"></span>
          </div>
          <div>
            <div class="brand-name">DormManager</div>
            <span class="brand-subtitle">Dai Nam Residence</span>
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

      <button class="mobile-nav-logout" type="button" @click="logout">
        <span class="mdi mdi-logout"></span>
        <span>Đăng xuất</span>
      </button>

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
          <v-btn
            class="theme-toggle"
            :icon="isDarkMode ? 'mdi-white-balance-sunny' : 'mdi-weather-night'"
            variant="tonal"
            color="primary"
            density="comfortable"
            :title="isDarkMode ? 'Chuyển sang giao diện sáng' : 'Chuyển sang giao diện tối'"
            @click="toggleTheme"
          />
          <v-menu v-model="notificationMenu" location="bottom end" :close-on-content-click="false">
            <template #activator="{ props }">
              <v-badge
                :model-value="unreadNotifications > 0"
                :content="unreadNotifications"
                color="error"
                offset-x="2"
                offset-y="2"
              >
                <v-btn
                  v-bind="props"
                  icon="mdi-bell-outline"
                  variant="tonal"
                  color="primary"
                  density="comfortable"
                  @click="loadNotifications"
                />
              </v-badge>
            </template>
            <section class="notification-menu">
              <div class="notification-head">
                <strong>Thông báo</strong>
                <v-btn size="small" variant="text" :loading="notificationLoading" @click="loadNotifications">Làm mới</v-btn>
              </div>
              <div v-if="notifications.length === 0" class="notification-empty">
                Chưa có thông báo.
              </div>
              <article
                v-for="item in notifications.slice(0, 8)"
                :key="item.id"
                class="notification-row"
                :class="{ unread: !item.isRead }"
                role="button"
                tabindex="0"
                @click="markNotificationRead(item)"
                @keydown.enter.prevent="markNotificationRead(item)"
              >
                <span :class="['notification-dot', severityClass(item.severity)]"></span>
                <div>
                  <strong>{{ item.title }}</strong>
                  <p>{{ item.content }}</p>
                  <small>{{ notificationAudienceLabel(item.targetAudience) }} · {{ formatNotificationTime(item.publishedAt || item.createdAt) }}</small>
                  <div v-if="item.attachments?.length" class="notification-attachments">
                    <button
                      v-for="attachment in item.attachments"
                      :key="attachment.id"
                      type="button"
                      @click.stop="downloadNotificationAttachment(item, attachment)"
                    >
                      <span class="mdi mdi-paperclip"></span>
                      <span>{{ attachment.fileName }}</span>
                    </button>
                  </div>
                </div>
              </article>
            </section>
          </v-menu>
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
          <v-btn
            class="topbar-logout"
            color="error"
            variant="tonal"
            prepend-icon="mdi-logout"
            @click="logout"
          >
            Đăng xuất
          </v-btn>
        </div>
      </header>

      <div class="page-body">
        <router-view />
      </div>
    </main>

    <button class="mobile-floating-logout" type="button" @click="logout">
      <span class="mdi mdi-logout"></span>
      <span>Đăng xuất</span>
    </button>
  </div>
</template>

<script setup>
import { computed, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import api from '@/services/api'
import { getPermissions, normalizeRole } from '@/utils/auth'

const route = useRoute()
const router = useRouter()
const userRole = ref(localStorage.getItem('user_role') || 'N2 Admin')
const fullName = ref(localStorage.getItem('fullName') || 'demo_admin')

const roleKey = computed(() => normalizeRole(userRole.value))
const isStudent = computed(() => roleKey.value === 'Student')
const isStaff = computed(() => roleKey.value === 'Staff')
const isDarkMode = ref(localStorage.getItem('ktx_theme_mode') === 'dark')
const shellModeClass = computed(() => ({
  'student-shell': isStudent.value,
  'operations-shell': !isStudent.value,
  'staff-shell': isStaff.value,
  'admin-shell': roleKey.value === 'Admin',
  'dark-shell': isDarkMode.value,
}))
const permissions = ref(getPermissions())
const notificationMenu = ref(false)
const notificationLoading = ref(false)
const notifications = ref([])
const unreadNotifications = computed(() =>
  notifications.value.filter((item) => !item.isRead).length)
const can = (permission) => roleKey.value === 'Admin' || permissions.value.includes(permission)
const canAccessItem = (item) =>
  (!item.adminOnly || roleKey.value === 'Admin') &&
  (!item.permission || can(item.permission)) &&
  (!item.permissionsAny || item.permissionsAny.some((permission) => can(permission)))
const toggleTheme = () => {
  isDarkMode.value = !isDarkMode.value
  localStorage.setItem('ktx_theme_mode', isDarkMode.value ? 'dark' : 'light')
}

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
  SystemNotifications: 'Tạo, gửi và theo dõi thông báo cho người dùng',
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
    to: '/student-service/students',
    names: ['StudentManage'],
    icon: 'mdi-account-school-outline',
    label: 'Hồ sơ sinh viên',
    permission: 'view_students',
  },
  {
    to: '/student-service/registrations',
    names: ['RoomRegistrationCreate'],
    icon: 'mdi-form-select',
    label: 'Đăng ký nội trú',
    adminOnly: true,
  },
  {
    to: '/student-service/registrations/approval',
    names: ['RoomRegistrationApproval'],
    icon: 'mdi-clipboard-check-outline',
    label: 'Duyệt xếp phòng',
    permission: 'approve_registrations',
  },
  {
    to: '/student-service/contracts',
    names: ['ContractList'],
    icon: 'mdi-file-document-outline',
    label: 'Hợp đồng thuê phòng',
    permission: 'manage_contracts',
  },
  {
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
  {
    to: '/system/notifications',
    names: ['SystemNotifications'],
    icon: 'mdi-bell-cog-outline',
    label: 'Thông báo hệ thống',
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
  if (['IncidentManage', 'BillingManagement', 'SystemLogs', 'SystemNotifications', 'AccountManage'].includes(route.name)) return 'Nhóm 3'
  return 'Nhóm 2'
})

const userInitial = computed(() => {
  return (fullName.value || 'U').trim().charAt(0).toUpperCase()
})

const loadNotifications = async () => {
  try {
    notificationLoading.value = true
    const response = await api.get('/notifications')
    notifications.value = normalizeList(response.data)
  } catch {
    notifications.value = []
  } finally {
    notificationLoading.value = false
  }
}

const markNotificationRead = async (item) => {
  if (item.isRead)
    return

  try {
    await api.put(`/notifications/${item.id}/read`)
    item.isRead = true
  } catch {
    await loadNotifications()
  }
}

const downloadNotificationAttachment = async (notification, attachment) => {
  try {
    const response = await api.get(`/notifications/${notification.id}/attachments/${attachment.id}`, {
      responseType: 'blob',
    })
    const blob = new Blob([response.data], {
      type: response.headers?.['content-type'] || attachment.contentType || 'application/octet-stream',
    })
    const url = URL.createObjectURL(blob)
    const link = document.createElement('a')
    link.href = url
    link.download = attachment.fileName || 'tep-dinh-kem'
    document.body.appendChild(link)
    link.click()
    link.remove()
    URL.revokeObjectURL(url)

    if (!notification.isRead) {
      await markNotificationRead(notification)
    }
  } catch {
    await loadNotifications()
  }
}

const normalizeList = (payload) => Array.isArray(payload) ? payload : payload?.data || []
const notificationAudienceLabel = (value) => ({
  All: 'Tất cả',
  Student: 'Sinh viên',
  Staff: 'Nhân viên',
  Admin: 'Admin',
}[value] || 'Tất cả')
const severityClass = (value) => `severity-${String(value || 'Normal').toLowerCase()}`
const formatNotificationTime = (value) => value
  ? new Intl.DateTimeFormat('vi-VN', { dateStyle: 'short', timeStyle: 'short' }).format(new Date(value))
  : '-'

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

onMounted(loadNotifications)
</script>

<style scoped>
.app-shell {
  display: grid;
  grid-template-columns: 260px minmax(0, 1fr);
  width: 100%;
  min-height: 100vh;
  overflow-x: clip;
  background: var(--app-bg);
  color: var(--ink);
}

.sidebar {
  position: sticky;
  top: 0;
  align-self: start;
  display: flex;
  flex-direction: column;
  height: 100vh;
  height: 100dvh;
  overflow: hidden;
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
  background: linear-gradient(135deg, #f97316, #c2410c);
  color: #ffffff;
  font-size: 28px;
  box-shadow: 0 14px 26px rgba(194, 65, 12, 0.32);
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
  overscroll-behavior: contain;
  padding: 8px 0;
  scrollbar-color: rgba(255, 255, 255, 0.24) transparent;
  scrollbar-width: thin;
}

.nav::-webkit-scrollbar {
  width: 5px;
}

.nav::-webkit-scrollbar-track {
  background: transparent;
}

.nav::-webkit-scrollbar-thumb {
  border-radius: 999px;
  background: rgba(255, 255, 255, 0.20);
}

.nav:hover::-webkit-scrollbar-thumb {
  background: rgba(255, 255, 255, 0.38);
}

.nav::-webkit-scrollbar-button {
  display: none;
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

.nav-item .mdi {
  color: rgba(255, 255, 255, 0.58);
  font-size: 21px;
}

.nav-item span:last-child {
  min-width: 0;
  overflow: visible;
  text-overflow: clip;
  white-space: normal;
  overflow-wrap: normal;
  word-break: normal;
  line-height: 1.25;
}

.nav-item:hover,
.nav-item.active {
  border-color: transparent;
  background: linear-gradient(135deg, #f97316, #ea580c);
  color: #ffffff;
  box-shadow: 0 14px 28px rgba(234, 88, 12, 0.22);
}

.nav-item.active .mdi,
.nav-item.active span:last-child {
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

.topbar-logout {
  min-height: 50px;
  border-radius: 8px;
  font-weight: 900;
}

.notification-menu {
  width: min(420px, calc(100vw - 32px));
  max-height: 520px;
  overflow-y: auto;
  padding: 12px;
  border: 1px solid var(--line);
  border-radius: 8px;
  background: #ffffff;
  box-shadow: 0 12px 28px rgba(15, 23, 42, 0.14);
}

.notification-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 10px;
  margin-bottom: 8px;
}

.notification-head strong {
  font-size: 15px;
}

.notification-empty {
  padding: 18px;
  color: var(--muted);
  text-align: center;
}

.notification-row {
  display: grid;
  grid-template-columns: 10px minmax(0, 1fr);
  gap: 10px;
  width: 100%;
  padding: 10px 8px;
  border: 0;
  border-bottom: 1px solid #edf0ee;
  background: transparent;
  text-align: left;
  cursor: pointer;
}

.notification-row.unread {
  background: #f6ffed;
}

.notification-row strong,
.notification-row small {
  display: block;
}

.notification-row p {
  margin: 4px 0;
  color: #31443a;
  font-size: 13px;
  line-height: 1.45;
}

.notification-row small {
  color: var(--muted);
  font-size: 12px;
}

.notification-attachments {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
  margin-top: 8px;
}

.notification-attachments button {
  display: inline-flex;
  align-items: center;
  max-width: 100%;
  gap: 4px;
  padding: 4px 8px;
  border: 1px solid #b7eb8f;
  border-radius: 6px;
  background: #f6ffed;
  color: #237804;
  font-size: 12px;
  font-weight: 800;
}

.notification-attachments button span:last-child {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.notification-dot {
  width: 9px;
  height: 9px;
  margin-top: 6px;
  border-radius: 999px;
  background: #8c8c8c;
}

.notification-dot.severity-important {
  background: var(--brand);
}

.notification-dot.severity-urgent {
  background: #cf1322;
}

.mobile-floating-logout {
  display: none;
}

.mobile-nav-logout {
  display: none;
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

  .topbar-logout {
    width: 100%;
    justify-content: center;
  }

  .mobile-floating-logout {
    position: fixed;
    right: 16px;
    bottom: calc(16px + env(safe-area-inset-bottom));
    z-index: 80;
    display: inline-flex;
    align-items: center;
    justify-content: center;
    gap: 8px;
    min-height: 46px;
    padding: 0 16px;
    border: 1px solid rgba(220, 38, 38, 0.18);
    border-radius: 999px;
    background: #dc2626;
    color: #ffffff;
    box-shadow: 0 14px 30px rgba(127, 29, 29, 0.28);
    font: inherit;
    font-size: 14px;
    font-weight: 900;
  }

  .mobile-floating-logout .mdi {
    font-size: 20px;
  }

  .mobile-nav-logout {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    gap: 8px;
    width: 100%;
    min-height: 42px;
    margin-top: 10px;
    border: 1px solid rgba(220, 38, 38, 0.2);
    border-radius: 8px;
    background: #fff1f2;
    color: #b91c1c;
    font: inherit;
    font-size: 14px;
    font-weight: 900;
  }

  .mobile-nav-logout .mdi {
    font-size: 19px;
  }

  .user-chip strong,
  .user-chip span {
    text-align: left;
  }

  .page-body {
    width: 100%;
    padding: 18px 18px 88px;
  }
}

.app-shell {
  grid-template-columns: 288px minmax(0, 1fr);
  background:
    radial-gradient(circle at 18% 0%, rgba(243, 111, 33, 0.12), transparent 32%),
    linear-gradient(135deg, #fff8f1 0%, #fffaf6 42%, #ffffff 100%);
}

.operations-shell {
  background:
    radial-gradient(circle at 0 0, rgba(243, 111, 33, 0.18), transparent 34%),
    linear-gradient(135deg, #140f0c 0%, #241510 36%, #fff8f1 36%, #fffaf6 100%);
}

.sidebar {
  padding: 0 14px 16px;
  background:
    linear-gradient(180deg, rgba(124, 45, 18, 0.28), transparent 34%),
    linear-gradient(180deg, #1a120d 0%, #24150e 58%, #140f0c 100%);
  box-shadow: 10px 0 32px rgba(124, 45, 18, 0.18);
}

.sidebar-top {
  margin: 0 -14px 12px;
  padding: 18px 14px 14px;
  border-bottom: 1px solid rgba(255, 184, 116, 0.14);
  background:
    linear-gradient(135deg, rgba(243, 111, 33, 0.24), transparent 52%),
    rgba(255, 255, 255, 0.02);
}

.brand-mark {
  border-radius: 10px;
  background: linear-gradient(135deg, #f36f21, #ffb347);
  color: #fff;
  box-shadow: 0 14px 28px rgba(243, 111, 33, 0.26);
}

.brand-name {
  font-size: 21px;
  letter-spacing: 0;
}

.brand-subtitle {
  color: #ffd8b8;
  letter-spacing: 0.12em;
}

.system-pill {
  border-color: rgba(255, 184, 116, 0.22);
  background: rgba(255, 255, 255, 0.08);
  color: #ffe6d3;
}

.live-dot {
  background: #ffb347;
  box-shadow: 0 0 0 4px rgba(255, 179, 71, 0.18);
}

.nav {
  padding: 6px 0 4px;
  scrollbar-color: rgba(255, 184, 116, 0.38) transparent;
}

.nav::-webkit-scrollbar-thumb {
  background: rgba(255, 184, 116, 0.26);
}

.nav:hover::-webkit-scrollbar-thumb {
  background: rgba(255, 184, 116, 0.48);
}

.nav-section {
  margin: 18px 10px 8px;
  color: rgba(255, 226, 203, 0.58);
  font-size: 10.5px;
}

.nav-item {
  grid-template-columns: 30px minmax(0, 1fr);
  min-height: 44px;
  margin: 3px 0;
  padding: 10px 12px;
  border-radius: 8px;
  color: rgba(255, 247, 237, 0.76);
}

.nav-item.rubric-item {
  grid-template-columns: 30px minmax(0, 1fr);
}

.nav-item .mdi {
  color: rgba(255, 216, 184, 0.78);
  font-size: 22px;
}

.nav-item:hover {
  border-color: rgba(255, 184, 116, 0.18);
  background: rgba(255, 255, 255, 0.08);
  color: #fff;
}

.nav-item.active {
  border-color: rgba(255, 184, 116, 0.32);
  background: linear-gradient(135deg, #f36f21, #d95712);
  color: #fff;
  box-shadow: 0 12px 24px rgba(124, 45, 18, 0.26);
}

.sidebar-note {
  border-color: rgba(255, 184, 116, 0.18);
  background: rgba(255, 255, 255, 0.07);
}

.sidebar-note .mdi {
  color: #ffb347;
}

.sidebar-note p {
  color: #f7d8bf;
}

.account-box {
  border-top-color: rgba(255, 184, 116, 0.16);
}

.account-avatar,
.user-avatar {
  background: #fff3e8;
  color: #9a3412;
}

.account-meta span {
  color: #eac4aa;
}

.main-panel {
  background:
    linear-gradient(180deg, rgba(255, 243, 232, 0.92), rgba(255, 248, 241, 0.76) 260px, #fffaf6 100%);
}

.topbar {
  min-height: 96px;
  border-bottom: 1px solid rgba(232, 196, 173, 0.72);
  background: rgba(255, 255, 255, 0.88);
  box-shadow: 0 10px 28px rgba(124, 45, 18, 0.07);
  backdrop-filter: blur(18px);
}

.breadcrumb-line {
  color: #8a5a3f;
}

.breadcrumb-line i {
  background: var(--brand);
  color: var(--brand);
}

.breadcrumb-line strong {
  color: var(--brand);
}

.topbar h1 {
  color: #24150e;
  font-size: clamp(22px, 1.7vw, 28px);
}

.topbar p {
  color: #6f5747;
}

.term-chip,
.user-chip {
  border-color: #f0d7c6;
  background: rgba(255, 255, 255, 0.86);
  box-shadow: 0 10px 22px rgba(124, 45, 18, 0.06);
}

.term-chip .mdi {
  color: var(--brand);
}

.notification-menu {
  border-color: #f0d7c6;
  box-shadow: 0 20px 46px rgba(124, 45, 18, 0.16);
}

.notification-row.unread {
  background: #fff7ed;
}

.notification-attachments button {
  border-color: #fed7aa;
  background: #fff7ed;
  color: #c2410c;
}

.notification-dot.severity-important {
  background: #f36f21;
}

.page-body {
  max-width: 1540px;
  padding: 28px 32px 54px;
}

.dark-shell {
  --app-bg: #160f0b;
  --surface: #201610;
  --surface-soft: #291b13;
  --line: rgba(255, 190, 135, 0.18);
  --line-strong: rgba(255, 190, 135, 0.32);
  --ink: #fff7ed;
  --muted: #d6b9a2;
  --muted-strong: #f2d7c2;
  background:
    radial-gradient(circle at 88% 0, rgba(243, 111, 33, 0.16), transparent 32%),
    linear-gradient(135deg, #140f0c 0%, #1f130d 58%, #25160e 100%);
}

.dark-shell .main-panel {
  background:
    radial-gradient(circle at 100% 0, rgba(243, 111, 33, 0.16), transparent 38%),
    linear-gradient(180deg, #1a120d 0%, #21150f 100%);
}

.dark-shell .topbar {
  border-bottom-color: rgba(255, 190, 135, 0.14);
  background: rgba(24, 15, 10, 0.88);
  box-shadow: 0 16px 32px rgba(0, 0, 0, 0.22);
}

.dark-shell .topbar h1,
.dark-shell .page-heading h2,
.dark-shell .page-head h2,
.dark-shell .section-title h3,
.dark-shell .detail-header h3,
.dark-shell .cell-title,
.dark-shell .term-chip strong,
.dark-shell .user-chip strong,
.dark-shell .account-meta strong,
.dark-shell h2,
.dark-shell h3 {
  color: #fff7ed !important;
}

.dark-shell .topbar p,
.dark-shell .page-heading p,
.dark-shell .page-head p,
.dark-shell .section-title p,
.dark-shell .cell-subtitle,
.dark-shell small,
.dark-shell .muted {
  color: #d6b9a2 !important;
}

.dark-shell .term-chip,
.dark-shell .user-chip,
.dark-shell .notification-menu,
.dark-shell :where(.v-card, .panel, .table-card, .filter-card, .dashboard-hero, .hero-summary div, .metric-card, .registration-metric, .stat-tile, .invoice-list, .invoice-detail, .history-panel, .wallet-panel, .topup-box, .wallet-history, .analytics-card, .chart-panel, .summary-band) {
  border-color: rgba(255, 190, 135, 0.18) !important;
  background: rgba(32, 22, 16, 0.92) !important;
  color: #fff7ed !important;
}

.dark-shell :where(.v-field, .v-table, .data-table, .account-table, .operations-table, .notifications-table) {
  background: rgba(32, 22, 16, 0.92) !important;
  color: #fff7ed !important;
}

.dark-shell :where(.data-table th, .v-table thead th) {
  background: #2f1d12 !important;
  color: #ffedd5 !important;
}

.dark-shell :where(.data-table td, .v-table tbody td) {
  border-color: rgba(255, 190, 135, 0.14) !important;
}

.dark-shell :where(.data-table tbody tr:hover, .v-table tbody tr:hover, .invoice-row:hover, .invoice-row.active, .notification-row.unread) {
  background: rgba(243, 111, 33, 0.10) !important;
}

.dark-shell :where(.v-label, .v-field__input, .v-select__selection-text, input, textarea) {
  color: #fff7ed !important;
}

.dark-shell :where(.v-field__outline, .v-field__outline__start, .v-field__outline__end) {
  border-color: rgba(255, 190, 135, 0.22) !important;
}

.dark-shell :where(.wallet-main, .wallet-transfer, .transfer-note, .allocation-row, .sidebar-note, .detail-summary > div, .description-box, .action-panel, .timeline-panel, .checklist-panel) {
  border-color: rgba(255, 190, 135, 0.20) !important;
  background: rgba(44, 28, 18, 0.92) !important;
}

.dark-shell .theme-toggle {
  box-shadow: 0 10px 22px rgba(243, 111, 33, 0.18);
}

@media (max-width: 1180px) {
  .app-shell {
    grid-template-columns: 260px minmax(0, 1fr);
  }

  .topbar-actions {
    gap: 10px;
  }

  .topbar-logout {
    padding-inline: 12px !important;
  }
}

@media (max-width: 920px) {
  .app-shell,
  .operations-shell {
    display: block;
    background: #fff8f1;
  }

  .sidebar {
    position: sticky;
    top: 0;
    z-index: 20;
    width: 100%;
    height: auto;
    max-height: none;
    padding: 12px 14px;
    box-shadow: 0 10px 28px rgba(124, 45, 18, 0.16);
  }

  .sidebar-top {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 12px;
    margin: 0 0 10px;
    padding: 0 0 10px;
    background: transparent;
  }

  .brand {
    margin: 0;
  }

  .brand-mark {
    width: 42px;
    height: 42px;
    font-size: 25px;
  }

  .brand-name {
    font-size: 18px;
  }

  .brand-subtitle {
    font-size: 10px;
  }

  .system-pill {
    display: inline-flex;
    flex: 0 0 auto;
    max-width: 42%;
  }

  .nav {
    display: grid;
    grid-auto-flow: column;
    grid-auto-columns: minmax(142px, 176px);
    gap: 8px;
    overflow-x: auto;
    padding: 2px 0 6px;
    scroll-snap-type: x proximity;
  }

  .nav-section,
  .sidebar-note,
  .account-box {
    display: none;
  }

  .nav-item,
  .nav-item.rubric-item {
    display: grid;
    grid-template-columns: 24px minmax(0, 1fr);
    scroll-snap-align: start;
    min-height: 42px;
    margin: 0;
    padding: 9px 10px;
    font-size: 12px;
  }

  .nav-item .mdi {
    font-size: 20px;
  }

  .mobile-nav-logout {
    display: none;
  }

  .topbar {
    position: static;
    min-height: auto;
    padding: 18px;
    border-bottom: 1px solid #f0d7c6;
  }

  .topbar-actions {
    display: grid;
    grid-template-columns: auto auto minmax(0, 1fr);
    align-items: center;
    width: 100%;
  }

  .topbar-actions > .v-badge {
    justify-self: start;
  }

  .term-chip,
  .user-chip,
  .topbar-logout {
    grid-column: 1 / -1;
    width: 100%;
  }

  .term-chip,
  .user-chip {
    min-width: 0;
    width: 100%;
  }

  .page-body {
    padding: 18px 14px 92px;
  }

  .mobile-floating-logout {
    display: none !important;
    background: linear-gradient(135deg, #f36f21, #c2410c);
    box-shadow: 0 16px 30px rgba(194, 65, 12, 0.30);
  }
}

@media (max-width: 560px) {
  .sidebar {
    padding: 10px 10px 12px;
  }

  .sidebar-top {
    align-items: flex-start;
    flex-direction: column;
  }

  .system-pill {
    max-width: 100%;
  }

  .nav {
    grid-auto-columns: minmax(128px, 152px);
  }

  .topbar {
    padding: 16px 14px;
  }

  .topbar h1 {
    font-size: 22px;
  }

  .topbar-actions {
    grid-template-columns: auto auto;
  }

  .user-chip strong,
  .user-chip span {
    text-align: left;
  }

  .page-body {
    padding-inline: 10px;
  }
}
</style>
