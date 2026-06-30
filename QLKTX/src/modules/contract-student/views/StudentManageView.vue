<template>
  <section class="student-page">
    <div class="student-hero">
      <div class="hero-copy">
        <span class="page-kicker">Student Profile</span>
        <h2>Hồ sơ sinh viên</h2>
        <p>Quản lý hồ sơ, lớp, khoa và lịch sử lưu trú theo dạng roster gọn. Bấm một sinh viên để đọc chi tiết thay vì nhìn một bảng quá dày.</p>
        <div class="heading-actions">
          <v-btn color="primary" variant="tonal" prepend-icon="mdi-refresh" :loading="loading" @click="loadAll">
            Làm mới
          </v-btn>
          <v-btn color="success" variant="flat" prepend-icon="mdi-account-plus-outline" @click="openCreateDialog">
            Thêm sinh viên
          </v-btn>
        </div>
      </div>

      <div class="hero-stats">
        <article v-for="metric in profileMetrics" :key="metric.label" class="stat-tile">
          <span :class="['mdi', metric.icon]"></span>
          <div>
            <strong>{{ metric.value }}</strong>
            <small>{{ metric.label }}</small>
          </div>
        </article>
      </div>
    </div>

    <v-alert v-if="error" type="error" variant="tonal" class="mb-4">{{ error }}</v-alert>
    <v-alert v-if="success" type="success" variant="tonal" class="mb-4">{{ success }}</v-alert>

    <div class="student-workspace">
      <section class="roster-panel">
        <div class="roster-toolbar">
          <div>
            <span class="page-kicker">Student Roster</span>
            <h3>{{ filteredStudents.length }} hồ sơ phù hợp</h3>
          </div>
          <v-btn
            color="primary"
            variant="tonal"
            prepend-icon="mdi-file-excel-outline"
            :disabled="filteredStudents.length === 0"
            @click="exportStudents"
          >
            Xuất Excel
          </v-btn>
        </div>

        <div class="search-shell">
          <v-text-field
            v-model="search"
            label="Tìm tên, MSSV, lớp hoặc email"
            density="comfortable"
            clearable
            prepend-inner-icon="mdi-magnify"
            hide-details
          />
          <div class="filter-row">
            <v-select
              v-model="facultyFilter"
              :items="facultyOptions"
              item-title="title"
              item-value="value"
              label="Khoa"
              density="compact"
              hide-details
            />
            <v-select
              v-model="statusFilter"
              :items="studentStatusOptions"
              item-title="title"
              item-value="value"
              label="Trạng thái"
              density="compact"
              hide-details
            />
            <v-btn variant="tonal" color="primary" icon="mdi-filter-remove-outline" title="Xóa lọc" @click="clearStudentFilters" />
          </div>
        </div>

        <div class="student-list">
          <div v-if="loading" class="empty-state">
            <span class="mdi mdi-loading mdi-spin"></span>
            Đang tải dữ liệu sinh viên...
          </div>
          <div v-else-if="filteredStudents.length === 0" class="empty-state">
            <span class="mdi mdi-account-search-outline"></span>
            Không tìm thấy sinh viên phù hợp.
          </div>
          <template v-else>
            <button
              v-for="student in paginatedStudents"
              :key="student.id"
              type="button"
              class="student-row-card"
              :class="{ active: highlightedStudent?.id === student.id }"
              @click="openStudentDetails(student)"
            >
              <span class="avatar-badge">{{ initials(student.fullName) }}</span>
              <span class="student-main">
                <strong>{{ student.fullName }}</strong>
                <small>{{ student.studentCode }} · {{ student.className || 'Chưa cập nhật lớp' }}</small>
              </span>
              <span class="student-meta">
                <span class="status-pill" :class="statusClass(student.status)">
                  {{ statusLabel(student.status) }}
                </span>
                <small>{{ student.facultyName || 'Chưa có khoa' }}</small>
              </span>
            </button>
          </template>
        </div>

        <div v-if="filteredStudents.length > 0" class="pagination-row">
          <span>{{ pageStart }}-{{ pageEnd }} / {{ filteredStudents.length }} sinh viên</span>
          <div class="pagination-actions">
            <button class="page-button" type="button" :disabled="currentPage === 1" @click="currentPage -= 1">
              &lt;
            </button>
            <button
              v-for="page in totalPages"
              :key="page"
              type="button"
              class="page-button"
              :class="{ active: currentPage === page }"
              @click="currentPage = page"
            >
              {{ page }}
            </button>
            <button class="page-button" type="button" :disabled="currentPage === totalPages" @click="currentPage += 1">
              &gt;
            </button>
          </div>
        </div>
      </section>

    </div>

    <v-dialog v-model="createDialog" max-width="980">
      <v-card class="dialog-card">
        <v-card-title class="dialog-title">
          <div>
            <span class="page-kicker">Create Student</span>
            <strong>Thêm sinh viên mới</strong>
          </div>
          <v-btn icon="mdi-close" variant="text" @click="createDialog = false" />
        </v-card-title>
        <v-card-text>
          <v-alert
            v-if="dialogError"
            type="error"
            variant="tonal"
            closable
            class="mb-4"
            @click:close="dialogError = ''"
          >
            {{ dialogError }}
          </v-alert>
          <v-form @submit.prevent="createStudent">
            <v-row>
              <v-col cols="12" md="3">
                <v-text-field v-model="form.studentCode" label="Mã sinh viên" density="compact" required />
              </v-col>
              <v-col cols="12" md="3">
                <v-text-field v-model="form.fullName" label="Họ tên" density="compact" required />
              </v-col>
              <v-col cols="12" md="3">
                <v-text-field v-model="form.cccd" label="CCCD" density="compact" required />
              </v-col>
              <v-col cols="12" md="3">
                <v-text-field v-model="form.phone" label="Số điện thoại" density="compact" required />
              </v-col>
              <v-col cols="12" md="3">
                <v-text-field v-model="form.email" label="Email" density="compact" required />
              </v-col>
              <v-col cols="12" md="3">
                <v-text-field v-model="form.facultyName" label="Khoa" density="compact" required />
              </v-col>
              <v-col cols="12" md="3">
                <v-text-field v-model="form.className" label="Lớp" density="compact" required />
              </v-col>
              <v-col cols="12" md="3">
                <v-select
                  v-model="form.gender"
                  :items="genderOptions"
                  item-title="title"
                  item-value="value"
                  label="Giới tính"
                  density="compact"
                />
              </v-col>
              <v-col cols="12">
                <v-textarea
                  v-model="form.residenceHistory"
                  label="Lịch sử lưu trú"
                  rows="3"
                  density="compact"
                  placeholder="Ví dụ: Chưa lưu trú, đang chờ xếp phòng hoặc ghi chú lịch sử trước đây"
                />
              </v-col>
            </v-row>
          </v-form>
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="createDialog = false">Hủy</v-btn>
          <v-btn color="success" :loading="saving" prepend-icon="mdi-content-save-outline" @click="createStudent">
            Lưu & tạo tài khoản
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <v-dialog v-model="editDialog" max-width="980">
      <v-card class="dialog-card">
        <v-card-title class="dialog-title">
          <div>
            <span class="page-kicker">Update Student</span>
            <strong>Cập nhật hồ sơ sinh viên</strong>
          </div>
          <v-btn icon="mdi-close" variant="text" @click="editDialog = false" />
        </v-card-title>
        <v-card-text>
          <v-alert
            v-if="dialogError"
            type="error"
            variant="tonal"
            closable
            class="mb-4"
            @click:close="dialogError = ''"
          >
            {{ dialogError }}
          </v-alert>
          <v-form @submit.prevent="updateStudent">
            <v-row>
              <v-col cols="12" md="3">
                <v-text-field v-model="editForm.studentCode" label="Mã sinh viên" density="compact" disabled />
              </v-col>
              <v-col cols="12" md="3">
                <v-text-field v-model="editForm.fullName" label="Họ tên" density="compact" required />
              </v-col>
              <v-col cols="12" md="3">
                <v-text-field v-model="editForm.cccd" label="CCCD" density="compact" required />
              </v-col>
              <v-col cols="12" md="3">
                <v-text-field v-model="editForm.phone" label="Số điện thoại" density="compact" required />
              </v-col>
              <v-col cols="12" md="3">
                <v-text-field v-model="editForm.email" label="Email" density="compact" required />
              </v-col>
              <v-col cols="12" md="3">
                <v-text-field v-model="editForm.facultyName" label="Khoa" density="compact" required />
              </v-col>
              <v-col cols="12" md="3">
                <v-text-field v-model="editForm.className" label="Lớp" density="compact" required />
              </v-col>
              <v-col cols="12" md="3">
                <v-select
                  v-model="editForm.gender"
                  :items="genderOptions"
                  item-title="title"
                  item-value="value"
                  label="Giới tính"
                  density="compact"
                />
              </v-col>
              <v-col cols="12">
                <v-textarea
                  v-model="editForm.residenceHistory"
                  label="Lịch sử lưu trú"
                  rows="4"
                  density="compact"
                  placeholder="Ghi nhận phòng, tòa, thời gian ở hoặc lịch sử chuyển phòng"
                />
              </v-col>
            </v-row>
          </v-form>
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="editDialog = false">Hủy</v-btn>
          <v-btn color="success" :loading="saving" prepend-icon="mdi-content-save-outline" @click="updateStudent">
            Lưu cập nhật
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <v-dialog v-model="detailDialog" max-width="900">
      <v-card v-if="selectedStudent" class="dialog-card">
        <v-card-title class="dialog-title">
          <div class="detail-title">
            <span class="avatar-badge large">{{ initials(selectedStudent.fullName) }}</span>
            <div>
              <strong>{{ selectedStudent.fullName }}</strong>
              <span>{{ selectedStudent.studentCode }}</span>
            </div>
          </div>
          <v-btn icon="mdi-close" variant="text" @click="detailDialog = false" />
        </v-card-title>
        <v-card-text>
          <div class="detail-strip">
            <span class="status-pill" :class="statusClass(selectedStudent.status)">
              {{ statusLabel(selectedStudent.status) }}
            </span>
            <span>{{ selectedStudent.facultyName || 'Chưa cập nhật khoa' }}</span>
            <span>{{ selectedStudent.className || 'Chưa cập nhật lớp' }}</span>
          </div>

          <div class="info-grid">
            <div v-for="field in selectedStudentFields" :key="field.label" class="info-field">
              <span>{{ field.label }}</span>
              <strong>{{ field.value }}</strong>
            </div>
          </div>
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn
            v-if="currentRoomForStudent(selectedStudent)"
            color="info"
            variant="tonal"
            prepend-icon="mdi-cube-scan"
            @click="openRoomMapperForStudent(selectedStudent)"
          >
            Sơ đồ giường
          </v-btn>
          <v-btn color="primary" variant="tonal" @click="detailDialog = false">Đóng</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <RoomBedMapperDialog
      v-model="roomMapperDialog"
      :room="roomMapperRoom"
      :students="students"
      :contracts="contracts"
      :focus-student-id="roomMapperFocusStudentId"
      readonly
      @room-updated="handleRoomUpdated"
    />
  </section>
