<template>
  <header class="main-header">
    <div class="logo">HỆ THỐNG KÝ TÚC XÁ</div>
    
    <div class="header-right">
      <div class="notification-wrapper" ref="dropdownRef">
        <div class="bell-icon" @click="toggleDropdown">
          🔔
          <span v-if="unreadCount > 0" class="badge">{{ unreadCount }}</span>
        </div>

        <div v-if="isOpen" class="notification-dropdown">
          <h3>Thông Báo Cá Nhân</h3>
          <hr />
          <div v-if="notifications.length === 0" class="empty-noti">
            Không có thông báo nào.
          </div>
          <ul v-else class="noti-list">
            <li 
              v-for="noti in notifications" 
              :key="noti.id" 
              :class="{ 'unread': !noti.isRead }"
              @click="handleMarkAsRead(noti)"
            >
              <h4>{{ noti.title }}</h4>
              <p>{{ noti.message }}</p>
              <small>{{ formatTime(noti.createdAt) }}</small>
            </li>
          </ul>
        </div>
      </div>

      <div class="user-info">
        Hi, <strong>{{ fullName }}</strong> ({{ role }})
      </div>
    </div>
  </header>
</template>

<script setup>
import { ref, onMounted, computed } from 'vue'
import axios from 'axios'

const isOpen = ref(false)
const notifications = ref([])
const fullName = ref(localStorage.getItem('fullName') || 'User')
const role = ref(localStorage.getItem('user_role') || 'Student')

// Tính toán số lượng thông báo chưa đọc
const unreadCount = computed(() => {
  return notifications.value.filter(n => !n.isRead).length
})

// Mở/Đóng danh sách thông báo
const toggleDropdown = () => {
  isOpen.value = !isOpen.value
  if (isOpen.value) {
    fetchNotifications()
  }
}

// Gọi API lấy thông báo từ Máy 3
const fetchNotifications = async () => {
  try {
    const token = localStorage.getItem('user_token')
    const response = await axios.get('http://192.168.61.57:5004/api/v1/billing/management/notifications', {
      headers: { Authorization: `Bearer ${token}` }
    })
    notifications.value = response.data
  } catch (error) {
    console.error('Lỗi lấy thông báo:', error)
  }
}

// Gọi API đánh dấu đã đọc khi click vào thông báo
const handleMarkAsRead = async (noti) => {
  if (noti.isRead) return // Nếu đọc rồi thì bỏ qua
  
  try {
    const token = localStorage.getItem('user_token')
    await axios.put(`http://192.168.61.57:5004/api/v1/billing/management/notifications/${noti.id}/read`, {}, {
      headers: { Authorization: `Bearer ${token}` }
    })
    noti.isRead = true // Cập nhật trạng thái lập tức trên giao diện
  } catch (error) {
    console.error('Lỗi cập nhật trạng thái thông báo:', error)
  }
}

// Hàm định dạng thời gian hiển thị ngắn gọn
const formatTime = (timeStr) => {
  const date = new Date(timeStr)
  return date.toLocaleString('vi-VN', { hour: '2-digit', minute: '2-digit', day: '2-digit', month: '2-digit' })
}

onMounted(() => {
  fetchNotifications() // Tự động tải số lượng thông báo khi vừa vào trang
})
</script>

<style scoped>
.main-header { display: flex; justify-content: space-between; align-items: center; padding: 15px 30px; background-color: #2c3e50; color: white; }
.header-right { display: flex; align-items: center; gap: 20px; }
.notification-wrapper { position: relative; cursor: pointer; }
.bell-icon { font-size: 22px; position: relative; }
.badge { position: absolute; top: -5px; right: -5px; background-color: red; color: white; border-radius: 50%; padding: 2px 6px; font-size: 11px; font-weight: bold; }
.notification-dropdown { position: absolute; top: 35px; right: 0; width: 320px; background: white; color: black; border: 1px solid #ccc; border-radius: 8px; box-shadow: 0 4px 12px rgba(0,0,0,0.15); z-index: 1000; padding: 10px; max-height: 400px; overflow-y: auto; }
.notification-dropdown h3 { margin: 5px 0; font-size: 16px; font-weight: bold; }
.noti-list { list-style: none; padding: 0; margin: 0; }
.noti-list li { padding: 10px; border-bottom: 1px solid #eee; transition: background 0.2s; }
.noti-list li:hover { background-color: #f5f5f5; }
.noti-list li.unread { background-color: #edf2fa; font-weight: bold; }
.noti-list h4 { margin: 0 0 5px 0; font-size: 14px; color: #333; }
.noti-list p { margin: 0 0 5px 0; font-size: 13px; color: #666; font-weight: normal; }
.noti-list small { color: #999; font-weight: normal; }
.empty-noti { text-align: center; padding: 20px; color: #999; }
</style>