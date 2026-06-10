<template>
  <section class="dashboard-page">
    <div class="dashboard-toolbar">
      <div class="toolbar-spacer"></div>
      <v-select
        v-model="buildingFilter"
        :items="buildingOptions"
        class="building-select"
        density="compact"
        hide-details
        variant="outlined"
      />
      <v-btn color="success" variant="flat" prepend-icon="mdi-plus" @click="loadDashboard">
        Làm mới
      </v-btn>
    </div>

    <div class="overview-grid">
      <div class="summary-grid">
        <article v-for="item in primaryCards" :key="item.label" class="summary-card">
          <div class="summary-icon">
            <span :class="['mdi', item.icon]"></span>
          </div>
          <strong>{{ item.value }}</strong>
          <span>{{ item.label }}</span>
        </article>
      </div>

      <div class="status-grid">
        <article v-for="item in contractCards" :key="item.label" class="status-card">
          <span>{{ item.label }}</span>
          <strong :class="item.tone">{{ item.value }}</strong>
          <small>
            <b :class="item.tone">{{ item.rate }}</b>
            {{ item.unit }}
          </small>
        </article>
      </div>

      <div class="summary-grid">
        <article v-for="item in secondaryCards" :key="item.label" class="summary-card">
          <div class="summary-icon">
            <span :class="['mdi', item.icon]"></span>
          </div>
          <strong>{{ item.value }}</strong>
          <span>{{ item.label }}</span>
        </article>
      </div>

      <div class="status-grid">
        <article v-for="item in roomCards" :key="item.label" class="status-card">
          <span>{{ item.label }}</span>
          <strong :class="item.tone">{{ item.value }}</strong>
          <small>
            <b :class="item.tone">{{ item.rate }}</b>
            {{ item.unit }}
          </small>
        </article>
      </div>
    </div>

    <div class="lower-grid">
      <section class="report-panel">
        <header>
          <h2>Tổng quan đăng ký</h2>
          <router-link to="/student-service/registrations">Xem chi tiết</router-link>
        </header>
        <div class="report-split">
          <div>
            <strong>{{ stats.pendingRegistrations }}</strong>
            <span>Chờ duyệt</span>
          </div>
          <div>
            <strong>{{ stats.approvedRegistrations }}</strong>
            <span>Đã duyệt</span>
          </div>
        </div>
      </section>

      <section class="report-panel">
        <header>
          <h2>Tổng quan hợp đồng</h2>
          <router-link to="/student-service/contracts">Xem chi tiết</router-link>
        </header>
        <div class="report-list">
          <div>
            <span>Ký mới trong kỳ</span>
            <strong>{{ stats.totalContracts }}</strong>
          </div>
          <div>
            <span>Hợp đồng hiệu lực</span>
            <strong class="success">{{ stats.activeContracts }}</strong>
          </div>
          <div>
            <span>Hết hạn</span>
            <strong class="danger">{{ stats.expiredContracts }}</strong>
          </div>
        </div>
      </section>

      <section class="report-panel">
        <header>
          <h2>Tổng quan phòng</h2>
          <router-link to="/student-service/registrations">Duyệt xếp phòng</router-link>
        </header>
        <div class="report-list">
          <div>
            <span>Tổng giường</span>
            <strong>{{ roomTotals.capacity }}</strong>
          </div>
          <div>
            <span>Đang sử dụng</span>
            <strong class="success">{{ roomTotals.occupied }}</strong>
          </div>
          <div>
            <span>Còn trống</span>
            <strong class="danger">{{ roomTotals.available }}</strong>
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
import { computed, onMounted, ref } from 'vue'
import api from '@/services/api'

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

const rooms = ref([])
const error = ref('')
const buildingFilter = ref('Tất cả tòa nhà')
const buildingOptions = ['Tất cả tòa nhà', 'Tòa A', 'Tòa B', 'Tòa C']

const normalizeList = (payload) => {
  if (Array.isArray(payload)) return payload
  if (Array.isArray(payload?.data)) return payload.data
  if (Array.isArray(payload?.value)) return payload.value
  return []
}

const filteredRooms = computed(() => {
  if (buildingFilter.value === 'Tất cả tòa nhà') return rooms.value

  const buildingName = buildingFilter.value.replace('Tòa ', '')
  return rooms.value.filter((room) => room.buildingName === buildingName)
})

