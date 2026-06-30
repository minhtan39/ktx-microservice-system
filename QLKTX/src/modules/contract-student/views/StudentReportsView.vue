<template>
  <section class="reports-page">
    <section class="reports-hero">
      <div>
        <span class="page-kicker">RESIDENCE ANALYTICS</span>
        <h2>Báo cáo nội trú</h2>
        <p>
          Tổng hợp dữ liệu thật từ hồ sơ sinh viên, đăng ký nội trú, hợp đồng và RoomService
          để admin biết điểm nghẽn cần xử lý trong ngày.
        </p>
      </div>
      <div class="hero-actions">
        <v-btn color="primary" variant="flat" prepend-icon="mdi-refresh" :loading="loading" @click="loadReports">
          Làm mới
        </v-btn>
        <v-btn color="success" variant="tonal" prepend-icon="mdi-file-excel-outline" @click="exportReport">
          Xuất Excel
        </v-btn>
      </div>
    </section>

    <v-progress-linear v-if="loading" indeterminate color="primary" />

    <v-alert v-if="error" type="warning" variant="tonal" density="comfortable">
      {{ error }}
    </v-alert>

    <div class="kpi-grid">
      <article v-for="metric in kpiMetrics" :key="metric.label" class="kpi-card">
        <span :class="['mdi', metric.icon, metric.tone]"></span>
        <div>
          <strong>{{ metric.value }}</strong>
          <p>{{ metric.label }}</p>
          <small>{{ metric.hint }}</small>
        </div>
      </article>
    </div>

    <section class="report-panel funnel-panel">
      <header class="panel-heading">
        <div>
          <span class="panel-kicker">OPERATION FUNNEL</span>
          <h3>Luồng xử lý N2 từ hồ sơ đến hợp đồng</h3>
        </div>
        <router-link to="/student-service/registrations/approval">Duyệt đơn</router-link>
      </header>

      <div class="funnel-track">
        <article v-for="step in funnelSteps" :key="step.label" class="funnel-step">
          <div class="step-top">
            <span :class="['mdi', step.icon]"></span>
            <strong>{{ step.value }}</strong>
          </div>
          <p>{{ step.label }}</p>
          <div class="step-bar">
            <i :style="{ width: `${step.percent}%` }"></i>
          </div>
          <small>{{ step.hint }}</small>
        </article>
      </div>
    </section>

    <div class="charts-grid">
      <section class="report-panel chart-panel">
        <header class="panel-heading compact">
          <div>
            <span class="panel-kicker">REGISTRATION</span>
            <h3>Trạng thái đăng ký</h3>
          </div>
        </header>
        <div v-if="registrationChartRows.length" class="chart-canvas">
          <canvas ref="registrationCanvas" aria-label="Biểu đồ trạng thái đăng ký"></canvas>
        </div>
        <div v-else class="empty-chart">
          <span class="mdi mdi-chart-donut-variant"></span>
          <strong>Chưa có đơn đăng ký</strong>
        </div>
      </section>

      <section class="report-panel chart-panel">
        <header class="panel-heading compact">
          <div>
            <span class="panel-kicker">CONTRACT</span>
            <h3>Trạng thái hợp đồng</h3>
          </div>
        </header>
        <div v-if="contractChartRows.length" class="chart-canvas">
          <canvas ref="contractCanvas" aria-label="Biểu đồ trạng thái hợp đồng"></canvas>
        </div>
        <div v-else class="empty-chart">
          <span class="mdi mdi-chart-donut-variant"></span>
          <strong>Chưa có hợp đồng</strong>
        </div>
      </section>

      <section class="report-panel chart-panel wide">
        <header class="panel-heading compact">
          <div>
            <span class="panel-kicker">ROOM SERVICE</span>
            <h3>Giường trống theo tòa</h3>
          </div>
          <router-link to="/facility/rooms">Xem sơ đồ phòng</router-link>
        </header>
        <div v-if="buildingRows.length" class="chart-canvas wide-canvas">
          <canvas ref="buildingCanvas" aria-label="Biểu đồ giường trống theo tòa"></canvas>
        </div>
        <div v-else class="empty-chart">
          <span class="mdi mdi-door-closed-lock"></span>
          <strong>RoomService chưa trả dữ liệu phòng</strong>
        </div>
      </section>

      <section class="report-panel chart-panel wide">
        <header class="panel-heading compact">
          <div>
            <span class="panel-kicker">STUDENT PROFILE</span>
            <h3>Sinh viên theo khoa</h3>
          </div>
          <router-link to="/student-service/students">Hồ sơ sinh viên</router-link>
        </header>
        <div v-if="facultyRows.length" class="chart-canvas wide-canvas">
          <canvas ref="facultyCanvas" aria-label="Biểu đồ sinh viên theo khoa"></canvas>
        </div>
        <div v-else class="empty-chart">
          <span class="mdi mdi-account-school-outline"></span>
          <strong>Chưa có dữ liệu khoa</strong>
        </div>
      </section>
    </div>

    <div class="insight-grid">
      <section class="report-panel">
        <header class="panel-heading">
          <div>
            <span class="panel-kicker">ACTION CENTER</span>
            <h3>Cảnh báo cần xử lý</h3>
          </div>
        </header>

        <div class="alert-list">
          <router-link
            v-for="alert in operationalAlerts"
            :key="alert.title"
            :to="alert.to"
            :class="['alert-row', alert.tone]"
          >
            <span :class="['mdi', alert.icon]"></span>
            <div>
              <strong>{{ alert.title }}</strong>
              <small>{{ alert.description }}</small>
            </div>
            <b>{{ alert.count }}</b>
          </router-link>
        </div>
      </section>

      <section class="report-panel">
        <header class="panel-heading">
          <div>
            <span class="panel-kicker">ROOM CAPACITY</span>
            <h3>Tóm tắt sức chứa</h3>
          </div>
        </header>

        <div class="capacity-summary">
          <div>
            <span>Tổng giường</span>
            <strong>{{ roomTotals.capacity }}</strong>
          </div>
          <div>
            <span>Đang ở</span>
            <strong>{{ roomTotals.occupied }}</strong>
          </div>
          <div>
            <span>Còn trống</span>
            <strong>{{ roomTotals.available }}</strong>
          </div>
        </div>

        <div class="building-list">
          <div v-for="row in buildingRows" :key="row.label" class="building-row">
            <div>
              <strong>{{ row.label }}</strong>
              <small>{{ row.occupied }} đang ở / {{ row.capacity }} tổng giường</small>
            </div>
            <b>{{ row.available }} trống</b>
          </div>
          <div v-if="buildingRows.length === 0" class="empty-line">
            Chưa đồng bộ được danh sách phòng từ nhóm 1.
          </div>
        </div>
      </section>
    </div>
  </section>
