<template>
  <section class="student-portal">
    <div class="student-hero">
      <div>
        <span class="page-kicker">Student self-service</span>
        <h2>Xin chào, {{ displayName }}</h2>
        <p>Theo dõi hồ sơ, gửi đăng ký nội trú và xem hợp đồng ký túc xá của bạn.</p>
      </div>

      <div class="hero-stats">
        <div class="stat-tile">
          <span class="mdi mdi-clipboard-text-outline"></span>
          <strong>{{ ownRegistrations.length }}</strong>
          <small>Đơn đăng ký</small>
        </div>
        <div class="stat-tile">
          <span class="mdi mdi-file-document-check-outline"></span>
          <strong>{{ ownContracts.length }}</strong>
          <small>Hợp đồng</small>
        </div>
        <div class="stat-tile">
          <span class="mdi mdi-bed-outline"></span>
          <strong>{{ currentRoom }}</strong>
          <small>Phòng hiện tại</small>
        </div>
      </div>
    </div>

    <v-alert v-if="error" type="error" variant="tonal" class="mb-4">
      {{ error }}
    </v-alert>

    <v-alert v-if="success" type="success" variant="tonal" class="mb-4">
      {{ success }}
    </v-alert>

    <div v-if="loading" class="loading-state">
      <v-progress-circular indeterminate color="success" />
      <span>Đang tải dữ liệu sinh viên...</span>
    </div>

    <template v-else>
      <div class="portal-grid">
        <section class="panel profile-panel">
          <div class="panel-title">
            <span class="mdi mdi-account-school-outline"></span>
            <div>
              <strong>Hồ sơ sinh viên</strong>
              <small>{{ student?.studentCode || studentCode || 'Chưa có mã sinh viên' }}</small>
            </div>
          </div>

          <div v-if="student" class="profile-list">
            <div>
              <span>Họ tên</span>
              <strong>{{ student.fullName }}</strong>
            </div>
            <div>
              <span>Khoa</span>
              <strong>{{ student.facultyName || 'Chưa cập nhật' }}</strong>
            </div>
            <div>
              <span>Lớp</span>
              <strong>{{ student.className || 'Chưa cập nhật' }}</strong>
            </div>
            <div>
              <span>Liên hệ</span>
              <strong>{{ student.phone || 'Chưa cập nhật' }}</strong>
            </div>
            <div>
              <span>Trạng thái</span>
              <strong>{{ statusLabel(student.status) }}</strong>
            </div>
            <div>
              <span>Lịch sử lưu trú</span>
              <strong>{{ student.residenceHistory || 'Chưa có lịch sử lưu trú' }}</strong>
            </div>
          </div>

          <div v-else class="empty-state">
            Chưa tìm thấy hồ sơ sinh viên tương ứng với tài khoản đăng nhập.
          </div>
        </section>

        <section class="panel registration-panel">
          <div class="panel-title">
            <span class="mdi mdi-form-select"></span>
            <div>
              <strong>Đăng ký nội trú online</strong>
              <small>Đơn sẽ chuyển sang màn duyệt của admin/nhân viên</small>
            </div>
          </div>

          <v-form class="registration-form" @submit.prevent="submitRegistration">
            <v-row dense>
              <v-col cols="12" sm="6">
                <v-select
                  v-model="form.buildingName"
                  :items="buildingOptions"
                  label="Tòa mong muốn"
                  density="comfortable"
                />
              </v-col>
              <v-col cols="12" sm="6">
                <v-select
                  v-model="form.roomType"
                  :items="roomTypeOptions"
                  label="Loại phòng"
                  density="comfortable"
                />
              </v-col>
              <v-col cols="12" sm="6">
                <v-select
                  v-model="form.priorityType"
                  :items="priorityOptions"
                  item-title="label"
                  item-value="value"
                  label="Diện ưu tiên"
                  density="comfortable"
                />
              </v-col>
              <v-col cols="12" sm="6">
                <v-text-field
                  v-model="form.priorityNote"
                  label="Ghi chú ưu tiên"
                  density="comfortable"
                />
              </v-col>
              <v-col cols="12" sm="6">
                <v-text-field
                  v-model="form.startDate"
                  type="date"
                  label="Ngày bắt đầu"
                  density="comfortable"
                />
              </v-col>
              <v-col cols="12" sm="6">
                <v-text-field
                  v-model="form.endDate"
                  type="date"
                  label="Ngày kết thúc"
                  density="comfortable"
                />
              </v-col>
            </v-row>

            <div class="form-actions">
              <v-btn
                color="success"
                type="submit"
                :loading="submitting"
                :disabled="!student"
                prepend-icon="mdi-send-outline"
              >
                Gửi đăng ký
              </v-btn>
              <v-btn
                variant="tonal"
                color="success"
                prepend-icon="mdi-refresh"
                @click="loadAll"
              >
                Làm mới
              </v-btn>
            </div>
          </v-form>
        </section>
      </div>

      <section class="panel">
        <div class="panel-title">
          <span class="mdi mdi-tools"></span>
          <div>
            <strong>Yêu cầu sửa chữa</strong>
            <small>Sinh viên gửi sự cố phòng ở, nhân viên/admin tiếp nhận và cập nhật trạng thái</small>
          </div>
        </div>

        <v-form class="registration-form" @submit.prevent="submitIncident">
          <v-row dense>
            <v-col cols="12" sm="3">
              <v-text-field
                v-model="incidentForm.roomName"
                label="Phòng"
                density="comfortable"
              />
            </v-col>
            <v-col cols="12" sm="3">
              <v-select
                v-model="incidentForm.building"
                :items="buildingOptions"
                label="Tòa"
                density="comfortable"
              />
            </v-col>
            <v-col cols="12" sm="3">
              <v-select
                v-model="incidentForm.category"
                :items="incidentCategories"
                item-title="title"
                item-value="value"
                label="Loại sự cố"
                density="comfortable"
              />
            </v-col>
            <v-col cols="12" sm="3">
              <v-select
                v-model="incidentForm.priority"
                :items="incidentPriorityOptions"
                item-title="title"
                item-value="value"
                label="Mức độ ưu tiên"
                density="comfortable"
              />
            </v-col>
            <v-col cols="12" sm="6">
              <v-text-field
                v-model="incidentForm.preferredVisitAt"
                type="datetime-local"
                label="Thời gian có thể tiếp nhận"
                density="comfortable"
              />
            </v-col>
            <v-col cols="12" sm="6">
              <v-text-field
                v-model="incidentForm.contactPhone"
                label="Số điện thoại liên hệ"
                density="comfortable"
              />
            </v-col>
            <v-col cols="12">
              <v-textarea
                v-model="incidentForm.description"
                label="Mô tả chi tiết"
                rows="3"
                density="comfortable"
              />
            </v-col>
          </v-row>

          <div class="form-actions">
            <v-btn
              color="success"
              type="submit"
              :loading="incidentSubmitting"
              :disabled="!student"
              prepend-icon="mdi-send-outline"
            >
              Gửi yêu cầu sửa chữa
            </v-btn>
          </div>
        </v-form>

        <div class="table-wrap mt-4">
          <table>
            <thead>
              <tr>
                <th>Ngày gửi</th>
                <th>Phòng</th>
                <th>Mức độ</th>
                <th>Loại sự cố</th>
                <th>Mô tả</th>
                <th>Phụ trách</th>
                <th>Trạng thái</th>
                <th>Thao tác</th>
              </tr>
            </thead>
            <tbody>
              <tr v-if="ownIncidents.length === 0">
                <td colspan="8" class="empty-cell">Bạn chưa có yêu cầu sửa chữa nào.</td>
              </tr>
              <tr v-for="incident in ownIncidents" :key="incident.id">
                <td>{{ formatDate(incident.createdAt) }}</td>
                <td>{{ incident.building }}-{{ incident.roomName }}</td>
                <td><span class="status-pill" :class="`priority-${incident.priority || 'normal'}`">{{ incidentPriorityLabel(incident.priority) }}</span></td>
                <td>{{ incidentCategoryLabel(incident.category) }}</td>
                <td>{{ incident.description }}</td>
                <td>
                  <strong>{{ incident.assignedName || 'Chưa phân công' }}</strong>
                  <small class="table-note">{{ incident.dueAt ? `Hạn ${formatDateTime(incident.dueAt)}` : incident.staffNote || '-' }}</small>
                </td>
                <td>
                  <span class="status-pill" :class="incidentStatusClass(incident.status)">
                    {{ incidentStatusLabel(incident.status) }}
                  </span>
                </td>
                <td>
                  <div v-if="incident.status === 'completed'" class="incident-actions">
                    <v-btn size="small" color="success" :loading="incidentActionLoading === incident.id" @click="confirmIncident(incident)">Đã sửa đạt</v-btn>
                    <v-btn size="small" variant="tonal" color="warning" :disabled="incidentActionLoading === incident.id" @click="openReopenDialog(incident)">Chưa đạt</v-btn>
                  </div>
                  <v-btn v-else-if="incident.status === 'confirmed'" size="small" variant="text" color="warning" @click="openReopenDialog(incident)">Yêu cầu xử lý lại</v-btn>
                  <span v-else class="table-note">{{ incident.staffNote || '-' }}</span>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </section>

      <section class="panel">
        <div class="panel-title">
          <span class="mdi mdi-clipboard-list-outline"></span>
          <div>
            <strong>Đơn đăng ký của tôi</strong>
            <small>Admin/nhân viên duyệt tại mục Duyệt xếp phòng</small>
          </div>
        </div>

        <div class="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Tòa</th>
                <th>Loại phòng</th>
                <th>Ưu tiên</th>
                <th>Thời gian ở</th>
                <th>Trạng thái</th>
                <th>Phòng xếp</th>
              </tr>
            </thead>
            <tbody>
              <tr v-if="ownRegistrations.length === 0">
                <td colspan="6" class="empty-cell">Bạn chưa có đơn đăng ký nội trú.</td>
              </tr>
              <tr v-for="registration in ownRegistrations" :key="registration.id">
                <td>{{ registration.buildingName }}</td>
                <td>{{ registration.roomType }}</td>
                <td>{{ priorityLabel(registration.priorityType) }}</td>
                <td>{{ formatDate(registration.startDate) }} - {{ formatDate(registration.endDate) }}</td>
                <td>
                  <span class="status-pill" :class="statusClass(registration.status)">
                    {{ statusLabel(registration.status) }}
                  </span>
                </td>
                <td>{{ registration.assignedRoomId ? `#${registration.assignedRoomId}` : 'Chưa xếp' }}</td>
              </tr>
            </tbody>
          </table>
        </div>
      </section>

      <section class="panel">
        <div class="panel-title">
          <span class="mdi mdi-file-document-outline"></span>
          <div>
            <strong>Hợp đồng của tôi</strong>
            <small>Hợp đồng được tạo tự động sau khi đơn được duyệt</small>
          </div>
        </div>

        <div class="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Mã hợp đồng</th>
                <th>Phòng</th>
                <th>Thời hạn</th>
                <th>Tiền cọc</th>
                <th>Tiền phòng</th>
                <th>Trạng thái</th>
              </tr>
            </thead>
            <tbody>
              <tr v-if="ownContracts.length === 0">
                <td colspan="6" class="empty-cell">Chưa có hợp đồng nào được tạo.</td>
              </tr>
              <tr v-for="contract in ownContracts" :key="contract.id">
                <td>{{ contract.contractCode }}</td>
                <td>#{{ contract.roomId }}</td>
                <td>{{ formatDate(contract.startDate) }} - {{ formatDate(contract.endDate) }}</td>
                <td>{{ money(contract.depositAmount) }}</td>
                <td>{{ money(contract.monthlyFee) }}</td>
                <td>
                  <span class="status-pill" :class="statusClass(contract.status)">
                    {{ statusLabel(contract.status) }}
                  </span>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </section>

      <v-dialog v-model="reopenDialog" max-width="520">
        <v-card>
          <v-card-title>Yêu cầu xử lý lại</v-card-title>
          <v-card-text>
            <p class="dialog-copy">Hãy mô tả phần chưa đạt để nhân viên tiếp tục xử lý đúng vấn đề.</p>
            <v-textarea v-model="reopenNote" label="Lý do chưa đạt" rows="4" variant="outlined" autofocus />
          </v-card-text>
          <v-card-actions>
            <v-spacer />
            <v-btn variant="text" @click="reopenDialog = false">Hủy</v-btn>
            <v-btn color="warning" :loading="incidentActionLoading === reopenIncidentTarget?.id" @click="submitReopen">Gửi lại yêu cầu</v-btn>
          </v-card-actions>
        </v-card>
      </v-dialog>
    </template>
  </section>
