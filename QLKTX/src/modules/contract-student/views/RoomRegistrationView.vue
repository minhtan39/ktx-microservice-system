<template>
  <section>
    <div class="page-heading">
      <div>
        <span class="page-kicker">Room Registration</span>
        <h2>Duyệt đăng ký phòng</h2>
      </div>
      <v-btn color="primary" variant="flat" prepend-icon="mdi-refresh" @click="loadAll">Làm mới</v-btn>
    </div>

    <v-card class="pa-5 mb-4 form-panel">
      <div class="panel-head">
        <div class="panel-icon">
          <span class="mdi mdi-clipboard-plus-outline"></span>
        </div>
        <h3 class="section-title">Tạo đơn đăng ký nội trú</h3>
      </div>
      <v-form @submit.prevent="createRegistration">
        <v-row>
          <v-col cols="12" md="3">
            <v-select
              v-model="form.studentId"
              :items="students"
              item-title="displayName"
              item-value="id"
              label="Sinh viên"
              density="compact"
              no-data-text="Chưa có hồ sơ sinh viên hợp lệ"
              required
            />
          </v-col>
          <v-col cols="12" md="2">
            <v-select v-model="form.buildingName" :items="buildings" label="Tòa" density="compact" />
          </v-col>
          <v-col cols="12" md="2">
            <v-select v-model="form.roomType" :items="roomTypes" label="Loại phòng" density="compact" />
          </v-col>
          <v-col cols="12" md="2">
            <v-select
              v-model="form.priorityType"
              :items="priorityTypes"
              item-title="title"
              item-value="value"
              label="Diện ưu tiên"
              density="compact"
            />
          </v-col>
          <v-col cols="12" md="3">
            <v-text-field v-model="form.priorityNote" label="Ghi chú ưu tiên" density="compact" />
          </v-col>
          <v-col cols="12" md="3">
            <v-text-field v-model="form.startDate" label="Ngày bắt đầu" type="date" density="compact" />
          </v-col>
          <v-col cols="12" md="3">
            <v-text-field v-model="form.endDate" label="Ngày kết thúc" type="date" density="compact" />
          </v-col>
          <v-col cols="12" md="3" class="d-flex align-center">
            <v-btn color="success" type="submit" :loading="saving">Gửi đăng ký</v-btn>
          </v-col>
        </v-row>
      </v-form>
    </v-card>

    <v-alert v-if="message" :type="messageType" variant="tonal" class="mb-4">{{ message }}</v-alert>

    <v-card class="pa-4 mb-4 filter-panel">
      <v-row>
        <v-col cols="12" md="4">
          <v-select
            v-model="statusFilter"
            :items="statusOptions"
            label="Hiển thị đơn"
            density="compact"
          />
        </v-col>
        <v-col cols="12" md="8" class="summary-row">
          <span>Chờ duyệt: {{ countByStatus('Pending') }}</span>
          <span>Đã duyệt: {{ countByStatus('Approved') }}</span>
          <span>Từ chối: {{ countByStatus('Rejected') }}</span>
        </v-col>
      </v-row>
    </v-card>

    <v-card class="table-card">
      <table class="data-table">
        <thead>
          <tr>
            <th>ID</th>
            <th>Sinh viên</th>
            <th>Nguyện vọng</th>
            <th>Ưu tiên</th>
            <th>Thời gian ở</th>
            <th>Trạng thái</th>
            <th>Phòng xếp</th>
            <th>Thao tác</th>
          </tr>
        </thead>
        <tbody>
          <tr v-if="loading">
            <td colspan="8">Đang tải dữ liệu...</td>
          </tr>
          <tr v-else-if="filteredRegistrations.length === 0">
            <td colspan="8">Không có đơn đăng ký trong nhóm đang chọn.</td>
          </tr>
          <tr v-for="registration in filteredRegistrations" :key="registration.id">
            <td>{{ registration.id }}</td>
            <td>{{ studentName(registration.studentId) }}</td>
            <td>Tòa {{ registration.buildingName || 'bất kỳ' }}<br />{{ registration.roomType || 'Bất kỳ' }}</td>
            <td>{{ priorityLabel(registration.priorityType) }}<br />Điểm: {{ registration.priorityScore }}</td>
            <td>{{ formatDate(registration.startDate) }} - {{ formatDate(registration.endDate) }}</td>
            <td>{{ registration.status }}</td>
            <td>{{ registration.assignedRoomId || 'Chưa xếp' }}</td>
            <td>
              <div class="action-row">
                <v-btn
                  color="success"
                  size="small"
                  :disabled="registration.status !== 'Pending'"
                  @click="approveRegistration(registration.id)"
                >
                  Tự xếp phòng
                </v-btn>
                <v-btn
                  color="error"
                  size="small"
                  variant="tonal"
                  :disabled="registration.status !== 'Pending'"
                  @click="rejectRegistration(registration.id)"
                >
                  Từ chối
                </v-btn>
              </div>
            </td>
          </tr>
        </tbody>
      </table>
    </v-card>
  </section>
</template>

<script setup>
import { computed, ref, onMounted } from 'vue'
import api from '@/services/api'
import {
  buildStudentNameMap,
  cleanStudents,
  normalizeList,
} from '../utils/studentDisplay'

const loading = ref(false)
const saving = ref(false)
const message = ref('')
const messageType = ref('success')
const students = ref([])
const registrations = ref([])
const statusFilter = ref('Pending')

