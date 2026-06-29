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
      <div class="metric-chip info">
        <span>Chờ ký online</span>
        <strong>{{ countByStatus('PendingSignature') }}</strong>
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

        <v-select
          v-model="buildingFilter"
          :items="buildingOptions"
          item-title="title"
          item-value="value"
          label="Tòa"
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
        <div class="table-action-bar">
          <span class="table-count">{{ filteredContracts.length }} hợp đồng</span>
          <v-btn
            color="primary"
            variant="tonal"
            prepend-icon="mdi-file-excel-outline"
            :disabled="filteredContracts.length === 0"
            @click="exportContracts"
          >
            Xuất Excel
          </v-btn>
        </div>
      </div>
      <table class="data-table compact-table">
        <thead>
          <tr>
            <th>Mã hợp đồng</th>
            <th>Sinh viên</th>
            <th>Phòng</th>
            <th>Thời hạn</th>
            <th>Tài chính</th>
            <th>Trạng thái</th>
            <th>Xem</th>
          </tr>
        </thead>
        <tbody>
          <tr v-if="loading" class="table-empty">
            <td colspan="7">Đang tải dữ liệu...</td>
          </tr>
          <tr v-else-if="filteredContracts.length === 0" class="table-empty">
            <td colspan="7">Chưa có hợp đồng phù hợp.</td>
          </tr>
          <tr v-for="contract in paginatedContracts" :key="contract.id">
            <td>
              <strong class="cell-title contract-code">{{ contract.contractCode }}</strong>
              <span class="cell-subtitle">ID: {{ contract.id }}</span>
            </td>
            <td>
              <strong class="cell-title">{{ studentName(contract.studentId) }}</strong>
              <span class="cell-subtitle">SV ID: {{ contract.studentId }}</span>
            </td>
            <td>
              <strong class="cell-title">{{ roomLabel(contract.roomId) }}</strong>
              <span class="cell-subtitle">Mã phòng: {{ contract.roomId }}</span>
            </td>
            <td>
              <strong class="cell-title">{{ formatDate(contract.startDate) }}</strong>
              <span class="cell-subtitle">đến {{ formatDate(contract.endDate) }}</span>
            </td>
            <td>
              <strong class="cell-title">{{ formatMoney(contract.monthlyFee) }}</strong>
              <span class="cell-subtitle">Cọc: {{ formatMoney(contract.depositAmount) }}</span>
            </td>
            <td>
              <span class="status-pill" :class="statusClass(contract.status)">
                {{ statusText(contract.status) }}
              </span>
              <span class="cell-subtitle">
                {{ contract.signedAt ? `Đã ký ${formatDate(contract.signedAt)}` : 'Chưa ký online' }}
              </span>
            </td>
            <td>
              <v-btn
                color="primary"
                variant="tonal"
                size="small"
                icon="mdi-arrow-right"
                @click="openContractDetails(contract)"
              />
            </td>
          </tr>
        </tbody>
      </table>
      <div class="pagination-row">
        <span>Hiển thị {{ pageStart }} - {{ pageEnd }} / {{ filteredContracts.length }} hợp đồng</span>
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

    <div class="contract-cards">
      <article v-if="loading" class="contract-card muted-card">
        Đang tải dữ liệu...
      </article>
      <article v-else-if="filteredContracts.length === 0" class="contract-card muted-card">
        Chưa có hợp đồng phù hợp.
      </article>
      <article v-for="contract in paginatedContracts" :key="contract.id" class="contract-card">
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
            <strong>{{ roomLabel(contract.roomId) }}</strong>
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
        <v-btn class="mt-4" color="primary" variant="tonal" size="small" @click="openContractDetails(contract)">
          Xem chi tiết
        </v-btn>
      </article>
    </div>

    <v-dialog v-model="detailDialog" max-width="900">
      <v-card v-if="selectedContract" class="dialog-card">
        <v-card-title class="dialog-title">
          <div>
            <span class="hero-kicker">Contract Details</span>
            <strong>{{ selectedContract.contractCode }}</strong>
          </div>
          <v-btn icon="mdi-close" variant="text" @click="detailDialog = false" />
        </v-card-title>
        <v-card-text>
          <div class="detail-strip">
            <span class="status-pill" :class="statusClass(selectedContract.status)">
              {{ statusText(selectedContract.status) }}
            </span>
            <span>{{ studentName(selectedContract.studentId) }}</span>
            <span>{{ roomLabel(selectedContract.roomId) }}</span>
          </div>

          <div class="contract-detail-grid">
            <div v-for="field in selectedContractFields" :key="field.label">
              <span>{{ field.label }}</span>
              <strong>{{ field.value }}</strong>
            </div>
          </div>

          <section class="terms-box">
            <span>Điều khoản</span>
            <p>{{ selectedContract.terms || 'Điều khoản mặc định về thời hạn ở, tiền cọc, tiền phòng và trách nhiệm sinh viên.' }}</p>
          </section>
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
const rooms = ref([])
const keyword = ref('')
const statusFilter = ref('All')
const buildingFilter = ref('All')
const detailDialog = ref(false)
const selectedContract = ref(null)
const currentPage = ref(1)
const pageSize = 8

