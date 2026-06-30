<template>
  <div class="audit-page">
    <div class="page-head">
      <div>
        <strong>NHẬT KÝ HỆ THỐNG</strong>
        <h2>Trung tâm audit vận hành</h2>
        <p>Theo dõi ai đã thao tác, thao tác ở module nào, kết quả ra sao và dữ liệu liên quan.</p>
      </div>
      <div class="head-actions">
        <button class="ghost-button" type="button" @click="exportCsv">
          <span class="mdi mdi-file-delimited-outline"></span>
          Xuất CSV
        </button>
        <button class="primary-button" type="button" :disabled="loading" @click="fetchLogs">
          <span class="mdi mdi-refresh"></span>
          Làm mới
        </button>
      </div>
    </div>

    <div v-if="errorMessage" class="inline-alert error">
      <span class="mdi mdi-alert-circle-outline"></span>
      <span>{{ errorMessage }}</span>
      <button type="button" @click="errorMessage = ''">Đóng</button>
    </div>

    <section class="metric-grid">
      <article class="metric-card">
        <span class="mdi mdi-shield-search-outline"></span>
        <div>
          <small>Tổng nhật ký</small>
          <strong>{{ statistics.total || 0 }}</strong>
        </div>
      </article>
      <article class="metric-card">
        <span class="mdi mdi-calendar-today-outline"></span>
        <div>
          <small>Hôm nay</small>
          <strong>{{ statistics.today || 0 }}</strong>
        </div>
      </article>
      <article class="metric-card">
        <span class="mdi mdi-alert-octagon-outline"></span>
        <div>
          <small>Lỗi / thất bại</small>
          <strong>{{ statistics.failures || 0 }}</strong>
        </div>
      </article>
      <article class="metric-card">
        <span class="mdi mdi-lock-alert-outline"></span>
        <div>
          <small>Hành động nhạy cảm</small>
          <strong>{{ statistics.sensitive || 0 }}</strong>
        </div>
      </article>
    </section>

    <section class="filter-panel">
      <label>
        <span>Từ khóa người thực hiện</span>
        <input v-model.trim="filters.actor" type="search" placeholder="Ví dụ: admin, nv001, mã sinh viên" @keyup.enter="fetchLogs" />
      </label>

      <label>
        <span>Module</span>
        <select v-model="filters.module">
          <option value="">Tất cả module</option>
          <option v-for="item in moduleOptions" :key="item" :value="item">{{ item }}</option>
        </select>
      </label>

      <label>
        <span>Trạng thái</span>
        <select v-model="filters.status">
          <option value="">Tất cả trạng thái</option>
          <option value="Success">Thành công</option>
          <option value="Failed">Thất bại</option>
          <option value="Warning">Cảnh báo</option>
        </select>
      </label>

      <label>
        <span>Vai trò</span>
        <select v-model="filters.role">
          <option value="">Tất cả vai trò</option>
          <option value="Admin">Admin</option>
          <option value="Staff">Nhân viên</option>
          <option value="Student">Sinh viên</option>
          <option value="System">Hệ thống</option>
        </select>
      </label>

      <label>
        <span>Từ ngày</span>
        <input v-model="filters.from" type="date" />
      </label>

      <label>
        <span>Đến ngày</span>
        <input v-model="filters.to" type="date" />
      </label>

      <label>
        <span>Số dòng</span>
        <select v-model.number="filters.take">
          <option :value="100">100 dòng</option>
          <option :value="250">250 dòng</option>
          <option :value="500">500 dòng</option>
          <option :value="1000">1000 dòng</option>
        </select>
      </label>

      <button class="primary-button filter-submit" type="button" :disabled="loading" @click="fetchLogs">
        <span class="mdi mdi-filter-check-outline"></span>
        Áp dụng lọc
      </button>
    </section>

    <section class="audit-table-card">
      <div class="table-head">
        <div>
          <strong>Danh sách nhật ký</strong>
          <span>{{ logs.length }} bản ghi gần nhất</span>
        </div>
        <div v-if="statistics.topActor" class="top-actor">
          <span class="mdi mdi-account-star-outline"></span>
          {{ statistics.topActor.name }} · {{ statistics.topActor.count }} thao tác
        </div>
      </div>

      <div class="table-scroll">
        <table class="data-table compact-table">
          <thead>
            <tr>
              <th>Thời gian</th>
              <th>Người thực hiện</th>
              <th>Module</th>
              <th>Hành động</th>
              <th>Đối tượng</th>
              <th>Trạng thái</th>
              <th>Chi tiết</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            <tr v-if="!loading && logs.length === 0">
              <td colspan="8" class="empty-cell">Chưa có nhật ký phù hợp với bộ lọc.</td>
            </tr>
            <tr v-if="loading">
              <td colspan="8" class="empty-cell">Đang tải nhật ký...</td>
            </tr>
            <tr v-for="log in logs" :key="log.id">
              <td>
                <span class="cell-title">{{ formatDate(log.createdAt) }}</span>
                <span class="cell-subtitle">#{{ log.id }}</span>
              </td>
              <td>
                <span class="cell-title">{{ log.actorName || log.actorId || 'Không rõ' }}</span>
                <span class="role-pill">{{ roleLabel(log.actorRole) }}</span>
              </td>
              <td>{{ log.module || 'Hệ thống' }}</td>
              <td><strong>{{ log.action || 'Thao tác' }}</strong></td>
              <td>
                <span class="cell-title">{{ log.targetName || log.targetType || '-' }}</span>
                <span v-if="log.targetId" class="cell-subtitle">{{ log.targetType }} #{{ log.targetId }}</span>
              </td>
              <td>
                <span :class="['status-pill', statusClass(log.status)]">{{ statusLabel(log.status) }}</span>
              </td>
              <td class="description-cell">{{ log.description }}</td>
              <td class="action-cell">
                <button class="text-button" type="button" @click="selectedLog = log">Xem</button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </section>

    <v-dialog v-model="detailDialog" max-width="760">
      <v-card v-if="selectedLog" class="detail-dialog">
        <div class="dialog-head">
          <div>
            <span class="page-kicker">CHI TIẾT NHẬT KÝ</span>
            <h3>{{ selectedLog.action }}</h3>
            <p>{{ formatDate(selectedLog.createdAt) }}</p>
          </div>
          <v-btn icon="mdi-close" variant="text" @click="selectedLog = null" />
        </div>

        <div class="detail-grid">
          <div><span>Người thực hiện</span><strong>{{ selectedLog.actorName }}</strong></div>
          <div><span>Vai trò</span><strong>{{ roleLabel(selectedLog.actorRole) }}</strong></div>
          <div><span>Module</span><strong>{{ selectedLog.module }}</strong></div>
          <div><span>Trạng thái</span><strong>{{ statusLabel(selectedLog.status) }}</strong></div>
          <div><span>Đối tượng</span><strong>{{ selectedLog.targetName || '-' }}</strong></div>
          <div><span>IP</span><strong>{{ selectedLog.ipAddress || '-' }}</strong></div>
        </div>

        <section class="detail-section">
          <strong>Mô tả</strong>
          <p>{{ selectedLog.description || 'Không có mô tả.' }}</p>
        </section>

        <section class="detail-section">
          <strong>Metadata</strong>
          <pre>{{ prettyMetadata(selectedLog.metadataJson) }}</pre>
        </section>
      </v-card>
    </v-dialog>
  </div>
