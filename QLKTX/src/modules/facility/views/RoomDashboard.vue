<template>
  <section class="room-dashboard">
    <div class="page-heading">
      <div>
        <span class="page-kicker">Nhóm 1 - Room & Building</span>
        <h2>Quản lý phòng và tòa nhà</h2>
      </div>
      <v-btn
        color="primary"
        variant="flat"
        prepend-icon="mdi-refresh"
        :loading="loading"
        @click="loadAll"
      >
        Làm mới
      </v-btn>
    </div>

    <v-alert v-if="message" :type="messageType" variant="tonal">
      {{ message }}
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
          <span>Sửa chữa</span>
          <strong>{{ roomMetrics.maintenanceRooms }}</strong>
        </v-sheet>
      </v-col>
    </v-row>

    <v-sheet class="tab-shell">
      <v-tabs v-model="activeTab" color="primary">
        <v-tab value="map">Sơ đồ tầng</v-tab>
        <v-tab value="buildings">Tòa nhà</v-tab>
        <v-tab value="roomTypes">Loại phòng</v-tab>
        <v-tab value="rooms">Phòng & trạng thái</v-tab>
      </v-tabs>
    </v-sheet>

    <v-window v-model="activeTab">
      <v-window-item value="map">
        <v-sheet class="panel">
          <div class="panel-toolbar">
            <v-row>
              <v-col cols="12" md="4">
                <v-select
                  v-model="selectedBuilding"
                  :items="buildingFilterOptions"
                  item-title="title"
                  item-value="value"
                  label="Tòa nhà"
                  density="comfortable"
                  variant="outlined"
                  hide-details
                />
              </v-col>
              <v-col cols="12" md="4">
                <v-select
                  v-model="selectedFloor"
                  :items="floorOptions"
                  item-title="title"
                  item-value="value"
                  label="Tầng"
                  density="comfortable"
                  variant="outlined"
                  hide-details
                />
              </v-col>
              <v-col cols="12" md="4">
                <v-select
                  v-model="selectedStatus"
                  :items="statusFilterOptions"
                  item-title="title"
                  item-value="value"
                  label="Trạng thái"
                  density="comfortable"
                  variant="outlined"
                  hide-details
                />
              </v-col>
            </v-row>
          </div>

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
              <v-sheet class="empty-state">Không có phòng phù hợp.</v-sheet>
            </v-col>
          </v-row>
        </v-sheet>
      </v-window-item>

      <v-window-item value="buildings">
        <v-sheet class="panel">
          <div class="panel-head">
            <h3>Tòa nhà</h3>
            <v-btn color="primary" prepend-icon="mdi-plus" @click="openCreateBuilding">
              Thêm tòa
            </v-btn>
          </div>

          <table class="data-table">
            <thead>
              <tr>
                <th>Mã tòa</th>
                <th>Tên hiển thị</th>
                <th>Số tầng</th>
                <th>Số phòng</th>
                <th>Giường trống</th>
                <th>Thao tác</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="building in buildings" :key="building.buildingName">
                <td>{{ building.buildingName }}</td>
                <td>{{ building.displayName }}</td>
                <td>{{ building.floors }}</td>
                <td>{{ building.totalRooms }}</td>
                <td>{{ building.availableBeds }}</td>
                <td>
                  <div class="row-actions">
                    <v-btn icon="mdi-pencil" size="small" variant="text" @click="openEditBuilding(building)" />
                    <v-btn icon="mdi-delete" size="small" variant="text" color="error" @click="deleteBuilding(building)" />
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </v-sheet>
      </v-window-item>

      <v-window-item value="roomTypes">
        <v-sheet class="panel">
          <div class="panel-head">
            <h3>Loại phòng</h3>
            <v-btn color="primary" prepend-icon="mdi-plus" @click="openCreateRoomType">
              Thêm loại phòng
            </v-btn>
          </div>

          <table class="data-table">
            <thead>
              <tr>
                <th>Loại phòng</th>
                <th>Số giường tối đa</th>
                <th>Đơn giá</th>
                <th>Số phòng</th>
                <th>Tiện nghi mặc định</th>
                <th>Thao tác</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="roomType in roomTypes" :key="roomType.roomType">
                <td>{{ roomType.roomType }}</td>
                <td>{{ roomType.capacity }}</td>
                <td>{{ formatPrice(roomType.monthlyFee) }}</td>
                <td>{{ roomType.totalRooms }}</td>
                <td>{{ roomType.amenities }}</td>
                <td>
                  <div class="row-actions">
                    <v-btn icon="mdi-pencil" size="small" variant="text" @click="openEditRoomType(roomType)" />
                    <v-btn icon="mdi-delete" size="small" variant="text" color="error" @click="deleteRoomType(roomType)" />
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </v-sheet>
      </v-window-item>

      <v-window-item value="rooms">
        <v-sheet class="panel">
          <div class="panel-head">
            <h3>Phòng & trạng thái</h3>
            <v-btn color="primary" prepend-icon="mdi-plus" @click="openCreateRoom">
              Thêm phòng
            </v-btn>
          </div>

          <table class="data-table">
            <thead>
              <tr>
                <th>Phòng</th>
                <th>Tòa / tầng</th>
                <th>Loại</th>
                <th>Giới tính</th>
                <th>Sức chứa</th>
                <th>Giá</th>
                <th>Trạng thái</th>
                <th>Thao tác</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="room in rooms" :key="room.roomId">
                <td>{{ room.roomNumber }}</td>
                <td>{{ room.buildingDisplayName }} / {{ room.floorName }}</td>
                <td>{{ room.roomType }}</td>
                <td>{{ room.genderText }}</td>
                <td>{{ room.occupiedBeds }}/{{ room.capacity }}</td>
                <td>{{ formatPrice(room.monthlyFee) }}</td>
                <td>
                  <v-select
                    :model-value="room.status"
                    :items="statusOptions"
                    item-title="title"
                    item-value="value"
                    density="compact"
                    variant="outlined"
                    hide-details
                    class="status-select"
                    @update:model-value="updateRoomStatus(room, $event)"
                  />
                </td>
                <td>
                  <div class="row-actions">
                    <v-btn icon="mdi-eye" size="small" variant="text" @click="openRoomDetail(room)" />
                    <v-btn icon="mdi-pencil" size="small" variant="text" @click="openEditRoom(room)" />
                    <v-btn icon="mdi-delete" size="small" variant="text" color="error" @click="deleteRoom(room)" />
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </v-sheet>
      </v-window-item>
    </v-window>

    <v-dialog v-model="buildingDialog" max-width="560px">
      <v-card>
        <v-card-title>{{ editingBuildingName ? 'Sửa tòa nhà' : 'Thêm tòa nhà' }}</v-card-title>
        <v-card-text>
          <v-row>
            <v-col cols="12" md="4">
              <v-text-field
                v-model="buildingForm.buildingName"
                label="Mã tòa"
                density="compact"
                :disabled="Boolean(editingBuildingName)"
              />
            </v-col>
            <v-col cols="12" md="4">
              <v-text-field v-model="buildingForm.displayName" label="Tên hiển thị" density="compact" />
            </v-col>
            <v-col cols="12" md="4">
              <v-text-field v-model.number="buildingForm.floors" label="Số tầng" type="number" density="compact" />
            </v-col>
            <v-col cols="12">
              <v-textarea v-model="buildingForm.description" label="Ghi chú" rows="2" density="compact" />
            </v-col>
          </v-row>
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="buildingDialog = false">Hủy</v-btn>
          <v-btn color="primary" :loading="saving" @click="saveBuilding">Lưu</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <v-dialog v-model="roomTypeDialog" max-width="620px">
      <v-card>
        <v-card-title>{{ editingRoomType ? 'Sửa loại phòng' : 'Thêm loại phòng' }}</v-card-title>
        <v-card-text>
          <v-row>
            <v-col cols="12" md="4">
              <v-text-field
                v-model="roomTypeForm.roomType"
                label="Loại phòng"
                density="compact"
                :disabled="Boolean(editingRoomType)"
              />
            </v-col>
            <v-col cols="12" md="4">
              <v-text-field v-model.number="roomTypeForm.capacity" label="Số giường tối đa" type="number" density="compact" />
            </v-col>
            <v-col cols="12" md="4">
              <v-text-field v-model.number="roomTypeForm.monthlyFee" label="Đơn giá tháng" type="number" density="compact" />
            </v-col>
            <v-col cols="12">
              <v-text-field v-model="roomTypeForm.amenities" label="Tiện nghi mặc định" density="compact" />
            </v-col>
            <v-col cols="12">
              <v-textarea v-model="roomTypeForm.description" label="Mô tả" rows="2" density="compact" />
            </v-col>
          </v-row>
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="roomTypeDialog = false">Hủy</v-btn>
          <v-btn color="primary" :loading="saving" @click="saveRoomType">Lưu</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <v-dialog v-model="roomDialog" max-width="720px">
      <v-card>
        <v-card-title>{{ editingRoomId ? 'Sửa phòng' : 'Thêm phòng' }}</v-card-title>
        <v-card-text>
          <v-row>
            <v-col cols="12" md="3">
              <v-text-field
                v-model.number="roomForm.roomId"
                label="ID phòng"
                type="number"
                density="compact"
                :disabled="Boolean(editingRoomId)"
              />
            </v-col>
            <v-col cols="12" md="3">
              <v-text-field v-model="roomForm.roomNumber" label="Số phòng" density="compact" />
            </v-col>
            <v-col cols="12" md="3">
              <v-select
                v-model="roomForm.buildingName"
                :items="buildingOptions"
                item-title="title"
                item-value="value"
                label="Tòa"
                density="compact"
              />
            </v-col>
            <v-col cols="12" md="3">
              <v-text-field v-model.number="roomForm.floor" label="Tầng" type="number" density="compact" />
            </v-col>
            <v-col cols="12" md="3">
              <v-select
                v-model="roomForm.roomType"
                :items="roomTypeOptions"
                item-title="title"
                item-value="value"
                label="Loại phòng"
                density="compact"
                @update:model-value="applySelectedRoomType"
              />
            </v-col>
            <v-col cols="12" md="3">
              <v-select
                v-model="roomForm.gender"
                :items="genderOptions"
                item-title="title"
                item-value="value"
                label="Giới tính"
                density="compact"
              />
            </v-col>
            <v-col cols="12" md="3">
              <v-text-field v-model.number="roomForm.capacity" label="Sức chứa" type="number" density="compact" />
            </v-col>
            <v-col cols="12" md="3">
              <v-text-field v-model.number="roomForm.monthlyFee" label="Đơn giá" type="number" density="compact" />
            </v-col>
            <v-col cols="12" md="4">
              <v-select
                v-model="roomForm.status"
                :items="statusOptions"
                item-title="title"
                item-value="value"
                label="Trạng thái"
                density="compact"
              />
            </v-col>
            <v-col cols="12" md="8">
              <v-text-field v-model="roomForm.amenities" label="Tiện nghi" density="compact" />
            </v-col>
          </v-row>
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="roomDialog = false">Hủy</v-btn>
          <v-btn color="primary" :loading="saving" @click="saveRoom">Lưu</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <v-dialog v-model="detailDialog" max-width="620px">
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

          <p class="font-weight-bold mb-2">Sinh viên đang ở (Reference IDs)</p>
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
          <v-btn color="primary" variant="text" @click="detailDialog = false">Đóng</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </section>
