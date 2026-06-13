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

    <div class="registration-metrics">
      <article
        v-for="metric in registrationMetrics"
        :key="metric.label"
        class="registration-metric"
        :class="metric.tone"
      >
        <span :class="['mdi', metric.icon]"></span>
        <div>
          <strong>{{ metric.value }}</strong>
          <small>{{ metric.label }}</small>
        </div>
      </article>
    </div>

    <div v-if="!isApprovalView" class="status-tabs">
      <button
        v-for="tab in registrationTabs"
        :key="tab.value"
        type="button"
        :class="{ active: statusFilter === tab.value }"
        @click="statusFilter = tab.value"
      >
        <span :class="['mdi', tab.icon]"></span>
        {{ tab.label }}
        <strong>{{ tab.count }}</strong>
      </button>
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
          <v-text-field
            v-model="registrationSearch"
            label="Tìm theo sinh viên, mã đơn hoặc phòng"
            prepend-inner-icon="mdi-magnify"
            density="compact"
            clearable
          />
        </v-col>
        <v-col cols="12" md="2">
          <v-select
            v-model="buildingFilter"
            :items="filterBuildingOptions"
            item-title="title"
            item-value="value"
            label="Tòa"
            density="compact"
          />
        </v-col>
        <v-col cols="12" md="2">
          <v-select
            v-model="roomTypeFilter"
            :items="filterRoomTypeOptions"
            item-title="title"
            item-value="value"
            label="Loại phòng"
            density="compact"
          />
        </v-col>
        <v-col v-if="isApprovalView" cols="12" md="2">
          <v-select
            v-model="statusFilter"
            :items="statusOptions"
            label="Hiển thị đơn"
            density="compact"
          />
        </v-col>
        <v-col cols="12" :md="isApprovalView ? 2 : 4" class="summary-row">
          <span>Chờ duyệt: {{ countByStatus('Pending') }}</span>
          <span>Đã duyệt: {{ countByStatus('Approved') }}</span>
          <span>Từ chối: {{ countByStatus('Rejected') }}</span>
        </v-col>
      </v-row>
    </v-card>

    <v-card class="table-card">
      <div class="table-toolbar">
        <div>
          <span class="page-kicker">{{ isApprovalView ? 'Room Assignment' : 'Registration Queue' }}</span>
          <h3>{{ isApprovalView ? 'Hàng chờ duyệt xếp phòng' : 'Danh sách đơn đăng ký' }}</h3>
        </div>
        <div class="table-action-bar">
          <span class="table-count">{{ filteredRegistrations.length }} đơn</span>
          <v-btn
            color="primary"
            variant="tonal"
            prepend-icon="mdi-file-excel-outline"
            :disabled="filteredRegistrations.length === 0"
            @click="exportRegistrations"
          >
            Xuất Excel
          </v-btn>
        </div>
      </div>
      <table class="data-table compact-table">
        <thead>
          <tr>
            <th>Mã đơn</th>
            <th>Sinh viên</th>
            <th>Nguyện vọng</th>
            <th>Ưu tiên</th>
            <th>Trạng thái</th>
            <th>Phòng xếp</th>
            <th v-if="isApprovalView">Thao tác</th>
          </tr>
        </thead>
        <tbody>
          <tr v-if="loading" class="table-empty">
            <td :colspan="isApprovalView ? 7 : 6">Đang tải dữ liệu...</td>
          </tr>
          <tr v-else-if="filteredRegistrations.length === 0" class="table-empty">
            <td :colspan="isApprovalView ? 7 : 6">{{ emptyText }}</td>
          </tr>
          <tr v-for="registration in paginatedRegistrations" :key="registration.id">
            <td>
              <strong class="cell-title">#{{ registration.id }}</strong>
              <span class="cell-subtitle">{{ formatDate(registration.startDate) }} - {{ formatDate(registration.endDate) }}</span>
            </td>
            <td>
              <strong class="cell-title">{{ studentName(registration.studentId) }}</strong>
              <span class="cell-subtitle">MSSV / ID: {{ registration.studentId }}</span>
            </td>
            <td>
              <strong class="cell-title">Tòa {{ registration.buildingName || 'bất kỳ' }}</strong>
              <span class="cell-subtitle">{{ registration.roomType || 'Bất kỳ loại phòng' }}</span>
            </td>
            <td>
              <strong class="cell-title">{{ priorityLabel(registration.priorityType) }}</strong>
              <span class="cell-subtitle">Điểm: {{ registration.priorityScore || 0 }}</span>
            </td>
            <td>
              <span class="status-pill" :class="statusClass(registration.status)">
                {{ statusLabel(registration.status) }}
              </span>
            </td>
            <td>{{ registration.assignedRoomId ? `Phòng ${registration.assignedRoomId}` : 'Chưa xếp' }}</td>
            <td v-if="isApprovalView">
              <div class="action-row">
                <v-btn
                  color="primary"
                  size="small"
                  :disabled="registration.status !== 'Pending'"
                  prepend-icon="mdi-bed-outline"
                  @click="openApprovalDialog(registration)"
                >
                  Chọn phòng
                </v-btn>
                <v-btn
                  color="success"
                  size="small"
                  variant="tonal"
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
      <div class="pagination-row">
        <span>Hiển thị {{ pageStart }} - {{ pageEnd }} / {{ filteredRegistrations.length }} đơn</span>
        <div class="pagination-actions">
          <button class="page-button" type="button" :disabled="currentPage === 1" @click="currentPage -= 1">
            &lt;
          </button>
          <button
            v-for="page in totalPages"
            :key="page"
            class="page-button"
            :class="{ active: currentPage === page }"
            type="button"
            @click="currentPage = page"
          >
            {{ page }}
          </button>
          <button class="page-button" type="button" :disabled="currentPage === totalPages" @click="currentPage += 1">
            &gt;
          </button>
        </div>
      </div>
    </v-card>

    <v-dialog v-model="approvalDialog" max-width="980">
      <v-card v-if="selectedRegistration" class="approval-dialog">
        <v-card-title class="dialog-title">
          <div>
            <span class="page-kicker">Room Assignment</span>
            <strong>Xếp phòng cho đơn #{{ selectedRegistration.id }}</strong>
          </div>
          <v-btn icon="mdi-close" variant="text" @click="approvalDialog = false" />
        </v-card-title>

        <v-card-text>
          <div class="assignment-summary">
            <div>
              <span>Sinh viên</span>
              <strong>{{ studentName(selectedRegistration.studentId) }}</strong>
            </div>
            <div>
              <span>Nguyện vọng</span>
              <strong>Tòa {{ selectedRegistration.buildingName || 'bất kỳ' }} - {{ selectedRegistration.roomType || 'bất kỳ loại phòng' }}</strong>
            </div>
            <div>
              <span>Ưu tiên</span>
              <strong>{{ priorityLabel(selectedRegistration.priorityType) }} · {{ selectedRegistration.priorityScore || 0 }} điểm</strong>
            </div>
          </div>

          <div class="assignment-filters">
            <v-select
              v-model="approvalDraft(selectedRegistration).buildingName"
              :items="manualBuildingOptions"
              item-title="title"
              item-value="value"
              label="Tòa xếp thực tế"
              density="compact"
              hide-details
              @update:model-value="clearDraftRoom(selectedRegistration.id)"
            />
            <v-select
              v-model="approvalDraft(selectedRegistration).roomType"
              :items="manualRoomTypeOptions"
              item-title="title"
              item-value="value"
              label="Loại phòng xếp thực tế"
              density="compact"
              hide-details
              @update:model-value="clearDraftRoom(selectedRegistration.id)"
            />
          </div>

          <div class="room-choice-grid">
            <button
              v-for="room in availableRoomsForRegistration(selectedRegistration)"
              :key="room.roomId"
              type="button"
              class="room-choice"
              :class="{ active: approvalDraft(selectedRegistration).roomId === room.roomId }"
              @click="approvalDraft(selectedRegistration).roomId = room.roomId"
            >
              <strong>{{ room.roomNumber || room.roomId }}</strong>
              <span>Tòa {{ room.buildingName }} · {{ room.roomType }}</span>
              <small>Còn {{ Number(room.availableBeds ?? 0) }}/{{ room.capacity }} giường · {{ room.gender ? 'Nam' : 'Nữ' }}</small>
            </button>
          </div>

          <v-alert v-if="availableRoomsForRegistration(selectedRegistration).length === 0" type="warning" variant="tonal" class="mt-4">
            Không có phòng còn giường phù hợp với bộ lọc hiện tại. Có thể đổi tòa/loại phòng xếp thực tế để tìm phòng khác.
          </v-alert>
        </v-card-text>

        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="approvalDialog = false">Hủy</v-btn>
          <v-btn
            color="primary"
            :disabled="!approvalDraft(selectedRegistration).roomId"
            @click="approveSelectedRoom"
          >
            Xác nhận xếp phòng
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </section>
</template>

