<template>
  <section class="account-page">
    <div class="page-head">
      <div>
        <span class="page-kicker">AuthService</span>
        <h2>Quản lý tài khoản</h2>
        <p>Admin xem và cập nhật tên đăng nhập, mật khẩu của nhân viên và sinh viên.</p>
      </div>

      <v-btn color="success" prepend-icon="mdi-refresh" :loading="loading" @click="loadAccounts">
        Làm mới
      </v-btn>
    </div>

    <div class="account-metrics">
      <article v-for="metric in accountMetrics" :key="metric.label" class="account-metric" :class="metric.tone">
        <span :class="['mdi', metric.icon]"></span>
        <div>
          <strong>{{ metric.value }}</strong>
          <small>{{ metric.label }}</small>
        </div>
      </article>
    </div>

    <v-alert v-if="error" type="error" variant="tonal" class="mb-4">
      {{ error }}
    </v-alert>

    <v-alert v-if="success" type="success" variant="tonal" class="mb-4">
      {{ success }}
    </v-alert>

    <v-progress-linear v-if="loading" indeterminate color="success" class="mb-4" />

    <section class="panel">
      <div class="toolbar-row">
        <v-text-field
          v-model="search"
          label="Tìm tài khoản"
          prepend-inner-icon="mdi-magnify"
          density="comfortable"
          hide-details
        />

        <v-select
          v-model="roleFilter"
          :items="roleOptions"
          item-title="title"
          item-value="value"
          label="Vai trò"
          density="comfortable"
          hide-details
        />
      </div>

      <div class="table-wrap">
        <table>
          <thead>
            <tr>
              <th>Tài khoản</th>
              <th>Mật khẩu</th>
              <th>Vai trò</th>
              <th>Họ tên</th>
              <th>Mã sinh viên</th>
              <th>Thao tác</th>
            </tr>
          </thead>
          <tbody>
            <tr v-if="filteredAccounts.length === 0">
              <td colspan="6" class="empty-cell">Không có tài khoản phù hợp.</td>
            </tr>
            <tr v-for="account in filteredAccounts" :key="`${account.role}-${account.username}`">
              <td><strong>{{ account.username }}</strong></td>
              <td><code>{{ account.password }}</code></td>
              <td>
                <span class="role-pill" :class="roleClass(account.role)">
                  {{ roleLabel(account.role) }}
                </span>
              </td>
              <td>{{ account.fullName }}</td>
              <td>{{ account.studentCode || '-' }}</td>
              <td>
                <div class="action-row">
                  <v-btn
                    color="success"
                    variant="tonal"
                    size="small"
                    prepend-icon="mdi-pencil-outline"
                    @click="openEdit(account)"
                  >
                    Sửa
                  </v-btn>
                  <v-btn
                    color="error"
                    variant="tonal"
                    size="small"
                    prepend-icon="mdi-delete-outline"
                    @click="openDelete(account)"
                  >
                    Xóa
                  </v-btn>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </section>

    <v-dialog v-model="dialog" max-width="520">
      <v-card v-if="editing">
        <v-card-title>Cập nhật tài khoản</v-card-title>
        <v-card-text>
          <v-form class="edit-form" @submit.prevent="saveAccount">
            <v-text-field
              v-model="form.username"
              label="Tên đăng nhập"
              density="comfortable"
            />

            <v-text-field
              v-model="form.password"
              label="Mật khẩu"
              density="comfortable"
              :type="showPassword ? 'text' : 'password'"
              :append-inner-icon="showPassword ? 'mdi-eye-off-outline' : 'mdi-eye-outline'"
              @click:append-inner="showPassword = !showPassword"
            />

            <v-text-field
              v-model="form.fullName"
              label="Họ tên"
              density="comfortable"
            />

            <v-text-field
              :model-value="editing.role"
              label="Vai trò"
              readonly
              density="comfortable"
            />

            <v-alert v-if="editing.role === 'Student'" type="info" variant="tonal" density="compact">
              Đổi tên đăng nhập sinh viên sẽ không làm thay đổi mã sinh viên; hồ sơ N2 vẫn được liên kết bằng studentId.
            </v-alert>
          </v-form>
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="dialog = false">Hủy</v-btn>
          <v-btn color="success" :loading="saving" @click="saveAccount">Lưu</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <v-dialog v-model="deleteDialog" max-width="480">
      <v-card v-if="deleteTarget">
        <v-card-title>Xác nhận xóa tài khoản</v-card-title>
        <v-card-text>
          <p>
            Bạn sắp xóa tài khoản <strong>{{ deleteTarget.username }}</strong>.
          </p>
          <v-alert
            v-if="deleteTarget.role === 'Student'"
            type="warning"
            variant="tonal"
            density="comfortable"
          >
            Tài khoản này thuộc sinh viên. Hồ sơ sinh viên tương ứng cũng sẽ bị xóa.
          </v-alert>
          <v-alert v-else type="info" variant="tonal" density="comfortable">
            Tài khoản nhân viên sẽ bị xóa khỏi AuthService.
          </v-alert>
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="deleteDialog = false">Hủy</v-btn>
          <v-btn color="error" :loading="deleting" @click="deleteAccount">
            Xóa tài khoản
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </section>
</template>