</template>

<script setup>
import { computed, onMounted, reactive, ref } from 'vue'
import api from '@/services/api'
import { cleanStudents, normalizeList } from '../utils/studentDisplay'

const loading = ref(true)
const submitting = ref(false)
const error = ref('')
const success = ref('')
const student = ref(null)
const ownRegistrations = ref([])
const ownContracts = ref([])
const ownIncidents = ref([])
const incidentActionLoading = ref(null)
const reopenDialog = ref(false)
const reopenIncidentTarget = ref(null)
const reopenNote = ref('')

const studentId = ref(Number(localStorage.getItem('student_id') || 0))
const studentCode = ref(
  localStorage.getItem('student_code') ||
  (localStorage.getItem('user_role') === 'Student'
    ? localStorage.getItem('username')
    : '') ||
  '')
const displayName = computed(() =>
  student.value?.fullName || localStorage.getItem('fullName') || 'Sinh viên')

const buildingOptions = ['A', 'B', 'C']
const roomTypeOptions = ['4-bed', '6-bed', '8-bed']
const priorityOptions = [
  { label: 'Không ưu tiên', value: '' },
  { label: 'Sinh viên ở xa', value: 'far' },
  { label: 'Hoàn cảnh khó khăn', value: 'poor' },
  { label: 'Diện chính sách', value: 'policy' },
  { label: 'Khuyết tật', value: 'disabled' },
  { label: 'Thành tích tốt', value: 'excellent' },
]

