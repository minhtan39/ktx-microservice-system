<template>
  <section class="student-page">
    <div class="page-heading">
      <div>
        <span class="page-kicker">Rubric 5 - Student Profile</span>
        <h2>Hồ sơ sinh viên</h2>
        <p>Lưu thông tin cá nhân, lớp, khoa và lịch sử lưu trú trước khi sinh viên đăng ký nội trú.</p>
      </div>
      <div class="heading-actions">
        <v-btn color="primary" variant="tonal" prepend-icon="mdi-refresh" :loading="loading" @click="loadStudents">
          Làm mới
        </v-btn>
        <v-btn color="success" variant="flat" prepend-icon="mdi-account-plus-outline" @click="openCreateDialog">
          Thêm sinh viên
        </v-btn>
      </div>
    </div>

    <div class="student-metrics">
      <article v-for="metric in profileMetrics" :key="metric.label" class="student-metric">
        <span :class="['mdi', metric.icon]"></span>
        <div>
          <strong>{{ metric.value }}</strong>
          <small>{{ metric.label }}</small>
        </div>
      </article>
    </div>

    <v-alert v-if="error" type="error" variant="tonal" class="mb-4">{{ error }}</v-alert>
    <v-alert v-if="success" type="success" variant="tonal" class="mb-4">{{ success }}</v-alert>

    <v-card class="filter-card">
      <div class="filter-head">
        <div>
          <span class="page-kicker">Student Filters</span>
          <h3>Lọc hồ sơ</h3>
        </div>
        <p>Nhấn vào một dòng để xem chi tiết hồ sơ và thông tin lưu trú.</p>
      </div>

      <div class="filter-grid">
        <v-text-field
          v-model="search"
          label="Tìm theo tên, MSSV, lớp hoặc email"
          density="compact"
          clearable
          prepend-inner-icon="mdi-magnify"
          hide-details
        />

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

        <v-btn variant="tonal" color="primary" prepend-icon="mdi-filter-remove-outline" @click="clearStudentFilters">
          Xóa lọc
        </v-btn>
      </div>
    </v-card>

    <v-card class="table-card">
      <div class="table-toolbar">
        <div>
          <span class="page-kicker">Student Directory</span>
          <h3>Danh sách hồ sơ</h3>
        </div>
        <div class="table-action-bar">
          <span class="table-count">{{ filteredStudents.length }} hồ sơ</span>
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
      </div>
      <table class="data-table compact-table">
        <thead>
          <tr>
            <th>Sinh viên</th>
            <th>Đào tạo</th>
            <th>Liên hệ</th>
            <th>Trạng thái</th>
            <th>Chi tiết</th>
          </tr>
        </thead>
        <tbody>
          <tr v-if="loading" class="table-empty">
            <td colspan="5">Đang tải dữ liệu...</td>
          </tr>
          <tr v-else-if="filteredStudents.length === 0" class="table-empty">
            <td colspan="5">
              <span class="empty-icon mdi mdi-account-search-outline"></span>
              Không tìm thấy sinh viên phù hợp.
            </td>
          </tr>
          <tr
            v-for="student in paginatedStudents"
            :key="student.id"
            class="click-row"
            @click="openStudentDetails(student)"
          >
            <td>
              <div class="student-cell">
                <span class="avatar-badge">{{ initials(student.fullName) }}</span>
                <div>
                  <strong class="cell-title">{{ student.fullName }}</strong>
                  <span class="cell-subtitle">{{ student.studentCode }} · {{ student.gender ? 'Nam' : 'Nữ' }}</span>
                </div>
              </div>
            </td>
            <td>
              <strong class="cell-title">{{ student.facultyName || 'Chưa cập nhật' }}</strong>
              <span class="cell-subtitle">{{ student.className || 'Chưa cập nhật lớp' }}</span>
            </td>
            <td>
              <strong class="cell-title">{{ student.phone || '-' }}</strong>
              <span class="cell-subtitle">{{ student.email || '-' }}</span>
            </td>
            <td>
              <span class="status-pill" :class="statusClass(student.status)">
                {{ statusLabel(student.status) }}
              </span>
            </td>
            <td>
              <v-btn color="primary" variant="tonal" size="small" icon="mdi-eye-outline" @click.stop="openStudentDetails(student)" />
            </td>
          </tr>
        </tbody>
      </table>

      <div v-if="filteredStudents.length > 0" class="pagination-row">
        <span>Hiển thị {{ pageStart }}-{{ pageEnd }} trên {{ filteredStudents.length }} sinh viên</span>
        <div class="pagination-actions">
          <v-btn icon="mdi-chevron-left" size="small" variant="tonal" :disabled="currentPage === 1" @click="currentPage -= 1" />
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
          <v-btn icon="mdi-chevron-right" size="small" variant="tonal" :disabled="currentPage === totalPages" @click="currentPage += 1" />
        </div>
      </div>
    </v-card>

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
                <v-text-field v-model="form.schoolName" label="Trường" density="compact" required />
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
          <v-btn color="primary" variant="tonal" @click="detailDialog = false">Đóng</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </section>
