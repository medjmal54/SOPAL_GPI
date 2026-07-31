<script setup>
import { ref, computed, onMounted } from 'vue'
import Modal from '../components/Modal.vue'
import { apiGet, apiPost } from '../services/api'
import { useAuth } from '../services/auth'
import { useToast } from '../composables/useToast'

const { state } = useAuth()
const { success, error: toastError } = useToast()

const materials = ref([])
const localisations = ref([])
const matypes = ref([])
const loading = ref(true)
const query = ref('')

const showRequest = ref(false)
const saving = ref(false)
const selectedMaterial = ref(null)
const descPanne = ref('')

onMounted(async () => {
  try {
    const [mats, locs, types] = await Promise.all([
      apiGet(`/Materials/by-employe/${state.matricule}`),
      apiGet('/Localistations'),
      apiGet('/Matypes'),
    ])
    materials.value = Array.isArray(mats) ? mats : []
    localisations.value = Array.isArray(locs) ? locs : []
    matypes.value = Array.isArray(types) ? types : []
  } catch (e) {
    toastError(e.message || 'Erreur de chargement.')
  } finally {
    loading.value = false
  }
})

const filtered = computed(() => {
  const list = materials.value || []
  if (!query.value) return list
  const q = query.value.toLowerCase()
  return list.filter(m => JSON.stringify(m).toLowerCase().includes(q))
})

function raw(mat, k1, k2) { return mat[k1] ?? mat[k2] ?? null }

function localisationName(id) {
  const found = localisations.value.find(l => (l.id ?? l.Id) === id)
  return found ? (found.nom ?? found.Nom ?? `#${id}`) : `#${id}`
}

function typeName(id) {
  const found = matypes.value.find(t => (t.id ?? t.Id) === id)
  return found ? (found.libelle ?? found.Libelle ?? `#${id}`) : `#${id}`
}

function etatBadgeClass(id) {
  const map = { 1: 'status-chip en-service', 2: 'status-chip en-stock-blue', 3: 'status-chip maintenance', 4: 'status-chip hors-service', 5: 'status-chip hors-service' }
  return map[id] ?? 'status-chip'
}

function openRequest(mat) {
  selectedMaterial.value = mat
  descPanne.value = ''
  showRequest.value = true
}

async function submitRequest() {
  if (!descPanne.value) return
  saving.value = true
  try {
    const matId = raw(selectedMaterial.value, 'id', 'Id')
    await apiPost(`/v1/mouvements/Requests?DescPanne=${encodeURIComponent(descPanne.value)}&Matid=${matId}&matricule=${state.matricule}`)
    success('Demande d\'intervention créée.')
    showRequest.value = false
  } catch (e) {
    toastError(e.message || 'Erreur.')
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <div>
    <div class="flex justify-between items-center mb-6">
      <div>
        <h2 class="font-headline-md text-headline-md font-bold text-on-surface">Mes Matériels</h2>
        <p class="font-label-sm text-label-sm text-on-surface-variant uppercase tracking-wider">Matériels qui vous sont affectés</p>
      </div>
    </div>

    <div v-if="loading" class="flex items-center justify-center py-24">
      <div class="flex flex-col items-center gap-4">
        <div class="spinner" />
        <p class="font-body-sm text-body-sm text-on-surface-variant">Chargement de vos matériels…</p>
      </div>
    </div>

    <div v-else-if="filtered.length === 0" class="bg-surface-container border border-outline-variant rounded-xl p-12 text-center">
      <span class="material-symbols-outlined text-[48px] text-on-surface-variant">devices</span>
      <p class="font-body-md text-body-md text-on-surface-variant mt-3">Aucun matériel ne vous est actuellement affecté.</p>
    </div>

    <template v-else>
      <div class="flex items-center gap-2 mb-4">
        <div class="relative flex-1 max-w-xs">
          <span class="material-symbols-outlined absolute left-3 top-1/2 -translate-y-1/2 text-on-surface-variant text-[18px]">search</span>
          <input v-model="query" class="w-full bg-surface border border-outline-variant rounded-lg pl-9 pr-3 py-2 font-label-md text-on-surface focus:border-primary focus:ring-1 focus:ring-primary outline-none" placeholder="Rechercher..." type="text">
        </div>
        <span class="font-label-sm text-label-sm text-on-surface-variant">{{ filtered.length }} matériel{{ filtered.length !== 1 ? 's' : '' }}</span>
      </div>

      <div class="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-3 gap-4">
        <div v-for="mat in filtered" :key="raw(mat, 'id', 'Id')" class="bg-surface-container border border-outline-variant rounded-xl p-4 hover:border-primary/50 transition-all group flex flex-col">
          <div class="flex items-start justify-between gap-3 mb-4">
            <div class="flex items-center gap-3">
              <div class="flex items-center justify-center rounded-lg w-10 h-10 bg-surface-container-higher shrink-0">
                <span class="material-symbols-outlined text-[22px] text-primary">devices</span>
              </div>
              <div class="min-w-0">
                <div class="font-body-md text-body-md font-bold text-on-surface truncate">{{ raw(mat, 'libelle', 'Libelle') }}</div>
                <div class="font-label-sm text-label-sm text-on-surface-variant truncate">{{ raw(mat, 'marque', 'Marque') }} · {{ raw(mat, 'modele', 'Modele') }}</div>
              </div>
            </div>
            <span :class="etatBadgeClass(raw(mat, 'etatId', 'EtatId'))">{{ typeName(raw(mat, 'etatId', 'EtatId')) }}</span>
          </div>

          <div class="flex-1 space-y-2 mb-4">
            <div class="flex items-center gap-2 font-label-sm text-label-sm text-on-surface-variant">
              <span class="material-symbols-outlined text-[16px]">inventory_2</span>
              <span>{{ typeName(raw(mat, 'typeMaterielId', 'TypeMaterielId')) }}</span>
            </div>
            <div class="flex items-center gap-2 font-label-sm text-label-sm text-on-surface-variant">
              <span class="material-symbols-outlined text-[16px]">location_on</span>
              <span>{{ localisationName(raw(mat, 'localisationId', 'LocalisationId')) }}</span>
            </div>
            <div class="font-label-sm text-label-sm text-on-surface-variant font-mono">
              N° {{ raw(mat, 'numSerie', 'NumSerie') || '—' }}
            </div>
          </div>

          <button @click="openRequest(mat)" class="w-full flex items-center justify-center gap-2 py-2 border border-outline-variant rounded-lg text-primary hover:bg-surface-container-high transition-colors font-label-md text-label-md mt-auto">
            <span class="material-symbols-outlined text-[16px]">build</span>
            Demander intervention
          </button>
        </div>
      </div>
    </template>

    <Modal v-if="showRequest" title="Demande d'intervention" confirm-text="Envoyer" :loading="saving" @close="showRequest = false" @confirm="submitRequest">
      <div class="space-y-3">
        <p class="font-body-sm text-body-sm text-on-surface-variant">
          Matériel : <strong class="text-on-surface">{{ raw(selectedMaterial, 'libelle', 'Libelle') }}</strong>
        </p>
        <div class="form-group">
          <label class="form-label">Description du problème</label>
          <textarea v-model="descPanne" class="form-input" rows="3" placeholder="Décrivez le problème rencontré…"></textarea>
        </div>
      </div>
    </Modal>
  </div>
</template>