const form = reactive({
  buildingName: 'A',
  roomType: '4-bed',
  priorityType: '',
  priorityNote: '',
  startDate: '',
  endDate: '',
})

const incidentCategories = [
  { title: 'Điện', value: 'Electric' },
  { title: 'Nước', value: 'Water' },
  { title: 'Nội thất', value: 'Furniture' },
  { title: 'Internet', value: 'Internet' },
  { title: 'An toàn', value: 'Safety' },
  { title: 'Vệ sinh', value: 'Sanitation' },
  { title: 'Khác', value: 'Other' },
]
const incidentPriorityOptions = [
  { title: 'Thấp', value: 'low' },
  { title: 'Bình thường', value: 'normal' },
  { title: 'Cao', value: 'high' },
  { title: 'Khẩn cấp', value: 'urgent' },
]
const incidentSubmitting = ref(false)
const incidentForm = reactive({
  roomName: '',
  building: 'A',
  category: 'Electric',
  priority: 'normal',
  preferredVisitAt: '',
  contactPhone: '',
  description: '',
})

const currentRoom = computed(() => {
  const activeContract = ownContracts.value.find((contract) =>
    String(contract.status || '').toLowerCase() === 'active')

  if (activeContract) return `#${activeContract.roomId}`
  return student.value?.residenceHistory ? 'Có' : 'Chưa có'
})

