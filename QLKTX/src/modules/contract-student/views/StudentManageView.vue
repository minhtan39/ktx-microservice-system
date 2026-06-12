<template>
  <section class="student-page">
    <div class="page-heading">
      <div>
        <span class="page-kicker">Rubric 5 - Student Profile</span>
        <h2>Hồ sơ sinh viên</h2>
        <p>Lưu thông tin cá nhân, lớp, khoa và lịch sử lưu trú trước khi sinh viên đăng ký nội trú.</p>
      </div>
      <v-btn color="primary" variant="flat" prepend-icon="mdi-refresh" :loading="loading" @click="loadStudents">
        Làm mới
      </v-btn>
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

    <v-card class="pa-5 mb-4 form-panel">
      <div class="panel-head">
        <div class="panel-icon">
          <span class="mdi mdi-account-plus-outline"></span>
        </div>
        <div>
          <h3 class="section-title">Thêm hồ sơ sinh viên</h3>
          <p>Mỗi hồ sơ sẽ được cấp tài khoản đăng nhập mặc định: tên đăng nhập và mật khẩu đều là MSSV.</p>
        </div>
      </div>
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
          <v-col cols="12" md="3" class="d-flex align-center">
            <v-btn color="success" type="submit" :loading="saving">Lưu & tạo tài khoản</v-btn>
          </v-col>
        </v-row>
      </v-form>
    </v-card>

    <v-alert v-if="error" type="error" variant="tonal" class="mb-4">{{ error }}</v-alert>
    <v-alert v-if="success" type="success" variant="tonal" class="mb-4">{{ success }}</v-alert>

    <v-card class="table-card">
      <div class="table-toolbar">
        <div>
          <span class="page-kicker">Student Directory</span>
          <h3>Danh sách hồ sơ</h3>
        </div>
        <p>Tài khoản sinh viên được tạo theo MSSV để liên thông AuthService.</p>
      </div>
      <table class="data-table">
        <thead>
          <tr>
            <th>MSSV</th>
            <th>Họ tên</th>
            <th>Khoa</th>
            <th>Lớp</th>
            <th>Liên hệ</th>
            <th>Trạng thái</th>
            <th>Tài khoản</th>
            <th>Lịch sử lưu trú</th>
          </tr>
        </thead>
        <tbody>
          <tr v-if="loading" class="table-empty">
            <td colspan="8">Đang tải dữ liệu...</td>
          </tr>
          <tr v-else-if="students.length === 0" class="table-empty">
            <td colspan="8">Chưa có hồ sơ sinh viên hợp lệ.</td>
          </tr>
          <tr v-for="student in students" :key="student.id">
            <td>{{ student.studentCode }}</td>
            <td>
              <strong>{{ student.fullName }}</strong>
              <span>{{ student.gender ? 'Nam' : 'Nữ' }}</span>
            </td>
            <td>{{ student.facultyName }}</td>
            <td>{{ student.className }}</td>
            <td>{{ student.phone }}<br />{{ student.email }}</td>
            <td>
              <span class="status-pill" :class="statusClass(student.status)">
                {{ statusLabel(student.status) }}
              </span>
            </td>
            <td>
              <strong>{{ student.studentCode }}</strong>
              <span>Mật khẩu mặc định: {{ student.studentCode }}</span>
            </td>
            <td>{{ student.residenceHistory || 'Chưa có lịch sử lưu trú' }}</td>
          </tr>
        </tbody>
      </table>
    </v-card>
  </section>
</template>

<script setup>
import { computed, onMounted, ref } from 'vue'
import api from '@/services/api'
import { cleanStudents } from '../utils/studentDisplay'

const loading = ref(false)
const saving = ref(false)
const error = ref('')
const success = ref('')
const students = ref([])

const genderOptions = [
  { title: 'Nam', value: true },
  { title: 'Nữ', value: false },
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
    await loadStudents()
  } catch (err) {
    error.value = 'Không lưu được sinh viên. Kiểm tra MSSV, CCCD, email và các trường bắt buộc.'
    console.error(err)
  } finally {
    saving.value = false
  }
}

const normalizeStudentCode = (value) => String(value || '').trim().toUpperCase()

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

.section-title {
  margin: 0;
  color: #14532d;
}

.form-panel {
  background:
    linear-gradient(135deg, rgba(22, 155, 99, 0.08), transparent 38%),
    #ffffff;
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
  max-width: 420px;
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

@media (max-width: 860px) {
  .page-heading {
    align-items: stretch;
    flex-direction: column;
  }

  .student-metrics {
    grid-template-columns: repeat(2, minmax(0, 1fr));
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
  .student-metrics {
    grid-template-columns: 1fr;
  }
}
</style>