<script setup>
import { computed, onMounted, reactive, ref } from 'vue'
import api from '@/services/api'

const loading = ref(false)
const saving = ref(false)
const deleting = ref(false)
const error = ref('')
const success = ref('')
const accounts = ref([])
const search = ref('')
const roleFilter = ref('All')
const dialog = ref(false)
const editing = ref(null)
const deleteDialog = ref(false)
const deleteTarget = ref(null)
const showPassword = ref(false)

const roleOptions = [
  { title: 'Tất cả', value: 'All' },
  { title: 'Nhân viên', value: 'Staff' },
  { title: 'Sinh viên', value: 'Student' },
]

const form = reactive({
  username: '',
  password: '',
  fullName: '',
})

const normalizeList = (data) => {
  if (Array.isArray(data)) return data
  if (Array.isArray(data?.data)) return data.data
  if (Array.isArray(data?.items)) return data.items
  return []
}

const loadAccounts = async () => {
  try {
    loading.value = true
    error.value = ''

    const response = await api.get('/auth/accounts')
    accounts.value = normalizeList(response.data)
  } catch (err) {
    error.value = 'Không tải được danh sách tài khoản từ AuthService.'
    console.error(err)
  } finally {
    loading.value = false
  }
}

const filteredAccounts = computed(() => {
  const keyword = search.value.trim().toLowerCase()

  return accounts.value.filter((account) => {
    const matchesRole = roleFilter.value === 'All' || account.role === roleFilter.value
    const haystack = [
      account.username,
      account.password,
      account.role,
      account.fullName,
      account.studentCode,
    ].join(' ').toLowerCase()

    return matchesRole && (!keyword || haystack.includes(keyword))
  })
})

const accountMetrics = computed(() => [
  {
    icon: 'mdi-account-group-outline',
    value: accounts.value.length,
    label: 'Tổng tài khoản',
    tone: 'all',
  },
  {
    icon: 'mdi-shield-account-outline',
    value: accounts.value.filter((account) => account.role === 'Staff').length,
    label: 'Nhân viên vận hành',
    tone: 'staff',
  },
  {
    icon: 'mdi-school-outline',
    value: accounts.value.filter((account) => account.role === 'Student').length,
    label: 'Sinh viên',
    tone: 'student',
  },
  {
    icon: 'mdi-account-key-outline',
    value: accounts.value.filter((account) => account.role === 'Student' && account.studentCode).length,
    label: 'Map theo MSSV',
    tone: 'mapped',
  },
])

const openEdit = (account) => {
  editing.value = account
  form.username = account.username
  form.password = account.password
  form.fullName = account.fullName
  showPassword.value = false
  error.value = ''
  success.value = ''
  dialog.value = true
}

const openDelete = (account) => {
  deleteTarget.value = account
  error.value = ''
  success.value = ''
  deleteDialog.value = true
}

