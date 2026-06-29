<template>
  <section class="billing-page">
    <div class="page-heading">
      <div>
        <span class="eyebrow">BILLING SERVICE</span>
        <h2>Thanh toán điện, nước hàng tháng</h2>
        <p>Nhập chỉ số, phát hành phiếu, gửi email và theo dõi giao dịch tự động.</p>
      </div>
      <v-btn prepend-icon="mdi-refresh" variant="outlined" :loading="loading" @click="loadAll">
        Làm mới
      </v-btn>
    </div>

    <v-alert v-if="error" type="error" variant="tonal" closable class="mb-4" @click:close="error = ''">
      {{ error }}
    </v-alert>
    <v-alert v-if="success" type="success" variant="tonal" closable class="mb-4" @click:close="success = ''">
      {{ success }}
    </v-alert>

    <div class="metric-grid">
      <article>
        <span>Chưa thanh toán</span>
        <strong>{{ unpaidInvoices.length }}</strong>
        <small>{{ formatMoney(unpaidTotal) }}</small>
      </article>
      <article>
        <span>Đã thanh toán</span>
        <strong>{{ paidInvoices.length }}</strong>
        <small>{{ formatMoney(paidTotal) }}</small>
      </article>
      <article>
        <span>Đơn giá điện</span>
        <strong>4.000đ</strong>
        <small>Mỗi số điện</small>
      </article>
      <article>
        <span>Đơn giá nước</span>
        <strong>20.000đ</strong>
        <small>Mỗi số nước</small>
      </article>
    </div>

    <section class="issue-panel">
      <div class="section-title">
        <div>
          <span class="mdi mdi-receipt-text-plus-outline"></span>
          <div>
            <h3>Phát hành phiếu theo phòng</h3>
            <p>Nhập điện nước một lần cho cả phòng, hệ thống chia theo số ngày ở thực tế và tạo phiếu riêng cho từng sinh viên.</p>
          </div>
        </div>
        <span class="rate-note">Điện 4.000đ/số · Nước 20.000đ/số</span>
      </div>

      <v-alert v-if="issueError" type="error" variant="tonal" closable class="mb-4" @click:close="issueError = ''">
        {{ issueError }}
      </v-alert>
      <v-alert v-if="issueSuccess" type="success" variant="tonal" closable class="mb-4" @click:close="issueSuccess = ''">
        {{ issueSuccess }}
      </v-alert>

      <div class="form-grid">
        <v-select
          v-model="form.roomId"
          :items="roomOptions"
          item-title="title"
          item-value="value"
          label="Phòng cần lập phiếu"
          variant="outlined"
          density="comfortable"
          @update:model-value="onRoomChanged"
        />
        <v-text-field
          v-model="form.billingPeriod"
          type="month"
          label="Tháng thanh toán"
          variant="outlined"
          density="comfortable"
        />
        <v-text-field
          v-model="form.dueDate"
          type="date"
          label="Hạn thanh toán"
          variant="outlined"
          density="comfortable"
        />
        <v-text-field
          v-model="form.roomName"
          label="Phòng"
          variant="outlined"
          density="comfortable"
        />
      </div>

      <div class="meter-grid">
        <div class="meter-box electric">
          <div class="meter-title">
            <span class="mdi mdi-flash-outline"></span>
            <strong>Chỉ số điện</strong>
          </div>
          <div class="meter-inputs">
            <v-text-field v-model.number="form.previousElectricityReading" type="number" min="0" label="Chỉ số cũ" variant="outlined" density="compact" />
            <span class="mdi mdi-arrow-right"></span>
            <v-text-field v-model.number="form.currentElectricityReading" type="number" min="0" label="Chỉ số mới" variant="outlined" density="compact" />
          </div>
          <p>{{ electricityUsage }} số × 4.000đ = <b>{{ formatMoney(electricityAmount) }}</b></p>
        </div>

        <div class="meter-box water">
          <div class="meter-title">
            <span class="mdi mdi-water-outline"></span>
            <strong>Chỉ số nước</strong>
          </div>
          <div class="meter-inputs">
            <v-text-field v-model.number="form.previousWaterReading" type="number" min="0" label="Chỉ số cũ" variant="outlined" density="compact" />
            <span class="mdi mdi-arrow-right"></span>
            <v-text-field v-model.number="form.currentWaterReading" type="number" min="0" label="Chỉ số mới" variant="outlined" density="compact" />
          </div>
          <p>{{ waterUsage }} số × 20.000đ = <b>{{ formatMoney(waterAmount) }}</b></p>
        </div>
      </div>

      <div class="occupant-panel">
        <div class="occupant-head">
          <div>
            <strong>{{ occupantRows.length }} sinh viên/hợp đồng trong phòng</strong>
            <small>Chỉ lấy hợp đồng đang hiệu lực và có ngày ở giao với kỳ {{ form.billingPeriod }}.</small>
          </div>
          <v-btn
            color="primary"
            variant="tonal"
            prepend-icon="mdi-calculator-variant-outline"
            :loading="previewing"
            :disabled="!canPreview"
            @click="previewAllocation"
          >
            Xem trước phân bổ
          </v-btn>
        </div>
        <v-table density="compact" class="allocation-table">
          <thead>
            <tr>
              <th>Sinh viên</th>
              <th>Hợp đồng</th>
              <th>Ngày ở</th>
              <th>Tiền phòng dự kiến</th>
              <th>Email</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="occupant in occupantRows" :key="occupant.contractId">
              <td><strong>{{ occupant.studentName }}</strong><small>{{ occupant.studentCode }}</small></td>
              <td><strong>{{ occupant.contractCode }}</strong><small>{{ formatDate(occupant.startDate) }} - {{ formatDate(occupant.endDate) }}</small></td>
              <td>{{ occupant.occupancyDays }}/{{ occupant.billingDays }} ngày</td>
              <td>{{ formatMoney(occupant.estimatedRoomFee) }}</td>
              <td :class="{ missing: !occupant.studentEmail }">{{ occupant.studentEmail || 'Thiếu email' }}</td>
            </tr>
            <tr v-if="occupantRows.length === 0">
              <td colspan="5" class="empty-row">Phòng này chưa có hợp đồng hiệu lực trong kỳ thanh toán đã chọn.</td>
            </tr>
          </tbody>
        </v-table>
      </div>

      <div v-if="roomPreview.length" class="preview-panel">
        <div class="preview-head">
          <div>
            <strong>Phân bổ điện nước dự kiến</strong>
            <small>Điện nước chia theo số ngày ở, tiền phòng tính theo hợp đồng và số ngày ở trong tháng.</small>
          </div>
          <span>{{ roomPreview.length }} phiếu sẽ được tạo</span>
        </div>
        <v-table density="compact" class="allocation-table">
          <thead>
            <tr>
              <th>Sinh viên</th>
              <th>Tiền phòng</th>
              <th>Điện phân bổ</th>
              <th>Nước phân bổ</th>
              <th>Tổng phiếu</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="item in roomPreview" :key="item.contractId">
              <td><strong>{{ item.studentName }}</strong><small>{{ item.allocationNote }}</small></td>
              <td>{{ formatMoney(item.roomFee) }}</td>
              <td>{{ item.electricityUsage }} số · {{ formatMoney(item.electricityAmount) }}</td>
              <td>{{ item.waterUsage }} số · {{ formatMoney(item.waterAmount) }}</td>
              <td><strong class="money">{{ formatMoney(item.totalAmount) }}</strong></td>
            </tr>
          </tbody>
        </v-table>
      </div>

      <div class="issue-footer">
        <div class="issue-summary">
          <div><span>Tổng điện nước phòng</span><strong>{{ formatMoney(utilityTotal) }}</strong></div>
          <div><span>Tổng các phiếu dự kiến</span><strong>{{ formatMoney(batchPreviewTotal) }}</strong></div>
        </div>
        <v-btn
          color="success"
          prepend-icon="mdi-printer-outline"
          size="large"
          :loading="issuing"
          :disabled="!canIssue"
          @click="issueAndPrint"
        >
          Phát hành, gửi email & in
        </v-btn>
      </div>
    </section>

    <section class="list-panel">
      <div class="section-title table-heading">
        <div>
          <span class="mdi mdi-file-document-multiple-outline"></span>
          <div>
            <h3>Danh sách phiếu thanh toán</h3>
            <p>Webhook ngân hàng sẽ tự đổi trạng thái khi đúng mã và đủ tiền.</p>
          </div>
        </div>
        <div class="table-tools">
          <v-text-field
            v-model="invoiceSearch"
            prepend-inner-icon="mdi-magnify"
            label="Tìm mã phiếu, sinh viên hoặc phòng"
            variant="outlined"
            density="compact"
            hide-details
            clearable
            class="search-field"
          />
          <v-select
            v-model="statusFilter"
            :items="statusOptions"
            item-title="title"
            item-value="value"
            label="Trạng thái"
            variant="outlined"
            density="compact"
            hide-details
            class="status-filter"
          />
          <v-select
            v-model="invoiceSort"
            :items="invoiceSortOptions"
            item-title="title"
            item-value="value"
            label="Sắp xếp theo"
            variant="outlined"
            density="compact"
            hide-details
            class="sort-filter"
          />
          <v-btn
            color="success"
            variant="tonal"
            prepend-icon="mdi-file-excel-outline"
            :disabled="filteredInvoices.length === 0"
            @click="exportInvoices"
          >
            Xuất Excel
          </v-btn>
        </div>
      </div>

      <v-data-table
        :headers="invoiceHeaders"
        :items="filteredInvoices"
        :loading="loading"
        item-value="id"
        :items-per-page="8"
        :items-per-page-options="[5, 8, 15, 30]"
        items-per-page-text="Số dòng mỗi trang"
        density="comfortable"
        hover
        class="ant-table"
        loading-text="Đang tải danh sách phiếu..."
        no-data-text="Chưa có phiếu thanh toán phù hợp."
      >
        <template #item.invoiceCode="{ item }">
          <div class="cell-stack">
            <strong>{{ item.invoiceCode }}</strong>
            <small>{{ formatDate(item.issuedAt) }}</small>
          </div>
        </template>
        <template #item.studentName="{ item }">
          <div class="cell-stack">
            <strong>{{ item.studentName }}</strong>
            <small>{{ item.studentCode }}</small>
          </div>
        </template>
        <template #item.billingPeriod="{ item }">
          <div class="cell-stack">
            <strong>{{ item.billingPeriod }}</strong>
            <small>Phòng {{ item.roomName }}</small>
          </div>
        </template>
        <template #item.utilityUsage="{ item }">
          <div class="cell-stack">
            <strong>{{ item.electricityUsage }} · {{ item.waterUsage }}</strong>
            <small>Điện · Nước</small>
          </div>
        </template>
        <template #item.totalAmount="{ item }">
          <div class="cell-stack">
            <strong class="money">{{ formatMoney(item.totalAmount) }}</strong>
            <small>Hạn {{ formatDate(item.dueDate) }}</small>
          </div>
        </template>
        <template #item.status="{ item }">
          <span :class="['status-pill', item.status.toLowerCase()]">{{ statusLabel(item.status) }}</span>
        </template>
        <template #item.actions="{ item }">
          <v-menu location="bottom end">
            <template #activator="{ props }">
              <v-btn v-bind="props" icon="mdi-dots-horizontal" variant="text" density="comfortable" title="Thao tác" />
            </template>
            <v-list density="compact" min-width="220">
              <v-list-item prepend-icon="mdi-eye-outline" title="Xem chi tiết" @click="openDetail(item)" />
              <v-list-item prepend-icon="mdi-printer-outline" title="In phiếu" @click="printInvoice(item)" />
              <v-list-item prepend-icon="mdi-email-fast-outline" title="Gửi lại email" @click="resendEmail(item)" />
              <v-list-item v-if="item.status !== 'Paid'" prepend-icon="mdi-check-decagram-outline" title="Xác nhận đã thanh toán" @click="markPaid(item)" />
            </v-list>
          </v-menu>
        </template>
      </v-data-table>
    </section>

    <section class="history-panel">
      <div class="section-title table-heading">
        <div>
          <span class="mdi mdi-history"></span>
          <div><h3>Lịch sử thanh toán</h3><p>Dùng chung cho tài khoản admin và nhân viên.</p></div>
        </div>
        <div class="history-tools">
          <v-text-field
            v-model="historySearch"
            prepend-inner-icon="mdi-magnify"
            label="Tìm sinh viên hoặc mã giao dịch"
            variant="outlined"
            density="compact"
            hide-details
            clearable
            class="history-search"
          />
          <v-select
            v-model="historySort"
            :items="historySortOptions"
            item-title="title"
            item-value="value"
            label="Sắp xếp theo"
            variant="outlined"
            density="compact"
            hide-details
            class="sort-filter"
          />
        </div>
      </div>
      <v-data-table
        :headers="historyHeaders"
        :items="filteredHistory"
        :loading="loading"
        item-value="id"
        :items-per-page="6"
        :items-per-page-options="[6, 12, 24]"
        items-per-page-text="Số dòng mỗi trang"
        density="comfortable"
        hover
        class="ant-table"
        loading-text="Đang tải lịch sử thanh toán..."
        no-data-text="Chưa phát sinh giao dịch đã hoàn thành."
      >
        <template #item.studentName="{ item }">
          <div class="cell-stack history-student">
            <strong><span class="mdi mdi-check-circle-outline"></span>{{ item.studentName }}</strong>
            <small>{{ item.studentCode }}</small>
          </div>
        </template>
        <template #item.invoiceCode="{ item }">
          <div class="cell-stack">
            <strong>{{ item.invoiceCode }}</strong>
            <small>Kỳ {{ item.billingPeriod }}</small>
          </div>
        </template>
        <template #item.paidAt="{ item }">
          <div class="cell-stack">
            <strong>{{ formatDateTime(item.paidAt) }}</strong>
            <small>{{ item.referenceCode }}</small>
          </div>
        </template>
        <template #item.method="{ item }">
          <span class="method-tag">{{ paymentMethodLabel(item.method) }}</span>
        </template>
        <template #item.amount="{ item }">
          <strong class="money">{{ formatMoney(item.amount) }}</strong>
        </template>
      </v-data-table>
    </section>

    <v-dialog v-model="detailDialog" max-width="820">
      <v-card v-if="selectedInvoice" class="invoice-dialog">
        <v-card-title>Chi tiết {{ selectedInvoice.invoiceCode }}</v-card-title>
        <v-card-text>
          <div class="invoice-detail">
            <div class="detail-lines">
              <p><span>Sinh viên</span><strong>{{ selectedInvoice.studentName }} ({{ selectedInvoice.studentCode }})</strong></p>
              <p><span>Kỳ thanh toán</span><strong>{{ selectedInvoice.billingPeriod }}</strong></p>
              <p v-if="selectedInvoice.batchCode"><span>Đợt phát hành</span><strong>{{ selectedInvoice.batchCode }}</strong></p>
              <p><span>Tiền phòng</span><strong>{{ formatMoney(selectedInvoice.roomFee) }}</strong></p>
              <p><span>Điện phân bổ</span><strong>{{ selectedInvoice.electricityUsage }} số · {{ formatMoney(selectedInvoice.electricityAmount) }}</strong></p>
              <p><span>Nước phân bổ</span><strong>{{ selectedInvoice.waterUsage }} số · {{ formatMoney(selectedInvoice.waterAmount) }}</strong></p>
              <p v-if="selectedInvoice.roomOccupantCount"><span>Cách chia</span><strong>{{ selectedInvoice.occupancyDays }}/{{ selectedInvoice.billingDays }} ngày · {{ selectedInvoice.roomOccupantCount }} sinh viên</strong></p>
              <p v-if="selectedInvoice.roomElectricityUsage"><span>Điện cả phòng</span><strong>{{ selectedInvoice.roomElectricityUsage }} số · {{ formatMoney(selectedInvoice.roomElectricityAmount) }}</strong></p>
              <p v-if="selectedInvoice.roomWaterUsage"><span>Nước cả phòng</span><strong>{{ selectedInvoice.roomWaterUsage }} số · {{ formatMoney(selectedInvoice.roomWaterAmount) }}</strong></p>
              <p v-if="selectedInvoice.allocationNote"><span>Ghi chú phân bổ</span><strong>{{ selectedInvoice.allocationNote }}</strong></p>
              <p class="total"><span>Tổng thanh toán</span><strong>{{ formatMoney(selectedInvoice.totalAmount) }}</strong></p>
              <p><span>Nội dung chuyển khoản</span><strong>{{ selectedInvoice.paymentCode }}</strong></p>
            </div>
            <div class="qr-panel">
              <img v-if="selectedInvoice.qrCodeUrl" :src="selectedInvoice.qrCodeUrl" alt="Mã QR thanh toán" />
              <div v-else class="qr-missing"><span class="mdi mdi-qrcode-remove"></span><p>Chưa cấu hình tài khoản VietQR.</p></div>
            </div>
          </div>
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="detailDialog = false">Đóng</v-btn>
          <v-btn color="success" prepend-icon="mdi-printer-outline" @click="printInvoice(selectedInvoice)">In phiếu</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </section>
