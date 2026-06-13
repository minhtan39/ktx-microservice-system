<template>
  <main class="reset-page">
    <section class="reset-shell">
      <router-link to="/login" class="brand-link">
        <span class="mdi mdi-home-city-outline"></span>
        <span>DormManager</span>
      </router-link>

      <div class="reset-content">
        <div class="form-icon">
          <span class="mdi mdi-lock-reset"></span>
        </div>
        <span class="page-kicker">Xác nhận từ email</span>
        <h1>Đặt lại mật khẩu</h1>
        <p>Tạo mật khẩu mới có ít nhất 6 ký tự cho tài khoản sinh viên.</p>

        <v-progress-linear v-if="validating" indeterminate color="success" class="mb-5" />

        <v-alert v-if="error" type="error" variant="tonal" class="mb-4">
          {{ error }}
        </v-alert>
        <v-alert v-if="success" type="success" variant="tonal" class="mb-4">
          {{ success }}
        </v-alert>

        <v-form v-if="tokenValid && !success" @submit.prevent="resetPassword">
          <v-text-field
            v-model="form.newPassword"
            label="Mật khẩu mới"
            prepend-inner-icon="mdi-lock-outline"
            :type="showPassword ? 'text' : 'password'"
            :append-inner-icon="showPassword ? 'mdi-eye-off-outline' : 'mdi-eye-outline'"
            autocomplete="new-password"
            density="comfortable"
            @click:append-inner="showPassword = !showPassword"
          />

          <v-text-field
            v-model="form.confirmPassword"
            label="Nhập lại mật khẩu mới"
            prepend-inner-icon="mdi-lock-check-outline"
            :type="showPassword ? 'text' : 'password'"
            autocomplete="new-password"
            density="comfortable"
          />

          <v-btn block color="success" size="large" type="submit" :loading="saving">
            Đặt lại mật khẩu
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
import { onMounted, reactive, ref } from 'vue'
import { useRoute } from 'vue-router'
import api from '@/services/api'

const route = useRoute()
const token = String(route.query.token || '')
const validating = ref(true)
const tokenValid = ref(false)
const saving = ref(false)
const showPassword = ref(false)
const error = ref('')
const success = ref('')
const form = reactive({
  newPassword: '',
  confirmPassword: '',
})

const validateToken = async () => {
  if (!token) {
    error.value = 'Liên kết đặt lại mật khẩu không hợp lệ.'
    validating.value = false
    return
  }

  try {
    await api.get('/auth/reset-password/validate', { params: { token } })
    tokenValid.value = true
  } catch (err) {
    error.value = err.response?.data?.message ||
      'Liên kết đặt lại mật khẩu không hợp lệ hoặc đã hết hạn.'
  } finally {
    validating.value = false
  }
}

const resetPassword = async () => {
  if (form.newPassword.length < 6) {
    error.value = 'Mật khẩu mới phải có ít nhất 6 ký tự.'
    return
  }

  if (form.newPassword !== form.confirmPassword) {
    error.value = 'Mật khẩu nhập lại chưa khớp.'
    return
  }

  try {
    saving.value = true
    error.value = ''

    const response = await api.post('/auth/reset-password', {
      token,
      newPassword: form.newPassword,
    })

    success.value = response.data?.message || 'Đặt lại mật khẩu thành công.'
    tokenValid.value = false
  } catch (err) {
    error.value = err.response?.data?.message ||
      'Không đặt lại được mật khẩu. Liên kết có thể đã hết hạn.'
  } finally {
    saving.value = false
  }
}

onMounted(validateToken)
</script>

<style scoped>
.reset-page {
  display: grid;
  min-height: 100vh;
  place-items: center;
  padding: 30px;
  background:
    linear-gradient(rgba(247, 248, 250, 0.94), rgba(247, 248, 250, 0.94)),
    url('/src/assets/hero.png') center/cover;
}

.reset-shell {
  width: min(100%, 540px);
  border: 1px solid var(--line);
  border-radius: 8px;
  background: #ffffff;
  box-shadow: var(--shadow-soft);
}

.brand-link {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 20px 26px;
  border-bottom: 1px solid var(--line);
  color: var(--brand-dark);
  font-size: 18px;
  font-weight: 900;
  text-decoration: none;
}

.brand-link .mdi {
  font-size: 29px;
}

.reset-content {
  padding: 30px;
}

.form-icon {
  display: grid;
  place-items: center;
  width: 50px;
  height: 50px;
  margin-bottom: 18px;
  border-radius: 8px;
  background: #e8f8f0;
  color: var(--brand-dark);
  font-size: 28px;
}

.reset-content h1 {
  margin: 0;
  color: var(--ink);
  font-size: 32px;
}

.reset-content > p {
  margin: 10px 0 24px;
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

@media (max-width: 560px) {
  .reset-page {
    padding: 16px;
  }

  .reset-content {
    padding: 24px 20px;
  }
}
</style>