</template>

<script setup>
import { computed, onMounted, ref, watch } from 'vue'
import api from '@/services/api'
import { exportRowsToExcel } from '@/utils/exportExcel'
import RoomBedMapperDialog from '../components/RoomBedMapperDialog.vue'
import { cleanStudents } from '../utils/studentDisplay'

const loading = ref(false)
const saving = ref(false)
const error = ref('')
const success = ref('')
const dialogError = ref('')
const students = ref([])
const rooms = ref([])
const contracts = ref([])
const search = ref('')
const facultyFilter = ref('All')
const statusFilter = ref('All')
const currentPage = ref(1)
const createDialog = ref(false)
const editDialog = ref(false)
const detailDialog = ref(false)
const roomMapperDialog = ref(false)
const roomMapperRoom = ref(null)
const roomMapperFocusStudentId = ref(null)
const selectedStudent = ref(null)
const pageSize = 8
const managedSchoolName = 'Trường quản lý ký túc xá'

const genderOptions = [
  { title: 'Nam', value: true },
  { title: 'Nữ', value: false },
]

const studentStatusOptions = [
  { title: 'Tất cả trạng thái', value: 'All' },
  { title: 'Đang lưu trú', value: 'Active' },
  { title: 'Chờ ký hợp đồng', value: 'PendingSignature' },
  { title: 'Chờ đăng ký', value: 'Pending' },
  { title: 'Khác', value: 'Other' },
]

