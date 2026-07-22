import { createRouter, createWebHistory } from 'vue-router'
import { useAuth } from './services/auth'
import LoginView from './views/LoginView.vue'
import DashboardView from './views/DashboardView.vue'
import UserHomeView from './views/UserHomeView.vue'
import MaterielsView from './views/MaterielsView.vue'
import MaterialDetailView from './views/MaterialDetailView.vue'
import MouvementsView from './views/MouvementsView.vue'
import EmployesView from './views/EmployesView.vue'
import RequestsView from './views/RequestsView.vue'
import MyInterventionsView from './views/MyInterventionsView.vue'

const routes = [
  { path: '/login', component: LoginView, meta: { public: true } },
  { path: '/', redirect: '/dashboard' },
  { path: '/dashboard', component: DashboardView, meta: { adminOnly: true } },
  { path: '/mes-materiels', component: UserHomeView },
  { path: '/materiels', component: MaterielsView, meta: { adminOnly: true } },
  { path: '/materiels/:id', component: MaterialDetailView, meta: { adminOnly: true } },
  { path: '/mouvements', component: MouvementsView, meta: { adminOnly: true } },
  { path: '/employes', component: EmployesView, meta: { adminOnly: true } },
  { path: '/demandes', component: RequestsView },
  { path: '/mes-interventions', component: MyInterventionsView, meta: { adminOnly: true } },
]

const router = createRouter({
  history: createWebHistory(),
  routes,
  scrollBehavior: () => ({ top: 0 }),
})

router.beforeEach((to, from, next) => {
  const { isAuthenticated, isAdmin } = useAuth()

  if (to.meta.public) {
    if (isAuthenticated()) {
      return next(isAdmin() ? '/dashboard' : '/mes-materiels')
    }
    return next()
  }

  if (!isAuthenticated()) {
    return next('/login')
  }

  if (to.meta.adminOnly && !isAdmin()) {
    return next('/mes-materiels')
  }

  if (to.path === '/' || to.path === '/dashboard') {
    if (!isAdmin()) {
      return next('/mes-materiels')
    }
  }

  next()
})

export default router