</template>

<script setup>
import { computed, onMounted, reactive, ref, watch } from 'vue'
import api from '../../../services/api'
import { exportRowsToExcel } from '@/utils/exportExcel'

const ELECTRICITY_RATE = 4000
const WATER_RATE = 20000
const loading = ref(false)
const issuing = ref(false)
const previewing = ref(false)
const error = ref('')
const success = ref('')
const issueError = ref('')
const issueSuccess = ref('')
const students = ref([])
const contracts = ref([])
const rooms = ref([])
const invoices = ref([])
const history = ref([])
const roomPreview = ref([])
const previewSummary = ref(null)
const statusFilter = ref('')
const invoiceSearch = ref('')
const historySearch = ref('')
const invoiceSort = ref('default')
const historySort = ref('default')
const detailDialog = ref(false)
const selectedInvoice = ref(null)

const invoiceHeaders = [
  { title: 'Mã phiếu', key: 'invoiceCode', sortable: false, minWidth: 150 },
  { title: 'Sinh viên', key: 'studentName', sortable: false, minWidth: 180 },
  { title: 'Kỳ / Phòng', key: 'billingPeriod', sortable: false, minWidth: 130 },
  { title: 'Điện · Nước', key: 'utilityUsage', sortable: false, minWidth: 120 },
  { title: 'Tổng tiền', key: 'totalAmount', sortable: false, align: 'end', minWidth: 150 },
  { title: 'Trạng thái', key: 'status', sortable: false, minWidth: 145 },
  { title: '', key: 'actions', sortable: false, align: 'end', width: 56 },
]

