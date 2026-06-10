<template>
  <section class="dashboard-page">
    <div class="page-heading dashboard-heading">
      <div>
        <span class="page-kicker">End-to-End Monitor</span>
        <h2>Bảng tin kết nối 3 nhóm</h2>
        <p>
          Theo dõi Gateway, RoomService, Contract & Student và Billing trong một
          luồng duyệt nội trú hoàn chỉnh.
        </p>
      </div>
      <v-btn color="success" variant="flat" prepend-icon="mdi-refresh" :loading="loading" @click="loadDashboard">
        Làm mới
      </v-btn>
    </div>

    <section class="connection-grid">
      <article v-for="service in serviceCards" :key="service.key" class="connection-card">
        <div class="connection-icon">
          <span :class="['mdi', service.icon]"></span>
        </div>
        <div class="connection-main">
          <div class="connection-topline">
            <span>{{ service.owner }}</span>
            <strong :class="['state-pill', service.state]">{{ service.stateLabel }}</strong>
          </div>
          <h3>{{ service.name }}</h3>
          <p>{{ service.note }}</p>
          <code>{{ service.endpoint }}</code>
        </div>
      </article>
    </section>

    <section class="flow-panel">
      <header>
        <div>
          <span class="panel-kicker">Luồng chấm điểm tích hợp</span>
          <h3>Sinh viên -> Đăng ký -> Xếp phòng -> Hợp đồng -> Khoản thu</h3>
        </div>
        <strong :class="['system-state', integrationReady ? 'online' : 'warning']">
          {{ integrationReady ? 'Đã sẵn sàng demo' : 'Đang chờ đủ dịch vụ' }}
        </strong>
      </header>

      <div class="flow-steps">
        <article v-for="step in flowSteps" :key="step.title" :class="['flow-step', step.state]">
          <span :class="['mdi', step.icon]"></span>
          <div>
            <strong>{{ step.title }}</strong>
            <p>{{ step.detail }}</p>
          </div>
        </article>
      </div>
    </section>

    <div class="metric-grid">
      <article v-for="metric in metrics" :key="metric.label" class="metric-card">
        <span :class="['mdi', metric.icon]"></span>
        <p>{{ metric.label }}</p>
        <strong>{{ metric.value }}</strong>
        <small>{{ metric.hint }}</small>
      </article>
    </div>

    <div class="operations-grid">
      <section class="report-panel">
        <header>
          <div>
            <span class="panel-kicker">Nhóm 1</span>
            <h3>Tình trạng phòng</h3>
          </div>
          <router-link to="/student-service/registrations">Duyệt xếp phòng</router-link>
        </header>
        <div class="room-summary">
          <div>
            <span>Tổng giường</span>
            <strong>{{ roomTotals.capacity }}</strong>
          </div>
          <div>
            <span>Đang ở</span>
            <strong class="success">{{ roomTotals.occupied }}</strong>
          </div>
          <div>
            <span>Còn trống</span>
            <strong class="blue">{{ roomTotals.available }}</strong>
          </div>
        </div>
        <div class="room-list">
          <div v-for="room in roomPreview" :key="room.roomId" class="room-row">
            <span>Phòng {{ room.roomId }} - Tòa {{ room.buildingName }}</span>
            <strong>{{ room.availableBeds }}/{{ room.capacity }} giường trống</strong>
          </div>
          <div v-if="roomPreview.length === 0" class="empty-row">Chưa nhận được dữ liệu phòng từ RoomService.</div>
        </div>
      </section>

      <section class="report-panel">
        <header>
          <div>
            <span class="panel-kicker">Nhóm 2</span>
            <h3>Hồ sơ và hợp đồng</h3>
          </div>
          <router-link to="/student-service/contracts">Xem hợp đồng</router-link>
        </header>
        <div class="report-list">
          <div>
            <span>Hồ sơ sinh viên</span>
            <strong>{{ stats.totalStudents }}</strong>
          </div>
          <div>
            <span>Đơn chờ duyệt</span>
            <strong class="warning">{{ stats.pendingRegistrations }}</strong>
          </div>
          <div>
            <span>Hợp đồng hiệu lực</span>
            <strong class="success">{{ stats.activeContracts }}</strong>
          </div>
          <div>
            <span>Hợp đồng hết hạn</span>
            <strong class="danger">{{ stats.expiredContracts }}</strong>
          </div>
        </div>
      </section>

      <section class="report-panel">
        <header>
          <div>
            <span class="panel-kicker">Nhóm 3</span>
            <h3>Khoản thu phát sinh</h3>
          </div>
          <span class="panel-total">{{ formatMoney(billingTotal) }}</span>
        </header>
        <div class="billing-list">
          <div v-for="item in billingPreview" :key="`${item.contractId}-${item.feeType}-${item.id}`" class="billing-row">
            <div>
              <strong>{{ item.contractCode || `HĐ #${item.contractId}` }}</strong>
              <span>{{ feeLabel(item.feeType) }}</span>
            </div>
            <b>{{ formatMoney(item.amount) }}</b>
          </div>
          <div v-if="billingPreview.length === 0" class="empty-row">
            Chưa có khoản thu. Khi duyệt đơn thành công, N2 sẽ gửi hợp đồng sang BillingService.
          </div>
        </div>
      </section>
    </div>

    <v-alert v-if="error" type="error" variant="tonal" class="mt-4">
      {{ error }}
    </v-alert>
  </section>
