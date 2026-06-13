<template>
  <section class="contract-ops-page">
    <div class="page-heading">
      <div>
        <span class="page-kicker">Contract Operations</span>
        <h2>Vận hành hợp đồng</h2>
        <p>
          Hợp đồng chính được tạo tự động ở bước duyệt xếp phòng. Màn này chỉ
          dùng để theo dõi, hủy hoặc kết thúc hợp đồng khi có thay đổi nghiệp vụ.
        </p>
      </div>
      <v-btn color="primary" variant="flat" prepend-icon="mdi-refresh" :loading="loading" @click="loadAll">
        Làm mới
      </v-btn>
    </div>

    <section class="ops-explainer">
      <article>
        <span class="mdi mdi-clipboard-check-outline"></span>
        <strong>Nguồn tạo hợp đồng</strong>
        <p>Duyệt đơn đăng ký sẽ tự gọi RoomService, xếp phòng và tạo hợp đồng.</p>
      </article>
      <article>
        <span class="mdi mdi-cash-sync"></span>
        <strong>Liên thông Billing</strong>
        <p>Sau khi hợp đồng được tạo, N2 gửi tiền cọc và tiền phòng tháng đầu sang BillingService.</p>
      </article>
      <article>
        <span class="mdi mdi-file-cog-outline"></span>
        <strong>Quản trị trạng thái</strong>
        <p>Chỉ hợp đồng đang hiệu lực mới được hủy hoặc kết thúc.</p>
      </article>
    </section>

    <section class="contract-alerts">
      <article class="warning">
        <span class="mdi mdi-alert-outline"></span>
        <div>
          <strong>{{ expiringSoonCount }} hợp đồng sắp hết hạn</strong>
          <p>Những hợp đồng có ngày kết thúc trong 30 ngày tới cần được kiểm tra.</p>
        </div>
      </article>
      <article class="success">
        <span class="mdi mdi-file-check-outline"></span>
        <div>
          <strong>{{ activeCount }} hợp đồng đang hiệu lực</strong>
          <p>Các hợp đồng này có thể hủy hoặc kết thúc khi phát sinh nghiệp vụ.</p>
        </div>
      </article>
    </section>

    <v-alert v-if="message" :type="messageType" variant="tonal" class="mb-4">
      {{ message }}
    </v-alert>

    <v-card class="pa-4 mb-4 filter-panel">
      <div class="filter-heading">
        <div>
          <span class="page-kicker">Contract Control</span>
          <h3>Kiểm soát trạng thái hợp đồng</h3>
        </div>
        <p>Trang này dành cho xử lý sau khi hợp đồng đã được phát hành.</p>
      </div>
      <v-row>
        <v-col cols="12" md="4">
          <v-text-field
            v-model="keyword"
            label="Tìm mã hợp đồng, sinh viên hoặc phòng"
            density="compact"
            clearable
            prepend-inner-icon="mdi-magnify"
          />
        </v-col>
        <v-col cols="12" md="3">
          <v-select
            v-model="statusFilter"
            :items="statusOptions"
            item-title="title"
            item-value="value"
            label="Lọc trạng thái"
            density="compact"
          />
        </v-col>
        <v-col cols="12" md="2">
          <v-select
            v-model="buildingFilter"
            :items="buildingOptions"
            item-title="title"
            item-value="value"
            label="Tòa"
            density="compact"
          />
        </v-col>
        <v-col cols="12" md="3" class="summary-row">
          <span>Tổng: {{ contracts.length }}</span>
          <span>Hiệu lực: {{ countByStatus('Active') }}</span>
          <span>Hết hạn: {{ countByStatus('Expired') }}</span>
        </v-col>
      </v-row>
    </v-card>

    <v-card class="table-card">
      <div class="table-toolbar">
        <div>
          <span class="page-kicker">Operations Queue</span>
          <h3>Hợp đồng cần theo dõi</h3>
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
            <th>Thao tác</th>
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
const currentPage = ref(1)
const pageSize = 8

const statusOptions = [
  { title: 'Tất cả', value: 'All' },
  { title: 'Đang hiệu lực', value: 'Active' },
  { title: 'Đã hủy', value: 'Cancelled' },
  { title: 'Hết hạn', value: 'Expired' },
]

