<template>
  <main class="recovery-page">
    <section class="recovery-visual">
      <router-link to="/login" class="brand-link">
        <span class="mdi mdi-home-city-outline"></span>
        <span>DormManager</span>
      </router-link>

      <div>
        <span class="page-kicker">Bảo mật tài khoản</span>
        <h1>Khôi phục quyền truy cập</h1>
        <p>Liên kết đặt lại mật khẩu sẽ được gửi đến email trong hồ sơ sinh viên.</p>
      </div>
    </section>

    <section class="recovery-panel">
      <div class="form-shell">
        <div class="form-icon">
          <span class="mdi mdi-email-lock-outline"></span>
        </div>
        <h2>Quên mật khẩu</h2>
        <p>Nhập mã sinh viên. Liên kết xác nhận có hiệu lực trong 30 phút và chỉ dùng được một lần.</p>

        <v-alert v-if="error" type="error" variant="tonal" class="mb-4">
          {{ error }}
        </v-alert>
        <v-alert v-if="success" type="success" variant="tonal" class="mb-4">
          {{ success }}
        </v-alert>

        <v-form @submit.prevent="submitRequest">
          <v-text-field
            v-model="studentCode"
            label="Mã sinh viên"
            prepend-inner-icon="mdi-account-school-outline"
            autocomplete="username"
            density="comfortable"
            :disabled="loading"
          />

          <v-btn
            block
            color="primary"
            size="large"
            type="submit"
            prepend-icon="mdi-email-arrow-right-outline"
            :loading="loading"
          >
            Gửi liên kết xác nhận
          </v-btn>
        </v-form>

        <router-link class="back-link" to="/login">
          <span class="mdi mdi-arrow-left"></span>
          Quay lại đăng nhập
        </router-link>
      </div>
    </section>
  </main>
</template>

<script setup>
import { ref } from 'vue'
import api from '@/services/api'

const studentCode = ref('')
const loading = ref(false)
const error = ref('')
const success = ref('')

const submitRequest = async () => {
  if (!studentCode.value.trim()) {
    error.value = 'Vui lòng nhập mã sinh viên.'
    return
  }

  try {
    loading.value = true
    error.value = ''
    success.value = ''

    const response = await api.post('/auth/forgot-password', {
      studentCode: studentCode.value.trim(),
    })

    success.value = response.data?.message ||
      'Nếu hồ sơ hợp lệ, hệ thống sẽ gửi liên kết đặt lại mật khẩu qua email.'
  } catch (err) {
    error.value = err.response?.data?.detail ||
      err.response?.data?.message ||
      'Không gửi được email. Vui lòng thử lại sau hoặc liên hệ quản trị viên.'
    console.error(err)
  } finally {
    loading.value = false
  }
}
</script>

<style scoped>
.recovery-page {
  display: grid;
  grid-template-columns: minmax(340px, 0.9fr) minmax(420px, 1.1fr);
  min-height: 100vh;
  background: var(--app-bg);
}

.recovery-visual {
  display: flex;
  flex-direction: column;
  justify-content: space-between;
  min-height: 100vh;
  padding: 44px;
  background:
    linear-gradient(135deg, rgba(26, 18, 13, 0.94), rgba(124, 45, 18, 0.86)),
    url('/src/assets/hero.png') center/cover;
  color: #ffffff;
}

.brand-link {
  display: inline-flex;
  align-items: center;
  gap: 10px;
  width: fit-content;
  color: #ffffff;
  font-size: 20px;
  font-weight: 900;
  text-decoration: none;
}

.brand-link .mdi {
  color: #ffb347;
  font-size: 34px;
}

.recovery-visual h1 {
  max-width: 520px;
  margin: 0;
  font-size: 44px;
  line-height: 1.08;
}

.recovery-visual p {
  max-width: 520px;
  margin: 18px 0 0;
  color: #ffe7d3;
  font-size: 17px;
  line-height: 1.6;
}

.recovery-visual .page-kicker {
  color: #ffcf9f;
}

.recovery-panel {
  display: grid;
  place-items: center;
  padding: 36px;
}

.form-shell {
  width: min(100%, 460px);
  padding: 32px;
  border: 1px solid var(--line);
  border-radius: 8px;
  background: #ffffff;
}

.form-icon {
  display: grid;
  place-items: center;
  width: 48px;
  height: 48px;
  margin-bottom: 18px;
  border-radius: 8px;
  background: #fff3e8;
  color: var(--brand);
  font-size: 26px;
}

.form-shell h2 {
  margin: 0;
  font-size: 30px;
}

.form-shell > p {
  margin: 9px 0 24px;
  color: var(--muted);
  line-height: 1.55;
}

.back-link {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 7px;
  margin-top: 20px;
  color: var(--brand-dark);
  font-weight: 800;
  text-decoration: none;
}

@media (max-width: 820px) {
  .recovery-page {
    grid-template-columns: 1fr;
  }

  .recovery-visual {
    min-height: 300px;
    padding: 28px;
  }

  .recovery-visual h1 {
    font-size: 34px;
  }

  .recovery-panel {
    padding: 22px;
  }
}
</style>
