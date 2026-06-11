<template>
  <section class="registration-page">
    <div class="page-heading">
      <div>
        <span class="page-kicker">{{ pageKicker }}</span>
        <h2>{{ pageTitle }}</h2>
        <p>{{ pageDescription }}</p>
      </div>
      <v-btn color="primary" variant="flat" prepend-icon="mdi-refresh" :loading="loading" @click="loadAll">
        Làm mới
      </v-btn>
    </div>

    <div class="mode-switch">
      <router-link
        to="/student-service/registrations"
        class="mode-card"
        :class="{ active: !isApprovalView }"
      >
        <span>6</span>
        <div>
          <strong>Đăng ký nội trú online</strong>
          <small>Nhập nguyện vọng, thời hạn ở và diện ưu tiên của sinh viên.</small>
        </div>
      </router-link>

      <router-link
        to="/student-service/registrations/approval"
        class="mode-card"
        :class="{ active: isApprovalView }"
      >
        <span>7</span>
        <div>
          <strong>Duyệt đơn xếp phòng</strong>
          <small>N2 gọi RoomService để tìm giường trống, cập nhật phòng và sinh hợp đồng.</small>
        </div>
      </router-link>
    </div>

    <v-alert v-if="message" :type="messageType" variant="tonal" class="mb-4">{{ message }}</v-alert>

    <v-card v-if="!isApprovalView" class="pa-5 mb-4 form-panel">
      <div class="panel-head">
        <div class="panel-icon">
          <span class="mdi mdi-clipboard-plus-outline"></span>
        </div>
        <div>
          <h3 class="section-title">Tạo đơn đăng ký nội trú</h3>
          <p>Sau khi tạo, đơn sẽ chuyển sang trạng thái chờ duyệt để cán bộ N2 xếp phòng.</p>
        </div>
      </div>

      <v-form @submit.prevent="createRegistration">
        <v-row>
          <v-col cols="12" md="4">
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
            <v-select
              v-model="form.buildingName"
              :items="buildingOptions"
              item-title="title"
              item-value="value"
              label="Tòa mong muốn"
              density="compact"
            />
          </v-col>
          <v-col cols="12" md="2">
            <v-select
              v-model="form.roomType"
              :items="roomTypeOptions"
              item-title="title"
              item-value="value"
              label="Loại phòng"
              density="compact"
            />
          </v-col>
          <v-col cols="12" md="4">
            <v-select
              v-model="form.priorityType"
              :items="priorityTypes"
              item-title="title"
              item-value="value"
              label="Diện ưu tiên"
              density="compact"
            />
          </v-col>
          <v-col cols="12" md="4">
            <v-text-field v-model="form.priorityNote" label="Ghi chú ưu tiên" density="compact" />
          </v-col>
          <v-col cols="12" md="3">
            <v-text-field v-model="form.startDate" label="Ngày bắt đầu" type="date" density="compact" />
          </v-col>
          <v-col cols="12" md="3">
            <v-text-field v-model="form.endDate" label="Ngày kết thúc" type="date" density="compact" />
          </v-col>
          <v-col cols="12" md="2" class="d-flex align-center">
            <v-btn color="success" type="submit" :loading="saving">Gửi đăng ký</v-btn>
          </v-col>
        </v-row>
      </v-form>
    </v-card>

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
            <th v-if="isApprovalView">Thao tác</th>
          </tr>
        </thead>
        <tbody>
          <tr v-if="loading">
            <td :colspan="isApprovalView ? 8 : 7">Đang tải dữ liệu...</td>
          </tr>
          <tr v-else-if="filteredRegistrations.length === 0">
            <td :colspan="isApprovalView ? 8 : 7">{{ emptyText }}</td>
          </tr>
          <tr v-for="registration in filteredRegistrations" :key="registration.id">
            <td>{{ registration.id }}</td>
            <td>{{ studentName(registration.studentId) }}</td>
            <td>
              <strong>Tòa {{ registration.buildingName || 'bất kỳ' }}</strong>
              <span>{{ registration.roomType || 'Bất kỳ loại phòng' }}</span>
            </td>
            <td>
              <strong>{{ priorityLabel(registration.priorityType) }}</strong>
              <span>Điểm: {{ registration.priorityScore || 0 }}</span>
            </td>
            <td>{{ formatDate(registration.startDate) }} - {{ formatDate(registration.endDate) }}</td>
            <td>
              <span class="status-pill" :class="statusClass(registration.status)">
                {{ statusLabel(registration.status) }}
              </span>
            </td>
            <td>{{ registration.assignedRoomId ? `Phòng ${registration.assignedRoomId}` : 'Chưa xếp' }}</td>
            <td v-if="isApprovalView">
              <div class="action-row">
                <div
                  v-if="registration.status === 'Pending'"
                  class="manual-room-picker"
                >
                  <v-select
                    v-model="approvalDraft(registration).buildingName"
                    :items="buildingOptions"
                    item-title="title"
                    item-value="value"
                    label="Tòa xếp"
                    density="compact"
                    hide-details
                    @update:model-value="clearDraftRoom(registration.id)"
                  />
                  <v-select
                    v-model="approvalDraft(registration).roomId"
                    :items="roomOptionsForRegistration(registration)"
                    item-title="title"
                    item-value="value"
                    label="Phòng còn giường"
                    density="compact"
                    hide-details
                    no-data-text="Không có phòng phù hợp"
                  />
                  <v-btn
                    color="primary"
                    size="small"
                    :disabled="!approvalDraft(registration).roomId"
                    @click="approveRegistration(registration.id, approvalDraft(registration).roomId)"
                  >
                    Duyệt phòng đã chọn
                  </v-btn>
                </div>
                <v-btn
                  color="success"
                  size="small"
                  :disabled="registration.status !== 'Pending'"
                  @click="approveRegistration(registration.id)"
                >
                  Tự xếp
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
import { computed, onMounted, ref, watch } from 'vue'
import { useRoute } from 'vue-router'
import api from '@/services/api'
import {
  buildStudentNameMap,
  cleanStudents,
  normalizeList,
} from '../utils/studentDisplay'

