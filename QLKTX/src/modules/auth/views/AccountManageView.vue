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
            <v-btn icon="mdi-delete-outline" variant="text" color="error" size="small" title="Xóa tài khoản" @click="openDelete(item)" />
          </div>
        </template>
      </v-data-table>
    </section>

    <v-dialog v-model="dialog" max-width="820">
      <v-card>
        <v-card-title>{{ editing ? 'Cập nhật tài khoản' : 'Tạo nhân viên vận hành' }}</v-card-title>
        <v-card-text>
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

    <v-dialog v-model="deleteDialog" max-width="480">
      <v-card v-if="deleteTarget">
        <v-card-title>Xác nhận xóa tài khoản</v-card-title>
        <v-card-text>
          <p>Bạn sắp xóa tài khoản <strong>{{ deleteTarget.username }}</strong>.</p>
          <v-alert :type="deleteTarget.role === 'Student' ? 'warning' : 'info'" variant="tonal">
            {{ deleteTarget.role === 'Student' ? 'Hồ sơ sinh viên tương ứng cũng sẽ bị xóa.' : 'Nên chuyển nhân viên nghỉ việc sang trạng thái Ngừng hoạt động để giữ lịch sử xử lý.' }}
          </v-alert>
        </v-card-text>
        <v-card-actions><v-spacer /><v-btn variant="text" @click="deleteDialog = false">Hủy</v-btn><v-btn color="error" :loading="deleting" @click="deleteAccount">Xóa tài khoản</v-btn></v-card-actions>
      </v-card>
    </v-dialog>
  </section>
</template>

<script setup>
import { computed, onMounted, reactive, ref } from 'vue'
import api from '@/services/api'
import { exportRowsToExcel } from '@/utils/exportExcel'

const loading = ref(false), saving = ref(false), deleting = ref(false), sendingAccessLink = ref('')
const error = ref(''), success = ref(''), accounts = ref([]), search = ref('')
const roleFilter = ref('All'), statusFilter = ref('All'), dialog = ref(false), deleteDialog = ref(false)
const editing = ref(null), deleteTarget = ref(null)

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

const loadAccounts = async () => { try { loading.value = true; error.value = ''; accounts.value = normalizeList((await api.get('/auth/accounts')).data) } catch (err) { error.value = 'Không tải được danh sách tài khoản.'; console.error(err) } finally { loading.value = false } }
const filteredAccounts = computed(() => { const keyword = search.value.trim().toLowerCase(); return accounts.value.filter((item) => (roleFilter.value === 'All' || item.role === roleFilter.value) && (statusFilter.value === 'All' || (item.accountStatus || 'Active') === statusFilter.value) && (!keyword || [item.username, item.fullName, item.employeeCode, item.studentCode, item.department].join(' ').toLowerCase().includes(keyword))) })
const accountMetrics = computed(() => [
  { icon: 'mdi-account-group-outline', value: accounts.value.length, label: 'Tổng tài khoản' },
  { icon: 'mdi-account-hard-hat-outline', value: accounts.value.filter((item) => item.role === 'Staff').length, label: 'Nhân viên vận hành' },
  { icon: 'mdi-account-check-outline', value: accounts.value.filter((item) => (item.accountStatus || 'Active') === 'Active').length, label: 'Đang hoạt động' },
  { icon: 'mdi-school-outline', value: accounts.value.filter((item) => item.role === 'Student').length, label: 'Sinh viên' },
])

const assignForm = (value) => Object.assign(form, emptyForm(), value, { permissions: [...(value?.permissions || emptyForm().permissions)] })
const openCreate = () => { editing.value = null; assignForm(null); error.value = ''; dialog.value = true }
const openEdit = (account) => { editing.value = account; assignForm(account); error.value = ''; dialog.value = true }
const openDelete = (account) => { deleteTarget.value = account; deleteDialog.value = true }

