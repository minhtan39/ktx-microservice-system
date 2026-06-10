<template>
  <div>
    <h2 class="text-h5 font-weight-bold mb-4 text-primary">Sơ đồ phòng ký túc xá</h2>

    <v-row class="mb-4 bg-white pa-3 rounded elevation-1">
      <v-col cols="12" sm="4">
        <v-select
          v-model="selectedBuilding"
          :items="buildings"
          label="Chọn Tòa Nhà"
          variant="outlined"
          density="comfortable"
          hide-details
        ></v-select>
      </v-col>
      <v-col cols="12" sm="4">
        <v-select
          v-model="selectedFloor"
          :items="floors"
          label="Chọn Tầng"
          variant="outlined"
          density="comfortable"
          hide-details
        ></v-select>
      </v-col>
    </v-row>

    <v-row>
      <v-col 
        v-for="room in filteredRooms" 
        :key="room.id" 
        cols="12" sm="6" md="4" lg="3"
      >
        <v-card 
          clicakble 
          @click="openRoomDetail(room)"
          :border="true"
          class="room-card elevation-2"
        >
          <v-sheet :color="getStatusColor(room.status)" height="8"></v-sheet>
          
          <v-card-item>
            <div class="d-flex justify-space-between align-center">
              <span class="text-h6 font-weight-bold">Phòng {{ room.name }}</span>
              <v-chip size="small" :color="getStatusColor(room.status)">
                {{ getStatusText(room.status) }}
              </v-chip>
            </div>
            
            <div class="mt-3 text-body-2 text-grey-darken-1">
              <v-icon size="small" class="mr-1">mdi-account-group</v-icon>
              Sức chứa: <strong>{{ room.currentOccupants }}/{{ room.capacity }}</strong> Sinh viên
            </div>
            <div class="text-body-2 text-grey-darken-1 mt-1">
              <v-icon size="small" class="mr-1">mdi-currency-usd</v-icon>
              Giá: <strong>{{ formatPrice(room.price) }}</strong> / tháng
            </div>
          </v-card-item>
        </v-card>
      </v-col>
    </v-row>

    <v-dialog v-model="dialog" max-width="500px">
      <v-card v-if="selectedRoom">
        <v-card-title class="bg-primary text-white">
          Chi tiết Phòng {{ selectedRoom.name }}
        </v-card-title>
        <v-card-text class="pt-4">
          <p class="mb-2"><strong>Trạng thái:</strong> {{ getStatusText(selectedRoom.status) }}</p>
          <p class="mb-2"><strong>Tiện nghi:</strong> {{ selectedRoom.amenities }}</p>
          <v-divider class="my-3"></v-divider>
          <p class="font-weight-bold mb-2">Danh sách sinh viên đang ở (Reference IDs):</p>
          
          <v-list density="compact">
            <v-list-item 
              v-for="(studentId, index) in selectedRoom.studentIds" 
              :key="index"
              prepend-icon="mdi-account"
            >
              Mã số tham chiếu SV: <v-chip size="small">{{ studentId }}</v-chip>
            </v-list-item>
          </v-list>
        </v-card-text>
        <v-card-actions>
          <v-spacer></v-spacer>
          <v-btn color="primary" variant="text" @click="dialog = false">Đóng</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </div>
</template>

<script setup>
import { ref, computed } from 'vue'

// Dữ liệu giả lập (Mock Data) - Sau này sẽ gọi qua Axios từ Gateway của Facility Service
const selectedBuilding = ref('Tòa A1')
const selectedFloor = ref('Tầng 1')

const buildings = ref(['Tòa A1', 'Tòa A2', 'Tòa B1'])
const floors = ref(['Tầng 1', 'Tầng 2', 'Tầng 3'])

const rooms = ref([
  { id: 'r1', name: '101', building: 'Tòa A1', floor: 'Tầng 1', status: 'available', capacity: 6, currentOccupants: 2, price: 500000, amenities: 'Điều hòa, Quạt trần, Tủ đồ', studentIds: ['SV001', 'SV002'] },
  { id: 'r2', name: '102', building: 'Tòa A1', floor: 'Tầng 1', status: 'full', capacity: 4, currentOccupants: 4, price: 800000, amenities: 'Điều hòa, Tủ lạnh, Bình nóng lạnh', studentIds: ['SV003', 'SV004', 'SV005', 'SV006'] },
  { id: 'r3', name: '103', building: 'Tòa A1', floor: 'Tầng 1', status: 'maintenance', capacity: 6, currentOccupants: 0, price: 500000, amenities: 'Quạt trần', studentIds: [] },
])

// Bộ lọc phòng theo tòa và tầng chọn trên UI
const filteredRooms = computed(() => {
  return rooms.value.filter(r => r.building === selectedBuilding.value && r.floor === selectedFloor.value)
})

// Quản lý trạng thái Dialog hiển thị chi tiết
const dialog = ref(false)
const selectedRoom = ref(null)

const openRoomDetail = (room) => {
  selectedRoom.value = room
  dialog.value = true
}

// Các hàm bổ trợ (Helpers) định dạng hiển thị dữ liệu
const getStatusColor = (status) => {
  if (status === 'available') return 'success' // Xanh lá
  if (status === 'full') return 'error'        // Đỏ
  return 'grey'                                // Xám (bảo trì)
}

const getStatusText = (status) => {
  if (status === 'available') return 'Còn chỗ'
  if (status === 'full') return 'Đầy phòng'
  return 'Đang bảo trì'
}

const formatPrice = (value) => {
  return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(value)
}
</script>

<style scoped>
.room-card {
  transition: transform 0.2s ease-in-out;
  cursor: pointer;
}
.room-card:hover {
  transform: translateY(-4px);
}
</style>