const historyHeaders = [
  { title: 'Sinh viên', key: 'studentName', sortable: false, minWidth: 190 },
  { title: 'Phiếu thanh toán', key: 'invoiceCode', sortable: false, minWidth: 160 },
  { title: 'Thời gian / Đối soát', key: 'paidAt', sortable: false, minWidth: 220 },
  { title: 'Phương thức', key: 'method', sortable: false, minWidth: 145 },
  { title: 'Người xác nhận', key: 'confirmedBy', sortable: false, minWidth: 160 },
  { title: 'Số tiền', key: 'amount', sortable: false, align: 'end', minWidth: 150 },
]

const today = new Date()
const defaultPeriod = `${today.getFullYear()}-${String(today.getMonth() + 1).padStart(2, '0')}`
const dueDate = new Date(today)
dueDate.setDate(dueDate.getDate() + 7)

const form = reactive({
  roomId: null,
  billingPeriod: defaultPeriod,
  dueDate: dueDate.toISOString().slice(0, 10),
  roomName: '',
  previousElectricityReading: 0,
  currentElectricityReading: 0,
  previousWaterReading: 0,
  currentWaterReading: 0,
})

const normalizeList = (payload) => {
  if (Array.isArray(payload)) return payload
  if (Array.isArray(payload?.data)) return payload.data
  if (Array.isArray(payload?.items)) return payload.items
  if (Array.isArray(payload?.data?.data)) return payload.data.data
  return []
}