</template>

<script setup>
import { computed, onMounted, ref, watch } from 'vue'
import api from '@/services/api'

const ALL_VALUE = 'all'

const activeTab = ref('map')
const loading = ref(false)
const saving = ref(false)
const message = ref('')
const messageType = ref('success')

const rooms = ref([])
const buildings = ref([])
const roomTypes = ref([])

const selectedBuilding = ref(ALL_VALUE)
const selectedFloor = ref(ALL_VALUE)
const selectedStatus = ref(ALL_VALUE)
const selectedRoom = ref(null)

const buildingDialog = ref(false)
const roomTypeDialog = ref(false)
const roomDialog = ref(false)
const detailDialog = ref(false)

const editingBuildingName = ref('')
const editingRoomType = ref('')
const editingRoomId = ref(null)

const emptyBuildingForm = () => ({
  buildingName: '',
  displayName: '',
  floors: 1,
  description: '',
})

const emptyRoomTypeForm = () => ({
  roomType: '',
  capacity: 4,
  monthlyFee: 0,
  description: '',
  amenities: '',
})

const emptyRoomForm = () => {
  const firstBuilding = buildings.value[0]?.buildingName || ''
  const firstRoomType = roomTypes.value[0]
  const roomId = rooms.value.reduce(
    (max, room) => Math.max(max, Number(room.roomId || 0)),
    0,
  ) + 1

  return {
    roomId,
    roomNumber: String(roomId),
    buildingName: firstBuilding,
    floor: 1,
    roomType: firstRoomType?.roomType || '',
    gender: true,
    capacity: firstRoomType?.capacity || 4,
    monthlyFee: firstRoomType?.monthlyFee || 0,
    status: 'Available',
    amenities: firstRoomType?.amenities || '',
  }
}

