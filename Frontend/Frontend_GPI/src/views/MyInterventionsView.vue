<script setup>
import { ref, computed, onMounted } from 'vue'
import { Wrench, Search, Eye, ToggleLeft, ToggleRight, List, Plus, CheckSquare, CircleCheck } from 'lucide-vue-next'
import PageHeader from '../components/PageHeader.vue'
import DataTable from '../components/DataTable.vue'
import Modal from '../components/Modal.vue'
import { apiGet, apiPost, apiPut } from '../services/api'
import { useAuth } from '../services/auth'
import { useToast } from '../composables/useToast'

const { state } = useAuth()
const { success, error: toastError } = useToast()

const items = ref([])
const materials = ref([])
const employees = ref([])
const loading = ref(true)
const query = ref('')

const showDetail = ref(false)
const selectedRequest = ref(null)
const interventions = ref([])
const loadingInterv = ref(false)

const showBatch = ref(false)
const batchSelected = ref([])
const batchComment = ref('')
const batchSaving = ref(false)

const showAddSingle = ref(false)
const addSingleRequest = ref(null)
const addSingleComment = ref('')
const addSingleSaving = ref(false)

async function loadData() {
  loading.value = true
  try {
    const [reqs, mats, emps] = await Promise.all([
      apiGet('/v1/mouvements/my-interventions'),
      apiGet('/Materials'),
      apiGet('/Employes'),
    ])
    items.value = Array.isArray(reqs) ? reqs : []
    materials.value = Array.isArray(mats) ? mats : []
    employees.value = Array.isArray(emps) ? emps : []
  } catch (e) {
    toastError(e.message || 'Erreur de chargement.')
  } finally {
    loading.value = false
  }
}

onMounted(loadData)

function materialName(id) {
  if (!id) return '#'
  const found = materials.value.find(m => m.id === id)
  return found?.libelle ?? `#${id}`
}

function employeeName(matricule) {
  if (!matricule) return null
  const found = employees.value.find(e => e.matricule === matricule)
  if (!found) return `#${matricule}`
  return `${found.nom ?? ''} ${found.prenom ?? ''}`.trim() || `#${matricule}`
}

