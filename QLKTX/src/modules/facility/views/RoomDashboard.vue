<template>
  <section class="room-dashboard">
    <div class="page-heading">
      <div>
        <span class="page-kicker">Nhóm 1 - Room & Building</span>
        <h2>Sơ đồ phòng ký túc xá</h2>
        <p>Dữ liệu phòng được đồng bộ trực tiếp từ RoomService qua Gateway.</p>
      </div>
      <v-btn
        color="primary"
        variant="flat"
        prepend-icon="mdi-refresh"
        :loading="loading"
        @click="loadRooms"
      >
        Làm mới
      </v-btn>
    </div>

    <v-alert v-if="errorMessage" type="error" variant="tonal">
      {{ errorMessage }}
    </v-alert>

    <v-row>
      <v-col cols="12" md="3">
        <v-sheet class="metric-sheet">
          <span>Tổng phòng</span>
          <strong>{{ roomMetrics.totalRooms }}</strong>
        </v-sheet>
      </v-col>
      <v-col cols="12" md="3">
        <v-sheet class="metric-sheet">
          <span>Còn giường</span>
          <strong>{{ roomMetrics.availableBeds }}</strong>
        </v-sheet>
      </v-col>
      <v-col cols="12" md="3">
        <v-sheet class="metric-sheet">
          <span>Đang ở</span>
          <strong>{{ roomMetrics.occupiedBeds }}</strong>
        </v-sheet>
      </v-col>
      <v-col cols="12" md="3">
        <v-sheet class="metric-sheet">
          <span>Đang bảo trì</span>
          <strong>{{ roomMetrics.maintenanceRooms }}</strong>
        </v-sheet>
      </v-col>
    </v-row>

    <v-sheet class="filter-panel">
      <v-row>
        <v-col cols="12" sm="4">
          <v-select
            v-model="selectedBuilding"
            :items="buildingOptions"
            item-title="title"
            item-value="value"
            label="Chọn tòa nhà"
            variant="outlined"
            density="comfortable"
            hide-details
          />
        </v-col>
        <v-col cols="12" sm="4">
          <v-select
            v-model="selectedFloor"
            :items="floorOptions"
            item-title="title"
            item-value="value"
            label="Chọn tầng"
            variant="outlined"
            density="comfortable"
            hide-details
          />
        </v-col>
        <v-col cols="12" sm="4">
          <v-select
            v-model="selectedStatus"
            :items="statusOptions"
            item-title="title"
            item-value="value"
            label="Trạng thái"
            variant="outlined"
            density="comfortable"
            hide-details
          />
        </v-col>
      </v-row>
    </v-sheet>

    <v-progress-linear v-if="loading" indeterminate color="primary" />

    <v-row>
      <v-col
        v-for="room in filteredRooms"
        :key="room.roomId"
        cols="12"
        sm="6"
        md="4"
        lg="3"
      >
        <v-card
          clickable
          :border="true"
          class="room-card elevation-2"
          @click="openRoomDetail(room)"
        >
          <v-sheet :color="getStatusColor(room.status)" height="8" />

          <v-card-item>
            <div class="room-card-head">
              <div>
                <span class="room-title">Phòng {{ room.roomNumber }}</span>
                <small>{{ room.buildingDisplayName }} - {{ room.floorName }}</small>
              </div>
              <v-chip size="small" :color="getStatusColor(room.status)">
                {{ getStatusText(room.status) }}
              </v-chip>
            </div>

            <div class="room-meta">
              <span>
                <v-icon size="small">mdi-account-group</v-icon>
                {{ room.occupiedBeds }}/{{ room.capacity }} sinh viên
              </span>
              <span>
                <v-icon size="small">mdi-bed</v-icon>
                Còn {{ room.availableBeds }} giường
              </span>
              <span>
                <v-icon size="small">mdi-home-city-outline</v-icon>
                {{ room.roomType }} - {{ room.genderText }}
              </span>
              <span>
                <v-icon size="small">mdi-cash</v-icon>
                {{ formatPrice(room.monthlyFee) }}/tháng
              </span>
            </div>
          </v-card-item>
        </v-card>
      </v-col>

      <v-col v-if="!loading && filteredRooms.length === 0" cols="12">
        <v-sheet class="empty-state">
          Không có phòng phù hợp với bộ lọc hiện tại.
        </v-sheet>
      </v-col>
    </v-row>

    <v-dialog v-model="dialog" max-width="620px">
      <v-card v-if="selectedRoom">
        <v-card-title class="bg-primary text-white">
          Chi tiết phòng {{ selectedRoom.roomNumber }}
        </v-card-title>
        <v-card-text class="room-dialog">
          <p><strong>Tòa:</strong> {{ selectedRoom.buildingDisplayName }}</p>
          <p><strong>Tầng:</strong> {{ selectedRoom.floorName }}</p>
          <p><strong>Loại phòng:</strong> {{ selectedRoom.roomType }}</p>
          <p><strong>Giới tính:</strong> {{ selectedRoom.genderText }}</p>
          <p><strong>Trạng thái:</strong> {{ getStatusText(selectedRoom.status) }}</p>
          <p><strong>Sức chứa:</strong> {{ selectedRoom.occupiedBeds }}/{{ selectedRoom.capacity }} sinh viên</p>
          <p><strong>Giá phòng:</strong> {{ formatPrice(selectedRoom.monthlyFee) }}/tháng</p>
          <p><strong>Tiện nghi:</strong> {{ selectedRoom.amenities }}</p>

          <v-divider class="my-4" />

          <p class="font-weight-bold mb-2">Danh sách sinh viên đang ở (Reference IDs)</p>
          <v-list v-if="selectedRoom.occupancyReferences.length > 0" density="compact">
            <v-list-item
              v-for="reference in selectedRoom.occupancyReferences"
              :key="`${reference.studentId}-${reference.registrationId}`"
              prepend-icon="mdi-account"
            >
              <div class="reference-row">
                <span>SV #{{ reference.studentId }}</span>
                <small>Đơn #{{ reference.registrationId }} - {{ reference.contractCode }}</small>
              </div>
            </v-list-item>
          </v-list>
          <v-alert v-else type="info" variant="tonal" density="compact">
            Phòng chưa có sinh viên được xếp từ N2.
          </v-alert>
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn color="primary" variant="text" @click="dialog = false">Đóng</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </section>
</template>