const saveAccount = async () => {
  if (!editing.value) return

  if (!form.username.trim() || !form.password.trim()) {
    error.value = 'Tên đăng nhập và mật khẩu không được để trống.'
    return
  }

  try {
    saving.value = true
    error.value = ''
    success.value = ''

    await api.put(`/auth/accounts/${encodeURIComponent(editing.value.username)}`, {
      username: form.username,
      password: form.password,
      fullName: form.fullName,
    })

    dialog.value = false
    success.value = 'Đã cập nhật tài khoản.'
    await loadAccounts()
  } catch (err) {
    if (err.response?.status === 409) {
      error.value = 'Tên đăng nhập mới đã tồn tại.'
    } else {
      error.value = 'Không cập nhật được tài khoản.'
    }
    console.error(err)
  } finally {
    saving.value = false
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

const roleClass = (role) => ({
  'role-staff': role === 'Staff',
  'role-student': role === 'Student',
})

const roleLabel = (role) => {
  if (role === 'Staff') return 'Nhân viên'
  if (role === 'Student') return 'Sinh viên'
  return role
}

onMounted(loadAccounts)
</script>

<style scoped>
.account-page {
  display: grid;
  gap: 16px;
}

.page-head {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 18px;
}

.page-head h2 {
  margin: 4px 0 6px;
  color: var(--ink);
  font-family: var(--font-heading);
  font-size: 28px;
}

.page-head p {
  margin: 0;
  color: var(--muted);
}

.account-metrics {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: 14px;
}

.account-metric {
  display: grid;
  grid-template-columns: 46px minmax(0, 1fr);
  align-items: center;
  gap: 14px;
  min-height: 92px;
  padding: 18px;
  border: 1px solid var(--line);
  border-radius: 8px;
  background: #ffffff;
}

.account-metric .mdi {
  display: grid;
  place-items: center;
  width: 46px;
  height: 46px;
  border-radius: 8px;
  background: #eef2ff;
  color: #3730a3;
  font-size: 24px;
}

.account-metric.student .mdi,
.account-metric.mapped .mdi {
  background: #eaf8f0;
  color: #0f7a44;
}

.account-metric strong,
.account-metric small {
  display: block;
}

.account-metric strong {
  color: var(--ink);
  font-family: var(--font-heading);
  font-size: 28px;
  line-height: 1;
}

.account-metric small {
  margin-top: 6px;
  color: var(--muted);
  font-size: 13px;
  font-weight: 800;
}

.panel {
  padding: 20px;
  border: 1px solid var(--line);
  border-radius: 8px;
  background: #ffffff;
}

.toolbar-row {
  display: grid;
  grid-template-columns: minmax(260px, 1fr) 180px;
  gap: 12px;
  margin-bottom: 16px;
}

.table-wrap {
  overflow-x: auto;
}

table {
  width: 100%;
  min-width: 780px;
  border-collapse: collapse;
}

th,
td {
  padding: 13px 12px;
  border-bottom: 1px solid #eef2f5;
  text-align: left;
  vertical-align: middle;
}

th {
  color: #34495e;
  font-size: 12px;
  font-weight: 900;
  letter-spacing: 0.04em;
  text-transform: uppercase;
}

td {
  color: var(--ink);
  font-size: 14px;
}

code {
  display: inline-flex;
  min-height: 28px;
  align-items: center;
  padding: 0 8px;
  border-radius: 6px;
  background: #f1f5f9;
  color: #0f172a;
  font-weight: 800;
}

.role-pill {
  display: inline-flex;
  align-items: center;
  min-height: 28px;
  padding: 0 10px;
  border-radius: 8px;
  background: #f1f5f9;
  color: #475569;
  font-size: 12px;
  font-weight: 900;
}

.role-staff {
  background: #eef2ff;
  color: #3730a3;
}

.role-student {
  background: #eaf8f0;
  color: #0f7a44;
}

.empty-cell {
  padding: 24px 12px;
  color: var(--muted);
  text-align: center;
}

.action-row {
  display: flex;
  gap: 8px;
  align-items: center;
}

.edit-form {
  display: grid;
  gap: 10px;
}

@media (max-width: 760px) {
  .page-head,
  .toolbar-row {
    display: grid;
    grid-template-columns: 1fr;
  }

  .account-metrics {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
}

@media (max-width: 560px) {
  .account-metrics {
    grid-template-columns: 1fr;
  }
}
</style>
