<template>
  <section class="operations-page">
    <div class="page-head">
      <div><span class="page-kicker">Vận hành kỹ thuật</span><h2>Sửa chữa và bảo trì</h2><p>Phân công rõ người phụ trách, theo dõi hạn xử lý và lưu toàn bộ lịch sử công việc.</p></div>
      <v-btn color="primary" prepend-icon="mdi-refresh" :loading="loading" @click="loadAll">Làm mới</v-btn>
    </div>

    <v-alert v-if="error" type="error" variant="tonal">{{ error }}</v-alert>
    <v-alert v-if="success" type="success" variant="tonal">{{ success }}</v-alert>

    <div class="metric-grid">
      <article v-for="metric in metrics" :key="metric.label" class="metric-card"><span :class="['mdi', metric.icon]"></span><div><strong>{{ metric.value }}</strong><small>{{ metric.label }}</small></div></article>
    </div>

    <v-tabs v-model="activeView" color="primary" class="view-tabs">
      <v-tab v-if="canManageIncidents" value="incidents">Yêu cầu sửa chữa</v-tab>
      <v-tab v-if="canManageMaintenance" value="maintenance">Lịch bảo trì</v-tab>
    </v-tabs>

    <v-window v-model="activeView">
      <v-window-item v-if="canManageIncidents" value="incidents">
        <section class="panel">
          <div class="toolbar-grid">
            <v-text-field v-model="search" label="Tìm sinh viên, phòng hoặc nội dung" prepend-inner-icon="mdi-magnify" variant="outlined" density="compact" hide-details clearable />
            <v-select v-model="statusFilter" :items="statusOptions" item-title="title" item-value="value" label="Trạng thái" variant="outlined" density="compact" hide-details />
            <v-select v-model="priorityFilter" :items="priorityOptionsWithAll" item-title="title" item-value="value" label="Mức ưu tiên" variant="outlined" density="compact" hide-details />
            <v-select v-model="assignmentFilter" :items="assignmentOptions" item-title="title" item-value="value" label="Phân công" variant="outlined" density="compact" hide-details />
          </div>

          <v-data-table :headers="incidentHeaders" :items="filteredIncidents" :loading="loading" :items-per-page="10" item-value="id" class="operations-table" no-data-text="Không có yêu cầu phù hợp.">
            <template #item.request="{ item }"><div class="cell-stack"><strong>#{{ item.id }} · {{ categoryLabel(item.category) }}</strong><small>{{ item.description }}</small></div></template>
            <template #item.student="{ item }"><div class="cell-stack"><strong>{{ item.studentName }}</strong><small>{{ item.studentCode }} · {{ item.building }}-{{ item.roomName }}</small></div></template>
            <template #item.priority="{ item }"><span :class="['priority-tag', item.priority]">{{ priorityLabel(item.priority) }}</span></template>
            <template #item.assignee="{ item }"><div class="cell-stack"><strong>{{ item.assignedName || 'Chưa phân công' }}</strong><small>{{ item.dueAt ? `Hạn ${formatDate(item.dueAt)}` : 'Chưa đặt hạn' }}</small></div></template>
            <template #item.status="{ item }"><span :class="['status-tag', item.status]">{{ statusLabel(item.status) }}</span></template>
            <template #item.createdAt="{ item }"><div class="cell-stack"><strong>{{ formatDateTime(item.createdAt) }}</strong><small>{{ item.preferredVisitAt ? `Có thể tiếp nhận: ${formatDateTime(item.preferredVisitAt)}` : 'Không yêu cầu thời gian' }}</small></div></template>
            <template #item.actions="{ item }"><v-btn icon="mdi-arrow-right" variant="text" color="primary" title="Mở yêu cầu" @click="openIncident(item)" /></template>
          </v-data-table>
        </section>
      </v-window-item>

      <v-window-item v-if="canManageMaintenance" value="maintenance">
        <section class="panel">
          <div class="maintenance-head"><div><h3>Kế hoạch bảo trì định kỳ</h3><p>Admin lập lịch, nhân viên cập nhật checklist và kết quả công việc được giao.</p></div><v-btn v-if="isAdmin" color="primary" prepend-icon="mdi-calendar-plus" @click="openCreateMaintenance">Tạo lịch bảo trì</v-btn></div>
          <v-data-table :headers="maintenanceHeaders" :items="sortedMaintenance" :loading="loading" :items-per-page="10" item-value="id" class="operations-table" no-data-text="Chưa có lịch bảo trì.">
            <template #item.work="{ item }"><div class="cell-stack"><strong>{{ item.title }}</strong><small>{{ item.assetCode }} · {{ item.assetName }}</small></div></template>
            <template #item.location="{ item }"><div class="cell-stack"><strong>{{ item.location }}</strong><small>{{ frequencyLabel(item.frequency) }}</small></div></template>
            <template #item.nextDueDate="{ item }"><span :class="{ overdue: isOverdue(item) }">{{ formatDate(item.nextDueDate) }}<small>{{ isOverdue(item) ? 'Quá hạn' : daysUntil(item.nextDueDate) }}</small></span></template>
            <template #item.assignedName="{ item }">{{ item.assignedName || 'Chưa phân công' }}</template>
            <template #item.status="{ item }"><span :class="['status-tag', item.status]">{{ maintenanceStatusLabel(item.status) }}</span></template>
            <template #item.actions="{ item }"><v-btn v-if="canEditMaintenance(item)" icon="mdi-pencil-outline" variant="text" color="primary" title="Cập nhật bảo trì" @click="openMaintenance(item)" /></template>
          </v-data-table>
        </section>
      </v-window-item>
    </v-window>

    <v-dialog v-model="incidentDialog" max-width="960" scrollable>
      <v-card v-if="selectedIncident">
        <v-card-title>Yêu cầu #{{ selectedIncident.id }} · {{ selectedIncident.building }}-{{ selectedIncident.roomName }}</v-card-title>
        <v-card-text class="incident-detail">
          <section class="detail-summary">
            <div><span>Sinh viên</span><strong>{{ selectedIncident.studentName }} · {{ selectedIncident.studentCode }}</strong></div>
            <div><span>Sự cố</span><strong>{{ categoryLabel(selectedIncident.category) }} · {{ priorityLabel(selectedIncident.priority) }}</strong></div>
            <div><span>Trạng thái</span><strong>{{ statusLabel(selectedIncident.status) }}</strong></div>
            <div><span>Chi phí thực tế</span><strong>{{ money(selectedIncident.actualCost) }}</strong></div>
          </section>
          <div class="description-box"><strong>Mô tả của sinh viên</strong><p>{{ selectedIncident.description }}</p></div>

          <div class="detail-grid">
            <section class="action-panel">
              <h3>Phân công và hẹn xử lý</h3>
              <v-alert v-if="isAdmin && incidentStaffOptions.length === 0" type="warning" variant="tonal" density="compact">Chưa có nhân viên active có quyền xử lý sửa chữa.</v-alert>
              <v-select v-model="incidentForm.assignedTo" :items="incidentStaffOptions" item-title="title" item-value="value" label="Nhân viên phụ trách" variant="outlined" density="compact" :disabled="!isAdmin" no-data-text="Không có nhân viên phù hợp" />
              <v-select v-model="incidentForm.priority" :items="priorityOptions" item-title="title" item-value="value" label="Mức ưu tiên" variant="outlined" density="compact" />
              <div class="two-cols"><v-text-field v-model="incidentForm.scheduledAt" type="datetime-local" label="Lịch hẹn" variant="outlined" density="compact" /><v-text-field v-model="incidentForm.dueAt" type="datetime-local" label="Hạn xử lý" variant="outlined" density="compact" /></div>
              <v-textarea v-model="incidentForm.assignmentNote" label="Ghi chú phân công" rows="2" variant="outlined" density="compact" />
              <v-btn v-if="canClaimSelected" block color="primary" variant="tonal" :loading="saving" @click="saveAssignment">{{ isAdmin ? 'Lưu phân công' : selectedIncident.assignedTo === username ? 'Cập nhật lịch hẹn' : 'Nhận công việc này' }}</v-btn>
            </section>

            <section v-if="canUpdateSelected" class="action-panel">
              <h3>Cập nhật tiến độ</h3>
              <v-select v-model="incidentForm.status" :items="progressStatusOptions" item-title="title" item-value="value" label="Trạng thái mới" variant="outlined" density="compact" />
              <v-textarea v-model="incidentForm.staffNote" label="Ghi chú xử lý" rows="2" variant="outlined" density="compact" />
              <v-text-field v-model="incidentForm.rootCause" label="Nguyên nhân" variant="outlined" density="compact" />
              <v-textarea v-model="incidentForm.resolution" label="Kết quả / nội dung bàn giao" rows="2" variant="outlined" density="compact" />
              <div class="two-cols"><v-text-field v-model.number="incidentForm.materialCost" type="number" min="0" label="Chi phí vật tư" variant="outlined" density="compact" /><v-text-field v-model.number="incidentForm.laborCost" type="number" min="0" label="Chi phí nhân công" variant="outlined" density="compact" /></div>
              <v-btn block color="success" :loading="saving" @click="saveProgress">Cập nhật tiến độ</v-btn>
            </section>
            <v-alert v-else type="info" variant="tonal">Yêu cầu này đang do nhân viên khác phụ trách. Bạn có thể xem lịch sử nhưng không thể cập nhật.</v-alert>
          </div>

          <section class="timeline-panel"><h3>Lịch sử xử lý</h3><div v-if="!selectedIncident.timeline?.length" class="muted">Chưa có lịch sử.</div><div v-for="entry in [...(selectedIncident.timeline || [])].reverse()" :key="`${entry.at}-${entry.action}`" class="timeline-row"><span></span><div><strong>{{ statusLabel(entry.status) }}</strong><small>{{ entry.actor }} · {{ formatDateTime(entry.at) }}</small><p v-if="entry.note">{{ entry.note }}</p></div></div></section>
        </v-card-text>
        <v-card-actions><v-spacer /><v-btn variant="text" @click="incidentDialog = false">Đóng</v-btn></v-card-actions>
      </v-card>
    </v-dialog>

    <v-dialog v-model="maintenanceDialog" max-width="760">
      <v-card><v-card-title>{{ editingMaintenance ? 'Cập nhật công việc bảo trì' : 'Tạo lịch bảo trì' }}</v-card-title><v-card-text>
        <div class="form-grid">
          <v-text-field v-model="maintenanceForm.title" label="Tên công việc" variant="outlined" density="compact" :disabled="!!editingMaintenance" />
          <v-text-field v-model="maintenanceForm.assetCode" label="Mã thiết bị" variant="outlined" density="compact" :disabled="!!editingMaintenance" />
          <v-text-field v-model="maintenanceForm.assetName" label="Tên thiết bị" variant="outlined" density="compact" :disabled="!!editingMaintenance" />
          <v-text-field v-model="maintenanceForm.location" label="Vị trí" variant="outlined" density="compact" :disabled="!!editingMaintenance" />
          <v-select v-model="maintenanceForm.frequency" :items="frequencyOptions" item-title="title" item-value="value" label="Chu kỳ" variant="outlined" density="compact" :disabled="!!editingMaintenance" />
          <v-text-field v-model="maintenanceForm.nextDueDate" type="date" label="Ngày đến hạn" variant="outlined" density="compact" />
          <v-alert v-if="isAdmin && maintenanceStaffOptions.length === 0" type="warning" variant="tonal" density="compact" class="full-width">Chưa có nhân viên active có quyền quản lý bảo trì.</v-alert>
          <v-select v-model="maintenanceForm.assignedTo" :items="maintenanceStaffOptions" item-title="title" item-value="value" label="Nhân viên phụ trách" variant="outlined" density="compact" :disabled="!isAdmin" no-data-text="Không có nhân viên phù hợp" />
          <v-select v-if="editingMaintenance" v-model="maintenanceForm.status" :items="maintenanceStatusOptions" item-title="title" item-value="value" label="Trạng thái" variant="outlined" density="compact" />
          <v-text-field v-if="editingMaintenance" v-model.number="maintenanceForm.cost" type="number" min="0" label="Chi phí" variant="outlined" density="compact" />
          <v-textarea v-model="maintenanceForm.notes" :label="editingMaintenance ? 'Ghi chú thực hiện' : 'Checklist, mỗi dòng một mục'" rows="4" variant="outlined" class="full-width" />
          <div v-if="editingMaintenance && maintenanceForm.checklist.length" class="checklist-panel full-width">
            <strong>Checklist bảo trì</strong>
            <v-checkbox
              v-for="item in maintenanceForm.checklist"
              :key="item"
              v-model="maintenanceForm.completedItems"
              :label="item"
              :value="item"
              density="compact"
              hide-details
            />
          </div>
        </div>
      </v-card-text><v-card-actions><v-spacer /><v-btn variant="text" @click="maintenanceDialog = false">Hủy</v-btn><v-btn color="primary" :loading="saving" @click="saveMaintenance">Lưu công việc</v-btn></v-card-actions></v-card>
    </v-dialog>
  </section>
