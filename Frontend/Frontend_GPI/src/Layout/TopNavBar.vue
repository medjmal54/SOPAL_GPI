<script setup>
import { computed, ref, watch, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuth } from '../services/auth'

const route = useRoute()
const router = useRouter()
const { state, isAdmin, logout } = useAuth()
const isLoginPage = computed(() => route.path === '/login')
const mobileMenuOpen = ref(false)
const theme = ref('dark')

function applyTheme(value) {
  theme.value = value
  document.documentElement.dataset.theme = value
  localStorage.setItem('sopal-theme', value)
}

function toggleTheme() {
  applyTheme(theme.value === 'dark' ? 'light' : 'dark')
}

onMounted(() => applyTheme(localStorage.getItem('sopal-theme') === 'light' ? 'light' : 'dark'))

const menuItems = computed(() => {
  if (isAdmin()) {
    return [
      { path: '/dashboard', label: 'Tableau de bord', icon: 'grid_view' },
      { path: '/materiels', label: 'Matériels', icon: 'settings_input_component' },
      { path: '/employes', label: 'Employés', icon: 'badge' },
      { path: '/mouvements', label: 'Mouvements', icon: 'swap_horiz' },
      { path: '/demandes', label: 'Demandes', icon: 'build' },
      { path: '/mes-interventions', label: 'Interventions', icon: 'handyman' },
    ]
  }
  return [
    { path: '/mes-materiels', label: 'Mes Matériels', icon: 'devices' },
    { path: '/demandes', label: 'Mes demandes', icon: 'build' },
  ]
})

const isActive = (path) => route.path === path || (path === '/materiels' && route.path.startsWith('/materiels/'))

watch(() => route.path, () => { mobileMenuOpen.value = false })

const userInitials = computed(() => {
  const p = state.prenom?.[0] ?? ''
  const n = state.nom?.[0] ?? ''
  return `${p}${n}`.toUpperCase() || '?'
})

async function handleLogout() {
  await logout()
  router.push('/login')
}
</script>

<template>
  <header v-if="!isLoginPage" class="app-topbar flex items-center w-full px-container-padding h-16 z-50 bg-surface border-b border-outline-variant shrink-0 fixed top-0 left-0 right-0">
    <div class="flex items-center gap-6 flex-1">
      <div class="flex items-center gap-compact-gap mr-4 shrink-0">
        <span class="brand-title font-display-lg font-bold text-primary">SOPAL GPI</span>
        <div class="h-6 w-px bg-outline-variant mx-2"></div>
      </div>
      <nav class="hidden lg:flex items-center gap-1 h-full">
        <router-link
          v-for="item in menuItems"
          :key="item.path"
          :to="item.path"
          class="flex items-center gap-2 px-3 py-1.5 transition-colors rounded-lg no-underline"
          :class="isActive(item.path)
            ? 'bg-secondary-container text-on-secondary-container font-bold'
            : 'text-on-surface-variant hover:bg-surface-container-highest'"
        >
          <span class="material-symbols-outlined text-[20px]">{{ item.icon }}</span>
          <span class="font-label-md">{{ item.label }}</span>
        </router-link>
      </nav>
    </div>
    <div class="flex items-center gap-stack-gap">
      <button @click="toggleTheme" class="theme-toggle p-2 rounded-lg hover:bg-surface-container-high transition-colors text-on-surface-variant" :title="theme === 'dark' ? 'Mode clair' : 'Mode sombre'" :aria-label="theme === 'dark' ? 'Activer le mode clair' : 'Activer le mode sombre'">
        <span class="material-symbols-outlined">{{ theme === 'dark' ? 'light_mode' : 'dark_mode' }}</span>
      </button>
      <button
        class="mobile-menu-button p-2 rounded-lg hover:bg-surface-container-high transition-colors text-on-surface-variant"
        :aria-expanded="mobileMenuOpen"
        aria-label="Ouvrir la navigation"
        title="Navigation"
        @click="mobileMenuOpen = !mobileMenuOpen"
      >
        <span class="material-symbols-outlined">{{ mobileMenuOpen ? 'close' : 'menu' }}</span>
      </button>
      <button @click="handleLogout" class="p-2 rounded-lg hover:bg-surface-container-high transition-colors text-on-surface-variant" title="Déconnexion">
        <span class="material-symbols-outlined">logout</span>
      </button>
      <div class="w-8 h-8 rounded-full bg-primary-container flex items-center justify-center border border-primary cursor-pointer overflow-hidden">
        <span class="font-label-sm text-on-primary-container font-bold">{{ userInitials }}</span>
      </div>
    </div>
    <nav v-if="mobileMenuOpen" class="mobile-navigation">
      <router-link
        v-for="item in menuItems"
        :key="item.path"
        :to="item.path"
        class="mobile-navigation__item"
        :class="isActive(item.path) ? 'is-active' : ''"
      >
        <span class="material-symbols-outlined text-[20px]">{{ item.icon }}</span>
        <span class="font-label-md">{{ item.label }}</span>
      </router-link>
    </nav>
  </header>
</template>
