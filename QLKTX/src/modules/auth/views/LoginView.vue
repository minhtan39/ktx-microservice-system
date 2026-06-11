<template>
  <main class="login-page">
    <section class="login-experience">
      <div class="brand-row">
        <span class="mdi mdi-home-city-outline"></span>
        <div>
          <strong>DormManager</strong>
          <small>Residence Portal</small>
        </div>
      </div>

      <div class="visual-content">
        <span class="eyebrow">Cổng nội trú sinh viên</span>
        <h1>Một chỗ ở mới, một nhịp sống mới.</h1>
        <p>Theo dõi phòng ở, đăng ký nội trú và hợp đồng trong một không gian gọn gàng, dễ dùng.</p>
      </div>

      <div class="campus-card" aria-hidden="true">
        <div class="campus-sky"></div>
        <div class="campus-building">
          <span v-for="index in 18" :key="index"></span>
        </div>
        <div class="campus-ground">
          <b></b>
          <i></i>
        </div>
      </div>

      <div class="life-strip">
        <span><b>24/7</b> Theo dõi hồ sơ</span>
        <span><b>01</b> Cổng đăng ký</span>
        <span><b>KTX</b> Sống tiện nghi</span>
      </div>
    </section>

    <section class="login-panel">
      <div class="panel-card">
        <span class="page-kicker">DormManager</span>
        <h2>Chào mừng bạn quay lại</h2>
        <p class="panel-copy">Sinh viên dùng mã sinh viên để đăng nhập. Admin và nhân viên dùng tài khoản được cấp.</p>

        <v-alert v-if="error" type="error" variant="tonal" class="mb-4">
          {{ error }}
        </v-alert>

        <v-form @submit.prevent="login">
          <v-text-field
            v-model="form.username"
            label="Mã sinh viên hoặc tài khoản"
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
            Vào hệ thống
          </v-btn>
        </v-form>

        <div class="login-note">
          <span class="mdi mdi-shield-check-outline"></span>
          <p>Tài khoản sinh viên được tạo từ hồ sơ do nhà trường quản lý.</p>
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
  username: '',
  password: '',
})

const homePathByRole = (role) => String(role || '').toLowerCase() === 'student'
  ? '/student/portal'
  : '/student-service/dashboard'

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
    error.value = 'Không đăng nhập được. Kiểm tra tên đăng nhập, mật khẩu hoặc AuthService.'
    console.error(err)
  } finally {
    loading.value = false
  }
}
</script>

