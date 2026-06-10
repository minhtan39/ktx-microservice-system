<template>
  <div>
    <h2 class="text-h5 font-weight-bold mb-4 text-primary">Quản lý sửa chữa và Sự cố</h2>

    <v-tabs v-model="activeTab" bg-color="white" color="primary" grow class="rounded elevation-1 mb-4">
      <v-tab value="new">🆕 Mới tiếp nhận ({{ countIncidents('new') }})</v-tab>
      <v-tab value="processing">🛠️ Đang xử lý ({{ countIncidents('processing') }})</v-tab>
      <v-tab value="done">✅ Hoàn thành ({{ countIncidents('done') }})</v-tab>
    </v-tabs>

    <v-window v-model="activeTab">
      <v-window-item v-for="status in ['new', 'processing', 'done']" :key="status" :value="status">
        <v-row>
          <v-col v-if="getIncidentsByStatus(status).length === 0" cols="12">
            <v-alert type="info" text="Không có yêu cầu sự cố nào ở trạng thái này."></v-alert>
          </v-col>
          
          <v-col 
            v-for="incident in getIncidentsByStatus(status)" 
            :key="incident.id" 
            cols="12" md="6"
          >
            <v-card class="elevation-2" :border="true">
              <v-card-item>
                <div class="d-flex justify-space-between">
                  <v-card-title class="text-subtitle-1 font-weight-bold">
                    Phòng {{ incident.roomName }} — Tòa {{ incident.building }}
                  </v-card-title>
                  <v-chip size="small" :color="getCategoryColor(incident.category)">
                    {{ incident.category }}
                  </v-chip>
                </div>
                <v-card-subtitle class="mt-1">Người gửi: {{ incident.studentName }}</v-card-subtitle>
              </v-card-item>

              <v-card-text class="text-body-2 bg-grey-lighten-5 py-3 mx-4 rounded text-grey-darken-3">
                <strong>Mô tả lỗi:</strong> {{ incident.description }}
              </v-card-text>

              <v-card-actions class="pa-4 justify-end">
                <v-btn 
                  v-if="status === 'new'" 
                  color="warning" 
                  variant="elevated" 
                  prepend-icon="mdi-wrench"
                  @click="updateStatus(incident.id, 'processing')"
                >
                  Tiếp nhận sửa chữa
                </v-btn>
                <v-btn 
                  v-if="status === 'processing'" 
                  color="success" 
                  variant="elevated" 
                  prepend-icon="mdi-check-circle"
                  @click="updateStatus(incident.id, 'done')"
                >
                  Báo cáo hoàn thành
                </v-btn>
                <span v-if="status === 'done'" class="text-success text-caption font-weight-bold">
                  <v-icon>mdi-check-all</v-icon> Đã sửa xong
                </span>
              </v-card-actions>
            </v-card>
          </v-col>
        </v-row>
      </v-window-item>
    </v-window>
  </div>
</template>

<script setup>
import { ref } from 'vue'

const activeTab = ref('new')

const incidents = ref([
  { id: 'ic1', roomName: '302', building: 'Tòa A1', studentName: 'Trần Văn C', category: 'Điện', description: 'Hỏng công tắc điều hòa, bật không lên điện nguồn.', status: 'new' },
  { id: 'ic2', roomName: '105', building: 'Tòa B1', studentName: 'Lê Văn D', category: 'Nước', description: 'Vòi hoa sen nhà vệ sinh bị rỉ nước liên tục gây ngập nền.', status: 'new' },
  { id: 'ic3', roomName: '204', building: 'Tòa A2', studentName: 'Phạm Thị E', category: 'Cơ sở vật chất', description: 'Khóa cửa ban công bị kẹt không khóa được.', status: 'processing' }
])

const getIncidentsByStatus = (status) => {
  return incidents.value.filter(i => i.status === status)
}

const countIncidents = (status) => {
  return incidents.value.filter(i => i.status === status).length
}

const getCategoryColor = (cat) => {
  if (cat === 'Điện') return 'orange-darken-2'
  if (cat === 'Nước') return 'blue'
  return 'brown'
}

const updateStatus = (id, newStatus) => {
  const item = incidents.value.find(i => i.id === id)
  if (item) {
    item.status = newStatus
    // Thực tế: Gửi lệnh PATCH cập nhật trạng thái sự cố tới Incident Service
    console.log(`Sự cố ${id} đã được chuyển sang trạng thái: ${newStatus}`)
  }
}
</script>