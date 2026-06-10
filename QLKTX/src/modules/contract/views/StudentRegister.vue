<template>
  <div>
    <h2 class="text-h5 font-weight-bold mb-4 text-primary">Đăng ký ở ký túc xá online</h2>

    <v-card class="mx-auto" max-width="900">
      <v-stepper v-model="step" :items="['Thông tin', 'Nguyện vọng', 'Xác nhận']">
        
        <template v-slot:item.1>
          <v-card title="Xác nhận thông tin sinh viên" flat>
            <v-row class="mt-2">
              <v-col cols="12" sm="6">
                <v-text-field label="Mã số sinh viên" model-value="SV20260001" readonly variant="filled"></v-text-field>
              </v-col>
              <v-col cols="12" sm="6">
                <v-text-field label="Họ và Tên" model-value="Nguyễn Văn A" readonly variant="filled"></v-text-field>
              </v-col>
              <v-col cols="12" sm="6">
                <v-text-field label="Khoa / Khóa" model-value="Công nghệ thông tin - K22" readonly variant="filled"></v-text-field>
              </v-col>
              <v-col cols="12" sm="6">
                <v-text-field label="Số điện thoại" variant="outlined" density="comfortable"></v-text-field>
              </v-col>
            </v-row>
            <v-card-actions class="justify-end">
              <v-btn color="primary" @click="step = 2">Tiếp tục</v-btn>
            </v-card-actions>
          </v-card>
        </template>

        <template v-slot:item.2>
          <v-card title="Lựa chọn phòng ở" flat>
            <v-row class="mt-2">
              <v-col cols="12" sm="6">
                <v-select
                  v-model="registration.buildingType"
                  :items="['Khu nhà Nam', 'Khu nhà Nữ']"
                  label="Khu vực tòa nhà"
                  variant="outlined"
                ></v-select>
              </v-col>
              <v-col cols="12" sm="6">
                <v-select
                  v-model="registration.roomType"
                  :items="['Phòng 4 người (Điều hòa)', 'Phòng 6 người (Thường)']"
                  label="Loại phòng mong muốn"
                  variant="outlined"
                ></v-select>
              </v-col>
              <v-col cols="12">
                <v-select
                  v-model="registration.duration"
                  :items="['Học kỳ I (5 tháng)', 'Học kỳ II (5 tháng)', 'Cả năm học (10 tháng)']"
                  label="Thời hạn thuê"
                  variant="outlined"
                ></v-select>
              </v-col>
            </v-row>
            <v-card-actions class="justify-space-between">
              <v-btn variant="text" @click="step = 1">Quay lại</v-btn>
              <v-btn color="primary" @click="step = 3">Tiếp tục</v-btn>
            </v-card-actions>
          </v-card>
        </template>

        <template v-slot:item.3>
          <v-card title="Cam kết & Gửi đơn" flat>
            <div class="pa-4 bg-grey-lighten-4 rounded mb-4 text-body-2" style="max-height: 200px; overflow-y: auto;">
              <p class="font-weight-bold mb-2">NỘI QUY KÝ TÚC XÁ ĐẠI HỌC</p>
              <p>1. Không nấu ăn trong phòng đối với các phòng không được phép.</p>
              <p>2. Trở về ký túc xá trước 23:00 hàng ngày.</p>
              <p>3. Thanh toán tiền phòng và các chi phí dịch vụ đúng hạn định kỳ hàng tháng...</p>
            </div>
            
            <v-checkbox
              v-model="registration.agreed"
              label="Tôi xin cam kết thực hiện đúng mọi nội quy của Ban quản lý Ký túc xá đề ra."
              color="primary"
            ></v-checkbox>

            <v-card-actions class="justify-space-between">
              <v-btn variant="text" @click="step = 2">Quay lại</v-btn>
              <v-btn color="success" :disabled="!registration.agreed" @click="submitRegistration">
                Gửi đơn đăng ký
              </v-btn>
            </v-card-actions>
          </v-card>
        </template>

      </v-stepper>
    </v-card>

    <v-snackbar v-model="snackbar" color="success" timeout="3000">
      Đăng ký phòng thành công! Đang chờ Ban quản lý phê duyệt.
    </v-snackbar>
  </div>
</template>

<script setup>
import { ref, reactive } from 'vue'
import api from '@/services/api'

const step = ref(1)
const snackbar = ref(false)

const registration = reactive({
  buildingType: '',
  roomType: '',
  duration: '',
  agreed: false
})

const submitRegistration = async () => {
  try {
    const startDate = new Date()

    let endDate = new Date()

    if (registration.duration.includes('5 tháng')) {
      endDate.setMonth(endDate.getMonth() + 5)
    } else {
      endDate.setMonth(endDate.getMonth() + 10)
    }

    const payload = {
      studentId: 1,
      buildingName: registration.buildingType,
      roomType: registration.roomType,
      startDate: startDate.toISOString(),
      endDate: endDate.toISOString(),
      status: 'Pending'
    }

    await api.post('/RoomRegistration', payload)

    snackbar.value = true

    registration.buildingType = ''
    registration.roomType = ''
    registration.duration = ''
    registration.agreed = false

    step.value = 1
  } catch (error) {
    console.error(error)
    alert('Đăng ký phòng thất bại!')
  }
}
</script>