const buildingForm = ref(emptyBuildingForm())
const roomTypeForm = ref(emptyRoomTypeForm())
const roomForm = ref(emptyRoomForm())

const statusOptions = [
  { title: 'Trống', value: 'Available' },
  { title: 'Đầy', value: 'Full' },
  { title: 'Đang sửa chữa', value: 'Maintenance' },
]

const statusFilterOptions = computed(() => [
  { title: 'Tất cả trạng thái', value: ALL_VALUE },
  ...statusOptions,
])

const genderOptions = [
  { title: 'Nam', value: true },
  { title: 'Nữ', value: false },
]

const buildingOptions = computed(() =>
  buildings.value.map((building) => ({
    title: building.displayName,
    value: building.buildingName,
  })),
)

const buildingFilterOptions = computed(() => [
  { title: 'Tất cả tòa', value: ALL_VALUE },
  ...buildingOptions.value,
])

const roomTypeOptions = computed(() =>
  roomTypes.value.map((roomType) => ({
    title: `${roomType.roomType} - ${formatPrice(roomType.monthlyFee)}`,
    value: roomType.roomType,
  })),
)

const floorOptions = computed(() => {
  const floors = new Set()

  if (selectedBuilding.value !== ALL_VALUE) {
    const building = buildings.value.find((item) => item.buildingName === selectedBuilding.value)
    for (let floor = 1; floor <= Number(building?.floors || 0); floor += 1) {
      floors.add(floor)
    }
  }

  rooms.value
    .filter((room) => selectedBuilding.value === ALL_VALUE || room.buildingName === selectedBuilding.value)
    .forEach((room) => floors.add(room.floor))

  return [
    { title: 'Tất cả tầng', value: ALL_VALUE },
    ...Array.from(floors)
      .sort((first, second) => first - second)
      .map((floor) => ({ title: `Tầng ${floor}`, value: floor })),
  ]
})

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
      room.status === selectedStatus.value

    return matchBuilding && matchFloor && matchStatus
  })
})