</template>

<script setup>
import { computed, onMounted, reactive, ref } from 'vue'
import api from '@/services/api'
import { getPermissions, normalizeRole } from '@/utils/auth'

const loading = ref(false), saving = ref(false), error = ref(''), success = ref('')
const activeView = ref('incidents'), incidents = ref([]), maintenance = ref([]), staff = ref([])
const search = ref(''), statusFilter = ref('all'), priorityFilter = ref('all'), assignmentFilter = ref('all')
const incidentDialog = ref(false), selectedIncident = ref(null), maintenanceDialog = ref(false), editingMaintenance = ref(null)
const role = normalizeRole(localStorage.getItem('user_role')), username = localStorage.getItem('username') || ''
const isAdmin = role === 'Admin', permissions = getPermissions()
const canManageIncidents = isAdmin || permissions.includes('manage_incidents')
const canManageMaintenance = isAdmin || permissions.includes('manage_maintenance')

const statusOptions = [{ title: 'Tất cả trạng thái', value: 'all' }, { title: 'Mới gửi', value: 'new' }, { title: 'Đã tiếp nhận', value: 'accepted' }, { title: 'Đã phân công', value: 'assigned' }, { title: 'Đang xử lý', value: 'processing' }, { title: 'Chờ vật tư', value: 'waiting-materials' }, { title: 'Đã hoàn thành', value: 'completed' }, { title: 'Cần xử lý lại', value: 'reopened' }, { title: 'Sinh viên đã xác nhận', value: 'confirmed' }, { title: 'Từ chối', value: 'rejected' }]
const progressStatusOptions = statusOptions.filter((item) => !['all', 'new', 'confirmed'].includes(item.value))
const priorityOptions = [{ title: 'Thấp', value: 'low' }, { title: 'Bình thường', value: 'normal' }, { title: 'Cao', value: 'high' }, { title: 'Khẩn cấp', value: 'urgent' }]
const priorityOptionsWithAll = [{ title: 'Tất cả mức độ', value: 'all' }, ...priorityOptions]
const assignmentOptions = [{ title: 'Tất cả phân công', value: 'all' }, { title: 'Chưa phân công', value: 'unassigned' }, { title: 'Công việc của tôi', value: 'mine' }]
const frequencyOptions = [{ title: 'Hàng tuần', value: 'Weekly' }, { title: 'Hàng tháng', value: 'Monthly' }, { title: 'Hàng quý', value: 'Quarterly' }, { title: 'Hàng năm', value: 'Yearly' }]
const maintenanceStatusOptions = [{ title: 'Đã lên lịch', value: 'scheduled' }, { title: 'Đang thực hiện', value: 'in-progress' }, { title: 'Chờ vật tư', value: 'waiting-materials' }, { title: 'Hoàn thành', value: 'completed' }, { title: 'Đã hủy', value: 'cancelled' }]
const incidentHeaders = [{ title: 'Yêu cầu', key: 'request', sortable: false }, { title: 'Sinh viên / Phòng', key: 'student', sortable: false }, { title: 'Ưu tiên', key: 'priority', sortable: false }, { title: 'Phụ trách / Hạn', key: 'assignee', sortable: false }, { title: 'Trạng thái', key: 'status', sortable: false }, { title: 'Thời gian', key: 'createdAt', sortable: false }, { title: '', key: 'actions', sortable: false }]
const maintenanceHeaders = [{ title: 'Công việc / Thiết bị', key: 'work', sortable: false }, { title: 'Vị trí / Chu kỳ', key: 'location', sortable: false }, { title: 'Đến hạn', key: 'nextDueDate', sortable: false }, { title: 'Phụ trách', key: 'assignedName', sortable: false }, { title: 'Trạng thái', key: 'status', sortable: false }, { title: '', key: 'actions', sortable: false }]
const incidentForm = reactive({ assignedTo: '', priority: 'normal', scheduledAt: '', dueAt: '', assignmentNote: '', status: 'processing', staffNote: '', rootCause: '', resolution: '', materialCost: 0, laborCost: 0 })
const blankMaintenance = () => ({ title: '', assetCode: '', assetName: '', location: '', category: 'Other', frequency: 'Monthly', nextDueDate: new Date().toISOString().slice(0, 10), assignedTo: '', status: 'scheduled', cost: 0, notes: '', checklist: [], completedItems: [] })
const maintenanceForm = reactive(blankMaintenance())
const normalizeList = (payload) => Array.isArray(payload) ? payload : payload?.data || []
const staffHasPermission = (item, permission) =>
  Array.isArray(item.permissions) &&
  item.permissions.some((itemPermission) => String(itemPermission).toLowerCase() === permission)