const saveAccount = async () => {
  if (!form.username.trim() || !form.fullName.trim() || (isStaffForm.value && (!form.employeeCode.trim() || !form.email.trim()))) { error.value = 'Vui lòng nhập đủ tên đăng nhập, họ tên, mã nhân viên và email.'; return }
  try {
    saving.value = true; error.value = ''; success.value = ''
    const payload = { ...form, permissions: [...form.permissions] }
    if (editing.value) await api.put(`/auth/accounts/${encodeURIComponent(editing.value.username)}`, payload)
    else await api.post('/auth/accounts', payload)
    dialog.value = false; success.value = editing.value ? 'Đã cập nhật tài khoản.' : 'Đã tạo nhân viên. Hãy gửi link kích hoạt qua email.'; await loadAccounts()
  } catch (err) { error.value = err.response?.data?.message || 'Không lưu được tài khoản.'; console.error(err) } finally { saving.value = false }
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
const deleteAccount = async () => {
  if (!deleteTarget.value) return

  try {
    deleting.value = true
    error.value = ''
    success.value = ''

    const account = deleteTarget.value
    await api.delete(`/auth/accounts/${encodeURIComponent(account.username)}`)

    deleteDialog.value = false
    deleteTarget.value = null
    success.value = account.role === 'Student'
      ? 'Đã xóa tài khoản và hồ sơ sinh viên.'
      : 'Đã xóa tài khoản nhân viên.'
    await loadAccounts()
  } catch (err) {
    error.value = err.response?.data?.detail || 'Không xóa được tài khoản.'
    console.error(err)
  } finally {
    deleting.value = false
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
.account-metrics { display: grid; grid-template-columns: repeat(4, 1fr); gap: 14px; } .account-metric { display: grid; grid-template-columns: 46px 1fr; gap: 14px; align-items: center; padding: 18px; border: 1px solid var(--line); border-radius: 8px; background: #fff; } .account-metric > .mdi { display: grid; place-items: center; width: 46px; height: 46px; border-radius: 8px; background: #e6f4ff; color: #1677ff; font-size: 24px; } .account-metric strong, .account-metric small { display: block; } .account-metric strong { font-size: 28px; } .account-metric small { color: var(--muted); }
.panel { padding: 18px; border: 1px solid var(--line); border-radius: 8px; background: #fff; } .toolbar-row { display: grid; grid-template-columns: minmax(260px, 1fr) 180px 190px auto; gap: 10px; align-items: center; margin-bottom: 16px; } .account-table { border: 1px solid #e8ece9; border-radius: 6px; } .cell-stack strong, .cell-stack small, .permission-summary small { display: block; } .cell-stack small, .permission-summary small, .muted { margin-top: 3px; color: var(--muted); font-size: 12px; }
.role-pill, .status-pill { display: inline-flex; padding: 5px 9px; border-radius: 999px; font-size: 12px; font-weight: 800; } .role-pill.staff { background: #e6f4ff; color: #0958d9; } .role-pill.student { background: #f6ffed; color: #389e0d; } .status-pill.active { background: #f6ffed; color: #389e0d; } .status-pill.pending { background: #fffbe6; color: #d48806; } .status-pill.locked, .status-pill.inactive { background: #fff1f0; color: #cf1322; }
.security-state { display: inline-flex; align-items: center; gap: 6px; font-weight: 800; color: #0f7f51; } .security-state .mdi { font-size: 18px; } .action-row { display: flex; justify-content: flex-end; gap: 4px; } .form-grid { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 8px 14px; } .permission-panel { margin-top: 12px; padding-top: 18px; border-top: 1px solid var(--line); } .permission-panel p { margin: 4px 0 14px; color: var(--muted); } .permission-grid { display: grid; grid-template-columns: repeat(2, 1fr); gap: 8px; } .permission-item { display: grid; grid-template-columns: 42px 1fr; align-items: start; padding: 8px; border: 1px solid #e8ece9; border-radius: 6px; } .permission-item strong, .permission-item small { display: block; } .permission-item small { margin-top: 3px; color: var(--muted); }
@media (max-width: 900px) { .account-metrics { grid-template-columns: repeat(2, 1fr); } .toolbar-row { grid-template-columns: 1fr; } } @media (max-width: 640px) { .page-head, .head-actions { flex-direction: column; } .account-metrics, .form-grid, .permission-grid { grid-template-columns: 1fr; } }
</style>
