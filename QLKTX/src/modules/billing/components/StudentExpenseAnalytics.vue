<template>
  <section class="analytics-section">
    <header class="analytics-heading">
      <div class="heading-copy">
        <span class="mdi mdi-chart-box-outline"></span>
        <div>
          <span class="eyebrow">PHÂN TÍCH CHI PHÍ</span>
          <h3>Chi phí nội trú của bạn</h3>
          <p>Theo dõi tiền phòng, điện và nước. Tiền nạp ví không được tính là chi phí.</p>
        </div>
      </div>

      <v-btn-toggle v-model="range" mandatory divided density="comfortable" color="primary" variant="outlined">
        <v-btn value="6m">6 tháng</v-btn>
        <v-btn value="12m">12 tháng</v-btn>
        <v-btn value="year">Năm nay</v-btn>
      </v-btn-toggle>
    </header>

    <v-progress-linear v-if="loading" indeterminate color="success" />
    <v-alert v-if="error" type="warning" variant="tonal" density="compact">{{ error }}</v-alert>

    <template v-if="statistics">
      <div class="analytics-kpis">
        <article>
          <span>Chi phí {{ formatPeriod(statistics.summary.currentPeriod) }}</span>
          <strong>{{ formatMoney(statistics.summary.currentTotal) }}</strong>
          <small>Tổng tiền phòng, điện và nước</small>
        </article>
        <article>
          <span>Đã thanh toán kỳ này</span>
          <strong class="positive">{{ formatMoney(statistics.summary.currentPaid) }}</strong>
          <small>Đã ghi nhận vào lịch sử thanh toán</small>
        </article>
        <article>
          <span>Còn phải thanh toán</span>
          <strong class="warning">{{ formatMoney(statistics.summary.currentUnpaid) }}</strong>
          <small>Số tiền chưa hoàn thành trong kỳ</small>
        </article>
        <article>
          <span>So với tháng trước</span>
          <strong :class="changeClass">{{ changeLabel }}</strong>
          <small>{{ changeDescription }}</small>
        </article>
      </div>

      <div class="chart-grid">
        <article class="chart-panel trend-panel">
          <div class="chart-title">
            <div>
              <h4>Xu hướng chi phí theo tháng</h4>
              <p>Bấm vào một tháng để mở phiếu thanh toán tương ứng.</p>
            </div>
            <span>{{ rangeLabel }}</span>
          </div>
          <div class="chart-canvas trend-canvas">
            <canvas ref="trendCanvas" aria-label="Biểu đồ xu hướng chi phí nội trú"></canvas>
          </div>
        </article>

        <article class="chart-panel composition-panel">
          <div class="chart-title">
            <div>
              <h4>Cơ cấu tháng hiện tại</h4>
              <p>{{ formatPeriod(statistics.summary.currentPeriod) }}</p>
            </div>
          </div>
          <div v-if="hasCurrentExpense" class="chart-canvas composition-canvas">
            <canvas ref="compositionCanvas" aria-label="Biểu đồ cơ cấu chi phí tháng hiện tại"></canvas>
          </div>
          <div v-else class="empty-chart">
            <span class="mdi mdi-chart-donut-variant"></span>
            <strong>Chưa có chi phí trong tháng này</strong>
            <p>Biểu đồ sẽ xuất hiện sau khi phiếu thanh toán được lập.</p>
          </div>
        </article>
      </div>

      <div class="analytics-insights">
        <div><span>Tổng trong khoảng đã chọn</span><strong>{{ formatMoney(statistics.summary.rangeTotal) }}</strong></div>
        <div><span>Trung bình mỗi tháng</span><strong>{{ formatMoney(statistics.summary.averageMonthly) }}</strong></div>
        <div><span>Tháng chi phí cao nhất</span><strong>{{ highestMonthLabel }}</strong></div>
      </div>
    </template>
  </section>
</template>

<script setup>
import { computed, nextTick, onBeforeUnmount, ref, watch } from 'vue'
import {
  ArcElement,
  BarController,
  BarElement,
  CategoryScale,
  Chart,
  DoughnutController,
  Legend,
  LinearScale,
  LineController,
  LineElement,
  PointElement,
  Tooltip,
} from 'chart.js'
import api from '../../../services/api'

Chart.register(
  ArcElement,
  BarController,
  BarElement,
  CategoryScale,
  DoughnutController,
  Legend,
  LinearScale,
  LineController,
  LineElement,
  PointElement,
  Tooltip,
)

const props = defineProps({
  studentId: { type: Number, required: true },
  refreshKey: { type: Number, default: 0 },
})
const emit = defineEmits(['period-selected'])

const range = ref('6m')
const statistics = ref(null)
const loading = ref(false)
const error = ref('')
const trendCanvas = ref(null)
const compositionCanvas = ref(null)
let trendChart = null
let compositionChart = null

