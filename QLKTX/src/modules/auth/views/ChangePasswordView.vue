<template>
  <section class="change-page">
    <div class="page-head">
      <div>
        <span class="page-kicker">Bảo mật tài khoản</span>
        <h2>Đổi mật khẩu</h2>
        <p>Bạn cần nhập đúng mật khẩu hiện tại trước khi tạo mật khẩu mới.</p>
      </div>
      <span class="mdi mdi-shield-lock-outline head-icon"></span>
    </div>

    <v-alert v-if="error" type="error" variant="tonal" class="mb-4">
      {{ error }}
    </v-alert>

    <section class="password-panel">
      <v-form @submit.prevent="changePassword">
        <v-text-field
          v-model="form.currentPassword"
          label="Mật khẩu hiện tại"
          prepend-inner-icon="mdi-lock-outline"
          :type="showCurrentPassword ? 'text' : 'password'"
          :append-inner-icon="showCurrentPassword ? 'mdi-eye-off-outline' : 'mdi-eye-outline'"
          autocomplete="current-password"
          density="comfortable"
          @click:append-inner="showCurrentPassword = !showCurrentPassword"
        />

        <v-text-field
          v-model="form.newPassword"
          label="Mật khẩu mới"
          prepend-inner-icon="mdi-lock-plus-outline"
          :type="showNewPassword ? 'text' : 'password'"
          :append-inner-icon="showNewPassword ? 'mdi-eye-off-outline' : 'mdi-eye-outline'"
          autocomplete="new-password"
          density="comfortable"
          hint="Ít nhất 6 ký tự và khác mật khẩu hiện tại"
          persistent-hint
          @click:append-inner="showNewPassword = !showNewPassword"
        />

        <v-text-field
          v-model="form.confirmPassword"
          label="Nhập lại mật khẩu mới"
          prepend-inner-icon="mdi-lock-check-outline"
          :type="showNewPassword ? 'text' : 'password'"
          autocomplete="new-password"
          density="comfortable"
        />

        <div class="form-actions">
          <v-btn
            color="success"
            size="large"
            type="submit"
            prepend-icon="mdi-content-save-check-outline"
            :loading="saving"
          >
            Đổi mật khẩu
          </v-btn>
        </div>
      </v-form>
    </section>
  </section>
</template>

<script setup>
import { reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import api from '@/services/api'

const router = useRouter()
const saving = ref(false)
const error = ref('')
const showCurrentPassword = ref(false)
const showNewPassword = ref(false)
const form = reactive({
  currentPassword: '',
  newPassword: '',
  confirmPassword: '',
})

const clearSession = () => {
  const keys = [
    'user_token',
    'user_role',
    'fullName',
    'username',
    'user_home',
    'student_id',
    'student_code',
  ]

  keys.forEach((key) => localStorage.removeItem(key))
}

const changePassword = async () => {
  if (!form.currentPassword || !form.newPassword) {
    error.value = 'Vui lòng nhập đầy đủ mật khẩu hiện tại và mật khẩu mới.'
    return
  }

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

    await api.post('/auth/change-password', {
      currentPassword: form.currentPassword,
      newPassword: form.newPassword,
    })

    clearSession()
    await router.replace({ name: 'Login', query: { passwordChanged: '1' } })
  } catch (err) {
    error.value = err.response?.data?.message ||
      err.response?.data?.detail ||
      'Không đổi được mật khẩu. Vui lòng kiểm tra mật khẩu hiện tại.'
    console.error(err)
  } finally {
    saving.value = false
  }
}
</script>

<style scoped>
.change-page {
  max-width: 820px;
}

.page-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 24px;
  margin-bottom: 22px;
}

.page-head h2 {
  margin: 0;
  color: var(--ink);
  font-size: 30px;
}

.page-head p {
  margin: 8px 0 0;
  color: var(--muted);
}

.head-icon {
  display: grid;
  place-items: center;
  width: 58px;
  height: 58px;
  flex: 0 0 58px;
  border-radius: 8px;
  background: #e8f8f0;
  color: var(--brand-dark);
  font-size: 32px;
}

.password-panel {
  max-width: 620px;
  padding: 28px;
  border: 1px solid var(--line);
  border-radius: 8px;
  background: #ffffff;
}

.form-actions {
  display: flex;
  justify-content: flex-end;
  margin-top: 8px;
}

@media (max-width: 640px) {
  .page-head {
    align-items: flex-start;
  }

  .password-panel {
    padding: 22px 18px;
  }

  .form-actions .v-btn {
    width: 100%;
  }
}
</style>
