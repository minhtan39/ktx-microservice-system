<template>
  <section class="account-page">
    <div class="page-head">
      <div>
        <span class="page-kicker">AuthService</span>
        <h2>Quan ly tai khoan</h2>
        <p>Admin xem va cap nhat username, mat khau cua nhan vien va sinh vien.</p>
      </div>

      <v-btn color="success" prepend-icon="mdi-refresh" :loading="loading" @click="loadAccounts">
        Lam moi
      </v-btn>
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
          label="Tim tai khoan"
          prepend-inner-icon="mdi-magnify"
          density="comfortable"
          hide-details
        />

        <v-select
          v-model="roleFilter"
          :items="roleOptions"
          label="Vai tro"
          density="comfortable"
          hide-details
        />
      </div>

      <div class="table-wrap">
        <table>
          <thead>
            <tr>
              <th>Tai khoan</th>
              <th>Mat khau</th>
              <th>Vai tro</th>
              <th>Ho ten</th>
              <th>Ma sinh vien</th>
              <th>Thao tac</th>
            </tr>
          </thead>
          <tbody>
            <tr v-if="filteredAccounts.length === 0">
              <td colspan="6" class="empty-cell">Khong co tai khoan phu hop.</td>
            </tr>
            <tr v-for="account in filteredAccounts" :key="`${account.role}-${account.username}`">
              <td><strong>{{ account.username }}</strong></td>
              <td><code>{{ account.password }}</code></td>
              <td>
                <span class="role-pill" :class="roleClass(account.role)">
                  {{ account.role }}
                </span>
              </td>
              <td>{{ account.fullName }}</td>
              <td>{{ account.studentCode || '-' }}</td>
              <td>
                <v-btn
                  color="success"
                  variant="tonal"
                  size="small"
                  prepend-icon="mdi-pencil-outline"
                  @click="openEdit(account)"
                >
                  Sua
                </v-btn>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </section>

    <v-dialog v-model="dialog" max-width="520">
      <v-card v-if="editing">
        <v-card-title>Cap nhat tai khoan</v-card-title>
        <v-card-text>
          <v-form class="edit-form" @submit.prevent="saveAccount">
            <v-text-field
              v-model="form.username"
              label="Tai khoan"
              density="comfortable"
            />

            <v-text-field
              v-model="form.password"
              label="Mat khau"
              density="comfortable"
              :type="showPassword ? 'text' : 'password'"
              :append-inner-icon="showPassword ? 'mdi-eye-off-outline' : 'mdi-eye-outline'"
              @click:append-inner="showPassword = !showPassword"
            />

            <v-text-field
              v-model="form.fullName"
              label="Ho ten"
              density="comfortable"
            />

            <v-text-field
              :model-value="editing.role"
              label="Vai tro"
              readonly
              density="comfortable"
            />

            <v-alert v-if="editing.role === 'Student'" type="info" variant="tonal" density="compact">
              Doi username sinh vien se doi tai khoan dang nhap; ho so N2 van duoc map bang studentId.
            </v-alert>
          </v-form>
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="dialog = false">Huy</v-btn>
          <v-btn color="success" :loading="saving" @click="saveAccount">Luu</v-btn>
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
const error = ref('')
const success = ref('')
const accounts = ref([])
const search = ref('')
const roleFilter = ref('All')
const dialog = ref(false)
const editing = ref(null)
const showPassword = ref(false)

const roleOptions = ['All', 'Staff', 'Student']

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
    error.value = 'Khong tai duoc danh sach tai khoan tu AuthService.'
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

const saveAccount = async () => {
  if (!editing.value) return

  if (!form.username.trim() || !form.password.trim()) {
    error.value = 'Tai khoan va mat khau khong duoc de trong.'
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
    success.value = 'Da cap nhat tai khoan.'
    await loadAccounts()
  } catch (err) {
    if (err.response?.status === 409) {
      error.value = 'Tai khoan moi da ton tai.'
    } else {
      error.value = 'Khong cap nhat duoc tai khoan.'
    }
    console.error(err)
  } finally {
    saving.value = false
  }
}

const roleClass = (role) => ({
  'role-staff': role === 'Staff',
  'role-student': role === 'Student',
})

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
  color: var(--brand-dark);
  font-size: 28px;
}

.page-head p {
  margin: 0;
  color: var(--muted);
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
}
</style>