const setDefaultDates = () => {
  const start = new Date()
  const end = new Date()
  end.setMonth(end.getMonth() + 10)

  form.startDate = toInputDate(start)
  form.endDate = toInputDate(end)
}

const toInputDate = (date) => {
  const year = date.getFullYear()
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')
  return `${year}-${month}-${day}`
}

const toIsoDate = (value) => new Date(`${value}T00:00:00`).toISOString()

const resolveStudent = async () => {
  const response = await api.get('/students')
  const students = cleanStudents(response.data)

  const matched = students.find((item) =>
    Number(item.id) === studentId.value ||
    (studentCode.value && item.studentCode === studentCode.value))

  student.value = matched || null

  if (student.value) {
    studentId.value = Number(student.value.id)
    studentCode.value = student.value.studentCode || studentCode.value
    localStorage.setItem('student_id', String(studentId.value))
    localStorage.setItem('student_code', studentCode.value)
    localStorage.setItem('fullName', student.value.fullName)
    incidentForm.contactPhone = incidentForm.contactPhone || student.value.phone || ''
  }
}

const loadRegistrations = async () => {
  const response = await api.get('/registrations')
  const all = normalizeList(response.data)

  ownRegistrations.value = all
    .filter((registration) => Number(registration.studentId) === studentId.value)
    .sort((first, second) =>
      new Date(second.createdAt || 0) - new Date(first.createdAt || 0))
}