</template>

<script setup>
import { computed, nextTick, onBeforeUnmount, onMounted, ref } from 'vue'
import {
  ArcElement,
  BarController,
  BarElement,
  CategoryScale,
  Chart,
  DoughnutController,
  Legend,
  LinearScale,
  Tooltip,
} from 'chart.js'
import api from '@/services/api'
import { exportRowsToExcel } from '@/utils/exportExcel'
import { cleanStudents, normalizeList } from '../utils/studentDisplay'

Chart.register(
  ArcElement,
  BarController,
  BarElement,
  CategoryScale,
  DoughnutController,
  Legend,
  LinearScale,
  Tooltip,
)

const loading = ref(false)
const error = ref('')
const students = ref([])
const registrations = ref([])
const contracts = ref([])
const rooms = ref([])
const registrationCanvas = ref(null)
const contractCanvas = ref(null)
const buildingCanvas = ref(null)
const facultyCanvas = ref(null)

let registrationChart = null
let contractChart = null
let buildingChart = null
let facultyChart = null

const statusKey = (value) => String(value || '').trim().toLowerCase()
const today = () => new Date()

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

const pendingRegistrations = computed(() => countByRegistrationStatus(['pending']))
const approvedRegistrations = computed(() => countByRegistrationStatus(['approved', 'assigned']))
const rejectedRegistrations = computed(() => countByRegistrationStatus(['rejected']))
const activeContracts = computed(() => countByContractStatus(['active']))
const pendingSignatureContracts = computed(() => countByContractStatus(['pendingsignature']))
const expiredContracts = computed(() => countByContractStatus(['expired']))

const expiringSoonContracts = computed(() => {
  const now = today()
  const next30Days = new Date(now)
  next30Days.setDate(now.getDate() + 30)

  return contracts.value.filter((contract) => {
    if (statusKey(contract.status) !== 'active' || !contract.endDate) return false
    const endDate = new Date(contract.endDate)
    return endDate >= now && endDate <= next30Days
  })
})