</template>

<script setup>
import { computed, onMounted, reactive, ref } from 'vue'
import api from '@/services/api'

const logs = ref([])
const statistics = ref({})
const loading = ref(false)
const errorMessage = ref('')
const selectedLog = ref(null)
const detailDialog = computed({
  get: () => Boolean(selectedLog.value),
  set: (value) => {
    if (!value) selectedLog.value = null
  },
})
const filters = reactive({
  actor: '',
  module: '',
  status: '',
  role: '',
  from: '',
  to: '',
  take: 250,
})

const moduleOptions = computed(() => {
  const fromStats = (statistics.value.modules || []).map((item) => item.module).filter(Boolean)
  const fromRows = logs.value.map((item) => item.module).filter(Boolean)
  return [...new Set([...fromStats, ...fromRows])].sort((a, b) => a.localeCompare(b, 'vi'))
})

const fetchLogs = async () => {
  try {
    loading.value = true
    errorMessage.value = ''
    const params = new URLSearchParams()
    Object.entries(filters).forEach(([key, value]) => {
      if (value !== '' && value !== null && value !== undefined) params.set(key, value)
    })

    const [logResponse, statResponse] = await Promise.all([
      api.get(`/system/logs?${params.toString()}`),
      api.get('/system/logs/statistics'),
    ])

    logs.value = Array.isArray(logResponse.data) ? logResponse.data : []
    statistics.value = statResponse.data || {}
  } catch (error) {
    errorMessage.value = error.response?.data?.message ||
      'Không tải được nhật ký hệ thống. Vui lòng kiểm tra quyền admin hoặc Gateway/BillingService.'
  } finally {
    loading.value = false
  }
}

