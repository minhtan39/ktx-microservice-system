<template>
  <section class="account-page">
    <div class="page-head">
      <div>
        <span class="page-kicker">AuthService</span>
        <h2>Quản lý tài khoản</h2>
        <p>Admin quản lý trạng thái, phân quyền và gửi link bảo mật; mật khẩu không hiển thị trong hệ thống.</p>
      </div>
      <div class="head-actions">
        <v-btn variant="outlined" prepend-icon="mdi-refresh" :loading="loading" @click="loadAccounts">Làm mới</v-btn>
        <v-btn color="primary" prepend-icon="mdi-account-plus-outline" @click="openCreate">Tạo nhân viên</v-btn>
      </div>
    </div>

    <div class="account-metrics">
      <article v-for="metric in accountMetrics" :key="metric.label" class="account-metric">
        <span :class="['mdi', metric.icon]"></span>
        <div><strong>{{ metric.value }}</strong><small>{{ metric.label }}</small></div>
      </article>
    </div>

    <v-alert v-if="error" type="error" variant="tonal">{{ error }}</v-alert>
    <v-alert v-if="success" type="success" variant="tonal">{{ success }}</v-alert>

    <section class="panel">
      <div class="toolbar-row">
        <v-text-field v-model="search" label="Tìm tài khoản, họ tên hoặc mã nhân viên" prepend-inner-icon="mdi-magnify" variant="outlined" density="compact" hide-details clearable />
        <v-select v-model="roleFilter" :items="roleOptions" item-title="title" item-value="value" label="Vai trò" variant="outlined" density="compact" hide-details />
        <v-select v-model="statusFilter" :items="statusOptions" item-title="title" item-value="value" label="Trạng thái" variant="outlined" density="compact" hide-details />
        <v-btn color="success" variant="tonal" prepend-icon="mdi-file-excel-outline" :disabled="filteredAccounts.length === 0" @click="exportAccounts">
          Xuất Excel
        </v-btn>
      </div>

      <v-data-table
        :headers="headers"
        :items="filteredAccounts"
        :loading="loading"
        :items-per-page="10"
        item-value="username"
        class="account-table"
        no-data-text="Không có tài khoản phù hợp."
      >
        <template #item.username="{ item }">
          <div class="cell-stack"><strong>{{ item.username }}</strong><small>{{ item.employeeCode || item.studentCode || '-' }}</small></div>
        </template>
        <template #item.profile="{ item }">
          <div class="cell-stack"><strong>{{ item.fullName }}</strong><small>{{ item.department || item.email || 'Chưa cập nhật hồ sơ' }}</small></div>
        </template>
        <template #item.role="{ item }"><span :class="['role-pill', item.role.toLowerCase()]">{{ roleLabel(item.role) }}</span></template>
        <template #item.accountStatus="{ item }"><span :class="['status-pill', String(item.accountStatus || 'Active').toLowerCase()]">{{ statusLabel(item.accountStatus) }}</span></template>
        <template #item.permissions="{ item }">
          <div v-if="item.role === 'Staff'" class="permission-summary">{{ item.permissions?.length || 0 }} quyền<small>{{ item.assignedArea || 'Chưa phân khu vực' }}</small></div>
          <span v-else class="muted">Theo hồ sơ sinh viên</span>
        </template>
        <template #item.security="{ item }">
          <div class="security-state">
            <span :class="['mdi', securityIcon(item)]"></span>
            <span>{{ securityLabel(item) }}</span>
          </div>
        </template>
        <template #item.actions="{ item }">
          <div class="action-row">
            <v-btn
              icon="mdi-email-lock-outline"
              variant="text"
              color="success"
              size="small"
              title="Gửi link kích hoạt hoặc đặt lại mật khẩu"
              :loading="sendingAccessLink === item.username"
              @click="sendAccessLink(item)"
            />
            <v-btn icon="mdi-pencil-outline" variant="text" color="primary" size="small" title="Sửa tài khoản" @click="openEdit(item)" />
            <v-btn
              :icon="isLocked(item) ? 'mdi-lock-open-outline' : 'mdi-lock-outline'"
              variant="text"
              :color="isLocked(item) ? 'success' : 'warning'"
              size="small"
              :title="isLocked(item) ? 'Mở khóa tài khoản' : 'Khóa tài khoản'"
              @click="openAccessChange(item)"
            />
          </div>
        </template>
      </v-data-table>
    </section>

    <v-dialog v-model="dialog" max-width="820">
      <v-card>
        <v-card-title>{{ editing ? 'Cập nhật tài khoản' : 'Tạo nhân viên vận hành' }}</v-card-title>
        <v-card-text>
          <v-alert v-if="dialogError" type="error" variant="tonal" closable class="mb-4" @click:close="dialogError = ''">
            {{ dialogError }}
          </v-alert>
          <v-form class="edit-form" @submit.prevent="saveAccount">
            <div class="form-grid">
              <v-text-field v-model="form.username" label="Tên đăng nhập" variant="outlined" density="comfortable" />
              <v-text-field v-model="form.fullName" label="Họ tên" variant="outlined" density="comfortable" />
              <v-text-field v-if="isStaffForm" v-model="form.employeeCode" label="Mã nhân viên" variant="outlined" density="comfortable" />
              <v-text-field v-if="isStaffForm" v-model="form.email" label="Email" type="email" variant="outlined" density="comfortable" />
              <v-text-field v-if="isStaffForm" v-model="form.phone" label="Số điện thoại" variant="outlined" density="comfortable" />
              <v-select v-if="isStaffForm" v-model="form.department" :items="departmentOptions" label="Bộ phận" variant="outlined" density="comfortable" />
              <v-text-field v-if="isStaffForm" v-model="form.jobTitle" label="Chức vụ" variant="outlined" density="comfortable" />
              <v-text-field v-if="isStaffForm" v-model="form.assignedArea" label="Khu vực phụ trách" variant="outlined" density="comfortable" />
              <v-select v-if="isStaffForm" v-model="form.accountStatus" :items="staffStatusOptions" item-title="title" item-value="value" label="Trạng thái tài khoản" variant="outlined" density="comfortable" />
            </div>

            <v-alert type="info" variant="tonal" density="compact" class="mt-2">
              Admin không xem hoặc đặt hộ mật khẩu. Sau khi lưu, hãy gửi link bảo mật để người dùng tự đặt mật khẩu qua email.
            </v-alert>

            <section v-if="isStaffForm" class="permission-panel">
              <div><strong>Quyền nghiệp vụ</strong><p>Chỉ những mục được bật mới xuất hiện trong menu nhân viên.</p></div>
              <div class="permission-grid">
                <label v-for="permission in permissionOptions" :key="permission.value" class="permission-item">
                  <v-checkbox v-model="form.permissions" :value="permission.value" color="primary" hide-details />
                  <span><strong>{{ permission.title }}</strong><small>{{ permission.description }}</small></span>
                </label>
              </div>
            </section>

            <v-alert v-if="editing?.role === 'Student'" type="info" variant="tonal" density="compact">Tài khoản sinh viên vẫn liên kết với hồ sơ N2 bằng studentId; màn này chỉ đổi thông tin đăng nhập.</v-alert>
          </v-form>
        </v-card-text>
        <v-card-actions><v-spacer /><v-btn variant="text" @click="dialog = false">Hủy</v-btn><v-btn color="primary" :loading="saving" @click="saveAccount">{{ editing ? 'Lưu thay đổi' : 'Tạo nhân viên' }}</v-btn></v-card-actions>
      </v-card>
    </v-dialog>

    <v-dialog v-model="accessDialog" max-width="520">
      <v-card v-if="accessTarget">
        <v-card-title>{{ isLocked(accessTarget) ? 'Xác nhận mở khóa tài khoản' : 'Xác nhận khóa tài khoản' }}</v-card-title>
        <v-card-text>
          <v-alert v-if="accessError" type="error" variant="tonal" closable class="mb-4" @click:close="accessError = ''">
            {{ accessError }}
          </v-alert>
          <p v-if="isLocked(accessTarget)">Bạn sắp mở khóa tài khoản <strong>{{ accessTarget.username }}</strong>. Người dùng có thể đăng nhập lại nếu mật khẩu còn hợp lệ.</p>
          <p v-else>Bạn sắp khóa tài khoản <strong>{{ accessTarget.username }}</strong>. Người dùng sẽ không thể đăng nhập cho đến khi được mở khóa.</p>
          <v-alert type="info" variant="tonal">
            Hồ sơ sinh viên/nhân viên, hợp đồng, hóa đơn và lịch sử nghiệp vụ vẫn được giữ nguyên. Hệ thống chỉ thay đổi quyền đăng nhập của tài khoản này.
          </v-alert>
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="accessDialog = false">Hủy</v-btn>
          <v-btn :color="isLocked(accessTarget) ? 'success' : 'warning'" :loading="locking" @click="toggleAccountLock">
            {{ isLocked(accessTarget) ? 'Mở khóa' : 'Khóa tài khoản' }}
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </section>
</template>

