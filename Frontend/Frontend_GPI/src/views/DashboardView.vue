<script setup>
import { onMounted, ref, computed } from 'vue'
import { Monitor, Users, ArrowRightLeft, MapPin, TrendingUp, TrendingDown, ArrowRight, Activity } from 'lucide-vue-next'
import { apiGet } from '../services/api'
import { useRouter } from 'vue-router'
import { Bar } from 'vue-chartjs'
import {
  Chart as ChartJS,
  CategoryScale,
  LinearScale,
  BarElement,
  Title,
  Tooltip,
  Legend,
} from 'chart.js'

ChartJS.register(CategoryScale, LinearScale, BarElement, Title, Tooltip, Legend)

const router = useRouter()
const loading = ref(true)
const stats   = ref({ materials: 0, employees: 0, movements: 0, localisations: 0 })
const recentMovements = ref([])
const allMovements = ref([])
const materials = ref([])
const localisations = ref([])

onMounted(async () => {
  try {
    const [matList, employees, movements, localList] = await Promise.all([
      apiGet('/Materials'),
      apiGet('/Employes'),
      apiGet('/v1/mouvements'),
      apiGet('/Localistations'),
    ])
    materials.value = Array.isArray(matList) ? matList : []
    localisations.value = Array.isArray(localList) ? localList : []
    stats.value = {
      materials: materials.value.length,
      employees: Array.isArray(employees) ? employees.length : 0,
      movements: Array.isArray(movements) ? movements.length : 0,
      localisations: localisations.value.length,
    }
    allMovements.value = Array.isArray(movements) ? movements : []
    recentMovements.value = Array.isArray(movements) ? movements.slice(-6).reverse() : []
  } finally {
    loading.value = false
  }
})

function materialName(id) {
  const found = materials.value.find(m => (m.id ?? m.Id) === id)
  return found ? (found.libelle ?? found.Libelle ?? `#${id}`) : `#${id}`
}

function localisationName(id) {
  const found = localisations.value.find(l => (l.id ?? l.Id) === id)
  return found ? (found.nom ?? found.Nom ?? `#${id}`) : `#${id}`
}

const statCards = computed(() => [
  {
    label: 'Matériels',
    value: stats.value.materials,
    icon: Monitor,
    color: 'text-cyan-400',
    bg: 'rgba(34,211,238,0.08)',
    border: 'rgba(34,211,238,0.18)',
    route: '/materiels',
  },
  {
    label: 'Employés',
    value: stats.value.employees,
    icon: Users,
    color: 'text-indigo-400',
    bg: 'rgba(99,102,241,0.08)',
    border: 'rgba(99,102,241,0.18)',
    route: '/employes',
  },
  {
    label: 'Mouvements',
    value: stats.value.movements,
    icon: ArrowRightLeft,
    color: 'text-violet-400',
    bg: 'rgba(167,139,250,0.08)',
    border: 'rgba(167,139,250,0.18)',
    route: '/mouvements',
  },
  {
    label: 'Localisations',
    value: stats.value.localisations,
    icon: MapPin,
    color: 'text-amber-400',
    bg: 'rgba(245,158,11,0.08)',
    border: 'rgba(245,158,11,0.18)',
    route: '/localisations',
  },
])

const typmovLabel = (t) => {
  const map = { 1: 'Affectation', 2: 'Retour', 3: 'Transfert', 4: 'Maintenance' }
  return map[t] ?? t ?? '—'
}
const typmovBadge = (t) => {
  const map = { 1: 'badge-cyan', 2: 'badge-amber', 3: 'badge-indigo', 4: 'badge-red' }
  return map[t] ?? 'badge-gray'
}

const monthNames = ['Jan', 'Fév', 'Mar', 'Avr', 'Mai', 'Juin', 'Juil', 'Août', 'Sep', 'Oct', 'Nov', 'Déc']

function getLastSixMonths() {
  const now = new Date()
  return Array.from({ length: 6 }, (_, idx) => {
    const date = new Date(now.getFullYear(), now.getMonth() - (5 - idx), 1)
    return { year: date.getFullYear(), month: date.getMonth() }
  })
}

function getMovementDate(item) {
  return item.dateMouvement ?? item.DateMouvement ?? item.date ?? null
}

const movementTrendLabels = computed(() =>
  getLastSixMonths().map(({ month }) => monthNames[month])
)

const movementTrendValues = computed(() => {
  const buckets = getLastSixMonths().reduce((acc, bucket) => {
    acc[`${bucket.year}-${bucket.month}`] = 0
    return acc
  }, {})

  allMovements.value.forEach((item) => {
    const raw = getMovementDate(item)
    if (!raw) return
    const date = new Date(raw)
    if (Number.isNaN(date.getTime())) return
    const key = `${date.getFullYear()}-${date.getMonth()}`
    if (key in buckets) buckets[key]++
  })

  return Object.values(buckets)
})

