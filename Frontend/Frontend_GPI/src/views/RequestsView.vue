<script setup>
import { ref, computed, onMounted, watch } from 'vue'
import { Wrench, Search, PlusCircle, Clock, UserCheck, Eye, CheckCircle, List, ToggleLeft, ToggleRight } from 'lucide-vue-next'
import PageHeader from '../components/PageHeader.vue'
import DataTable from '../components/DataTable.vue'
import Modal from '../components/Modal.vue'
import { apiGet, apiPost, apiPut } from '../services/api'
import { useAuth } from '../services/auth'
import { useToast } from '../composables/useToast'
import { useSignalR } from '../services/signalr'

const { state, isAdmin } = useAuth()
const { success, error: toastError } = useToast()
const { requests: signalRequests, startConnection } = useSignalR()

const items = ref([])
const materials = ref([])
const employees = ref([])
const loading = ref(true)
const query = ref('')
const showAdd = ref(false)
const saving = ref(false)

const descPanne = ref('')
const matId = ref('')

// ─── Admin: Intervention Detail ───
const showIntervDetail = ref(false)
const selectedRequest = ref(null)
const interventions = ref([])
const loadingInterv = ref(false)
const intervComment = ref('')

const pendingCount = ref(0)

async function loadData() {
  loading.value = true
  try {
    const [reqs, mats, emps] = await Promise.all([
      isAdmin()
        ? apiGet('/v1/mouvements/Requests')
        : apiGet(`/v1/mouvements/Requests/${state.matricule}`),
      apiGet('/Materials'),
      apiGet('/Employes'),
    ])
    items.value = Array.isArray(reqs) ? reqs : []
    materials.value = Array.isArray(mats) ? mats : []
    employees.value = Array.isArray(emps) ? emps : []

    if (isAdmin()) {
      const countData = await apiGet('/v1/mouvements/Requests/count')
      pendingCount.value = countData?.count ?? items.value.length
    }
  } catch (e) {
    toastError(e.message || 'Erreur de chargement.')
  } finally {
    loading.value = false
  }
}

onMounted(() => {
  loadData()
  if (isAdmin()) {
    startConnection()
  }
})

watch(signalRequests, (newRequests) => {
  for (const req of newRequests) {
    const id = req.id ?? req.Id
    if (!items.value.some(i => (i.id ?? i.Id) === id)) {
      items.value.unshift(req)
      pendingCount.value++
      success('Nouvelle demande d\'intervention reçue.')
    }
  }
}, { deep: true })

const filtered = computed(() => {
  const list = items.value || []
  if (!query.value) return list
  const q = query.value.toLowerCase()
  return list.filter(i => JSON.stringify(i).toLowerCase().includes(q))
})

function materialName(id) {
  const found = materials.value.find(m => (m.id ?? m.Id) === id)
  return found ? (found.libelle ?? found.Libelle ?? `#${id}`) : `#${id}`
}

function employeeName(matricule) {
  if (!matricule) return null
  const found = employees.value.find(e => (e.matricule ?? e.Matricule) === matricule)
  return found ? `${found.nom ?? found.Nom ?? ''} ${found.prenom ?? found.Prenom ?? ''}`.trim() || `#${matricule}` : `#${matricule}`
}