<script setup>
import { computed, onMounted, ref, watch } from 'vue'
import { useRoute } from 'vue-router'
import api from '@/services/api'
import { exportRowsToExcel } from '@/utils/exportExcel'
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
const registrationSearch = ref('')
const buildingFilter = ref('All')
const roomTypeFilter = ref('All')
const approvalDialog = ref(false)
const selectedRegistration = ref(null)
const currentPage = ref(1)
const pageSize = 8

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

const manualBuildingOptions = computed(() => [
  { title: 'Tất cả tòa còn giường', value: '' },
  ...buildingOptions.value,
])

const manualRoomTypeOptions = computed(() => [
  { title: 'Tất cả loại phòng còn giường', value: '' },
  ...roomTypeOptions.value,
])

const filterBuildingOptions = computed(() => [
  { title: 'Tất cả tòa', value: 'All' },
  ...buildingOptions.value,
])

const filterRoomTypeOptions = computed(() => [
  { title: 'Tất cả loại phòng', value: 'All' },
  ...roomTypeOptions.value,
])

const filteredRegistrations = computed(() => {
  const keyword = registrationSearch.value.trim().toLowerCase()

  return registrations.value.filter((registration) => {
    const matchesStatus = statusFilter.value === 'All' || registration.status === statusFilter.value
    const matchesBuilding = buildingFilter.value === 'All' || registration.buildingName === buildingFilter.value
    const matchesRoomType = roomTypeFilter.value === 'All' || registration.roomType === roomTypeFilter.value
    const haystack = [
      registration.id,
      studentName(registration.studentId),
      registration.buildingName,
      registration.roomType,
      registration.assignedRoomId,
      priorityLabel(registration.priorityType),
    ].join(' ').toLowerCase()
    const matchesKeyword = !keyword || haystack.includes(keyword)

    return matchesStatus && matchesBuilding && matchesRoomType && matchesKeyword
  })
})