const missingContactStudents = computed(() =>
  students.value.filter((student) => !student.phone || !student.email || !student.cccd),
)

const activeContractStudentIds = computed(() =>
  new Set(
    contracts.value
      .filter((contract) => ['active', 'pendingsignature'].includes(statusKey(contract.status)))
      .map((contract) => Number(contract.studentId)),
  ),
)

const stayingWithoutContract = computed(() =>
  students.value.filter((student) =>
    statusKey(student.status) === 'active' && !activeContractStudentIds.value.has(Number(student.id))),
)

const kpiMetrics = computed(() => [
  {
    icon: 'mdi-account-school-outline',
    tone: 'blue',
    value: students.value.length,
    label: 'Hồ sơ sinh viên',
    hint: 'Nguồn /students',
  },
  {
    icon: 'mdi-clipboard-clock-outline',
    tone: 'warning',
    value: pendingRegistrations.value,
    label: 'Đơn chờ duyệt',
    hint: 'Cần xếp phòng',
  },
  {
    icon: 'mdi-file-sign',
    tone: 'purple',
    value: pendingSignatureContracts.value,
    label: 'Chờ ký online',
    hint: 'Cần nhắc sinh viên',
  },
  {
    icon: 'mdi-bed-outline',
    tone: 'success',
    value: roomTotals.value.available,
    label: 'Giường trống',
    hint: 'Nguồn RoomService',
  },
  {
    icon: 'mdi-alert-circle-outline',
    tone: operationalAlerts.value.length > 1 ? 'danger' : 'success',
    value: actionableAlertTotal.value,
    label: 'Việc cần xử lý',
    hint: 'Tổng cảnh báo vận hành',
  },
])

const maxFunnelValue = computed(() =>
  Math.max(
    students.value.length,
    registrations.value.length,
    pendingRegistrations.value,
    approvedRegistrations.value,
    contracts.value.length,
    activeContracts.value,
    1,
  ),
)

const funnelSteps = computed(() => [
  {
    icon: 'mdi-account-school-outline',
    label: 'Hồ sơ',
    value: students.value.length,
    hint: 'Sinh viên đã được nhà trường tạo hồ sơ',
  },
  {
    icon: 'mdi-form-select',
    label: 'Đăng ký nội trú',
    value: registrations.value.length,
    hint: 'Đơn online đã gửi về hệ thống',
  },
  {
    icon: 'mdi-clipboard-clock-outline',
    label: 'Chờ duyệt',
    value: pendingRegistrations.value,
    hint: 'Admin cần chọn phòng hoặc tự xếp',
  },
  {
    icon: 'mdi-bed-outline',
    label: 'Đã xếp phòng',
    value: approvedRegistrations.value,
    hint: 'Đã đối chiếu giường trống',
  },
  {
    icon: 'mdi-file-document-edit-outline',
    label: 'Hợp đồng',
    value: contracts.value.length,
    hint: 'Tự khởi tạo sau khi duyệt',
  },
  {
    icon: 'mdi-file-check-outline',
    label: 'Hiệu lực',
    value: activeContracts.value,
    hint: 'Đang quản lý lưu trú',
  },
].map((step) => ({
  ...step,
  percent: Math.max(6, Math.round((Number(step.value || 0) / maxFunnelValue.value) * 100)),
})))

const registrationChartRows = computed(() => {
  const rows = groupByStatus(registrations.value, registrationStatusLabel)
  return rows.filter((row) => row.value > 0)
})

const contractChartRows = computed(() => {
  const rows = groupByStatus(contracts.value, contractStatusLabel)
  return rows.filter((row) => row.value > 0)
})

const facultyRows = computed(() =>
  Object.values(
    students.value.reduce((groups, student) => {
      const label = student.facultyName || 'Chưa cập nhật'
      groups[label] ||= { label, value: 0 }
      groups[label].value += 1
      return groups
    }, {}),
  )
    .sort((first, second) => second.value - first.value)
    .slice(0, 8),
)

