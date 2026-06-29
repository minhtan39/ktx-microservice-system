<template>
  <section class="payment-page">
    <div class="page-heading">
      <div>
        <span class="eyebrow">TÀI KHOẢN SINH VIÊN</span>
        <h2>Thanh toán nội trú</h2>
        <p>Xem chi tiết tiền phòng, điện, nước và quét QR để thanh toán.</p>
      </div>
      <v-btn prepend-icon="mdi-refresh" variant="outlined" :loading="loading" @click="loadPayments(true)">Làm mới</v-btn>
    </div>

    <v-alert v-if="error" type="error" variant="tonal">{{ error }}</v-alert>

    <div class="summary-band">
      <div><span>Cần thanh toán</span><strong>{{ formatMoney(unpaidTotal) }}</strong></div>
      <div><span>Phiếu chưa trả</span><strong>{{ unpaidInvoices.length }}</strong></div>
      <div><span>Đã hoàn thành</span><strong>{{ paidInvoices.length }}</strong></div>
    </div>

    <section class="wallet-panel">
      <div class="wallet-main">
        <div>
          <span class="eyebrow">VÍ KTX</span>
          <h3>Số dư ví sinh viên</h3>
          <strong>{{ formatMoney(wallet?.balance) }}</strong>
          <p>Ví chỉ dùng để nạp tiền và tự động thanh toán hóa đơn nội trú hàng tháng.</p>
        </div>
        <v-switch
          :model-value="Boolean(wallet?.autoPayEnabled)"
          color="success"
          density="comfortable"
          hide-details
          :loading="walletSaving"
          label="Tự động thanh toán khi đủ số dư"
          @update:model-value="toggleAutoPay"
        />
      </div>

      <div class="wallet-grid">
        <div class="topup-box">
          <div class="section-title compact">
            <div><span class="mdi mdi-wallet-plus-outline"></span><div><h3>Nạp tiền vào ví</h3><p>Chọn số tiền rồi quét QR, nội dung chuyển khoản giữ đúng mã ví.</p></div></div>
          </div>
          <div class="topup-actions">
            <v-text-field
              v-model.number="topUpAmount"
              type="number"
              min="10000"
              step="10000"
              label="Số tiền muốn nạp"
              variant="outlined"
              density="compact"
              hide-details
            />
            <v-btn color="success" prepend-icon="mdi-qrcode" :loading="walletQrLoading" @click="refreshTopUpQr">
              Tạo QR nạp ví
            </v-btn>
          </div>
          <div class="wallet-transfer">
            <img v-if="topUpQrCodeUrl" :src="topUpQrCodeUrl" alt="Mã QR nạp ví KTX" />
            <div v-else class="qr-missing small"><span class="mdi mdi-qrcode-remove"></span><p>Chưa có QR nạp ví.</p></div>
            <div>
              <span>Nội dung chuyển khoản ví</span>
              <strong>{{ topUpCode || '-' }}</strong>
              <v-btn prepend-icon="mdi-content-copy" variant="tonal" size="small" @click="copyWalletCode">
                Sao chép mã ví
              </v-btn>
              <small v-if="walletCopyMessage" class="copy-note">{{ walletCopyMessage }}</small>
            </div>
          </div>
        </div>

        <div class="wallet-history">
          <div class="section-title compact">
            <div><span class="mdi mdi-clipboard-text-clock-outline"></span><div><h3>Giao dịch ví gần đây</h3><p>Nạp ví và các lần hệ thống tự trừ tiền hóa đơn.</p></div></div>
          </div>
          <article v-for="transaction in walletTransactions" :key="transaction.id">
            <span :class="['mdi', transaction.amount >= 0 ? 'mdi-arrow-down-circle-outline' : 'mdi-lightning-bolt-circle']"></span>
            <div>
              <strong>{{ walletTransactionLabel(transaction.type) }}</strong>
              <small>{{ formatDateTime(transaction.createdAt) }} · {{ transaction.referenceCode }}</small>
            </div>
            <b :class="{ debit: transaction.amount < 0 }">{{ formatSignedMoney(transaction.amount) }}</b>
          </article>
          <p v-if="walletTransactions.length === 0" class="empty-history wallet-empty">Chưa có giao dịch ví.</p>
        </div>
      </div>
    </section>

    <StudentExpenseAnalytics
      :student-id="studentId"
      :refresh-key="analyticsRefreshKey"
      @period-selected="selectPeriodInvoice"
    />

    <div class="payment-layout">
      <section class="invoice-list">
        <div class="section-title">
          <div><span class="mdi mdi-receipt-text-outline"></span><div><h3>Phiếu thanh toán</h3><p>Chọn một phiếu để xem chi tiết.</p></div></div>
        </div>

        <button
          v-for="invoice in invoices"
          :key="invoice.id"
          type="button"
          :class="['invoice-row', { active: selectedInvoice?.id === invoice.id }]"
          @click="selectedInvoice = invoice"
        >
          <div>
            <strong>{{ invoice.billingPeriod }} · Phòng {{ invoice.roomName }}</strong>
            <span>{{ invoice.invoiceCode }}</span>
          </div>
          <div class="row-amount">
            <strong>{{ formatMoney(invoice.totalAmount) }}</strong>
            <span :class="['status-pill', invoice.status.toLowerCase()]">{{ statusLabel(invoice.status) }}</span>
          </div>
        </button>

        <div v-if="invoices.length === 0" class="empty-state">
          <span class="mdi mdi-receipt-text-remove-outline"></span>
          <p>Bạn chưa có phiếu thanh toán hàng tháng.</p>
        </div>
      </section>

      <section v-if="selectedInvoice" class="invoice-detail">
        <div class="detail-header">
          <div><span>PHIẾU THANH TOÁN</span><h3>{{ selectedInvoice.invoiceCode }}</h3></div>
          <span :class="['status-pill', selectedInvoice.status.toLowerCase()]">{{ statusLabel(selectedInvoice.status) }}</span>
        </div>

        <div class="line-items">
          <div><span>Tiền phòng</span><strong>{{ formatMoney(selectedInvoice.roomFee) }}</strong></div>
          <div><span>Điện phân bổ ({{ selectedInvoice.electricityUsage }} số × 4.000đ)</span><strong>{{ formatMoney(selectedInvoice.electricityAmount) }}</strong></div>
          <div><span>Nước phân bổ ({{ selectedInvoice.waterUsage }} số × 20.000đ)</span><strong>{{ formatMoney(selectedInvoice.waterAmount) }}</strong></div>
          <div v-if="selectedInvoice.roomElectricityUsage || selectedInvoice.roomWaterUsage" class="allocation-row">
            <span>Điện nước cả phòng</span>
            <strong>{{ selectedInvoice.roomElectricityUsage || selectedInvoice.electricityUsage }} số điện · {{ selectedInvoice.roomWaterUsage || selectedInvoice.waterUsage }} số nước</strong>
          </div>
          <div v-if="selectedInvoice.allocationNote" class="allocation-row">
            <span>Cách chia</span>
            <strong>{{ selectedInvoice.allocationNote }}</strong>
          </div>
          <div class="grand-total"><span>Tổng thanh toán</span><strong>{{ formatMoney(selectedInvoice.totalAmount) }}</strong></div>
        </div>

        <template v-if="selectedInvoice.status !== 'Paid'">
          <div class="qr-box">
            <img v-if="selectedInvoice.qrCodeUrl" :src="selectedInvoice.qrCodeUrl" alt="Mã QR chuyển khoản" />
            <div v-else class="qr-missing"><span class="mdi mdi-qrcode-remove"></span><p>Quản trị viên chưa cấu hình VietQR.</p></div>
          </div>
          <div class="transfer-note">
            <span>Nội dung chuyển khoản bắt buộc</span>
            <div><strong>{{ selectedInvoice.paymentCode }}</strong><v-btn icon="mdi-content-copy" variant="text" density="compact" @click="copyPaymentCode" /></div>
            <small v-if="paymentCopyMessage" class="copy-note">{{ paymentCopyMessage }}</small>
            <small>Hệ thống chỉ tự động xác nhận khi nội dung đúng và số tiền đủ.</small>
          </div>
          <p class="due-note"><span class="mdi mdi-calendar-clock-outline"></span> Hạn thanh toán: <strong>{{ formatDate(selectedInvoice.dueDate) }}</strong></p>
        </template>

        <div v-else class="paid-box">
          <span class="mdi mdi-check-decagram"></span>
          <div><strong>Thanh toán đã hoàn thành</strong><p>Ghi nhận lúc {{ formatDateTime(selectedInvoice.paidAt) }}</p></div>
        </div>
      </section>
    </div>

    <section class="history-panel">
      <div class="section-title"><div><span class="mdi mdi-history"></span><div><h3>Lịch sử của bạn</h3><p>Các khoản đã được ngân hàng hoặc nhân viên xác nhận.</p></div></div></div>
      <div class="history-grid">
        <article v-for="item in history" :key="item.id">
          <span class="mdi mdi-bank-check"></span>
          <div><strong>{{ item.billingPeriod }} · {{ item.invoiceCode }}</strong><small>{{ formatDateTime(item.paidAt) }} · {{ item.referenceCode }}</small></div>
          <b>{{ formatMoney(item.amount) }}</b>
        </article>
        <p v-if="history.length === 0" class="empty-history">Chưa có lịch sử thanh toán.</p>
      </div>
    </section>
  </section>
