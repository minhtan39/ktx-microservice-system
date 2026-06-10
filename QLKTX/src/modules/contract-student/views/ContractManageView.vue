<template>
  <section>
    <div class="page-heading">
      <div>
        <span class="page-kicker">Contract Operations</span>
        <h2>Quản lý hợp đồng</h2>
      </div>
      <v-btn color="primary" variant="flat" prepend-icon="mdi-refresh" @click="loadAll">Làm mới</v-btn>
    </div>

    <v-card class="pa-5 mb-4 form-panel">
      <div class="panel-head">
        <div class="panel-icon">
          <span class="mdi mdi-file-plus-outline"></span>
        </div>
        <h3 class="section-title">Tạo hợp đồng thủ công</h3>
      </div>
      <v-form @submit.prevent="createContract">
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
            <v-text-field v-model.number="form.roomId" label="Room ID" type="number" density="compact" />
          </v-col>
          <v-col cols="12" md="2">
            <v-text-field v-model="form.startDate" label="Ngày bắt đầu" type="date" density="compact" />
          </v-col>
          <v-col cols="12" md="2">
            <v-text-field v-model="form.endDate" label="Ngày kết thúc" type="date" density="compact" />
          </v-col>
          <v-col cols="12" md="3">
            <v-text-field v-model="form.contractCode" label="Mã hợp đồng (có thể bỏ trống)" density="compact" />
          </v-col>
          <v-col cols="12" md="2">
            <v-text-field v-model.number="form.depositAmount" label="Tiền cọc" type="number" density="compact" />
          </v-col>
          <v-col cols="12" md="2">
            <v-text-field v-model.number="form.monthlyFee" label="Tiền phòng/tháng" type="number" density="compact" />
          </v-col>
          <v-col cols="12" md="6">
            <v-text-field v-model="form.terms" label="Điều khoản" density="compact" />
          </v-col>
          <v-col cols="12" md="2" class="d-flex align-center">
            <v-btn color="success" type="submit" :loading="saving">Lưu hợp đồng</v-btn>
          </v-col>
        </v-row>
      </v-form>
    </v-card>

    <v-alert v-if="message" :type="messageType" variant="tonal" class="mb-4">
      {{ message }}
    </v-alert>

    <v-card class="pa-4 mb-4 filter-panel">
      <v-row>
        <v-col cols="12" md="4">
          <v-text-field v-model="keyword" label="Tìm mã hợp đồng hoặc sinh viên" density="compact" clearable />
        </v-col>
        <v-col cols="12" md="3">
          <v-select
            v-model="statusFilter"
            :items="statusOptions"
            label="Lọc trạng thái"
            density="compact"
          />
        </v-col>
        <v-col cols="12" md="5" class="summary-row">
          <span>Tổng: {{ contracts.length }}</span>
          <span>Đang hiệu lực: {{ countByStatus('Active') }}</span>
          <span>Hết hạn: {{ countByStatus('Expired') }}</span>
        </v-col>
      </v-row>
    </v-card>

    <v-card class="table-card">
      <table class="data-table">
        <thead>
          <tr>
            <th>Mã hợp đồng</th>
            <th>Sinh viên</th>
            <th>Phòng</th>
            <th>Thời hạn</th>
            <th>Tiền cọc</th>
            <th>Tiền phòng</th>
            <th>Trạng thái</th>
            <th>Thao tác</th>
          </tr>
        </thead>
        <tbody>
          <tr v-if="loading">
            <td colspan="8">Đang tải dữ liệu...</td>
          </tr>
          <tr v-else-if="filteredContracts.length === 0">
            <td colspan="8">Chưa có hợp đồng phù hợp.</td>
          </tr>
          <tr v-for="contract in filteredContracts" :key="contract.id">
            <td>{{ contract.contractCode }}</td>
            <td>{{ studentName(contract.studentId) }}</td>
            <td>#{{ contract.roomId }}</td>
            <td>{{ formatDate(contract.startDate) }} - {{ formatDate(contract.endDate) }}</td>
            <td>{{ formatMoney(contract.depositAmount) }}</td>
            <td>{{ formatMoney(contract.monthlyFee) }}</td>
            <td>
              <span class="status-pill" :class="contract.status.toLowerCase()">
                {{ contract.status }}
              </span>
            </td>
            <td>
              <div class="action-row">
                <v-btn
                  color="warning"
                  size="small"
                  variant="tonal"
                  :disabled="contract.status !== 'Active'"
                  @click="cancelContract(contract.id)"
                >
                  Hủy
                </v-btn>
                <v-btn
                  color="primary"
                  size="small"
                  :disabled="contract.status !== 'Active'"
                  @click="expireContract(contract.id)"
                >
                  Kết thúc
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
const contracts = ref([])
const students = ref([])
const keyword = ref('')
const statusFilter = ref('All')