const loadContracts = async () => {
  if (!studentId.value) {
    ownContracts.value = []
    return
  }

  try {
    const response = await api.get(`/contracts/student/${studentId.value}`)
    ownContracts.value = normalizeList(response.data)
  } catch {
    const response = await api.get('/contracts')
    ownContracts.value = normalizeList(response.data)
      .filter((contract) => Number(contract.studentId) === studentId.value)
  }
}

const loadIncidents = async () => {
  if (!studentId.value) {
    ownIncidents.value = []
    return
  }

  const response = await api.get(`/incidents?studentId=${studentId.value}`)
  ownIncidents.value = normalizeList(response.data)
}

const loadAll = async () => {
  try {
    loading.value = true
    error.value = ''
    success.value = ''

    await resolveStudent()
    await Promise.all([
      loadRegistrations(),
      loadContracts(),
      loadIncidents(),
    ])
  } catch (err) {
    error.value = 'Không tải được dữ liệu sinh viên. Kiểm tra Gateway và ContractStudentService.'
    console.error(err)
  } finally {
    loading.value = false
  }
}

const submitIncident = async () => {
  if (!student.value) {
    error.value = 'Chưa tìm thấy hồ sơ sinh viên để gửi yêu cầu sửa chữa.'
    return
  }

  if (!incidentForm.roomName.trim() || !incidentForm.description.trim()) {
    error.value = 'Vui lòng nhập phòng và mô tả sự cố.'
    return
  }

  try {
    incidentSubmitting.value = true
    error.value = ''
    success.value = ''

    await api.post('/incidents', {
      studentId: studentId.value,
      studentCode: studentCode.value,
      studentName: displayName.value,
      roomName: incidentForm.roomName,
      building: incidentForm.building,
      category: incidentForm.category,
      priority: incidentForm.priority,
      preferredVisitAt: incidentForm.preferredVisitAt
        ? new Date(incidentForm.preferredVisitAt).toISOString()
        : null,
      contactPhone: incidentForm.contactPhone,
      description: incidentForm.description,
    })

    success.value = 'Đã gửi yêu cầu sửa chữa. Nhân viên/admin sẽ tiếp nhận và cập nhật trạng thái.'
    incidentForm.description = ''
    incidentForm.preferredVisitAt = ''
    await loadIncidents()
  } catch (err) {
    error.value = 'Không gửi được yêu cầu sửa chữa. Kiểm tra Gateway và BillingService.'
    console.error(err)
  } finally {
    incidentSubmitting.value = false
  }
}

const confirmIncident = async (incident) => {
  try {
    incidentActionLoading.value = incident.id
    error.value = ''
    await api.post(`/incidents/${incident.id}/confirm`, {
      note: 'Sinh viên xác nhận yêu cầu đã được xử lý đạt yêu cầu.',
    })
    success.value = 'Đã xác nhận yêu cầu sửa chữa hoàn thành.'
    await loadIncidents()
  } catch (err) {
    error.value = err.response?.data?.message || 'Không xác nhận được yêu cầu sửa chữa.'
  } finally {
    incidentActionLoading.value = null
  }
}

const openReopenDialog = (incident) => {
  reopenIncidentTarget.value = incident
  reopenNote.value = ''
  reopenDialog.value = true
}

const submitReopen = async () => {
  if (!reopenIncidentTarget.value || !reopenNote.value.trim()) {
    error.value = 'Vui lòng nhập lý do cần xử lý lại.'
    return
  }

  try {
    incidentActionLoading.value = reopenIncidentTarget.value.id
    error.value = ''
    await api.post(`/incidents/${reopenIncidentTarget.value.id}/reopen`, {
      note: reopenNote.value.trim(),
    })
    reopenDialog.value = false
    success.value = 'Đã gửi lại yêu cầu cho nhân viên phụ trách.'
    await loadIncidents()
  } catch (err) {
    error.value = err.response?.data?.message || 'Không thể mở lại yêu cầu sửa chữa.'
  } finally {
    incidentActionLoading.value = null
  }
}

