<template>
  <div class="logs-container">
    <div class="heading-area">
      <h2>🛡️ NHẬT KÝ HOẠT ĐỘNG HỆ THỐNG (AUDIT LOGS)</h2>
      <button @click="fetchLogs" class="btn-refresh">🔄 Làm mới nhật ký</button>
    </div>

    <div class="filter-box">
      <input type="text" v-model="searchActor" placeholder="Tìm theo người thực hiện..." />
      <select v-model="filterAction">
        <option value="">-- Tất cả hành động --</option>
        <option value="Login">Login (Đăng nhập)</option>
        <option value="Create Incident">Create Incident (Báo hỏng)</option>
        <option value="Comment">Comment (Bình luận)</option>
      </select>
    </div>

    <div class="table-responsive">
      <table class="logs-table">
        <thead>
          <tr>
            <th>Thời Gian</th>
            <th>Tài Khoản</th>
            <th>Hành Động</th>
            <th>Chi Tiết Hoạt Động</th>
          </tr>
        </thead>
        <tbody>
          <tr v-if="filteredLogs.length === 0">
            <td colspan="4" class="no-data">Không có lịch sử nhật ký nào phù hợp.</td>
          </tr>
          <tr 
            v-else 
            v-for="log in filteredLogs" 
            :key="log.id"
            :class="getActionClass(log.action)"
          >
            <td>{{ formatDateTime(log.timestamp) }}</td>
            <td><span class="actor-badge">{{ log.actor }}</span></td>
            <td><strong class="action-text">{{ log.action }}</strong></td>
            <td>{{ log.details }}</td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted, computed } from 'vue'
import axios from 'axios'

const logs = ref([])
const searchActor = ref('')
const filterAction = ref('')

// Hàm gọi API lấy dữ liệu Logs từ Backend Máy 3
const fetchLogs = async () => {
  try {
    const token = localStorage.getItem('user_token')
    const response = await axios.get('http://192.168.61.57:5004/api/v1/billing/management/system-logs', {
      headers: { Authorization: `Bearer ${token}` }
    })
    logs.value = response.data
  } catch (error) {
    alert('Bạn không có quyền xem nhật ký này hoặc phiên đăng nhập hết hạn!')
    console.error(error)
  }
}

// Logic lọc dữ liệu ngay trên giao diện
const filteredLogs = computed(() => {
  return logs.value.filter(log => {
    const matchActor = log.actor.toLowerCase().includes(searchActor.value.toLowerCase())
    const matchAction = filterAction.value === '' || log.action === filterAction.value
    return matchActor && matchAction
  })
})

// Định dạng ngày giờ chuẩn Việt Nam
const formatDateTime = (timeStr) => {
  return new Date(timeStr).toLocaleString('vi-VN')
}

// Đổi màu sắc dòng tùy theo loại hành động để báo cáo trông đẹp mắt hơn
const getActionClass = (action) => {
  if (action === 'Login') return 'log-login'
  if (action === 'Create Incident') return 'log-create'
  if (action === 'Comment') return 'log-comment'
  return ''
}

onMounted(() => {
  fetchLogs()
})
</script>

<style scoped>
.logs-container { padding: 25px; background: #fafafa; min-height: 100vh; }
.heading-area { display: flex; justify-content: space-between; align-items: center; margin-bottom: 20px; }
.btn-refresh { padding: 10px 15px; background-color: #3498db; color: white; border: none; border-radius: 4px; cursor: pointer; font-weight: bold; }
.btn-refresh:hover { background-color: #2980b9; }

.filter-box { display: flex; gap: 15px; margin-bottom: 20px; }
.filter-box input, .filter-box select { padding: 10px; border: 1px solid #ccc; border-radius: 4px; width: 250px; }

.table-responsive { background: white; border-radius: 8px; box-shadow: 0 2px 8px rgba(0,0,0,0.06); overflow: hidden; }
.logs-table { width: 100%; border-collapse: collapse; text-align: left; font-size: 14px; }
.logs-table th { background-color: #34495e; color: white; padding: 12px 15px; font-weight: bold; }
.logs-table td { padding: 12px 15px; border-bottom: 1px solid #eee; }

.no-data { text-align: center; color: #999; padding: 30px !important; }
.actor-badge { background-color: #e0e0e0; padding: 3px 8px; border-radius: 12px; font-weight: 500; font-size: 13px; }

/* Styles highlight từng loại log */
.log-login { background-color: #f7fcff; }
.log-login .action-text { color: #2980b9; }
.log-create { background-color: #fff9f0; }
.log-create .action-text { color: #e67e22; }
.log-comment { background-color: #f4fbf7; }
.log-comment .action-text { color: #27ae60; }
</style>