const route = useRoute()
const loading = ref(false)
const saving = ref(false)
const message = ref('')
const messageType = ref('success')
const students = ref([])
const registrations = ref([])
const buildings = ref([])
const roomTypes = ref([])
const rooms = ref([])
const approvalDrafts = ref({})
const statusFilter = ref('Pending')

const isApprovalView = computed(() => route.name === 'RoomRegistrationApproval')
const pageKicker = computed(() => isApprovalView.value ? 'Rubric 7 - Approval' : 'Rubric 6 - Online Registration')
const pageTitle = computed(() => isApprovalView.value ? 'Duyệt đơn xếp phòng' : 'Đăng ký ở ký túc xá online')
const pageDescription = computed(() =>
  isApprovalView.value
    ? 'Trọng tâm logic N2: đối chiếu giới tính, tòa, loại phòng, số giường trống rồi cập nhật RoomService và tạo hợp đồng.'
    : 'Tiếp nhận đơn nội trú trực tuyến, lưu nguyện vọng phòng và tính điểm ưu tiên trước khi chuyển sang bước duyệt.',
)
const emptyText = computed(() =>
  isApprovalView.value
    ? 'Không có đơn chờ duyệt trong nhóm đang chọn.'
    : 'Chưa có đơn đăng ký phù hợp.',
)

const statusOptions = [
  { title: 'Chỉ đơn chờ duyệt', value: 'Pending' },
  { title: 'Đã duyệt', value: 'Approved' },
  { title: 'Đã từ chối', value: 'Rejected' },
  { title: 'Tất cả lịch sử', value: 'All' },
]

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

const studentMap = computed(() => buildStudentNameMap(students.value))
const studentById = computed(() =>
  new Map(students.value.map((student) => [Number(student.id), student])))

const buildingOptions = computed(() => {
  const options = buildings.value
    .map((building) => ({
      title: building.displayName
        ? `${building.displayName} (${building.buildingName})`
        : `Tòa ${building.buildingName}`,
      value: building.buildingName,
    }))
    .filter((building) => building.value)

  return options.length > 0
    ? options
    : ['A', 'B', 'C'].map((buildingName) => ({
      title: `Tòa ${buildingName}`,
      value: buildingName,
    }))
})

const roomTypeOptions = computed(() => {
  const options = roomTypes.value
    .map((roomType) => ({
      title: roomType.description
        ? `${roomType.description} (${roomType.roomType})`
        : roomType.roomType,
      value: roomType.roomType,
    }))
    .filter((roomType) => roomType.value)

  return options.length > 0
    ? options
    : ['4-bed', '6-bed', '8-bed'].map((roomType) => ({
      title: roomType,
      value: roomType,
    }))
})