const emptyForm = () => ({
  studentCode: '',
  fullName: '',
  cccd: '',
  phone: '',
  email: '',
  schoolName: managedSchoolName,
  facultyName: '',
  className: '',
  residenceHistory: '',
  gender: true,
})

const form = ref(emptyForm())
const editForm = ref({ ...emptyForm(), id: null })

const activeStudents = computed(() => students.value.filter((student) => student.status === 'Active').length)
const pendingStudents = computed(() => students.value.filter((student) => student.status !== 'Active').length)
const accountReady = computed(() => students.value.filter((student) => student.studentCode).length)
const profileMetrics = computed(() => [
  {
    icon: 'mdi-account-multiple-outline',
    value: students.value.length,
    label: 'Hồ sơ đang quản lý',
  },
  {
    icon: 'mdi-bed-outline',
    value: activeStudents.value,
    label: 'Đang lưu trú',
  },
  {
    icon: 'mdi-clipboard-clock-outline',
    value: pendingStudents.value,
    label: 'Chờ đăng ký nội trú',
  },
  {
    icon: 'mdi-account-key-outline',
    value: accountReady.value,
    label: 'Tài khoản MSSV sẵn sàng',
  },
])

const facultyOptions = computed(() => {
  const faculties = [...new Set(students.value.map((student) => student.facultyName).filter(Boolean))]
    .sort((first, second) => first.localeCompare(second, 'vi'))

  return [
    { title: 'Tất cả khoa', value: 'All' },
    ...faculties.map((faculty) => ({ title: faculty, value: faculty })),
  ]
})

