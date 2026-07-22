<script setup>
import { ref, computed, onMounted } from 'vue'
import { MapPin, Search } from 'lucide-vue-next'
import PageHeader from '../components/PageHeader.vue'
import DataTable from '../components/DataTable.vue'
import { apiGet } from '../services/api'
import { useToast } from '../composables/useToast'

const { error: toastError } = useToast()

const items = ref([])
const loading = ref(true)
const query = ref('')

onMounted(async () => {
  try {
    const data = await apiGet('/Localistations')
    items.value = Array.isArray(data) ? data : []
  } catch (e) {
    toastError(e.message || 'Erreur de chargement.')
  } finally {
    loading.value = false
  }
})

const filtered = computed(() => {
  const list = items.value || []
  if (!query.value) return list
  const q = query.value.toLowerCase()
  return list.filter(i => JSON.stringify(i).toLowerCase().includes(q))
})

const columns = [
  { key: 'id', label: 'ID', sortable: true },
  { key: 'nom', label: 'Nom', sortable: true },
  { key: 'libelle', label: 'Libellé', sortable: true },
]
</script>

<template>
  <div>
    <PageHeader
      title="Localisations"
      description="Gestion des sites et localisations du parc."
      :icon="MapPin"
    />

    <div class="flex items-center gap-2 mb-4">
      <div class="flex items-center gap-2 flex-1 max-w-xs rounded-xl px-3 py-2.5 text-sm"
        style="background:rgba(8,12,30,0.8);border:1px solid rgba(255,255,255,0.08);">
        <Search :size="15" class="text-slate-500 flex-shrink-0" />
        <input v-model="query" placeholder="Filtrer…" class="bg-transparent outline-none text-slate-200 placeholder-slate-600 w-full" />
      </div>
    </div>

    <DataTable
      :columns="columns"
      :rows="filtered"
      :loading="loading"
      empty-text="Aucune localisation trouvée."
    >
      <template #cell-id="{ value }">
        <span class="font-mono text-xs text-indigo-300">{{ value }}</span>
      </template>
    </DataTable>
  </div>
</template>