const isActiveStaff = (item) => String(item.accountStatus || '').toLowerCase() === 'active'
const buildStaffOptions = (permission) => staff.value
  .filter((item) => isActiveStaff(item) && staffHasPermission(item, permission))
  .map((item) => ({ title: `${item.employeeCode || item.username} · ${item.fullName}`, value: item.username }))
const incidentStaffOptions = computed(() => buildStaffOptions('manage_incidents'))
const maintenanceStaffOptions = computed(() => buildStaffOptions('manage_maintenance'))

const filteredIncidents = computed(() => { const keyword = search.value.trim().toLowerCase(); return incidents.value.filter((item) => (statusFilter.value === 'all' || item.status === statusFilter.value) && (priorityFilter.value === 'all' || item.priority === priorityFilter.value) && (assignmentFilter.value === 'all' || (assignmentFilter.value === 'mine' ? item.assignedTo === username : !item.assignedTo)) && (!keyword || [item.studentName, item.studentCode, item.roomName, item.building, item.description].join(' ').toLowerCase().includes(keyword))) })
const sortedMaintenance = computed(() => [...maintenance.value].sort((a, b) => new Date(a.nextDueDate) - new Date(b.nextDueDate)))
const canClaimSelected = computed(() => isAdmin || !selectedIncident.value?.assignedTo || selectedIncident.value?.assignedTo === username)
const canUpdateSelected = computed(() => isAdmin || selectedIncident.value?.assignedTo === username)
const canEditMaintenance = (item) => isAdmin || item.assignedTo === username
const metrics = computed(() => [{ label: 'Mới chờ tiếp nhận', value: incidents.value.filter((item) => item.status === 'new').length, icon: 'mdi-inbox-arrow-down-outline' }, { label: 'Đang xử lý', value: incidents.value.filter((item) => ['assigned', 'processing', 'waiting-materials', 'reopened'].includes(item.status)).length, icon: 'mdi-tools' }, { label: 'Quá hạn', value: incidents.value.filter((item) => item.dueAt && new Date(item.dueAt) < new Date() && !['confirmed', 'rejected', 'cancelled'].includes(item.status)).length, icon: 'mdi-clock-alert-outline' }, { label: 'Bảo trì sắp hạn', value: maintenance.value.filter((item) => new Date(item.nextDueDate) <= new Date(Date.now() + 7 * 86400000) && item.status !== 'completed').length, icon: 'mdi-calendar-alert-outline' }])

