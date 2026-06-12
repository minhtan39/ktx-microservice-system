<template>
  <section class="contract-page">
    <div class="contract-hero">
      <div>
        <span class="hero-kicker">Rubric 8 - Contract Registry</span>
        <h2>Hợp đồng thuê phòng</h2>
        <p>
          Đây là sổ hợp đồng được sinh sau khi đơn đăng ký được duyệt. Màn này
          dùng để chứng minh N2 tự khởi tạo hợp đồng, thời hạn ở, tiền cọc và
          tiền phòng.
        </p>
      </div>

      <v-btn color="primary" variant="flat" prepend-icon="mdi-refresh" :loading="loading" @click="loadAll">
        Làm mới
      </v-btn>
    </div>

    <div class="metric-strip">
      <div class="metric-chip">
        <span>Tổng hợp đồng</span>
        <strong>{{ contracts.length }}</strong>
      </div>
      <div class="metric-chip success">
        <span>Đang hiệu lực</span>
        <strong>{{ countByStatus('Active') }}</strong>
      </div>
      <div class="metric-chip danger">
        <span>Hết hạn</span>
        <strong>{{ countByStatus('Expired') }}</strong>
      </div>
      <div class="metric-chip warning">
        <span>Đã hủy</span>
        <strong>{{ countByStatus('Cancelled') }}</strong>
      </div>
    </div>

    <v-alert v-if="message" :type="messageType" variant="tonal" class="mb-4">
      {{ message }}
    </v-alert>

    <v-card class="filter-card">
      <div class="filter-grid">
        <v-text-field
          v-model="keyword"
          label="Tìm theo mã hợp đồng, sinh viên hoặc phòng"
          density="compact"
          clearable
          prepend-inner-icon="mdi-magnify"
        />

        <v-select
          v-model="statusFilter"
          :items="statusOptions"
          item-title="title"
          item-value="value"
          label="Trạng thái"
          density="compact"
        />
      </div>
    </v-card>

    <v-card class="table-card desktop-table">
      <div class="table-toolbar">
        <div>
          <span class="hero-kicker">Contract Registry</span>
          <h3>Sổ hợp đồng đã phát hành</h3>
        </div>
        <p>Chỉ hiển thị hợp đồng được tạo sau bước duyệt xếp phòng; trạng thái từ chối không đi vào sổ hợp đồng.</p>
      </div>
      <table class="data-table">
        <thead>
          <tr>
            <th>Mã hợp đồng</th>
            <th>Sinh viên</th>
            <th>Phòng</th>
            <th>Thời hạn</th>
            <th>Tiền cọc</th>
            <th>Tiền phòng</th>
            <th>Điều khoản</th>
            <th>Trạng thái</th>
          </tr>
        </thead>
        <tbody>
          <tr v-if="loading" class="table-empty">
            <td colspan="8">Đang tải dữ liệu...</td>
          </tr>
          <tr v-else-if="filteredContracts.length === 0" class="table-empty">
            <td colspan="8">Chưa có hợp đồng phù hợp.</td>
          </tr>
          <tr v-for="contract in filteredContracts" :key="contract.id">
            <td>
              <strong class="contract-code">{{ contract.contractCode }}</strong>
            </td>
            <td>{{ studentName(contract.studentId) }}</td>
            <td>Phòng {{ contract.roomId }}</td>
            <td>{{ formatDate(contract.startDate) }} - {{ formatDate(contract.endDate) }}</td>
            <td>{{ formatMoney(contract.depositAmount) }}</td>
            <td>{{ formatMoney(contract.monthlyFee) }}</td>
            <td class="terms-cell">{{ contract.terms || 'Điều khoản mặc định' }}</td>
            <td>
              <span class="status-pill" :class="statusClass(contract.status)">
                {{ statusText(contract.status) }}
              </span>
            </td>
          </tr>
        </tbody>
      </table>
    </v-card>

    <div class="contract-cards">
      <article v-if="loading" class="contract-card muted-card">
        Đang tải dữ liệu...
      </article>
      <article v-else-if="filteredContracts.length === 0" class="contract-card muted-card">
        Chưa có hợp đồng phù hợp.
      </article>
      <article v-for="contract in filteredContracts" :key="contract.id" class="contract-card">
        <div class="card-top">
          <div>
            <span class="mini-label">Mã hợp đồng</span>
            <strong>{{ contract.contractCode }}</strong>
          </div>
          <span class="status-pill" :class="statusClass(contract.status)">
            {{ statusText(contract.status) }}
          </span>
        </div>

        <div class="card-grid">
          <div>
            <span>Sinh viên</span>
            <strong>{{ studentName(contract.studentId) }}</strong>
          </div>
          <div>
            <span>Phòng</span>
            <strong>{{ contract.roomId }}</strong>
          </div>
          <div>
            <span>Thời hạn</span>
            <strong>{{ formatDate(contract.startDate) }} - {{ formatDate(contract.endDate) }}</strong>
          </div>
          <div>
            <span>Tiền phòng</span>
            <strong>{{ formatMoney(contract.monthlyFee) }}</strong>
          </div>
        </div>

        <p>{{ contract.terms || 'Điều khoản mặc định' }}</p>
      </article>
    </div>
  </section>
</template>

<script setup>
import { computed, onMounted, ref } from 'vue'
import api from '@/services/api'
import {
  buildStudentNameMap,
  cleanStudents,
  normalizeList,
} from '../utils/studentDisplay'