<script setup>
import { computed, onMounted, reactive, ref } from 'vue'
import api from '@/services/api'
import { exportRowsToExcel } from '@/utils/exportExcel'

const loading = ref(false), saving = ref(false), locking = ref(false), sendingAccessLink = ref('')
const error = ref(''), success = ref(''), accounts = ref([]), search = ref('')
const dialogError = ref(''), accessError = ref('')
const roleFilter = ref('All'), statusFilter = ref('All'), dialog = ref(false), accessDialog = ref(false)
const editing = ref(null), accessTarget = ref(null)

const headers = [
  { title: 'Tài khoản', key: 'username', sortable: false }, { title: 'Hồ sơ', key: 'profile', sortable: false },
  { title: 'Vai trò', key: 'role', sortable: false }, { title: 'Trạng thái', key: 'accountStatus', sortable: false },
  { title: 'Phạm vi', key: 'permissions', sortable: false }, { title: 'Bảo mật', key: 'security', sortable: false },
  { title: '', key: 'actions', sortable: false, align: 'end' },
]
const roleOptions = [{ title: 'Tất cả', value: 'All' }, { title: 'Nhân viên', value: 'Staff' }, { title: 'Sinh viên', value: 'Student' }]
const statusOptions = [{ title: 'Tất cả', value: 'All' }, { title: 'Đang hoạt động', value: 'Active' }, { title: 'Chờ kích hoạt', value: 'Pending' }, { title: 'Tạm khóa', value: 'Locked' }, { title: 'Ngừng hoạt động', value: 'Inactive' }]
const staffStatusOptions = statusOptions.slice(1)
const departmentOptions = ['Kỹ thuật', 'Quản lý nội trú', 'Kế toán', 'An ninh', 'Vệ sinh']
const permissionOptions = [
  { value: 'manage_incidents', title: 'Xử lý sửa chữa', description: 'Tiếp nhận và cập nhật yêu cầu sửa chữa' },
  { value: 'manage_maintenance', title: 'Thực hiện bảo trì', description: 'Quản lý lịch và checklist bảo trì' },
  { value: 'view_students', title: 'Xem sinh viên', description: 'Tra cứu hồ sơ và phòng đang ở' },
  { value: 'approve_registrations', title: 'Duyệt đăng ký', description: 'Duyệt đơn và xếp phòng' },
  { value: 'manage_contracts', title: 'Quản lý hợp đồng', description: 'Theo dõi hợp đồng nội trú' },
  { value: 'view_rooms', title: 'Xem phòng', description: 'Theo dõi phòng và số giường' },
  { value: 'issue_billing', title: 'Phát hành hóa đơn', description: 'Nhập điện nước và tạo phiếu tháng' },
  { value: 'confirm_payments', title: 'Xác nhận thanh toán', description: 'Đối soát giao dịch thủ công' },
]