const loadAll = async () => { try { loading.value = true; error.value = ''; const requests = [api.get('/incidents'), api.get('/maintenance'), api.get('/auth/staff')]; const [incidentResponse, maintenanceResponse, staffResponse] = await Promise.all(requests); incidents.value = normalizeList(incidentResponse.data); maintenance.value = normalizeList(maintenanceResponse.data); staff.value = normalizeList(staffResponse.data) } catch (err) { error.value = 'Không tải được dữ liệu sửa chữa và bảo trì.'; console.error(err) } finally { loading.value = false } }
const toLocalInput = (value) => value ? new Date(new Date(value).getTime() - new Date(value).getTimezoneOffset() * 60000).toISOString().slice(0, 16) : ''
const openIncident = (item) => { selectedIncident.value = item; Object.assign(incidentForm, { assignedTo: item.assignedTo || (isAdmin ? '' : username), priority: item.priority || 'normal', scheduledAt: toLocalInput(item.scheduledAt), dueAt: toLocalInput(item.dueAt), assignmentNote: '', status: ['new', 'assigned'].includes(item.status) ? 'processing' : item.status, staffNote: item.staffNote || '', rootCause: item.rootCause || '', resolution: item.resolution || '', materialCost: item.materialCost || 0, laborCost: item.laborCost || 0 }); incidentDialog.value = true }
const selectedStaff = () => staff.value.find((item) => item.username === incidentForm.assignedTo)
const saveAssignment = async () => { if (!selectedIncident.value) return; if (!incidentForm.assignedTo) incidentForm.assignedTo = username; try { saving.value = true; const response = await api.patch(`/incidents/${selectedIncident.value.id}/assign`, { assignedTo: incidentForm.assignedTo, assignedName: selectedStaff()?.fullName || localStorage.getItem('fullName'), priority: incidentForm.priority, scheduledAt: incidentForm.scheduledAt || null, dueAt: incidentForm.dueAt || null, note: incidentForm.assignmentNote }); success.value = response.data?.warning || 'Đã lưu phân công và đồng bộ trạng thái phòng nếu xác định được phòng.'; await refreshSelected() } catch (err) { error.value = err.response?.data?.message || 'Không lưu được phân công.' } finally { saving.value = false } }
const saveProgress = async () => { if (!selectedIncident.value) return; try { saving.value = true; const response = await api.patch(`/incidents/${selectedIncident.value.id}/status`, { status: incidentForm.status, handledBy: username, staffNote: incidentForm.staffNote, rootCause: incidentForm.rootCause, resolution: incidentForm.resolution, materialCost: incidentForm.materialCost, laborCost: incidentForm.laborCost }); success.value = response.data?.warning || (incidentForm.status === 'completed' ? 'Đã hoàn thành yêu cầu và cập nhật lại trạng thái phòng.' : 'Đã cập nhật tiến độ yêu cầu và trạng thái phòng.'); await refreshSelected() } catch (err) { error.value = err.response?.data?.message || 'Không cập nhật được tiến độ.' } finally { saving.value = false } }
const refreshSelected = async () => { await loadAll(); const fresh = incidents.value.find((item) => item.id === selectedIncident.value.id); if (fresh) selectedIncident.value = fresh }
const openCreateMaintenance = () => { editingMaintenance.value = null; Object.assign(maintenanceForm, blankMaintenance()); maintenanceDialog.value = true }
const openMaintenance = (item) => { editingMaintenance.value = item; Object.assign(maintenanceForm, { ...blankMaintenance(), ...item, nextDueDate: String(item.nextDueDate).slice(0, 10), notes: item.notes || '', checklist: [...(item.checklist || [])], completedItems: [...(item.completedItems || [])] }); maintenanceDialog.value = true }
const saveMaintenance = async () => { try { saving.value = true; const assignee = staff.value.find((item) => item.username === maintenanceForm.assignedTo); const response = editingMaintenance.value ? await api.patch(`/maintenance/${editingMaintenance.value.id}`, { status: maintenanceForm.status, assignedTo: maintenanceForm.assignedTo, assignedName: assignee?.fullName, nextDueDate: maintenanceForm.nextDueDate, completedItems: maintenanceForm.completedItems, cost: maintenanceForm.cost, notes: maintenanceForm.notes, updatedBy: username }) : await api.post('/maintenance', { ...maintenanceForm, assignedName: assignee?.fullName, checklist: maintenanceForm.notes.split('\n').map((line) => line.trim()).filter(Boolean), notes: '' }); maintenanceDialog.value = false; success.value = response.data?.warning || 'Đã lưu lịch bảo trì.'; await loadAll() } catch (err) { error.value = err.response?.data?.message || 'Không lưu được lịch bảo trì.' } finally { saving.value = false } }

