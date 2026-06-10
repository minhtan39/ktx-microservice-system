<template>
  <main class="login-page">
    <section class="login-visual">
      <div class="brand-row">
        <span class="mdi mdi-home-city-outline"></span>
        <div>
          <strong>KTX Management</strong>
          <small>Smart Dormitory Microservices</small>
        </div>
      </div>

      <div class="visual-content">
        <span class="eyebrow">API Gateway Login</span>
        <h1>Đăng nhập hệ thống quản lý ký túc xá</h1>
        <p>
          Một frontend dùng chung cho Room & Building, Contract & Student,
          Billing/Maintenance và AuthService.
        </p>
      </div>

      <div class="service-strip">
        <span>Gateway</span>
        <span>RoomService</span>
        <span>ContractStudent</span>
        <span>Billing</span>
      </div>
    </section>

    <section class="login-panel">
      <div class="panel-card">
        <span class="page-kicker">Nhóm 3 - AuthService</span>
        <h2>Đăng nhập</h2>
        <p class="panel-copy">Admin/nhân viên vào màn quản trị, sinh viên vào cổng tự phục vụ.</p>

        <v-alert v-if="error" type="error" variant="tonal" class="mb-4">
          {{ error }}
        </v-alert>

        <v-form @submit.prevent="login">
          <v-text-field
            v-model="form.username"
            label="Tên đăng nhập"
            density="comfortable"
            prepend-inner-icon="mdi-account-outline"
            autocomplete="username"
          />

          <v-text-field
            v-model="form.password"
            label="Mật khẩu"
            density="comfortable"
            prepend-inner-icon="mdi-lock-outline"
            :type="showPassword ? 'text' : 'password'"
            :append-inner-icon="showPassword ? 'mdi-eye-off-outline' : 'mdi-eye-outline'"
            autocomplete="current-password"
            @click:append-inner="showPassword = !showPassword"
          />

          <v-btn
            block
            color="success"
            size="large"
            type="submit"
            :loading="loading"
          >
            Đăng nhập
          </v-btn>
        </v-form>

        <div class="demo-box">
          <strong>Tài khoản demo</strong>
          <button type="button" @click="useDemo('admin', 'admin123')">Quản trị: admin / admin123</button>
          <button type="button" @click="useDemo('nhanvien', 'staff123')">Nhân viên: nhanvien / staff123</button>
          <button type="button" @click="useDemo('sinhvien', 'sv123')">Sinh viên: sinhvien / sv123</button>
        </div>
      </div>
    </section>
  </main>
</template>

<script setup>
import { reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import api from '@/services/api'

const router = useRouter()
const loading = ref(false)
const error = ref('')
const showPassword = ref(false)

const form = reactive({
  username: 'admin',
  password: 'admin123',
})

const useDemo = (username, password) => {
  form.username = username
  form.password = password
  error.value = ''
}

const homePathByRole = (role) => {
  return String(role || '').toLowerCase() === 'student'
    ? '/student/portal'
    : '/student-service/dashboard'
}

const login = async () => {
  try {
    loading.value = true
    error.value = ''

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
    const homePath = payload.homePath || homePathByRole(role)

    localStorage.setItem('user_token', token)
    localStorage.setItem('user_role', role)
    localStorage.setItem('fullName', payload.fullName || payload.username || form.username)
    localStorage.setItem('username', payload.username || form.username)
    localStorage.setItem('user_home', homePath)

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
    error.value = 'Không đăng nhập được. Kiểm tra AuthService hoặc tài khoản demo.'
    console.error(err)
  } finally {
    loading.value = false
  }
}
</script>

<style scoped>
.login-page {
  display: grid;
  grid-template-columns: minmax(420px, 1fr) minmax(420px, 520px);
  min-height: 100vh;
  background: #f7f8fa;
}

.login-visual {
  position: relative;
  display: flex;
  flex-direction: column;
  justify-content: space-between;
  padding: 46px;
  overflow: hidden;
  background:
    linear-gradient(135deg, rgba(22, 155, 99, 0.92), rgba(5, 56, 37, 0.92)),
    url('/src/assets/hero.png') center/cover;
  color: #ffffff;
}

.brand-row {
  display: flex;
  align-items: center;
  gap: 14px;
  position: relative;
  z-index: 1;
}

.brand-row .mdi {
  color: #c8f4df;
  font-size: 42px;
}

.brand-row strong,
.brand-row small {
  display: block;
}

.brand-row strong {
  font-size: 23px;
  font-weight: 900;
}

.brand-row small {
  margin-top: 4px;
  color: #d7f8e7;
  font-size: 13px;
  font-weight: 800;
  letter-spacing: 0.08em;
  text-transform: uppercase;
}

.visual-content {
  position: relative;
  z-index: 1;
  max-width: 680px;
}

.eyebrow {
  display: block;
  margin-bottom: 12px;
  color: #c8f4df;
  font-size: 13px;
  font-weight: 900;
  letter-spacing: 0.08em;
  text-transform: uppercase;
}

.visual-content h1 {
  margin: 0;
  max-width: 720px;
  font-size: 46px;
  line-height: 1.08;
}

.visual-content p {
  max-width: 560px;
  margin: 18px 0 0;
  color: #e7fff1;
  font-size: 17px;
  line-height: 1.6;
}

.service-strip {
  position: relative;
  z-index: 1;
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
}

.service-strip span {
  display: inline-flex;
  align-items: center;
  min-height: 34px;
  padding: 0 12px;
  border: 1px solid rgba(255, 255, 255, 0.28);
  border-radius: 8px;
  background: rgba(255, 255, 255, 0.08);
  color: #ffffff;
  font-size: 13px;
  font-weight: 800;
}

.login-panel {
  display: grid;
  place-items: center;
  padding: 42px;
}

.panel-card {
  width: 100%;
  max-width: 430px;
  padding: 30px;
  border: 1px solid var(--line);
  border-radius: 8px;
  background: #ffffff;
}

.panel-card h2 {
  margin: 0;
  color: var(--ink);
  font-size: 30px;
}

.panel-copy {
  margin: 8px 0 24px;
  color: var(--muted);
  line-height: 1.5;
}

.demo-box {
  display: grid;
  gap: 8px;
  margin-top: 22px;
  padding-top: 18px;
  border-top: 1px solid var(--line);
}

.demo-box strong {
  color: var(--ink);
  font-size: 13px;
}

.demo-box button {
  min-height: 36px;
  border: 1px solid var(--line);
  border-radius: 8px;
  background: #f8fafc;
  color: #334155;
  cursor: pointer;
  font-weight: 800;
  text-align: left;
  padding: 0 12px;
}

.demo-box button:hover {
  border-color: rgba(22, 155, 99, 0.42);
  background: #f0fdf4;
}

@media (max-width: 920px) {
  .login-page {
    grid-template-columns: 1fr;
  }

  .login-visual {
    min-height: 420px;
    padding: 30px;
  }

  .visual-content h1 {
    font-size: 34px;
  }

  .login-panel {
    padding: 24px;
  }
}
</style>