function formatDate(d) {
  if (!d) return '—'
  const date = new Date(d)
  if (Number.isNaN(date.getTime())) return '—'
  return date.toLocaleDateString('fr-FR', { day: '2-digit', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit' })
}

function statusLabel(statut) {
  return statut || 'En cours'
}

function isTerminated(statut) {
  return statusLabel(statut) === 'Terminé'
}

const inProgressCount = computed(() => items.value.filter(i => !isTerminated(i.statut)).length)

const filtered = computed(() => {
  if (!query.value) return items.value
  const q = query.value.toLowerCase()
  return items.value.filter(item => {
    const search = [
      item.commentaires,
      item.statut,
      item.dateMouvement,
      materialName(item.materielId),
    ]
      .filter(Boolean)
      .join(' ')
      .toLowerCase()
    return search.includes(q)
  })
})

const columns = [
  { key: 'dateMouvement', label: 'Date', sortable: true },
  { key: 'materielId', label: 'Matériel', sortable: true },
  { key: 'commentaires', label: 'Description', sortable: false },
  { key: 'statut', label: 'Statut', sortable: true },
]

function openDetail(row) {
  selectedRequest.value = row
  showDetail.value = true
  loadInterventions(row.id)
}

async function loadInterventions(mouvementId) {
  loadingInterv.value = true
  interventions.value = []
  try {
    const data = await apiGet(`/Intervention/by-mouvement/${mouvementId}`)
    interventions.value = Array.isArray(data) ? data : []
  } catch (e) {
    toastError(e.message || 'Erreur de chargement des interventions.')
  } finally {
    loadingInterv.value = false
  }
}

async function toggleStatus(row) {
  const id = row.id
  try {
    const res = await apiPut(`/v1/mouvements/${id}/toggle-resolve`)
    const idx = items.value.findIndex(i => i.id === id)
    if (idx !== -1) {
      items.value[idx] = { ...items.value[idx], statut: res.statut }
    }
    success(res.message || 'Statut mis à jour.')
  } catch (e) {
    toastError(e.message || 'Erreur lors du changement de statut.')
  }
}

async function confirmToggleStatusFromDetail() {
  await toggleStatus(selectedRequest.value)
  showDetail.value = false
  await loadData()
}

function openBatch() {
  batchSelected.value = []
  batchComment.value = ''
  showBatch.value = true
}

function toggleBatchSelect(rowId) {
  const idx = batchSelected.value.indexOf(rowId)
  if (idx === -1) batchSelected.value.push(rowId)
  else batchSelected.value.splice(idx, 1)
}

function isBatchSelected(rowId) {
  return batchSelected.value.includes(rowId)
}

async function submitBatch() {
  if (!batchSelected.value.length || !batchComment.value) return
  batchSaving.value = true
  try {
    const itemsPayload = batchSelected.value.map(id => {
      const row = items.value.find(i => i.id === id)
      return { matId: row?.materielId, maintId: id }
    })
    await apiPost('/Intervention/batch', {
      commentaires: batchComment.value,
      date: new Date().toISOString().split('T')[0],
      items: itemsPayload,
    })
    success(`${itemsPayload.length} intervention(s) créée(s).`)
    showBatch.value = false
    await loadData()
  } catch (e) {
    toastError(e.message || "Erreur lors de l'ajout multiple.")
  } finally {
    batchSaving.value = false
  }
}

function openAddSingle(row) {
  addSingleRequest.value = row
  addSingleComment.value = ''
  showAddSingle.value = true
}

async function submitAddSingle() {
  if (!addSingleComment.value || !addSingleRequest.value) return
  addSingleSaving.value = true
  try {
    await apiPost('/Intervention', {
      commentaires: addSingleComment.value,
      date: new Date().toISOString().split('T')[0],
      matId: addSingleRequest.value.materielId,
      maintId: addSingleRequest.value.id,
    })
    success('Intervention enregistrée.')
    showAddSingle.value = false
  } catch (e) {
    toastError(e.message || "Erreur lors de l'ajout.")
  } finally {
    addSingleSaving.value = false
  }
}

function interventionProgress(statut) {
  if (isTerminated(statut)) return 100
  if (statusLabel(statut) === 'En cours') return 50
  return 0
}

function progressGradient(statut) {
  if (isTerminated(statut)) return 'linear-gradient(90deg,#22c55e,#4ade80)'
  if (statusLabel(statut) === 'En cours') return 'linear-gradient(90deg,#6366f1,#818cf8)'
  return '#475569'
}

function badgeStyle(statut) {
  if (isTerminated(statut)) return 'background:rgba(34,197,94,0.15);color:#4ade80;'
  if (statusLabel(statut) === 'En cours') return 'background:rgba(99,102,241,0.15);color:#a5b4fc;'
  return 'background:rgba(148,163,184,0.15);color:#94a3b8;'
}
</script>

<template>
  <div>
    <PageHeader
      title="Mes Interventions"
      description="Gérez les demandes que vous avez prises en charge."
      :icon="Wrench"
    >
      <template #actions>
        <span class="badge badge-indigo">
          <Wrench :size="11" class="mr-1" />
          {{ inProgressCount }} en cours
        </span>
      </template>
    </PageHeader>

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
      :show-actions="true"
      empty-text="Aucune intervention assignée."
    >
      <template #cell-dateMouvement="{ value }">
        <span class="text-slate-400 text-xs">{{ formatDate(value) }}</span>
      </template>
      <template #cell-materielId="{ value }">
        <span class="text-slate-300">{{ materialName(value) }}</span>
      </template>
      <template #cell-commentaires="{ value }">
        <span class="text-slate-400 text-sm truncate block max-w-xs">{{ value }}</span>
      </template>
      <template #cell-statut="{ value, row }">
        <button
          class="inline-flex items-center gap-1.5 text-xs font-medium transition cursor-pointer"
          :class="[isTerminated(value) ? 'text-emerald-400 hover:text-emerald-300' : 'text-indigo-400 hover:text-indigo-300']"
          @click="toggleStatus(row)"
        >
          <ToggleRight v-if="isTerminated(value)" :size="14" />
          <ToggleLeft v-else :size="14" />
          {{ statusLabel(value) }}
        </button>
      </template>
      <template #actions="{ row }">
        <div class="flex items-center gap-1.5">
          <button
            class="btn btn-sm gap-1"
            style="background:rgba(34,197,94,0.1);color:#4ade80;border:1px solid rgba(34,197,94,0.25);"
            title="Ajouter une intervention"
            @click="openAddSingle(row)"
          >
            <Plus :size="12" />
            Ajouter
          </button>
          <button
            class="btn btn-sm gap-1"
            style="background:rgba(99,102,241,0.1);color:#a5b4fc;border:1px solid rgba(99,102,241,0.25);"
            title="Voir les interventions"
            @click="openDetail(row)"
          >
            <Eye :size="12" />
            Détails
          </button>
        </div>
      </template>
    </DataTable>

    <!-- Detail Modal -->
    <Modal
      v-if="showDetail"
      :title="`Intervention — ${materialName(selectedRequest?.materielId)}`"
      size="lg"
      hide-footer
      @close="showDetail = false"
    >
      <div class="space-y-5">
        <div class="rounded-xl p-3" style="background:rgba(8,12,30,0.6);border:1px solid rgba(255,255,255,0.06);">
          <div class="text-xs text-slate-500 mb-1">Description de la panne</div>
          <p class="text-sm text-slate-300">{{ selectedRequest?.commentaires || 'Aucune description.' }}</p>
        </div>

        <div>
          <div class="flex items-center justify-between mb-2">
            <div class="flex items-center gap-2">
              <CircleCheck :size="14" class="text-indigo-400" />
              <span class="text-sm font-semibold text-slate-300">Progression</span>
            </div>
            <span class="text-xs font-medium px-2 py-0.5 rounded-full" :style="badgeStyle(selectedRequest?.statut)">
              {{ statusLabel(selectedRequest?.statut) }}
            </span>
          </div>
          <div class="w-full h-2 rounded-full overflow-hidden" style="background:rgba(255,255,255,0.06);">
            <div class="h-full rounded-full transition-all duration-500" :style="{
              width: interventionProgress(selectedRequest?.statut) + '%',
              background: progressGradient(selectedRequest?.statut)
            }" />
          </div>
          <div class="flex justify-between mt-1.5">
            <span class="text-xs text-slate-600">0%</span>
            <span class="text-xs text-slate-600">{{ interventionProgress(selectedRequest?.statut) }}%</span>
            <span class="text-xs text-slate-600">100%</span>
          </div>
        </div>

        <div>
          <div class="flex items-center gap-2 mb-3">
            <List :size="14" class="text-indigo-400" />
            <span class="text-sm font-semibold text-slate-300">
              Interventions existantes
              <span v-if="interventions.length" class="badge badge-gray ml-1">{{ interventions.length }}</span>
            </span>
          </div>

          <div v-if="loadingInterv" class="flex items-center justify-center py-6">
            <div class="spinner" style="width:20px;height:20px;" />
          </div>
          <div v-else-if="interventions.length === 0" class="text-sm text-slate-600 py-4 text-center">
            Aucune intervention enregistrée pour le moment.
          </div>
          <div v-else class="space-y-2 max-h-48 overflow-y-auto pr-1">
            <div
              v-for="(interv, idx) in interventions"
              :key="interv.id"
              class="rounded-xl p-3 flex items-start gap-3"
              style="background:rgba(99,102,241,0.06);border:1px solid rgba(99,102,241,0.12);"
            >
              <div
                class="flex-shrink-0 flex items-center justify-center rounded-full mt-0.5"
                style="width:28px;height:28px;background:rgba(99,102,241,0.15);border:1px solid rgba(99,102,241,0.25);"
              >
                <span class="text-xs font-bold text-indigo-400">{{ idx + 1 }}</span>
              </div>
              <div class="flex-1 min-w-0">
                <div class="flex items-center gap-2 mb-1">
                  <span class="text-xs text-slate-500">{{ formatDate(interv.date) }}</span>
                  <span v-if="interv.intervenantId" class="text-xs text-slate-600">
                    — {{ employeeName(interv.intervenantId) }}
                  </span>
                </div>
                <p class="text-sm text-slate-300">{{ interv.commentaires }}</p>
              </div>
            </div>
          </div>
        </div>

        <div class="pt-2" style="border-top:1px solid rgba(255,255,255,0.06);">
          <button
            class="btn w-full justify-center gap-2"
            :style="isTerminated(selectedRequest?.statut)
              ? 'background:rgba(99,102,241,0.1);color:#a5b4fc;border:1px solid rgba(99,102,241,0.25);'
              : 'background:rgba(34,197,94,0.1);color:#4ade80;border:1px solid rgba(34,197,94,0.25);'"
            @click="confirmToggleStatusFromDetail"
          >
            <ToggleRight v-if="isTerminated(selectedRequest?.statut)" :size="15" />
            <ToggleLeft v-else :size="15" />
            {{ isTerminated(selectedRequest?.statut) ? 'Marquer comme en cours' : 'Marquer comme terminé' }}
          </button>
        </div>
      </div>
    </Modal>

    <!-- Add Single Modal -->
    <Modal
      v-if="showAddSingle"
      :title="`Nouvelle intervention — ${materialName(addSingleRequest?.materielId)}`"
      size="md"
      confirm-text="Ajouter"
      :loading="addSingleSaving"
      @close="showAddSingle = false"
      @confirm="submitAddSingle"
    >
      <div class="space-y-4">
        <div class="rounded-xl p-3" style="background:rgba(8,12,30,0.6);border:1px solid rgba(255,255,255,0.06);">
          <div class="text-xs text-slate-500 mb-1">Description de la panne</div>
          <p class="text-sm text-slate-300">{{ addSingleRequest?.commentaires || 'Aucune description.' }}</p>
        </div>
        <div>
          <div class="flex items-center gap-2 mb-2">
            <Wrench :size="14" class="text-amber-400" />
            <span class="text-sm font-semibold text-slate-300">Description de l'intervention</span>
          </div>
          <textarea v-model="addSingleComment" class="form-input" rows="3" placeholder="Décrivez l'intervention réalisée…" />
        </div>
      </div>
    </Modal>

    <!-- Batch Modal -->
    <Modal
      v-if="showBatch"
      title="Intervention multiple"
      size="lg"
      confirm-text="Créer les interventions"
      :loading="batchSaving"
      @close="showBatch = false"
      @confirm="submitBatch"
    >
      <div class="space-y-5">
        <div class="text-sm text-slate-400">
          Sélectionnez les demandes et décrivez l'intervention réalisée. Une même intervention sera créée pour chaque demande sélectionnée.
        </div>

        <div>
          <div class="flex items-center gap-2 mb-3">
            <CheckSquare :size="14" class="text-indigo-400" />
            <span class="text-sm font-semibold text-slate-300">
              Demandes disponibles
              <span v-if="batchSelected.length" class="badge badge-gray ml-1">{{ batchSelected.length }}/{{ items.length }}</span>
            </span>
          </div>

          <div v-if="items.length === 0" class="text-sm text-slate-600 py-4 text-center">
            Aucune demande en cours.
          </div>

          <div v-else class="space-y-1.5 max-h-56 overflow-y-auto pr-1">
            <label
              v-for="row in items"
              :key="row.id"
              class="flex items-center gap-3 rounded-xl px-3 py-2.5 cursor-pointer transition"
              :style="isBatchSelected(row.id)
                ? 'background:rgba(99,102,241,0.12);border:1px solid rgba(99,102,241,0.3);'
                : 'background:rgba(8,12,30,0.6);border:1px solid rgba(255,255,255,0.06);'"
              @click.prevent="toggleBatchSelect(row.id)"
            >
              <input
                type="checkbox"
                :checked="isBatchSelected(row.id)"
                class="accent-indigo-500 w-4 h-4"
              />
              <div class="flex-1 min-w-0">
                <div class="text-sm text-slate-300 font-medium">{{ materialName(row.materielId) }}</div>
                <div class="text-xs text-slate-500 truncate">{{ row.commentaires || 'Aucune description' }}</div>
              </div>
              <span class="text-xs text-slate-600">{{ formatDate(row.dateMouvement) }}</span>
            </label>
          </div>
        </div>

        <div>
          <div class="flex items-center gap-2 mb-2">
            <Wrench :size="14" class="text-amber-400" />
            <span class="text-sm font-semibold text-slate-300">Description de l'intervention</span>
          </div>
          <textarea v-model="batchComment" class="form-input" rows="3" placeholder="Décrivez l'intervention réalisée…" />
        </div>
      </div>
    </Modal>
  </div>
</template>
