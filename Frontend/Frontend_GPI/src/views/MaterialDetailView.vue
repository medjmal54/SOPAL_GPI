<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import Modal from '../components/Modal.vue'
import { apiGet, apiPost } from '../services/api'
import { useToast } from '../composables/useToast'
import { useAuth } from '../services/auth'

const { success, error: toastError } = useToast()
const { state: authState, isAdmin } = useAuth()
const route = useRoute()
const router = useRouter()

const loading = ref(true)
const material = ref(null)
const movements = ref([])
const locations = ref([])
const matypes = ref([])
const employees = ref([])
const loadingMovs = ref(false)

const showAffecter = ref(false)
const showTransfert = ref(false)
const showRetour = ref(false)
const saving = ref(false)
const empMat = ref('')
const localId = ref('')

const showIntervDetail = ref(false)
const selectedMouvement = ref(null)
const interventions = ref([])
const loadingInterv = ref(false)
const intervComment = ref('')
const savingInterv = ref(false)

const matId = computed(() => Number(route.params.id))

const typmovLabel = (t) => ({ 1: 'Affectation', 2: 'Retour', 3: 'Transfert', 4: 'Maintenance' })[t] ?? String(t ?? '—')
const typmovBadge = (t) => ({ 1: 'status-chip affecte', 2: 'status-chip maintenance', 3: 'status-chip en-stock-blue', 4: 'status-chip hors-service' })[t] ?? 'status-chip'
const typmovIcon = (t) => ({ 1: 'person_add', 2: 'assignment_return', 3: 'move_down', 4: 'build' })[t] ?? 'swap_horiz'
const typmovDotClass = (t) => ({ 1: 'border-tertiary', 2: 'border-secondary', 3: 'border-primary', 4: 'border-outline' })[t] ?? 'border-outline'
const typmovDotIcon = (t) => ({ 1: 'person_add', 2: 'assignment_return', 3: 'move_down', 4: 'build' })[t] ?? 'swap_horiz'