const filteredStudents = computed(() => {
  const keyword = search.value.trim().toLowerCase()

  return students.value.filter((student) => {
    const haystack = [
      student.studentCode,
      student.fullName,
      student.className,
      student.facultyName,
      student.phone,
      student.email,
    ].join(' ').toLowerCase()
    const matchesSearch = !keyword || haystack.includes(keyword)
    const matchesFaculty = facultyFilter.value === 'All' || student.facultyName === facultyFilter.value
    const matchesStatus =
      statusFilter.value === 'All' ||
      student.status === statusFilter.value ||
      (statusFilter.value === 'Other' && !['Active', 'Pending', 'PendingSignature'].includes(student.status))

    return matchesSearch && matchesFaculty && matchesStatus
  })
})

const totalPages = computed(() => Math.max(1, Math.ceil(filteredStudents.value.length / pageSize)))
const paginatedStudents = computed(() => {
  const start = (currentPage.value - 1) * pageSize
  return filteredStudents.value.slice(start, start + pageSize)
})
const pageStart = computed(() => filteredStudents.value.length === 0 ? 0 : (currentPage.value - 1) * pageSize + 1)
const pageEnd = computed(() => Math.min(currentPage.value * pageSize, filteredStudents.value.length))
const highlightedStudent = computed(() => {
  if (selectedStudent.value && filteredStudents.value.some((student) => student.id === selectedStudent.value.id)) {
    return selectedStudent.value
  }

  return null
})

const selectedStudentFields = computed(() => {
  if (!selectedStudent.value) return []

  return [
    { label: 'Mã sinh viên', value: selectedStudent.value.studentCode || '-' },
    { label: 'Giới tính', value: selectedStudent.value.gender ? 'Nam' : 'Nữ' },
    { label: 'CCCD', value: selectedStudent.value.cccd || '-' },
    { label: 'Số điện thoại', value: selectedStudent.value.phone || '-' },
    { label: 'Email', value: selectedStudent.value.email || '-' },
    { label: 'Khoa', value: selectedStudent.value.facultyName || '-' },
    { label: 'Lớp', value: selectedStudent.value.className || '-' },
    { label: 'Tài khoản mặc định', value: `${selectedStudent.value.studentCode || '-'} / ${selectedStudent.value.studentCode || '-'}` },
    { label: 'Lịch sử lưu trú', value: selectedStudent.value.residenceHistory || 'Chưa có lịch sử lưu trú' },
  ]
})

watch([search, facultyFilter, statusFilter], () => {
  currentPage.value = 1
})

watch(filteredStudents, () => {
  if (currentPage.value > totalPages.value) currentPage.value = totalPages.value
})

const loadStudents = async () => {
  try {
    error.value = ''
    loading.value = true
    const res = await api.get('/students')
    students.value = cleanStudents(res.data)
  } catch (err) {
    error.value = 'Không tải được danh sách sinh viên.'
    console.error(err)
  } finally {
    loading.value = false
  }
}

const loadRooms = async () => {
  const res = await api.get('/rooms')
  rooms.value = normalizeList(res.data).map(normalizeRoom)
}

const loadContracts = async () => {
  const res = await api.get('/contracts')
  contracts.value = normalizeList(res.data)
}