const buildingRows = computed(() => {
  const groups = rooms.value.reduce((acc, room) => {
    const building = String(room.buildingName || room.buildingId || 'Chưa rõ').replace(/^Tòa\s*/i, '')
    const label = building === 'Chưa rõ' ? building : `Tòa ${building}`
    const capacity = Number(room.capacity || 0)
    const occupied = Number(room.occupiedBeds ?? room.currentOccupancy ?? 0)
    const available = Number(room.availableBeds ?? Math.max(capacity - occupied, 0))

    acc[label] ||= { label, capacity: 0, occupied: 0, available: 0 }
    acc[label].capacity += capacity
    acc[label].occupied += occupied
    acc[label].available += available
    return acc
  }, {})

  return Object.values(groups).sort((first, second) => first.label.localeCompare(second.label, 'vi'))
})

const actionableAlertTotal = computed(() =>
  pendingRegistrations.value +
  pendingSignatureContracts.value +
  expiringSoonContracts.value.length +
  missingContactStudents.value.length +
  stayingWithoutContract.value.length,
)

const operationalAlerts = computed(() => {
  const alerts = []

  if (rooms.value.length === 0) {
    alerts.push({
      icon: 'mdi-lan-disconnect',
      tone: 'danger',
      title: 'Chưa đồng bộ RoomService',
      description: 'Không có dữ liệu giường trống nên không thể đánh giá khả năng xếp phòng.',
      count: 1,
      to: '/facility/rooms',
    })
  }

  if (pendingRegistrations.value > 0) {
    alerts.push({
      icon: 'mdi-clipboard-clock-outline',
      tone: 'warning',
      title: 'Đơn đăng ký đang chờ duyệt',
      description: 'Cần kiểm tra nguyện vọng, phòng trống và xếp chỗ cho sinh viên.',
      count: pendingRegistrations.value,
      to: '/student-service/registrations/approval',
    })
  }

  if (pendingSignatureContracts.value > 0) {
    alerts.push({
      icon: 'mdi-file-sign',
      tone: 'warning',
      title: 'Hợp đồng chưa ký online',
      description: 'Nên gửi nhắc ký để hoàn tất hồ sơ pháp lý.',
      count: pendingSignatureContracts.value,
      to: '/student-service/contracts/manage',
    })
  }

  if (expiringSoonContracts.value.length > 0) {
    alerts.push({
      icon: 'mdi-calendar-alert-outline',
      tone: 'danger',
      title: 'Hợp đồng sắp hết hạn',
      description: 'Theo dõi mốc 30 ngày để gia hạn hoặc kết thúc đúng quy trình.',
      count: expiringSoonContracts.value.length,
      to: '/student-service/contracts/manage',
    })
  }

  if (missingContactStudents.value.length > 0) {
    alerts.push({
      icon: 'mdi-account-alert-outline',
      tone: 'warning',
      title: 'Hồ sơ thiếu thông tin liên hệ',
      description: 'CCCD, số điện thoại và email cần đủ để gửi hợp đồng, thông báo, hóa đơn.',
      count: missingContactStudents.value.length,
      to: '/student-service/students',
    })
  }

  if (stayingWithoutContract.value.length > 0) {
    alerts.push({
      icon: 'mdi-file-search-outline',
      tone: 'danger',
      title: 'Sinh viên đang ở nhưng thiếu hợp đồng',
      description: 'Đối chiếu sinh viên Active với hợp đồng Active hoặc PendingSignature.',
      count: stayingWithoutContract.value.length,
      to: '/student-service/contracts',
    })
  }

  if (alerts.length === 0) {
    alerts.push({
      icon: 'mdi-check-circle-outline',
      tone: 'success',
      title: 'Vận hành N2 đang ổn định',
      description: 'Không phát hiện đơn tồn, hợp đồng sắp hết hạn hoặc hồ sơ thiếu thông tin.',
      count: 0,
      to: '/student-service/dashboard',
    })
  }

  return alerts
})

const countByRegistrationStatus = (keys) =>
  registrations.value.filter((registration) => keys.includes(statusKey(registration.status))).length

const countByContractStatus = (keys) =>
  contracts.value.filter((contract) => keys.includes(statusKey(contract.status))).length

const groupByStatus = (rows, labelResolver) => {
  const groups = rows.reduce((acc, row) => {
    const label = labelResolver(row.status)
    acc[label] ||= { label, value: 0 }
    acc[label].value += 1
    return acc
  }, {})

  return Object.values(groups).sort((first, second) => second.value - first.value)
}