</template>

<script setup>
import axios from 'axios'
import { computed, onMounted, ref } from 'vue'
import api from '@/services/api'

const loading = ref(false)
const error = ref('')
const rooms = ref([])
const billingItems = ref([])
const stats = ref({
  totalStudents: 0,
  totalContracts: 0,
  totalRegistrations: 0,
  totalCheckHistories: 0,
  activeContracts: 0,
  pendingRegistrations: 0,
  approvedRegistrations: 0,
  expiredContracts: 0,
})

const checks = ref({
  gateway: { state: 'checking', note: 'Đang kiểm tra Gateway' },
  room: { state: 'checking', note: 'Đang kiểm tra RoomService' },
  contract: { state: 'checking', note: 'Đang kiểm tra Contract & Student Service' },
  billing: { state: 'checking', note: 'Đang kiểm tra BillingService' },
})

const serviceDefinitions = [
  {
    key: 'gateway',
    owner: 'Nhóm 3',
    name: 'API Gateway',
    endpoint: '/health',
    icon: 'mdi-router-network',
  },
  {
    key: 'room',
    owner: 'Nhóm 1',
    name: 'Room & Building Service',
    endpoint: '/api/rooms',
    icon: 'mdi-door-open',
  },
  {
    key: 'contract',
    owner: 'Nhóm 2',
    name: 'Contract & Student Service',
    endpoint: '/api/dashboard/statistics',
    icon: 'mdi-account-school-outline',
  },
  {
    key: 'billing',
    owner: 'Nhóm 3',
    name: 'Billing Service',
    endpoint: '/api/billing/contracts',
    icon: 'mdi-cash-multiple',
  },
]

const normalizeList = (payload) => {
  if (Array.isArray(payload)) return payload
  if (Array.isArray(payload?.data)) return payload.data
  if (Array.isArray(payload?.value)) return payload.value
  return []
}

const stateLabel = {
  online: 'Đã nối',
  warning: 'Cần xem',
  offline: 'Chưa nối',
  checking: 'Đang kiểm tra',
}

const serviceCards = computed(() =>
  serviceDefinitions.map((service) => ({
    ...service,
    ...checks.value[service.key],
    stateLabel: stateLabel[checks.value[service.key]?.state] || 'Không rõ',
  })),
)

const integrationReady = computed(() =>
  ['gateway', 'room', 'contract', 'billing'].every((key) => checks.value[key]?.state === 'online'),
)

const roomTotals = computed(() =>
  rooms.value.reduce(
    (total, room) => {
      const capacity = Number(room.capacity || 0)
      const occupied = Number(room.occupiedBeds ?? room.currentOccupancy ?? 0)
      const available = Number(room.availableBeds ?? Math.max(capacity - occupied, 0))

      total.capacity += capacity
      total.occupied += occupied
      total.available += available
      return total
    },
    { capacity: 0, occupied: 0, available: 0 },
  ),
)

const roomPreview = computed(() =>
  [...rooms.value]
    .sort((first, second) => Number(first.roomId || first.id || 0) - Number(second.roomId || second.id || 0))
    .slice(0, 5),
)

const billingTotal = computed(() =>
  billingItems.value.reduce((total, item) => total + Number(item.amount || 0), 0),
)