const emptyForm = () => ({ username: '', fullName: '', employeeCode: '', email: '', phone: '', department: 'Kỹ thuật', jobTitle: 'Nhân viên vận hành', assignedArea: '', accountStatus: 'Pending', permissions: ['manage_incidents', 'manage_maintenance', 'view_students', 'view_rooms'] })
const form = reactive(emptyForm())
const isStaffForm = computed(() => !editing.value || editing.value.role === 'Staff')
const normalizeList = (data) => Array.isArray(data) ? data : data?.data || []
const isLocked = (account) => (account?.accountStatus || 'Active') === 'Locked'

const loadAccounts = async () => { try { loading.value = true; error.value = ''; accounts.value = normalizeList((await api.get('/auth/accounts')).data) } catch (err) { error.value = 'Không tải được danh sách tài khoản.'; console.error(err) } finally { loading.value = false } }
const filteredAccounts = computed(() => { const keyword = search.value.trim().toLowerCase(); return accounts.value.filter((item) => (roleFilter.value === 'All' || item.role === roleFilter.value) && (statusFilter.value === 'All' || (item.accountStatus || 'Active') === statusFilter.value) && (!keyword || [item.username, item.fullName, item.employeeCode, item.studentCode, item.department].join(' ').toLowerCase().includes(keyword))) })
const accountMetrics = computed(() => [
  { icon: 'mdi-account-group-outline', value: accounts.value.length, label: 'Tổng tài khoản' },
  { icon: 'mdi-account-hard-hat-outline', value: accounts.value.filter((item) => item.role === 'Staff').length, label: 'Nhân viên vận hành' },
  { icon: 'mdi-account-check-outline', value: accounts.value.filter((item) => (item.accountStatus || 'Active') === 'Active').length, label: 'Đang hoạt động' },
  { icon: 'mdi-school-outline', value: accounts.value.filter((item) => item.role === 'Student').length, label: 'Sinh viên' },
])