const getRoomId = (room) => Number(room?.roomId ?? room?.id ?? 0)
const getRoomLabel = (room) => {
  if (!room) return ''
  const roomNumber = room.roomNumber || room.name || room.roomName || getRoomId(room)
  const building = room.buildingName || room.building || room.buildingCode
  return building ? `Phòng ${roomNumber} - Tòa ${building}` : `Phòng ${roomNumber}`
}
const roomMap = computed(() => new Map(rooms.value.map((room) => [getRoomId(room), room])))
const studentMap = computed(() => new Map(students.value.map((student) => [Number(student.id), student])))
const roomOptions = computed(() => rooms.value.map((room) => {
  const roomId = getRoomId(room)
  const residentCount = activeRoomContracts(roomId).length
  const capacity = Number(room.capacity || room.totalBeds || room.beds || 0)
  return {
    title: `${getRoomLabel(room)} · ${residentCount}${capacity ? `/${capacity}` : ''} sinh viên`,
    value: roomId,
  }
}))

const electricityUsage = computed(() => Math.max(0, Number(form.currentElectricityReading || 0) - Number(form.previousElectricityReading || 0)))
const waterUsage = computed(() => Math.max(0, Number(form.currentWaterReading || 0) - Number(form.previousWaterReading || 0)))
const electricityAmount = computed(() => electricityUsage.value * ELECTRICITY_RATE)
const waterAmount = computed(() => waterUsage.value * WATER_RATE)
const utilityTotal = computed(() => electricityAmount.value + waterAmount.value)
const batchPreviewTotal = computed(() => roomPreview.value.length
  ? roomPreview.value.reduce((sum, item) => sum + Number(item.totalAmount || 0), 0)
  : occupantRows.value.reduce((sum, item) => sum + Number(item.estimatedRoomFee || 0), 0) + utilityTotal.value)
const canPreview = computed(() => form.roomId && form.billingPeriod && form.dueDate && occupantRows.value.length > 0 && form.currentElectricityReading >= form.previousElectricityReading && form.currentWaterReading >= form.previousWaterReading)
const canIssue = computed(() => canPreview.value && roomPreview.value.length > 0)
const unpaidInvoices = computed(() => invoices.value.filter((item) => item.status !== 'Paid'))
const paidInvoices = computed(() => invoices.value.filter((item) => item.status === 'Paid'))
const unpaidTotal = computed(() => unpaidInvoices.value.reduce((sum, item) => sum + Number(item.totalAmount || 0), 0))
const paidTotal = computed(() => paidInvoices.value.reduce((sum, item) => sum + Number(item.totalAmount || 0), 0))
const normalizeSearch = (value) => String(value || '').trim().toLocaleLowerCase('vi')
const compareText = (left, right) => String(left || '').localeCompare(String(right || ''), 'vi', { sensitivity: 'base', numeric: true })
const compareDate = (left, right) => {
  const leftTime = new Date(left || 0).getTime()
  const rightTime = new Date(right || 0).getTime()
  return (Number.isNaN(leftTime) ? 0 : leftTime) - (Number.isNaN(rightTime) ? 0 : rightTime)
}
const sortItems = (items, sortValue, dateKey, amountKey) => {
  const sorted = [...items]

  switch (sortValue) {
    case 'newest': return sorted.sort((left, right) => compareDate(right[dateKey], left[dateKey]))
    case 'oldest': return sorted.sort((left, right) => compareDate(left[dateKey], right[dateKey]))
    case 'student-asc': return sorted.sort((left, right) => compareText(left.studentName, right.studentName))
    case 'student-desc': return sorted.sort((left, right) => compareText(right.studentName, left.studentName))
    case 'amount-asc': return sorted.sort((left, right) => Number(left[amountKey] || 0) - Number(right[amountKey] || 0))
    case 'amount-desc': return sorted.sort((left, right) => Number(right[amountKey] || 0) - Number(left[amountKey] || 0))
    default: return sorted
  }
}
const filteredInvoices = computed(() => {
  const keyword = normalizeSearch(invoiceSearch.value)

  const filtered = invoices.value.filter((item) => {
    const matchesStatus = !statusFilter.value || item.status === statusFilter.value
    const matchesKeyword = !keyword || [
      item.invoiceCode,
      item.studentName,
      item.studentCode,
      item.billingPeriod,
      item.roomName,
      item.paymentCode,
    ].some((value) => normalizeSearch(value).includes(keyword))

    return matchesStatus && matchesKeyword
  })

  return sortItems(filtered, invoiceSort.value, 'issuedAt', 'totalAmount')
})
const filteredHistory = computed(() => {
  const keyword = normalizeSearch(historySearch.value)
  const filtered = !keyword ? history.value : history.value.filter((item) => [
    item.studentName,
    item.studentCode,
    item.invoiceCode,
    item.referenceCode,
    item.confirmedBy,
    item.method,
  ].some((value) => normalizeSearch(value).includes(keyword)))

  return sortItems(filtered, historySort.value, 'paidAt', 'amount')
})