const statusLabel = (value) => statusOptions.find((item) => item.value === value)?.title || value
const priorityLabel = (value) => priorityOptions.find((item) => item.value === value)?.title || 'Bình thường'
const categoryLabel = (value) => ({ Electric: 'Điện', Water: 'Nước', Internet: 'Internet', Furniture: 'Nội thất', Safety: 'An toàn', Sanitation: 'Vệ sinh' }[value] || 'Khác')
const maintenanceStatusLabel = (value) => maintenanceStatusOptions.find((item) => item.value === value)?.title || value
const frequencyLabel = (value) => frequencyOptions.find((item) => item.value === value)?.title || value
const formatDate = (value) => value ? new Intl.DateTimeFormat('vi-VN').format(new Date(value)) : '-'
const formatDateTime = (value) => value ? new Intl.DateTimeFormat('vi-VN', { dateStyle: 'short', timeStyle: 'short' }).format(new Date(value)) : '-'
const money = (value) => new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND', maximumFractionDigits: 0 }).format(Number(value || 0))
const isOverdue = (item) => new Date(item.nextDueDate) < new Date() && item.status !== 'completed'
const daysUntil = (value) => { const days = Math.ceil((new Date(value) - new Date()) / 86400000); return days <= 0 ? 'Đến hạn hôm nay' : `Còn ${days} ngày` }
onMounted(() => {
  if (!canManageIncidents && canManageMaintenance) activeView.value = 'maintenance'
  loadAll()
})
</script>