</template>

<script setup>
import { computed, onMounted, ref } from 'vue'
import api from '../../../services/api'
import StudentExpenseAnalytics from '../components/StudentExpenseAnalytics.vue'

const loading = ref(false)
const error = ref('')
const invoices = ref([])
const history = ref([])
const walletInfo = ref(null)
const walletSaving = ref(false)
const walletQrLoading = ref(false)
const topUpAmount = ref(500000)
const selectedInvoice = ref(null)
const analyticsRefreshKey = ref(0)
const paymentCopyMessage = ref('')
const walletCopyMessage = ref('')
const studentId = Number(localStorage.getItem('student_id') || 0)

const normalizeList = (payload) => {
  if (Array.isArray(payload)) return payload
  if (Array.isArray(payload?.data)) return payload.data
  if (Array.isArray(payload?.items)) return payload.items
  return []
}

const unpaidInvoices = computed(() => invoices.value.filter((item) => item.status !== 'Paid'))
const paidInvoices = computed(() => invoices.value.filter((item) => item.status === 'Paid'))
const unpaidTotal = computed(() => unpaidInvoices.value.reduce((sum, item) => sum + Number(item.totalAmount || 0), 0))
const wallet = computed(() => walletInfo.value?.wallet || null)
const walletTransactions = computed(() => walletInfo.value?.transactions || [])
const topUpCode = computed(() => walletInfo.value?.topUpCode || '')
const topUpQrCodeUrl = computed(() => walletInfo.value?.topUpQrCodeUrl || '')
const formatMoney = (value) => new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND', maximumFractionDigits: 0 }).format(Number(value || 0))
const formatSignedMoney = (value) => `${Number(value || 0) >= 0 ? '+' : '-'}${formatMoney(Math.abs(Number(value || 0)))}`
const formatDate = (value) => value ? new Intl.DateTimeFormat('vi-VN').format(new Date(value)) : '-'
const formatDateTime = (value) => value ? new Intl.DateTimeFormat('vi-VN', { dateStyle: 'short', timeStyle: 'short' }).format(new Date(value)) : '-'
const statusLabel = (status) => status === 'Paid' ? 'Đã thanh toán' : 'Chưa thanh toán'
const walletTransactionLabel = (type) => ({
  TopUp: 'Nạp ví',
  AutoPayment: 'Tự động thanh toán hóa đơn',
  ManualAdjustment: 'Điều chỉnh số dư',
}[type] || type || 'Giao dịch ví')

