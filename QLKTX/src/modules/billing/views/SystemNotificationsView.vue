<template>
  <div class="notifications-page">
    <section class="page-head">
      <div>
        <strong>THÔNG BÁO HỆ THỐNG</strong>
        <h2>Trung tâm thông báo</h2>
        <p>Admin tạo thông báo cho toàn hệ thống, sinh viên, nhân viên hoặc nhóm quản trị.</p>
      </div>
      <v-btn color="primary" prepend-icon="mdi-bell-plus-outline" @click="openCreate">
        Tạo thông báo
      </v-btn>
    </section>

    <v-alert v-if="error" type="error" variant="tonal" closable @click:close="error = ''">
      {{ error }}
    </v-alert>
    <v-alert v-if="success" type="success" variant="tonal" closable @click:close="success = ''">
      {{ success }}
    </v-alert>

    <section class="metric-grid">
      <div v-for="metric in metrics" :key="metric.label" class="metric-card">
        <span :class="['mdi', metric.icon]"></span>
        <div>
          <small>{{ metric.label }}</small>
          <strong>{{ metric.value }}</strong>
        </div>
      </div>
    </section>

    <section class="panel">
      <div class="toolbar-grid">
        <v-text-field
          v-model="search"
          prepend-inner-icon="mdi-magnify"
          label="Tìm tiêu đề, nội dung"
          density="compact"
          variant="outlined"
          hide-details
        />
        <v-select
          v-model="audienceFilter"
          :items="audienceFilterOptions"
          item-title="title"
          item-value="value"
          label="Đối tượng"
          density="compact"
          variant="outlined"
          hide-details
        />
        <v-select
          v-model="statusFilter"
          :items="statusFilterOptions"
          item-title="title"
          item-value="value"
          label="Trạng thái"
          density="compact"
          variant="outlined"
          hide-details
        />
        <v-btn variant="outlined" prepend-icon="mdi-refresh" :loading="loading" @click="loadNotifications">
          Làm mới
        </v-btn>
      </div>

      <v-data-table
        :headers="headers"
        :items="filteredNotifications"
        :loading="loading"
        item-value="id"
        :items-per-page="10"
        class="notifications-table"
        no-data-text="Chưa có thông báo phù hợp."
      >
        <template #item.title="{ item }">
          <div class="cell-stack">
            <strong>{{ item.title }}</strong>
            <small>{{ item.content }}</small>
            <div v-if="item.attachments?.length" class="attachment-list table-attachments">
              <v-btn
                v-for="attachment in item.attachments"
                :key="attachment.id"
                size="x-small"
                variant="tonal"
                prepend-icon="mdi-paperclip"
                @click.stop="downloadAttachment(item, attachment)"
              >
                {{ attachment.fileName }}
              </v-btn>
            </div>
          </div>
        </template>
        <template #item.targetAudience="{ item }">
          {{ audienceLabel(item.targetAudience) }}
        </template>
        <template #item.severity="{ item }">
          <span :class="['pill', severityClass(item.severity)]">{{ severityLabel(item.severity) }}</span>
        </template>
        <template #item.status="{ item }">
          <span :class="['pill', statusClass(item.status)]">{{ statusLabel(item.status) }}</span>
        </template>
        <template #item.attachments="{ item }">
          <strong>{{ item.attachments?.length || 0 }}</strong>
        </template>
        <template #item.createdAt="{ item }">
          <div class="cell-stack">
            <strong>{{ formatDateTime(item.createdAt) }}</strong>
            <small>{{ item.createdBy }}</small>
          </div>
        </template>
        <template #item.readCount="{ item }">
          <strong>{{ item.readCount || 0 }}</strong>
        </template>
        <template #item.actions="{ item }">
          <div class="action-row">
            <v-btn
              v-if="item.status !== 'Published'"
              size="small"
              variant="tonal"
              color="success"
              :loading="savingId === item.id"
              @click="setStatus(item, 'Published')"
            >
              Gửi
            </v-btn>
            <v-btn
              v-if="item.status !== 'Expired'"
              size="small"
              variant="text"
              color="error"
              :loading="savingId === item.id"
              @click="setStatus(item, 'Expired')"
            >
              Ẩn
            </v-btn>
          </div>
        </template>
      </v-data-table>
    </section>

    <v-dialog v-model="dialog" max-width="720">
      <v-card>
        <v-card-title>Tạo thông báo hệ thống</v-card-title>
        <v-card-text>
          <v-alert v-if="dialogError" type="error" variant="tonal" closable class="mb-4" @click:close="dialogError = ''">
            {{ dialogError }}
          </v-alert>
          <div class="form-grid">
            <v-text-field v-model="form.title" label="Tiêu đề" variant="outlined" density="compact" />
            <v-select
              v-model="form.targetAudience"
              :items="audienceOptions"
              item-title="title"
              item-value="value"
              label="Đối tượng nhận"
              variant="outlined"
              density="compact"
            />
            <v-select
              v-model="form.severity"
              :items="severityOptions"
              item-title="title"
              item-value="value"
              label="Mức độ"
              variant="outlined"
              density="compact"
            />
            <v-text-field v-model="form.expiresAt" type="datetime-local" label="Hết hiệu lực" variant="outlined" density="compact" />
            <v-textarea v-model="form.content" label="Nội dung" rows="5" variant="outlined" class="full-width" />
            <v-file-input
              v-model="form.attachments"
              class="full-width"
              label="Tệp đính kèm"
              prepend-icon="mdi-paperclip"
              variant="outlined"
              density="compact"
              multiple
              show-size
              counter
              clearable
              hint="Tối đa 5 tệp, mỗi tệp 5 MB"
              persistent-hint
            />
            <v-checkbox v-model="form.publishNow" label="Gửi ngay sau khi tạo" density="compact" hide-details />
          </div>
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="dialog = false">Hủy</v-btn>
          <v-btn color="primary" :loading="saving" @click="createNotification">Lưu thông báo</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </div>