const statusOptions = [
  { title: 'Chỉ đơn chờ duyệt', value: 'Pending' },
  { title: 'Đã duyệt', value: 'Approved' },
  { title: 'Đã từ chối', value: 'Rejected' },
  { title: 'Tất cả lịch sử', value: 'All' },
]

const buildings = ['A', 'B', 'C']
const roomTypes = ['4-bed', '6-bed', '8-bed']
const priorityTypes = [
  { title: 'Không ưu tiên', value: '' },
  { title: 'Khuyết tật', value: 'disabled' },
  { title: 'Diện chính sách', value: 'policy' },
  { title: 'Hộ nghèo', value: 'poor' },
  { title: 'Nhà xa', value: 'far' },
  { title: 'Thành tích tốt', value: 'excellent' },
]

const today = new Date().toISOString().slice(0, 10)
const nextYear = new Date()
nextYear.setFullYear(nextYear.getFullYear() + 1)

const emptyForm = () => ({
  studentId: null,
  buildingName: 'A',
  roomType: '4-bed',
  priorityType: '',
  priorityNote: '',
  startDate: today,
  endDate: nextYear.toISOString().slice(0, 10),
})

const form = ref(emptyForm())

const studentMap = computed(() => {
  return buildStudentNameMap(students.value)
})

const filteredRegistrations = computed(() => {
  if (statusFilter.value === 'All') return registrations.value

  return registrations.value.filter((registration) => registration.status === statusFilter.value)
})

const showMessage = (text, type = 'success') => {
  message.value = text
  messageType.value = type
}

const loadStudents = async () => {
  const res = await api.get('/students')
  students.value = cleanStudents(res.data)
}

const loadRegistrations = async () => {
  const res = await api.get('/registrations')
  registrations.value = normalizeList(res.data)
}

const loadAll = async () => {
  try {
    loading.value = true
    await Promise.all([loadStudents(), loadRegistrations()])
  } catch (err) {
    showMessage('Không tải được dữ liệu đăng ký phòng.', 'error')
    console.error(err)
  } finally {
    loading.value = false
  }
}

const createRegistration = async () => {
  try {
    saving.value = true
    await api.post('/registrations', form.value)
    form.value = emptyForm()
    showMessage('Đã tạo đơn đăng ký phòng.')
    await loadRegistrations()
  } catch (err) {
    showMessage('Không tạo được đơn đăng ký. Kiểm tra sinh viên và ngày ở.', 'error')
    console.error(err)
  } finally {
    saving.value = false
  }
}

const approveRegistration = async (id) => {
  try {
    await api.put(`/registrations/${id}/approve`)
    showMessage('Đã duyệt đơn, tự xếp phòng và tạo hợp đồng.')
    await loadAll()
  } catch (err) {
    showMessage('Không duyệt được đơn. Có thể phòng phù hợp đã hết giường.', 'error')
    console.error(err)
  }
}

const rejectRegistration = async (id) => {
  try {
    await api.put(`/registrations/${id}/reject`)
    showMessage('Đã từ chối đơn đăng ký.')
    await loadRegistrations()
  } catch (err) {
    showMessage('Không từ chối được đơn đăng ký.', 'error')
    console.error(err)
  }
}

const studentName = (id) => studentMap.value.get(id) || `Sinh viên #${id}`

const priorityLabel = (value) => {
  return priorityTypes.find((item) => item.value === value)?.title || 'Không ưu tiên'
}

const countByStatus = (status) => {
  return registrations.value.filter((registration) => registration.status === status).length
}

const formatDate = (value) => {
  if (!value) return ''
  return new Date(value).toLocaleDateString('vi-VN')
}

onMounted(loadAll)
</script>

<style scoped>
.page-heading {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 20px;
}

.page-kicker {
  display: block;
  margin-bottom: 5px;
  color: var(--primary);
  font-size: 12px;
  font-weight: 900;
  letter-spacing: 0.08em;
  text-transform: uppercase;
}

.page-heading h2 {
  margin: 0;
  color: var(--ink);
  font-size: 26px;
}

.section-title {
  margin: 0;
  color: #1f5f8b;
}

.form-panel {
  background:
    linear-gradient(135deg, rgba(32, 169, 139, 0.08), transparent 40%),
    #ffffff;
}

.filter-panel {
  background:
    linear-gradient(135deg, rgba(23, 107, 135, 0.07), transparent 38%),
    #ffffff;
}

.summary-row {
  display: flex;
  align-items: center;
  justify-content: flex-end;
  gap: 18px;
  color: #40576a;
  font-weight: 700;
}

.panel-head {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-bottom: 18px;
}

.panel-icon {
  display: grid;
  place-items: center;
  width: 42px;
  height: 42px;
  border-radius: 10px;
  background: #e7f3ff;
  color: #176b87;
  font-size: 23px;
}

.table-card {
  overflow: hidden;
}

.data-table {
  width: 100%;
  border-collapse: collapse;
  background: #ffffff;
}

.data-table th,
.data-table td {
  padding: 12px 14px;
  border-bottom: 1px solid var(--line);
  text-align: left;
  vertical-align: top;
}

.data-table th {
  background: #f5f9fc;
  color: #2c3e50;
  font-weight: 700;
}

.data-table tbody tr:hover {
  background: #f8fbfd;
}

.action-row {
  display: flex;
  gap: 8px;
  flex-wrap: wrap;
}
</style>