const totalPages = computed(() => Math.max(1, Math.ceil(filteredRegistrations.value.length / pageSize)))
const paginatedRegistrations = computed(() => {
  const start = (currentPage.value - 1) * pageSize
  return filteredRegistrations.value.slice(start, start + pageSize)
})
const pageStart = computed(() =>
  filteredRegistrations.value.length === 0 ? 0 : (currentPage.value - 1) * pageSize + 1)
const pageEnd = computed(() =>
  Math.min(currentPage.value * pageSize, filteredRegistrations.value.length))

const availableRoomCount = computed(() => rooms.value.filter((room) => isRoomAvailable(room)).length)
const registrationMetrics = computed(() => [
  {
    icon: 'mdi-clipboard-clock-outline',
    value: countByStatus('Pending'),
    label: 'Đơn chờ duyệt',
    tone: 'warning',
  },
  {
    icon: 'mdi-check-decagram-outline',
    value: countByStatus('Approved'),
    label: 'Đã xếp phòng',
    tone: 'success',
  },
  {
    icon: 'mdi-bed-outline',
    value: availableRoomCount.value,
    label: 'Phòng còn giường',
    tone: 'info',
  },
  {
    icon: 'mdi-file-sign-outline',
    value: countByStatus('Rejected'),
    label: 'Đơn đã từ chối',
    tone: 'danger',
  },
])

const registrationTabs = computed(() => [
  {
    label: 'Tất cả',
    value: 'All',
    icon: 'mdi-file-document-multiple-outline',
    count: registrations.value.length,
  },
  {
    label: 'Chờ duyệt',
    value: 'Pending',
    icon: 'mdi-clock-outline',
    count: countByStatus('Pending'),
  },
  {
    label: 'Đã duyệt',
    value: 'Approved',
    icon: 'mdi-check-decagram-outline',
    count: countByStatus('Approved'),
  },
  {
    label: 'Từ chối',
    value: 'Rejected',
    icon: 'mdi-close-circle-outline',
    count: countByStatus('Rejected'),
  },
])

watch(
  () => route.name,
  () => {
    statusFilter.value = isApprovalView.value ? 'Pending' : 'All'
    buildingFilter.value = 'All'
    roomTypeFilter.value = 'All'
    registrationSearch.value = ''
    message.value = ''
  },
  { immediate: true },
)

