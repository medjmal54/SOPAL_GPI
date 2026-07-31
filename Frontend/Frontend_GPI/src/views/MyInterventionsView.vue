<script setup>
import { ref, computed, onMounted } from 'vue'
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

const showAddSingle = ref(false)
const addSingleRequest = ref(null)
const addSingleComment = ref('')
const addSingleSaving = ref(false)

async function loadData() {
  loading.value = true
  try {
    const [reqs, mats, emps] = await Promise.all([
      apiGet('/v1/mouvements/my-interventions'), apiGet('/Materials'), apiGet('/Employes'),
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

function isTerminated(statut) { return statut === 'Terminé' }

const showDone = ref(false)

const activeCount = computed(() => {
  const list = items.value || []
  return showDone.value
    ? list.filter(i => isTerminated(i.statut)).length
    : list.filter(i => !isTerminated(i.statut)).length
})

const tabFiltered = computed(() => {
  const list = items.value || []
  let result = showDone.value ? list.filter(i => isTerminated(i.statut)) : list.filter(i => !isTerminated(i.statut))
  if (query.value) {
    const q = query.value.toLowerCase()
    result = result.filter(item => {
      const search = [item.commentaires, item.statut, item.dateMouvement, materialName(item.materielId)].filter(Boolean).join(' ').toLowerCase()
      return search.includes(q)
    })
  }
  return result
})

async function loadInterventions(mouvementId) {
  loadingInterv.value = true
  interventions.value = []
  try {
    const data = await apiGet(`/Intervention/by-mouvement/${mouvementId}`)
    interventions.value = Array.isArray(data) ? data : []
  } catch (e) { toastError(e.message || 'Erreur de chargement.')
  } finally { loadingInterv.value = false }
}

function openDetail(row) {
  selectedRequest.value = row
  showDetail.value = true
  loadInterventions(row.id ?? row.Id)
}

async function toggleStatus(row) {
  const id = row.id ?? row.Id
  try {
    const res = await apiPut(`/v1/mouvements/${id}/toggle-resolve`)
    const list = items.value || []
    const idx = list.findIndex(i => (i.id ?? i.Id) === id)
    if (idx !== -1) list[idx] = { ...list[idx], statut: res.statut }
    success(res.message || 'Statut mis à jour.')
  } catch (e) { toastError(e.message || 'Erreur.') }
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
      matId: addSingleRequest.value.materielId ?? addSingleRequest.value.MaterielId,
      maintId: addSingleRequest.value.id ?? addSingleRequest.value.Id,
    })
    success('Intervention enregistrée.')
    showAddSingle.value = false
  } catch (e) { toastError(e.message || 'Erreur.')
  } finally { addSingleSaving.value = false }
}
</script>

<template>
  <div>
    <div class="flex justify-between items-center mb-6">
      <div>
        <h2 class="font-headline-md text-headline-md font-bold text-on-surface">Mes Interventions</h2>
        <p class="font-label-sm text-label-sm text-on-surface-variant uppercase tracking-wider">Demandes prises en charge</p>
      </div>
      <div class="flex items-center gap-2">
        <span class="status-chip" :class="showDone ? 'affecte' : 'en-stock-blue'">
          {{ activeCount }} {{ showDone ? 'terminés' : 'en cours' }}
        </span>
        <span class="font-label-sm bg-surface-container-high px-2 py-0.5 rounded border border-outline-variant text-on-surface-variant">
          Total {{ items.value?.length ?? 0 }}
        </span>
      </div>
    </div>

    <div class="flex items-center gap-2 mb-4">
      <button @click="showDone = false" class="px-4 py-1.5 rounded-lg font-label-md text-label-md transition" :class="!showDone ? 'bg-secondary-container text-on-secondary-container font-bold' : 'text-on-surface-variant border border-outline-variant hover:bg-surface-container-high'">
        En cours
      </button>
      <button @click="showDone = true" class="px-4 py-1.5 rounded-lg font-label-md text-label-md transition" :class="showDone ? 'bg-tertiary/20 text-tertiary font-bold' : 'text-on-surface-variant border border-outline-variant hover:bg-surface-container-high'">
        Terminés
      </button>
      <div class="relative flex-1 max-w-xs ml-auto">
        <span class="material-symbols-outlined absolute left-3 top-1/2 -translate-y-1/2 text-on-surface-variant text-[16px]">search</span>
        <input v-model="query" class="w-full bg-surface border border-outline-variant rounded-lg pl-9 pr-3 py-1.5 font-label-sm text-on-surface focus:border-primary focus:ring-1 focus:ring-primary outline-none" placeholder="Filtrer..." type="text">
      </div>
    </div>

    <div v-if="loading" class="flex items-center justify-center py-24"><div class="spinner" /></div>

    <div v-else-if="tabFiltered.length === 0" class="bg-surface-container border border-outline-variant rounded-xl p-12 text-center">
      <span class="material-symbols-outlined text-[48px] text-on-surface-variant">handyman</span>
      <p class="font-body-md text-body-md text-on-surface-variant mt-3">Aucune intervention assignée.</p>
    </div>

    <div v-else class="space-y-3">
      <div v-for="row in tabFiltered" :key="row.id ?? row.Id" class="bg-surface-container border border-outline-variant rounded-lg p-4 hover:bg-surface-container-high transition-colors">
        <div class="flex items-start justify-between gap-4">
          <div class="flex items-start gap-3 flex-1 min-w-0">
            <div class="w-9 h-9 rounded-full bg-surface-container-highest border border-outline-variant flex items-center justify-center shrink-0" :class="isTerminated(row.statut) ? 'border-tertiary/30' : 'border-primary/30'">
              <span class="material-symbols-outlined text-[18px]" :class="isTerminated(row.statut) ? 'text-tertiary' : 'text-primary'">handyman</span>
            </div>
            <div class="flex-1 min-w-0">
              <div class="flex items-center gap-2 mb-1">
                <span class="font-label-md text-label-md font-bold text-on-surface">{{ materialName(row.materielId ?? row.MaterielId) }}</span>
                <span :class="isTerminated(row.statut) ? 'status-chip affecte' : 'status-chip en-stock-blue'">{{ row.statut || 'En cours' }}</span>
              </div>
              <p class="font-body-sm text-body-sm text-on-surface-variant truncate">{{ row.commentaires || 'Aucune description' }}</p>
              <div class="flex items-center gap-2 mt-1">
                <span class="font-label-sm text-label-sm text-on-surface-variant">{{ formatDate(row.dateMouvement ?? row.DateMouvement) }}</span>
              </div>
            </div>
          </div>
          <div class="flex items-center gap-2 shrink-0">
            <button @click="openAddSingle(row)" v-if="!isTerminated(row.statut)" class="btn btn-sm" style="background:rgba(78,222,163,0.1);color:#4edea3;border:1px solid rgba(78,222,163,0.3);">
              <span class="material-symbols-outlined text-[14px]">add</span> Ajouter
            </button>
            <button @click="openDetail(row)" class="btn btn-sm btn-ghost">
              <span class="material-symbols-outlined text-[14px]">visibility</span> Détails
            </button>
            <button @click="toggleStatus(row)" class="btn btn-sm" :class="isTerminated(row.statut) ? 'btn-ghost' : 'btn-primary'">
              <span class="material-symbols-outlined text-[14px]">{{ isTerminated(row.statut) ? 'undo' : 'check_circle' }}</span>
            </button>
          </div>
        </div>
      </div>
    </div>

    <Modal v-if="showDetail" :title="`Intervention — ${materialName(selectedRequest?.materielId)}`" size="lg" hide-footer @close="showDetail = false">
      <div class="space-y-5">
        <div class="bg-surface-container-low border border-outline-variant rounded-lg p-3">
          <div class="font-label-sm text-label-sm text-on-surface-variant mb-1">Description</div>
          <p class="font-body-sm text-body-sm text-on-surface">{{ selectedRequest?.commentaires || 'Aucune description.' }}</p>
        </div>
        <div>
          <div class="flex items-center gap-2 mb-2">
            <span class="material-symbols-outlined text-primary text-[18px]">list</span>
            <span class="font-headline-sm text-headline-sm text-on-surface">Interventions</span>
            <span v-if="interventions.length" class="font-label-sm bg-surface-container-high px-2 py-0.5 rounded border border-outline-variant text-on-surface-variant ml-1">{{ interventions.length }}</span>
          </div>
          <div v-if="loadingInterv" class="flex items-center justify-center py-6"><div class="spinner" /></div>
          <div v-else-if="interventions.length === 0" class="font-body-sm text-body-sm text-on-surface-variant py-4 text-center">Aucune intervention.</div>
          <div v-else class="space-y-2 max-h-48 overflow-y-auto">
            <div v-for="(interv, idx) in interventions" :key="interv.id" class="bg-surface-container-low border border-outline-variant rounded-lg p-3 flex items-start gap-3">
              <div class="w-7 h-7 rounded-full bg-primary-container/20 border border-primary/30 flex items-center justify-center shrink-0 mt-0.5">
                <span class="font-label-sm text-label-sm text-primary font-bold">{{ idx + 1 }}</span>
              </div>
              <div class="flex-1 min-w-0">
                <div class="flex items-center gap-2 mb-1">
                  <span class="font-label-sm text-label-sm text-on-surface-variant">{{ formatDate(interv.date) }}</span>
                  <span v-if="interv.intervenantId" class="font-label-sm text-label-sm text-on-surface-variant">— {{ employeeName(interv.intervenantId) }}</span>
                </div>
                <p class="font-body-md text-body-md text-on-surface">{{ interv.commentaires }}</p>
              </div>
            </div>
          </div>
        </div>
        <div class="pt-2 border-t border-outline-variant/30">
          <button @click="toggleStatus(selectedRequest); showDetail = false; loadData()" class="btn w-full justify-center gap-2" :class="isTerminated(selectedRequest?.statut) ? 'btn-ghost' : 'btn-primary'">
            <span class="material-symbols-outlined text-[16px]">{{ isTerminated(selectedRequest?.statut) ? 'undo' : 'check_circle' }}</span>
            {{ isTerminated(selectedRequest?.statut) ? 'Marquer comme en cours' : 'Marquer comme terminé' }}
          </button>
        </div>
      </div>
    </Modal>

    <Modal v-if="showAddSingle" :title="`Nouvelle intervention — ${materialName(addSingleRequest?.materielId)}`" confirm-text="Ajouter" :loading="addSingleSaving" @close="showAddSingle = false" @confirm="submitAddSingle">
      <div class="space-y-4">
        <div class="bg-surface-container-low border border-outline-variant rounded-lg p-3">
          <div class="font-label-sm text-label-sm text-on-surface-variant mb-1">Description</div>
          <p class="font-body-sm text-body-sm text-on-surface">{{ addSingleRequest?.commentaires || 'Aucune description.' }}</p>
        </div>
        <div class="form-group">
          <label class="form-label">Intervention</label>
          <textarea v-model="addSingleComment" class="form-input" rows="3" placeholder="Décrivez l'intervention réalisée…"></textarea>
        </div>
      </div>
    </Modal>
  </div>
</template>
