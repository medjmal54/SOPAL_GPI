<script setup>
import { computed, onMounted, ref, watch } from 'vue'
import { useRoute } from 'vue-router'
import { apiGet } from '../services/api'

const route = useRoute()
const loading = ref(true)
const error = ref(null)
const items = ref([])
const materials = ref([])
const localisations = ref([])
const query = ref('')
const viewMode = ref('table')

function materialName(id) {
  const found = materials.value.find(m => (m.id ?? m.Id) === id)
  return found ? (found.libelle ?? found.Libelle ?? `#${id}`) : `#${id}`
}

function localisationName(id) {
  const found = localisations.value.find(l => (l.id ?? l.Id) === id)
  return found ? (found.nom ?? found.Nom ?? `#${id}`) : `#${id}`
}

function displayValue(item, field) {
  if (field === 'materielId') return materialName(item[field])
  if (field === 'LocalisationId') return localisationName(item[field])
  return item[field]
}

const pageIcon = computed(() => {
  switch (route.path) {
    case '/materiels': return 'devices'
    case '/mouvements': return 'swap_horiz'
    case '/employes': return 'badge'
    case '/localisations': return 'location_on'
    default: return 'database'
  }
})

const config = computed(() => {
  switch (route.path) {
    case '/materiels':
      return { title: 'Parc Matériel', description: 'Consultez et gérez les actifs du parc.', fields: ['Libelle', 'Marque', 'Modele', 'NumSerie', 'Actif'] }
    case '/mouvements':
      return { title: 'Historique des Mouvements', description: 'Suivi des affectations, transferts et retours.', fields: ['DateMouvement', 'typmov', 'materielId', 'LocalisationId'] }
    case '/employes':
      return { title: 'Personnel', description: 'Fiche et suivi des collaborateurs.', fields: ['Nom', 'Prenom', 'Email', 'Service', 'Poste'] }
    case '/localisations':
      return { title: 'Localisations', description: 'Vue des sites et emplacements du parc.', fields: ['Nom'] }
    default:
      return { title: 'Données', description: '', fields: [] }
  }
})

const filteredItems = computed(() => {
  const list = items.value || []
  if (!query.value) return list
  const needle = query.value.toLowerCase()
  return list.filter((item) => JSON.stringify(item).toLowerCase().includes(needle))
})

async function loadData() {
  loading.value = true
  items.value = []

  try {
    if (route.path === '/mouvements') {
      const [mouvs, mats, locs] = await Promise.all([
        apiGet('/v1/mouvements'),
        apiGet('/Materials'),
        apiGet('/Localistations'),
      ])
      items.value = Array.isArray(mouvs) ? mouvs : []
      materials.value = Array.isArray(mats) ? mats : []
      localisations.value = Array.isArray(locs) ? locs : []
    } else {
      const endpoint = route.path === '/materiels'
        ? '/Materials'
        : route.path === '/employes'
          ? '/Employes'
          : route.path === '/localisations'
            ? '/Localistations'
            : '/Materials'

      const data = await apiGet(endpoint)
      items.value = Array.isArray(data) ? data : []
    }
  } catch (err) {
    error.value = 'Erreur de chargement des données.'
    console.error(err)
  } finally {
    loading.value = false
  }
}

watch(() => route.path, loadData, { immediate: true })

onMounted(loadData)
</script>

<template>
  <div>
    <div class="flex justify-between items-center mb-6">
      <div>
        <h2 class="font-headline-md text-headline-md font-bold text-on-surface">{{ config.title }}</h2>
        <p class="font-label-sm text-label-sm text-on-surface-variant uppercase tracking-wider">{{ config.description }}</p>
      </div>
    </div>

    <div class="flex flex-col gap-4 md:flex-row md:items-center md:justify-between mb-6">
      <div class="relative w-full md:w-72">
        <span class="material-symbols-outlined absolute left-3 top-1/2 -translate-y-1/2 text-on-surface-variant text-[16px]">search</span>
        <input v-model="query" placeholder="Rechercher…" class="w-full bg-surface border border-outline-variant rounded-lg pl-9 pr-3 py-2 font-label-sm text-on-surface focus:border-primary focus:ring-1 focus:ring-primary outline-none placeholder:text-on-surface-variant/50" />
      </div>
      <div class="flex items-center gap-2">
        <button v-for="mode in ['table', 'cards']" :key="mode" class="flex items-center gap-1.5 rounded-lg border px-3 py-1.5 font-label-sm text-label-sm transition" :class="viewMode === mode ? 'bg-primary-container text-on-primary-container border-primary' : 'border-outline-variant text-on-surface-variant hover:text-on-surface hover:bg-surface-container-high'" @click="viewMode = mode">
          <span class="material-symbols-outlined text-[14px]">{{ mode === 'table' ? 'table' : 'grid_view' }}</span>
          {{ mode === 'table' ? 'Tableau' : 'Cartes' }}
        </button>
      </div>
    </div>

    <div v-if="loading" class="flex items-center justify-center py-24"><div class="spinner" /></div>
    <div v-else-if="error" class="bg-secondary-container/10 border border-secondary/20 rounded-xl p-6 font-body-sm text-body-sm text-secondary">{{ error }}</div>
    <div v-else>
      <div v-if="viewMode === 'table'" class="bg-surface-container border border-outline-variant rounded-xl overflow-x-auto">
        <table class="w-full text-left border-collapse">
          <thead class="bg-surface-container-high border-b border-outline-variant">
            <tr>
              <th v-for="field in config.fields" :key="field" class="px-4 py-3 font-label-md text-label-md text-on-surface-variant uppercase tracking-wider whitespace-nowrap">{{ field }}</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-outline-variant/30">
            <tr v-for="(item, index) in filteredItems" :key="index" class="hover:bg-surface-container-highest transition-colors" :class="index % 2 === 1 ? 'bg-surface-container-low' : ''">
              <td v-for="field in config.fields" :key="field" class="px-4 py-3 font-body-md text-body-md text-on-surface">
                <span v-if="typeof item[field] === 'boolean'" class="status-chip" :class="item[field] ? 'affecte' : 'hors-service'">{{ item[field] ? 'Oui' : 'Non' }}</span>
                <span v-else-if="displayValue(item, field) !== null && displayValue(item, field) !== undefined">{{ displayValue(item, field) }}</span>
                <span v-else class="text-on-surface-variant">—</span>
              </td>
            </tr>
          </tbody>
        </table>
        <div v-if="!filteredItems.length" class="flex items-center justify-center py-12 font-body-md text-body-md text-on-surface-variant">Aucune donnée trouvée.</div>
      </div>

      <div v-else class="grid gap-4 grid-cols-1 sm:grid-cols-2 xl:grid-cols-3">
        <div v-for="(item, index) in filteredItems" :key="index" class="bg-surface-container border border-outline-variant rounded-xl p-5 hover:border-primary/50 transition-colors">
          <div class="flex items-center justify-between mb-4">
            <div class="flex items-center gap-2">
              <span class="material-symbols-outlined text-primary text-[18px]">{{ pageIcon }}</span>
              <span class="font-label-sm text-label-sm text-on-surface-variant uppercase">#{{ index + 1 }}</span>
            </div>
          </div>
          <div class="space-y-2">
            <div v-for="field in config.fields" :key="field" class="flex items-start justify-between gap-3 border-b border-outline-variant/20 pb-2 last:border-0">
              <span class="font-label-sm text-label-sm text-on-surface-variant">{{ field }}</span>
              <span class="font-body-md text-body-md text-on-surface text-right">{{ displayValue(item, field) ?? '—' }}</span>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
