<template>
  <div class="login-wrapper">
    <div class="login-container">
      <h2>Đăng Nhập Hệ Thống KTX</h2>

      <form @submit.prevent="handleLogin">
        <div class="form-group">
          <label>Tài khoản</label>
          <input
            v-model="username"
            type="text"
            required
            placeholder="Nhập tài khoản"
          />
        </div>

        <div class="form-group">
          <label>Mật khẩu</label>
          <input
            v-model="password"
            type="password"
            required
            placeholder="Nhập mật khẩu"
          />
        </div>

        <button type="submit" :disabled="loading" class="btn-login">
          {{ loading ? 'Đang xác thực...' : 'Đăng nhập' }}
        </button>

        <p v-if="errorMessage" class="error-msg">{{ errorMessage }}</p>
      </form>
    </div>
  </div>
</template>

<script setup>
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import api from '@/services/api'

const username = ref('')
const password = ref('')
const loading = ref(false)
const errorMessage = ref('')
const router = useRouter()

const handleLogin = async () => {
  loading.value = true
  errorMessage.value = ''

  try {
    const response = await api.post('/auth/login', {
      username: username.value,
      password: password.value,
    })

    const authData = response.data.data || response.data

    localStorage.setItem('user_token', authData.token)
    localStorage.setItem('user_role', authData.role)
    localStorage.setItem('fullName', authData.username)
    localStorage.setItem('username', authData.username)

    router.push('/student-service/dashboard')
  } catch (error) {
    console.error(error)

    if (error.response && error.response.status === 401) {
      errorMessage.value = 'Tài khoản hoặc mật khẩu không chính xác.'
    } else {
      errorMessage.value = 'Không kết nối được server. Kiểm tra Backend và API Gateway.'
    }
  } finally {
    loading.value = false
  }
}
</script>

<style scoped>
.login-wrapper {
  display: flex;
  justify-content: center;
  align-items: center;
  width: 100vw;
  height: 100vh;
  background-color: #f5f6fa;
  font-family: Arial, sans-serif;
}

.login-container {
  width: 100%;
  max-width: 420px;
  padding: 35px;
  background: white;
  border-radius: 8px;
  box-shadow: 0 4px 15px rgba(0, 0, 0, 0.08);
  text-align: center;
}

h2 {
  margin-bottom: 25px;
  color: #2c3e50;
  font-size: 22px;
  font-weight: bold;
}

.form-group {
  text-align: left;
  margin-bottom: 18px;
}

.form-group label {
  display: block;
  font-size: 14px;
  margin-bottom: 6px;
  color: #333;
  font-weight: 600;
}

.form-group input {
  width: 100%;
  padding: 10px;
  border: 1px solid #ccc;
  border-radius: 4px;
  font-size: 14px;
  box-sizing: border-box;
}

.form-group input:focus {
  border-color: #1abc9c;
  outline: none;
}

.btn-login {
  width: 100%;
  padding: 12px;
  background-color: #1abc9c;
  color: white;
  border: none;
  border-radius: 4px;
  font-size: 15px;
  font-weight: bold;
  cursor: pointer;
  margin-top: 10px;
}

.btn-login:hover {
  background-color: #16a085;
}

.btn-login:disabled {
  background-color: #bdc3c7;
  cursor: not-allowed;
}

.error-msg {
  color: #e74c3c;
  margin-top: 15px;
  font-size: 13px;
  font-weight: bold;
}
</style>