const formatDate = (value) => {
  if (!value) return '-'
  return new Date(value).toLocaleString('vi-VN', {
    hour12: false,
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
  })
}

const statusLabel = (status) => ({
  Success: 'Thành công',
  Failed: 'Thất bại',
  Warning: 'Cảnh báo',
}[status] || status || 'Không rõ')

const statusClass = (status) => ({
  Success: 'success',
  Failed: 'failed',
  Warning: 'warning',
}[status] || 'neutral')

const roleLabel = (role) => ({
  Admin: 'Admin',
  Staff: 'Nhân viên',
  Student: 'Sinh viên',
  System: 'Hệ thống',
  User: 'Người dùng',
}[role] || role || 'Không rõ')

const prettyMetadata = (value) => {
  if (!value) return 'Không có metadata.'
  try {
    return JSON.stringify(JSON.parse(value), null, 2)
  } catch {
    return value
  }
}

const exportCsv = () => {
  const header = ['Thời gian', 'Người thực hiện', 'Vai trò', 'Module', 'Hành động', 'Đối tượng', 'Trạng thái', 'Chi tiết']
  const rows = logs.value.map((item) => [
    formatDate(item.createdAt),
    item.actorName,
    roleLabel(item.actorRole),
    item.module,
    item.action,
    item.targetName || item.targetId,
    statusLabel(item.status),
    item.description,
  ])
  const csv = [header, ...rows]
    .map((row) => row.map((cell) => `"${String(cell || '').replaceAll('"', '""')}"`).join(','))
    .join('\n')
  const blob = new Blob([`\ufeff${csv}`], { type: 'text/csv;charset=utf-8;' })
  const url = URL.createObjectURL(blob)
  const link = document.createElement('a')
  link.href = url
  link.download = `nhat-ky-he-thong-${new Date().toISOString().slice(0, 10)}.csv`
  link.click()
  URL.revokeObjectURL(url)
}

onMounted(fetchLogs)
</script>