const statusOptions = [
  { title: 'Tất cả', value: 'All' },
  { title: 'Đang hiệu lực', value: 'Active' },
  { title: 'Chờ ký online', value: 'PendingSignature' },
  { title: 'Đã hủy', value: 'Cancelled' },
  { title: 'Hết hạn', value: 'Expired' },
]

const studentMap = computed(() => buildStudentNameMap(students.value))
const roomMap = computed(() => new Map(rooms.value.map((room) => [Number(room.roomId), room])))
const buildingOptions = computed(() => {
  const buildings = [...new Set(rooms.value.map((room) => room.buildingName).filter(Boolean))]
    .sort((first, second) => first.localeCompare(second, 'vi'))

  return [
    { title: 'Tất cả tòa', value: 'All' },
    ...buildings.map((building) => ({ title: `Tòa ${building}`, value: building })),
  ]
})

const filteredContracts = computed(() => {
  const search = (keyword.value || '').trim().toLowerCase()

  return contracts.value.filter((contract) => {
    const matchesStatus = statusFilter.value === 'All' || contract.status === statusFilter.value
    const student = studentName(contract.studentId).toLowerCase()
    const room = roomMap.value.get(Number(contract.roomId))
    const matchesBuilding = buildingFilter.value === 'All' || room?.buildingName === buildingFilter.value
    const matchesKeyword =
      !search ||
      contract.contractCode?.toLowerCase().includes(search) ||
      student.includes(search) ||
      String(contract.roomId).includes(search) ||
      roomLabel(contract.roomId).toLowerCase().includes(search)

    return matchesStatus && matchesBuilding && matchesKeyword
  })
})

const totalPages = computed(() => Math.max(1, Math.ceil(filteredContracts.value.length / pageSize)))
const paginatedContracts = computed(() => {
  const start = (currentPage.value - 1) * pageSize
  return filteredContracts.value.slice(start, start + pageSize)
})
const pageStart = computed(() =>
  filteredContracts.value.length === 0 ? 0 : (currentPage.value - 1) * pageSize + 1)
const pageEnd = computed(() =>
  Math.min(currentPage.value * pageSize, filteredContracts.value.length))