const assignForm = (value) => Object.assign(form, emptyForm(), value, { permissions: [...(value?.permissions || emptyForm().permissions)] })
const openCreate = () => { editing.value = null; assignForm(null); dialogError.value = ''; error.value = ''; dialog.value = true }
const openEdit = (account) => { editing.value = account; assignForm(account); dialogError.value = ''; error.value = ''; dialog.value = true }
const openAccessChange = (account) => { accessTarget.value = account; accessError.value = ''; accessDialog.value = true }

const saveAccount = async () => {
  if (!form.username.trim() || !form.fullName.trim() || (isStaffForm.value && (!form.employeeCode.trim() || !form.email.trim()))) { dialogError.value = 'Vui lòng nhập đủ tên đăng nhập, họ tên, mã nhân viên và email.'; return }
  try {
    saving.value = true; dialogError.value = ''; error.value = ''; success.value = ''
    const payload = { ...form, permissions: [...form.permissions] }
    if (editing.value) await api.put(`/auth/accounts/${encodeURIComponent(editing.value.username)}`, payload)
    else await api.post('/auth/accounts', payload)
    dialog.value = false; success.value = editing.value ? 'Đã cập nhật tài khoản.' : 'Đã tạo nhân viên. Hãy gửi link kích hoạt qua email.'; await loadAccounts()
  } catch (err) { dialogError.value = err.response?.data?.message || 'Không lưu được tài khoản.'; console.error(err) } finally { saving.value = false }
}

const sendAccessLink = async (account) => {
  if (!account?.username) return

  try {
    sendingAccessLink.value = account.username
    error.value = ''
    success.value = ''

    const response = await api.post(`/auth/accounts/${encodeURIComponent(account.username)}/access-link`)
    success.value = response.data?.message || 'Đã gửi link bảo mật đến email người dùng.'
    await loadAccounts()
  } catch (err) {
    error.value = err.response?.data?.detail ||
      err.response?.data?.message ||
      'Không gửi được link bảo mật. Hãy kiểm tra email tài khoản và cấu hình Gmail.'
    console.error(err)
  } finally {
    sendingAccessLink.value = ''
  }
}
const buildAccountPayload = (account, accountStatus) => ({
  username: account.username,
  fullName: account.fullName || account.username,
  employeeCode: account.employeeCode || '',
  email: account.email || '',
  phone: account.phone || '',
  department: account.department || '',
  jobTitle: account.jobTitle || '',
  assignedArea: account.assignedArea || '',
  accountStatus,
  permissions: [...(account.permissions || [])],
})

const toggleAccountLock = async () => {
  if (!accessTarget.value) return

  try {
    locking.value = true
    accessError.value = ''
    error.value = ''
    success.value = ''

    const account = accessTarget.value
    const nextStatus = isLocked(account) ? 'Active' : 'Locked'
    await api.put(`/auth/accounts/${encodeURIComponent(account.username)}`, buildAccountPayload(account, nextStatus))

    accessDialog.value = false
    accessTarget.value = null
    success.value = nextStatus === 'Locked'
      ? 'Đã khóa tài khoản. Hồ sơ và dữ liệu nghiệp vụ vẫn được giữ nguyên.'
      : 'Đã mở khóa tài khoản.'
    await loadAccounts()
  } catch (err) {
    accessError.value = err.response?.data?.detail ||
      err.response?.data?.message ||
      'Không cập nhật được trạng thái tài khoản.'
    console.error(err)
  } finally {
    locking.value = false
  }
}

