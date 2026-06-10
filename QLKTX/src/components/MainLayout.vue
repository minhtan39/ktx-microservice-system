<template>
  <div class="app-shell">
    <aside class="sidebar">
      <div class="brand">
        <div class="brand-mark">
          <span class="mdi mdi-home-city-outline"></span>
        </div>
        <div>
          <div class="brand-name">KTX</div>
          <span class="brand-subtitle">Contract & Student</span>
        </div>
      </div>

      <nav class="nav">
        <p class="nav-section">Nghiệp vụ nhóm N2</p>

        <router-link to="/student-service/dashboard" class="nav-item" active-class="active">
          <span class="mdi mdi-view-dashboard-outline"></span>
          <span>Tổng quan N2</span>
        </router-link>

        <router-link to="/student-service/students" class="nav-item" active-class="active">
          <span class="mdi mdi-account-school-outline"></span>
          <span>Hồ sơ sinh viên</span>
        </router-link>

        <router-link to="/student-service/registrations" class="nav-item" active-class="active">
          <span class="mdi mdi-clipboard-check-outline"></span>
          <span>Đăng ký & duyệt phòng</span>
        </router-link>

        <router-link to="/student-service/contracts" class="nav-item" active-class="active">
          <span class="mdi mdi-file-document-outline"></span>
          <span>Danh sách hợp đồng</span>
        </router-link>

        <router-link to="/student-service/contracts/manage" class="nav-item" active-class="active">
          <span class="mdi mdi-file-cog-outline"></span>
          <span>Quản lý hợp đồng</span>
        </router-link>
      </nav>

      <div class="scope-box">
        <span class="mdi mdi-shield-check-outline"></span>
        <div>
          <strong>Phạm vi N2</strong>
          <p>Sinh viên, đăng ký phòng, duyệt xếp phòng và hợp đồng.</p>
        </div>
      </div>

      <div class="account-box">
        <div class="account-avatar">{{ userInitial }}</div>
        <div class="account-meta">
          <strong>{{ fullName }}</strong>
          <span>{{ userRole }}</span>
        </div>
      </div>
    </aside>

    <main class="main-panel">
      <header class="topbar">
        <div>
          <span class="service-label">Contract & Student Service</span>
          <h1>KTX MANAGEMENT - QUẢN LÝ KÝ TÚC XÁ THÔNG MINH</h1>
          <p>{{ pageTitle }}</p>
        </div>

        <div class="user-chip">
          <div>
            <strong>{{ fullName }}</strong>
            <span>{{ userRole }}</span>
          </div>
          <div class="user-avatar">{{ userInitial }}</div>
        </div>
      </header>

      <div class="page-body">
        <router-view />
      </div>
    </main>
  </div>
</template>

<script setup>
import { computed, ref } from 'vue'
import { useRoute } from 'vue-router'

const route = useRoute()
const userRole = ref(localStorage.getItem('user_role') || 'N2 Admin')
const fullName = ref(localStorage.getItem('fullName') || 'demo_admin')

const titleByRoute = {
  StudentServiceDashboard: 'Tổng quan nghiệp vụ N2',
  StudentManage: 'Quản lý hồ sơ sinh viên',
  RoomRegistrationManage: 'Đăng ký phòng và duyệt xếp phòng',
  ContractList: 'Danh sách hợp đồng',
  ContractManage: 'Quản lý hợp đồng',
}

const pageTitle = computed(() =>
  titleByRoute[route.name] || 'Tổng quan nghiệp vụ N2')

const userInitial = computed(() => {
  return (fullName.value || 'U').trim().charAt(0).toUpperCase()
})
</script>

<style scoped>
.app-shell {
  display: grid;
  grid-template-columns: 300px minmax(0, 1fr);
  min-height: 100vh;
  background: var(--app-bg);
  color: var(--ink);
}

.sidebar {
  position: sticky;
  top: 0;
  display: flex;
  flex-direction: column;
  height: 100vh;
  padding: 28px 20px 20px;
  background: var(--sidebar);
  color: #f5f7f8;
}

.brand {
  display: flex;
  align-items: center;
  gap: 12px;
  min-height: 62px;
  margin-bottom: 24px;
}

.brand-mark {
  display: grid;
  place-items: center;
  width: 42px;
  height: 42px;
  color: var(--brand);
  font-size: 34px;
}

.brand-name {
  color: var(--brand);
  font-size: 28px;
  font-weight: 900;
  line-height: 1;
}

.brand-subtitle {
  display: block;
  margin-top: 5px;
  color: #9aa1a8;
  font-size: 12px;
  font-weight: 800;
  letter-spacing: 0.08em;
  text-transform: uppercase;
}

.nav {
  flex: 1;
  min-height: 0;
  overflow-y: auto;
  padding-right: 2px;
}