const monthCount = computed(() => {
  if (range.value === '12m') return 12
  if (range.value === 'year') return new Date().getMonth() + 1
  return 6
})
const rangeLabel = computed(() => ({ '6m': '6 tháng gần nhất', '12m': '12 tháng gần nhất', year: 'Từ đầu năm đến nay' }[range.value]))
const currentBreakdown = computed(() => statistics.value?.monthlyBreakdown?.at(-1) || null)
const hasCurrentExpense = computed(() => Number(currentBreakdown.value?.totalAmount || 0) > 0)
const changePercent = computed(() => statistics.value?.summary?.changePercent)
const changeLabel = computed(() => {
  if (changePercent.value === null || changePercent.value === undefined) return 'Chưa đủ dữ liệu'
  if (changePercent.value === 0) return 'Không thay đổi'
  return `${changePercent.value > 0 ? '+' : ''}${new Intl.NumberFormat('vi-VN', { maximumFractionDigits: 1 }).format(changePercent.value)}%`
})
const changeDescription = computed(() => {
  if (changePercent.value === null || changePercent.value === undefined) return 'Chưa có chi phí tháng trước để đối chiếu'
  if (changePercent.value > 0) return 'Chi phí tăng so với tháng trước'
  if (changePercent.value < 0) return 'Chi phí giảm so với tháng trước'
  return 'Chi phí bằng tháng trước'
})
const changeClass = computed(() => ({
  positive: Number(changePercent.value) < 0,
  warning: Number(changePercent.value) > 0,
  neutral: changePercent.value === null || changePercent.value === undefined,
}))
const highestMonthLabel = computed(() => {
  const summary = statistics.value?.summary
  return summary?.highestPeriod
    ? `${formatPeriod(summary.highestPeriod)} · ${formatMoney(summary.highestTotal)}`
    : 'Chưa có dữ liệu'
})

const formatMoney = (value) => new Intl.NumberFormat('vi-VN', {
  style: 'currency',
  currency: 'VND',
  maximumFractionDigits: 0,
}).format(Number(value || 0))
const formatCompactMoney = (value) => new Intl.NumberFormat('vi-VN', {
  notation: 'compact',
  maximumFractionDigits: 1,
}).format(Number(value || 0))
const formatPeriod = (period) => {
  const [year, month] = String(period || '').split('-')
  return year && month ? `tháng ${Number(month)}/${year}` : '-'
}

const chartTooltip = {
  callbacks: {
    label: (context) => `${context.dataset.label || context.label || 'Chi phí'}: ${formatMoney(context.raw)}`,
  },
}

const destroyCharts = () => {
  trendChart?.destroy()
  compositionChart?.destroy()
  trendChart = null
  compositionChart = null
}

const renderCharts = async () => {
  await nextTick()
  destroyCharts()
  const rows = statistics.value?.monthlyBreakdown || []

  if (trendCanvas.value && rows.length) {
    trendChart = new Chart(trendCanvas.value, {
      type: 'bar',
      data: {
        labels: rows.map((item) => formatPeriod(item.period).replace('tháng ', 'T')),
        datasets: [
          { label: 'Tiền phòng', data: rows.map((item) => item.roomFee), backgroundColor: '#2563eb', stack: 'expense', borderRadius: 3 },
          { label: 'Tiền điện', data: rows.map((item) => item.electricityAmount), backgroundColor: '#f59e0b', stack: 'expense', borderRadius: 3 },
          { label: 'Tiền nước', data: rows.map((item) => item.waterAmount), backgroundColor: '#06b6d4', stack: 'expense', borderRadius: 3 },
          {
            type: 'line',
            label: 'Tổng chi phí',
            data: rows.map((item) => item.totalAmount),
            borderColor: '#f36f21',
            backgroundColor: '#f36f21',
            pointBackgroundColor: '#ffffff',
            pointBorderColor: '#f36f21',
            pointBorderWidth: 2,
            pointRadius: 4,
            stack: 'total',
            tension: 0.25,
          },
        ],
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        interaction: { mode: 'index', intersect: false },
        onClick: (_event, elements) => {
          if (elements.length) emit('period-selected', rows[elements[0].index].period)
        },
        plugins: { legend: { position: 'bottom', labels: { usePointStyle: true, boxWidth: 9, padding: 18 } }, tooltip: chartTooltip },
        scales: {
          x: { stacked: true, grid: { display: false }, ticks: { color: '#65736a' } },
          y: { stacked: true, beginAtZero: true, ticks: { color: '#65736a', callback: formatCompactMoney }, grid: { color: '#e8eeea' } },
        },
      },
    })
  }

  if (compositionCanvas.value && hasCurrentExpense.value) {
    compositionChart = new Chart(compositionCanvas.value, {
      type: 'doughnut',
      data: {
        labels: ['Tiền phòng', 'Tiền điện', 'Tiền nước'],
        datasets: [{
          data: [currentBreakdown.value.roomFee, currentBreakdown.value.electricityAmount, currentBreakdown.value.waterAmount],
          backgroundColor: ['#2563eb', '#f59e0b', '#06b6d4'],
          borderColor: '#ffffff',
          borderWidth: 3,
          hoverOffset: 5,
        }],
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        cutout: '62%',
        plugins: { legend: { position: 'bottom', labels: { usePointStyle: true, boxWidth: 9, padding: 16 } }, tooltip: chartTooltip },
      },
    })
  }
}