const exportAccounts = () => {
  exportRowsToExcel({
    filename: 'danh-sach-tai-khoan.xls',
    sheetName: 'Danh sách tài khoản',
    rows: filteredAccounts.value,
    columns: [
      { header: 'Tên đăng nhập', value: (account) => account.username },
      { header: 'Vai trò', value: (account) => roleLabel(account.role) },
      { header: 'Họ tên', value: (account) => account.fullName || '-' },
      { header: 'Mã nhân viên', value: (account) => account.employeeCode || '-' },
      { header: 'Mã sinh viên', value: (account) => account.studentCode || '-' },
      { header: 'Bộ phận', value: (account) => account.department || '-' },
      { header: 'Khu vực phụ trách', value: (account) => account.assignedArea || '-' },
      { header: 'Trạng thái', value: (account) => statusLabel(account.accountStatus) },
      { header: 'Bảo mật', value: (account) => securityLabel(account) },
      { header: 'Số quyền', value: (account) => account.permissions?.length || 0 },
    ],
  })
}

const roleLabel = (role) => role === 'Staff' ? 'Nhân viên' : role === 'Student' ? 'Sinh viên' : role
const statusLabel = (status) => ({ Active: 'Đang hoạt động', Pending: 'Chờ kích hoạt', Locked: 'Tạm khóa', Inactive: 'Ngừng hoạt động' }[status || 'Active'])
const securityLabel = (account) => ({
  PendingActivation: 'Chờ kích hoạt',
  NeedsPasswordSetup: 'Cần đặt mật khẩu',
  TemporarilyLocked: 'Tạm khóa đăng nhập',
  PasswordSet: 'Đã thiết lập',
}[account.securityState] || (account.passwordConfigured ? 'Đã thiết lập' : 'Cần đặt mật khẩu'))
const securityIcon = (account) => ({
  PendingActivation: 'mdi-email-clock-outline',
  NeedsPasswordSetup: 'mdi-lock-alert-outline',
  TemporarilyLocked: 'mdi-lock-clock-outline',
  PasswordSet: 'mdi-shield-check-outline',
}[account.securityState] || 'mdi-shield-key-outline')
onMounted(loadAccounts)
</script>

