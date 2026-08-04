<script setup>
import { ref, computed, onMounted, watch } from 'vue'
import Modal from '../components/Modal.vue'
import { apiGet, apiPost, apiPut } from '../services/api'
import { useAuth } from '../services/auth'
import { useToast } from '../composables/useToast'
import { useSignalR } from '../services/signalr'
import { useExport } from '../composables/useExport'

const { state, isAdmin } = useAuth()
const { success, error: toastError } = useToast()
const { requests: signalRequests, startConnection } = useSignalR()
const { exportData } = useExport()

const items = ref([])
const materials = ref([])
const employees = ref([])
const loading = ref(true)
const exporting = ref(false)
const query = ref('')
const showAdd = ref(false)
const saving = ref(false)

const descPanne = ref('')
const matId = ref('')

const showIntervDetail = ref(false)
const selectedRequest = ref(null)
const interventions = ref([])
const loadingInterv = ref(false)
const intervComment = ref('')

const pendingCount = ref(0)

async function loadData() {
  loading.value = true
  try {
    const params = new URLSearchParams()
    if (query.value) params.set('q', query.value)
    const queryString = params.toString()
    const [reqs, mats, emps] = await Promise.all([
      isAdmin()
        ? apiGet(`/v1/mouvements/Requests${queryString ? `?${queryString}` : ''}`)
        : apiGet(`/v1/mouvements/Requests/${state.matricule}${queryString ? `?${queryString}` : ''}`),
      isAdmin() ? apiGet('/Materials') : apiGet(`/Materials/by-employe/${state.matricule}`),
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
  if (isAdmin()) startConnection()
})

let searchTimer = null
watch(query, () => {
  clearTimeout(searchTimer)
  searchTimer = setTimeout(loadData, 300)
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

function openAdd() {
  descPanne.value = ''
  matId.value = ''
  showAdd.value = true
}

async function submitAdd() {
  if (!descPanne.value || !matId.value) return
  saving.value = true
  try {
    await apiPost(`/v1/mouvements/Requests?DescPanne=${encodeURIComponent(descPanne.value)}&Matid=${matId.value}&matricule=${state.matricule}`)
    success('Demande d\'intervention créée.')
    showAdd.value = false
    loadData()
  } catch (e) {
    toastError(e.message || 'Erreur lors de la création.')
  } finally {
    saving.value = false
  }
}

async function startIntervention(row) {
  try {
    await apiPut(`/v1/mouvements/${row.id ?? row.Id}/start-intervention`)
    success('Intervention démarrée.')
    loadData()
  } catch (e) {
    toastError(e.message || 'Erreur.')
  }
}

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
    toastError(e.message || 'Erreur de chargement.')
  } finally {
    loadingInterv.value = false
  }
}

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
    toastError(e.message || 'Erreur.')
  } finally {
    saving.value = false
  }
}

async function resolveRequest() {
  if (!selectedRequest.value) return
  saving.value = true
  try {
    await apiPut(`/v1/mouvements/${selectedRequest.value.id ?? selectedRequest.value.Id}/resolve`)
    success('Demande résolue.')
    showIntervDetail.value = false
    loadData()
  } catch (e) {
    toastError(e.message || 'Erreur.')
  } finally {
    saving.value = false
  }
}

async function toggleMaintenanceStatus(row) {
  try {
    const res = await apiPut(`/v1/mouvements/${row.id ?? row.Id}/toggle-resolve`)
    const idx = items.value.findIndex(i => (i.id ?? i.Id) === (row.id ?? row.Id))
    if (idx !== -1) items.value[idx] = { ...items.value[idx], statut: res.statut }
    success(res.message || 'Statut mis à jour.')
  } catch (e) {
    toastError(e.message || 'Erreur.')
  }
}

const isEnCours = computed(() => {
  if (!selectedRequest.value) return false
  const s = selectedRequest.value.statut ?? selectedRequest.value.Statut
  return s === 'En cours' || s === 'Pas encore intervenu'
})

const filteredUserMats = computed(() => {
  const list = materials.value || []
  return list.filter(m => (m.employeMat ?? m.EmployeMat) === state.matricule)
})

async function exportRequests(format) {
  exporting.value = true
  try {
    const params = {}
    if (query.value) params.q = query.value
    if (!isAdmin()) params.matricule = state.matricule
    await exportData('requests', params, format)
  } catch (e) {
    toastError(e.message || "Erreur lors de l'export.")
  } finally {
    exporting.value = false
  }
}

function statusColor(s) {
  if (s === 'Pas encore intervenu') return 'status-chip maintenance'
  if (s === 'En cours') return 'status-chip en-stock-blue'
  if (s === 'Terminé') return 'status-chip affecte'
  return 'status-chip'
}