const roomTotals = computed(() => {
  return filteredRooms.value.reduce(
    (total, room) => {
      total.capacity += Number(room.capacity || 0)
      total.occupied += Number(room.occupiedBeds || 0)
      total.available += Number(room.availableBeds || 0)
      total.disabled += room.status === 'Maintenance' ? Number(room.capacity || 0) : 0
      return total
    },
    { capacity: 0, occupied: 0, available: 0, disabled: 0 },
  )
})

const totalContracts = computed(() => Math.max(stats.value.totalContracts, 1))
const totalRegistrations = computed(() => Math.max(stats.value.totalRegistrations, 1))
const totalBeds = computed(() => Math.max(roomTotals.value.capacity, 1))
const occupancyRate = computed(() => Math.round((roomTotals.value.occupied / totalBeds.value) * 100))

const percent = (value, total) => {
  return `${((Number(value || 0) / Math.max(Number(total || 0), 1)) * 100).toFixed(2)} %`
}

const primaryCards = computed(() => [
  {
    icon: 'mdi-domain',
    label: 'Tòa nhà',
    value: new Set(rooms.value.map((room) => room.buildingName)).size || 3,
  },
  {
    icon: 'mdi-door-open',
    label: 'Phòng',
    value: filteredRooms.value.length,
  },
])

const secondaryCards = computed(() => [
  {
    icon: 'mdi-bed-outline',
    label: 'Giường',
    value: roomTotals.value.capacity,
  },
  {
    icon: 'mdi-percent-outline',
    label: 'Tỷ lệ lấp đầy',
    value: `${occupancyRate.value}%`,
  },
])

const contractCards = computed(() => [
  {
    label: 'Sinh viên đang ở',
    value: stats.value.totalStudents,
    rate: percent(stats.value.totalStudents, Math.max(stats.value.totalStudents, 1)),
    unit: 'sinh viên',
    tone: 'success',
  },
  {
    label: 'Đơn đang chờ duyệt',
    value: stats.value.pendingRegistrations,
    rate: percent(stats.value.pendingRegistrations, totalRegistrations.value),
    unit: 'đăng ký',
    tone: 'warning',
  },
  {
    label: 'Đơn đã duyệt',
    value: stats.value.approvedRegistrations,
    rate: percent(stats.value.approvedRegistrations, totalRegistrations.value),
    unit: 'đăng ký',
    tone: 'danger',
  },
  {
    label: 'Hợp đồng hết hạn',
    value: stats.value.expiredContracts,
    rate: percent(stats.value.expiredContracts, totalContracts.value),
    unit: 'hợp đồng',
    tone: 'muted',
  },
])

const roomCards = computed(() => [
  {
    label: 'Giường đang thuê',
    value: roomTotals.value.occupied,
    rate: percent(roomTotals.value.occupied, totalBeds.value),
    unit: 'giường',
    tone: 'success',
  },
  {
    label: 'Hợp đồng hiệu lực',
    value: stats.value.activeContracts,
    rate: percent(stats.value.activeContracts, totalContracts.value),
    unit: 'hợp đồng',
    tone: 'warning',
  },
  {
    label: 'Giường đang trống',
    value: roomTotals.value.available,
    rate: percent(roomTotals.value.available, totalBeds.value),
    unit: 'giường',
    tone: 'danger',
  },
  {
    label: 'Giường ngưng hoạt động',
    value: roomTotals.value.disabled,
    rate: percent(roomTotals.value.disabled, totalBeds.value),
    unit: 'giường',
    tone: 'muted',
  },
])

const loadDashboard = async () => {
  try {
    error.value = ''
    const [statsResponse, roomsResponse] = await Promise.all([
      api.get('/Dashboard/statistics'),
      api.get('/rooms'),
    ])

    stats.value = statsResponse.data
    rooms.value = normalizeList(roomsResponse.data)
  } catch (err) {
    error.value = 'Không tải được dữ liệu bảng tin. Kiểm tra Gateway, ContractStudentService và RoomService.'
    console.error(err)
  }
}

onMounted(loadDashboard)
</script>