const sixMonthTotal = computed(() =>
  movementTrendValues.value.reduce((sum, v) => sum + v, 0)
)

const monthGrowth = computed(() => {
  const values = movementTrendValues.value
  if (values.length < 2) return 0
  const prev = values[values.length - 2]
  const last = values[values.length - 1]
  if (prev === 0) return last === 0 ? 0 : 100
  return Math.round(((last - prev) / prev) * 100)
})

const chartData = computed(() => ({
  labels: movementTrendLabels.value,
  datasets: [
    {
      label: 'Mouvements',
      data: movementTrendValues.value,
      backgroundColor: (ctx) => {
        const chart = ctx.chart
        const { ctx: context, chartArea } = chart
        if (!chartArea) return 'rgba(99,102,241,0.6)'
        const gradient = context.createLinearGradient(0, chartArea.bottom, 0, chartArea.top)
        gradient.addColorStop(0, 'rgba(99,102,241,0.15)')
        gradient.addColorStop(1, 'rgba(99,102,241,0.7)')
        return gradient
      },
      hoverBackgroundColor: 'rgba(129,140,248,0.85)',
      borderRadius: 8,
      borderSkipped: false,
      maxBarThickness: 40,
    },
  ],
}))

const chartOptions = computed(() => ({
  responsive: true,
  maintainAspectRatio: false,
  plugins: {
    legend: { display: false },
    tooltip: {
      backgroundColor: 'rgba(15,20,45,0.95)',
      titleColor: '#e2e8f0',
      bodyColor: '#94a3b8',
      borderColor: 'rgba(99,102,241,0.3)',
      borderWidth: 1,
      cornerRadius: 10,
      padding: 12,
      titleFont: { size: 13, weight: '600' },
      bodyFont: { size: 12 },
      callbacks: {
        label: (ctx) => `${ctx.parsed.y} mouvement${ctx.parsed.y !== 1 ? 's' : ''}`,
      },
    },
  },
  scales: {
    x: {
      grid: { display: false },
      ticks: { color: '#64748b', font: { size: 11, weight: '500' } },
      border: { display: false },
    },
    y: {
      beginAtZero: true,
      grid: { color: 'rgba(255,255,255,0.04)', drawBorder: false },
      ticks: {
        color: '#475569',
        font: { size: 11 },
        stepSize: 1,
        precision: 0,
      },
      border: { display: false },
    },
  },
  interaction: {
    intersect: false,
    mode: 'index',
  },
  animation: {
    duration: 800,
    easing: 'easeOutQuart',
  },
}))

const statusCards = computed(() => [
  { label: 'Parc total', value: stats.value.materials, icon: Monitor, bg: 'bg-cyan-500/10', text: 'text-cyan-300', route: '/materiels' },
  { label: 'Affectés', value: stats.value.movements, icon: ArrowRightLeft, bg: 'bg-violet-500/10', text: 'text-violet-300', route: '/mouvements' },
  { label: 'Localisations', value: stats.value.localisations, icon: MapPin, bg: 'bg-amber-500/10', text: 'text-amber-300', route: '/localisations' },
  { label: 'Employés', value: stats.value.employees, icon: Users, bg: 'bg-indigo-500/10', text: 'text-indigo-300', route: '/employes' },
])
</script>

