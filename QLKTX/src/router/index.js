import { createRouter, createWebHistory } from 'vue-router'

const ADMIN_ROLES = ['Admin', 'Staff']
const ADMIN_ONLY_ROLES = ['Admin']
const STUDENT_ROLES = ['Student']

const normalizeRole = (role) => {
  const normalized = String(role || '').toLowerCase()

  if (normalized === 'student' || normalized === 'sinhvien') return 'Student'
  if (normalized === 'staff' || normalized === 'nhanvien') return 'Staff'
  return 'Admin'
}

const defaultHomeForRole = (role = localStorage.getItem('user_role')) => {
  return normalizeRole(role) === 'Student'
    ? '/student/portal'
    : '/student-service/dashboard'
}

const routes = [
  {
    path: '/login',
    name: 'Login',
    component: () => import('../modules/auth/views/LoginView.vue'),
  },
  {
    path: '/',
    meta: { requiresAuth: true },
    component: () => import('../components/MainLayout.vue'),
    children: [
      {
        path: '',
        redirect: () => defaultHomeForRole(),
      },
      {
        path: 'student/portal',
        name: 'StudentPortal',
        meta: { roles: STUDENT_ROLES },
        component: () =>
          import('../modules/contract-student/views/StudentPortalView.vue'),
      },
      {
        path: 'student-service/dashboard',
        name: 'StudentServiceDashboard',
        meta: { roles: ADMIN_ROLES },
        component: () =>
          import('../modules/contract-student/views/DashboardView.vue'),
      },
      {
        path: 'student-service/students',
        name: 'StudentManage',
        meta: { roles: ADMIN_ROLES },
        component: () =>
          import('../modules/contract-student/views/StudentManageView.vue'),
      },
      {
        path: 'student-service/registrations',
        name: 'RoomRegistrationCreate',
        meta: { roles: ADMIN_ROLES },
        component: () =>
          import('../modules/contract-student/views/RoomRegistrationView.vue'),
      },
      {
        path: 'student-service/registrations/approval',
        name: 'RoomRegistrationApproval',
        meta: { roles: ADMIN_ROLES },
        component: () =>
          import('../modules/contract-student/views/RoomRegistrationView.vue'),
      },
      {
        path: 'student-service/contracts',
        name: 'ContractList',
        meta: { roles: ADMIN_ROLES },
        component: () =>
          import('../modules/contract-student/views/ContractListView.vue'),
      },
      {
        path: 'student-service/contracts/manage',
        name: 'ContractManage',
        meta: { roles: ADMIN_ROLES },
        component: () =>
          import('../modules/contract-student/views/ContractManageView.vue'),
      },
      {
        path: 'facility/rooms',
        name: 'RoomDashboard',
        meta: { roles: ADMIN_ROLES },
        component: () =>
          import('../modules/facility/views/RoomDashboard.vue'),
      },
      {
        path: 'finance/incidents',
        name: 'IncidentManage',
        meta: { roles: ADMIN_ROLES },
        component: () =>
          import('../modules/finance/views/IncidentManage.vue'),
      },
      {
        path: 'auth/accounts',
        name: 'AccountManage',
        meta: { roles: ADMIN_ONLY_ROLES },
        component: () =>
          import('../modules/auth/views/AccountManageView.vue'),
      },
      {
        path: 'system/logs',
        name: 'SystemLogs',
        meta: { roles: ADMIN_ROLES },
        component: () =>
          import('../modules/billing/views/SystemLogsView.vue'),
      },
      {
        path: 'contract/register',
        redirect: '/student-service/registrations',
      },
      {
        path: 'contract/manage',
        redirect: '/student-service/contracts/manage',
      },
    ],
  },
  {
    path: '/:pathMatch(.*)*',
    redirect: () => defaultHomeForRole(),
  },
]

const router = createRouter({
  history: createWebHistory(),
  routes,
})

router.beforeEach((to) => {
  const token = localStorage.getItem('user_token')
  const role = normalizeRole(localStorage.getItem('user_role'))

  if (to.name === 'Login' && token) {
    return defaultHomeForRole(role)
  }

  if (to.matched.some((record) => record.meta.requiresAuth) && !token) {
    return '/login'
  }

  const allowedRoles = to.matched.flatMap((record) => record.meta.roles || [])

  if (allowedRoles.length > 0 && !allowedRoles.includes(role)) {
    return defaultHomeForRole(role)
  }

  return true
})

export default router