<style scoped>
.audit-page { display: grid; gap: 18px; }
.page-head, .head-actions, .table-head { display: flex; align-items: flex-start; justify-content: space-between; gap: 16px; }
.page-head strong { color: var(--brand); font-size: 12px; font-weight: 900; }
.page-head h2 { margin: 4px 0 6px; font-size: 30px; }
.page-head p { margin: 0; color: var(--muted); }
.head-actions { flex-wrap: wrap; }
.primary-button, .ghost-button, .text-button {
  display: inline-flex; align-items: center; justify-content: center; gap: 8px;
  min-height: 42px; padding: 0 14px; border-radius: 8px; font: inherit; font-weight: 900; cursor: pointer;
}
.primary-button { border: 1px solid var(--brand); background: var(--brand); color: #fff; }
.ghost-button { border: 1px solid var(--line-strong); background: #fff; color: var(--brand-dark); }
.text-button { min-height: 34px; border: 0; background: transparent; color: var(--brand-dark); }
.primary-button:disabled { opacity: .65; cursor: wait; }
.inline-alert { display: flex; align-items: center; gap: 12px; padding: 14px 16px; border-radius: 8px; font-weight: 800; }
.inline-alert.error { background: #fde7e9; color: #b00020; }
.inline-alert button { margin-left: auto; border: 0; background: transparent; color: inherit; font: inherit; font-weight: 900; cursor: pointer; }
.metric-grid { display: grid; grid-template-columns: repeat(4, minmax(0, 1fr)); gap: 14px; }
.metric-card { display: grid; grid-template-columns: 48px minmax(0, 1fr); gap: 14px; align-items: center; padding: 18px; border: 1px solid var(--line); border-radius: 8px; background: #fff; }
.metric-card > .mdi { display: grid; place-items: center; width: 48px; height: 48px; border-radius: 8px; background: var(--brand-soft); color: var(--brand); font-size: 25px; }
.metric-card small, .metric-card strong { display: block; }
.metric-card small { color: var(--muted); }
.metric-card strong { font-size: 28px; }
.filter-panel { display: grid; grid-template-columns: minmax(220px, 1.4fr) repeat(6, minmax(140px, 1fr)) auto; gap: 12px; align-items: end; padding: 18px; border: 1px solid var(--line); border-radius: 8px; background: #fff; }
.filter-panel label { display: grid; gap: 6px; min-width: 0; }
.filter-panel span { color: var(--muted); font-size: 12px; font-weight: 800; }
.filter-panel input, .filter-panel select { width: 100%; min-height: 42px; padding: 0 12px; border: 1px solid var(--line-strong); border-radius: 8px; background: #fff; color: var(--ink); font: inherit; }
.filter-submit { white-space: nowrap; }
.audit-table-card { border: 1px solid var(--line); border-radius: 8px; background: #fff; overflow: hidden; }
.table-head { padding: 16px 18px; border-bottom: 1px solid var(--line); }
.table-head strong, .table-head span { display: block; }
.table-head span { margin-top: 4px; color: var(--muted); font-size: 13px; }
.top-actor { display: inline-flex; align-items: center; gap: 8px; padding: 8px 10px; border-radius: 8px; background: var(--brand-soft); color: var(--brand-dark); font-weight: 900; }
.table-scroll { overflow-x: auto; }
.description-cell { max-width: 320px; color: var(--muted-strong); }
.action-cell { text-align: right; }
.empty-cell { padding: 34px !important; color: var(--muted); text-align: center; }
.role-pill, .status-pill { display: inline-flex; width: fit-content; margin-top: 4px; padding: 5px 9px; border-radius: 999px; font-size: 12px; font-weight: 800; }
.role-pill { background: #fff3e8; color: #c2410c; }
.status-pill.success { background: #dcfce7; color: #166534; }
.status-pill.failed { background: #fee2e2; color: #b91c1c; }
.status-pill.warning { background: #fff7e6; color: #b45309; }
.status-pill.neutral { background: #f5f5f5; color: #595959; }
.detail-dialog { padding: 22px; }
.dialog-head { display: flex; justify-content: space-between; gap: 14px; }
.dialog-head h3 { margin: 4px 0 6px; }
.dialog-head p { margin: 0; color: var(--muted); }
.detail-grid { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 10px; margin-top: 18px; }
.detail-grid div, .detail-section { padding: 14px; border: 1px solid var(--line); border-radius: 8px; background: var(--surface-soft); }
.detail-grid span, .detail-grid strong { display: block; }
.detail-grid span { color: var(--muted); font-size: 12px; }
.detail-section { margin-top: 12px; }
.detail-section p { margin: 8px 0 0; color: var(--muted-strong); line-height: 1.55; }
.detail-section pre { margin: 8px 0 0; padding: 12px; border-radius: 8px; background: #1f1713; color: #ffedd5; overflow: auto; white-space: pre-wrap; }
@media (max-width: 1200px) { .filter-panel { grid-template-columns: repeat(3, minmax(0, 1fr)); } .filter-submit { grid-column: 1 / -1; } }
@media (max-width: 760px) { .page-head, .head-actions, .table-head { flex-direction: column; } .metric-grid, .filter-panel, .detail-grid { grid-template-columns: 1fr; } .head-actions, .primary-button, .ghost-button { width: 100%; } }
</style>