watch([registrationSearch, buildingFilter, roomTypeFilter, statusFilter], () => {
  currentPage.value = 1
})

watch(filteredRegistrations, () => {
  if (currentPage.value > totalPages.value) {
    currentPage.value = totalPages.value
  }
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

const exportRegistrations = () => {
  exportRowsToExcel({
    filename: isApprovalView.value ? 'duyet-xep-phong.xls' : 'don-dang-ky-noi-tru.xls',
    sheetName: isApprovalView.value ? 'Duyệt xếp phòng' : 'Đơn đăng ký nội trú',
    rows: filteredRegistrations.value,
    columns: [
      { header: 'Mã đơn', value: (registration) => registration.id },
      { header: 'Sinh viên', value: (registration) => studentName(registration.studentId) },
      { header: 'Tòa mong muốn', value: (registration) => registration.buildingName || 'Bất kỳ' },
      { header: 'Loại phòng', value: (registration) => registration.roomType || 'Bất kỳ' },
      { header: 'Ưu tiên', value: (registration) => priorityLabel(registration.priorityType) },
      { header: 'Điểm ưu tiên', value: (registration) => registration.priorityScore || 0 },
      { header: 'Ngày bắt đầu', value: (registration) => formatDate(registration.startDate) },
      { header: 'Ngày kết thúc', value: (registration) => formatDate(registration.endDate) },
      { header: 'Trạng thái', value: (registration) => statusLabel(registration.status) },
      {
        header: 'Phòng xếp',
        value: (registration) => registration.assignedRoomId
          ? `Phòng ${registration.assignedRoomId}`
          : 'Chưa xếp',
      },
    ],
  })
}

const studentName = (id) => studentMap.value.get(id) || `Sinh viên #${id}`

const openApprovalDialog = (registration) => {
  selectedRegistration.value = registration
  approvalDraft(registration)
  approvalDialog.value = true
}

const approveSelectedRoom = async () => {
  if (!selectedRegistration.value) return

  const draft = approvalDraft(selectedRegistration.value)
  if (!draft.roomId) return

  await approveRegistration(selectedRegistration.value.id, draft.roomId)
  approvalDialog.value = false
}

const syncApprovalDrafts = () => {
  const nextDrafts = {}

  registrations.value.forEach((registration) => {
    nextDrafts[registration.id] = approvalDrafts.value[registration.id] || {
      buildingName: registration.buildingName || '',
      roomType: registration.roomType || '',
      roomId: null,
    }
  })

  approvalDrafts.value = nextDrafts
}

const approvalDraft = (registration) => {
  if (!approvalDrafts.value[registration.id]) {
    approvalDrafts.value[registration.id] = {
      buildingName: registration.buildingName || '',
      roomType: registration.roomType || '',
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

const availableRoomsForRegistration = (registration) => {
  const draft = approvalDraft(registration)
  const student = studentById.value.get(Number(registration.studentId))

  return rooms.value
    .filter((room) => isRoomAvailable(room))
    .filter((room) => !student || room.gender === student.gender)
    .filter((room) => isSameCode(room.buildingName, draft.buildingName))
    .filter((room) => isSameCode(room.roomType, draft.roomType))
    .sort((first, second) =>
      first.buildingName.localeCompare(second.buildingName) ||
      Number(first.floor || 0) - Number(second.floor || 0) ||
      Number(first.roomId || 0) - Number(second.roomId || 0))
}

const roomOptionsForRegistration = (registration) => {
  return availableRoomsForRegistration(registration)
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

.registration-metrics {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: 14px;
}

.registration-metric {
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

.registration-metric .mdi {
  display: grid;
  place-items: center;
  width: 46px;
  height: 46px;
  border-radius: 8px;
  background: #eff6ff;
  color: var(--blue);
  font-size: 24px;
}

.registration-metric strong,
.registration-metric small {
  display: block;
}

.registration-metric strong {
  color: var(--ink);
  font-family: var(--font-heading);
  font-size: 28px;
  line-height: 1;
}

.registration-metric small {
  margin-top: 6px;
  color: var(--muted);
  font-size: 13px;
  font-weight: 800;
}

.registration-metric.success .mdi {
  background: #ecfdf5;
  color: var(--brand-dark);
}

.registration-metric.warning .mdi {
  background: #fff7ed;
  color: #b45309;
}

.registration-metric.danger .mdi {
  background: #fff1f2;
  color: #be123c;
}

.status-tabs {
  display: flex;
  align-items: center;
  gap: 10px;
  flex-wrap: wrap;
  padding: 6px;
  border: 1px solid var(--line);
  border-radius: 8px;
  background: #ffffff;
}

.status-tabs button {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  min-height: 40px;
  padding: 0 13px;
  border: 1px solid transparent;
  border-radius: 8px;
  background: #f7faf8;
  color: var(--muted-strong);
  cursor: pointer;
  font-weight: 900;
}

.status-tabs button.active {
  border-color: rgba(15, 127, 81, 0.2);
  background: var(--brand);
  color: #ffffff;
}

.status-tabs strong {
  display: inline-grid;
  place-items: center;
  min-width: 24px;
  height: 24px;
  padding: 0 7px;
  border-radius: 999px;
  background: rgba(255, 255, 255, 0.72);
  color: var(--brand-dark);
  font-size: 12px;
}

.status-tabs button:not(.active) strong {
  background: #eaf8f0;
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
  background:
    linear-gradient(135deg, rgba(22, 155, 99, 0.1), transparent 58%),
    #ffffff;
  box-shadow: 0 12px 26px rgba(15, 127, 81, 0.08);
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

.table-toolbar {
  display: flex;
  align-items: flex-end;
  justify-content: space-between;
  gap: 18px;
  padding: 20px 22px;
  border-bottom: 1px solid var(--line);
  background: #ffffff;
}

.table-toolbar h3,
.table-toolbar p {
  margin: 0;
}

.table-toolbar h3 {
  color: var(--ink);
  font-family: var(--font-heading);
  font-size: 20px;
}

.table-toolbar p {
  max-width: 560px;
  color: var(--muted);
  font-size: 13px;
  line-height: 1.45;
  text-align: right;
}

.table-empty td {
  padding: 30px 18px;
  color: var(--muted);
  font-weight: 800;
  text-align: center;
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
  grid-template-columns: minmax(140px, 0.55fr) minmax(170px, 0.65fr) minmax(320px, 1fr) auto;
  align-items: center;
  gap: 8px;
  min-width: 820px;
  padding: 10px;
  border: 1px solid rgba(37, 99, 235, 0.15);
  border-radius: 8px;
  background: #f8fbff;
}

.approval-dialog {
  background: #ffffff;
}

.dialog-title {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  border-bottom: 1px solid var(--line);
}

.dialog-title strong {
  display: block;
  color: var(--ink);
  font-family: var(--font-heading);
  font-size: 21px;
}

.assignment-summary {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 12px;
  margin-bottom: 16px;
}

.assignment-summary div {
  padding: 13px;
  border: 1px solid var(--line);
  border-radius: 8px;
  background: #fbfdfb;
}

.assignment-summary span,
.assignment-summary strong {
  display: block;
}

.assignment-summary span {
  color: var(--muted);
  font-size: 12px;
  font-weight: 900;
}

.assignment-summary strong {
  margin-top: 5px;
  color: var(--ink);
  font-size: 14px;
  line-height: 1.35;
}

.assignment-filters {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 12px;
  margin-bottom: 16px;
}

.room-choice-grid {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 12px;
}

.room-choice {
  min-height: 108px;
  padding: 14px;
  border: 1px solid var(--line);
  border-radius: 8px;
  background: #ffffff;
  color: var(--ink);
  cursor: pointer;
  text-align: left;
}

.room-choice.active {
  border-color: var(--brand);
  background: #ecfdf5;
}

.room-choice strong,
.room-choice span,
.room-choice small {
  display: block;
}

.room-choice strong {
  color: var(--brand-dark);
  font-family: var(--font-heading);
  font-size: 22px;
}

.room-choice span {
  margin-top: 8px;
  color: var(--ink);
  font-weight: 900;
}

.room-choice small {
  margin-top: 6px;
  color: var(--muted);
  line-height: 1.35;
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

  .registration-metrics {
    grid-template-columns: repeat(2, minmax(0, 1fr));
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

  .assignment-summary,
  .assignment-filters,
  .room-choice-grid {
    grid-template-columns: 1fr;
  }

  .table-toolbar {
    align-items: flex-start;
    flex-direction: column;
  }

  .table-toolbar p {
    text-align: left;
  }
}

@media (max-width: 560px) {
  .registration-metrics {
    grid-template-columns: 1fr;
  }
}
</style>