const loadStatistics = async () => {
  if (!props.studentId) return
  loading.value = true
  error.value = ''
  try {
    const response = await api.get(`/billing/statistics/student/${props.studentId}?months=${monthCount.value}`)
    statistics.value = response.data
    await renderCharts()
  } catch (err) {
    error.value = err.response?.data?.message || err.message || 'Không tải được dữ liệu phân tích chi phí.'
    statistics.value = null
    destroyCharts()
  } finally {
    loading.value = false
  }
}

watch([() => props.studentId, () => props.refreshKey, range], loadStatistics, { immediate: true })
onBeforeUnmount(destroyCharts)
</script>

<style scoped>
.analytics-section { display: grid; gap: 16px; padding: 24px 0; border-top: 1px solid #f4ded0; border-bottom: 1px solid #f4ded0; }
.analytics-heading, .heading-copy, .chart-title { display: flex; align-items: center; justify-content: space-between; gap: 16px; }
.heading-copy { justify-content: flex-start; min-width: 0; }
.heading-copy > .mdi { display: grid; width: 44px; height: 44px; place-items: center; flex: 0 0 auto; border-radius: 7px; background: #fff3e8; color: #f36f21; font-size: 25px; }
.heading-copy h3, .chart-title h4 { margin: 0; color: #24150e; }
.heading-copy p, .chart-title p { margin: 4px 0 0; color: #7a6a5d; }
.eyebrow { color: #c2410c; font-size: 12px; font-weight: 900; }
.analytics-kpis { display: grid; grid-template-columns: repeat(4, minmax(0, 1fr)); gap: 12px; }
.analytics-kpis article { display: grid; min-width: 0; gap: 6px; padding: 17px; border: 1px solid #f4ded0; border-radius: 8px; background: #fff; }
.analytics-kpis span, .analytics-kpis small { color: #66746c; }
.analytics-kpis strong { color: #14261c; font-size: 22px; overflow-wrap: anywhere; }
.analytics-kpis strong.positive { color: #c2410c; }
.analytics-kpis strong.warning { color: #b45309; }
.analytics-kpis strong.neutral { color: #66746c; font-size: 17px; }
.chart-grid { display: grid; grid-template-columns: minmax(0, 1.65fr) minmax(300px, .75fr); gap: 14px; }
.chart-panel { min-width: 0; padding: 18px; border: 1px solid #f4ded0; border-radius: 8px; background: #fff; }
.chart-title { align-items: flex-start; }
.chart-title > span { padding: 5px 9px; border-radius: 6px; background: #fff3e8; color: #c2410c; font-size: 12px; font-weight: 800; white-space: nowrap; }
.chart-canvas { position: relative; width: 100%; margin-top: 14px; }
.trend-canvas { height: 330px; }
.composition-canvas { height: 330px; }
.empty-chart { display: grid; min-height: 330px; place-content: center; justify-items: center; color: #718078; text-align: center; }
.empty-chart .mdi { color: #9aa8a0; font-size: 56px; }
.empty-chart strong { margin-top: 8px; color: #425048; }
.empty-chart p { max-width: 280px; margin: 5px 0 0; }
.analytics-insights { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); border: 1px solid #dce5df; border-radius: 8px; background: #f8fbf9; }
.analytics-insights > div { display: grid; gap: 5px; padding: 14px 17px; border-right: 1px solid #dce5df; }
.analytics-insights > div:last-child { border-right: 0; }
.analytics-insights span { color: #68756d; }
.analytics-insights strong { color: #17432e; }
@media (max-width: 1100px) { .analytics-kpis { grid-template-columns: repeat(2, 1fr); } .chart-grid { grid-template-columns: 1fr; } }
@media (max-width: 700px) { .analytics-heading { align-items: stretch; flex-direction: column; } .analytics-heading :deep(.v-btn-toggle) { width: 100%; } .analytics-heading :deep(.v-btn) { flex: 1; } .analytics-kpis, .analytics-insights { grid-template-columns: 1fr; } .analytics-insights > div { border-right: 0; border-bottom: 1px solid #dce5df; } .analytics-insights > div:last-child { border-bottom: 0; } .trend-canvas, .composition-canvas { height: 290px; } }
</style>