const registrationStatusLabel = (status) => ({
  pending: 'Chờ duyệt',
  approved: 'Đã duyệt',
  assigned: 'Đã xếp phòng',
  rejected: 'Từ chối',
}[statusKey(status)] || (status || 'Chưa rõ'))

const contractStatusLabel = (status) => ({
  active: 'Hiệu lực',
  pendingsignature: 'Chờ ký online',
  expired: 'Hết hạn',
  cancelled: 'Đã hủy',
  canceled: 'Đã hủy',
}[statusKey(status)] || (status || 'Chưa rõ'))

const destroyCharts = () => {
  registrationChart?.destroy()
  contractChart?.destroy()
  buildingChart?.destroy()
  facultyChart?.destroy()
  registrationChart = null
  contractChart = null
  buildingChart = null
  facultyChart = null
}

const chartOptions = {
  responsive: true,
  maintainAspectRatio: false,
  plugins: {
    legend: {
      position: 'bottom',
      labels: {
        boxWidth: 10,
        boxHeight: 10,
        usePointStyle: true,
      },
    },
  },
}

const renderCharts = async () => {
  await nextTick()
  destroyCharts()

  if (registrationCanvas.value && registrationChartRows.value.length) {
    registrationChart = createDoughnutChart(registrationCanvas.value, registrationChartRows.value)
  }

  if (contractCanvas.value && contractChartRows.value.length) {
    contractChart = createDoughnutChart(contractCanvas.value, contractChartRows.value, [
      '#16a34a',
      '#2563eb',
      '#f97316',
      '#dc2626',
      '#64748b',
    ])
  }

  if (buildingCanvas.value && buildingRows.value.length) {
    buildingChart = new Chart(buildingCanvas.value, {
      type: 'bar',
      data: {
        labels: buildingRows.value.map((row) => row.label),
        datasets: [
          {
            label: 'Đang ở',
            data: buildingRows.value.map((row) => row.occupied),
            backgroundColor: '#0f766e',
            borderRadius: 5,
          },
          {
            label: 'Còn trống',
            data: buildingRows.value.map((row) => row.available),
            backgroundColor: '#f97316',
            borderRadius: 5,
          },
        ],
      },
      options: {
        ...chartOptions,
        scales: {
          x: { stacked: true, grid: { display: false } },
          y: { stacked: true, beginAtZero: true, ticks: { precision: 0 } },
        },
      },
    })
  }

  if (facultyCanvas.value && facultyRows.value.length) {
    facultyChart = new Chart(facultyCanvas.value, {
      type: 'bar',
      data: {
        labels: facultyRows.value.map((row) => row.label),
        datasets: [
          {
            label: 'Sinh viên',
            data: facultyRows.value.map((row) => row.value),
            backgroundColor: '#2563eb',
            borderRadius: 5,
          },
        ],
      },
      options: {
        ...chartOptions,
        indexAxis: 'y',
        scales: {
          x: { beginAtZero: true, ticks: { precision: 0 } },
          y: { grid: { display: false } },
        },
      },
    })
  }
}

const createDoughnutChart = (canvas, rows, colors = ['#f97316', '#16a34a', '#dc2626', '#2563eb', '#64748b']) =>
  new Chart(canvas, {
    type: 'doughnut',
    data: {
      labels: rows.map((row) => row.label),
      datasets: [
        {
          data: rows.map((row) => row.value),
          backgroundColor: colors,
          borderColor: '#ffffff',
          borderWidth: 4,
          hoverOffset: 6,
        },
      ],
    },
    options: {
      ...chartOptions,
      cutout: '64%',
    },
  })

const loadReports = async () => {
  loading.value = true
  error.value = ''

  const results = await Promise.allSettled([
    api.get('/students'),
    api.get('/registrations'),
    api.get('/contracts'),
    api.get('/rooms'),
  ])

  const [studentRes, registrationRes, contractRes, roomRes] = results
  students.value = studentRes.status === 'fulfilled' ? cleanStudents(studentRes.value.data) : []
  registrations.value = registrationRes.status === 'fulfilled' ? normalizeList(registrationRes.value.data) : []
  contracts.value = contractRes.status === 'fulfilled' ? normalizeList(contractRes.value.data) : []
  rooms.value = roomRes.status === 'fulfilled' ? normalizeList(roomRes.value.data) : []

  const failed = results
    .map((result, index) => result.status === 'rejected' ? ['hồ sơ', 'đăng ký', 'hợp đồng', 'phòng'][index] : '')
    .filter(Boolean)

  if (failed.length) {
    error.value = `Chưa tải được dữ liệu ${failed.join(', ')}. Các biểu đồ còn lại vẫn hiển thị theo dữ liệu đã nhận.`
  }

  loading.value = false
  await renderCharts()
}