const filteredRegistrations = computed(() => {
  if (statusFilter.value === 'All') return registrations.value
  return registrations.value.filter((registration) => registration.status === statusFilter.value)
})

watch(
  () => route.name,
  () => {
    statusFilter.value = isApprovalView.value ? 'Pending' : 'All'
    message.value = ''
  },
  { immediate: true },
)

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

const loadRoomCatalog = async () => {
  const [buildingRes, roomTypeRes, roomRes] = await Promise.all([
    api.get('/buildings'),
    api.get('/roomtypes'),
    api.get('/rooms'),
  ])

  buildings.value = normalizeList(buildingRes.data)
  roomTypes.value = normalizeList(roomTypeRes.data)
  rooms.value = normalizeList(roomRes.data)
}

const loadAll = async () => {
  try {
    loading.value = true
    await Promise.all([loadStudents(), loadRegistrations(), loadRoomCatalog()])
    syncApprovalDrafts()
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
    showMessage('Đã tạo đơn đăng ký. Chuyển sang bước duyệt để N2 tự động xếp phòng.')
    await loadRegistrations()
  } catch (err) {
    showMessage('Không tạo được đơn đăng ký. Kiểm tra sinh viên và thời gian ở.', 'error')
    console.error(err)
  } finally {
    saving.value = false
  }
}