const billingPreview = computed(() => billingItems.value.slice(0, 5))

const metrics = computed(() => [
  {
    icon: 'mdi-account-multiple-outline',
    label: 'Sinh viên',
    value: stats.value.totalStudents,
    hint: 'Hồ sơ do N2 quản lý',
  },
  {
    icon: 'mdi-clipboard-clock-outline',
    label: 'Đơn chờ duyệt',
    value: stats.value.pendingRegistrations,
    hint: 'Cần đối chiếu phòng trống',
  },
  {
    icon: 'mdi-bed-outline',
    label: 'Giường trống',
    value: roomTotals.value.available,
    hint: 'Lấy từ RoomService nhóm 1',
  },
  {
    icon: 'mdi-file-sign',
    label: 'Hợp đồng hiệu lực',
    value: stats.value.activeContracts,
    hint: 'Tự sinh sau khi duyệt đơn',
  },
  {
    icon: 'mdi-receipt-text-check-outline',
    label: 'Khoản thu',
    value: billingItems.value.length,
    hint: 'Từ BillingService nhóm 3',
  },
])

const flowSteps = computed(() => [
  {
    title: '1. Lưu hồ sơ sinh viên',
    icon: 'mdi-account-school-outline',
    state: checks.value.contract.state,
    detail: `${stats.value.totalStudents} hồ sơ đang có trong N2`,
  },
  {
    title: '2. Tiếp nhận đăng ký online',
    icon: 'mdi-form-select',
    state: checks.value.contract.state,
    detail: `${stats.value.totalRegistrations} đơn, ${stats.value.pendingRegistrations} đơn đang chờ duyệt`,
  },
  {
    title: '3. Tự động xếp phòng',
    icon: 'mdi-transit-connection-variant',
    state: checks.value.room.state === 'online' && checks.value.contract.state === 'online' ? 'online' : 'warning',
    detail: `N2 gọi RoomService, hiện còn ${roomTotals.value.available} giường trống`,
  },
  {
    title: '4. Tạo hợp đồng thuê phòng',
    icon: 'mdi-file-document-edit-outline',
    state: checks.value.contract.state,
    detail: `${stats.value.totalContracts} hợp đồng, ${stats.value.activeContracts} đang hiệu lực`,
  },
  {
    title: '5. Gửi khoản thu sang Billing',
    icon: 'mdi-cash-sync',
    state: checks.value.billing.state,
    detail: `${billingItems.value.length} khoản thu đã nhận từ hợp đồng`,
  },
])

const resolveGatewayHealthUrl = () => {
  const baseUrl = api.defaults.baseURL || '/api'

  if (baseUrl.startsWith('http')) {
    const url = new URL(baseUrl)
    return `${url.origin}/health`
  }

  return '/health'
}

const markOnline = (key, note) => {
  checks.value[key] = { state: 'online', note }
}

const markError = (key, err) => {
  const status = err?.response?.status
  let note = 'Không nhận được phản hồi từ dịch vụ.'

  if (status === 401) note = 'Gateway tới được service nhưng đang bị JWT chặn.'
  else if (status === 404) note = 'Gateway chưa route đúng endpoint này.'
  else if (status >= 500) note = 'Service phản hồi lỗi máy chủ, cần xem log container.'

  checks.value[key] = {
    state: status === 401 ? 'warning' : 'offline',
    note,
  }
}

const loadGateway = async () => {
  try {
    await axios.get(resolveGatewayHealthUrl())
    markOnline('gateway', 'Gateway public đang nhận request.')
  } catch (err) {
    markError('gateway', err)
  }
}

const loadRooms = async () => {
  try {
    const response = await api.get('/rooms')
    rooms.value = normalizeList(response.data)
    markOnline('room', `${rooms.value.length} phòng được đồng bộ qua Gateway.`)
  } catch (err) {
    rooms.value = []
    markError('room', err)
  }
}

const loadStats = async () => {
  try {
    const response = await api.get('/Dashboard/statistics')
    stats.value = { ...stats.value, ...response.data }
    markOnline('contract', 'N2 đang phục vụ hồ sơ, đăng ký và hợp đồng.')
  } catch (err) {
    markError('contract', err)
  }
}

