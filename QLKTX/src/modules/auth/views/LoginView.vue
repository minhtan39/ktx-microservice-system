<template>
  <main class="login-page">
    <router-link class="back-home" to="/">
      <span class="mdi mdi-arrow-left"></span>
      Trang giới thiệu
    </router-link>

    <section class="login-shell">
      <div class="login-brand">
        <span class="brand-mark mdi mdi-home-city-outline"></span>
        <div>
          <strong>DormManager</strong>
          <small>Smart Residence System</small>
        </div>
      </div>

      <v-card class="login-card">
        <div class="card-head">
          <span class="page-kicker">Đăng nhập hệ thống</span>
          <h1>Chào mừng quay lại</h1>
          <p>Sinh viên đăng nhập bằng MSSV. Admin và nhân viên dùng tài khoản do nhóm AuthService cấp.</p>
        </div>

        <v-alert v-if="error" type="error" variant="tonal" class="mb-4">
          {{ error }}
        </v-alert>

        <v-alert v-if="success" type="success" variant="tonal" class="mb-4">
          {{ success }}
        </v-alert>

        <v-snackbar
          v-model="toastVisible"
          :color="toastColor"
          location="top right"
          timeout="4500"
          multi-line
        >
          {{ toastText }}
          <template #actions>
            <v-btn variant="text" @click="clearToast">Đóng</v-btn>
          </template>
        </v-snackbar>

        <v-form class="login-form" @submit.prevent="login">
          <v-text-field
            v-model="form.username"
            label="Mã sinh viên hoặc tài khoản"
            density="comfortable"
            prepend-inner-icon="mdi-account-outline"
            autocomplete="username"
            variant="outlined"
          />

          <v-text-field
            v-model="form.password"
            label="Mật khẩu"
            density="comfortable"
            prepend-inner-icon="mdi-lock-outline"
            :type="showPassword ? 'text' : 'password'"
            :append-inner-icon="showPassword ? 'mdi-eye-off-outline' : 'mdi-eye-outline'"
            autocomplete="current-password"
            variant="outlined"
            @click:append-inner="showPassword = !showPassword"
          />

          <div class="login-meta">
            <router-link to="/forgot-password">Quên mật khẩu?</router-link>
            <span>Chưa có tài khoản? Liên hệ admin để tạo hồ sơ sinh viên.</span>
          </div>

          <v-btn
            block
            color="primary"
            size="large"
            type="submit"
            :loading="loading"
            prepend-icon="mdi-login"
          >
            Vào hệ thống
          </v-btn>
        </v-form>
      </v-card>

      <div class="login-foot">
        <span class="mdi mdi-shield-check-outline"></span>
        JWT được cấp bởi AuthService, các service còn lại chỉ xác thực token qua Gateway.
      </div>
    </section>
  </main>
</template>