const roomMetrics = computed(() => {
  return rooms.value.reduce(
    (summary, room) => {
      summary.totalRooms += 1
      summary.occupiedBeds += room.occupiedBeds
      summary.availableBeds += room.availableBeds

      if (room.status === 'Maintenance') {
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

watch(selectedBuilding, () => {
  selectedFloor.value = ALL_VALUE
})

const showMessage = (text, type = 'success') => {
  message.value = text
  messageType.value = type
}

const normalizeList = (payload) => {
  if (Array.isArray(payload)) return payload
  if (Array.isArray(payload?.data)) return payload.data
  if (Array.isArray(payload?.value)) return payload.value
  return []
}

const loadAll = async () => {
  try {
    loading.value = true
    const [buildingResponse, roomTypeResponse, roomResponse] = await Promise.all([
      api.get('/buildings'),
      api.get('/room-types'),
      api.get('/rooms'),
    ])

    buildings.value = normalizeList(buildingResponse.data)
    roomTypes.value = normalizeList(roomTypeResponse.data)
    rooms.value = normalizeList(roomResponse.data).map(normalizeRoom)
  } catch (error) {
    showMessage('Không tải được dữ liệu RoomService.', 'error')
    console.error(error)
  } finally {
    loading.value = false
  }
}

const normalizeRoom = (room) => {
  const capacity = Number(room.capacity ?? 0)
  const occupiedBeds = Number(room.occupiedBeds ?? room.currentOccupancy ?? 0)
  const availableBeds = Number(room.availableBeds ?? Math.max(capacity - occupiedBeds, 0))
  const buildingName = String(room.buildingName ?? '').replace(/^Tòa\s*/i, '')
  const status = normalizeStatus(room.status, availableBeds)

  return {
    ...room,
    roomId: Number(room.roomId ?? room.id ?? 0),
    roomNumber: String(room.roomNumber ?? room.name ?? room.roomId ?? ''),
    buildingName,
    buildingDisplayName: room.buildingDisplayName || `Tòa ${buildingName}`,
    floor: Number(room.floor ?? 1),
    floorName: room.floorName || `Tầng ${room.floor ?? 1}`,
    roomType: room.roomType ?? '',
    gender: parseGender(room.gender),
    genderText: room.genderText || (parseGender(room.gender) ? 'Nam' : 'Nữ'),
    capacity,
    occupiedBeds,
    availableBeds,
    monthlyFee: Number(room.monthlyFee ?? room.price ?? 0),
    status,
    amenities: room.amenities || '',
    occupancyReferences: Array.isArray(room.occupancyReferences) ? room.occupancyReferences : [],
  }
}

const parseGender = (value) => {
  if (typeof value === 'boolean') return value
  const normalized = String(value || '').toLowerCase()
  return normalized === 'true' || normalized === 'nam' || normalized === 'male'
}

const normalizeStatus = (status, availableBeds = 0) => {
  const normalized = String(status || '').toLowerCase()
  if (normalized.includes('maintenance') || normalized.includes('sửa')) return 'Maintenance'
  if (normalized.includes('full') || normalized.includes('đầy') || normalized.includes('day')) return 'Full'
  if (normalized.includes('available') || normalized.includes('trống') || normalized.includes('trong')) return 'Available'
  return availableBeds > 0 ? 'Available' : 'Full'
}

const openRoomDetail = (room) => {
  selectedRoom.value = room
  detailDialog.value = true
}

const openCreateBuilding = () => {
  editingBuildingName.value = ''
  buildingForm.value = emptyBuildingForm()
  buildingDialog.value = true
}

const openEditBuilding = (building) => {
  editingBuildingName.value = building.buildingName
  buildingForm.value = {
    buildingName: building.buildingName,
    displayName: building.displayName,
    floors: building.floors,
    description: building.description || '',
  }
  buildingDialog.value = true
}

const saveBuilding = async () => {
  try {
    saving.value = true
    if (editingBuildingName.value) {
      await api.put(`/buildings/${encodeURIComponent(editingBuildingName.value)}`, buildingForm.value)
      showMessage('Đã cập nhật tòa nhà.')
    } else {
      await api.post('/buildings', buildingForm.value)
      showMessage('Đã thêm tòa nhà.')
    }
    buildingDialog.value = false
    await loadAll()
  } catch (error) {
    showMessage(error.response?.data?.message || 'Không lưu được tòa nhà.', 'error')
    console.error(error)
  } finally {
    saving.value = false
  }
}

const deleteBuilding = async (building) => {
  if (!window.confirm(`Xóa ${building.displayName}?`)) return

  try {
    await api.delete(`/buildings/${encodeURIComponent(building.buildingName)}`)
    showMessage('Đã xóa tòa nhà.')
    await loadAll()
  } catch (error) {
    showMessage(error.response?.data?.message || 'Không xóa được tòa nhà.', 'error')
    console.error(error)
  }
}

const openCreateRoomType = () => {
  editingRoomType.value = ''
  roomTypeForm.value = emptyRoomTypeForm()
  roomTypeDialog.value = true
}

const openEditRoomType = (roomType) => {
  editingRoomType.value = roomType.roomType
  roomTypeForm.value = {
    roomType: roomType.roomType,
    capacity: roomType.capacity,
    monthlyFee: roomType.monthlyFee,
    description: roomType.description || '',
    amenities: roomType.amenities || '',
  }
  roomTypeDialog.value = true
}

const saveRoomType = async () => {
  try {
    saving.value = true
    if (editingRoomType.value) {
      await api.put(`/room-types/${encodeURIComponent(editingRoomType.value)}`, roomTypeForm.value)
      showMessage('Đã cập nhật loại phòng.')
    } else {
      await api.post('/room-types', roomTypeForm.value)
      showMessage('Đã thêm loại phòng.')
    }
    roomTypeDialog.value = false
    await loadAll()
  } catch (error) {
    showMessage(error.response?.data?.message || 'Không lưu được loại phòng.', 'error')
    console.error(error)
  } finally {
    saving.value = false
  }
}

const deleteRoomType = async (roomType) => {
  if (!window.confirm(`Xóa loại phòng ${roomType.roomType}?`)) return

  try {
    await api.delete(`/room-types/${encodeURIComponent(roomType.roomType)}`)
    showMessage('Đã xóa loại phòng.')
    await loadAll()
  } catch (error) {
    showMessage(error.response?.data?.message || 'Không xóa được loại phòng.', 'error')
    console.error(error)
  }
}

const openCreateRoom = () => {
  editingRoomId.value = null
  roomForm.value = emptyRoomForm()
  roomDialog.value = true
}

const openEditRoom = (room) => {
  editingRoomId.value = room.roomId
  roomForm.value = {
    roomId: room.roomId,
    roomNumber: room.roomNumber,
    buildingName: room.buildingName,
    floor: room.floor,
    roomType: room.roomType,
    gender: room.gender,
    capacity: room.capacity,
    monthlyFee: room.monthlyFee,
    status: room.status,
    amenities: room.amenities || '',
  }
  roomDialog.value = true
}

const applySelectedRoomType = () => {
  const selectedType = roomTypes.value.find((item) => item.roomType === roomForm.value.roomType)
  if (!selectedType) return

  roomForm.value.capacity = selectedType.capacity
  roomForm.value.monthlyFee = selectedType.monthlyFee
  roomForm.value.amenities = selectedType.amenities || roomForm.value.amenities
}

const saveRoom = async () => {
  try {
    saving.value = true
    const payload = {
      ...roomForm.value,
      roomId: Number(roomForm.value.roomId),
      floor: Number(roomForm.value.floor),
      capacity: Number(roomForm.value.capacity),
      monthlyFee: Number(roomForm.value.monthlyFee),
    }

    if (editingRoomId.value) {
      await api.put(`/rooms/${editingRoomId.value}`, payload)
      showMessage('Đã cập nhật phòng.')
    } else {
      await api.post('/rooms', payload)
      showMessage('Đã thêm phòng.')
    }

    roomDialog.value = false
    await loadAll()
  } catch (error) {
    showMessage(error.response?.data?.message || 'Không lưu được phòng.', 'error')
    console.error(error)
  } finally {
    saving.value = false
  }
}

const updateRoomStatus = async (room, status) => {
  try {
    await api.patch(`/rooms/${room.roomId}/status`, { status })
    showMessage('Đã cập nhật trạng thái phòng.')
    await loadAll()
  } catch (error) {
    showMessage(error.response?.data?.message || 'Không cập nhật được trạng thái phòng.', 'error')
    console.error(error)
  }
}

const deleteRoom = async (room) => {
  if (!window.confirm(`Xóa phòng ${room.roomNumber}?`)) return

  try {
    await api.delete(`/rooms/${room.roomId}`)
    showMessage('Đã xóa phòng.')
    await loadAll()
  } catch (error) {
    showMessage(error.response?.data?.message || 'Không xóa được phòng.', 'error')
    console.error(error)
  }
}

const getStatusColor = (status) => {
  if (status === 'Available') return 'success'
  if (status === 'Full') return 'error'
  return 'grey'
}

const getStatusText = (status) => {
  if (status === 'Available') return 'Trống'
  if (status === 'Full') return 'Đầy'
  return 'Đang sửa chữa'
}

const formatPrice = (value) => {
  return new Intl.NumberFormat('vi-VN', {
    style: 'currency',
    currency: 'VND',
  }).format(value || 0)
}

onMounted(loadAll)
</script>

<style scoped>
.room-dashboard {
  display: flex;
  flex-direction: column;
  gap: 18px;
}

.page-heading,
.panel-head {
  display: flex;
  align-items: flex-end;
  justify-content: space-between;
  gap: 18px;
}

.page-heading h2,
.panel-head h3 {
  margin: 4px 0 0;
  color: #1f3a5f;
  font-weight: 800;
}

.page-heading h2 {
  font-size: 1.7rem;
}

.panel-head h3 {
  font-size: 1.15rem;
}

.page-kicker {
  color: #1a73e8;
  font-size: 0.78rem;
  font-weight: 800;
  letter-spacing: 0;
  text-transform: uppercase;
}

.metric-sheet,
.tab-shell,
.panel,
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

.tab-shell {
  overflow: hidden;
}

.panel {
  display: flex;
  flex-direction: column;
  gap: 18px;
  margin-top: 18px;
  padding: 18px;
}

.panel-toolbar {
  margin-bottom: 2px;
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

.data-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 0.92rem;
}

.data-table th,
.data-table td {
  border-bottom: 1px solid #e6ebf2;
  padding: 12px 10px;
  text-align: left;
  vertical-align: middle;
}

.data-table th {
  color: #526174;
  font-size: 0.78rem;
  font-weight: 800;
  text-transform: uppercase;
}

.row-actions {
  display: flex;
  align-items: center;
  gap: 4px;
}

.status-select {
  min-width: 150px;
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

@media (max-width: 760px) {
  .page-heading,
  .panel-head {
    align-items: stretch;
    flex-direction: column;
  }

  .data-table {
    display: block;
    overflow-x: auto;
    white-space: nowrap;
  }
}
</style>