const loadBilling = async () => {
  try {
    const response = await api.get('/billing/contracts')
    billingItems.value = normalizeList(response.data)
    markOnline('billing', `${billingItems.value.length} khoản thu đang được BillingService giữ.`)
  } catch (err) {
    billingItems.value = []
    markError('billing', err)
  }
}

const loadDashboard = async () => {
  loading.value = true
  error.value = ''

  checks.value = {
    gateway: { state: 'checking', note: 'Đang kiểm tra Gateway' },
    room: { state: 'checking', note: 'Đang kiểm tra RoomService' },
    contract: { state: 'checking', note: 'Đang kiểm tra Contract & Student Service' },
    billing: { state: 'checking', note: 'Đang kiểm tra BillingService' },
  }

  await Promise.all([loadGateway(), loadRooms(), loadStats(), loadBilling()])

  if (!integrationReady.value) {
    error.value = 'Chưa đủ 4 điểm nối Gateway, RoomService, Contract & Student và Billing. Xem thẻ trạng thái phía trên để biết service nào cần sửa.'
  }

  loading.value = false
}

const formatMoney = (value) =>
  new Intl.NumberFormat('vi-VN', {
    style: 'currency',
    currency: 'VND',
    maximumFractionDigits: 0,
  }).format(Number(value || 0))

const feeLabel = (value) => {
  const labels = {
    Deposit: 'Tiền cọc',
    FirstMonthRoomFee: 'Tiền phòng tháng đầu',
  }

  return labels[value] || value || 'Khoản thu'
}

onMounted(loadDashboard)
</script>

<style scoped>
.dashboard-page {
  display: flex;
  flex-direction: column;
  gap: 22px;
}

.dashboard-heading {
  align-items: flex-end;
  margin-bottom: 0;
}

.dashboard-heading p {
  max-width: 760px;
  margin: 8px 0 0;
  color: var(--muted);
  font-size: 15px;
  line-height: 1.5;
}

.connection-grid {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: 14px;
}

.connection-card {
  display: grid;
  grid-template-columns: 48px minmax(0, 1fr);
  gap: 14px;
  min-height: 152px;
  padding: 18px;
  border: 1px solid var(--line);
  border-radius: 8px;
  background: #ffffff;
}

.connection-icon {
  display: grid;
  place-items: center;
  width: 48px;
  height: 48px;
  border-radius: 8px;
  background: #ecfdf5;
  color: var(--brand-dark);
  font-size: 25px;
}

.connection-main {
  min-width: 0;
}

.connection-topline {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 10px;
  min-height: 24px;
}

.connection-topline span {
  color: var(--muted);
  font-size: 12px;
  font-weight: 800;
  text-transform: uppercase;
}

.state-pill,
.system-state {
  display: inline-flex;
  align-items: center;
  min-height: 26px;
  padding: 0 10px;
  border-radius: 999px;
  font-size: 12px;
  font-weight: 900;
  white-space: nowrap;
}

.state-pill.online,
.system-state.online {
  background: #dcfce7;
  color: #15803d;
}

.state-pill.warning,
.system-state.warning {
  background: #fef3c7;
  color: #b45309;
}

.state-pill.offline {
  background: #ffe4e6;
  color: #be123c;
}

.state-pill.checking {
  background: #e0f2fe;
  color: #0369a1;
}

.connection-card h3 {
  margin: 12px 0 6px;
  color: var(--ink);
  font-size: 16px;
}

.connection-card p {
  min-height: 38px;
  margin: 0;
  color: var(--muted);
  font-size: 13px;
  line-height: 1.45;
}

.connection-card code {
  display: block;
  margin-top: 10px;
  color: #334155;
  font-size: 12px;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.flow-panel,
.report-panel,
.metric-card {
  border: 1px solid var(--line);
  border-radius: 8px;
  background: #ffffff;
}

.flow-panel header,
.report-panel header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 18px;
  padding: 18px 22px;
  border-bottom: 1px solid var(--line);
}

.panel-kicker {
  display: block;
  margin-bottom: 4px;
  color: var(--brand-dark);
  font-size: 12px;
  font-weight: 900;
  letter-spacing: 0.06em;
  text-transform: uppercase;
}

.flow-panel h3,
.report-panel h3 {
  margin: 0;
  color: var(--ink);
  font-size: 18px;
}

.flow-steps {
  display: grid;
  grid-template-columns: repeat(5, minmax(0, 1fr));
}

