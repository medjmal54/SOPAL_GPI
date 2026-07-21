<script setup>
import { ref, computed, onMounted } from 'vue'
import { Monitor, Wrench, MapPin, Tag, Search, Package } from 'lucide-vue-next'
import PageHeader from '../components/PageHeader.vue'
import Modal from '../components/Modal.vue'
import { apiGet, apiPost } from '../services/api'
import { useAuth } from '../services/auth'
import { useToast } from '../composables/useToast'

const { state } = useAuth()
const { success, error: toastError } = useToast()

const materials = ref([])
const localisations = ref([])
const matypes = ref([])
const etats = ref([])
const loading = ref(true)
const query = ref('')

const showRequest = ref(false)
const saving = ref(false)
const selectedMaterial = ref(null)
const descPanne = ref('')

onMounted(async () => {
  try {
    const [mats, locs, types, etatsList] = await Promise.all([
      apiGet(`/Materials/by-employe/${state.matricule}`),
      apiGet('/Localistations'),
      apiGet('/Matypes'),
      apiGet('/Etatmat'),
    ])
    materials.value = Array.isArray(mats) ? mats : []
    localisations.value = Array.isArray(locs) ? locs : []
    matypes.value = Array.isArray(types) ? types : []
    etats.value = Array.isArray(etatsList) ? etatsList : []
  } catch (e) {
    toastError(e.message || 'Erreur de chargement.')
  } finally {
    loading.value = false
  }
})

const filtered = computed(() => {
  if (!query.value) return materials.value
  const q = query.value.toLowerCase()
  return materials.value.filter(m => JSON.stringify(m).toLowerCase().includes(q))
})

function raw(mat, k1, k2) {
  return mat[k1] ?? mat[k2] ?? null
}

function localisationName(id) {
  const found = localisations.value.find(l => (l.id ?? l.Id) === id)
  return found ? (found.nom ?? found.Nom ?? `#${id}`) : `#${id}`
}

function typeName(id) {
  const found = matypes.value.find(t => (t.id ?? t.Id) === id)
  return found ? (found.libelle ?? found.Libelle ?? `#${id}`) : `#${id}`
}

function etatName(id) {
  const found = etats.value.find(e => (e.id ?? e.Id) === id)
  return found ? (found.libelle ?? found.Libelle ?? `#${id}`) : `#${id}`
}

function etatBadgeClass(id) {
  const map = { 1: 'badge-gray', 2: 'badge-cyan', 3: 'badge-amber', 4: 'badge-red', 5: 'badge-red' }
  return map[id] ?? 'badge-gray'
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
    await apiPost(
      `/v1/mouvements/Requests?DescPanne=${encodeURIComponent(descPanne.value)}&Matid=${matId}&matricule=${state.matricule}`
    )
    success('Demande d\'intervention créée avec succès.')
    showRequest.value = false
  } catch (e) {
    toastError(e.message || 'Erreur lors de la création.')
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <div>
    <PageHeader
      title="Mes Matériels"
      description="Consultez les matériels qui vous sont affectés et signalez un problème."
      :icon="Monitor"
    />

    <!-- Loading -->
    <div v-if="loading" class="flex items-center justify-center py-24">
      <div class="flex flex-col items-center gap-4">
        <div class="spinner" style="width:36px;height:36px;" />
        <p class="text-sm text-slate-500">Chargement de vos matériels…</p>
      </div>
    </div>

    <!-- Empty state -->
    <div
      v-else-if="materials.length === 0"
      class="glass p-12 text-center"
    >
      <Package :size="40" class="mx-auto mb-4 text-slate-600" />
      <p class="text-slate-400 text-sm">Aucun matériel ne vous est actuellement affecté.</p>
    </div>

    <template v-else>
      <!-- Search -->
      <div class="flex items-center gap-2 mb-5">
        <div
          class="flex items-center gap-2 flex-1 max-w-xs rounded-xl px-3 py-2.5 text-sm"
          style="background:rgba(8,12,30,0.8);border:1px solid rgba(255,255,255,0.08);"
        >
          <Search :size="15" class="text-slate-500 flex-shrink-0" />
          <input
            v-model="query"
            placeholder="Rechercher…"
            class="bg-transparent outline-none text-slate-200 placeholder-slate-600 w-full"
          />
        </div>
        <div class="text-xs text-slate-600 ml-2">
          {{ filtered.length }} matériel{{ filtered.length !== 1 ? 's' : '' }}
        </div>
      </div>

      <!-- Material Cards Grid -->
      <div class="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-3 gap-4">
        <div
          v-for="mat in filtered"
          :key="raw(mat, 'id', 'Id')"
          class="glass p-5 group hover:-translate-y-0.5 transition-all duration-200 flex flex-col"
        >
          <!-- Card Header -->
          <div class="flex items-start justify-between gap-3 mb-4">
            <div class="flex items-center gap-3">
              <div
                class="flex items-center justify-center rounded-xl flex-shrink-0"
                style="width:40px;height:40px;background:rgba(34,211,238,0.1);border:1px solid rgba(34,211,238,0.2);"
              >
                <Monitor :size="18" class="text-cyan-400" />
              </div>
              <div class="min-w-0">
                <div class="text-sm font-semibold text-slate-200 truncate">
                  {{ raw(mat, 'libelle', 'Libelle') }}
                </div>
                <div class="text-xs text-slate-500 truncate">
                  {{ raw(mat, 'marque', 'Marque') }} · {{ raw(mat, 'modele', 'Modele') }}
                </div>
              </div>
            </div>
            <span :class="`badge ${etatBadgeClass(raw(mat, 'etatId', 'EtatId'))}`">
              {{ etatName(raw(mat, 'etatId', 'EtatId')) }}
            </span>
          </div>

          <!-- Card Details -->
          <div class="flex-1 space-y-2.5 mb-4">
            <div class="flex items-center gap-2 text-xs text-slate-400">
              <Tag :size="11" class="text-slate-500 flex-shrink-0" />
              <span>{{ typeName(raw(mat, 'typeMaterielId', 'TypeMaterielId')) }}</span>
            </div>
            <div class="flex items-center gap-2 text-xs text-slate-400">
              <MapPin :size="11" class="text-slate-500 flex-shrink-0" />
              <span>{{ localisationName(raw(mat, 'localisationId', 'LocalisationId')) }}</span>
            </div>
            <div class="text-xs text-slate-600 font-mono">
              N° {{ raw(mat, 'numSerie', 'NumSerie') }}
            </div>
          </div>

          <!-- Card Action -->
          <button
            class="btn w-full justify-center gap-2"
            style="background:rgba(245,158,11,0.1);color:#fcd34d;border:1px solid rgba(245,158,11,0.25);"
            @click="openRequest(mat)"
          >
            <Wrench :size="14" />
            Demander intervention
          </button>
        </div>
      </div>
    </template>

    <!-- Request Modal -->
    <Modal
      v-if="showRequest"
      title="Demande d'intervention"
      confirm-text="Envoyer"
      :loading="saving"
      @close="showRequest = false"
      @confirm="submitRequest"
    >
      <div class="space-y-3">
        <p class="text-sm text-slate-400">
          Matériel : <strong class="text-slate-200">
            {{ raw(selectedMaterial, 'libelle', 'Libelle') }} — {{ raw(selectedMaterial, 'marque', 'Marque') }}
          </strong>
        </p>
        <div class="form-group">
          <label class="form-label">Description du problème</label>
          <textarea
            v-model="descPanne"
            class="form-input"
            rows="3"
            placeholder="Décrivez le problème rencontré…"
          />
        </div>
      </div>
    </Modal>
  </div>
</template>