const loadWallet = async () => {
  const amount = Math.max(Number(topUpAmount.value || 0), 0)
  const response = await api.get(`/billing/wallets/${studentId}?amount=${amount}`)
  walletInfo.value = response.data
}

const loadPayments = async (refreshAnalytics = false) => {
  loading.value = true
  error.value = ''
  try {
    if (!studentId) throw new Error('Tài khoản chưa liên kết với hồ sơ sinh viên.')
    const [invoiceResponse, historyResponse, walletResponse] = await Promise.all([
      api.get(`/billing/monthly-invoices?studentId=${studentId}`),
      api.get(`/billing/payment-history?studentId=${studentId}`),
      api.get(`/billing/wallets/${studentId}?amount=${Math.max(Number(topUpAmount.value || 0), 0)}`),
    ])
    invoices.value = normalizeList(invoiceResponse.data)
    history.value = normalizeList(historyResponse.data)
    walletInfo.value = walletResponse.data

    if (!selectedInvoice.value || !invoices.value.some((item) => item.id === selectedInvoice.value.id)) {
      selectedInvoice.value = unpaidInvoices.value[0] || invoices.value[0] || null
    } else {
      selectedInvoice.value = invoices.value.find((item) => item.id === selectedInvoice.value.id)
    }
    if (refreshAnalytics) analyticsRefreshKey.value += 1
  } catch (err) {
    error.value = err.response?.data?.detail || err.response?.data?.message || err.message || 'Không tải được thông tin thanh toán.'
  } finally {
    loading.value = false
  }
}