<style scoped>
.dashboard-page {
  display: flex;
  flex-direction: column;
  gap: 24px;
}

.dashboard-toolbar {
  display: flex;
  align-items: center;
  justify-content: flex-end;
  gap: 14px;
  min-height: 54px;
}

.toolbar-spacer {
  flex: 1;
}

.building-select {
  max-width: 220px;
}

.overview-grid {
  display: grid;
  grid-template-columns: 480px minmax(0, 1fr);
  gap: 24px;
}

.summary-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 24px;
}

.summary-card {
  display: flex;
  flex-direction: column;
  justify-content: space-between;
  min-height: 234px;
  padding: 26px;
  border: 1px solid var(--line);
  border-radius: 8px;
  background:
    linear-gradient(#eef2f5 1px, transparent 1px),
    linear-gradient(90deg, #eef2f5 1px, transparent 1px),
    #ffffff;
  background-size: 34px 34px;
}

.summary-icon {
  color: #111827;
  font-size: 36px;
}

.summary-card strong {
  margin-top: auto;
  color: #030712;
  font-size: 38px;
  line-height: 1;
}

.summary-card span:last-child {
  margin-top: 12px;
  color: #111827;
  font-size: 17px;
}

.status-grid {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  border: 1px solid var(--line);
  border-radius: 8px;
  overflow: hidden;
  background: #ffffff;
}

.status-card {
  min-height: 234px;
  padding: 28px 26px;
  border-right: 1px solid var(--line);
  background: #ffffff;
}

.status-card:last-child {
  border-right: 0;
}

.status-card span {
  display: block;
  min-height: 48px;
  color: #111827;
  font-size: 18px;
  font-weight: 700;
  line-height: 1.35;
}

.status-card strong {
  display: block;
  margin-top: 28px;
  font-size: 38px;
  line-height: 1;
}

.status-card small {
  display: block;
  margin-top: 14px;
  color: #8a8f98;
  font-size: 16px;
}

.status-card b {
  font-weight: 800;
}

.success {
  color: #12a35f !important;
}

.warning {
  color: #d99a00 !important;
}

.danger {
  color: #e11d48 !important;
}

.muted {
  color: #64748b !important;
}

.lower-grid {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 24px;
}

.report-panel {
  min-height: 172px;
  border: 1px solid var(--line);
  border-radius: 8px;
  background: #ffffff;
}

.report-panel header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  min-height: 70px;
  padding: 0 26px;
  border-bottom: 1px solid var(--line);
}

.report-panel h2 {
  margin: 0;
  color: #111827;
  font-size: 18px;
}

.report-panel a {
  color: var(--brand-dark);
  font-size: 14px;
  font-weight: 800;
  text-decoration: none;
}

.report-split {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
}

.report-split div {
  min-height: 100px;
  padding: 24px 26px;
  border-right: 1px solid var(--line);
}

.report-split div:last-child {
  border-right: 0;
}

.report-split strong {
  display: block;
  color: #111827;
  font-size: 30px;
}

.report-split span {
  display: block;
  margin-top: 10px;
  color: var(--muted);
}

.report-list {
  display: grid;
  gap: 10px;
  padding: 22px 26px;
}

.report-list div {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
}

.report-list span {
  color: #374151;
}

.report-list strong {
  color: #111827;
  font-size: 18px;
}

@media (max-width: 1320px) {
  .overview-grid {
    grid-template-columns: 1fr;
  }

  .status-grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }

  .status-card:nth-child(2) {
    border-right: 0;
  }

  .status-card:nth-child(-n + 2) {
    border-bottom: 1px solid var(--line);
  }
}

@media (max-width: 900px) {
  .dashboard-toolbar {
    align-items: stretch;
    flex-direction: column;
  }

  .toolbar-spacer {
    display: none;
  }

  .building-select {
    max-width: none;
  }

  .summary-grid,
  .status-grid,
  .lower-grid {
    grid-template-columns: 1fr;
  }

  .summary-card,
  .status-card {
    min-height: 180px;
  }

  .status-card,
  .status-card:nth-child(2) {
    border-right: 0;
    border-bottom: 1px solid var(--line);
  }

  .status-card:last-child {
    border-bottom: 0;
  }
}
</style>
