<template>
  <section class="employee-dashboard">
    <div class="page-head">
      <div>
        <span class="page-kicker">Không gian nhân viên</span>
        <h2>Công việc của tôi</h2>
        <p>{{ department || 'Bộ phận vận hành' }} · {{ assignedArea || 'Chưa phân khu vực' }}</p>
      </div>
      <v-btn color="primary" prepend-icon="mdi-refresh" :loading="loading" @click="loadData">Làm mới</v-btn>
    </div>

    <v-alert v-if="error" type="error" variant="tonal">{{ error }}</v-alert>

    <div class="metric-grid">
      <article v-for="metric in metrics" :key="metric.label" class="metric-card">
        <span :class="['mdi', metric.icon]"></span>
        <div><strong>{{ metric.value }}</strong><small>{{ metric.label }}</small></div>
      </article>
    </div>

    <div class="dashboard-grid">
      <section class="panel">
        <div class="panel-title">
          <div><h3>Yêu cầu được giao</h3><p>Ưu tiên công việc quá hạn và mức khẩn cấp.</p></div>
          <router-link to="/finance/incidents">Xem tất cả</router-link>
        </div>
        <div v-if="myIncidents.length === 0" class="empty-state">Chưa có yêu cầu nào được giao cho bạn.</div>
        <button v-for="incident in myIncidents.slice(0, 6)" :key="incident.id" class="work-row" @click="$router.push('/finance/incidents')">
          <span :class="['priority-dot', incident.priority]"></span>
          <div>
            <strong>{{ incident.building }}-{{ incident.roomName }} · {{ categoryLabel(incident.category) }}</strong>
            <small>{{ incident.studentName }} · {{ statusLabel(incident.status) }}</small>
          </div>
          <time>{{ dueLabel(incident.dueAt) }}</time>
        </button>
      </section>

      <section class="panel">
        <div class="panel-title">
          <div><h3>Lịch bảo trì</h3><p>Công việc định kỳ sắp đến hạn.</p></div>
          <router-link to="/finance/incidents">Mở lịch</router-link>
        </div>
        <div v-if="myMaintenance.length === 0" class="empty-state">Chưa có lịch bảo trì được giao.</div>
        <div v-for="plan in myMaintenance.slice(0, 6)" :key="plan.id" class="maintenance-row">
          <span class="mdi mdi-calendar-wrench-outline"></span>
          <div><strong>{{ plan.title }}</strong><small>{{ plan.location }} · {{ plan.assetName }}</small></div>
          <time>{{ formatDate(plan.nextDueDate) }}</time>
        </div>
      </section>
    </div>
  </section>
</template>

<script setup>
import { computed, onMounted, ref } from 'vue'
import api from '@/services/api'

const loading = ref(false)
const error = ref('')
const incidents = ref([])
const maintenance = ref([])
const username = localStorage.getItem('username') || ''
const department = localStorage.getItem('employee_department') || ''
const assignedArea = localStorage.getItem('employee_area') || ''

const normalizeList = (payload) => Array.isArray(payload) ? payload : payload?.data || []
const myIncidents = computed(() => incidents.value.filter((item) => item.assignedTo === username))
const myMaintenance = computed(() => maintenance.value.filter((item) => item.assignedTo === username))
const metrics = computed(() => [
  { label: 'Chưa tiếp nhận', value: incidents.value.filter((item) => item.status === 'new').length, icon: 'mdi-inbox-arrow-down-outline' },
  { label: 'Được giao cho tôi', value: myIncidents.value.filter((item) => !['confirmed', 'rejected', 'cancelled'].includes(item.status)).length, icon: 'mdi-account-hard-hat-outline' },
  { label: 'Đang xử lý', value: myIncidents.value.filter((item) => ['processing', 'waiting-materials', 'reopened'].includes(item.status)).length, icon: 'mdi-tools' },
  { label: 'Bảo trì sắp hạn', value: myMaintenance.value.filter((item) => new Date(item.nextDueDate) <= new Date(Date.now() + 7 * 86400000)).length, icon: 'mdi-calendar-alert-outline' },
])