</template>

<script setup>
import { computed, onMounted, ref, watch } from 'vue'
import api from '@/services/api'
import { exportRowsToExcel } from '@/utils/exportExcel'
import { cleanStudents } from '../utils/studentDisplay'

const loading = ref(false)
const saving = ref(false)
const error = ref('')
const success = ref('')
const students = ref([])
const search = ref('')
const facultyFilter = ref('All')
const statusFilter = ref('All')
const currentPage = ref(1)
const createDialog = ref(false)
const detailDialog = ref(false)
const selectedStudent = ref(null)
const pageSize = 8

const genderOptions = [
  { title: 'Nam', value: true },
  { title: 'Nữ', value: false },
]

const studentStatusOptions = [
  { title: 'Tất cả trạng thái', value: 'All' },
  { title: 'Đang lưu trú', value: 'Active' },
  { title: 'Chờ đăng ký', value: 'Pending' },
  { title: 'Khác', value: 'Other' },
]

const emptyForm = () => ({
  studentCode: '',
  fullName: '',
  cccd: '',
  phone: '',
  email: '',
  schoolName: '',
  facultyName: '',
  className: '',
  gender: true,
})

const form = ref(emptyForm())

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
      (statusFilter.value === 'Other' && !['Active', 'Pending'].includes(student.status))

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