const selectedContractFields = computed(() => {
  if (!selectedContract.value) return []

  return [
    { label: 'Sinh viên', value: studentName(selectedContract.value.studentId) },
    { label: 'Phòng', value: roomLabel(selectedContract.value.roomId) },
    { label: 'Ngày bắt đầu', value: formatDate(selectedContract.value.startDate) },
    { label: 'Ngày kết thúc', value: formatDate(selectedContract.value.endDate) },
    { label: 'Tiền cọc', value: formatMoney(selectedContract.value.depositAmount) },
    { label: 'Tiền phòng tháng', value: formatMoney(selectedContract.value.monthlyFee) },
    { label: 'Trạng thái', value: statusText(selectedContract.value.status) },
    { label: 'Ký online', value: selectedContract.value.signedAt ? `Đã ký ${formatDate(selectedContract.value.signedAt)}` : 'Chưa ký online' },
    { label: 'Người ký', value: selectedContract.value.signatureFullName || '-' },
    { label: 'Số lần gia hạn', value: selectedContract.value.renewalCount || 0 },
    { label: 'Mã tham chiếu phòng', value: selectedContract.value.roomId || '-' },
  ]
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

const loadRooms = async () => {
  const res = await api.get('/rooms')
  rooms.value = normalizeList(res.data)
}

const loadAll = async () => {
  try {
    loading.value = true
    message.value = ''
    await Promise.all([loadStudents(), loadContracts(), loadRooms()])
  } catch (err) {
    showMessage('Không tải được danh sách hợp đồng.', 'error')
    console.error(err)
  } finally {
    loading.value = false
  }
}

watch([keyword, statusFilter, buildingFilter], () => {
  currentPage.value = 1
})

watch(filteredContracts, () => {
  if (currentPage.value > totalPages.value) {
    currentPage.value = totalPages.value
  }
})

const exportContracts = () => {
  exportRowsToExcel({
    filename: 'danh-sach-hop-dong.xls',
    sheetName: 'Danh sách hợp đồng',
    rows: filteredContracts.value,
    columns: [
      { header: 'Mã hợp đồng', value: (contract) => contract.contractCode },
      { header: 'Sinh viên', value: (contract) => studentName(contract.studentId) },
      { header: 'Phòng', value: (contract) => roomLabel(contract.roomId) },
      { header: 'Ngày bắt đầu', value: (contract) => formatDate(contract.startDate) },
      { header: 'Ngày kết thúc', value: (contract) => formatDate(contract.endDate) },
      { header: 'Tiền cọc', value: (contract) => formatMoney(contract.depositAmount) },
      { header: 'Tiền phòng tháng', value: (contract) => formatMoney(contract.monthlyFee) },
      { header: 'Trạng thái', value: (contract) => statusText(contract.status) },
      { header: 'Ký online', value: (contract) => contract.signedAt ? `Đã ký ${formatDate(contract.signedAt)}` : 'Chưa ký' },
      { header: 'Số lần gia hạn', value: (contract) => contract.renewalCount || 0 },
      { header: 'Điều khoản', value: (contract) => contract.terms || 'Điều khoản mặc định' },
    ],
  })
}

const studentName = (id) => studentMap.value.get(id) || `Sinh viên #${id}`
const roomLabel = (id) => {
  const room = roomMap.value.get(Number(id))
  if (!room) return `Phòng ${id}`

  return `Phòng ${room.roomNumber || room.roomId} - Tòa ${room.buildingName}`
}

const openContractDetails = (contract) => {
  selectedContract.value = contract
  detailDialog.value = true
}

const countByStatus = (status) => contracts.value.filter((contract) => contract.status === status).length
const statusClass = (status) => String(status || '').toLowerCase()

const statusText = (status) => {
  if (status === 'Active') return 'Hiệu lực'
  if (status === 'PendingSignature') return 'Chờ ký online'
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
  grid-template-columns: minmax(0, 1fr) 220px 180px;
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
  font-size: 22px;
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

.contract-detail-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 12px;
}

.contract-detail-grid div,
.terms-box {
  padding: 13px;
  border: 1px solid var(--line);
  border-radius: 8px;
  background: #fbfdfb;
}

.contract-detail-grid span,
.contract-detail-grid strong,
.terms-box span {
  display: block;
}

.contract-detail-grid span,
.terms-box span {
  color: var(--muted);
  font-size: 12px;
  font-weight: 900;
}

.contract-detail-grid strong {
  margin-top: 5px;
  color: var(--ink);
  font-size: 14px;
  line-height: 1.4;
}

.terms-box {
  margin-top: 12px;
}

.terms-box p {
  margin: 6px 0 0;
  color: var(--ink);
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

  .contract-detail-grid {
    grid-template-columns: 1fr;
  }
}
</style>