</template>

<script setup>
import { computed, onMounted, reactive, ref } from 'vue'
import api from '@/services/api'

const loading = ref(false)
const saving = ref(false)
const savingId = ref(null)
const dialog = ref(false)
const error = ref('')
const success = ref('')
const dialogError = ref('')
const notifications = ref([])
const search = ref('')
const audienceFilter = ref('All')
const statusFilter = ref('All')

const blankForm = () => ({
  title: '',
  content: '',
  targetAudience: 'All',
  severity: 'Normal',
  expiresAt: '',
  publishNow: true,
  attachments: [],
})
const form = reactive(blankForm())

const audienceOptions = [
  { title: 'Tất cả người dùng', value: 'All' },
  { title: 'Sinh viên', value: 'Student' },
  { title: 'Nhân viên', value: 'Staff' },
  { title: 'Admin', value: 'Admin' },
]
const audienceFilterOptions = [{ title: 'Tất cả đối tượng', value: 'All' }, ...audienceOptions.slice(1)]
const severityOptions = [
  { title: 'Thông thường', value: 'Normal' },
  { title: 'Quan trọng', value: 'Important' },
  { title: 'Khẩn cấp', value: 'Urgent' },
]
const statusFilterOptions = [
  { title: 'Tất cả trạng thái', value: 'All' },
  { title: 'Nháp', value: 'Draft' },
  { title: 'Đã gửi', value: 'Published' },
  { title: 'Đã ẩn', value: 'Expired' },
]
const headers = [
  { title: 'Thông báo', key: 'title', sortable: false },
  { title: 'Đối tượng', key: 'targetAudience', sortable: false },
  { title: 'Mức độ', key: 'severity', sortable: false },
  { title: 'Trạng thái', key: 'status', sortable: false },
  { title: 'Tệp', key: 'attachments', sortable: false },
  { title: 'Đã đọc', key: 'readCount', sortable: false },
  { title: 'Tạo lúc', key: 'createdAt', sortable: false },
  { title: '', key: 'actions', sortable: false },
]

const metrics = computed(() => [
  { label: 'Tổng thông báo', value: notifications.value.length, icon: 'mdi-bell-outline' },
  { label: 'Đang gửi', value: notifications.value.filter((item) => item.status === 'Published').length, icon: 'mdi-send-check-outline' },
  { label: 'Nháp', value: notifications.value.filter((item) => item.status === 'Draft').length, icon: 'mdi-file-outline' },
  { label: 'Lượt đã đọc', value: notifications.value.reduce((sum, item) => sum + Number(item.readCount || 0), 0), icon: 'mdi-eye-check-outline' },
])

const filteredNotifications = computed(() => {
  const keyword = search.value.trim().toLowerCase()
  return notifications.value.filter((item) =>
    (audienceFilter.value === 'All' || item.targetAudience === audienceFilter.value) &&
    (statusFilter.value === 'All' || item.status === statusFilter.value) &&
    (!keyword || [item.title, item.content, item.createdBy].join(' ').toLowerCase().includes(keyword)))
})

const loadNotifications = async () => {
  try {
    loading.value = true
    error.value = ''
    const response = await api.get('/notifications/manage')
    notifications.value = normalizeList(response.data)
  } catch (err) {
    error.value = err.response?.data?.message || 'Không tải được danh sách thông báo.'
  } finally {
    loading.value = false
  }
}

const openCreate = () => {
  Object.assign(form, blankForm())
  dialogError.value = ''
  error.value = ''
  dialog.value = true
}

const createNotification = async () => {
  if (!form.title.trim() || !form.content.trim()) {
    dialogError.value = 'Vui lòng nhập tiêu đề và nội dung thông báo.'
    return
  }

  try {
    saving.value = true
    dialogError.value = ''
    const payload = buildNotificationPayload()
    await api.post('/notifications', payload, {
      headers: { 'Content-Type': 'multipart/form-data' },
    })
    dialog.value = false
    success.value = form.publishNow ? 'Đã gửi thông báo hệ thống.' : 'Đã lưu bản nháp thông báo.'
    await loadNotifications()
  } catch (err) {
    dialogError.value = err.response?.data?.message || 'Không tạo được thông báo.'
  } finally {
    saving.value = false
  }
}