const selectPeriodInvoice = (period) => {
  const periodInvoices = invoices.value.filter((item) => item.billingPeriod === period)
  selectedInvoice.value = periodInvoices.find((item) => item.status !== 'Paid') || periodInvoices[0] || null

  if (selectedInvoice.value) {
    requestAnimationFrame(() => document.querySelector('.payment-layout')?.scrollIntoView({ behavior: 'smooth', block: 'start' }))
  }
}

const copyPaymentCode = async () => {
  await navigator.clipboard.writeText(selectedInvoice.value.paymentCode)
  paymentCopyMessage.value = 'Đã sao chép nội dung chuyển khoản.'
  walletCopyMessage.value = ''
}

const copyWalletCode = async () => {
  if (!topUpCode.value) return
  await navigator.clipboard.writeText(topUpCode.value)
  walletCopyMessage.value = 'Đã sao chép mã nạp ví.'
  paymentCopyMessage.value = ''
}

const refreshTopUpQr = async () => {
  walletQrLoading.value = true
  error.value = ''
  try {
    if (!studentId) throw new Error('Tài khoản chưa liên kết với hồ sơ sinh viên.')
    if (Number(topUpAmount.value || 0) <= 0) throw new Error('Vui lòng nhập số tiền nạp ví lớn hơn 0.')
    await loadWallet()
  } catch (err) {
    error.value = err.response?.data?.detail || err.response?.data?.message || err.message || 'Không tạo được QR nạp ví.'
  } finally {
    walletQrLoading.value = false
  }
}

const toggleAutoPay = async (enabled) => {
  walletSaving.value = true
  error.value = ''
  try {
    await api.put(`/billing/wallets/${studentId}/auto-pay`, { enabled })
    await loadPayments(true)
  } catch (err) {
    error.value = err.response?.data?.detail || err.response?.data?.message || 'Không cập nhật được tự động thanh toán.'
  } finally {
    walletSaving.value = false
  }
}

onMounted(() => loadPayments(false))
</script>

