<template>
  <section>
    <div class="page-heading">
      <div>
        <span class="page-kicker">Student Profile</span>
        <h2>Quản lý sinh viên</h2>
      </div>
      <v-btn color="primary" variant="flat" prepend-icon="mdi-refresh" @click="loadStudents">Làm mới</v-btn>
    </div>

    <v-card class="pa-5 mb-4 form-panel">
      <div class="panel-head">
        <div class="panel-icon">
          <span class="mdi mdi-account-plus-outline"></span>
        </div>
        <h3 class="section-title">Thêm hồ sơ sinh viên</h3>
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
            <v-btn color="success" type="submit" :loading="saving">Lưu sinh viên</v-btn>
          </v-col>
        </v-row>
      </v-form>
    </v-card>

    <v-alert v-if="error" type="error" variant="tonal" class="mb-4">{{ error }}</v-alert>

    <v-card class="table-card">
      <table class="data-table">
        <thead>
          <tr>
            <th>MSSV</th>
            <th>Họ tên</th>
            <th>Khoa</th>
            <th>Lớp</th>
            <th>Liên hệ</th>
            <th>Trạng thái</th>
            <th>Lịch sử lưu trú</th>
          </tr>
        </thead>
        <tbody>
          <tr v-if="loading">
            <td colspan="7">Đang tải dữ liệu...</td>
          </tr>
          <tr v-for="student in students" :key="student.id">
            <td>{{ student.studentCode }}</td>
            <td>{{ student.fullName }}</td>
            <td>{{ student.facultyName }}</td>
            <td>{{ student.className }}</td>
            <td>{{ student.phone }}<br />{{ student.email }}</td>
            <td>{{ student.status }}</td>
            <td>{{ student.residenceHistory || 'Chưa có' }}</td>
          </tr>
        </tbody>
      </table>
    </v-card>
  </section>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import api from '@/services/api'
import { cleanStudents } from '../utils/studentDisplay'

const loading = ref(false)
const saving = ref(false)
const error = ref('')
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
    saving.value = true
    await api.post('/students', form.value)
    form.value = emptyForm()
    await loadStudents()
  } catch (err) {
    error.value = 'Không lưu được sinh viên. Kiểm tra MSSV, CCCD, email và các trường bắt buộc.'
    console.error(err)
  } finally {
    saving.value = false
  }
}

onMounted(loadStudents)
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
    linear-gradient(135deg, rgba(23, 107, 135, 0.07), transparent 38%),
    #ffffff;
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
  background: #e5f5f1;
  color: #128b73;
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
</style>