const loadAll = async () => {
  try {
    error.value = ''
    loading.value = true
    const [studentRes, roomRes, contractRes] = await Promise.all([
      api.get('/students'),
      api.get('/rooms').catch(() => ({ data: [] })),
      api.get('/contracts').catch(() => ({ data: [] })),
    ])

    students.value = cleanStudents(studentRes.data)
    rooms.value = normalizeList(roomRes.data).map(normalizeRoom)
    contracts.value = normalizeList(contractRes.data)
  } catch (err) {
    error.value = 'Không tải được dữ liệu hồ sơ sinh viên.'
    console.error(err)
  } finally {
    loading.value = false
  }
}

const normalizeList = (payload) => {
  if (Array.isArray(payload)) return payload
  if (Array.isArray(payload?.data)) return payload.data
  if (Array.isArray(payload?.value)) return payload.value
  return []
}

const normalizeRoom = (room) => {
  const capacity = Number(room.capacity ?? 0)
  const occupiedBeds = Number(room.occupiedBeds ?? room.currentOccupancy ?? 0)
  const availableBeds = Number(room.availableBeds ?? Math.max(capacity - occupiedBeds, 0))
  const buildingName = String(room.buildingName ?? '').replace(/^Tòa\s*/i, '')

  return {
    ...room,
    roomId: Number(room.roomId ?? room.id ?? 0),
    roomNumber: String(room.roomNumber ?? room.name ?? room.roomId ?? ''),
    buildingName,
    buildingDisplayName: room.buildingDisplayName || `Tòa ${buildingName}`,
    floor: Number(room.floor ?? 1),
    floorName: room.floorName || `Tầng ${room.floor ?? 1}`,
    roomType: room.roomType ?? '',
    gender: parseGender(room.gender),
    genderText: room.genderText || (parseGender(room.gender) ? 'Nam' : 'Nữ'),
    capacity,
    occupiedBeds,
    availableBeds,
    monthlyFee: Number(room.monthlyFee ?? room.price ?? 0),
    status: room.status || (availableBeds > 0 ? 'Available' : 'Full'),
    amenities: room.amenities || '',
    occupancyReferences: Array.isArray(room.occupancyReferences) ? room.occupancyReferences : [],
  }
}

const parseGender = (value) => {
  if (typeof value === 'boolean') return value

  const normalized = String(value || '').toLowerCase()
  return normalized === 'true' || normalized === 'nam' || normalized === 'male'
}

const createStudent = async () => {
  try {
    error.value = ''
    success.value = ''
    dialogError.value = ''
    saving.value = true

    const normalizedStudentCode = normalizeStudentCode(form.value.studentCode)

    if (students.value.some((student) =>
      normalizeStudentCode(student.studentCode) === normalizedStudentCode)) {
      dialogError.value = `MSSV ${normalizedStudentCode} đã tồn tại. Không thể tạo trùng hồ sơ hoặc tài khoản.`
      return
    }

    const payload = {
      ...form.value,
      studentCode: normalizedStudentCode,
      schoolName: managedSchoolName,
    }

    const res = await api.post('/students', payload)
    const createdStudent = res.data?.data || res.data || payload

    try {
      const accountResponse = await api.post('/auth/student-accounts', {
        studentId: createdStudent.id,
        studentCode: createdStudent.studentCode || payload.studentCode,
        fullName: createdStudent.fullName || payload.fullName,
        email: createdStudent.email || payload.email,
        phone: createdStudent.phone || payload.phone,
      })

      const accountUsername = accountResponse.data?.data?.username || payload.studentCode

      try {
        await api.post(`/auth/accounts/${encodeURIComponent(accountUsername)}/access-link`)
        success.value = `Đã tạo hồ sơ, tài khoản và gửi liên kết đặt mật khẩu đến email sinh viên ${payload.studentCode}.`
      } catch (emailError) {
        const emailMessage = emailError.response?.data?.detail || emailError.response?.data?.message
        success.value = `Đã tạo hồ sơ và tài khoản chờ kích hoạt. Chưa gửi được email: ${emailMessage || 'hãy kiểm tra Gmail và email sinh viên.'}`
      }
    } catch (accountError) {
      const accountMessage = accountError.response?.data?.message
      if (accountError.response?.status === 409) {
        success.value = `Đã tạo hồ sơ sinh viên ${payload.studentCode}. Tài khoản đăng nhập MSSV này đã tồn tại nên hệ thống không tạo trùng.`
      } else {
        success.value = `Đã tạo hồ sơ sinh viên. Chưa tạo được tài khoản đăng nhập: ${accountMessage || 'AuthService chưa sẵn sàng.'}`
      }
      console.warn(accountError)
    }

    form.value = emptyForm()
    createDialog.value = false
    await loadStudents()
  } catch (err) {
    dialogError.value = err.response?.data?.message || 'Không lưu được sinh viên. Kiểm tra MSSV, CCCD, email và các trường bắt buộc.'
    console.error(err)
  } finally {
    saving.value = false
  }
}