const statusOptions = [
  { title: 'Tất cả', value: '' },
  { title: 'Chưa thanh toán', value: 'Unpaid' },
  { title: 'Đã thanh toán', value: 'Paid' },
]

const invoiceSortOptions = [
  { title: 'Mặc định', value: 'default' },
  { title: 'Ngày phát hành: Mới nhất trước', value: 'newest' },
  { title: 'Ngày phát hành: Cũ nhất trước', value: 'oldest' },
  { title: 'Sinh viên: A → Z', value: 'student-asc' },
  { title: 'Sinh viên: Z → A', value: 'student-desc' },
  { title: 'Tổng tiền: Thấp → cao', value: 'amount-asc' },
  { title: 'Tổng tiền: Cao → thấp', value: 'amount-desc' },
]

const historySortOptions = [
  { title: 'Mặc định', value: 'default' },
  { title: 'Thời gian thanh toán: Mới nhất trước', value: 'newest' },
  { title: 'Thời gian thanh toán: Cũ nhất trước', value: 'oldest' },
  { title: 'Sinh viên: A → Z', value: 'student-asc' },
  { title: 'Sinh viên: Z → A', value: 'student-desc' },
  { title: 'Số tiền: Thấp → cao', value: 'amount-asc' },
  { title: 'Số tiền: Cao → thấp', value: 'amount-desc' },
]

const formatMoney = (value) => new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND', maximumFractionDigits: 0 }).format(Number(value || 0))
const formatDate = (value) => value ? new Intl.DateTimeFormat('vi-VN').format(new Date(value)) : '-'
const formatDateTime = (value) => value ? new Intl.DateTimeFormat('vi-VN', { dateStyle: 'short', timeStyle: 'short' }).format(new Date(value)) : '-'
const statusLabel = (status) => ({ Paid: 'Đã thanh toán', Unpaid: 'Chưa thanh toán', Active: 'Đang hiệu lực', Expired: 'Hết hạn', Cancelled: 'Đã hủy' }[status] || status || '-')
const paymentMethodLabel = (method) => ({
  BankWebhook: 'Ngân hàng tự động',
  WalletAutoPay: 'Ví KTX tự động',
  Manual: 'Xác nhận thủ công',
}[method] || method || 'Không xác định')
const parseLocalDate = (value, fallback = null) => {
  if (!value) return fallback
  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? fallback : date
}
const periodRange = computed(() => {
  const start = new Date(`${form.billingPeriod || defaultPeriod}-01T00:00:00`)
  const end = new Date(start)
  end.setMonth(end.getMonth() + 1)
  return { start, end, days: Math.round((end - start) / 86400000) }
})
const calculateOccupancyDays = (contract) => {
  const { start, end } = periodRange.value
  const contractStart = parseLocalDate(contract.startDate, start)
  const contractEnd = parseLocalDate(contract.endDate, new Date(end.getTime() - 86400000))
  const activeStart = contractStart > start ? contractStart : start
  const endExclusive = new Date((contractEnd < end ? contractEnd : new Date(end.getTime() - 86400000)).getTime())
  endExclusive.setDate(endExclusive.getDate() + 1)
  if (endExclusive <= activeStart) return 0
  return Math.round((endExclusive - activeStart) / 86400000)
}
const activeRoomContracts = (roomId) => contracts.value.filter((contract) =>
  Number(contract.roomId) === Number(roomId) &&
  contract.status === 'Active' &&
  calculateOccupancyDays(contract) > 0)
const occupantRows = computed(() => activeRoomContracts(form.roomId).map((contract) => {
  const student = studentMap.value.get(Number(contract.studentId)) || {}
  const occupancyDays = calculateOccupancyDays(contract)
  const billingDays = periodRange.value.days || 1
  return {
    contractId: Number(contract.id),
    contractCode: contract.contractCode,
    studentId: Number(contract.studentId),
    studentCode: student.studentCode || `SV${contract.studentId}`,
    studentName: student.fullName || 'Sinh viên',
    studentEmail: student.email || '',
    roomFee: Number(contract.monthlyFee || 0),
    estimatedRoomFee: Math.round(Number(contract.monthlyFee || 0) * occupancyDays / billingDays),
    startDate: contract.startDate,
    endDate: contract.endDate,
    occupancyDays,
    billingDays,
  }
}))