.nav-section {
  margin: 18px 10px 12px;
  color: #7b8288;
  font-size: 12px;
  font-weight: 900;
  letter-spacing: 0.08em;
  text-transform: uppercase;
}

.nav-item {
  display: grid;
  grid-template-columns: 26px minmax(0, 1fr);
  gap: 12px;
  align-items: center;
  min-height: 46px;
  width: 100%;
  margin: 6px 0;
  padding: 10px 13px;
  border: 1px solid transparent;
  border-radius: 8px;
  background: transparent;
  color: #f2f5f6;
  cursor: pointer;
  font-size: 15px;
  font-weight: 800;
  line-height: 1.25;
  text-align: left;
  text-decoration: none;
}

.nav-item .mdi {
  color: #b8bdc2;
  font-size: 21px;
}

.nav-item span:last-child {
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.nav-item:hover,
.nav-item.active {
  border-color: #2c3033;
  background: var(--sidebar-soft);
}

.nav-item.active .mdi,
.nav-item.active span:last-child {
  color: #ffffff;
}

.scope-box {
  display: grid;
  grid-template-columns: 32px minmax(0, 1fr);
  gap: 10px;
  margin: 16px 0;
  padding: 14px;
  border: 1px solid #24282c;
  border-radius: 8px;
  background: #111416;
}

.scope-box .mdi {
  color: var(--brand);
  font-size: 24px;
}

.scope-box strong {
  display: block;
  color: #ffffff;
  font-size: 13px;
}

.scope-box p {
  margin: 4px 0 0;
  color: #9aa1a8;
  font-size: 12px;
  line-height: 1.45;
}

.account-box {
  display: grid;
  grid-template-columns: 42px minmax(0, 1fr);
  gap: 12px;
  align-items: center;
  margin-top: 8px;
  padding-top: 18px;
  border-top: 1px solid #24282c;
}

.account-avatar,
.user-avatar {
  display: grid;
  place-items: center;
  width: 42px;
  height: 42px;
  border-radius: 50%;
  background: #f4f5f6;
  color: #111827;
  font-weight: 800;
}

.account-meta {
  min-width: 0;
}

.account-meta strong,
.account-meta span {
  display: block;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.account-meta strong {
  color: #ffffff;
  font-size: 14px;
}

.account-meta span {
  margin-top: 2px;
  color: #8e949a;
  font-size: 13px;
}

.main-panel {
  min-width: 0;
  min-height: 100vh;
  background: var(--app-bg);
}

.topbar {
  position: sticky;
  top: 0;
  z-index: 5;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 24px;
  min-height: 88px;
  padding: 18px 30px;
  border-bottom: 1px solid var(--line);
  background: rgba(255, 255, 255, 0.96);
  backdrop-filter: blur(12px);
}

.service-label {
  display: block;
  margin-bottom: 4px;
  color: var(--brand);
  font-size: 12px;
  font-weight: 900;
  letter-spacing: 0.08em;
  text-transform: uppercase;
}

.topbar h1 {
  margin: 0;
  color: var(--brand-dark);
  font-size: 20px;
  font-weight: 900;
  line-height: 1.25;
}

.topbar p {
  margin: 6px 0 0;
  color: #111827;
  font-size: 15px;
  font-weight: 700;
}

.user-chip {
  display: flex;
  align-items: center;
  gap: 12px;
  flex: 0 0 auto;
}

.user-chip strong,
.user-chip span {
  display: block;
  text-align: right;
}

.user-chip strong {
  color: var(--brand-dark);
  font-size: 15px;
}

.user-chip span {
  margin-top: 2px;
  color: #111827;
  font-size: 13px;
}

.page-body {
  padding: 28px 30px 48px;
}

@media (max-width: 1100px) {
  .app-shell {
    grid-template-columns: 270px minmax(0, 1fr);
  }

  .sidebar {
    padding: 24px 16px;
  }
}

@media (max-width: 860px) {
  .app-shell {
    grid-template-columns: 1fr;
  }

  .sidebar {
    position: static;
    height: auto;
  }

  .brand {
    margin-bottom: 12px;
  }

  .nav {
    display: flex;
    gap: 8px;
    overflow-x: auto;
    padding-bottom: 4px;
  }

  .nav-section,
  .scope-box,
  .account-box {
    display: none;
  }

  .nav-item {
    flex: 0 0 188px;
    margin: 0;
  }

  .topbar {
    position: static;
    align-items: flex-start;
    flex-direction: column;
    padding: 18px;
  }

  .user-chip {
    width: 100%;
    justify-content: flex-start;
  }

  .user-chip strong,
  .user-chip span {
    text-align: left;
  }

  .page-body {
    padding: 18px;
  }
}
</style>