const updateStudent = async () => {
  if (!editForm.value.id) return

  try {
    error.value = ''
    success.value = ''
    dialogError.value = ''
    saving.value = true

    const payload = {
      fullName: editForm.value.fullName,
      cccd: editForm.value.cccd,
      phone: editForm.value.phone,
      email: editForm.value.email,
      schoolName: managedSchoolName,
      facultyName: editForm.value.facultyName,
      className: editForm.value.className,
      residenceHistory: editForm.value.residenceHistory,
      gender: editForm.value.gender,
    }

    const res = await api.put(`/students/${editForm.value.id}`, payload)
    const updatedStudent = res.data?.data || res.data
    const updatedId = Number(updatedStudent?.id || editForm.value.id)

    success.value = `Đã cập nhật hồ sơ sinh viên ${editForm.value.studentCode}.`
    editDialog.value = false
    await loadStudents()
    selectedStudent.value = students.value.find((student) => Number(student.id) === updatedId) || null
  } catch (err) {
    dialogError.value = err.response?.data?.message || 'Không cập nhật được hồ sơ. Kiểm tra CCCD, email và các trường bắt buộc.'
    console.error(err)
  } finally {
    saving.value = false
  }
}

const openCreateDialog = () => {
  form.value = emptyForm()
  error.value = ''
  dialogError.value = ''
  createDialog.value = true
}

const openEditDialog = (student) => {
  if (!student) return

  editForm.value = mapStudentToForm(student)
  error.value = ''
  success.value = ''
  dialogError.value = ''
  editDialog.value = true
}

const openStudentDetails = (student) => {
  selectedStudent.value = student
  detailDialog.value = true
}

const activeContractForStudent = (student) => {
  if (!student) return null

  return contracts.value
    .filter((contract) =>
      Number(contract.studentId) === Number(student.id) &&
      ['active', 'pendingsignature'].includes(String(contract.status || '').toLowerCase()))
    .sort((first, second) => new Date(second.startDate || 0) - new Date(first.startDate || 0))[0] || null
}

const currentRoomForStudent = (student) => {
  const contract = activeContractForStudent(student)
  if (!contract) return null

  return rooms.value.find((room) => Number(room.roomId) === Number(contract.roomId)) || null
}

const openRoomMapperForStudent = (student) => {
  const room = currentRoomForStudent(student)

  if (!room) {
    error.value = 'Sinh viên này chưa có phòng đang lưu trú hoặc hợp đồng chờ ký.'
    return
  }

  selectedStudent.value = student
  roomMapperRoom.value = room
  roomMapperFocusStudentId.value = student.id
  roomMapperDialog.value = true
}

const handleRoomUpdated = (updatedRoom) => {
  const normalizedRoom = normalizeRoom(updatedRoom)
  const index = rooms.value.findIndex((room) => Number(room.roomId) === Number(normalizedRoom.roomId))

  if (index >= 0) {
    rooms.value.splice(index, 1, normalizedRoom)
  }

  if (Number(roomMapperRoom.value?.roomId) === Number(normalizedRoom.roomId)) {
    roomMapperRoom.value = normalizedRoom
  }
}

const clearStudentFilters = () => {
  search.value = ''
  facultyFilter.value = 'All'
  statusFilter.value = 'All'
}

const exportStudents = () => {
  exportRowsToExcel({
    filename: 'danh-sach-sinh-vien.xls',
    sheetName: 'Danh sách sinh viên',
    rows: filteredStudents.value,
    columns: [
      { header: 'MSSV', value: (student) => student.studentCode },
      { header: 'Họ tên', value: (student) => student.fullName },
      { header: 'Giới tính', value: (student) => student.gender ? 'Nam' : 'Nữ' },
      { header: 'Khoa', value: (student) => student.facultyName },
      { header: 'Lớp', value: (student) => student.className },
      { header: 'Số điện thoại', value: (student) => student.phone },
      { header: 'Email', value: (student) => student.email },
      { header: 'CCCD', value: (student) => student.cccd },
      { header: 'Trạng thái', value: (student) => statusLabel(student.status) },
      { header: 'Tài khoản mặc định', value: (student) => student.studentCode },
      { header: 'Lịch sử lưu trú', value: (student) => student.residenceHistory || 'Chưa có lịch sử lưu trú' },
    ],
  })
}

