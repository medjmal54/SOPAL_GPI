<script setup>
import { ref, computed, onMounted, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import {
  Monitor, ArrowLeft, UserCheck, MapPin, RotateCcw, Clock, Tag, Hash,
  CalendarDays, Building2, Briefcase, FileText, ChevronRight,
  Pencil, Trash2, Wrench, CheckCircle, AlertTriangle
} from 'lucide-vue-next'
import PageHeader from '../components/PageHeader.vue'
import Modal      from '../components/Modal.vue'
import { apiGet, apiPost } from '../services/api'
import { useToast } from '../composables/useToast'
import { useAuth } from '../services/auth'

const { success, error: toastError } = useToast()
const { state: authState, isAdmin } = useAuth()
const route = useRoute()
const router = useRouter()

const loading       = ref(true)
const material      = ref(null)
const movements     = ref([])
const locations     = ref([])
const matypes       = ref([])
const employees     = ref([])
const loadingMovs   = ref(false)

const showAffecter  = ref(false)
const showTransfert = ref(false)
const showRetour    = ref(false)
const saving        = ref(false)
const empMat        = ref('')
const localId       = ref('')

// ─── Interventions Modal State ───
const showIntervDetail  = ref(false)
const selectedMouvement = ref(null)
const interventions     = ref([])
const loadingInterv     = ref(false)
const intervComment     = ref('')
const savingInterv      = ref(false)

const matId = computed(() => Number(route.params.id))

const typmovLabel = (t) => ({ 1: 'Affectation', 2: 'Retour', 3: 'Transfert', 4: 'Maintenance' })[t] ?? String(t ?? '—')
const typmovBadge = (t) => ({ 1: 'badge-cyan', 2: 'badge-amber', 3: 'badge-indigo', 4: 'badge-red' })[t] ?? 'badge-gray'
const typmovDot   = (t) => ({ 1: 'dot-cyan', 2: 'dot-amber', 3: 'dot-indigo', 4: 'dot-gray' })[t] ?? 'dot-gray'
const typmovIcon  = (t) => ({ 1: UserCheck, 2: RotateCcw, 3: MapPin, 4: Wrench })[t] ?? RotateCcw

function localisationName(id) {
  const found = locations.value.find(l => (l.id ?? l.Id) === id)
  return found ? (found.Nom ?? found.nom ?? found.name ?? `#${id}`) : `#${id}`
}

function typeName(id) {
  const found = matypes.value.find(t => (t.id ?? t.Id) === id)
  return found ? (found.libelle ?? found.Libelle ?? found.nom ?? `#${id}`) : `#${id}`
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

function raw(mat, k1, k2) {
  return mat[k1] ?? mat[k2] ?? null
}

const matDetail = computed(() => {
  const m = material.value
  if (!m) return null
  return {
    libelle:         raw(m, 'libelle', 'Libelle') || '—',
    marque:          raw(m, 'marque', 'Marque') || '—',
    modele:          raw(m, 'modele', 'Modele') || '—',
    numSerie:        raw(m, 'numSerie', 'NumSerie') || '—',
    dateAcquisition: raw(m, 'dateAcquisition', 'DateAcquisition'),
    actif:           raw(m, 'actif', 'Actif'),
    commentaires:    raw(m, 'commentaires', 'Commentaires'),
    dateModif:       raw(m, 'dateModif', 'DateModif'),
    typeId:          raw(m, 'typeMaterielId', 'TypeMaterielId'),
    localisationId:  raw(m, 'localisationId', 'LocalisationId'),
    etatId:          raw(m, 'etatId', 'EtatId'),
    employeMat:      raw(m, 'employeMat', 'EmployeMat'),
  }
})

async function loadAll() {
  loading.value = true
  try {
    const [mats, locs, types, emps] = await Promise.all([
      apiGet('/Materials'),
      apiGet('/Localistations'),
      apiGet('/Matypes'),
      apiGet('/Employes'),
    ])
    locations.value = Array.isArray(locs) ? locs : []
    matypes.value   = Array.isArray(types) ? types : []
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
  try {
    const m = await apiGet(`/Materials/${matId.value}`)
    if (m) material.value = m
  } catch {}
}

// ─── Affecter ───
function openAffecter() {
  empMat.value = ''
  showAffecter.value = true
}
async function submitAffecter() {
  if (!empMat.value) return
  saving.value = true
  try {
    await apiPost(`/v1/mouvements/${matId.value}/affecter/${empMat.value}`)
    success(`Matériel affecté à l'employé #${empMat.value}.`)
    showAffecter.value = false
    await Promise.all([reloadMaterial(), loadMovements()])
  } catch (e) {
    toastError(e.message || 'Affectation impossible.')
  } finally {
    saving.value = false
  }
}

// ─── Transfert ───
function openTransfert() {
  localId.value = ''
  showTransfert.value = true
}
async function submitTransfert() {
  if (!localId.value) return
  saving.value = true
  try {
    await apiPost(`/v1/mouvements/${matId.value}/transfert/${localId.value}`)
    success('Transfert validé.')
    showTransfert.value = false
    await Promise.all([reloadMaterial(), loadMovements()])
  } catch (e) {
    toastError(e.message || 'Transfert impossible.')
  } finally {
    saving.value = false
  }
}

// ─── Retour ───
function openRetour() {
  showRetour.value = true
}
async function submitRetour() {
  saving.value = true
  try {
    await apiPost(`/v1/mouvements/${matId.value}/retour`)
    success('Retour validé.')
    showRetour.value = false
    await Promise.all([reloadMaterial(), loadMovements()])
  } catch (e) {
    toastError(e.message || 'Retour impossible.')
  } finally {
    saving.value = false
  }
}

// ─── Interventions Modal Methods ───
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
  } catch (e) {
    toastError(e.message || 'Erreur de chargement des interventions.')
  } finally {
    loadingInterv.value = false
  }
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
  } catch (e) {
    toastError(e.message || 'Erreur lors de l\'ajout.')
  } finally {
    savingInterv.value = false
  }
}

