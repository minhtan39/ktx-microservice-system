<template>
  <section class="dashboard-page">
    <section class="dashboard-hero">
      <div>
        <span class="page-kicker">Tổng quan ký túc xá</span>
        <h2>Hôm nay ở KTX</h2>
        <p>Dữ liệu quan trọng được gom lại để nhân viên nhìn một lần là biết cần xử lý gì trước.</p>
        <div class="hero-actions">
          <router-link class="primary-action" to="/student-service/registrations/approval">
            <span class="mdi mdi-clipboard-check-outline"></span>
            Duyệt đơn chờ
          </router-link>
          <router-link class="secondary-action" to="/facility/rooms">
            Xem sơ đồ phòng
          </router-link>
        </div>
      </div>
      <div class="hero-summary">
        <div>
          <span>Giường trống</span>
          <strong>{{ roomTotals.available }}</strong>
          <small>{{ roomTotals.capacity }} tổng giường</small>
        </div>
        <div>
          <span>Đơn chờ duyệt</span>
          <strong>{{ stats.pendingRegistrations }}</strong>
          <small>Cần xếp phòng</small>
        </div>
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

    <div class="focus-grid">
      <section class="report-panel task-panel">
        <header>
          <div>
            <span class="panel-kicker">Việc cần xử lý</span>
            <h3>Ưu tiên trong ca trực</h3>
          </div>
          <v-btn color="success" variant="tonal" prepend-icon="mdi-refresh" :loading="loading" @click="loadDashboard">
            Làm mới
          </v-btn>
        </header>

        <div class="task-list">
          <router-link to="/student-service/registrations/approval" class="task-card">
            <span class="mdi mdi-clipboard-clock-outline"></span>
            <div>
              <strong>{{ stats.pendingRegistrations }} đơn chờ duyệt</strong>
              <small>Kiểm tra nguyện vọng, chọn tòa/phòng còn giường</small>
            </div>
          </router-link>
          <router-link to="/student-service/contracts/manage" class="task-card">
            <span class="mdi mdi-file-sign"></span>
            <div>
              <strong>{{ stats.expiredContracts }} hợp đồng cần xem</strong>
              <small>Theo dõi hợp đồng hết hạn hoặc cần gia hạn</small>
            </div>
          </router-link>
          <router-link to="/finance/incidents" class="task-card">
            <span class="mdi mdi-tools"></span>
            <div>
              <strong>Theo dõi yêu cầu sửa chữa</strong>
              <small>Giữ trải nghiệm phòng ở ổn định cho sinh viên</small>
            </div>
          </router-link>
        </div>
      </section>

      <section class="report-panel room-panel">
        <header>
          <div>
            <span class="panel-kicker">Phòng ở</span>
            <h3>Tình trạng giường trống</h3>
          </div>
          <router-link to="/facility/rooms">Chi tiết phòng</router-link>
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
    </div>

    <div class="operations-grid">
      <section class="report-panel">
        <header>
          <div>
            <span class="panel-kicker">Sinh viên</span>
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
            <span class="panel-kicker">Tài chính</span>
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

    <section class="connection-strip">
      <header>
        <div>
          <span class="panel-kicker">Trạng thái hệ thống</span>
          <h3>{{ integrationReady ? 'Các điểm kết nối đang sẵn sàng' : 'Một vài điểm kết nối cần kiểm tra' }}</h3>
        </div>
        <strong :class="['system-state', integrationReady ? 'online' : 'warning']">
          {{ integrationReady ? 'Ổn định' : 'Cần xem' }}
        </strong>
      </header>
      <div class="connection-list">
        <article v-for="service in serviceCards" :key="service.key" class="connection-card">
          <span :class="['mdi', service.icon]"></span>
          <div>
            <strong>{{ service.name }}</strong>
            <small>{{ service.note }}</small>
          </div>
          <b :class="['state-pill', service.state]">{{ service.stateLabel }}</b>
        </article>
      </div>
    </section>

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
    hint: 'Hồ sơ đang quản lý',
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
    hint: 'Có thể xếp ngay',
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
    hint: 'Phát sinh từ hợp đồng',
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
  gap: 18px;
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