const approveRegistration = async (id, roomId = null) => {
  try {
    await api.put(
      `/registrations/${id}/approve`,
      null,
      roomId ? { params: { roomId } } : undefined,
    )
    showMessage(roomId
      ? 'Đã duyệt đơn theo phòng đã chọn, cập nhật RoomService và tạo hợp đồng.'
      : 'Đã duyệt đơn, tự xếp phòng, cập nhật RoomService và tạo hợp đồng.')
    await loadAll()
  } catch (err) {
    showMessage('Không duyệt được đơn. Có thể phòng đã chọn không phù hợp giới tính, đã hết giường hoặc RoomService chưa nối.', 'error')
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

const syncApprovalDrafts = () => {
  const nextDrafts = {}

  registrations.value.forEach((registration) => {
    nextDrafts[registration.id] = approvalDrafts.value[registration.id] || {
      buildingName: registration.buildingName || '',
      roomId: null,
    }
  })

  approvalDrafts.value = nextDrafts
}

const approvalDraft = (registration) => {
  if (!approvalDrafts.value[registration.id]) {
    approvalDrafts.value[registration.id] = {
      buildingName: registration.buildingName || '',
      roomId: null,
    }
  }

  return approvalDrafts.value[registration.id]
}

const clearDraftRoom = (registrationId) => {
  if (approvalDrafts.value[registrationId]) {
    approvalDrafts.value[registrationId].roomId = null
  }
}

const roomOptionsForRegistration = (registration) => {
  const draft = approvalDraft(registration)
  const student = studentById.value.get(Number(registration.studentId))

  return rooms.value
    .filter((room) => isRoomAvailable(room))
    .filter((room) => !student || room.gender === student.gender)
    .filter((room) => isSameCode(room.buildingName, draft.buildingName || registration.buildingName))
    .filter((room) => isSameCode(room.roomType, registration.roomType))
    .sort((first, second) =>
      first.buildingName.localeCompare(second.buildingName) ||
      Number(first.floor || 0) - Number(second.floor || 0) ||
      Number(first.roomId || 0) - Number(second.roomId || 0))
    .map((room) => ({
      title: roomOptionLabel(room),
      value: room.roomId,
    }))
}

const isRoomAvailable = (room) => {
  const status = String(room.status || '').toLowerCase()
  const availableBeds = Number(room.availableBeds ?? 0)

  return status !== 'maintenance' &&
    status !== 'full' &&
    availableBeds > 0
}

const isSameCode = (first, second) => {
  if (!second) return true
  return String(first || '').trim().toLowerCase() ===
    String(second || '').trim().toLowerCase()
}

const roomOptionLabel = (room) => {
  const genderText = room.genderText || (room.gender ? 'Nam' : 'Nữ')
  const floorText = room.floorName || `Tầng ${room.floor || '-'}`
  const availableBeds = Number(room.availableBeds ?? 0)
  const fee = Number(room.monthlyFee || 0).toLocaleString('vi-VN')

  return `Tòa ${room.buildingName} - Phòng ${room.roomNumber || room.roomId} - ${floorText} - ${genderText} - còn ${availableBeds}/${room.capacity} giường - ${fee}đ`
}

const priorityLabel = (value) => {
  return priorityTypes.find((item) => item.value === value)?.title || 'Không ưu tiên'
}

const countByStatus = (status) => {
  return registrations.value.filter((registration) => registration.status === status).length
}

const statusLabel = (status) => {
  const labels = {
    Pending: 'Chờ duyệt',
    Approved: 'Đã duyệt',
    Rejected: 'Từ chối',
  }

  return labels[status] || status
}

const statusClass = (status) => {
  return String(status || '').toLowerCase()
}

const formatDate = (value) => {
  if (!value) return ''
  return new Date(value).toLocaleDateString('vi-VN')
}

onMounted(loadAll)
</script>

<style scoped>
.registration-page {
  display: flex;
  flex-direction: column;
  gap: 18px;
}

.page-heading {
  display: flex;
  align-items: flex-end;
  justify-content: space-between;
  gap: 18px;
  margin-bottom: 0;
}

.page-heading p {
  max-width: 860px;
  margin: 8px 0 0;
  color: var(--muted);
  font-size: 15px;
  line-height: 1.5;
}

.mode-switch {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 14px;
}

.mode-card {
  display: grid;
  grid-template-columns: 42px minmax(0, 1fr);
  gap: 14px;
  min-height: 96px;
  padding: 18px;
  border: 1px solid var(--line);
  border-radius: 8px;
  background: #ffffff;
  color: inherit;
  text-decoration: none;
}

.mode-card.active {
  border-color: rgba(22, 155, 99, 0.32);
  background: #f0fdf4;
}

.mode-card > span {
  display: grid;
  place-items: center;
  width: 42px;
  height: 42px;
  border-radius: 8px;
  background: #111827;
  color: #ffffff;
  font-weight: 900;
}

.mode-card.active > span {
  background: var(--brand);
  color: #052e1c;
}

.mode-card strong {
  display: block;
  color: var(--ink);
  font-size: 16px;
}

.mode-card small {
  display: block;
  margin-top: 6px;
  color: var(--muted);
  line-height: 1.45;
}

.section-title {
  margin: 0;
  color: #14532d;
}

.form-panel {
  background:
    linear-gradient(135deg, rgba(22, 155, 99, 0.08), transparent 40%),
    #ffffff;
}

.filter-panel {
  background: #ffffff;
}

.summary-row {
  display: flex;
  align-items: center;
  justify-content: flex-end;
  gap: 18px;
  color: #40576a;
  font-weight: 800;
}

.panel-head {
  display: flex;
  align-items: flex-start;
  gap: 12px;
  margin-bottom: 18px;
}

.panel-head p {
  margin: 5px 0 0;
  color: var(--muted);
  font-size: 14px;
}

.panel-icon {
  display: grid;
  place-items: center;
  width: 42px;
  height: 42px;
  border-radius: 8px;
  background: #dcfce7;
  color: #15803d;
  font-size: 23px;
}

.table-card {
  overflow: hidden;
}

.data-table td strong,
.data-table td span {
  display: block;
}

.data-table td span {
  margin-top: 4px;
  color: var(--muted);
  font-size: 13px;
}

.action-row {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-wrap: wrap;
}

.manual-room-picker {
  display: grid;
  grid-template-columns: minmax(150px, 0.6fr) minmax(320px, 1fr) auto;
  align-items: center;
  gap: 8px;
  min-width: 620px;
}

.status-pill {
  display: inline-flex !important;
  align-items: center;
  min-height: 28px;
  padding: 0 10px;
  border-radius: 999px;
  background: #e8eef5;
  color: #34495e !important;
  font-size: 12px !important;
  font-weight: 900;
}

.status-pill.pending {
  background: #fef3c7;
  color: #b45309 !important;
}

.status-pill.approved {
  background: #dcfce7;
  color: #15803d !important;
}

.status-pill.rejected {
  background: #ffe4e6;
  color: #be123c !important;
}

@media (max-width: 860px) {
  .page-heading {
    align-items: stretch;
    flex-direction: column;
  }

  .mode-switch {
    grid-template-columns: 1fr;
  }

  .summary-row {
    align-items: flex-start;
    flex-direction: column;
    gap: 8px;
  }

  .manual-room-picker {
    grid-template-columns: 1fr;
    min-width: 0;
    width: 100%;
  }
}
</style>