<template>
  <div>

    <!-- Loading -->
    <div v-if="loading" class="flex items-center justify-center py-24">
      <div class="flex flex-col items-center gap-4">
        <div class="spinner" style="width:36px;height:36px;" />
        <p class="text-sm text-slate-500">Chargement des données…</p>
      </div>
    </div>

    <!-- Error -->
    <div
      v-else-if="error"
      class="glass p-5 text-sm text-rose-400 flex items-center gap-3"
      style="border-color:rgba(239,68,68,0.2);"
    >
      <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
        <circle cx="12" cy="12" r="10"/><line x1="12" y1="8" x2="12" y2="12"/><line x1="12" y1="16" x2="12.01" y2="16"/>
      </svg>
      {{ error }}
    </div>

    <template v-else>
      <!-- Metric Cards -->
      <div class="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-4 gap-4 mb-6">
        <div
          v-for="card in statusCards"
          :key="card.label"
          class="glass p-5 group hover:-translate-y-0.5 transition-all duration-200"
          @click="router.push(card.route || '/dashboard')"
          style="cursor:pointer;"
        >
          <div class="flex items-center justify-between mb-4">
            <div>
              <p class="text-xs uppercase tracking-[0.24em] text-slate-500">{{ card.label }}</p>
              <div class="mt-3 text-3xl font-semibold text-slate-100 stat-value">{{ card.value }}</div>
            </div>
            <div :class="`rounded-2xl p-3 ${card.bg}`">
              <component :is="card.icon" :size="18" :class="card.text" />
            </div>
          </div>
          <div class="flex items-center gap-2 text-xs text-slate-500">
            <span
              v-if="monthGrowth !== 0"
              class="inline-flex items-center gap-1 px-2 py-1 rounded-full"
              :class="monthGrowth > 0 ? 'bg-emerald-500/10 text-emerald-400' : 'bg-rose-500/10 text-rose-400'"
            >
              <component :is="monthGrowth > 0 ? TrendingUp : TrendingDown" :size="10" />
              {{ monthGrowth > 0 ? '+' : '' }}{{ monthGrowth }}%
            </span>
            <span>Ce mois</span>
          </div>
        </div>
      </div>

      <div class="grid gap-5 xl:grid-cols-[1.45fr_0.95fr]">
        <!-- Chart Section -->
        <div class="glass overflow-hidden p-5">
          <div class="flex items-center justify-between gap-4 mb-5">
            <div>
              <div class="text-xs uppercase tracking-[0.24em] text-slate-500">Évolution du parc</div>
              <h2 class="mt-2 text-xl font-semibold text-slate-100">Derniers 6 mois</h2>
            </div>
            <button class="btn btn-ghost btn-sm" @click="router.push('/mouvements')">
              Voir tout <ArrowRight :size="12" />
            </button>
          </div>

          <!-- Chart.js Bar Chart -->
          <div class="relative" style="height: 260px;">
            <Bar :data="chartData" :options="chartOptions" />
          </div>

          <div class="mt-6 grid gap-3 sm:grid-cols-2">
            <div class="rounded-3xl bg-slate-950/70 p-4 border border-white/5">
              <div class="text-xs uppercase tracking-[0.24em] text-slate-500">Mouvements ce mois</div>
              <div class="mt-3 text-2xl font-semibold text-cyan-300">{{ movementTrendValues[movementTrendValues.length - 1] }}</div>
              <div class="mt-2 text-sm text-slate-400">
                Variation
                <span :class="monthGrowth >= 0 ? 'text-emerald-400' : 'text-rose-400'">
                  {{ monthGrowth > 0 ? '+' : '' }}{{ monthGrowth }}%
                </span>
              </div>
            </div>
            <div class="rounded-3xl bg-slate-950/70 p-4 border border-white/5">
              <div class="text-xs uppercase tracking-[0.24em] text-slate-500">Mouvements totaux</div>
              <div class="mt-3 text-2xl font-semibold text-violet-300">{{ sixMonthTotal }}</div>
              <div class="mt-2 text-sm text-slate-400">Sur 6 mois</div>
            </div>
          </div>
        </div>

        <div class="flex flex-col gap-5">
          <!-- Quick Access -->
          <div class="glass p-5">
            <div class="flex items-center gap-2 mb-4">
              <TrendingUp :size="15" class="text-indigo-400" />
              <span class="text-sm font-semibold text-slate-200">Accès rapide</span>
            </div>
            <div class="grid gap-3">
              <button
                v-for="card in statCards"
                :key="card.label"
                class="w-full flex items-center gap-3 rounded-2xl border border-white/5 bg-slate-950/60 px-4 py-3 text-sm text-slate-300 transition hover:bg-slate-900"
                @click="router.push(card.route)"
              >
                <component :is="card.icon" :size="16" :class="card.color" />
                {{ card.label }}
                <ArrowRight class="ml-auto text-slate-500" :size="12" />
              </button>
            </div>
          </div>

          <!-- Recent Movements -->
          <div class="glass p-5">
            <div class="flex items-center gap-2 mb-4">
              <Activity :size="15" class="text-indigo-400" />
              <span class="text-sm font-semibold text-slate-200">Mouvements récents</span>
            </div>
            <div v-if="recentMovements.length === 0" class="text-sm text-slate-400">Aucun mouvement enregistré.</div>
            <table v-else class="data-table">
              <thead>
                <tr>
                  <th>Type</th>
                  <th>Matériel</th>
                  <th>Localisation</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="(m, i) in recentMovements" :key="i">
                  <td><span :class="`badge ${typmovBadge(m.typmov)}`">{{ typmovLabel(m.typmov) }}</span></td>
                  <td class="text-slate-300 text-sm">{{ materialName(m.materielId) }}</td>
                  <td class="text-slate-300 text-sm">{{ localisationName(m.localisationId) }}</td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      </div>
    </template>
  </div>
</template>