const loading = ref(false)
const message = ref('')
const messageType = ref('success')
const contracts = ref([])
const students = ref([])
const keyword = ref('')
const statusFilter = ref('All')

const statusOptions = [
  { title: 'Tất cả', value: 'All' },
  { title: 'Đang hiệu lực', value: 'Active' },
  { title: 'Đã hủy', value: 'Cancelled' },
  { title: 'Hết hạn', value: 'Expired' },
]

const studentMap = computed(() => buildStudentNameMap(students.value))

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
    showMessage('Không tải được danh sách hợp đồng.', 'error')
    console.error(err)
  } finally {
    loading.value = false
  }
}

const studentName = (id) => studentMap.value.get(id) || `Sinh viên #${id}`
const countByStatus = (status) => contracts.value.filter((contract) => contract.status === status).length
const statusClass = (status) => String(status || '').toLowerCase()

const statusText = (status) => {
  if (status === 'Active') return 'Hiệu lực'
  if (status === 'Expired') return 'Hết hạn'
  if (status === 'Cancelled') return 'Đã hủy'
  return status
}

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
.contract-page {
  display: flex;
  flex-direction: column;
  gap: 18px;
}

.contract-hero {
  display: flex;
  justify-content: space-between;
  gap: 18px;
  align-items: center;
  min-height: 148px;
  padding: 26px;
  border: 1px solid var(--line);
  border-radius: 8px;
  background:
    linear-gradient(135deg, rgba(22, 155, 99, 0.13), transparent 42%),
    linear-gradient(90deg, rgba(255, 255, 255, 0.92), rgba(247, 250, 248, 0.78)),
    #ffffff;
}

.hero-kicker {
  display: block;
  margin-bottom: 8px;
  color: var(--brand-dark);
  font-size: 12px;
  font-weight: 900;
  letter-spacing: 0.08em;
  text-transform: uppercase;
}

.contract-hero h2 {
  margin: 0;
  color: var(--ink);
  font-size: 30px;
  line-height: 1.1;
}

.contract-hero p {
  max-width: 760px;
  margin: 10px 0 0;
  color: var(--muted);
  font-size: 15px;
  line-height: 1.5;
}

.metric-strip {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: 14px;
}

.metric-chip {
  min-height: 92px;
  padding: 18px;
  border: 1px solid var(--line);
  border-radius: 8px;
  background:
    linear-gradient(180deg, #ffffff, #fbfdfb);
}

.metric-chip span,
.mini-label,
.card-grid span {
  display: block;
  color: var(--muted);
  font-size: 12px;
  font-weight: 900;
  letter-spacing: 0.04em;
  text-transform: uppercase;
}

.metric-chip strong {
  display: block;
  margin-top: 8px;
  color: var(--ink);
  font-size: 30px;
  line-height: 1;
}

.metric-chip.success strong {
  color: #15803d;
}

.metric-chip.danger strong {
  color: #be123c;
}

.metric-chip.warning strong {
  color: #b45309;
}

.filter-card {
  padding: 18px;
  background:
    linear-gradient(135deg, rgba(37, 99, 235, 0.05), transparent 44%),
    #ffffff;
}

.filter-grid {
  display: grid;
  grid-template-columns: minmax(0, 1fr) 220px;
  gap: 14px;
  align-items: start;
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
  max-width: 520px;
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

.contract-code {
  color: var(--brand-dark);
}

.terms-cell {
  max-width: 360px;
  color: var(--muted);
  line-height: 1.45;
}

.status-pill {
  display: inline-block;
  min-width: 86px;
  padding: 5px 10px;
  border-radius: 999px;
  text-align: center;
  font-size: 12px;
  font-weight: 900;
  background: #e8eef5;
  color: #34495e;
}

.status-pill.active {
  background: #dcfce7;
  color: #15803d;
}

.status-pill.cancelled {
  background: #fef3c7;
  color: #b45309;
}

.status-pill.expired {
  background: #ffe4e6;
  color: #be123c;
}

.contract-cards {
  display: none;
}

.contract-card {
  padding: 18px;
  border: 1px solid var(--line);
  border-radius: 8px;
  background: #ffffff;
}

.muted-card {
  color: var(--muted);
  font-weight: 700;
}

.card-top {
  display: flex;
  justify-content: space-between;
  gap: 12px;
  align-items: flex-start;
  margin-bottom: 16px;
}

.card-top strong {
  display: block;
  margin-top: 5px;
  color: var(--brand-dark);
  font-size: 18px;
}

.card-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 14px;
}

.card-grid strong {
  display: block;
  margin-top: 5px;
  color: var(--ink);
  line-height: 1.35;
}

.contract-card p {
  margin: 16px 0 0;
  color: var(--muted);
  line-height: 1.5;
}

@media (max-width: 1120px) {
  .metric-strip {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
}

@media (max-width: 760px) {
  .contract-hero {
    align-items: flex-start;
    flex-direction: column;
    padding: 20px;
  }

  .contract-hero h2 {
    font-size: 25px;
  }

  .metric-strip,
  .filter-grid {
    grid-template-columns: 1fr;
  }

  .desktop-table {
    display: none;
  }

  .contract-cards {
    display: grid;
    gap: 14px;
  }

  .table-toolbar {
    align-items: flex-start;
    flex-direction: column;
  }

  .table-toolbar p {
    text-align: left;
  }
}
</style>