function localisationName(id) {
  const found = locations.value.find(l => (l.id ?? l.Id) === id)
  return found ? (found.Nom ?? found.nom ?? `#${id}`) : `#${id}`
}
function typeName(id) {
  const found = matypes.value.find(t => (t.id ?? t.Id) === id)
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
  return date.toLocaleDateString('fr-FR', { day: '2-digit', month: 'short', year: 'numeric' })
}
function formatDateTime(d) {
  if (!d) return '—'
  const date = new Date(d)
  if (Number.isNaN(date.getTime())) return '—'
  return date.toLocaleDateString('fr-FR', { day: '2-digit', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit' })
}
function raw(mat, k1, k2) { return mat[k1] ?? mat[k2] ?? null }

const matDetail = computed(() => {
  const m = material.value
  if (!m) return null
  return {
    libelle: raw(m, 'libelle', 'Libelle') || '—',
    marque: raw(m, 'marque', 'Marque') || '—',
    modele: raw(m, 'modele', 'Modele') || '—',
    numSerie: raw(m, 'numSerie', 'NumSerie') || '—',
    dateAcquisition: raw(m, 'dateAcquisition', 'DateAcquisition'),
    actif: raw(m, 'actif', 'Actif'),
    commentaires: raw(m, 'commentaires', 'Commentaires'),
    dateModif: raw(m, 'dateModif', 'DateModif'),
    typeId: raw(m, 'typeMaterielId', 'TypeMaterielId'),
    localisationId: raw(m, 'localisationId', 'LocalisationId'),
    employeMat: raw(m, 'employeMat', 'EmployeMat'),
  }
})

async function loadAll() {
  loading.value = true
  try {
    const [mats, locs, types, emps] = await Promise.all([
      apiGet('/Materials'), apiGet('/Localistations'), apiGet('/Matypes'), apiGet('/Employes'),
    ])
    locations.value = Array.isArray(locs) ? locs : []
    matypes.value = Array.isArray(types) ? types : []
    employees.value = Array.isArray(emps) ? emps : []
    const allMats = Array.isArray(mats) ? mats : []
    material.value = allMats.find(m => (m.id ?? m.Id) === matId.value) ?? null
    if (!material.value) {
      toastError('Matériel introuvable.')
      router.replace('/materiels')
      return
    }
    await loadMovements()
  } catch (e) {
    toastError(e.message || 'Erreur de chargement.')
  } finally {
    loading.value = false
  }
}

async function loadMovements() {
  loadingMovs.value = true
  try {
    const data = await apiGet(`/v1/mouvements/${matId.value}/details`)
    movements.value = (Array.isArray(data) ? data : []).sort((a, b) => {
      const da = new Date(a.dateMouvement ?? a.DateMouvement ?? 0)
      const db = new Date(b.dateMouvement ?? b.DateMouvement ?? 0)
      return db - da
    })
  } catch (e) {
    toastError(e.message || 'Erreur de chargement des mouvements.')
  } finally {
    loadingMovs.value = false
  }
}

async function reloadMaterial() {
  try { const m = await apiGet(`/Materials/${matId.value}`); if (m) material.value = m } catch {}
}

function openAffecter() { empMat.value = ''; showAffecter.value = true }
async function submitAffecter() {
  if (!empMat.value) return
  saving.value = true
  try {
    await apiPost(`/v1/mouvements/${matId.value}/affecter/${empMat.value}`)
    success(`Matériel affecté.`); showAffecter.value = false
    await Promise.all([reloadMaterial(), loadMovements()])
  } catch (e) { toastError(e.message || 'Affectation impossible.')
  } finally { saving.value = false }
}

function openTransfert() { localId.value = ''; showTransfert.value = true }
async function submitTransfert() {
  if (!localId.value) return
  saving.value = true
  try {
    await apiPost(`/v1/mouvements/${matId.value}/transfert/${localId.value}`)
    success('Transfert validé.'); showTransfert.value = false
    await Promise.all([reloadMaterial(), loadMovements()])
  } catch (e) { toastError(e.message || 'Transfert impossible.')
  } finally { saving.value = false }
}

function openRetour() { showRetour.value = true }
async function submitRetour() {
  saving.value = true
  try {
    await apiPost(`/v1/mouvements/${matId.value}/retour`)
    success('Retour validé.'); showRetour.value = false
    await Promise.all([reloadMaterial(), loadMovements()])
  } catch (e) { toastError(e.message || 'Retour impossible.')
  } finally { saving.value = false }
}

async function openIntervDetail(row) {
  selectedMouvement.value = row
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
  } catch (e) { toastError(e.message || 'Erreur de chargement.')
  } finally { loadingInterv.value = false }
}

async function submitInterv() {
  if (!intervComment.value || !selectedMouvement.value) return
  savingInterv.value = true
  try {
    await apiPost('/Intervention', {
      commentaires: intervComment.value,
      date: new Date().toISOString().split('T')[0],
      matId: selectedMouvement.value.materielId ?? selectedMouvement.value.MaterielId ?? matId.value,
      maintId: selectedMouvement.value.id ?? selectedMouvement.value.Id,
    })
    success('Intervention enregistrée.')
    intervComment.value = ''
    await loadInterventions(selectedMouvement.value.id ?? selectedMouvement.value.Id)
  } catch (e) { toastError(e.message || 'Erreur.')
  } finally { savingInterv.value = false }
}

onMounted(loadAll)
</script>

<template>
  <div>
    <div v-if="loading" class="flex items-center justify-center py-24">
      <div class="flex flex-col items-center gap-4">
        <div class="spinner" />
        <p class="font-body-sm text-body-sm text-on-surface-variant">Chargement du matériel…</p>
      </div>
    </div>

    <template v-else-if="matDetail">
      <button class="inline-flex items-center gap-2 font-body-sm text-body-sm text-on-surface-variant hover:text-on-surface transition-colors mb-4" @click="router.push('/materiels')">
        <span class="material-symbols-outlined text-[16px]">arrow_back</span>
        Retour aux matériels
      </button>

      <div class="bg-surface border border-outline-variant rounded-xl p-container-padding mb-6">
        <div class="flex justify-between items-start flex-wrap gap-3">
          <div class="flex gap-stack-gap">
            <div class="w-20 h-20 bg-surface-container-high rounded-lg border border-outline-variant flex items-center justify-center shrink-0">
              <span class="material-symbols-outlined text-[40px] text-primary">laptop_mac</span>
            </div>
            <div>
              <div class="flex items-center gap-compact-gap mb-unit">
                <h1 class="font-headline-md text-headline-md text-on-surface">{{ matDetail.libelle }}</h1>
                <span :class="matDetail.actif ? 'status-chip affecte' : 'status-chip hors-service'">{{ matDetail.actif ? 'Actif' : 'Inactif' }}</span>
              </div>
              <p class="font-body-sm text-body-sm text-on-surface-variant">{{ matDetail.marque }} · {{ matDetail.modele }}</p>
              <div class="flex gap-compact-gap mt-unit">
                <span class="font-label-sm text-label-sm text-primary font-mono">N° {{ matDetail.numSerie }}</span>
              </div>
            </div>
          </div>
          <span class="font-label-sm text-label-sm text-on-surface-variant">Acquis le {{ formatDate(matDetail.dateAcquisition) }}</span>
        </div>
      </div>

      <div v-if="isAdmin()" class="flex gap-3 mb-6 flex-wrap">
        <button v-if="!matDetail.employeMat" @click="openAffecter" class="btn" style="background:rgba(78,222,163,0.1);color:#4edea3;border:1px solid rgba(78,222,163,0.3);">
          <span class="material-symbols-outlined text-[16px]">person_add</span> Affecter
        </button>
        <button @click="openTransfert" class="btn btn-ghost">
          <span class="material-symbols-outlined text-[16px]">swap_horiz</span> Transférer
        </button>
        <button v-if="matDetail.employeMat" @click="openRetour" class="btn" style="background:rgba(255,179,173,0.1);color:#ffb3ad;border:1px solid rgba(255,179,173,0.3);">
          <span class="material-symbols-outlined text-[16px]">assignment_return</span> Retour
        </button>
      </div>

      <div class="bg-surface border border-outline-variant rounded-xl p-container-padding mb-6">
        <h3 class="font-headline-sm text-headline-sm text-on-surface mb-stack-gap flex items-center gap-compact-gap border-b border-outline-variant pb-unit">
          <span class="material-symbols-outlined text-primary">description</span> Fiche Technique
        </h3>
        <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-x-gutter gap-y-stack-gap">
          <div class="flex flex-col border-b border-outline-variant/30 pb-unit">
            <span class="font-label-sm text-label-sm text-on-surface-variant">Libellé</span>
            <span class="font-body-md text-body-md text-on-surface">{{ matDetail.libelle }}</span>
          </div>
          <div class="flex flex-col border-b border-outline-variant/30 pb-unit">
            <span class="font-label-sm text-label-sm text-on-surface-variant">Marque</span>
            <span class="font-body-md text-body-md text-on-surface">{{ matDetail.marque }}</span>
          </div>
          <div class="flex flex-col border-b border-outline-variant/30 pb-unit">
            <span class="font-label-sm text-label-sm text-on-surface-variant">Modèle</span>
            <span class="font-body-md text-body-md text-on-surface">{{ matDetail.modele }}</span>
          </div>
          <div class="flex flex-col border-b border-outline-variant/30 pb-unit">
            <span class="font-label-sm text-label-sm text-on-surface-variant">N° de Série</span>
            <span class="font-body-md text-body-md text-on-surface">{{ matDetail.numSerie }}</span>
          </div>
          <div class="flex flex-col border-b border-outline-variant/30 pb-unit">
            <span class="font-label-sm text-label-sm text-on-surface-variant">Date d'Acquisition</span>
            <span class="font-body-md text-body-md text-on-surface">{{ formatDate(matDetail.dateAcquisition) }}</span>
          </div>
          <div class="flex flex-col border-b border-outline-variant/30 pb-unit">
            <span class="font-label-sm text-label-sm text-on-surface-variant">Type</span>
            <span class="font-body-md text-body-md text-on-surface">{{ typeName(matDetail.typeId) }}</span>
          </div>
          <div class="flex flex-col border-b border-outline-variant/30 pb-unit">
            <span class="font-label-sm text-label-sm text-on-surface-variant">Localisation</span>
            <span class="font-body-md text-body-md text-on-surface">{{ localisationName(matDetail.localisationId) }}</span>
          </div>
          <div class="flex flex-col border-b border-outline-variant/30 pb-unit">
            <span class="font-label-sm text-label-sm text-on-surface-variant">Employé</span>
            <span class="font-body-md text-body-md text-on-surface">{{ matDetail.employeMat ? employeeName(matDetail.employeMat) : 'Non affecté' }}</span>
          </div>
          <div class="flex flex-col border-b border-outline-variant/30 pb-unit">
            <span class="font-label-sm text-label-sm text-on-surface-variant">Dernière MAJ</span>
            <span class="font-body-md text-body-md text-on-surface">{{ formatDateTime(matDetail.dateModif) }}</span>
          </div>
        </div>
        <div class="mt-stack-gap flex flex-col gap-unit">
          <span class="font-label-sm text-label-sm text-on-surface-variant">Commentaires</span>
          <div class="bg-surface-container-low p-compact-gap rounded-lg border border-outline-variant font-body-sm text-body-sm text-on-surface-variant">
            {{ matDetail.commentaires || 'Aucun commentaire.' }}
          </div>
        </div>
      </div>

      <div class="bg-surface border border-outline-variant rounded-xl p-container-padding">
        <div class="flex justify-between items-center mb-stack-gap">
          <h3 class="font-headline-sm text-headline-sm text-on-surface flex items-center gap-compact-gap">
            <span class="material-symbols-outlined text-primary">history</span> Historique des Mouvements
          </h3>
          <span v-if="movements.length" class="font-label-sm px-2 py-0.5 bg-surface-container-high rounded border border-outline-variant text-on-surface-variant">{{ movements.length }}</span>
        </div>

        <div v-if="loadingMovs" class="flex items-center justify-center py-8">
          <div class="spinner" />
        </div>

        <div v-else-if="movements.length === 0" class="flex flex-col items-center py-10 text-on-surface-variant">
          <span class="material-symbols-outlined text-[36px] mb-2">history</span>
          <span class="font-body-sm text-body-sm">Aucun mouvement enregistré.</span>
        </div>

        <div v-else class="flex flex-col gap-stack-gap relative before:absolute before:left-[17px] before:top-4 before:bottom-4 before:w-0.5 before:bg-outline-variant">
          <div v-for="(m, i) in movements" :key="m.id ?? i" class="flex gap-stack-gap relative z-10">
            <div class="w-9 h-9 rounded-full bg-surface border-2 flex items-center justify-center shrink-0" :class="typmovDotClass(m.typmov ?? m.Typmov)">
              <span class="material-symbols-outlined text-[18px]" :class="{'text-primary': (m.typmov ?? 3) === 3, 'text-tertiary': (m.typmov ?? 1) === 1, 'text-secondary': (m.typmov ?? 2) === 2}">{{ typmovDotIcon(m.typmov ?? m.Typmov) }}</span>
            </div>
            <div class="flex-1 bg-surface-container-low border border-outline-variant rounded-lg p-stack-gap">
              <div class="flex justify-between items-start mb-unit gap-2 flex-wrap">
                <div class="flex items-center gap-2 flex-wrap">
                  <span :class="typmovBadge(m.typmov ?? m.Typmov)">{{ typmovLabel(m.typmov ?? m.Typmov) }}</span>
                  <span class="font-label-md text-label-md text-on-surface font-bold">{{ m.typmov === 1 ? 'Affectation' : m.typmov === 3 ? 'Changement localisation' : m.typmov === 2 ? 'Retour' : 'Maintenance' }}</span>
                </div>
                <span class="font-label-sm text-label-sm text-on-surface-variant">{{ formatDateTime(m.dateMouvement ?? m.DateMouvement) }}</span>
              </div>
              <div class="grid grid-cols-1 sm:grid-cols-2 gap-stack-gap mt-unit">
                <div v-if="(m.typmov ?? m.Typmov) !== 1">
                  <span class="block font-label-sm text-on-surface-variant uppercase">Ancienne Loc.</span>
                  <span class="font-body-sm text-body-sm text-on-surface-variant">—</span>
                </div>
                <div>
                  <span class="block font-label-sm text-on-surface-variant uppercase">{{ (m.typmov ?? m.Typmov) === 1 ? 'Employé' : 'Nouvelle Localisation' }}</span>
                  <span class="font-body-sm text-body-sm text-on-surface">
                    {{ (m.typmov ?? m.Typmov) === 1 ? employeeName(m.employeMat) : localisationName(m.localisationId ?? m.LocalisationId) }}
                  </span>
                </div>
              </div>
              <div v-if="m.commentaires" class="mt-compact-gap border-t border-outline-variant/30 pt-unit">
                <span class="font-label-sm text-label-sm text-on-surface-variant italic flex items-center gap-unit">
                  <span class="material-symbols-outlined text-[14px]">notes</span>
                  {{ m.commentaires }}
                </span>
              </div>
              <div v-if="(m.typmov ?? m.Typmov) === 4" class="mt-2">
                <button @click="openIntervDetail(m)" class="btn btn-sm btn-ghost">
                  <span class="material-symbols-outlined text-[14px]">build</span> Interventions
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>
    </template>

    <Modal v-if="showAffecter" title="Affecter un matériel" confirm-text="Affecter" :loading="saving" size="sm" @close="showAffecter = false" @confirm="submitAffecter">
      <div class="space-y-3">
        <p class="font-body-sm text-body-sm text-on-surface-variant">Matériel : <strong class="text-on-surface">{{ matDetail?.libelle }}</strong></p>
        <div class="form-group">
          <label class="form-label">Employé</label>
          <select v-model="empMat" class="form-input">
            <option value="">— Choisir un employé —</option>
            <option v-for="emp in employees" :key="emp.matricule ?? emp.Matricule" :value="emp.matricule ?? emp.Matricule">{{ emp.nom ?? emp.Nom }} {{ emp.prenom ?? emp.Prenom }}</option>
          </select>
        </div>
      </div>
    </Modal>

    <Modal v-if="showTransfert" title="Transfert de localisation" confirm-text="Transférer" :loading="saving" size="sm" @close="showTransfert = false" @confirm="submitTransfert">
      <div class="space-y-3">
        <p class="font-body-sm text-body-sm text-on-surface-variant">Matériel : <strong class="text-on-surface">{{ matDetail?.libelle }}</strong></p>
        <div class="form-group">
          <label class="form-label">Nouvelle localisation</label>
          <select v-model="localId" class="form-input">
            <option value="">— Choisir une localisation —</option>
            <option v-for="loc in locations" :key="loc.id ?? loc.Id" :value="loc.id ?? loc.Id">{{ loc.nom ?? loc.Nom }}</option>
          </select>
        </div>
      </div>
    </Modal>

    <Modal v-if="showRetour" title="Retour matériel" confirm-text="Confirmer le retour" :loading="saving" size="sm" @close="showRetour = false" @confirm="submitRetour">
      <p class="font-body-sm text-body-sm text-on-surface-variant">Confirmer le retour de <strong class="text-on-surface">{{ matDetail?.libelle }}</strong> ?</p>
    </Modal>

    <Modal v-if="showIntervDetail" :title="`Interventions — ${matDetail?.libelle}`" size="lg" confirm-text="Ajouter" :loading="savingInterv" @close="showIntervDetail = false" @confirm="submitInterv">
      <div class="space-y-5">
        <div class="bg-surface-container-low border border-outline-variant rounded-lg p-3">
          <div class="font-label-sm text-label-sm text-on-surface-variant mb-1">Description</div>
          <p class="font-body-sm text-body-sm text-on-surface">{{ selectedMouvement?.commentaires || 'Aucune description.' }}</p>
        </div>
        <div>
          <div class="flex items-center gap-2 mb-3">
            <span class="material-symbols-outlined text-primary text-[18px]">list</span>
            <span class="font-headline-sm text-headline-sm text-on-surface">Interventions</span>
          </div>
          <div v-if="loadingInterv" class="flex items-center justify-center py-6"><div class="spinner" /></div>
          <div v-else-if="interventions.length === 0" class="font-body-sm text-body-sm text-on-surface-variant py-4 text-center">Aucune intervention.</div>
          <div v-else class="space-y-2 max-h-48 overflow-y-auto">
            <div v-for="interv in interventions" :key="interv.id ?? interv.Id" class="bg-surface-container-low border border-outline-variant rounded-lg p-3 flex items-start gap-3">
              <div class="w-7 h-7 rounded-full bg-primary-container/20 border border-primary/30 flex items-center justify-center shrink-0 mt-0.5">
                <span class="material-symbols-outlined text-[14px] text-primary">build</span>
              </div>
              <div class="flex-1 min-w-0">
                <div class="flex items-center gap-2 mb-1">
                  <span class="font-label-sm text-label-sm text-on-surface-variant">{{ formatDate(interv.date ?? interv.Date) }}</span>
                  <span v-if="interv.intervenantId ?? interv.IntervenantId" class="font-label-sm text-label-sm text-on-surface-variant">— {{ employeeName(interv.intervenantId ?? interv.IntervenantId) }}</span>
                </div>
                <p class="font-body-md text-body-md text-on-surface">{{ interv.commentaires || interv.Commentaires || '—' }}</p>
              </div>
            </div>
          </div>
        </div>
        <div>
          <div class="flex items-center gap-2 mb-2">
            <span class="material-symbols-outlined text-[#f59e0b] text-[18px]">add_circle</span>
            <span class="font-headline-sm text-headline-sm text-on-surface">Nouvelle intervention</span>
          </div>
          <textarea v-model="intervComment" class="form-input" rows="3" placeholder="Décrivez l'intervention réalisée…"></textarea>
        </div>
      </div>
    </Modal>
  </div>
</template>