const loadData = async () => {
  try {
    loading.value = true
    error.value = ''
    const [incidentResponse, maintenanceResponse] = await Promise.all([api.get('/incidents'), api.get('/maintenance')])
    incidents.value = normalizeList(incidentResponse.data)
    maintenance.value = normalizeList(maintenanceResponse.data)
  } catch (err) {
    error.value = 'Không tải được công việc nhân viên.'
    console.error(err)
  } finally { loading.value = false }
}

const statusLabel = (status) => ({ new: 'Mới gửi', assigned: 'Đã phân công', accepted: 'Đã tiếp nhận', processing: 'Đang xử lý', 'waiting-materials': 'Chờ vật tư', completed: 'Chờ sinh viên xác nhận', confirmed: 'Đã xác nhận', reopened: 'Cần xử lý lại' }[status] || status)
const categoryLabel = (category) => ({ Electric: 'Điện', Water: 'Nước', Internet: 'Internet', Furniture: 'Nội thất', Safety: 'An toàn' }[category] || 'Khác')
const formatDate = (value) => value ? new Intl.DateTimeFormat('vi-VN').format(new Date(value)) : 'Chưa đặt hạn'
const dueLabel = (value) => value && new Date(value) < new Date() ? 'Quá hạn' : formatDate(value)

onMounted(loadData)
</script>

<style scoped>
.employee-dashboard { display: grid; gap: 18px; }
.page-head, .panel-title { display: flex; justify-content: space-between; align-items: flex-start; gap: 18px; }
.page-head h2 { margin: 4px 0 6px; font-size: 30px; }
.page-head p, .panel-title p { margin: 0; color: var(--muted); }
.metric-grid { display: grid; grid-template-columns: repeat(4, minmax(0, 1fr)); gap: 14px; }
.metric-card { display: grid; grid-template-columns: 46px 1fr; gap: 14px; align-items: center; min-height: 96px; padding: 18px; border: 1px solid var(--line); border-radius: 8px; background: #fff; }
.metric-card > .mdi { display: grid; place-items: center; width: 46px; height: 46px; border-radius: 8px; background: #fff3e8; color: #f36f21; font-size: 24px; }
.metric-card strong, .metric-card small { display: block; } .metric-card strong { font-size: 28px; } .metric-card small { margin-top: 4px; color: var(--muted); }
.dashboard-grid { display: grid; grid-template-columns: 1.1fr .9fr; gap: 18px; }
.panel { padding: 20px; border: 1px solid var(--line); border-radius: 8px; background: #fff; }
.panel-title { margin-bottom: 16px; } .panel-title h3 { margin: 0 0 5px; } .panel-title a { color: #c2410c; font-weight: 800; text-decoration: none; }
.work-row, .maintenance-row { display: grid; grid-template-columns: 12px minmax(0, 1fr) auto; gap: 12px; align-items: center; width: 100%; min-height: 62px; padding: 10px 0; border: 0; border-bottom: 1px solid #edf0ee; background: transparent; text-align: left; }
.work-row { cursor: pointer; } .work-row:hover { background: #fff7ed; }
.work-row strong, .work-row small, .maintenance-row strong, .maintenance-row small { display: block; } .work-row small, .maintenance-row small, time { margin-top: 4px; color: var(--muted); font-size: 12px; }
.priority-dot { width: 9px; height: 9px; border-radius: 50%; background: #8c8c8c; } .priority-dot.high { background: #fa8c16; } .priority-dot.urgent { background: #f5222d; }
.maintenance-row { grid-template-columns: 30px minmax(0, 1fr) auto; } .maintenance-row > .mdi { color: #f36f21; font-size: 22px; }
.empty-state { padding: 34px 12px; color: var(--muted); text-align: center; }
@media (max-width: 1000px) { .metric-grid { grid-template-columns: repeat(2, 1fr); } .dashboard-grid { grid-template-columns: 1fr; } }
@media (max-width: 600px) { .page-head { flex-direction: column; } .metric-grid { grid-template-columns: 1fr; } }
</style>