const mapStudentToForm = (student) => ({
  id: student.id,
  studentCode: student.studentCode || '',
  fullName: student.fullName || '',
  cccd: student.cccd || '',
  phone: student.phone || '',
  email: student.email || '',
  schoolName: student.schoolName || managedSchoolName,
  facultyName: student.facultyName || '',
  className: student.className || '',
  residenceHistory: student.residenceHistory || '',
  gender: Boolean(student.gender),
})

const normalizeStudentCode = (value) => String(value || '').trim().toUpperCase()

const initials = (name) => {
  return String(name || 'SV')
    .trim()
    .split(/\s+/)
    .slice(-2)
    .map((part) => part[0])
    .join('')
    .toUpperCase()
}

const statusLabel = (status) => {
  if (status === 'Active') return 'Đang lưu trú'
  if (status === 'PendingSignature') return 'Chờ ký hợp đồng'
  if (status === 'Pending') return 'Chờ đăng ký'
  return status || 'Chưa xác định'
}

const statusClass = (status) => String(status || 'pending').toLowerCase()

onMounted(loadAll)
</script>

<style scoped>
.student-page {
  display: grid;
  gap: 18px;
}

.student-hero {
  display: grid;
  grid-template-columns: minmax(0, 1.15fr) minmax(360px, 0.85fr);
  gap: 18px;
  min-height: 250px;
  padding: 28px;
  border: 1px solid rgba(15, 127, 81, 0.14);
  border-radius: 8px;
  background:
    linear-gradient(135deg, rgba(22, 155, 99, 0.13), transparent 42%),
    linear-gradient(180deg, #ffffff, #f8fbf9);
  box-shadow: 0 14px 35px rgba(17, 24, 39, 0.06);
}

.hero-copy {
  display: flex;
  flex-direction: column;
  justify-content: center;
  min-width: 0;
}

.hero-copy h2 {
  max-width: 620px;
  margin: 0;
  color: var(--ink);
  font-family: var(--font-heading);
  font-size: clamp(34px, 4vw, 54px);
  line-height: 1.02;
}

.hero-copy p {
  max-width: 640px;
  margin: 16px 0 0;
  color: var(--muted);
  font-size: 16px;
  line-height: 1.65;
}

.heading-actions {
  display: flex;
  align-items: center;
  gap: 10px;
  flex-wrap: wrap;
  margin-top: 26px;
}

.hero-stats {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 12px;
  align-content: center;
}

.stat-tile {
  display: grid;
  grid-template-columns: 42px minmax(0, 1fr);
  align-items: center;
  gap: 12px;
  min-height: 94px;
  padding: 16px;
  border: 1px solid rgba(15, 127, 81, 0.12);
  border-radius: 8px;
  background: rgba(255, 255, 255, 0.78);
  backdrop-filter: blur(8px);
}

.stat-tile .mdi {
  display: grid;
  place-items: center;
  width: 42px;
  height: 42px;
  border-radius: 8px;
  background: #ecfdf5;
  color: var(--brand-dark);
  font-size: 22px;
}

.stat-tile strong,
.stat-tile small {
  display: block;
}

.stat-tile strong {
  color: var(--ink);
  font-family: var(--font-heading);
  font-size: 30px;
  line-height: 1;
}

.stat-tile small {
  margin-top: 6px;
  color: var(--muted);
  font-size: 13px;
  font-weight: 800;
}

.student-workspace {
  display: block;
}

.roster-panel {
  border: 1px solid var(--line);
  border-radius: 8px;
  background: #ffffff;
  box-shadow: 0 10px 28px rgba(17, 24, 39, 0.05);
}

.roster-panel {
  overflow: hidden;
}

.roster-toolbar {
  display: flex;
  align-items: flex-end;
  justify-content: space-between;
  gap: 16px;
  padding: 20px 22px 16px;
  border-bottom: 1px solid var(--line);
}

.roster-toolbar h3 {
  margin: 0;
  color: var(--ink);
  font-family: var(--font-heading);
  font-size: 24px;
}

.search-shell {
  display: grid;
  gap: 12px;
  padding: 16px 22px;
  background: #fbfdfc;
}

.filter-row {
  display: grid;
  grid-template-columns: minmax(0, 1fr) minmax(0, 1fr) 44px;
  gap: 10px;
  align-items: center;
}

.student-list {
  display: grid;
  gap: 0;
}

.student-row-card {
  display: grid;
  grid-template-columns: 44px minmax(0, 1fr) minmax(160px, auto);
  align-items: center;
  gap: 14px;
  width: 100%;
  padding: 17px 22px;
  border: 0;
  border-bottom: 1px solid var(--line);
  background: #ffffff;
  color: inherit;
  cursor: pointer;
  text-align: left;
  transition: background 0.15s ease, box-shadow 0.15s ease;
}

.student-row-card:hover,
.student-row-card.active {
  background: #f3faf6;
}

.student-row-card.active {
  box-shadow: inset 4px 0 0 var(--brand);
}

.avatar-badge {
  display: inline-grid !important;
  place-items: center;
  width: 38px;
  height: 38px;
  flex: 0 0 auto;
  border-radius: 999px;
  background: #eaf8f0;
  color: #0f7a44 !important;
  font-size: 13px !important;
  font-weight: 900;
}

.student-main,
.student-meta {
  display: grid;
  gap: 5px;
  min-width: 0;
}

.student-main strong {
  overflow: hidden;
  color: var(--ink);
  font-size: 15px;
  font-weight: 900;
  line-height: 1.3;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.student-main small,
.student-meta small {
  overflow: hidden;
  color: var(--muted);
  font-size: 12px;
  font-weight: 700;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.student-meta {
  justify-items: end;
  text-align: right;
}

.status-pill {
  display: inline-flex !important;
  align-items: center;
  min-height: 28px;
  padding: 0 10px;
  border-radius: 999px;
  background: #fef3c7;
  color: #b45309 !important;
  font-size: 12px !important;
  font-weight: 900;
}

.status-pill.active {
  background: #dcfce7;
  color: #15803d !important;
}

.status-pill.pendingsignature {
  background: #dbeafe;
  color: #1d4ed8 !important;
}

.empty-state {
  display: grid;
  place-items: center;
  gap: 10px;
  min-height: 280px;
  padding: 28px;
  color: var(--muted);
  font-weight: 900;
  text-align: center;
}

.empty-state .mdi {
  color: var(--brand);
  font-size: 34px;
}

.pagination-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  padding: 16px 20px;
  border-top: 1px solid var(--line);
  color: var(--muted);
  font-size: 13px;
  font-weight: 800;
  background: #ffffff;
}

.pagination-actions {
  display: flex;
  align-items: center;
  gap: 6px;
}

.page-button {
  width: 32px;
  height: 32px;
  border: 1px solid var(--line);
  border-radius: 8px;
  background: #ffffff;
  color: var(--muted);
  cursor: pointer;
  font-weight: 900;
}

.page-button.active {
  border-color: var(--brand);
  background: var(--brand);
  color: #ffffff;
}

.dialog-card {
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

.detail-title {
  display: flex;
  align-items: center;
  gap: 12px;
}

.detail-title span:not(.avatar-badge) {
  display: block;
  margin-top: 4px;
  color: var(--muted);
  font-size: 13px;
}

.detail-strip {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 10px;
  margin-bottom: 18px;
}

.detail-strip > span:not(.status-pill) {
  display: inline-flex;
  align-items: center;
  min-height: 28px;
  padding: 0 10px;
  border-radius: 999px;
  background: #f7faf8;
  color: var(--muted);
  font-size: 12px;
  font-weight: 900;
}

.info-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 12px;
}

.info-field {
  padding: 13px;
  border: 1px solid var(--line);
  border-radius: 8px;
  background: #fbfdfb;
}

.info-field span,
.info-field strong {
  display: block;
}

.info-field span {
  color: var(--muted);
  font-size: 12px;
  font-weight: 800;
}

.info-field strong {
  margin-top: 5px;
  color: var(--ink);
  font-size: 14px;
  line-height: 1.4;
}

@media (max-width: 980px) {
  .student-hero {
    grid-template-columns: 1fr;
  }
}

@media (max-width: 860px) {
  .hero-stats {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }

  .roster-toolbar,
  .pagination-row {
    align-items: flex-start;
    flex-direction: column;
  }

  .info-grid {
    grid-template-columns: 1fr;
  }
}

@media (max-width: 560px) {
  .student-hero {
    padding: 20px;
  }

  .hero-copy h2 {
    font-size: 32px;
  }

  .hero-stats,
  .filter-row,
  .student-row-card {
    grid-template-columns: 1fr;
  }

  .student-meta {
    justify-items: start;
    text-align: left;
  }
}
</style>