const exportReport = () => {
  const rows = [
    ...kpiMetrics.value.map((metric) => ({
      group: 'Chỉ số tổng quan',
      name: metric.label,
      value: metric.value,
      note: metric.hint,
    })),
    ...operationalAlerts.value.map((alert) => ({
      group: 'Cảnh báo vận hành',
      name: alert.title,
      value: alert.count,
      note: alert.description,
    })),
    ...buildingRows.value.map((row) => ({
      group: 'Sức chứa theo tòa',
      name: row.label,
      value: row.available,
      note: `${row.occupied}/${row.capacity} giường đang sử dụng`,
    })),
  ]

  exportRowsToExcel({
    filename: 'bao-cao-noi-tru.xls',
    sheetName: 'Báo cáo nội trú',
    rows,
    columns: [
      { header: 'Nhóm dữ liệu', value: (row) => row.group },
      { header: 'Chỉ mục', value: (row) => row.name },
      { header: 'Giá trị', value: (row) => row.value },
      { header: 'Ghi chú', value: (row) => row.note },
    ],
  })
}

onMounted(loadReports)
onBeforeUnmount(destroyCharts)
</script>

<style scoped>
.reports-page {
  display: flex;
  flex-direction: column;
  gap: 18px;
}

.reports-hero {
  display: grid;
  grid-template-columns: minmax(0, 1fr) auto;
  gap: 24px;
  align-items: center;
  padding: 30px;
  border: 1px solid rgba(249, 115, 22, 0.16);
  border-radius: 8px;
  background:
    linear-gradient(135deg, rgba(255, 247, 237, 0.92), rgba(240, 253, 250, 0.86)),
    #ffffff;
  box-shadow: 0 16px 40px rgba(67, 40, 24, 0.08);
}

.page-kicker,
.panel-kicker {
  color: #f97316;
  font-size: 12px;
  font-weight: 900;
  letter-spacing: 0;
  text-transform: uppercase;
}

.reports-hero h2 {
  margin: 8px 0;
  color: #24170f;
  font-size: clamp(32px, 5vw, 58px);
  line-height: 1;
}

.reports-hero p {
  max-width: 720px;
  margin: 0;
  color: #765f4e;
  font-size: 16px;
  line-height: 1.7;
}

.hero-actions {
  display: flex;
  flex-wrap: wrap;
  justify-content: flex-end;
  gap: 10px;
}

.kpi-grid,
.charts-grid,
.insight-grid {
  display: grid;
  gap: 14px;
}

.kpi-grid {
  grid-template-columns: repeat(5, minmax(0, 1fr));
}

.kpi-card,
.report-panel {
  border: 1px solid rgba(120, 77, 44, 0.14);
  border-radius: 8px;
  background: rgba(255, 255, 255, 0.94);
  box-shadow: 0 12px 28px rgba(67, 40, 24, 0.06);
}

.kpi-card {
  display: flex;
  gap: 14px;
  align-items: center;
  min-height: 118px;
  padding: 18px;
}

.kpi-card .mdi {
  display: grid;
  place-items: center;
  width: 46px;
  height: 46px;
  border-radius: 8px;
  background: #fff7ed;
  color: #f97316;
  font-size: 24px;
}

.kpi-card .mdi.success {
  background: #dcfce7;
  color: #16a34a;
}

.kpi-card .mdi.warning {
  background: #fef3c7;
  color: #d97706;
}

.kpi-card .mdi.danger {
  background: #fee2e2;
  color: #dc2626;
}

.kpi-card .mdi.blue {
  background: #dbeafe;
  color: #2563eb;
}

.kpi-card .mdi.purple {
  background: #ede9fe;
  color: #7c3aed;
}

.kpi-card strong {
  display: block;
  color: #24170f;
  font-size: 31px;
  line-height: 1;
}

.kpi-card p {
  margin: 4px 0 2px;
  color: #372318;
  font-weight: 800;
}