const buildNotificationPayload = () => {
  const payload = new FormData()
  payload.append('title', form.title.trim())
  payload.append('content', form.content.trim())
  payload.append('targetAudience', form.targetAudience)
  payload.append('severity', form.severity)
  payload.append('publishNow', String(form.publishNow))

  if (form.expiresAt) {
    payload.append('expiresAt', new Date(form.expiresAt).toISOString())
  }

  const files = Array.isArray(form.attachments)
    ? form.attachments
    : form.attachments
      ? [form.attachments]
      : []

  files.forEach((file) => {
    payload.append('attachments', file)
  })

  return payload
}

const setStatus = async (item, status) => {
  try {
    savingId.value = item.id
    error.value = ''
    await api.patch(`/notifications/${item.id}/status`, { status })
    success.value = status === 'Published' ? 'Đã gửi thông báo.' : 'Đã ẩn thông báo.'
    await loadNotifications()
  } catch (err) {
    error.value = err.response?.data?.message || 'Không cập nhật được trạng thái thông báo.'
  } finally {
    savingId.value = null
  }
}

const downloadAttachment = async (notification, attachment) => {
  try {
    error.value = ''
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
  } catch (err) {
    error.value = err.response?.data?.message || 'Không tải được tệp đính kèm.'
  }
}

const normalizeList = (payload) => Array.isArray(payload) ? payload : payload?.data || []
const audienceLabel = (value) => audienceOptions.find((item) => item.value === value)?.title || 'Tất cả người dùng'
const severityLabel = (value) => severityOptions.find((item) => item.value === value)?.title || 'Thông thường'
const statusLabel = (value) => statusFilterOptions.find((item) => item.value === value)?.title || value
const severityClass = (value) => `severity-${String(value || 'Normal').toLowerCase()}`
const statusClass = (value) => `status-${String(value || 'Draft').toLowerCase()}`
const formatDateTime = (value) => value
  ? new Intl.DateTimeFormat('vi-VN', { dateStyle: 'short', timeStyle: 'short' }).format(new Date(value))
  : '-'

onMounted(loadNotifications)
</script>

<style scoped>
.notifications-page { display: grid; gap: 18px; min-width: 0; max-width: 100%; }
.page-head { display: flex; align-items: flex-start; justify-content: space-between; gap: 18px; }
.page-head strong { color: #c2410c; font-size: 12px; letter-spacing: 0; }
.page-head h2 { margin: 4px 0 6px; font-size: 30px; }
.page-head p { margin: 0; color: var(--muted); }
.metric-grid { display: grid; grid-template-columns: repeat(4, minmax(0, 1fr)); gap: 14px; }
.metric-card { display: grid; grid-template-columns: 46px minmax(0, 1fr); gap: 14px; align-items: center; padding: 18px; border: 1px solid var(--line); border-radius: 8px; background: #fff; }
.metric-card > .mdi { display: grid; place-items: center; width: 46px; height: 46px; border-radius: 8px; background: #fff3e8; color: #f36f21; font-size: 24px; }
.metric-card small, .metric-card strong { display: block; }
.metric-card small { color: var(--muted); }
.metric-card strong { font-size: 28px; }
.panel { padding: 18px; border: 1px solid var(--line); border-radius: 8px; background: #fff; }
.toolbar-grid { display: grid; grid-template-columns: minmax(260px, 1fr) 180px 180px auto; gap: 10px; margin-bottom: 16px; }
.notifications-table { border: 1px solid #e8ece9; border-radius: 6px; }
:deep(.notifications-table .v-table__wrapper) { overflow-x: auto; }
.cell-stack { max-width: 440px; }
.cell-stack strong, .cell-stack small { display: block; }
.cell-stack small { margin-top: 4px; color: var(--muted); overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.attachment-list { display: flex; flex-wrap: wrap; gap: 6px; margin-top: 8px; }
.table-attachments { max-width: 420px; }
.pill { display: inline-flex; padding: 5px 9px; border-radius: 999px; font-size: 12px; font-weight: 800; white-space: nowrap; }
.severity-normal, .status-draft { background: #f5f5f5; color: #595959; }
.severity-important, .status-published { background: #fff3e8; color: #c2410c; }
.severity-urgent, .status-expired { background: #fff1f0; color: #cf1322; }
.action-row { display: flex; align-items: center; justify-content: flex-end; gap: 6px; }
.form-grid { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 12px; }
.full-width { grid-column: 1 / -1; }
@media (max-width: 1000px) {
  .metric-grid { grid-template-columns: repeat(2, minmax(0, 1fr)); }
  .toolbar-grid { grid-template-columns: minmax(0, 1fr) minmax(0, 1fr); }
}
@media (max-width: 720px) {
  .page-head { flex-direction: column; }
  .metric-grid, .toolbar-grid, .form-grid { grid-template-columns: minmax(0, 1fr); }
}
</style>