<style scoped>
.login-page {
  display: grid;
  grid-template-columns: minmax(0, 1.2fr) minmax(420px, 520px);
  min-height: 100vh;
  background:
    radial-gradient(circle at 15% 15%, rgba(22, 155, 99, 0.10), transparent 28%),
    linear-gradient(135deg, #f7fbf8 0%, #eef7f1 42%, #fff8ec 100%);
}

.login-experience {
  position: relative;
  display: flex;
  flex-direction: column;
  justify-content: space-between;
  min-height: 100vh;
  padding: 48px;
  overflow: hidden;
  color: #12352d;
}

.brand-row {
  display: flex;
  align-items: center;
  gap: 14px;
  position: relative;
  z-index: 1;
}

.brand-row .mdi {
  color: #169b63;
  font-size: 42px;
}

.brand-row strong,
.brand-row small {
  display: block;
}

.brand-row strong {
  color: #0b3b31;
  font-size: 24px;
  font-weight: 900;
}

.brand-row small {
  margin-top: 4px;
  color: #5b756e;
  font-size: 13px;
  font-weight: 800;
  letter-spacing: 0.04em;
  text-transform: uppercase;
}

.visual-content {
  position: relative;
  z-index: 2;
  max-width: 620px;
}

.eyebrow {
  display: block;
  margin-bottom: 12px;
  color: #0f7f51;
  font-size: 13px;
  font-weight: 900;
  letter-spacing: 0.06em;
  text-transform: uppercase;
}

.visual-content h1 {
  margin: 0;
  max-width: 660px;
  color: #0b2f28;
  font-size: 58px;
  line-height: 1.03;
}

.visual-content p {
  max-width: 520px;
  margin: 18px 0 0;
  color: #4f665f;
  font-size: 18px;
  line-height: 1.6;
}

.campus-card {
  position: relative;
  z-index: 1;
  align-self: flex-end;
  width: min(520px, 70%);
  min-height: 320px;
  margin: 28px 8% 18px 0;
  border: 1px solid rgba(12, 57, 47, 0.12);
  border-radius: 8px;
  overflow: hidden;
  background: #dff4e9;
  box-shadow: 0 28px 70px rgba(17, 24, 39, 0.12);
}

.campus-sky {
  position: absolute;
  inset: 0;
  background:
    linear-gradient(180deg, rgba(255, 255, 255, 0.72), rgba(255, 255, 255, 0)),
    linear-gradient(135deg, #c8f4df, #fff6df);
}

.campus-building {
  position: absolute;
  left: 13%;
  right: 13%;
  bottom: 58px;
  display: grid;
  grid-template-columns: repeat(6, 1fr);
  gap: 14px;
  min-height: 188px;
  padding: 26px 28px;
  border-radius: 8px 8px 0 0;
  background: linear-gradient(135deg, #0f513f, #0d3f35);
}

.campus-building::before {
  content: '';
  position: absolute;
  left: 12%;
  right: 12%;
  top: -32px;
  height: 48px;
  border-radius: 8px 8px 0 0;
  background: #174e42;
}

.campus-building span {
  display: inline-flex;
  min-height: 30px;
  border-radius: 4px;
  background: rgba(255, 244, 194, 0.9);
  box-shadow: inset 0 0 0 1px rgba(255, 255, 255, 0.38);
}

.campus-ground {
  position: absolute;
  left: 0;
  right: 0;
  bottom: 0;
  height: 92px;
  background: linear-gradient(180deg, #bfe7cf, #8fc9ad);
}

.campus-ground b,
.campus-ground i {
  position: absolute;
  display: block;
  border-radius: 999px;
  background: rgba(255, 255, 255, 0.72);
}

.campus-ground b {
  left: 12%;
  right: 12%;
  top: 28px;
  height: 10px;
}

.campus-ground i {
  left: 36%;
  right: 36%;
  bottom: 18px;
  height: 12px;
}

.life-strip {
  position: relative;
  z-index: 2;
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 12px;
  max-width: 680px;
}

.life-strip span {
  display: grid;
  gap: 5px;
  min-height: 74px;
  padding: 15px 16px;
  border: 1px solid rgba(12, 57, 47, 0.10);
  border-radius: 8px;
  background: rgba(255, 255, 255, 0.74);
  color: #526962;
  font-size: 13px;
  font-weight: 700;
  backdrop-filter: blur(12px);
}

.life-strip b {
  color: #0f513f;
  font-size: 20px;
  line-height: 1;
}

.login-panel {
  display: grid;
  place-items: center;
  padding: 42px;
}

.panel-card {
  width: 100%;
  max-width: 430px;
  padding: 34px;
  border: 1px solid rgba(15, 81, 63, 0.10);
  border-radius: 8px;
  background: #ffffff;
  box-shadow: 0 22px 60px rgba(17, 24, 39, 0.08);
}

.panel-card h2 {
  margin: 0;
  color: #0b2f28;
  font-size: 32px;
  line-height: 1.15;
}

.panel-copy {
  margin: 8px 0 24px;
  color: var(--muted);
  line-height: 1.5;
}

.login-note {
  display: grid;
  grid-template-columns: 30px minmax(0, 1fr);
  gap: 10px;
  margin-top: 18px;
  padding: 14px;
  border-radius: 8px;
  background: #f2fbf6;
  color: #45635b;
}

.login-note .mdi {
  color: #169b63;
  font-size: 22px;
}

.login-note p {
  margin: 0;
  font-size: 13px;
  line-height: 1.45;
}

@media (max-width: 920px) {
  .login-page {
    grid-template-columns: 1fr;
  }

  .login-experience {
    min-height: auto;
    padding: 30px;
  }

  .visual-content h1 {
    font-size: 38px;
  }

  .campus-card {
    align-self: stretch;
    width: 100%;
    min-height: 240px;
    margin: 24px 0 0;
  }

  .life-strip {
    grid-template-columns: 1fr;
    margin-top: 18px;
  }

  .login-panel {
    padding: 24px;
  }
}
</style>