const submitRegistration = async () => {
  if (!student.value) {
    error.value = 'Chưa tìm thấy hồ sơ sinh viên để tạo đơn đăng ký.'
    return
  }

  if (new Date(form.endDate) <= new Date(form.startDate)) {
    error.value = 'Ngày kết thúc phải sau ngày bắt đầu.'
    return
  }

  try {
    submitting.value = true
    error.value = ''
    success.value = ''

    await api.post('/registrations', {
      studentId: studentId.value,
      buildingName: form.buildingName,
      roomType: form.roomType,
      priorityType: form.priorityType,
      priorityNote: form.priorityNote,
      startDate: toIsoDate(form.startDate),
      endDate: toIsoDate(form.endDate),
      status: 'Pending',
    })

    success.value = 'Đã gửi đơn đăng ký. Admin/nhân viên có thể duyệt ở mục Duyệt xếp phòng.'
    form.priorityType = ''
    form.priorityNote = ''
    await loadRegistrations()
  } catch (err) {
    error.value = 'Không gửi được đơn đăng ký. Kiểm tra dữ liệu sinh viên và service N2.'
    console.error(err)
  } finally {
    submitting.value = false
  }
}

const priorityLabel = (value) => {
  return priorityOptions.find((item) => item.value === value)?.label || 'Không ưu tiên'
}

const statusLabel = (status) => {
  const normalized = String(status || '').toLowerCase()
  if (normalized === 'pending') return 'Chờ duyệt'
  if (normalized === 'approved') return 'Đã duyệt'
  if (normalized === 'rejected') return 'Đã từ chối'
  if (normalized === 'active') return 'Đang hiệu lực'
  if (normalized === 'cancelled') return 'Đã hủy'
  if (normalized === 'expired') return 'Hết hạn'
  return status || 'Chưa cập nhật'
}

const statusClass = (status) => {
  const normalized = String(status || '').toLowerCase()
  return {
    'status-pending': normalized === 'pending',
    'status-approved': normalized === 'approved' || normalized === 'active',
    'status-rejected': normalized === 'rejected' || normalized === 'cancelled',
    'status-expired': normalized === 'expired',
  }
}

const incidentStatusLabel = (status) => {
  const normalized = String(status || '').toLowerCase()
  if (normalized === 'new') return 'Mới gửi'
  if (normalized === 'accepted') return 'Đã tiếp nhận'
  if (normalized === 'assigned') return 'Đã phân công'
  if (normalized === 'processing') return 'Đang xử lý'
  if (normalized === 'waiting-materials') return 'Chờ vật tư'
  if (normalized === 'done' || normalized === 'completed') return 'Chờ bạn xác nhận'
  if (normalized === 'confirmed') return 'Đã hoàn thành'
  if (normalized === 'reopened') return 'Đang xử lý lại'
  if (normalized === 'rejected') return 'Từ chối'
  if (normalized === 'cancelled') return 'Đã hủy'
  return status || 'Chưa cập nhật'
}

const incidentPriorityLabel = (priority) =>
  incidentPriorityOptions.find((item) => item.value === priority)?.title || 'Bình thường'

const incidentCategoryLabel = (category) => {
  return incidentCategories.find((item) => item.value === category)?.title || category
}

const incidentStatusClass = (status) => {
  const normalized = String(status || '').toLowerCase()
  return {
    'status-pending': normalized === 'new',
    'status-approved': ['accepted', 'assigned', 'processing', 'confirmed'].includes(normalized),
    'status-expired': ['waiting-materials', 'completed', 'reopened'].includes(normalized),
    'status-rejected': ['rejected', 'cancelled'].includes(normalized),
  }
}

const formatDate = (value) => {
  if (!value) return '-'
  return new Intl.DateTimeFormat('vi-VN').format(new Date(value))
}

const formatDateTime = (value) => {
  if (!value) return '-'
  return new Intl.DateTimeFormat('vi-VN', {
    dateStyle: 'short',
    timeStyle: 'short',
  }).format(new Date(value))
}

const money = (value) => {
  return new Intl.NumberFormat('vi-VN', {
    style: 'currency',
    currency: 'VND',
    maximumFractionDigits: 0,
  }).format(Number(value || 0))
}

setDefaultDates()
onMounted(loadAll)
</script>

