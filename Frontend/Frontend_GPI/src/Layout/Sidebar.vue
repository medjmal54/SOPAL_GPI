<script setup>
import { computed, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuth } from '../services/auth'
import {
  LayoutDashboard,
  Monitor,
  ArrowRightLeft,
  Users,
  Wrench,
  LogOut,
  Cpu,
  ChevronLeft,
  Package,
  ClipboardCheck,
  MapPin,
} from 'lucide-vue-next'

const route = useRoute()
const router = useRouter()
const { state, isAdmin, logout } = useAuth()
const collapsed = ref(false)
const toggleHovered = ref(false)

const menuItems = computed(() => {
  if (isAdmin()) {
    return [
      { path: '/dashboard', label: 'Tableau de bord', icon: LayoutDashboard, color: 'text-indigo-400' },
      { path: '/materiels', label: 'Parc Matériel', icon: Monitor, color: 'text-cyan-400' },
      { path: '/mouvements', label: 'Historique Flux', icon: ArrowRightLeft, color: 'text-violet-400' },
      { path: '/localisations', label: 'Localisations', icon: MapPin, color: 'text-amber-400' },
      { path: '/demandes', label: 'Demandes', icon: Wrench, color: 'text-amber-400' },
      { path: '/mes-interventions', label: 'Mes Interventions', icon: ClipboardCheck, color: 'text-indigo-400' },
      { path: '/employes', label: 'Personnel', icon: Users, color: 'text-emerald-400' },
    ]
  }
  return [
    { path: '/mes-materiels', label: 'Mes Matériels', icon: Package, color: 'text-cyan-400' },
  ]
})

const isActive = (path) => route.path === path

const userInitials = computed(() => {
  const p = state.prenom?.[0] ?? ''
  const n = state.nom?.[0] ?? ''
  return `${p}${n}`.toUpperCase() || '??'
})

const userRole = computed(() => state.role === 'Admin' ? 'Administrateur IT' : 'Utilisateur')

async function handleLogout() {
  await logout()
  router.push('/login')
}
</script>

<template>
  <div class="flex h-screen w-screen overflow-hidden" style="background:#080c1a;">

    <!-- SIDEBAR -->
    <aside
      class="flex flex-col flex-shrink-0 h-full transition-all duration-300 ease-in-out"
      :style="{ width: collapsed ? '72px' : '260px' }"
      style="background:rgba(12,16,35,0.95); border-right:1px solid rgba(255,255,255,0.06);"
    >
      <!-- Brand -->
      <div
        class="flex items-center gap-3 px-4 py-5 flex-shrink-0"
        style="border-bottom:1px solid rgba(255,255,255,0.05);"
      >
        <div
          class="flex items-center justify-center rounded-xl flex-shrink-0"
          style="width:38px;height:38px;background:rgba(99,102,241,0.15);border:1px solid rgba(99,102,241,0.3);box-shadow:0 0 20px rgba(99,102,241,0.2);"
        >
          <Cpu :size="20" class="text-indigo-400" />
        </div>
        <div v-show="!collapsed" class="overflow-hidden">
          <div class="font-bold text-sm tracking-widest text-slate-100 uppercase">SOPAL GPI</div>
          <div class="text-[10px] text-indigo-400/70 font-mono tracking-[0.2em] uppercase">Asset Manager</div>
        </div>
        <button
          @click="collapsed = !collapsed"
          @mouseenter="toggleHovered = true"
          @mouseleave="toggleHovered = false"
          class="ml-auto flex-shrink-0 p-2 rounded-lg transition-all duration-300 ease-out flex items-center justify-center relative"
          :class="toggleHovered
            ? 'bg-indigo-500/20 border-indigo-400/40'
            : 'bg-indigo-500/10 border-indigo-400/20 hover:bg-indigo-500/15'"
          :style="{
            border: '1px solid',
            boxShadow: toggleHovered ? '0 0 12px rgba(99,102,241,0.3)' : 'none'
          }"
        >
          <ChevronLeft
            :size="16"
            class="text-indigo-400 transition-all duration-300 ease-out"
            :class="collapsed ? 'rotate-180' : 'rotate-0'"
            :style="{ transformOrigin: 'center' }"
          />
        </button>
      </div>

      <!-- Navigation -->
      <nav class="flex-1 px-3 py-4 space-y-1 overflow-y-auto">
        <div v-show="!collapsed" class="px-2 pb-2 text-[10px] font-semibold text-slate-600 tracking-[0.15em] uppercase">
          Navigation
        </div>
        <router-link
          v-for="item in menuItems"
          :key="item.path"
          :to="item.path"
          class="flex items-center gap-3 px-3 py-2.5 rounded-xl font-medium text-sm transition-all duration-200 relative group no-underline"
          :class="isActive(item.path)
            ? 'text-slate-100'
            : 'text-slate-500 hover:text-slate-300 hover:bg-white/[0.04]'"
          :style="isActive(item.path)
            ? 'background:rgba(99,102,241,0.12);border:1px solid rgba(99,102,241,0.2);'
            : 'border:1px solid transparent;'"
          :title="collapsed ? item.label : ''"
        >
          <span v-if="isActive(item.path)" class="nav-active-bar" />
          <component
            :is="item.icon"
            :size="18"
            class="flex-shrink-0 transition-transform duration-200 group-hover:scale-110"
            :class="isActive(item.path) ? item.color : ''"
          />
          <span v-show="!collapsed" class="truncate">{{ item.label }}</span>
          <div
            v-if="collapsed"
            class="absolute left-full ml-3 px-2.5 py-1.5 rounded-lg text-xs font-medium text-slate-200 pointer-events-none opacity-0 group-hover:opacity-100 transition-opacity z-50 whitespace-nowrap"
            style="background:#1a2040;border:1px solid rgba(255,255,255,0.1);"
          >
            {{ item.label }}
          </div>
        </router-link>
      </nav>

      <!-- User card -->
      <div
        class="flex-shrink-0 p-3 flex items-center gap-3"
        style="border-top:1px solid rgba(255,255,255,0.05);"
      >
        <div
          class="flex-shrink-0 flex items-center justify-center rounded-full font-bold text-xs text-indigo-300"
          style="width:34px;height:34px;background:rgba(99,102,241,0.15);border:1px solid rgba(99,102,241,0.25);"
        >
          {{ userInitials }}
        </div>
        <div v-show="!collapsed" class="flex-1 min-w-0">
          <div class="text-sm font-semibold text-slate-200 truncate">{{ state.prenom }} {{ state.nom }}</div>
          <div class="text-[10px] font-mono text-slate-500 truncate">{{ userRole }}</div>
        </div>
        <button
          v-show="!collapsed"
          class="p-1.5 rounded-lg transition-colors btn-ghost flex-shrink-0"
          title="Déconnexion"
          @click="handleLogout"
        >
          <LogOut :size="15" class="text-slate-600 hover:text-rose-400 transition-colors" />
        </button>
      </div>
    </aside>

    <!-- MAIN CONTENT -->
    <main class="flex-1 flex flex-col h-full overflow-hidden">
      <div class="flex-1 overflow-y-auto p-6 dot-grid">
        <router-view v-slot="{ Component }">
          <transition name="page" mode="out-in">
            <component :is="Component" />
          </transition>
        </router-view>
      </div>
    </main>
  </div>
</template>