.system-state {
  min-height: 32px;
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

.dashboard-hero,
.report-panel,
.metric-card,
.connection-strip {
  border: 1px solid var(--line);
  border-radius: 8px;
  background: #ffffff;
}

.dashboard-hero {
  display: grid;
  grid-template-columns: minmax(0, 1fr) minmax(280px, 360px);
  gap: 28px;
  align-items: end;
  min-height: 280px;
  padding: 30px;
  overflow: hidden;
  background:
    linear-gradient(135deg, rgba(243, 111, 33, 0.13), transparent 44%),
    linear-gradient(120deg, #ffffff, #fff7ed);
}

.dashboard-hero h2 {
  max-width: 640px;
  margin: 0;
  color: #24150e;
  font-size: 44px;
  line-height: 1.05;
}

.dashboard-hero p {
  max-width: 590px;
  margin: 12px 0 0;
  color: #6f5747;
  font-size: 16px;
  line-height: 1.6;
}

.hero-actions {
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
  margin-top: 24px;
}

.primary-action,
.secondary-action,
.task-card,
.report-panel a {
  text-decoration: none;
}

.primary-action,
.secondary-action {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-height: 42px;
  padding: 0 16px;
  border-radius: 8px;
  font-weight: 900;
}

.primary-action {
  gap: 8px;
  background: var(--brand);
  color: #ffffff;
}

.secondary-action {
  border: 1px solid #fed7aa;
  background: #ffffff;
  color: var(--brand-dark);
}

.hero-summary {
  display: grid;
  gap: 12px;
}

.hero-summary div {
  min-height: 116px;
  padding: 18px;
  border: 1px solid #fed7aa;
  border-radius: 8px;
  background: rgba(255, 255, 255, 0.82);
}

.hero-summary span,
.hero-summary small {
  display: block;
  color: var(--muted);
  font-weight: 800;
}

.hero-summary strong {
  display: block;
  margin: 7px 0;
  color: #c2410c;
  font-size: 40px;
  line-height: 1;
}

.metric-grid {
  display: grid;
  grid-template-columns: repeat(5, minmax(0, 1fr));
  gap: 14px;
}

.metric-card {
  min-height: 142px;
  padding: 18px;
}

.metric-card .mdi {
  color: var(--brand-dark);
  font-size: 28px;
}

.metric-card p {
  margin: 14px 0 8px;
  color: var(--muted);
  font-weight: 800;
}

.metric-card strong {
  display: block;
  color: var(--ink);
  font-size: 32px;
  line-height: 1;
}

.metric-card small {
  display: block;
  margin-top: 9px;
  color: var(--muted);
  line-height: 1.35;
}

.focus-grid {
  display: grid;
  grid-template-columns: minmax(420px, 0.9fr) minmax(520px, 1.1fr);
  gap: 16px;
}

.operations-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 16px;
}

.report-panel header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 18px;
  padding: 18px 22px;
  border-bottom: 1px solid var(--line);
}

.connection-strip header {
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

.report-panel h3,
.connection-strip h3 {
  margin: 0;
  color: var(--ink);
  font-size: 18px;
}

.task-list {
  display: grid;
  gap: 10px;
  padding: 18px;
}

.task-card {
  display: grid;
  grid-template-columns: 42px minmax(0, 1fr);
  align-items: center;
  gap: 12px;
  min-height: 76px;
  padding: 14px;
  border: 1px solid #eef2f5;
  border-radius: 8px;
  background: #fbfdfc;
  color: var(--ink);
}

.task-card .mdi {
  display: grid;
  place-items: center;
  width: 42px;
  height: 42px;
  border-radius: 8px;
  background: #eaf8f0;
  color: var(--brand-dark);
  font-size: 23px;
}

.task-card strong,
.task-card small {
  display: block;
}

.task-card strong {
  color: var(--ink);
}

.task-card small {
  margin-top: 4px;
  color: var(--muted);
  font-size: 13px;
  line-height: 1.35;
}

.report-panel {
  min-height: 300px;
  overflow: hidden;
}

.room-panel {
  min-height: 100%;
}

.report-panel a,
.panel-total {
  color: var(--brand-dark);
  font-size: 13px;
  font-weight: 900;
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

 .connection-list {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: 0;
}

.connection-card {
  display: grid;
  grid-template-columns: 36px minmax(0, 1fr) auto;
  align-items: center;
  gap: 10px;
  min-height: 98px;
  padding: 18px;
  border-right: 1px solid var(--line);
}

.connection-card:last-child {
  border-right: 0;
}

.connection-card > .mdi {
  color: var(--brand-dark);
  font-size: 26px;
}

.connection-card strong,
.connection-card small {
  display: block;
}

.connection-card strong {
  color: var(--ink);
  font-size: 14px;
}

.connection-card small {
  margin-top: 4px;
  color: var(--muted);
  font-size: 12px;
  line-height: 1.35;
}

@media (max-width: 1320px) {
  .dashboard-hero,
  .focus-grid,
  .operations-grid,
  .metric-grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }

  .connection-list {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
}

@media (max-width: 760px) {
  .dashboard-hero,
  .focus-grid,
  .operations-grid,
  .metric-grid,
  .room-summary,
  .connection-list {
    grid-template-columns: 1fr;
  }

  .dashboard-hero {
    padding: 20px;
  }

  .dashboard-hero h2 {
    font-size: 32px;
  }

  .report-panel header,
  .connection-strip header {
    align-items: flex-start;
    flex-direction: column;
  }

  .room-summary div {
    border-right: 0;
    border-bottom: 1px solid var(--line);
  }

  .room-summary div:last-child {
    border-bottom: 0;
  }

  .connection-card {
    border-right: 0;
    border-bottom: 1px solid var(--line);
  }

  .connection-card:last-child {
    border-bottom: 0;
  }
}
</style>