const statusOptions = ['All', 'Active', 'Cancelled', 'Expired']

const today = new Date().toISOString().slice(0, 10)
const nextYear = new Date()
nextYear.setFullYear(nextYear.getFullYear() + 1)

const emptyForm = () => ({
  contractCode: '',
  studentId: null,
  roomId: 101,
  startDate: today,
  endDate: nextYear.toISOString().slice(0, 10),
  depositAmount: 500000,
  monthlyFee: 800000,
  terms: '',
})

const form = ref(emptyForm())

const studentMap = computed(() => {
  return buildStudentNameMap(students.value)
})

const filteredContracts = computed(() => {
  const search = (keyword.value || '').trim().toLowerCase()

  return contracts.value.filter((contract) => {
    const matchesStatus = statusFilter.value === 'All' || contract.status === statusFilter.value
    const student = studentName(contract.studentId).toLowerCase()
    const matchesKeyword =
      !search ||
      contract.contractCode?.toLowerCase().includes(search) ||
      student.includes(search) ||
      String(contract.roomId).includes(search)

    return matchesStatus && matchesKeyword
  })
})

const showMessage = (text, type = 'success') => {
  message.value = text
  messageType.value = type
}

const loadStudents = async () => {
  const res = await api.get('/students')
  students.value = cleanStudents(res.data)
}

const loadContracts = async () => {
  const res = await api.get('/contracts')
  contracts.value = normalizeList(res.data)
}

const loadAll = async () => {
  try {
    loading.value = true
    message.value = ''
    await Promise.all([loadStudents(), loadContracts()])
  } catch (err) {
    showMessage('Không tải được dữ liệu hợp đồng.', 'error')
    console.error(err)
  } finally {
    loading.value = false
  }
}

const createContract = async () => {
  try {
    saving.value = true
    message.value = ''
    await api.post('/contracts', form.value)
    form.value = emptyForm()
    showMessage('Đã tạo hợp đồng.')
    await loadContracts()
  } catch (err) {
    showMessage('Không tạo được hợp đồng. Kiểm tra sinh viên, phòng và thời hạn.', 'error')
    console.error(err)
  } finally {
    saving.value = false
  }
}

const cancelContract = async (id) => {
  try {
    await api.put(`/contracts/${id}/cancel`)
    showMessage('Đã hủy hợp đồng.')
    await loadContracts()
  } catch (err) {
    showMessage('Không hủy được hợp đồng.', 'error')
    console.error(err)
  }
}

const expireContract = async (id) => {
  try {
    await api.put(`/contracts/${id}/expire`)
    showMessage('Đã kết thúc hợp đồng.')
    await loadContracts()
  } catch (err) {
    showMessage('Không kết thúc được hợp đồng.', 'error')
    console.error(err)
  }
}

const studentName = (id) => studentMap.value.get(id) || `Sinh viên #${id}`

const countByStatus = (status) => contracts.value.filter((contract) => contract.status === status).length

const formatDate = (value) => {
  if (!value) return ''
  return new Date(value).toLocaleDateString('vi-VN')
}

const formatMoney = (value) => {
  return Number(value || 0).toLocaleString('vi-VN', {
    style: 'currency',
    currency: 'VND',
  })
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
    linear-gradient(135deg, rgba(215, 139, 19, 0.10), transparent 40%),
    #ffffff;
}

.filter-panel {
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
  background: #fff4df;
  color: #a76a00;
  font-size: 23px;
}

.summary-row {
  display: flex;
  align-items: center;
  justify-content: flex-end;
  gap: 18px;
  color: #40576a;
  font-weight: 600;
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
  flex-wrap: wrap;
  gap: 8px;
}

.status-pill {
  display: inline-block;
  min-width: 86px;
  padding: 4px 10px;
  border-radius: 999px;
  text-align: center;
  font-weight: 700;
  background: #e8eef5;
  color: #34495e;
}

.status-pill.active {
  background: #e3f6ec;
  color: #12834c;
}

.status-pill.cancelled {
  background: #fff3dc;
  color: #9a6200;
}

.status-pill.expired {
  background: #fbe4e8;
  color: #b4233c;
}
</style>
