<template>
  <section class="incident-page">
    <div class="page-head">
      <div>
        <span class="page-kicker">Billing & Maintenance</span>
        <h2>Quản lý yêu cầu sửa chữa</h2>
        <p>Sinh viên gửi yêu cầu từ cổng sinh viên, nhân viên/admin tiếp nhận và cập nhật tiến độ xử lý.</p>
      </div>

      <v-btn color="success" prepend-icon="mdi-refresh" :loading="loading" @click="loadIncidents">
        Làm mới
      </v-btn>
    </div>

    <v-alert v-if="error" type="error" variant="tonal" class="mb-4">
      {{ error }}
    </v-alert>

    <v-alert v-if="success" type="success" variant="tonal" class="mb-4">
      {{ success }}
    </v-alert>

    <v-progress-linear v-if="loading" indeterminate color="success" class="mb-4" />

    <v-tabs v-model="activeTab" bg-color="white" color="success" grow class="rounded elevation-1 mb-4">
      <v-tab v-for="tab in statusTabs" :key="tab.value" :value="tab.value">
        {{ tab.label }} ({{ countIncidents(tab.value) }})
      </v-tab>
    </v-tabs>

    <v-window v-model="activeTab">
      <v-window-item v-for="tab in statusTabs" :key="tab.value" :value="tab.value">
        <v-row>
          <v-col v-if="getIncidentsByStatus(tab.value).length === 0" cols="12">
            <v-alert type="info" variant="tonal">
              Không có yêu cầu sửa chữa nào ở trạng thái này.
            </v-alert>
          </v-col>

          <v-col
            v-for="incident in getIncidentsByStatus(tab.value)"
            :key="incident.id"
            cols="12"
            md="6"
          >
            <v-card class="incident-card" variant="outlined">
              <v-card-item>
                <div class="card-title-row">
                  <div>
                    <v-card-title>Phòng {{ incident.roomName }} - Tòa {{ incident.building }}</v-card-title>
                    <v-card-subtitle>
                      {{ incident.studentName }} · {{ incident.studentCode }} · {{ formatDateTime(incident.createdAt) }}
                    </v-card-subtitle>
                  </div>

                  <v-chip size="small" :color="getCategoryColor(incident.category)">
                    {{ incident.category }}
                  </v-chip>
                </div>
              </v-card-item>

              <v-card-text>
                <div class="description-box">
                  <strong>Mô tả:</strong>
                  <p>{{ incident.description }}</p>
                </div>

                <div v-if="incident.staffNote || incident.handledBy" class="note-box">
                  <strong>Xử lý:</strong>
                  <p>{{ incident.staffNote || 'Đã cập nhật trạng thái' }}</p>
                  <small v-if="incident.handledBy">Nguoi xu ly: {{ incident.handledBy }}</small>
                </div>
              </v-card-text>

              <v-card-actions class="justify-end">
                <v-btn
                  v-if="tab.value === 'new'"
                  color="warning"
                  variant="elevated"
                  prepend-icon="mdi-wrench"
                  @click="updateStatus(incident.id, 'processing')"
                >
                  Tiep nhan
                </v-btn>

                <v-btn
                  v-if="tab.value === 'new'"
                  color="error"
                  variant="tonal"
                  prepend-icon="mdi-close-circle-outline"
                  @click="updateStatus(incident.id, 'rejected')"
                >
                  Tu choi
                </v-btn>

                <v-btn
                  v-if="tab.value === 'processing'"
                  color="success"
                  variant="elevated"
                  prepend-icon="mdi-check-circle-outline"
                  @click="updateStatus(incident.id, 'done')"
                >
                  Hoan thanh
                </v-btn>

                <span v-if="tab.value === 'done'" class="status-done">
                  <span class="mdi mdi-check-all"></span>
                  Da sua xong
                </span>

                <span v-if="tab.value === 'rejected'" class="status-rejected">
                  <span class="mdi mdi-close-circle-outline"></span>
                  Da tu choi
                </span>
              </v-card-actions>
            </v-card>
          </v-col>
        </v-row>
      </v-window-item>
    </v-window>
  </section>
</template>

<script setup>
import { onMounted, ref } from 'vue'
import api from '@/services/api'

const activeTab = ref('new')
const loading = ref(false)
const error = ref('')
const success = ref('')
const incidents = ref([])

const statusTabs = [
  { value: 'new', label: 'Moi tiep nhan' },
  { value: 'processing', label: 'Dang xu ly' },
  { value: 'done', label: 'Hoan thanh' },
  { value: 'rejected', label: 'Tu choi' },
]

const normalizeList = (data) => {
  if (Array.isArray(data)) return data
  if (Array.isArray(data?.data)) return data.data
  if (Array.isArray(data?.items)) return data.items
  return []
}

const loadIncidents = async () => {
  try {
    loading.value = true
    error.value = ''

    const response = await api.get('/incidents')
    incidents.value = normalizeList(response.data)
  } catch (err) {
    error.value = 'Không tải được danh sách yêu cầu sửa chữa từ BillingService.'
    console.error(err)
  } finally {
    loading.value = false
  }
}

const getIncidentsByStatus = (status) => {
  return incidents.value.filter((incident) => incident.status === status)
}

const countIncidents = (status) => {
  return getIncidentsByStatus(status).length
}

const getCategoryColor = (category) => {
  if (category === 'Electric') return 'orange-darken-2'
  if (category === 'Water') return 'blue'
  if (category === 'Internet') return 'indigo'
  if (category === 'Furniture') return 'brown'
  return 'grey-darken-1'
}

const updateStatus = async (id, status) => {
  const staffNote = window.prompt('Ghi chú xử lý', '')

  try {
    error.value = ''
    success.value = ''

    await api.patch(`/incidents/${id}/status`, {
      status,
      handledBy: localStorage.getItem('username') || localStorage.getItem('fullName') || 'staff',
      staffNote,
    })

    success.value = 'Đã cập nhật trạng thái yêu cầu sửa chữa.'
    await loadIncidents()
  } catch (err) {
    error.value = 'Không cập nhật được trạng thái yêu cầu sửa chữa.'
    console.error(err)
  }
}

const formatDateTime = (value) => {
  if (!value) return '-'
  return new Intl.DateTimeFormat('vi-VN', {
    dateStyle: 'short',
    timeStyle: 'short',
  }).format(new Date(value))
}

onMounted(loadIncidents)
</script>

<style scoped>
.incident-page {
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

.incident-card {
  height: 100%;
  border-radius: 8px;
  background: #ffffff;
}

.card-title-row {
  display: flex;
  justify-content: space-between;
  gap: 16px;
}

.description-box,
.note-box {
  padding: 12px;
  border-radius: 8px;
  background: #f8fafc;
}

.description-box p,
.note-box p {
  margin: 6px 0 0;
  color: var(--ink);
  line-height: 1.55;
}

.note-box {
  margin-top: 10px;
  background: #f0fdf4;
}

.note-box small {
  display: block;
  margin-top: 6px;
  color: var(--muted);
}

.status-done,
.status-rejected {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  font-size: 13px;
  font-weight: 900;
}

.status-done {
  color: #0f7a44;
}

.status-rejected {
  color: #b91c1c;
}

@media (max-width: 760px) {
  .page-head {
    display: grid;
  }
}
</style>