<style scoped>
.payment-page { display: grid; width: 100%; min-width: 0; gap: 20px; }
.payment-page > * { width: 100%; min-width: 0; max-width: 100%; }
.page-heading, .section-title > div, .detail-header, .history-grid article { display: flex; align-items: center; justify-content: space-between; gap: 16px; }
.page-heading > div, .section-title > div { min-width: 0; }
.page-heading h2, .section-title h3, .detail-header h3 { margin: 0; color: #14261c; }
.page-heading h2 { font-size: 30px; }
.page-heading p, .section-title p { margin: 5px 0 0; color: #68756d; }
.eyebrow, .detail-header > div > span { color: #108657; font-size: 12px; font-weight: 900; }
.summary-band { display: grid; grid-template-columns: 2fr 1fr 1fr; border: 1px solid #dce5df; border-radius: 8px; background: #fff; }
.summary-band div { display: grid; gap: 5px; padding: 20px; border-right: 1px solid #e3e9e5; }
.summary-band div:last-child { border-right: 0; }
.summary-band span { color: #68766e; }
.summary-band strong { color: #0f7f51; font-size: 28px; }
.wallet-panel { display: grid; gap: 18px; padding: 22px; border: 1px solid #dce5df; border-radius: 8px; background: #fff; }
.wallet-main { display: flex; align-items: center; justify-content: space-between; gap: 18px; padding: 18px; border-radius: 8px; background: linear-gradient(135deg, #f0fbf5, #eef6ff); }
.wallet-main > div { min-width: 0; }
.wallet-main h3 { margin: 5px 0 4px; color: #14261c; font-size: 22px; }
.wallet-main strong { display: block; color: #0f7f51; font-size: 34px; line-height: 1.15; }
.wallet-main p { max-width: 620px; margin: 6px 0 0; color: #607068; }
.wallet-grid { display: grid; grid-template-columns: minmax(0, 1.1fr) minmax(320px, .9fr); gap: 16px; }
.topup-box, .wallet-history { padding: 18px; border: 1px solid #e0e8e3; border-radius: 8px; background: #fbfdfb; }
.section-title.compact { margin-bottom: 14px; }
.topup-actions { display: grid; grid-template-columns: minmax(180px, 1fr) auto; gap: 12px; align-items: center; }
.wallet-transfer { display: grid; grid-template-columns: 150px minmax(0, 1fr); gap: 16px; align-items: center; margin-top: 14px; padding: 14px; border: 1px dashed #acd3bd; border-radius: 8px; background: #f4fbf7; }
.wallet-transfer img { width: 150px; max-width: 100%; }
.wallet-transfer > div { display: grid; gap: 8px; min-width: 0; }
.wallet-transfer span { color: #66746b; }
.wallet-transfer strong { color: #0f7f51; font-size: 20px; overflow-wrap: anywhere; }
.copy-note { display: block; color: #087947 !important; font-weight: 700; }
.qr-missing.small { display: grid; place-items: center; min-height: 130px; color: #8a9690; text-align: center; }
.qr-missing.small .mdi { font-size: 42px; }
.wallet-history article { display: grid; grid-template-columns: auto minmax(0, 1fr) auto; gap: 12px; align-items: center; padding: 12px 0; border-bottom: 1px solid #e3e9e5; }
.wallet-history article:last-of-type { border-bottom: 0; }
.wallet-history article > .mdi { color: #0f8b5a; font-size: 25px; }
.wallet-history article > div { display: grid; gap: 3px; min-width: 0; }
.wallet-history strong, .wallet-history small { overflow: hidden; text-overflow: ellipsis; }
.wallet-history small { color: #748078; }
.wallet-history b { color: #0f7f51; white-space: nowrap; }
.wallet-history b.debit { color: #b45309; }
.wallet-empty { padding: 20px 0 4px !important; }
.payment-layout { display: grid; grid-template-columns: minmax(320px, .85fr) minmax(430px, 1.15fr); gap: 18px; align-items: start; }
.invoice-list, .invoice-detail, .history-panel { padding: 22px; border: 1px solid #dce5df; border-radius: 8px; background: #fff; }
.section-title { margin-bottom: 18px; }
.section-title > div { justify-content: flex-start; }
.section-title .mdi { display: grid; place-items: center; width: 42px; height: 42px; border-radius: 7px; background: #e8f7ef; color: #0f8b5a; font-size: 23px; }
.invoice-row { display: flex; width: 100%; align-items: center; justify-content: space-between; gap: 14px; padding: 16px 12px; border: 0; border-top: 1px solid #e3e9e5; background: transparent; color: inherit; text-align: left; cursor: pointer; }
.invoice-row:hover, .invoice-row.active { background: #f0f8f4; }
.invoice-row strong, .invoice-row span { display: block; }
.invoice-row span { margin-top: 4px; color: #758179; }
.row-amount { text-align: right; }
.row-amount strong { color: #0f7f51; }
.status-pill { display: inline-flex !important; width: fit-content; margin-left: auto; padding: 5px 9px; border-radius: 999px; font-size: 12px; font-weight: 800; }
.status-pill.unpaid { background: #fff2d7; color: #9b6200; }
.status-pill.paid { background: #def7e8; color: #087947; }
.detail-header { padding-bottom: 18px; border-bottom: 1px solid #e3e9e5; }
.line-items { margin-top: 12px; }
.line-items > div { display: flex; justify-content: space-between; gap: 16px; padding: 11px 0; }
.line-items span { color: #65736a; }
.allocation-row { display: grid !important; gap: 5px !important; padding: 12px !important; border: 1px dashed #b8d8c5; border-radius: 7px; background: #f5fbf7; }
.allocation-row strong { color: #1d3a2a; font-size: 14px; line-height: 1.5; text-align: right; }
.grand-total { margin-top: 4px; padding-top: 16px !important; border-top: 2px solid #dce5df; font-size: 20px; }
.grand-total strong { color: #0f7f51; }
.qr-box { display: grid; place-items: center; margin-top: 12px; text-align: center; }
.qr-box img { width: 290px; max-width: 100%; }
.qr-missing .mdi { font-size: 70px; color: #9aa49e; }
.transfer-note { padding: 15px; border: 1px dashed #9bcdb4; border-radius: 7px; background: #f2fbf6; text-align: center; }
.transfer-note > span, .transfer-note small { color: #66746b; }
.transfer-note > div { display: flex; align-items: center; justify-content: center; gap: 4px; margin: 5px 0; }
.transfer-note strong { color: #0f7f51; font-size: 20px; }
.due-note { text-align: center; color: #59675e; }
.paid-box { display: flex; align-items: center; gap: 14px; margin-top: 22px; padding: 18px; border-radius: 7px; background: #e6f8ee; color: #087947; }
.paid-box > .mdi { font-size: 40px; }
.paid-box p { margin: 4px 0 0; }
.history-grid article { padding: 13px 2px; border-bottom: 1px solid #e3e9e5; }
.history-grid article > .mdi { color: #0f8b5a; font-size: 24px; }
.history-grid article > div { flex: 1; display: grid; }
.history-grid small { color: #748077; }
.history-grid b { color: #0f7f51; }
.empty-state, .empty-history { padding: 36px 10px; color: #738077; text-align: center; }
.empty-state .mdi { font-size: 52px; }
@media (max-width: 900px) { .payment-layout, .wallet-grid { grid-template-columns: 1fr; } }
@media (max-width: 620px) { .page-heading, .wallet-main { align-items: stretch; flex-direction: column; } .summary-band, .topup-actions, .wallet-transfer { grid-template-columns: 1fr; } .summary-band div { border-right: 0; border-bottom: 1px solid #e3e9e5; } .page-heading h2 { font-size: 24px; } }
</style>