const loadAll = async () => {
  loading.value = true
  error.value = ''
  try {
    const [studentResponse, contractResponse, roomResponse, invoiceResponse, historyResponse] = await Promise.all([
      api.get('/students').catch(() => ({ data: [] })),
      api.get('/contracts').catch(() => ({ data: [] })),
      api.get('/rooms').catch(() => ({ data: [] })),
      api.get('/billing/monthly-invoices'),
      api.get('/billing/payment-history'),
    ])
    students.value = normalizeList(studentResponse.data)
    contracts.value = normalizeList(contractResponse.data)
    rooms.value = normalizeList(roomResponse.data)
    invoices.value = normalizeList(invoiceResponse.data)
    history.value = normalizeList(historyResponse.data)

    if (!form.roomId && rooms.value.length) {
      form.roomId = getRoomId(rooms.value[0])
      onRoomChanged()
    } else if (form.roomId) {
      const room = roomMap.value.get(Number(form.roomId))
      form.roomName = getRoomLabel(room) || form.roomName
    }
  } catch (err) {
    error.value = err.response?.data?.detail || err.response?.data?.message || 'Không tải được dữ liệu thanh toán.'
  } finally {
    loading.value = false
  }
}

const resetPreview = () => {
  roomPreview.value = []
  previewSummary.value = null
}

const onRoomChanged = () => {
  issueError.value = ''
  issueSuccess.value = ''
  const room = roomMap.value.get(Number(form.roomId))
  form.roomName = getRoomLabel(room) || String(form.roomId || '')

  const latest = invoices.value
    .filter((item) => Number(item.roomId) === Number(form.roomId))
    .sort((first, second) => new Date(second.issuedAt || 0) - new Date(first.issuedAt || 0))[0]

  form.previousElectricityReading = Number(latest?.currentElectricityReading || 0)
  form.currentElectricityReading = form.previousElectricityReading
  form.previousWaterReading = Number(latest?.currentWaterReading || 0)
  form.currentWaterReading = form.previousWaterReading
  resetPreview()
}

const buildRoomInvoicePayload = () => ({
  roomId: Number(form.roomId || 0),
  roomName: form.roomName,
  billingPeriod: form.billingPeriod,
  previousElectricityReading: Number(form.previousElectricityReading || 0),
  currentElectricityReading: Number(form.currentElectricityReading || 0),
  previousWaterReading: Number(form.previousWaterReading || 0),
  currentWaterReading: Number(form.currentWaterReading || 0),
  dueDate: form.dueDate,
  issuedBy: localStorage.getItem('fullName') || localStorage.getItem('username') || 'Nhân viên',
  occupants: occupantRows.value.map((item) => ({
    contractId: item.contractId,
    contractCode: item.contractCode,
    studentId: item.studentId,
    studentCode: item.studentCode,
    studentName: item.studentName,
    studentEmail: item.studentEmail,
    roomFee: item.roomFee,
    startDate: item.startDate,
    endDate: item.endDate,
  })),
})

const previewAllocation = async () => {
  previewing.value = true
  issueError.value = ''
  issueSuccess.value = ''
  try {
    const response = await api.post('/billing/monthly-invoices/room/preview', buildRoomInvoicePayload())
    roomPreview.value = normalizeList(response.data.allocations)
    previewSummary.value = response.data
  } catch (err) {
    roomPreview.value = []
    previewSummary.value = null
    issueError.value = err.response?.data?.message || err.response?.data?.detail || 'Không xem trước được phân bổ hóa đơn.'
  } finally {
    previewing.value = false
  }
}

const issueAndPrint = async () => {
  const printWindow = window.open('', '_blank')
  if (!printWindow) {
    issueError.value = 'Trình duyệt đang chặn cửa sổ in.'
    return
  }

  issuing.value = true
  issueError.value = ''
  issueSuccess.value = ''

  try {
    const response = await api.post('/billing/monthly-invoices/room/issue', buildRoomInvoicePayload())
    const createdInvoices = normalizeList(response.data.invoices)
    issueSuccess.value = response.data.message
    await loadAll()
    await printInvoices(createdInvoices, printWindow)
  } catch (err) {
    printWindow?.close()
    issueError.value = err.response?.data?.message || err.response?.data?.detail || 'Không phát hành được phiếu thanh toán.'
  } finally {
    issuing.value = false
  }
}

const openDetail = (invoice) => {
  selectedInvoice.value = invoice
  detailDialog.value = true
}

const resendEmail = async (invoice) => {
  error.value = ''
  try {
    const response = await api.post(`/billing/monthly-invoices/${invoice.id}/resend-email`)
    success.value = response.data.message
    await loadAll()
  } catch (err) {
    error.value = err.response?.data?.detail || 'Không gửi lại được email.'
  }
}

const markPaid = async (invoice) => {
  if (!window.confirm(`Xác nhận phiếu ${invoice.invoiceCode} đã được thanh toán?`)) return
  try {
    await api.post(`/billing/monthly-invoices/${invoice.id}/mark-paid`, {
      amount: invoice.totalAmount,
      referenceCode: `MANUAL-${Date.now()}`,
      confirmedBy: localStorage.getItem('fullName') || 'Nhân viên',
    })
    success.value = 'Đã xác nhận thanh toán và lưu lịch sử.'
    await loadAll()
  } catch (err) {
    error.value = err.response?.data?.message || 'Không xác nhận được thanh toán.'
  }
}

const exportInvoices = () => {
  exportRowsToExcel({
    filename: 'danh-sach-phieu-thanh-toan.xls',
    sheetName: 'Danh sách phiếu thanh toán',
    rows: filteredInvoices.value,
    columns: [
      { header: 'Mã phiếu', value: (invoice) => invoice.invoiceCode },
      { header: 'Sinh viên', value: (invoice) => invoice.studentName },
      { header: 'MSSV', value: (invoice) => invoice.studentCode },
      { header: 'Kỳ thanh toán', value: (invoice) => invoice.billingPeriod },
      { header: 'Phòng', value: (invoice) => invoice.roomName },
      { header: 'Tiền phòng', value: (invoice) => formatMoney(invoice.roomFee) },
      { header: 'Điện', value: (invoice) => formatMoney(invoice.electricityAmount) },
      { header: 'Nước', value: (invoice) => formatMoney(invoice.waterAmount) },
      { header: 'Tổng tiền', value: (invoice) => formatMoney(invoice.totalAmount) },
      { header: 'Hạn thanh toán', value: (invoice) => formatDate(invoice.dueDate) },
      { header: 'Trạng thái', value: (invoice) => statusLabel(invoice.status) },
      { header: 'Nội dung chuyển khoản', value: (invoice) => invoice.paymentCode },
    ],
  })
}