<script setup>
import { computed, onMounted, ref } from 'vue'
import api from '@/services/api'

const ALL_VALUE = 'all'

const loading = ref(false)
const errorMessage = ref('')
const rooms = ref([])
const selectedBuilding = ref(ALL_VALUE)
const selectedFloor = ref(ALL_VALUE)
const selectedStatus = ref(ALL_VALUE)
const dialog = ref(false)
const selectedRoom = ref(null)

const statusOptions = [
  { title: 'Tất cả trạng thái', value: ALL_VALUE },
  { title: 'Còn chỗ', value: 'available' },
  { title: 'Đầy phòng', value: 'full' },
  { title: 'Đang bảo trì', value: 'maintenance' },
]

const buildingOptions = computed(() => [
  { title: 'Tất cả tòa', value: ALL_VALUE },
  ...uniqueBy(rooms.value, (room) => room.buildingName)
    .sort((first, second) => first.buildingName.localeCompare(second.buildingName))
    .map((room) => ({
      title: room.buildingDisplayName,
      value: room.buildingName,
    })),
])

const floorOptions = computed(() => [
  { title: 'Tất cả tầng', value: ALL_VALUE },
  ...uniqueBy(rooms.value, (room) => room.floor)
    .sort((first, second) => first.floor - second.floor)
    .map((room) => ({
      title: room.floorName,
      value: room.floor,
    })),
])