const selectedStudentFields = computed(() => {
  if (!selectedStudent.value) return []

  return [
    { label: 'Mã sinh viên', value: selectedStudent.value.studentCode || '-' },
    { label: 'Giới tính', value: selectedStudent.value.gender ? 'Nam' : 'Nữ' },
    { label: 'CCCD', value: selectedStudent.value.cccd || '-' },
    { label: 'Số điện thoại', value: selectedStudent.value.phone || '-' },
    { label: 'Email', value: selectedStudent.value.email || '-' },
    { label: 'Trường', value: selectedStudent.value.schoolName || '-' },
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

const createStudent = async () => {
  try {
    error.value = ''
    success.value = ''
    saving.value = true

    const payload = {
      ...form.value,
      studentCode: normalizeStudentCode(form.value.studentCode),
    }

    const res = await api.post('/students', payload)
    const createdStudent = res.data?.data || res.data || payload

    try {
      await api.post('/auth/student-accounts', {
        studentId: createdStudent.id,
        studentCode: createdStudent.studentCode || payload.studentCode,
        fullName: createdStudent.fullName || payload.fullName,
      })

      success.value = `Đã tạo hồ sơ và tài khoản sinh viên: ${payload.studentCode} / ${payload.studentCode}.`
    } catch (accountError) {
      success.value = `Đã tạo hồ sơ sinh viên. Tài khoản sẽ dùng mặc định ${payload.studentCode} / ${payload.studentCode} sau khi AuthService được deploy bản mới.`
      console.warn(accountError)
    }

    form.value = emptyForm()
    createDialog.value = false
    await loadStudents()
  } catch (err) {
    error.value = 'Không lưu được sinh viên. Kiểm tra MSSV, CCCD, email và các trường bắt buộc.'
    console.error(err)
  } finally {
    saving.value = false
  }
}

const openCreateDialog = () => {
  form.value = emptyForm()
  error.value = ''
  createDialog.value = true
}

const openStudentDetails = (student) => {
  selectedStudent.value = student
  detailDialog.value = true
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
  if (status === 'Pending') return 'Chờ đăng ký'
  return status || 'Chưa xác định'
}

const statusClass = (status) => String(status || 'pending').toLowerCase()

onMounted(loadStudents)
</script>

<style scoped>
.student-page {
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
  max-width: 760px;
  margin: 8px 0 0;
  color: var(--muted);
  font-size: 15px;
  line-height: 1.5;
}

.heading-actions {
  display: flex;
  align-items: center;
  gap: 10px;
  flex-wrap: wrap;
  justify-content: flex-end;
}

.student-metrics {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: 14px;
}

.student-metric {
  display: grid;
  grid-template-columns: 46px minmax(0, 1fr);
  align-items: center;
  gap: 14px;
  min-height: 92px;
  padding: 18px;
  border: 1px solid rgba(15, 127, 81, 0.14);
  border-radius: 8px;
  background:
    linear-gradient(135deg, rgba(22, 155, 99, 0.08), transparent 50%),
    #ffffff;
}

.student-metric .mdi {
  display: grid;
  place-items: center;
  width: 46px;
  height: 46px;
  border-radius: 8px;
  background: #ecfdf5;
  color: var(--brand-dark);
  font-size: 24px;
}

.student-metric strong,
.student-metric small {
  display: block;
}

.student-metric strong {
  color: var(--ink);
  font-family: var(--font-heading);
  font-size: 28px;
  line-height: 1;
}

.student-metric small {
  margin-top: 6px;
  color: var(--muted);
  font-size: 13px;
  font-weight: 800;
}

.filter-card {
  padding: 18px;
  background: #ffffff;
}

.filter-head,
.table-toolbar {
  display: flex;
  align-items: flex-end;
  justify-content: space-between;
  gap: 18px;
}

.filter-head {
  margin-bottom: 16px;
}

.filter-head h3,
.filter-head p,
.table-toolbar h3,
.table-toolbar p {
  margin: 0;
}

.filter-head h3,
.table-toolbar h3 {
  color: var(--ink);
  font-family: var(--font-heading);
  font-size: 20px;
}

.filter-head p,
.table-toolbar p {
  max-width: 520px;
  color: var(--muted);
  font-size: 13px;
  line-height: 1.45;
  text-align: right;
}

.filter-grid {
  display: grid;
  grid-template-columns: minmax(260px, 1fr) 220px 190px auto;
  align-items: center;
  gap: 12px;
}

.table-card {
  overflow: hidden;
}

.table-toolbar {
  padding: 20px 22px;
  border-bottom: 1px solid var(--line);
  background: #ffffff;
}

.table-empty td {
  padding: 34px 18px;
  color: var(--muted);
  font-weight: 800;
  text-align: center;
}

.empty-icon {
  display: block;
  margin-bottom: 8px;
  color: var(--brand);
  font-size: 26px;
}

.click-row {
  cursor: pointer;
}

.student-cell {
  display: flex;
  align-items: center;
  gap: 12px;
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

.avatar-badge.large {
  width: 48px;
  height: 48px;
  font-size: 15px !important;
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
  .filter-grid {
    grid-template-columns: 1fr 1fr;
  }
}

@media (max-width: 860px) {
  .page-heading {
    align-items: stretch;
    flex-direction: column;
  }

  .heading-actions {
    justify-content: flex-start;
  }

  .student-metrics {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }

  .filter-head,
  .table-toolbar,
  .pagination-row {
    align-items: flex-start;
    flex-direction: column;
  }

  .filter-head p,
  .table-toolbar p {
    text-align: left;
  }

  .filter-grid,
  .info-grid {
    grid-template-columns: 1fr;
  }
}

@media (max-width: 560px) {
  .student-metrics {
    grid-template-columns: 1fr;
  }
}
</style>