<script setup>
import { computed, reactive, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import api from '@/services/api'
import { defaultHomeForRole } from '@/utils/auth'

const router = useRouter()
const route = useRoute()
const loading = ref(false)
const error = ref('')
const success = ref(route.query.passwordChanged === '1'
  ? 'Mật khẩu đã được thay đổi. Vui lòng đăng nhập lại.'
  : '')
const showPassword = ref(false)
const toastVisible = computed({
  get: () => Boolean(error.value || success.value),
  set: (visible) => {
    if (!visible) clearToast()
  },
})
const toastText = computed(() => error.value || success.value)
const toastColor = computed(() => error.value ? 'error' : 'success')

const clearToast = () => {
  error.value = ''
  success.value = ''
}

const form = reactive({
  username: '',
  password: '',
})

const login = async () => {
  try {
    loading.value = true
    error.value = ''
    success.value = ''

    const response = await api.post('/auth/login', {
      username: form.username,
      password: form.password,
    })

    const payload = response.data?.data || response.data || {}
    const token = payload.token || payload.accessToken || payload.jwt

    if (!token) {
      throw new Error('AuthService không trả token.')
    }

    const role = payload.role || 'User'
    const homePath = payload.homePath || defaultHomeForRole(role)

    localStorage.setItem('user_token', token)
    localStorage.setItem('user_role', role)
    localStorage.setItem('fullName', payload.fullName || payload.username || form.username)
    localStorage.setItem('username', payload.username || form.username)
    localStorage.setItem('user_home', homePath)
    localStorage.setItem('user_permissions', JSON.stringify(payload.permissions || []))
    localStorage.setItem('employee_code', payload.employeeCode || '')
    localStorage.setItem('employee_department', payload.department || '')
    localStorage.setItem('employee_area', payload.assignedArea || '')

    if (payload.studentId) {
      localStorage.setItem('student_id', String(payload.studentId))
    } else {
      localStorage.removeItem('student_id')
    }

    if (payload.studentCode) {
      localStorage.setItem('student_code', payload.studentCode)
    } else {
      localStorage.removeItem('student_code')
    }

    await router.push(homePath)
  } catch (err) {
    error.value = err.response?.data?.message ||
      'Không đăng nhập được. Kiểm tra tên đăng nhập, mật khẩu hoặc AuthService.'
    console.error(err)
  } finally {
    loading.value = false
  }
}
</script>

<style scoped>
.login-page {
  position: relative;
  display: grid;
  place-items: center;
  min-height: 100vh;
  padding: 40px 18px;
  background:
    linear-gradient(180deg, rgba(255, 255, 255, 0.76), rgba(255, 255, 255, 0)),
    #f5f5f5;
}

.back-home {
  position: fixed;
  top: 22px;
  left: 24px;
  display: inline-flex;
  align-items: center;
  gap: 8px;
  min-height: 36px;
  padding: 0 12px;
  border: 1px solid var(--line-strong);
  border-radius: 6px;
  background: #ffffff;
  color: rgba(0, 0, 0, 0.72);
  font-size: 14px;
  font-weight: 800;
  text-decoration: none;
}

.login-shell {
  display: grid;
  gap: 22px;
  width: min(100%, 460px);
}

.login-brand {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 12px;
}

.brand-mark {
  display: grid;
  place-items: center;
  width: 46px;
  height: 46px;
  border-radius: 8px;
  background: #1677ff;
  color: #ffffff;
  font-size: 28px;
}

.login-brand strong,
.login-brand small {
  display: block;
}

.login-brand strong {
  color: rgba(0, 0, 0, 0.88);
  font-family: var(--font-heading);
  font-size: 24px;
  line-height: 1;
}

.login-brand small {
  margin-top: 5px;
  color: rgba(0, 0, 0, 0.45);
  font-size: 12px;
  font-weight: 900;
  letter-spacing: 0;
  text-transform: uppercase;
}

.login-card {
  padding: 34px;
  background: #ffffff;
}

.card-head {
  margin-bottom: 24px;
  text-align: center;
}

.card-head h1 {
  margin: 0;
  color: rgba(0, 0, 0, 0.88);
  font-family: var(--font-heading);
  font-size: 30px;
  line-height: 1.2;
}

.card-head p {
  margin: 10px auto 0;
  max-width: 360px;
  color: rgba(0, 0, 0, 0.55);
  font-size: 14px;
  line-height: 1.55;
}

.login-form {
  display: grid;
  gap: 2px;
}

.login-meta {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  margin: -2px 0 16px;
  color: rgba(0, 0, 0, 0.45);
  font-size: 13px;
}

.login-meta a {
  flex: 0 0 auto;
  color: #1677ff;
  font-weight: 900;
  text-decoration: none;
}

.login-meta a:hover {
  text-decoration: underline;
}

.login-meta span {
  text-align: right;
}

.login-foot {
  display: flex;
  justify-content: center;
  gap: 8px;
  color: rgba(0, 0, 0, 0.45);
  font-size: 13px;
  line-height: 1.45;
  text-align: center;
}

.login-foot .mdi {
  color: #52c41a;
  font-size: 18px;
}

@media (max-width: 560px) {
  .login-page {
    align-items: start;
    padding-top: 82px;
  }

  .back-home {
    left: 16px;
    top: 16px;
  }

  .login-card {
    padding: 24px;
  }

  .login-meta {
    align-items: flex-start;
    flex-direction: column;
  }

  .login-meta span {
    text-align: left;
  }
}
</style>