function formatDate(d) {
  if (!d) return '—'
  const date = new Date(d)
  if (Number.isNaN(date.getTime())) return '—'
  return date.toLocaleDateString('fr-FR', { day: '2-digit', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit' })
}

const columns = [
  { key: 'dateMouvement', label: 'Date', sortable: true },
  { key: 'materielId', label: 'Matériel', sortable: true },
  { key: 'commentaires', label: 'Description', sortable: false },
  { key: 'statut', label: 'Statut', sortable: true },
  { key: 'employeMat', label: 'Demandeur', sortable: false },
]

// ─── User: Create Request ───
function openAdd() {
  descPanne.value = ''
  matId.value = ''
  showAdd.value = true
}

async function submitAdd() {
  if (!descPanne.value || !matId.value) return
  saving.value = true
  try {
    const params = new URLSearchParams({
      DescPanne: descPanne.value,
      Matid: String(matId.value),
      matricule: String(state.matricule)
    })
    await apiPost(`/v1/mouvements/Requests?${params.toString()}`)
    success('Demande d\'intervention créée.')
    showAdd.value = false
    loadData()
  } catch (e) {
    toastError(e.message || 'Erreur lors de la création.')
  } finally {
    saving.value = false
  }
}

// ─── Admin: Start Intervention (Pas encore intervenu → En cours) ───
async function startIntervention(row) {
  try {
    await apiPut(`/v1/mouvements/${row.id ?? row.Id}/start-intervention`)
    success('Intervention démarrée. Consultez "Mes Interventions" pour la gérer.')
    loadData()
  } catch (e) {
    toastError(e.message || 'Erreur lors du démarrage de l\'intervention.')
  }
}

// ─── Admin: Open Intervention Detail ───
async function openIntervDetail(row) {
  selectedRequest.value = row
  intervComment.value = ''
  showIntervDetail.value = true
  await loadInterventions(row.id ?? row.Id)
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

// ─── Admin: Add Intervention ───
async function submitInterv() {
  if (!intervComment.value || !selectedRequest.value) return
  saving.value = true
  try {
    await apiPost('/Intervention', {
      commentaires: intervComment.value,
      date: new Date().toISOString().split('T')[0],
      matId: selectedRequest.value.materielId ?? selectedRequest.value.MaterielId,
      maintId: selectedRequest.value.id ?? selectedRequest.value.Id,
    })
    success('Intervention enregistrée.')
    intervComment.value = ''
    await loadInterventions(selectedRequest.value.id ?? selectedRequest.value.Id)
  } catch (e) {
    toastError(e.message || 'Erreur lors de l\'ajout.')
  } finally {
    saving.value = false
  }
}

// ─── Admin: Resolve Request ───
async function resolveRequest() {
  if (!selectedRequest.value) return
  saving.value = true
  try {
    await apiPut(`/v1/mouvements/${selectedRequest.value.id ?? selectedRequest.value.Id}/resolve`)
    success('Demande marquée comme résolue.')
    showIntervDetail.value = false
    loadData()
  } catch (e) {
    toastError(e.message || 'Erreur lors de la résolution.')
  } finally {
    saving.value = false
  }
}

// ─── Admin: Toggle Maintenance Status (En cours <-> Terminé) ───
async function toggleMaintenanceStatus(row) {
  try {
    const res = await apiPut(`/v1/mouvements/${row.id ?? row.Id}/toggle-resolve`)
    const idx = items.value.findIndex(i => (i.id ?? i.Id) === (row.id ?? row.Id))
    if (idx !== -1) {
      items.value[idx] = { ...items.value[idx], statut: res.statut }
    }
    success(res.message || 'Statut mis à jour.')
  } catch (e) {
    toastError(e.message || 'Erreur lors du changement de statut.')
  }
}

const isEnCours = computed(() => {
  if (!selectedRequest.value) return false
  return (selectedRequest.value.statut ?? selectedRequest.value.Statut) === 'En cours'
})

const filteredUserMats = computed(() => {
  const list = materials.value || []
  return list.filter(m => (m.employeMat ?? m.EmployeMat) === state.matricule)
})
</script>

<template>
  <div>
    <PageHeader
      :title="isAdmin() ? 'Demandes d\'intervention' : 'Mes demandes'"
      :description="isAdmin() ? 'Gérez les demandes de maintenance du parc.' : 'Consultez et créez vos demandes d\'intervention.'"
      :icon="Wrench"
    >
      <template #actions>
        <div class="flex items-center gap-3">
          <span v-if="isAdmin()" class="badge badge-amber">
            <Clock :size="11" class="mr-1" />
            {{ pendingCount }} en attente
          </span>
          <button v-if="!isAdmin()" class="btn btn-primary" @click="openAdd">
            <PlusCircle :size="15" />
            Nouvelle demande
          </button>
        </div>
      </template>
    </PageHeader>

    <!-- Search -->
    <div class="flex items-center gap-2 mb-4">
      <div class="flex items-center gap-2 flex-1 max-w-xs rounded-xl px-3 py-2.5 text-sm"
        style="background:rgba(8,12,30,0.8);border:1px solid rgba(255,255,255,0.08);">
        <Search :size="15" class="text-slate-500 flex-shrink-0" />
        <input v-model="query" placeholder="Filtrer…" class="bg-transparent outline-none text-slate-200 placeholder-slate-600 w-full" />
      </div>
    </div>

    <!-- Table -->
    <DataTable
      :columns="columns"
      :rows="filtered"
      :loading="loading"
      :show-actions="isAdmin()"
      empty-text="Aucune demande en cours."
    >
      <template #cell-dateMouvement="{ value }">
        <span class="text-slate-400 text-xs">{{ formatDate(value) }}</span>
      </template>
      <template #cell-materielId="{ value }">
        <span class="text-slate-300">{{ materialName(value) }}</span>
      </template>
      <template #cell-commentaires="{ value }">
        <span class="text-slate-400 text-sm truncate block max-w-xs">{{ value || '—' }}</span>
      </template>
      <template #cell-statut="{ value, row }">
        <button
          v-if="isAdmin() && (value === 'En cours' || value === 'Terminé')"
          class="inline-flex items-center gap-1.5 text-xs font-medium transition cursor-pointer"
          :class="value === 'Terminé' ? 'text-emerald-400 hover:text-emerald-300' : 'text-indigo-400 hover:text-indigo-300'"
          @click="toggleMaintenanceStatus(row)"
        >
          <ToggleRight v-if="value === 'Terminé'" :size="14" />
          <ToggleLeft v-else :size="14" />
          {{ value }}
        </button>
        <span v-else-if="value === 'Pas encore intervenu'" class="badge badge-amber">
          {{ value }}
        </span>
        <span v-else class="badge badge-gray">
          {{ value || 'En attente' }}
        </span>
      </template>
      <template #cell-employeMat="{ value }">
        <span v-if="value" class="text-slate-300 text-sm">{{ employeeName(value) }}</span>
        <span v-else class="text-slate-600 text-xs">—</span>
      </template>
      <template #actions="{ row }">
        <button
          v-if="(row.statut ?? row.Statut) === 'Pas encore intervenu'"
          class="btn btn-primary btn-sm"
          @click="startIntervention(row)"
        >
          <Wrench :size="12" />
          Intervenir
        </button>
        <button
          v-else-if="(row.statut ?? row.Statut) === 'En cours' || (row.statut ?? row.Statut) === 'Terminé'"
          class="btn btn-sm"
          style="background:rgba(99,102,241,0.1);color:#a5b4fc;border:1px solid rgba(99,102,241,0.25);"
          @click="openIntervDetail(row)"
        >
          <Eye :size="12" />
          Ouvrir
        </button>
      </template>
    </DataTable>

    <!-- USER: Create Request Modal -->
    <Modal
      v-if="showAdd"
      title="Nouvelle demande d'intervention"
      confirm-text="Envoyer"
      :loading="saving"
      @close="showAdd = false"
      @confirm="submitAdd"
    >
      <div class="space-y-3">
        <p class="text-sm text-slate-400">
          Sélectionnez le matériel concerné et décrivez le problème.
        </p>
        <div class="form-group">
          <label class="form-label">Matériel</label>
          <select v-model="matId" class="form-input">
            <option value="">— Choisir un matériel —</option>
            <option v-for="m in filteredUserMats" :key="m.id ?? m.Id" :value="m.id ?? m.Id">
              {{ m.libelle ?? m.Libelle }} — {{ m.marque ?? m.Marque }}
            </option>
          </select>
        </div>
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

    <!-- ADMIN: Intervention Detail Modal -->
    <Modal
      v-if="showIntervDetail"
      :title="`Intervention — ${materialName(selectedRequest?.materielId)}`"
      size="lg"
      :hide-footer="!isEnCours"
      :confirm-text="isEnCours ? 'Ajouter' : ''"
      :loading="saving"
      @close="showIntervDetail = false"
      @confirm="submitInterv"
    >
      <div class="space-y-5">
        <!-- Request Info -->
        <div class="rounded-xl p-3" style="background:rgba(8,12,30,0.6);border:1px solid rgba(255,255,255,0.06);">
          <div class="text-xs text-slate-500 mb-1">Description de la panne</div>
          <p class="text-sm text-slate-300">{{ selectedRequest?.commentaires || 'Aucune description.' }}</p>
        </div>

        <!-- Existing Interventions -->
        <div>
          <div class="flex items-center gap-2 mb-3">
            <List :size="14" class="text-indigo-400" />
            <span class="text-sm font-semibold text-slate-300">
              Interventions existantes
              <span v-if="interventions.length" class="badge badge-gray ml-1">{{ interventions.length }}</span>
            </span>
          </div>

          <!-- Loading -->
          <div v-if="loadingInterv" class="flex items-center justify-center py-6">
            <div class="spinner" style="width:20px;height:20px;" />
          </div>

          <!-- Empty -->
          <div v-else-if="interventions.length === 0" class="text-sm text-slate-600 py-4 text-center">
            Aucune intervention enregistrée pour le moment.
          </div>

          <!-- Intervention List -->
          <div v-else class="space-y-2 max-h-48 overflow-y-auto pr-1">
            <div
              v-for="interv in interventions"
              :key="interv.id ?? interv.Id"
              class="rounded-xl p-3 flex items-start gap-3"
              style="background:rgba(99,102,241,0.06);border:1px solid rgba(99,102,241,0.12);"
            >
              <div
                class="flex-shrink-0 flex items-center justify-center rounded-full mt-0.5"
                style="width:28px;height:28px;background:rgba(99,102,241,0.15);border:1px solid rgba(99,102,241,0.25);"
              >
                <Wrench :size="12" class="text-indigo-400" />
              </div>
              <div class="flex-1 min-w-0">
                <div class="flex items-center gap-2 mb-1">
                  <span class="text-xs text-slate-500">{{ formatDate(interv.date ?? interv.Date) }}</span>
                  <span v-if="interv.intervenantId ?? interv.IntervenantId" class="text-xs text-slate-600">
                    — {{ employeeName(interv.intervenantId ?? interv.IntervenantId) }}
                  </span>
                </div>
                <p class="text-sm text-slate-300">{{ interv.commentaires || interv.Commentaires || '—' }}</p>
              </div>
            </div>
          </div>
        </div>

        <!-- New Intervention Form -->
        <div v-if="isEnCours">
          <div class="flex items-center gap-2 mb-2">
            <Wrench :size="14" class="text-amber-400" />
            <span class="text-sm font-semibold text-slate-300">Nouvelle intervention</span>
          </div>
          <textarea
            v-model="intervComment"
            class="form-input"
            rows="3"
            placeholder="Décrivez l'intervention réalisée…"
          />
        </div>

        <!-- Resolve / Toggle Button -->
        <div class="pt-2" style="border-top:1px solid rgba(255,255,255,0.06);">
          <button
            class="btn w-full justify-center gap-2"
            :style="(selectedRequest?.statut ?? selectedRequest?.Statut) === 'Terminé'
              ? 'background:rgba(99,102,241,0.1);color:#a5b4fc;border:1px solid rgba(99,102,241,0.25);'
              : 'background:rgba(34,197,94,0.1);color:#4ade80;border:1px solid rgba(34,197,94,0.25);'"
            @click="toggleMaintenanceStatus(selectedRequest); showIntervDetail = false; loadData()"
          >
            <ToggleRight v-if="(selectedRequest?.statut ?? selectedRequest?.Statut) === 'Terminé'" :size="15" />
            <ToggleLeft v-else :size="15" />
            {{ (selectedRequest?.statut ?? selectedRequest?.Statut) === 'Terminé' ? 'Marquer comme en cours' : 'Marquer comme terminé' }}
          </button>
        </div>
      </div>
    </Modal>
  </div>
</template>
