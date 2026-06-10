import { createRouter, createWebHistory } from 'vue-router'

const routes = [
  {
    path: '/login',
    redirect: '/student-service/dashboard',
  },
  {
    path: '/',
    component: () => import('../components/MainLayout.vue'),
    children: [
      {
        path: '',
        redirect: '/student-service/dashboard',
      },
      {
        path: 'student-service/dashboard',
        name: 'StudentServiceDashboard',
        component: () =>
          import('../modules/contract-student/views/DashboardView.vue'),
      },
      {
        path: 'student-service/students',
        name: 'StudentManage',
        component: () =>
          import('../modules/contract-student/views/StudentManageView.vue'),
      },
      {
        path: 'student-service/registrations',
        name: 'RoomRegistrationCreate',
        component: () =>
          import('../modules/contract-student/views/RoomRegistrationView.vue'),
      },
      {
        path: 'student-service/registrations/approval',
        name: 'RoomRegistrationApproval',
        component: () =>
          import('../modules/contract-student/views/RoomRegistrationView.vue'),
      },
      {
        path: 'student-service/contracts',
        name: 'ContractList',
        component: () =>
          import('../modules/contract-student/views/ContractListView.vue'),
      },
      {
        path: 'student-service/contracts/manage',
        name: 'ContractManage',
        component: () =>
          import('../modules/contract-student/views/ContractManageView.vue'),
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
    redirect: '/student-service/dashboard',
  },
]

const router = createRouter({
  history: createWebHistory(),
  routes,
})

export default router