.kpi-card small,
.funnel-step small,
.alert-row small,
.building-row small {
  color: #7c6a5e;
}

.report-panel {
  padding: 22px;
}

.panel-heading {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 14px;
  margin-bottom: 18px;
}

.panel-heading.compact {
  margin-bottom: 10px;
}

.panel-heading h3 {
  margin: 4px 0 0;
  color: #24170f;
  font-size: 22px;
}

.panel-heading a {
  color: #0f766e;
  font-weight: 800;
  text-decoration: none;
}

.funnel-track {
  display: grid;
  grid-template-columns: repeat(6, minmax(0, 1fr));
  gap: 10px;
}

.funnel-step {
  padding: 14px;
  border: 1px solid rgba(249, 115, 22, 0.16);
  border-radius: 8px;
  background: #fffaf6;
}

.step-top {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
}

.step-top .mdi {
  color: #f97316;
  font-size: 24px;
}

.step-top strong {
  color: #24170f;
  font-size: 28px;
}

.funnel-step p {
  min-height: 44px;
  margin: 10px 0;
  color: #3b2a20;
  font-weight: 850;
}

.step-bar {
  height: 8px;
  overflow: hidden;
  border-radius: 999px;
  background: #f1e4d9;
}

.step-bar i {
  display: block;
  height: 100%;
  border-radius: inherit;
  background: linear-gradient(90deg, #f97316, #16a34a);
}

.charts-grid {
  grid-template-columns: repeat(4, minmax(0, 1fr));
}

.chart-panel {
  min-height: 348px;
}

.chart-panel.wide {
  grid-column: span 2;
}

.chart-canvas {
  position: relative;
  height: 250px;
}

.wide-canvas {
  height: 270px;
}

.empty-chart,
.empty-line {
  display: grid;
  place-items: center;
  min-height: 220px;
  color: #8a776a;
  text-align: center;
}

.empty-chart .mdi {
  display: block;
  margin-bottom: 8px;
  color: #f97316;
  font-size: 34px;
}

.insight-grid {
  grid-template-columns: minmax(0, 1.2fr) minmax(340px, 0.8fr);
}

.alert-list,
.building-list {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.alert-row,
.building-row {
  display: flex;
  align-items: center;
  gap: 14px;
  padding: 14px;
  border-radius: 8px;
  background: #fffaf6;
  text-decoration: none;
}

.alert-row {
  border-left: 4px solid #16a34a;
  color: inherit;
}

.alert-row.warning {
  border-left-color: #f59e0b;
}

.alert-row.danger {
  border-left-color: #dc2626;
}

.alert-row .mdi {
  color: #f97316;
  font-size: 24px;
}

.alert-row div,
.building-row div {
  min-width: 0;
  flex: 1;
}

.alert-row strong,
.building-row strong {
  display: block;
  color: #2a1d15;
}

.alert-row b,
.building-row b {
  color: #24170f;
  font-size: 22px;
}

.capacity-summary {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 8px;
  margin-bottom: 14px;
}

.capacity-summary div {
  padding: 14px;
  border-radius: 8px;
  background: #f8fafc;
}

.capacity-summary span {
  display: block;
  color: #7c6a5e;
  font-size: 12px;
}

.capacity-summary strong {
  color: #24170f;
  font-size: 26px;
}

.building-row {
  justify-content: space-between;
  border: 1px solid rgba(120, 77, 44, 0.1);
}

@media (max-width: 1180px) {
  .kpi-grid {
    grid-template-columns: repeat(3, minmax(0, 1fr));
  }

  .funnel-track {
    grid-template-columns: repeat(3, minmax(0, 1fr));
  }

  .charts-grid,
  .insight-grid {
    grid-template-columns: 1fr;
  }

  .chart-panel.wide {
    grid-column: auto;
  }
}

@media (max-width: 760px) {
  .reports-hero {
    grid-template-columns: 1fr;
    padding: 20px;
  }

  .hero-actions {
    justify-content: stretch;
  }

  .hero-actions :deep(.v-btn) {
    flex: 1;
  }

  .kpi-grid,
  .funnel-track {
    grid-template-columns: 1fr;
  }

  .panel-heading,
  .alert-row,
  .building-row {
    align-items: flex-start;
  }

  .capacity-summary {
    grid-template-columns: 1fr;
  }
}
</style>