<style scoped>
.account-page { display: grid; gap: 18px; } .page-head, .head-actions { display: flex; justify-content: space-between; align-items: flex-start; gap: 12px; } .page-head h2 { margin: 4px 0 6px; font-size: 30px; } .page-head p { margin: 0; color: var(--muted); }
.account-metrics { display: grid; grid-template-columns: repeat(4, 1fr); gap: 14px; } .account-metric { display: grid; grid-template-columns: 46px 1fr; gap: 14px; align-items: center; padding: 18px; border: 1px solid var(--line); border-radius: 8px; background: #fff; } .account-metric > .mdi { display: grid; place-items: center; width: 46px; height: 46px; border-radius: 8px; background: #fff3e8; color: #ea580c; font-size: 24px; } .account-metric strong, .account-metric small { display: block; } .account-metric strong { font-size: 28px; } .account-metric small { color: var(--muted); }
.panel { padding: 18px; border: 1px solid var(--line); border-radius: 8px; background: #fff; } .toolbar-row { display: grid; grid-template-columns: minmax(260px, 1fr) 180px 190px auto; gap: 10px; align-items: center; margin-bottom: 16px; } .account-table { border: 1px solid #e8ece9; border-radius: 6px; } .cell-stack strong, .cell-stack small, .permission-summary small { display: block; } .cell-stack small, .permission-summary small, .muted { margin-top: 3px; color: var(--muted); font-size: 12px; }
.role-pill, .status-pill { display: inline-flex; padding: 5px 9px; border-radius: 999px; font-size: 12px; font-weight: 800; } .role-pill.staff { background: #fff3e8; color: #c2410c; } .role-pill.student { background: #fff7ed; color: #9a3412; } .status-pill.active { background: #f6ffed; color: #389e0d; } .status-pill.pending { background: #fffbe6; color: #d48806; } .status-pill.locked, .status-pill.inactive { background: #fff1f0; color: #cf1322; }
.security-state { display: inline-flex; align-items: center; gap: 6px; font-weight: 800; color: #c2410c; } .security-state .mdi { font-size: 18px; } .action-row { display: flex; justify-content: flex-end; gap: 4px; } .form-grid { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 8px 14px; } .permission-panel { margin-top: 12px; padding-top: 18px; border-top: 1px solid var(--line); } .permission-panel p { margin: 4px 0 14px; color: var(--muted); } .permission-grid { display: grid; grid-template-columns: repeat(2, 1fr); gap: 8px; } .permission-item { display: grid; grid-template-columns: 42px 1fr; align-items: start; padding: 8px; border: 1px solid #f1dfd0; border-radius: 6px; } .permission-item strong, .permission-item small { display: block; } .permission-item small { margin-top: 3px; color: var(--muted); }
@media (max-width: 900px) { .account-metrics { grid-template-columns: repeat(2, 1fr); } .toolbar-row { grid-template-columns: 1fr; } } @media (max-width: 640px) { .page-head, .head-actions { flex-direction: column; } .account-metrics, .form-grid, .permission-grid { grid-template-columns: 1fr; } }
</style>

<style>
.account-page,
.account-page .account-metrics,
.account-page .toolbar-row,
.account-page .panel {
  min-width: 0;
}

.account-page .account-table {
  overflow-x: auto;
}

@media (max-width: 640px) {
  .account-page .head-actions,
  .account-page .head-actions .v-btn {
    width: 100%;
  }
}

.app-shell.dark-shell .account-page {
  color: #f8fafc;
}

.app-shell.dark-shell .account-page .page-head p,
.app-shell.dark-shell .account-page .cell-stack small,
.app-shell.dark-shell .account-page .permission-summary small,
.app-shell.dark-shell .account-page .muted,
.app-shell.dark-shell .account-page .permission-item small {
  color: #cbd8ea !important;
}

.app-shell.dark-shell .account-page .account-metric,
.app-shell.dark-shell .account-page .panel {
  border-color: rgba(255, 255, 255, 0.16) !important;
  background: #1f2937 !important;
  color: #f8fafc !important;
  box-shadow: 0 18px 36px rgba(0, 0, 0, 0.22);
}

.app-shell.dark-shell .account-page .account-metric > .mdi {
  background: rgba(255, 122, 26, 0.18) !important;
  color: #ffc04d !important;
}

.app-shell.dark-shell .account-page .account-metric strong,
.app-shell.dark-shell .account-page .cell-stack strong {
  color: #ffffff !important;
}

.app-shell.dark-shell .account-page .account-metric small {
  color: #dbe7f5 !important;
}

.app-shell.dark-shell .account-page .account-table,
.app-shell.dark-shell .account-page .v-table,
.app-shell.dark-shell .account-page .v-data-table {
  border-color: rgba(255, 255, 255, 0.18) !important;
  background: #1f2937 !important;
  color: #f8fafc !important;
}

.app-shell.dark-shell .account-page .v-table thead th,
.app-shell.dark-shell .account-page .account-table thead th {
  background: #263244 !important;
  color: #ffffff !important;
}

.app-shell.dark-shell .account-page .v-table tbody td,
.app-shell.dark-shell .account-page .account-table tbody td {
  border-color: rgba(255, 255, 255, 0.12) !important;
  color: #f8fafc !important;
}

.app-shell.dark-shell .account-page .v-table tbody tr:hover {
  background: rgba(255, 122, 26, 0.12) !important;
}

.app-shell.dark-shell .account-page .security-state {
  color: #ffb45c !important;
}

.app-shell.dark-shell .account-page .role-pill.staff,
.app-shell.dark-shell .account-page .role-pill.student {
  background: rgba(255, 122, 26, 0.16) !important;
  color: #ffc04d !important;
}

.app-shell.dark-shell .account-page .status-pill.active {
  background: rgba(74, 222, 128, 0.18) !important;
  color: #bbf7d0 !important;
}

.app-shell.dark-shell .account-page .status-pill.pending {
  background: rgba(250, 204, 21, 0.18) !important;
  color: #fde68a !important;
}

.app-shell.dark-shell .account-page .status-pill.locked,
.app-shell.dark-shell .account-page .status-pill.inactive {
  background: rgba(248, 113, 113, 0.18) !important;
  color: #fecaca !important;
}

.app-shell.dark-shell .account-page .permission-item {
  border-color: rgba(255, 255, 255, 0.14) !important;
  background: rgba(15, 23, 42, 0.32);
}
</style>
