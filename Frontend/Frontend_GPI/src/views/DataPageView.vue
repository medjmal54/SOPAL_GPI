<script setup>
import { computed, onMounted, ref, watch } from 'vue'
import { useRoute } from 'vue-router'
import { Boxes, Users, ArrowRightLeft, MapPinned, Search, PlusCircle, Table2, LayoutGrid } from 'lucide-vue-next'
import PageHeader from '../components/PageHeader.vue'
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

const config = computed(() => {
  switch (route.path) {
    case '/materiels':
      return { title: 'Parc Matériel', description: 'Consultez et gérez les actifs du parc.', badge: 'API /Materials', icon: Boxes, fields: ['Libelle', 'Marque', 'Modele', 'NumSerie', 'Actif'] }
    case '/mouvements':
      return { title: 'Historique des Mouvements', description: 'Suivi des affectations, transferts et retours.', badge: 'API /Mouvements', icon: ArrowRightLeft, fields: ['DateMouvement', 'typmov', 'materielId', 'LocalisationId'] }
    case '/employes':
      return { title: 'Personnel', description: 'Fiche et suivi des collaborateurs.', badge: 'API /Employes', icon: Users, fields: ['Nom', 'Prenom', 'Email', 'Service', 'Poste'] }
    case '/localisations':
      return { title: 'Localisations', description: 'Vue des sites et emplacements du parc.', badge: 'API /Localistations', icon: MapPinned, fields: ['Nom'] }
    default:
      return { title: 'Données', description: '', badge: '', icon: Boxes, fields: [] }
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
  <div class="space-y-6">
    <PageHeader :title="config.title" :description="config.description" :badge="config.badge" :icon="config.icon" />

    <div class="rounded-3xl border border-slate-800/80 bg-slate-900/70 p-4 shadow-[0_16px_40px_rgba(2,8,23,0.25)]">
      <div class="flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
        <div class="flex items-center gap-2 rounded-2xl border border-slate-800 bg-slate-950/70 px-3 py-2 text-sm text-slate-400">
          <Search :size="16" />
          <input v-model="query" placeholder="Rechercher dans les données…" class="w-full bg-transparent outline-none placeholder:text-slate-500 md:w-72" />
        </div>
        <div class="flex items-center gap-2">
          <button v-for="mode in ['table', 'cards']" :key="mode" class="rounded-2xl border px-3 py-2 text-sm transition" :class="viewMode === mode ? 'border-cyan-500/20 bg-cyan-500/10 text-cyan-300' : 'border-slate-800 bg-slate-950/70 text-slate-400 hover:text-white'" @click="viewMode = mode">
            <component :is="mode === 'table' ? Table2 : LayoutGrid" :size="16" class="mr-1 inline" />
            {{ mode === 'table' ? 'Tableau' : 'Cartes' }}
          </button>
          <button class="flex items-center gap-2 rounded-2xl border border-cyan-500/20 bg-cyan-500/10 px-3 py-2 text-sm font-medium text-cyan-300 transition hover:bg-cyan-500/20">
            <PlusCircle :size="16" />
            Ajouter un élément
          </button>
        </div>
      </div>
    </div>

    <div v-if="loading" class="rounded-3xl border border-slate-800 bg-slate-900/70 p-8 text-sm text-slate-400">Chargement des données…</div>
    <div v-else-if="error" class="rounded-3xl border border-rose-500/20 bg-rose-500/10 p-6 text-sm text-rose-300">{{ error }}</div>
    <div v-else>
      <div v-if="viewMode === 'table'" class="overflow-hidden rounded-3xl border border-slate-800/80 bg-slate-900/70 shadow-[0_16px_40px_rgba(2,8,23,0.25)]">
        <div class="overflow-x-auto">
          <table class="min-w-full divide-y divide-slate-800 text-sm">
            <thead class="bg-slate-950/70 text-left text-slate-400">
              <tr>
                <th v-for="field in config.fields" :key="field" class="px-4 py-3 font-medium">{{ field }}</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-800/70 bg-slate-900/70">
              <tr v-for="(item, index) in filteredItems" :key="index" class="transition hover:bg-slate-800/40">
                <td v-for="field in config.fields" :key="field" class="px-4 py-3 text-slate-300">
                  <span v-if="typeof item[field] === 'boolean'">{{ item[field] ? 'Oui' : 'Non' }}</span>
                  <span v-else-if="displayValue(item, field) !== null && displayValue(item, field) !== undefined">{{ displayValue(item, field) }}</span>
                  <span v-else class="text-slate-500">—</span>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
        <div v-if="!filteredItems.length" class="p-6 text-sm text-slate-400">Aucune donnée ne correspond à la recherche.</div>
      </div>

      <div v-else class="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
        <div v-for="(item, index) in filteredItems" :key="index" class="rounded-3xl border border-slate-800/80 bg-slate-900/70 p-5 shadow-[0_16px_40px_rgba(2,8,23,0.25)]">
          <div class="mb-4 flex items-center justify-between">
            <div class="rounded-full border border-cyan-500/20 bg-cyan-500/10 px-3 py-1 text-[11px] font-medium uppercase tracking-[0.2em] text-cyan-300">{{ config.badge }}</div>
            <span class="text-xs text-slate-500">#{{ index + 1 }}</span>
          </div>
          <div class="space-y-2 text-sm text-slate-300">
            <div v-for="field in config.fields" :key="field" class="flex items-start justify-between gap-3 border-b border-slate-800/70 pb-2">
              <span class="text-slate-500">{{ field }}</span>
              <span class="text-right text-slate-200">{{ displayValue(item, field) ?? '—' }}</span>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