const filteredRooms = computed(() => {
  return rooms.value.filter((room) => {
    const matchBuilding =
      selectedBuilding.value === ALL_VALUE ||
      room.buildingName === selectedBuilding.value
    const matchFloor =
      selectedFloor.value === ALL_VALUE ||
      room.floor === selectedFloor.value
    const matchStatus =
      selectedStatus.value === ALL_VALUE ||
      normalizeStatus(room.status) === selectedStatus.value

    return matchBuilding && matchFloor && matchStatus
  })
})

const roomMetrics = computed(() => {
  return rooms.value.reduce(
    (summary, room) => {
      summary.totalRooms += 1
      summary.occupiedBeds += room.occupiedBeds
      summary.availableBeds += room.availableBeds

      if (normalizeStatus(room.status) === 'maintenance') {
        summary.maintenanceRooms += 1
      }

      return summary
    },
    {
      totalRooms: 0,
      occupiedBeds: 0,
      availableBeds: 0,
      maintenanceRooms: 0,
    },
  )
})

const loadRooms = async () => {
  try {
    loading.value = true
    errorMessage.value = ''

    const response = await api.get('/rooms')
    const payload = Array.isArray(response.data) ? response.data : response.data?.data || []
    rooms.value = payload.map(normalizeRoom)
  } catch (error) {
    rooms.value = []
    errorMessage.value = 'Không tải được dữ liệu phòng từ RoomService.'
    console.error(error)
  } finally {
    loading.value = false
  }
}

const openRoomDetail = (room) => {
  selectedRoom.value = room
  dialog.value = true
}

const normalizeRoom = (room) => {
  const roomId = Number(room.roomId ?? room.id ?? 0)
  const capacity = Number(room.capacity ?? 0)
  const occupiedBeds = Number(room.occupiedBeds ?? room.currentOccupancy ?? room.currentOccupants ?? 0)
  const availableBeds = Number(room.availableBeds ?? Math.max(capacity - occupiedBeds, 0))
  const buildingName = String(room.buildingName ?? room.building ?? '').replace(/^Tòa\s*/i, '')
  const floor = Number(room.floor ?? getFloorFromRoomId(roomId))
  const gender = parseGender(room.gender)

  return {
    roomId,
    roomNumber: String(room.roomNumber ?? room.name ?? roomId),
    buildingName,
    buildingDisplayName: room.buildingDisplayName || `Tòa ${buildingName}`,
    floor,
    floorName: room.floorName || `Tầng ${floor}`,
    roomType: room.roomType ?? room.roomTypeName ?? '',
    gender,
    genderText: room.genderText || (gender ? 'Nam' : 'Nữ'),
    capacity,
    occupiedBeds,
    availableBeds,
    monthlyFee: Number(room.monthlyFee ?? room.price ?? 0),
    status: normalizeStatus(room.status, availableBeds),
    amenities: room.amenities || buildAmenities(room.roomType ?? room.roomTypeName),
    studentIds: Array.isArray(room.studentIds) ? room.studentIds : [],
    occupancyReferences: normalizeReferences(room),
  }
}

const normalizeReferences = (room) => {
  if (Array.isArray(room.occupancyReferences)) {
    return room.occupancyReferences.map((reference) => ({
      studentId: reference.studentId,
      registrationId: reference.registrationId,
      contractCode: reference.contractCode || '',
    }))
  }

  if (Array.isArray(room.studentIds)) {
    return room.studentIds.map((studentId) => ({
      studentId,
      registrationId: '',
      contractCode: '',
    }))
  }

  return []
}

const normalizeStatus = (status, availableBeds = 0) => {
  const normalized = String(status || '').toLowerCase()

  if (normalized.includes('maintenance') || normalized.includes('sửa') || normalized.includes('bao tri')) {
    return 'maintenance'
  }

  if (normalized.includes('full') || normalized.includes('đầy') || normalized.includes('day')) {
    return 'full'
  }

  if (normalized.includes('available') || normalized.includes('trống') || normalized.includes('trong')) {
    return 'available'
  }

  return availableBeds > 0 ? 'available' : 'full'
}