.flow-step {
  display: grid;
  grid-template-columns: 36px minmax(0, 1fr);
  gap: 12px;
  min-height: 138px;
  padding: 20px;
  border-right: 1px solid var(--line);
}

.flow-step:last-child {
  border-right: 0;
}

.flow-step > .mdi {
  color: var(--slate);
  font-size: 28px;
}

.flow-step.online > .mdi {
  color: var(--brand-dark);
}

.flow-step.warning > .mdi,
.flow-step.checking > .mdi {
  color: var(--amber);
}

.flow-step.offline > .mdi {
  color: var(--rose);
}

.flow-step strong {
  display: block;
  color: var(--ink);
  font-size: 14px;
}

.flow-step p {
  margin: 8px 0 0;
  color: var(--muted);
  font-size: 13px;
  line-height: 1.45;
}

.metric-grid {
  display: grid;
  grid-template-columns: repeat(5, minmax(0, 1fr));
  gap: 14px;
}

.metric-card {
  min-height: 150px;
  padding: 20px;
}

.metric-card .mdi {
  color: var(--brand-dark);
  font-size: 28px;
}

.metric-card p {
  margin: 16px 0 8px;
  color: var(--muted);
  font-weight: 800;
}

.metric-card strong {
  display: block;
  color: var(--ink);
  font-size: 34px;
  line-height: 1;
}

.metric-card small {
  display: block;
  margin-top: 10px;
  color: var(--muted);
  line-height: 1.35;
}

.operations-grid {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 16px;
}

.report-panel {
  min-height: 320px;
  overflow: hidden;
}

.report-panel a,
.panel-total {
  color: var(--brand-dark);
  font-size: 13px;
  font-weight: 900;
  text-decoration: none;
  white-space: nowrap;
}

.room-summary {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  border-bottom: 1px solid var(--line);
}

.room-summary div {
  padding: 18px;
  border-right: 1px solid var(--line);
}

.room-summary div:last-child {
  border-right: 0;
}

.room-summary span,
.report-list span,
.billing-row span {
  display: block;
  color: var(--muted);
  font-size: 13px;
}

.room-summary strong {
  display: block;
  margin-top: 8px;
  color: var(--ink);
  font-size: 24px;
}

.room-list,
.billing-list,
.report-list {
  display: grid;
  gap: 0;
  padding: 8px 18px 18px;
}

.room-row,
.billing-row,
.report-list div,
.empty-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 14px;
  min-height: 44px;
  border-bottom: 1px solid var(--line);
}

.room-row:last-child,
.billing-row:last-child,
.report-list div:last-child {
  border-bottom: 0;
}

.room-row span,
.billing-row strong,
.report-list span {
  color: var(--ink);
  font-weight: 800;
}

.room-row strong,
.billing-row b,
.report-list strong {
  color: var(--brand-dark);
  font-size: 14px;
  white-space: nowrap;
}

.report-list div {
  min-height: 54px;
}

.report-list strong {
  font-size: 20px;
}

.billing-row {
  align-items: flex-start;
  padding: 12px 0;
}

.billing-row span {
  margin-top: 4px;
}

.empty-row {
  justify-content: flex-start;
  color: var(--muted);
  font-size: 14px;
  line-height: 1.45;
}

.success {
  color: #15803d !important;
}

.warning {
  color: #b45309 !important;
}

.danger {
  color: #be123c !important;
}

.blue {
  color: #2563eb !important;
}

@media (max-width: 1320px) {
  .connection-grid,
  .metric-grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }

  .flow-steps,
  .operations-grid {
    grid-template-columns: 1fr;
  }

  .flow-step,
  .flow-step:last-child {
    border-right: 0;
    border-bottom: 1px solid var(--line);
  }

  .flow-step:last-child {
    border-bottom: 0;
  }
}

@media (max-width: 760px) {
  .dashboard-heading,
  .flow-panel header,
  .report-panel header {
    align-items: flex-start;
    flex-direction: column;
  }

  .connection-grid,
  .metric-grid,
  .room-summary {
    grid-template-columns: 1fr;
  }

  .room-summary div {
    border-right: 0;
    border-bottom: 1px solid var(--line);
  }

  .room-summary div:last-child {
    border-bottom: 0;
  }
}
</style>