<style scoped>
.operations-page { display: grid; min-width: 0; max-width: 100%; gap: 18px; } .page-head, .maintenance-head { display: flex; min-width: 0; justify-content: space-between; align-items: flex-start; gap: 18px; } .page-head h2 { margin: 4px 0 6px; font-size: 30px; } .page-head p, .maintenance-head p { margin: 0; color: var(--muted); overflow-wrap: anywhere; }
.metric-grid { display: grid; grid-template-columns: repeat(4, 1fr); gap: 14px; } .metric-card { display: grid; grid-template-columns: 46px 1fr; gap: 14px; align-items: center; padding: 18px; border: 1px solid var(--line); border-radius: 8px; background: #fff; } .metric-card > .mdi { display: grid; place-items: center; width: 46px; height: 46px; border-radius: 8px; background: #e6f4ff; color: #1677ff; font-size: 24px; } .metric-card strong, .metric-card small { display: block; } .metric-card strong { font-size: 28px; } .metric-card small { color: var(--muted); }
.view-tabs, .panel { min-width: 0; max-width: 100%; border: 1px solid var(--line); border-radius: 8px; background: #fff; } .panel { margin-top: 14px; padding: 18px; overflow: hidden; } .toolbar-grid { display: grid; grid-template-columns: minmax(260px, 1fr) repeat(3, 180px); gap: 10px; margin-bottom: 16px; } .operations-table { width: 100%; max-width: 100%; border: 1px solid #e8ece9; border-radius: 6px; } :deep(.operations-table .v-table__wrapper) { max-width: 100%; overflow-x: auto; } .cell-stack { max-width: 320px; } .cell-stack strong, .cell-stack small, .overdue small { display: block; } .cell-stack small, .overdue small, .muted { margin-top: 4px; color: var(--muted); font-size: 12px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.priority-tag, .status-tag { display: inline-flex; padding: 5px 9px; border-radius: 999px; font-size: 12px; font-weight: 800; white-space: nowrap; } .priority-tag.low { background: #f5f5f5; color: #595959; } .priority-tag.normal { background: #e6f4ff; color: #0958d9; } .priority-tag.high { background: #fff7e6; color: #d46b08; } .priority-tag.urgent { background: #fff1f0; color: #cf1322; } .status-tag { background: #f5f5f5; color: #595959; } .status-tag.processing, .status-tag.in-progress { background: #e6f4ff; color: #0958d9; } .status-tag.completed, .status-tag.confirmed { background: #f6ffed; color: #389e0d; } .status-tag.waiting-materials, .status-tag.reopened { background: #fff7e6; color: #d46b08; } .status-tag.rejected, .status-tag.cancelled { background: #fff1f0; color: #cf1322; }
.maintenance-head { margin-bottom: 16px; } .maintenance-head h3 { margin: 0 0 5px; } .overdue { color: #cf1322; font-weight: 800; }
.incident-detail { display: grid; gap: 18px; } .detail-summary { display: grid; grid-template-columns: repeat(4, 1fr); gap: 10px; } .detail-summary > div, .description-box, .action-panel, .timeline-panel { padding: 14px; border: 1px solid #e8ece9; border-radius: 8px; } .detail-summary span, .detail-summary strong { display: block; } .detail-summary span { color: var(--muted); font-size: 12px; } .detail-summary strong { margin-top: 5px; } .description-box p { margin: 7px 0 0; line-height: 1.55; } .detail-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 14px; } .action-panel h3, .timeline-panel h3 { margin: 0 0 14px; } .two-cols, .form-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 10px; } .full-width { grid-column: 1 / -1; }
.timeline-row { display: grid; grid-template-columns: 12px 1fr; gap: 10px; padding: 9px 0; } .timeline-row > span { width: 9px; height: 9px; margin-top: 6px; border-radius: 50%; background: #1677ff; } .timeline-row strong, .timeline-row small { display: block; } .timeline-row small { margin-top: 3px; color: var(--muted); } .timeline-row p { margin: 5px 0 0; }
.checklist-panel { padding: 14px; border: 1px solid #e8ece9; border-radius: 8px; background: #fafafa; }
.checklist-panel > strong { display: block; margin-bottom: 6px; }
@media (max-width: 1100px) { .metric-grid { grid-template-columns: repeat(2, minmax(0, 1fr)); } .toolbar-grid { grid-template-columns: minmax(0, 1fr) minmax(0, 1fr); } .detail-summary { grid-template-columns: repeat(2, minmax(0, 1fr)); } } @media (max-width: 720px) { .page-head, .maintenance-head { flex-direction: column; } .page-head h2 { font-size: 26px; } .metric-grid, .toolbar-grid, .detail-grid, .detail-summary, .two-cols, .form-grid { grid-template-columns: minmax(0, 1fr); } .panel { padding: 12px; } }
</style>