const printInvoice = async (invoice, targetWindow = null) => {
  await printInvoices([invoice], targetWindow)
}

const printInvoices = async (invoiceList, targetWindow = null) => {
  const popup = targetWindow || window.open('', '_blank')
  if (!popup) {
    error.value = 'Trình duyệt đang chặn cửa sổ in.'
    return
  }

  if (!invoiceList.length) {
    popup.close()
    error.value = 'Không có phiếu thanh toán nào để in.'
    return
  }

  const body = invoiceList.map((invoice) => {
    const qr = invoice.qrCodeUrl
      ? `<img src="${invoice.qrCodeUrl}" alt="VietQR" style="width:220px;max-width:100%"><p><b>${invoice.paymentCode}</b></p>`
      : '<p>Chưa cấu hình VietQR.</p>'

    return `<section class="invoice-print"><h1>PHIẾU THANH TOÁN HÀNG THÁNG</h1><p><b>Mã phiếu:</b> ${invoice.invoiceCode}</p><p><b>Sinh viên:</b> ${invoice.studentName} (${invoice.studentCode})</p><p><b>Phòng:</b> ${invoice.roomName} &nbsp; <b>Kỳ:</b> ${invoice.billingPeriod}</p><p><b>Cách chia:</b> ${invoice.allocationNote || 'Phiếu lập trực tiếp cho sinh viên.'}</p><table><tr><th>Khoản thu</th><th class="right">Thành tiền</th></tr><tr><td>Tiền phòng</td><td class="right">${formatMoney(invoice.roomFee)}</td></tr><tr><td>Điện phân bổ: ${invoice.electricityUsage} số từ ${invoice.roomElectricityUsage || invoice.electricityUsage} số phòng</td><td class="right">${formatMoney(invoice.electricityAmount)}</td></tr><tr><td>Nước phân bổ: ${invoice.waterUsage} số từ ${invoice.roomWaterUsage || invoice.waterUsage} số phòng</td><td class="right">${formatMoney(invoice.waterAmount)}</td></tr><tr class="total"><td>Tổng cộng</td><td class="right">${formatMoney(invoice.totalAmount)}</td></tr></table><p><b>Hạn thanh toán:</b> ${formatDate(invoice.dueDate)}</p><div class="qr">${qr}</div></section>`
  }).join('')

  popup.document.write(`<!doctype html><html lang="vi"><head><meta charset="utf-8"><title>Phiếu thanh toán</title><style>body{font-family:Arial,sans-serif;color:#17201b;max-width:820px;margin:30px auto;padding:0 20px}h1{color:#0f7f51}table{width:100%;border-collapse:collapse;margin:20px 0}th,td{border:1px solid #cfd8d2;padding:11px}th{text-align:left;background:#eaf7ef}.right{text-align:right}.total{font-size:20px;font-weight:700;color:#0f7f51}.qr{text-align:center;margin-top:24px}.invoice-print{page-break-after:always}.invoice-print:last-child{page-break-after:auto}@media print{button{display:none}}</style></head><body>${body}</body></html>`)
  popup.document.close()

  const images = Array.from(popup.document.images)
  await Promise.all(images.map((image) => new Promise((resolve) => {
    if (image.complete) {
      resolve()
      return
    }

    const finish = () => resolve()
    image.addEventListener('load', finish, { once: true })
    image.addEventListener('error', finish, { once: true })
    setTimeout(finish, 5000)
  })))

  popup.focus()
  popup.print()
}

watch(
  () => [
    form.roomId,
    form.billingPeriod,
    form.previousElectricityReading,
    form.currentElectricityReading,
    form.previousWaterReading,
    form.currentWaterReading,
    form.dueDate,
    occupantRows.value.map((item) => `${item.contractId}:${item.occupancyDays}:${item.studentEmail}`).join('|'),
  ],
  resetPreview,
)

onMounted(loadAll)
</script>