function statusIcon(s) {
  if (s === 'Pas encore intervenu') return 'schedule'
  if (s === 'En cours') return 'build'
  if (s === 'Terminé') return 'check_circle'
  return 'help'
}
</script>

<template>
  <div>
    <div class="flex justify-between items-center mb-6">
      <div>
        <h2 class="font-headline-md text-headline-md font-bold text-on-surface">
          {{ isAdmin() ? 'Demandes d\'intervention' : 'Mes demandes' }}
        </h2>
        <p class="font-label-sm text-label-sm text-on-surface-variant uppercase tracking-wider">
          {{ isAdmin() ? 'Gérez les demandes de maintenance' : 'Consultez et créez vos demandes' }}
        </p>
      </div>
      <div class="flex items-center gap-3">
        <span v-if="isAdmin()" class="status-chip maintenance">
          <span class="material-symbols-outlined text-[14px]">schedule</span>
          {{ pendingCount }} en attente
        </span>
        <button :disabled="exporting" @click="exportRequests('xlsx')" class="btn btn-ghost flex items-center gap-2">
          <span class="material-symbols-outlined text-[16px]">description</span>
          Excel
        </button>
        <button :disabled="exporting" @click="exportRequests('csv')" class="btn btn-ghost flex items-center gap-2">
          <span class="material-symbols-outlined text-[16px]">table_view</span>
          CSV
        </button>
        <button v-if="!isAdmin()" class="btn btn-primary flex items-center gap-2" @click="openAdd">
          <span class="material-symbols-outlined text-[16px]">add</span>
          Nouvelle demande
        </button>
      </div>
    </div>

    <div class="flex items-center gap-2 mb-4">
      <div class="relative flex-1 max-w-xs">
        <span class="material-symbols-outlined absolute left-3 top-1/2 -translate-y-1/2 text-on-surface-variant text-[16px]">search</span>
        <input v-model="query" class="w-full bg-surface border border-outline-variant rounded-lg pl-9 pr-3 py-1.5 font-label-sm text-on-surface focus:border-primary focus:ring-1 focus:ring-primary outline-none" placeholder="Filtrer..." type="text">
      </div>
      <span class="font-label-sm text-label-sm text-on-surface-variant">{{ items.length }} demande{{ items.length !== 1 ? 's' : '' }}</span>
    </div>

    <div v-if="loading" class="flex items-center justify-center py-24"><div class="spinner" /></div>

    <div v-else-if="items.length === 0" class="bg-surface-container border border-outline-variant rounded-xl p-12 text-center">
      <span class="material-symbols-outlined text-[48px] text-on-surface-variant">assignment</span>
      <p class="font-body-md text-body-md text-on-surface-variant mt-3">Aucune demande pour le moment.</p>
    </div>

    <div v-else class="bg-surface-container border border-outline-variant rounded-xl overflow-hidden">
      <div class="overflow-x-auto">
        <table class="data-table">
          <thead>
            <tr>
              <th>Date</th>
              <th>Matériel</th>
              <th>Description</th>
              <th>Statut</th>
              <th v-if="isAdmin()">Demandeur</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="row in items" :key="row.id ?? row.Id">
              <td class="font-label-sm text-label-sm text-on-surface-variant whitespace-nowrap">{{ formatDate(row.dateMouvement ?? row.DateMouvement) }}</td>
              <td class="font-body-md text-body-md text-on-surface">{{ materialName(row.materielId ?? row.MaterielId) }}</td>
              <td class="font-body-sm text-body-sm text-on-surface-variant max-w-xs truncate">{{ row.commentaires || '—' }}</td>
              <td>
                <span :class="statusColor(row.statut ?? row.Statut)" class="inline-flex items-center gap-1">
                  <span class="material-symbols-outlined text-[14px]">{{ statusIcon(row.statut ?? row.Statut) }}</span>
                  {{ row.statut ?? row.Statut }}
                </span>
              </td>
              <td v-if="isAdmin()" class="font-body-sm text-body-sm text-on-surface-variant whitespace-nowrap">{{ employeeName(row.employeMat ?? row.EmployeMat) || '—' }}</td>
              <td>
                <div class="flex items-center gap-2">
                  <button v-if="(row.statut ?? row.Statut) === 'Pas encore intervenu' && isAdmin()" class="btn btn-primary btn-sm flex items-center gap-1.5" @click="startIntervention(row)">
                    <span class="material-symbols-outlined text-[14px]">build</span>
                    Intervenir
                  </button>
                  <button v-else-if="((row.statut ?? row.Statut) === 'En cours' || (row.statut ?? row.Statut) === 'Terminé') && isAdmin()" class="btn btn-sm" style="background:rgba(173,198,255,0.1);color:#adc6ff;border:1px solid rgba(173,198,255,0.25);" @click="openIntervDetail(row)">
                    <span class="material-symbols-outlined text-[14px]">visibility</span>
                    Ouvrir
                  </button>
                  <span v-else class="font-label-sm text-label-sm text-on-surface-variant">—</span>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <Modal v-if="showAdd" title="Nouvelle demande" confirm-text="Envoyer" :loading="saving" @close="showAdd = false" @confirm="submitAdd">
      <div class="space-y-3">
        <p class="font-body-sm text-body-sm text-on-surface-variant">Sélectionnez le matériel et décrivez le problème.</p>
        <div class="form-group">
          <label class="form-label">Matériel</label>
          <select v-model="matId" class="form-input">
            <option value="">— Choisir —</option>
            <option v-for="m in filteredUserMats" :key="m.id ?? m.Id" :value="m.id ?? m.Id">
              {{ m.libelle ?? m.Libelle }} — {{ m.marque ?? m.Marque }}
            </option>
          </select>
        </div>
        <div class="form-group">
          <label class="form-label">Description</label>
          <textarea v-model="descPanne" class="form-input" rows="3" placeholder="Décrivez le problème…" />
        </div>
      </div>
    </Modal>

    <Modal v-if="showIntervDetail" :title="`Intervention — ${materialName(selectedRequest?.materielId)}`" size="lg" :hide-footer="!isEnCours" :confirm-text="isEnCours ? 'Ajouter' : ''" :loading="saving" @close="showIntervDetail = false" @confirm="submitInterv">
      <div class="space-y-5">
        <div class="bg-surface-container-low border border-outline-variant rounded-lg p-3">
          <div class="font-label-sm text-label-sm text-on-surface-variant mb-1">Description de la panne</div>
          <p class="font-body-sm text-body-sm text-on-surface">{{ selectedRequest?.commentaires || 'Aucune description.' }}</p>
        </div>

        <div>
          <div class="flex items-center gap-2 mb-3">
            <span class="material-symbols-outlined text-primary text-[16px]">list</span>
            <span class="font-body-md text-body-md font-bold text-on-surface">
              Interventions
              <span v-if="interventions.length" class="font-label-sm bg-surface-container-high px-2 py-0.5 rounded border border-outline-variant text-on-surface-variant ml-1">{{ interventions.length }}</span>
            </span>
          </div>

          <div v-if="loadingInterv" class="flex items-center justify-center py-6"><div class="spinner" /></div>
          <div v-else-if="interventions.length === 0" class="font-body-sm text-body-sm text-on-surface-variant py-4 text-center">Aucune intervention enregistrée.</div>
          <div v-else class="space-y-2 max-h-48 overflow-y-auto pr-1">
            <div v-for="(interv, idx) in interventions" :key="interv.id ?? interv.Id" class="bg-surface-container-low border border-outline-variant rounded-lg p-3 flex items-start gap-3">
              <div class="w-7 h-7 rounded-full bg-primary-container/20 border border-primary/30 flex items-center justify-center shrink-0 mt-0.5">
                <span class="font-label-sm text-label-sm text-primary font-bold">{{ idx + 1 }}</span>
              </div>
              <div class="flex-1 min-w-0">
                <div class="flex items-center gap-2 mb-1">
                  <span class="font-label-sm text-label-sm text-on-surface-variant">{{ formatDate(interv.date ?? interv.Date) }}</span>
                  <span v-if="interv.intervenantId ?? interv.IntervenantId" class="font-label-sm text-label-sm text-on-surface-variant">— {{ employeeName(interv.intervenantId ?? interv.IntervenantId) }}</span>
                </div>
                <p class="font-body-sm text-body-sm text-on-surface">{{ interv.commentaires || interv.Commentaires || '—' }}</p>
              </div>
            </div>
          </div>
        </div>

        <div v-if="isEnCours" class="form-group">
          <label class="form-label">Nouvelle intervention</label>
          <textarea v-model="intervComment" class="form-input" rows="3" placeholder="Décrivez l'intervention…" />
        </div>

        <div class="pt-2 border-t border-outline-variant/30 flex gap-2">
          <button v-if="selectedRequest?.statut !== 'Terminé'" class="btn w-full justify-center gap-2" style="background:rgba(78,222,163,0.1);color:#4edea3;border:1px solid rgba(78,222,163,0.25);" @click="resolveRequest()">
            <span class="material-symbols-outlined text-[16px]">check_circle</span>
            Résoudre
          </button>
        </div>
      </div>
    </Modal>
  </div>
</template>