const studentMap = computed(() => buildStudentNameMap(students.value))
const roomMap = computed(() => new Map(rooms.value.map((room) => [Number(room.roomId), room])))
const activeCount = computed(() => countByStatus('Active'))
const expiringSoonCount = computed(() => {
  const now = new Date()
  const next30Days = new Date()
  next30Days.setDate(now.getDate() + 30)

  return contracts.value.filter((contract) => {
    if (contract.status !== 'Active' || !contract.endDate) return false
    const endDate = new Date(contract.endDate)
    return endDate >= now && endDate <= next30Days
  }).length
})
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
    showMessage('Không tải được dữ liệu hợp đồng.', 'error')
    console.error(err)
  } finally {
    loading.value = false
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
    filename: 'quan-ly-hop-dong.xls',
    sheetName: 'Quản lý hợp đồng',
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
    ],
  })
}

const studentName = (id) => studentMap.value.get(id) || `Sinh viên #${id}`
const roomLabel = (id) => {
  const room = roomMap.value.get(Number(id))
  if (!room) return `Phòng ${id}`

  return `Phòng ${room.roomNumber || room.roomId} - Tòa ${room.buildingName}`
}

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
.contract-ops-page {
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

.ops-explainer {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 14px;
}

.ops-explainer article {
  display: grid;
  grid-template-columns: 42px minmax(0, 1fr);
  gap: 14px;
  min-height: 112px;
  padding: 18px;
  border: 1px solid var(--line);
  border-radius: 8px;
  background:
    linear-gradient(135deg, rgba(22, 155, 99, 0.06), transparent 60%),
    #ffffff;
}

.ops-explainer .mdi {
  display: grid;
  place-items: center;
  width: 42px;
  height: 42px;
  border-radius: 8px;
  background: #ecfdf5;
  color: var(--brand-dark);
  font-size: 23px;
}

.ops-explainer strong {
  display: block;
  color: var(--ink);
}

.ops-explainer p {
  margin: 6px 0 0;
  color: var(--muted);
  font-size: 13px;
  line-height: 1.45;
}

.contract-alerts {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 14px;
}

.contract-alerts article {
  display: grid;
  grid-template-columns: 46px minmax(0, 1fr);
  gap: 14px;
  align-items: center;
  min-height: 104px;
  padding: 18px;
  border: 1px solid var(--line);
  border-radius: 8px;
  background: #ffffff;
}

.contract-alerts .mdi {
  display: grid;
  place-items: center;
  width: 46px;
  height: 46px;
  border-radius: 8px;
  font-size: 24px;
}

.contract-alerts .warning {
  border-color: #fed7aa;
  background: #fffbeb;
}

.contract-alerts .warning .mdi {
  background: #fff7ed;
  color: #b45309;
}

.contract-alerts .success {
  border-color: #bbf7d0;
  background: #f0fdf4;
}

.contract-alerts .success .mdi {
  background: #dcfce7;
  color: #15803d;
}

.contract-alerts strong {
  display: block;
  color: var(--ink);
  font-family: var(--font-heading);
  font-size: 18px;
}

.contract-alerts p {
  margin: 6px 0 0;
  color: var(--muted);
  font-size: 13px;
  line-height: 1.45;
}

.filter-panel {
  background: #ffffff;
}

.filter-heading,
.table-toolbar {
  display: flex;
  align-items: flex-end;
  justify-content: space-between;
  gap: 18px;
}

.filter-heading {
  padding-bottom: 16px;
}

.filter-heading h3,
.filter-heading p,
.table-toolbar h3,
.table-toolbar p {
  margin: 0;
}

.filter-heading h3,
.table-toolbar h3 {
  color: var(--ink);
  font-family: var(--font-heading);
  font-size: 20px;
}

.filter-heading p,
.table-toolbar p {
  max-width: 520px;
  color: var(--muted);
  font-size: 13px;
  line-height: 1.45;
  text-align: right;
}

.summary-row {
  display: flex;
  align-items: center;
  justify-content: flex-end;
  gap: 18px;
  color: #40576a;
  font-weight: 800;
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
  padding: 30px 18px;
  color: var(--muted);
  font-weight: 800;
  text-align: center;
}

.contract-code {
  color: var(--brand-dark);
}

.action-row {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
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

@media (max-width: 980px) {
  .page-heading {
    align-items: stretch;
    flex-direction: column;
  }

  .ops-explainer {
    grid-template-columns: 1fr;
  }

  .contract-alerts {
    grid-template-columns: 1fr;
  }

  .filter-heading,
  .table-toolbar {
    align-items: flex-start;
    flex-direction: column;
  }

  .filter-heading p,
  .table-toolbar p {
    text-align: left;
  }

  .summary-row {
    align-items: flex-start;
    flex-direction: column;
    gap: 8px;
  }
}
</style>