onMounted(loadAll)
</script>

<template>
  <div>
    <!-- Loading -->
    <div v-if="loading" class="flex items-center justify-center py-24">
      <div class="flex flex-col items-center gap-4">
        <div class="spinner" style="width:36px;height:36px;" />
        <p class="text-sm text-slate-500">Chargement du matériel…</p>
      </div>
    </div>

    <template v-else-if="matDetail">
      <!-- Back button -->
      <button
        class="inline-flex items-center gap-2 text-sm text-slate-500 hover:text-slate-300 transition-colors mb-5"
        @click="router.push('/materiels')"
      >
        <ArrowLeft :size="16" />
        Retour aux matériels
      </button>

      <!-- Header -->
      <PageHeader
        :title="matDetail.libelle"
        :description="`${matDetail.marque} · ${matDetail.modele}`"
        :icon="Monitor"
      >
        <template #actions>
          <div class="flex items-center gap-2">
            <span :class="matDetail.actif ? 'badge badge-green' : 'badge badge-red'" style="font-size:12px;padding:4px 12px;">
              {{ matDetail.actif ? 'Actif' : 'Inactif' }}
            </span>
            <span class="text-xs text-slate-500 font-mono">N° {{ matDetail.numSerie }}</span>
          </div>
        </template>
      </PageHeader>

      <!-- Actions Bar (Admin only) -->
      <div v-if="isAdmin()" class="glass p-4 mb-6">
        <div class="flex items-center justify-between flex-wrap gap-3">
          <div class="text-xs font-semibold text-slate-500 uppercase tracking-widest">Actions</div>
          <div class="flex items-center gap-2 flex-wrap">
            <button
              v-if="!matDetail.employeMat"
              class="btn"
              style="background:rgba(34,211,238,0.1);color:#67e8f9;border:1px solid rgba(34,211,238,0.25);"
              @click="openAffecter"
            >
              <UserCheck :size="14" />
              Affecter
            </button>
            <button
              class="btn"
              style="background:rgba(99,102,241,0.1);color:#a5b4fc;border:1px solid rgba(99,102,241,0.25);"
              @click="openTransfert"
            >
              <MapPin :size="14" />
              Transfert
            </button>
            <button
              v-if="matDetail.employeMat"
              class="btn"
              style="background:rgba(245,158,11,0.1);color:#fcd34d;border:1px solid rgba(245,158,11,0.25);"
              @click="openRetour"
            >
              <RotateCcw :size="14" />
              Retour
            </button>
          </div>
        </div>
      </div>

      <!-- Info Grid -->
      <div class="mat-info-grid mb-6">
        <div class="mat-info-cell">
          <div class="mat-info-label flex items-center gap-1.5">
            <Tag :size="10" />
            Type
          </div>
          <div class="mat-info-value">{{ typeName(matDetail.typeId) }}</div>
        </div>
        <div class="mat-info-cell">
          <div class="mat-info-label flex items-center gap-1.5">
            <CalendarDays :size="10" />
            Acquisition
          </div>
          <div class="mat-info-value">{{ formatDate(matDetail.dateAcquisition) }}</div>
        </div>
        <div class="mat-info-cell">
          <div class="mat-info-label flex items-center gap-1.5">
            <Building2 :size="10" />
            Localisation
          </div>
          <div class="mat-info-value">{{ localisationName(matDetail.localisationId) }}</div>
        </div>
        <div class="mat-info-cell">
          <div class="mat-info-label flex items-center gap-1.5">
            <Briefcase :size="10" />
            Employé
          </div>
          <div class="mat-info-value">
            <span v-if="matDetail.employeMat" class="text-slate-200">{{ employeeName(matDetail.employeMat) }}</span>
            <span v-else class="text-slate-600">Non affecté</span>
          </div>
        </div>
        <div class="mat-info-cell">
          <div class="mat-info-label flex items-center gap-1.5">
            <Clock :size="10" />
            Dernière MAJ
          </div>
          <div class="mat-info-value text-xs">{{ formatDateTime(matDetail.dateModif) }}</div>
        </div>
        <div class="mat-info-cell">
          <div class="mat-info-label flex items-center gap-1.5">
            <FileText :size="10" />
            Commentaires
          </div>
          <div class="mat-info-value text-xs text-slate-400">{{ matDetail.commentaires || '—' }}</div>
        </div>
      </div>

      <!-- Movement Timeline -->
      <div class="glass p-5">
        <div class="flex items-center gap-2 mb-5">
          <Clock :size="14" class="text-indigo-400" />
          <span class="text-sm font-semibold text-slate-300">Historique des mouvements</span>
          <span v-if="movements.length" class="badge badge-gray ml-1">{{ movements.length }}</span>
        </div>

        <!-- Loading -->
        <div v-if="loadingMovs" class="flex items-center justify-center py-8">
          <div class="spinner" style="width:24px;height:24px;" />
        </div>

        <!-- Empty state -->
        <div v-else-if="movements.length === 0" class="timeline-empty">
          <div class="py-10 text-center text-sm text-slate-500">
            <RotateCcw :size="28" class="mx-auto mb-3 text-slate-600" />
            Aucun mouvement enregistré pour ce matériel.
          </div>
        </div>

        <!-- Timeline -->
        <div v-else class="timeline" style="padding-right: 8px;">
          <div
            v-for="(m, i) in movements"
            :key="m.id ?? i"
            class="timeline-item"
          >
            <div class="timeline-dot" :class="typmovDot(m.typmov ?? m.Typmov)" />
            <div class="timeline-card">
              <div class="timeline-date">
                <span>{{ formatDateTime(m.dateMouvement ?? m.DateMouvement) }}</span>
                <span :class="`badge ${typmovBadge(m.typmov ?? m.Typmov)}`">
                  {{ typmovLabel(m.typmov ?? m.Typmov) }}
                </span>
              </div>
              <div class="timeline-detail">
                <div class="flex items-center gap-1.5 mb-1">
                  <component :is="typmovIcon(m.typmov ?? m.Typmov)" :size="12" class="text-slate-500" />
                  <span v-if="(m.typmov ?? m.Typmov) === 1 && m.employeMat">
                    Affecté à <strong>{{ employeeName(m.employeMat) }}</strong>
                  </span>
                  <span v-else-if="(m.typmov ?? m.Typmov) === 3">
                    Transfert vers <strong>{{ localisationName(m.localisationId ?? m.LocalisationId) }}</strong>
                  </span>
                  <span v-else-if="(m.typmov ?? m.Typmov) === 2">
                    Retour — <strong>{{ localisationName(m.localisationId ?? m.LocalisationId) }}</strong>
                  </span>
                  <span v-else-if="(m.typmov ?? m.Typmov) === 4">
                    Demande d'intervention — <strong>{{ m.commentaires || 'Maintenance' }}</strong>
                    <span v-if="m.statut ?? m.Statut" class="ml-2">
                      <span v-if="(m.statut ?? m.Statut) === 'Terminé'" class="inline-flex items-center gap-1 text-xs font-medium text-emerald-400">
                        <CheckCircle :size="10" />
                        Terminé
                      </span>
                      <span v-else-if="(m.statut ?? m.Statut) === 'En cours'" class="inline-flex items-center gap-1 text-xs font-medium text-indigo-400">
                        <Wrench :size="10" />
                        En cours
                      </span>
                      <span v-else class="inline-flex items-center gap-1 text-xs font-medium text-amber-400">
                        <AlertTriangle :size="10" />
                        {{ m.statut ?? m.Statut }}
                      </span>
                    </span>
                  </span>
                  <span v-else>
                    Mouvement vers <strong>{{ localisationName(m.localisationId ?? m.LocalisationId) }}</strong>
                  </span>
                </div>
                <div v-if="m.ancienEtatmatId !== m.nouvEtatmatId" class="flex items-center gap-1 text-xs text-slate-500 mt-1">
                  <span>État changé</span>
                  <ChevronRight :size="10" />
                </div>
              </div>
              <div v-if="m.commentaires" class="timeline-comment">
                {{ m.commentaires }}
              </div>
              <div v-if="(m.typmov ?? m.Typmov) === 4 && (m.employeMat ?? m.EmployeMat) === authState.matricule" class="mt-3 pt-2 flex items-center justify-between border-t border-white/5">
                <span class="text-xs text-slate-500">Mouvement #{{ m.id ?? m.Id }}</span>
                <button
                  class="btn btn-sm flex items-center gap-1"
                  style="background:rgba(99,102,241,0.1);color:#a5b4fc;border:1px solid rgba(99,102,241,0.25);padding:2px 8px;font-size:11px;"
                  @click="openIntervDetail(m)"
                >
                  <Wrench :size="10" />
                  <span>Interventions</span>
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>
    </template>

    <!-- AFFECTER Modal -->
    <Modal
      v-if="showAffecter"
      title="Affecter un matériel"
      confirm-text="Affecter"
      :loading="saving"
      size="sm"
      @close="showAffecter = false"
      @confirm="submitAffecter"
    >
      <div class="space-y-3">
        <p class="text-sm text-slate-400">
          Matériel : <strong class="text-slate-200">{{ matDetail?.libelle }} #{{ matId }}</strong>
        </p>
        <div class="form-group">
          <label class="form-label">Employé (Matricule)</label>
          <select v-model="empMat" class="form-input">
            <option value="">— Choisir un employé —</option>
            <option v-for="emp in employees" :key="emp.matricule ?? emp.Matricule" :value="emp.matricule ?? emp.Matricule">
              {{ emp.nom ?? emp.Nom }} {{ emp.prenom ?? emp.Prenom }} — #{{ emp.matricule ?? emp.Matricule }}
            </option>
          </select>
        </div>
      </div>
    </Modal>

    <!-- TRANSFERT Modal -->
    <Modal
      v-if="showTransfert"
      title="Transfert de localisation"
      confirm-text="Transférer"
      :loading="saving"
      size="sm"
      @close="showTransfert = false"
      @confirm="submitTransfert"
    >
      <div class="space-y-3">
        <p class="text-sm text-slate-400">
          Matériel : <strong class="text-slate-200">{{ matDetail?.libelle }} #{{ matId }}</strong>
        </p>
        <div class="form-group">
          <label class="form-label">Nouvelle localisation</label>
          <select v-model="localId" class="form-input">
            <option value="">— Choisir une localisation —</option>
            <option v-for="loc in locations" :key="loc.id ?? loc.Id" :value="loc.id ?? loc.Id">
              {{ loc.nom ?? loc.Nom ?? `Localisation #${loc.id ?? loc.Id}` }}
            </option>
          </select>
        </div>
      </div>
    </Modal>

    <!-- RETOUR Modal -->
    <Modal
      v-if="showRetour"
      title="Retour matériel"
      confirm-text="Confirmer le retour"
      confirm-class="btn btn-success"
      :loading="saving"
      size="sm"
      @close="showRetour = false"
      @confirm="submitRetour"
    >
      <p class="text-sm text-slate-400 leading-relaxed">
        Confirmer le retour du matériel
        <strong class="text-slate-200">{{ matDetail?.libelle }} #{{ matId }}</strong> ?
        L'affectation à l'employé sera annulée.
      </p>
    </Modal>

    <!-- Interventions Detail Modal -->
    <Modal
      v-if="showIntervDetail"
      :title="`Interventions — ${matDetail?.libelle}`"
      size="lg"
      confirm-text="Ajouter"
      :loading="savingInterv"
      @close="showIntervDetail = false"
      @confirm="submitInterv"
    >
      <div class="space-y-5">
        <!-- Movement Info -->
        <div class="rounded-xl p-3" style="background:rgba(8,12,30,0.6);border:1px solid rgba(255,255,255,0.06);">
          <div class="text-xs text-slate-500 mb-1">Mouvement / Description</div>
          <p class="text-sm text-slate-300">
            Type : <span :class="`badge ${typmovBadge(selectedMouvement?.typmov)}`" style="font-size:11px;padding:2px 8px;margin-left:4px;">{{ typmovLabel(selectedMouvement?.typmov) }}</span>
            <span v-if="selectedMouvement?.commentaires" class="block mt-1 text-xs text-slate-400">
              Note : {{ selectedMouvement?.commentaires }}
            </span>
          </p>
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
            Aucune intervention enregistrée pour ce mouvement.
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
        <div>
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
      </div>
    </Modal>
  </div>
</template>