const parseGender = (value) => {
  if (typeof value === 'boolean') return value

  const normalized = String(value || '').toLowerCase()
  return normalized === 'true' || normalized === 'nam' || normalized === 'male'
}

const getFloorFromRoomId = (roomId) => {
  if (!roomId) return 1
  return Math.max(Math.floor(roomId / 100), 1)
}

const buildAmenities = (roomType) => {
  const normalized = String(roomType || '').toLowerCase()
  if (normalized.includes('4')) return 'Điều hòa, quạt trần, tủ đồ'
  if (normalized.includes('6')) return 'Quạt trần, tủ đồ'
  return 'Quạt trần, giường tầng, tủ đồ'
}

const uniqueBy = (items, keySelector) => {
  const seen = new Set()
  return items.filter((item) => {
    const key = keySelector(item)

    if (seen.has(key)) return false
    seen.add(key)
    return true
  })
}

const getStatusColor = (status) => {
  const normalized = normalizeStatus(status)
  if (normalized === 'available') return 'success'
  if (normalized === 'full') return 'error'
  return 'grey'
}

const getStatusText = (status) => {
  const normalized = normalizeStatus(status)
  if (normalized === 'available') return 'Còn chỗ'
  if (normalized === 'full') return 'Đầy phòng'
  return 'Đang bảo trì'
}

const formatPrice = (value) => {
  return new Intl.NumberFormat('vi-VN', {
    style: 'currency',
    currency: 'VND',
  }).format(value || 0)
}

onMounted(loadRooms)
</script>

<style scoped>
.room-dashboard {
  display: flex;
  flex-direction: column;
  gap: 18px;
}

.page-heading {
  display: flex;
  align-items: flex-end;
  justify-content: space-between;
  gap: 18px;
}

.page-heading h2 {
  margin: 4px 0 0;
  font-size: 1.7rem;
  font-weight: 800;
  color: #1f3a5f;
}

.page-heading p {
  margin: 6px 0 0;
  color: #607085;
}

.page-kicker {
  color: #1a73e8;
  font-size: 0.78rem;
  font-weight: 800;
  letter-spacing: 0;
  text-transform: uppercase;
}

.metric-sheet,
.filter-panel,
.empty-state {
  border: 1px solid #e4e8ef;
  border-radius: 8px;
  background: #fff;
}

.metric-sheet {
  display: flex;
  flex-direction: column;
  gap: 8px;
  min-height: 94px;
  padding: 18px;
}

.metric-sheet span {
  color: #607085;
  font-size: 0.88rem;
}

.metric-sheet strong {
  color: #1f3a5f;
  font-size: 1.8rem;
  line-height: 1;
}

.filter-panel {
  padding: 16px;
}

.room-card {
  height: 100%;
  overflow: hidden;
  transition: transform 0.2s ease-in-out, box-shadow 0.2s ease-in-out;
  cursor: pointer;
}

.room-card:hover {
  transform: translateY(-4px);
}

.room-card-head {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 12px;
}

.room-card-head small {
  display: block;
  margin-top: 3px;
  color: #607085;
}

.room-title {
  color: #1f3a5f;
  font-size: 1.1rem;
  font-weight: 800;
}

.room-meta {
  display: flex;
  flex-direction: column;
  gap: 8px;
  margin-top: 16px;
  color: #526174;
  font-size: 0.9rem;
}

.room-meta span {
  display: flex;
  align-items: center;
  gap: 8px;
}

.empty-state {
  padding: 28px;
  text-align: center;
  color: #607085;
}

.room-dialog {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.room-dialog p {
  margin: 0;
}

.reference-row {
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.reference-row small {
  color: #607085;
}

@media (max-width: 700px) {
  .page-heading {
    align-items: stretch;
    flex-direction: column;
  }
}
</style>