<style scoped>
.billing-page { display: grid; width: 100%; min-width: 0; gap: 20px; }
.billing-page > * { width: 100%; min-width: 0; max-width: 100%; }
.page-heading, .section-title, .section-title > div, .issue-footer, .history-list article { display: flex; align-items: center; justify-content: space-between; gap: 16px; }
.page-heading > div, .section-title > div { min-width: 0; }
.section-title { flex-wrap: wrap; }
.page-heading h2, .section-title h3 { margin: 0; color: #13251c; }
.page-heading h2 { font-size: 30px; }
.page-heading p, .section-title p { margin: 5px 0 0; color: #66736b; }
.eyebrow { color: #13875a; font-size: 12px; font-weight: 900; }
.metric-grid { display: grid; grid-template-columns: repeat(4, minmax(0, 1fr)); gap: 14px; }
.metric-grid > *, .form-grid > *, .meter-grid > * { min-width: 0; }
.metric-grid article, .issue-panel, .list-panel, .history-panel { border: 1px solid #dce5df; border-radius: 8px; background: #fff; }
.metric-grid article { display: grid; gap: 5px; padding: 18px; }
.metric-grid span, .metric-grid small { color: #6a776f; }
.metric-grid strong { font-size: 26px; color: #12251b; }
.issue-panel, .list-panel, .history-panel { padding: 22px; }
.section-title { margin-bottom: 20px; }
.section-title > div > .mdi { display: grid; place-items: center; width: 42px; height: 42px; border-radius: 7px; background: #e8f7ef; color: #0f8b5a; font-size: 23px; }
.rate-note { color: #0f7f51; font-weight: 800; text-align: right; }
.form-grid { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: 14px; }
.meter-grid { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 16px; margin-top: 2px; }
.meter-box { padding: 18px; border: 1px solid #dce5df; border-left: 4px solid; border-radius: 7px; background: #fbfdfb; }
.meter-box.electric { border-left-color: #e0a400; }
.meter-box.water { border-left-color: #1976d2; }
.meter-title { display: flex; gap: 9px; align-items: center; margin-bottom: 14px; font-size: 18px; }
.meter-inputs { display: grid; grid-template-columns: 1fr auto 1fr; gap: 10px; align-items: center; }
.meter-box p { margin: 0; color: #58665e; }
.occupant-panel, .preview-panel { margin-top: 16px; padding: 16px; border: 1px solid #dce5df; border-radius: 7px; background: #fbfdfb; }
.occupant-head, .preview-head { display: flex; align-items: center; justify-content: space-between; gap: 14px; margin-bottom: 12px; }
.occupant-head > div, .preview-head > div { display: grid; gap: 4px; min-width: 0; }
.occupant-head strong, .preview-head strong { color: #15271d; font-size: 16px; }
.occupant-head small, .preview-head small { color: #66736b; overflow-wrap: anywhere; }
.preview-head > span { display: inline-flex; padding: 6px 10px; border-radius: 999px; background: #e4f8ec; color: #0a7e4c; font-size: 12px; font-weight: 800; white-space: nowrap; }
.allocation-table { overflow: hidden; border: 1px solid #e1e8e3; border-radius: 6px; background: #fff; }
.allocation-table :deep(.v-table__wrapper) { overflow-x: auto; }
.allocation-table table { min-width: 760px; }
.allocation-table th { background: #f5f7f6; color: #536157; font-size: 12px; font-weight: 900; white-space: nowrap; }
.allocation-table td { vertical-align: top; }
.allocation-table td strong, .allocation-table td small { display: block; }
.allocation-table td small { margin-top: 3px; color: #748078; }
.allocation-table .missing { color: #c2410c; font-weight: 800; }
.issue-footer { margin-top: 18px; padding-top: 18px; border-top: 1px solid #e1e8e3; }
.issue-footer > div { display: grid; gap: 2px; }
.issue-footer span { color: #647168; }
.issue-footer strong { color: #0f7f51; font-size: 28px; }
.issue-summary { grid-template-columns: repeat(2, minmax(0, max-content)); gap: 28px !important; }
.issue-summary > div { display: grid; gap: 2px; }
.table-heading { align-items: flex-end; }
.table-tools { display: grid; grid-template-columns: minmax(220px, 1fr) 170px 230px auto; gap: 10px; width: min(940px, 100%); align-items: center; }
.history-tools { display: grid; grid-template-columns: minmax(230px, 1fr) 280px; gap: 10px; width: min(630px, 100%); }
.search-field, .history-search, .status-filter, .sort-filter { min-width: 0; }
.ant-table { overflow: hidden; border: 1px solid #dfe6e1; border-radius: 6px; box-shadow: 0 1px 2px rgba(26, 43, 34, 0.04); }
.ant-table :deep(.v-table__wrapper) { overflow-x: auto; }
.ant-table :deep(table) { min-width: 920px; }
.ant-table :deep(thead th) { height: 46px; background: #f5f7f6; color: #4e5c54; font-size: 12px; font-weight: 800; letter-spacing: 0; white-space: nowrap; }
.ant-table :deep(tbody td) { height: 62px; border-bottom-color: #e8ede9; color: #26342c; }
.ant-table :deep(tbody tr:last-child td) { border-bottom: 0; }
.ant-table :deep(tbody tr:hover td) { background: #f6fbf8 !important; }
.ant-table :deep(.v-data-table-footer) { min-height: 56px; border-top: 1px solid #e8ede9; background: #fff; }
.ant-table :deep(.v-data-table-rows-no-data td) { padding: 34px 16px; color: #748078; text-align: center; }
.cell-stack { display: grid; gap: 3px; min-width: 0; }
.cell-stack strong, .cell-stack small { display: block; overflow: hidden; text-overflow: ellipsis; }
.cell-stack strong { color: #203029; font-size: 14px; }
.cell-stack small { color: #748078; font-size: 12px; }
.history-student strong { display: flex; align-items: center; gap: 7px; }
.history-student .mdi { color: #0f985d; font-size: 19px; }
.money { color: #0f7f51; }
.status-pill { display: inline-flex; padding: 5px 9px; border-radius: 999px; font-size: 12px; font-weight: 800; }
.status-pill.unpaid { background: #fff2d7; color: #9b6200; }
.status-pill.paid { background: #def7e8; color: #087947; }
.method-tag { display: inline-flex; padding: 5px 9px; border: 1px solid #cfe0d6; border-radius: 5px; background: #f3faf6; color: #25714e; font-size: 12px; font-weight: 700; white-space: nowrap; }
.empty-row { padding: 28px; text-align: center; color: #77837b; }
.invoice-detail { display: grid; grid-template-columns: minmax(0, 1fr) 300px; gap: 24px; }
.detail-lines p { display: flex; justify-content: space-between; gap: 20px; padding: 10px 0; margin: 0; border-bottom: 1px solid #e5ebe7; }
.detail-lines span { color: #67746c; }
.detail-lines .total strong { color: #0f7f51; font-size: 22px; }
.qr-panel { display: grid; place-items: center; text-align: center; }
.qr-panel img { width: 280px; max-width: 100%; }
.qr-missing .mdi { font-size: 64px; color: #9aa49e; }
@media (max-width: 1100px) { .metric-grid { grid-template-columns: repeat(2, 1fr); } .form-grid { grid-template-columns: repeat(2, 1fr); } }
@media (max-width: 720px) { .page-heading, .section-title, .issue-footer, .occupant-head, .preview-head { align-items: stretch; flex-direction: column; } .metric-grid, .form-grid, .meter-grid, .invoice-detail, .issue-summary { grid-template-columns: 1fr; } .page-heading h2 { font-size: 24px; } .table-tools, .history-tools { grid-template-columns: 1fr; width: 100%; } .ant-table :deep(.v-data-table-footer) { align-items: flex-start; flex-wrap: wrap; padding: 10px 8px; } }
</style>
