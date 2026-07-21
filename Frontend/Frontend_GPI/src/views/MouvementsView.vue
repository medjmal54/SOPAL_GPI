<script setup>
import { ref, computed, onMounted } from 'vue'
import { ArrowRightLeft, Search } from 'lucide-vue-next'
import PageHeader from '../components/PageHeader.vue'
import DataTable  from '../components/DataTable.vue'
import { apiGet } from '../services/api'
import { useToast } from '../composables/useToast'

const { error: toastError } = useToast()

// ─── State ───
const items         = ref([])
const loading       = ref(true)
const query         = ref('')
const materials     = ref([])
const localisations = ref([])

async function loadData() {
  loading.value = true
  try {
    const [mouvs, mats, locs] = await Promise.all([
      apiGet('/v1/mouvements'),
      apiGet('/Materials'),
      apiGet('/Localistations'),
    ])
    items.value = Array.isArray(mouvs) ? mouvs : []
    materials.value = Array.isArray(mats) ? mats : []
    localisations.value = Array.isArray(locs) ? locs : []
  } catch (e) {
    toastError(e.message || 'Erreur de chargement.')
  } finally {
    loading.value = false
  }
}

function materialName(id) {
  const found = materials.value.find(m => (m.id ?? m.Id) === id)
  return found ? (found.libelle ?? found.Libelle ?? `#${id}`) : `#${id}`
}

function localisationName(id) {
  const found = localisations.value.find(l => (l.id ?? l.Id) === id)
  return found ? (found.nom ?? found.Nom ?? `#${id}`) : `#${id}`
}

onMounted(loadData)

const filtered = computed(() => {
  if (!query.value) return items.value
  const q = query.value.toLowerCase()
  return items.value.filter(i => JSON.stringify(i).toLowerCase().includes(q))
})

const columns = [
  { key: 'dateMouvement', label: 'Date',         sortable: true  },
  { key: 'typmov',        label: 'Type',         sortable: true  },
  { key: 'materielId',    label: 'Matériel',     sortable: true  },
  { key: 'localisationId',label: 'Localisation', sortable: false },
  { key: 'employeMat',    label: 'Employé',      sortable: false },
]

const typmovLabel = (t) => ({ 1: 'Affectation', 2: 'Retour', 3: 'Transfert', 4: 'Maintenance' })[t] ?? String(t ?? '—')
const typmovBadge = (t) => ({ 1: 'badge-cyan', 2: 'badge-amber', 3: 'badge-indigo', 4: 'badge-red' })[t] ?? 'badge-gray'
</script>

<template>
  <div>
    <PageHeader
      title="Historique des Mouvements"
      description="Suivi des affectations, transferts et retours de matériels."
      :icon="ArrowRightLeft"
    />

    <!-- Search -->
    <div class="flex items-center gap-2 mb-4">
      <div class="flex items-center gap-2 flex-1 max-w-xs rounded-xl px-3 py-2.5 text-sm"
        style="background:rgba(8,12,30,0.8);border:1px solid rgba(255,255,255,0.08);">
        <Search :size="15" class="text-slate-500 flex-shrink-0" />
        <input v-model="query" placeholder="Filtrer les mouvements…" class="bg-transparent outline-none text-slate-200 placeholder-slate-600 w-full" />
      </div>
    </div>

    <!-- Table -->
    <DataTable
      :columns="columns"
      :rows="filtered"
      :loading="loading"
      empty-text="Aucun mouvement enregistré."
    >
      <template #cell-typmov="{ value }">
        <span :class="`badge ${typmovBadge(value)}`">{{ typmovLabel(value) }}</span>
      </template>
      <template #cell-dateMouvement="{ value }">
        <span class="text-slate-400 text-xs">{{ value ? new Date(value).toLocaleDateString('fr-FR') : '—' }}</span>
      </template>
      <template #cell-materielId="{ value }">
        <span class="text-slate-300">{{ materialName(value) }}</span>
      </template>
      <template #cell-localisationId="{ value }">
        <span class="text-slate-300">{{ localisationName(value) }}</span>
      </template>
      <template #cell-employeMat="{ value }">
        <span v-if="value" class="badge badge-gray">Emp. #{{ value }}</span>
        <span v-else class="text-slate-600 text-xs">Non affecté</span>
      </template>
    </DataTable>
  </div>
</template>