<style scoped>
.student-portal {
  display: grid;
  gap: 18px;
}

.incident-actions {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
}

.table-note {
  display: block;
  margin-top: 4px;
  color: var(--muted);
  font-size: 12px;
}

.priority-low { background: #f5f5f5; color: #595959; }
.priority-normal { background: #e6f4ff; color: #0958d9; }
.priority-high { background: #fff7e6; color: #d46b08; }
.priority-urgent { background: #fff1f0; color: #cf1322; }
.dialog-copy { margin: 0 0 14px; color: var(--muted); }

.student-hero {
  display: flex;
  justify-content: space-between;
  gap: 28px;
  padding: 28px;
  border: 1px solid var(--line);
  border-radius: 8px;
  background: linear-gradient(135deg, #ffffff, #f1fbf6);
}

.student-hero h2 {
  margin: 4px 0 8px;
  color: var(--brand-dark);
  font-size: 30px;
}

.student-hero p {
  max-width: 640px;
  margin: 0;
  color: var(--muted);
  line-height: 1.6;
}

.hero-stats {
  display: grid;
  grid-template-columns: repeat(3, minmax(110px, 1fr));
  gap: 12px;
  min-width: 390px;
}

.stat-tile,
.panel {
  border: 1px solid var(--line);
  border-radius: 8px;
  background: #ffffff;
}

.stat-tile {
  display: grid;
  align-content: center;
  gap: 4px;
  min-height: 112px;
  padding: 16px;
}

.stat-tile .mdi {
  color: var(--brand);
  font-size: 26px;
}

.stat-tile strong {
  color: var(--ink);
  font-size: 25px;
}

.stat-tile small {
  color: var(--muted);
  font-weight: 800;
}

.loading-state {
  display: flex;
  align-items: center;
  gap: 12px;
  min-height: 140px;
  padding: 24px;
  color: var(--muted);
}

.portal-grid {
  display: grid;
  grid-template-columns: minmax(320px, 0.85fr) minmax(420px, 1.15fr);
  gap: 18px;
}

.panel {
  padding: 20px;
}

.panel-title {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-bottom: 18px;
}

.panel-title .mdi {
  display: grid;
  place-items: center;
  width: 40px;
  height: 40px;
  border-radius: 8px;
  background: #eaf8f0;
  color: var(--brand-dark);
  font-size: 22px;
}

.panel-title strong,
.panel-title small {
  display: block;
}

.panel-title strong {
  color: var(--ink);
  font-size: 17px;
}

.panel-title small {
  margin-top: 3px;
  color: var(--muted);
}

.profile-list {
  display: grid;
  gap: 12px;
}

.profile-list div {
  display: grid;
  gap: 4px;
  padding-bottom: 12px;
  border-bottom: 1px solid #eef2f5;
}

.profile-list span {
  color: var(--muted);
  font-size: 12px;
  font-weight: 900;
  letter-spacing: 0.04em;
  text-transform: uppercase;
}

.profile-list strong {
  color: var(--ink);
  font-size: 14px;
  line-height: 1.45;
}

.registration-form {
  display: grid;
  gap: 10px;
}

.form-actions {
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
}

.table-wrap {
  overflow-x: auto;
}

table {
  width: 100%;
  border-collapse: collapse;
  min-width: 760px;
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

.status-pill {
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

.status-pending {
  background: #fff8e6;
  color: #a16207;
}

.status-approved {
  background: #eaf8f0;
  color: #0f7a44;
}

.status-rejected {
  background: #fee2e2;
  color: #b91c1c;
}

.status-expired {
  background: #eef2f7;
  color: #475569;
}

.empty-state,
.empty-cell {
  color: var(--muted);
}

.empty-cell {
  padding: 24px 12px;
  text-align: center;
}

@media (max-width: 1100px) {
  .student-hero,
  .portal-grid {
    grid-template-columns: 1fr;
  }

  .student-hero {
    display: grid;
  }

  .hero-stats {
    min-width: 0;
  }
}

@media (max-width: 720px) {
  .student-hero,
  .panel {
    padding: 16px;
  }

  .hero-stats {
    grid-template-columns: 1fr;
  }

  .student-hero h2 {
    font-size: 24px;
  }
}
</style>
