<script setup>
import { ref, computed, onMounted, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import Modal from '../components/Modal.vue'
import { apiGet, apiPost, apiPut, apiDelete } from '../services/api'
import { useToast } from '../composables/useToast'
import { useAuth } from '../services/auth'
import { useExport } from '../composables/useExport'

const { success, error: toastError } = useToast()
const { isAdmin } = useAuth()
const { exportData } = useExport()
const route = useRoute()
const router = useRouter()

const items = ref([])
const matypes = ref([])
const locations = ref([])
const employees = ref([])
const movements = ref([])
const loading = ref(true)
const loadingMovements = ref(false)
const exporting = ref(false)
const query = ref('')
const selectedAsset = ref(null)
const operation = ref('transfert')
const employeeMat = ref('')
const locationId = ref('')
const movementComment = ref('')

const showAdd = ref(false)
const showEdit = ref(false)
const showDel = ref(false)
const saving = ref(false)
const selected = ref(null)

const emptyForm = () => ({
  libelle: '', marque: '', modele: '', numSerie: '', dateAcquisition: '', actif: true, typeMaterielId: 1, commentaires: '',
})
const form = ref(emptyForm())

async function loadData() {
  loading.value = true
  try {
    const params = new URLSearchParams()
    if (query.value) params.set('q', query.value)
    const queryString = params.toString()
    const [search, types, locs, emps] = await Promise.all([
      apiGet(`/Materials/search${queryString ? `?${queryString}` : ''}`),
      apiGet('/Matypes'),
      apiGet('/Localistations'),
      apiGet('/Employes'),
    ])
    items.value = Array.isArray(search?.items) ? search.items : []
    matypes.value = Array.isArray(types) ? types : []
    locations.value = Array.isArray(locs) ? locs : []
    employees.value = Array.isArray(emps) ? emps : []
    const requestedId = Number(route.query.asset)
    selectedAsset.value = items.value.find(m => Number(raw(m, 'id', 'Id')) === requestedId) ?? items.value[0] ?? null
  } catch (e) {
    toastError(e.message || 'Erreur de chargement.')
  } finally {
    loading.value = false
  }
}

onMounted(loadData)

let searchTimer = null
watch(query, () => {
  clearTimeout(searchTimer)
  searchTimer = setTimeout(loadData, 300)
})

function selectAsset(mat) {
  selectedAsset.value = mat
  router.replace({ path: '/materiels', query: { asset: raw(mat, 'id', 'Id') } })
}

function raw(mat, k1, k2) { return mat[k1] ?? mat[k2] ?? null }

function formatDate(value, withTime = false) {
  if (!value) return '—'
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return '—'
  return date.toLocaleDateString('fr-FR', withTime
    ? { day: '2-digit', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit' }
    : { day: '2-digit', month: 'short', year: 'numeric' })
}

function employeeName(matricule) {
  if (!matricule) return 'Non affecté'
  const found = employees.value.find(e => String(raw(e, 'matricule', 'Matricule')) === String(matricule))
  return found ? `${raw(found, 'nom', 'Nom') ?? ''} ${raw(found, 'prenom', 'Prenom') ?? ''}`.trim() : `#${matricule}`
}

function localisationName(id) {
  if (!id) return 'Non localisé'
  const found = locations.value.find(l => Number(raw(l, 'id', 'Id')) === Number(id))
  return found ? raw(found, 'nom', 'Nom') : `#${id}`
}

const assetState = computed(() => {
  const m = selectedAsset.value
  if (!m) return { label: 'Indisponible', className: 'asset-state inactive' }
  if (raw(m, 'actif', 'Actif') === false) return { label: 'Inactif', className: 'asset-state inactive' }
  if (raw(m, 'employeMat', 'EmployeMat')) return { label: 'Affecté', className: 'asset-state assigned' }
  return { label: 'En stock', className: 'asset-state stock' }
})

const detailFields = computed(() => {
  const m = selectedAsset.value
  if (!m) return []
  return [
    { label: 'Libellé', value: raw(m, 'libelle', 'Libelle') },
    { label: 'Marque', value: raw(m, 'marque', 'Marque') },
    { label: 'Modèle', value: raw(m, 'modele', 'Modele') },
    { label: 'N° de Série', value: raw(m, 'numSerie', 'NumSerie') },
    { label: 'Date d\'Acquisition', value: formatDate(raw(m, 'dateAcquisition', 'DateAcquisition')) },
    { label: 'Code-Barres', value: `SOPAL-${String(raw(m, 'id', 'Id')).padStart(4, '0')}` },
    { label: 'Type', value: typeName(raw(m, 'typeMaterielId', 'TypeMaterielId')) },
    { label: 'Localisation', value: localisationName(raw(m, 'localisationId', 'LocalisationId')) },
    { label: 'Affectataire', value: employeeName(raw(m, 'employeMat', 'EmployeMat')) },
  ]
})

const movementLabels = { 1: 'Affectation', 2: 'Retour', 3: 'Transfert', 4: 'Maintenance' }
const movementIcons = { 1: 'person_add', 2: 'assignment_return', 3: 'move_down', 4: 'build' }

async function loadMovements() {
  if (!selectedAsset.value) { movements.value = []; return }
  const id = raw(selectedAsset.value, 'id', 'Id')
  if (id == null) { movements.value = []; return }
  loadingMovements.value = true
  movements.value = []
  try {
    const data = await apiGet(`/v1/mouvements/${id}/details`)
    if (Number(raw(selectedAsset.value, 'id', 'Id')) !== Number(id)) return
    movements.value = (Array.isArray(data) ? data : []).sort((a, b) =>
      new Date(raw(b, 'dateMouvement', 'DateMouvement') ?? 0) - new Date(raw(a, 'dateMouvement', 'DateMouvement') ?? 0))
  } catch (e) {
    toastError(e.message || 'Erreur de chargement des mouvements.')
  } finally { loadingMovements.value = false }
}

watch(selectedAsset, () => {
  operation.value = raw(selectedAsset.value ?? {}, 'employeMat', 'EmployeMat') ? 'retour' : 'affecter'
  employeeMat.value = ''
  locationId.value = ''
  movementComment.value = ''
  loadMovements()
})

async function refreshSelected() {
  const id = raw(selectedAsset.value, 'id', 'Id')
  if (id == null) return
  const fresh = await apiGet(`/Materials/${id}`)
  if (fresh) {
    const index = items.value.findIndex(m => Number(raw(m, 'id', 'Id')) === Number(id))
    if (index >= 0) items.value[index] = fresh
    selectedAsset.value = fresh
  }
}

async function submitMovement() {
  if (!selectedAsset.value) return
  const id = raw(selectedAsset.value, 'id', 'Id')
  if (operation.value === 'affecter' && !employeeMat.value) return toastError('Sélectionnez un employé.')
  if (operation.value === 'transfert' && !locationId.value) return toastError('Sélectionnez une localisation.')
  saving.value = true
  try {
    if (operation.value === 'affecter') await apiPost(`/v1/mouvements/${id}/affecter/${employeeMat.value}`)
    else if (operation.value === 'transfert') await apiPost(`/v1/mouvements/${id}/transfert/${locationId.value}`)
    else await apiPost(`/v1/mouvements/${id}/retour`)
    success('Mouvement enregistré avec succès.')
    await Promise.all([refreshSelected(), loadMovements()])
  } catch (e) { toastError(e.message || 'Mouvement impossible.')
  } finally { saving.value = false }
}

async function exportMats(format) {
  exporting.value = true
  try {
    await exportData('materials', query.value ? { q: query.value } : {}, format)
  } catch (e) {
    toastError(e.message || "Erreur lors de l'export.")
  } finally {
    exporting.value = false
  }
}

function openAdd() { form.value = emptyForm(); showAdd.value = true }
async function submitAdd() {
  saving.value = true
  try {
    await apiPost('/Materials', {
      libelle: form.value.libelle, marque: form.value.marque, modele: form.value.modele,
      numSerie: form.value.numSerie, dateAcquisition: form.value.dateAcquisition,
      actif: form.value.actif, typeMaterielId: Number(form.value.typeMaterielId), commentaires: form.value.commentaires,
    })
    success('Matériel ajouté avec succès.'); showAdd.value = false; await loadData()
  } catch (e) { toastError(e.message || 'Erreur lors de l\'ajout.')
  } finally { saving.value = false }
}

function openEdit(row) {
  selected.value = row
  form.value = {
    libelle: row.libelle ?? '', marque: row.marque ?? '', modele: row.modele ?? '',
    numSerie: row.numSerie ?? '', dateAcquisition: row.dateAcquisition ?? '',
    actif: row.actif ?? true, typeMaterielId: row.typeMaterielId ?? row.TypeMaterielId ?? 1, commentaires: row.commentaires ?? '',
  }
  showEdit.value = true
}
async function submitEdit() {
  if (!selected.value) return
  saving.value = true
  try {
    await apiPut(`/Materials/${selected.value.id}`, {
      libelle: form.value.libelle, marque: form.value.marque, modele: form.value.modele,
      numSerie: form.value.numSerie, dateAcquisition: form.value.dateAcquisition,
      actif: form.value.actif, typeMaterielId: Number(form.value.typeMaterielId), commentaires: form.value.commentaires,
    })
    success('Matériel mis à jour.'); showEdit.value = false; await loadData()
  } catch (e) { toastError(e.message || 'Erreur lors de la modification.')
  } finally { saving.value = false }
}

function openDel(row) { selected.value = row; showDel.value = true }
async function submitDel() {
  if (!selected.value) return
  saving.value = true
  try {
    await apiDelete(`/Materials/${selected.value.id}`)
    success('Matériel supprimé.'); showDel.value = false; await loadData()
  } catch (e) { toastError(e.message || 'Action impossible.')
  } finally { saving.value = false }
}

function typeName(id) {
  const found = matypes.value.find(t => (t.id ?? t.Id) === id)
  return found ? (found.libelle ?? found.Libelle ?? `#${id}`) : `#${id}`
}
</script>

<template>
  <div class="workspace-layout flex h-[calc(100vh-64px)] overflow-hidden -m-container-padding">
    <!-- Left Panel: Asset List -->
    <aside class="w-[300px] bg-surface-container border-r border-outline-variant flex flex-col shrink-0">
      <div class="p-stack-gap border-b border-outline-variant shrink-0">
        <div class="flex items-center justify-between mb-stack-gap">
          <h2 class="font-headline-sm text-headline-sm font-bold text-on-surface">Inventaire</h2>
          <span class="font-label-sm bg-surface-container-high px-2 py-0.5 rounded border border-outline-variant text-on-surface-variant">{{ items.length }} Items</span>
        </div>
        <div class="relative">
          <span class="material-symbols-outlined absolute left-compact-gap top-1/2 -translate-y-1/2 text-on-surface-variant text-[16px]">filter_list</span>
          <input v-model="query" class="w-full bg-surface border border-outline-variant rounded pl-8 pr-compact-gap py-unit font-label-sm text-on-surface focus:border-primary focus:ring-1 focus:ring-primary outline-none" placeholder="Filtrer par Libellé, SN..." type="text">
        </div>
      </div>
      <div class="flex-1 overflow-y-auto p-compact-gap flex flex-col gap-unit">
        <div v-if="loading" class="flex items-center justify-center py-12">
          <div class="spinner" />
        </div>
        <button
          v-for="mat in items"
          :key="mat.id ?? mat.Id"
          class="w-full text-left p-compact-gap rounded-r flex flex-col gap-unit transition-colors"
          :class="selectedAsset === mat ? 'bg-surface-container-highest border-l-4 border-primary' : 'bg-surface-container border-l-4 border-transparent hover:bg-surface-container-high'"
          @click="selectAsset(mat)"
        >
          <div class="flex justify-between items-start w-full">
            <span class="font-label-md text-label-md text-on-surface">{{ raw(mat, 'libelle', 'Libelle') }}</span>
            <div class="w-2 h-2 rounded-full bg-tertiary shadow-[0_0_8px_rgba(78,222,163,0.6)]"></div>
          </div>
          <span class="font-body-sm text-body-sm text-on-surface-variant truncate">{{ raw(mat, 'marque', 'Marque') }} {{ raw(mat, 'modele', 'Modele') }}</span>
          <div class="flex items-center gap-1 font-label-sm text-label-sm text-on-surface-variant">
            <span class="material-symbols-outlined text-[14px]">location_on</span>
            {{ typeName(raw(mat, 'typeMaterielId', 'TypeMaterielId')) }}
          </div>
        </button>
        <div v-if="!loading && items.length === 0" class="flex flex-col items-center py-12 text-on-surface-variant">
          <span class="material-symbols-outlined text-[40px] mb-2">inventory_2</span>
          <span class="font-body-sm text-body-sm">Aucun matériel trouvé</span>
        </div>
      </div>
      <div class="p-stack-gap border-t border-outline-variant shrink-0">
        <button v-if="isAdmin()" @click="openAdd" class="w-full flex items-center justify-center gap-compact-gap py-compact-gap border border-outline-variant rounded text-primary hover:bg-surface-container-high transition-colors font-label-md text-label-md">
          <span class="material-symbols-outlined text-[18px]">add_box</span> Nouveau Matériel
        </button>
        <div class="mt-2 grid grid-cols-2 gap-2">
          <button :disabled="exporting" @click="exportMats('xlsx')" class="flex items-center justify-center gap-1 py-1.5 border border-outline-variant rounded text-on-surface-variant hover:bg-surface-container-high transition-colors font-label-sm text-label-sm">
            <span class="material-symbols-outlined text-[15px]">description</span> Excel
          </button>
          <button :disabled="exporting" @click="exportMats('csv')" class="flex items-center justify-center gap-1 py-1.5 border border-outline-variant rounded text-on-surface-variant hover:bg-surface-container-high transition-colors font-label-sm text-label-sm">
            <span class="material-symbols-outlined text-[15px]">table_view</span> CSV
          </button>
        </div>
      </div>
    </aside>

    <!-- Center Panel: Technical Sheet and Timeline -->
    <section class="asset-center flex-1 bg-background flex flex-col min-w-0 overflow-y-auto">
      <div v-if="!selectedAsset" class="flex items-center justify-center flex-1 text-on-surface-variant">
        <div class="text-center">
          <span class="material-symbols-outlined text-[48px]">devices</span>
          <p class="font-body-md text-body-md mt-2">Sélectionnez un matériel</p>
        </div>
      </div>

      <template v-else>
        <div class="asset-header p-container-padding border-b border-outline-variant shrink-0 bg-surface">
          <div class="flex justify-between items-start mb-stack-gap">
            <div class="flex gap-stack-gap">
              <div class="asset-code flex items-center justify-center w-20 h-20 bg-surface-container-high rounded-lg border border-outline-variant shrink-0">
                <span class="material-symbols-outlined text-[40px] text-primary">qr_code_2</span>
              </div>
              <div>
                <div class="flex items-center gap-compact-gap mb-unit">
                  <h1 class="font-headline-md text-headline-md text-on-surface">{{ raw(selectedAsset, 'libelle', 'Libelle') }}</h1>
                </div>
                <p class="font-body-sm text-body-sm text-on-surface-variant">{{ raw(selectedAsset, 'marque', 'Marque') }} · {{ raw(selectedAsset, 'modele', 'Modele') }}</p>
                <div class="flex gap-compact-gap mt-unit">
                  <span :class="assetState.className">{{ assetState.label }}</span>
                  <span class="asset-location"><span class="material-symbols-outlined">location_on</span>{{ localisationName(raw(selectedAsset, 'localisationId', 'LocalisationId')) }}</span>
                </div>
              </div>
            </div>
            <div class="acquired-date">
              <span>ACQUIS LE</span>
              <strong>{{ formatDate(raw(selectedAsset, 'dateAcquisition', 'DateAcquisition')) }}</strong>
            </div>
          </div>
        </div>

        <div class="asset-content flex-1 p-container-padding">
          <div class="bg-surface border border-outline-variant rounded-xl p-container-padding">
            <h3 class="font-headline-sm text-headline-sm text-on-surface mb-stack-gap flex items-center gap-compact-gap border-b border-outline-variant pb-unit">
              <span class="material-symbols-outlined text-primary">description</span> Fiche Technique
            </h3>
            <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-x-gutter gap-y-stack-gap">
              <div v-for="field in detailFields" :key="field.label" class="flex flex-col border-b border-outline-variant/30 pb-unit">
                <span class="font-label-sm text-label-sm text-on-surface-variant">{{ field.label }}</span>
                <span class="font-body-md text-body-md text-on-surface">{{ field.value || '—' }}</span>
              </div>
            </div>
            <div class="mt-stack-gap flex flex-col gap-unit">
              <span class="font-label-sm text-label-sm text-on-surface-variant">Commentaires / Notes</span>
              <div class="bg-surface-container-low p-compact-gap rounded-lg border border-outline-variant font-body-sm text-body-sm text-on-surface-variant">
                {{ raw(selectedAsset, 'commentaires', 'Commentaires') || 'Aucun commentaire.' }}
              </div>
            </div>
          </div>

          <div class="asset-history bg-surface border border-outline-variant rounded-xl p-container-padding mt-container-padding">
            <div class="history-heading">
              <h3><span class="material-symbols-outlined">history</span> Historique des Mouvements</h3>
              <span>{{ movements.length }} événement{{ movements.length === 1 ? '' : 's' }}</span>
            </div>
            <div v-if="loadingMovements" class="flex items-center justify-center py-8"><div class="spinner" /></div>
            <div v-else-if="movements.length === 0" class="empty-history">
              <span class="material-symbols-outlined">history_toggle_off</span><p>Aucun mouvement enregistré.</p>
            </div>
            <div v-else class="movement-timeline">
              <article v-for="(movement, index) in movements" :key="raw(movement, 'id', 'Id') ?? index" class="movement-entry">
                <div class="movement-marker"><span class="material-symbols-outlined">{{ movementIcons[raw(movement, 'typmov', 'Typmov')] ?? 'swap_horiz' }}</span></div>
                <div class="movement-card">
                  <div class="movement-topline">
                    <div><span class="movement-type">{{ movementLabels[raw(movement, 'typmov', 'Typmov')] ?? 'Mouvement' }}</span></div>
                    <time>{{ formatDate(raw(movement, 'dateMouvement', 'DateMouvement'), true) }}</time>
                  </div>
                  <div class="movement-data">
                    <div v-if="raw(movement, 'employeMat', 'EmployeMat')"><small>EMPLOYÉ</small><strong>{{ employeeName(raw(movement, 'employeMat', 'EmployeMat')) }}</strong></div>
                    <div v-if="raw(movement, 'localisationId', 'LocalisationId')"><small>LOCALISATION</small><strong>{{ localisationName(raw(movement, 'localisationId', 'LocalisationId')) }}</strong></div>
                    <div v-if="raw(movement, 'commentaires', 'Commentaires')"><small>COMMENTAIRE</small><strong>{{ raw(movement, 'commentaires', 'Commentaires') }}</strong></div>
                  </div>
                </div>
              </article>
            </div>
          </div>

          <div class="mt-container-padding flex items-center justify-center gap-4">
            <button v-if="isAdmin()" @click="openEdit(selectedAsset)" class="btn btn-ghost">
              <span class="material-symbols-outlined text-[16px]">edit</span> Modifier
            </button>
            <button v-if="isAdmin()" @click="openDel(selectedAsset)" class="btn btn-danger">
              <span class="material-symbols-outlined text-[16px]">delete</span> Supprimer
            </button>
          </div>
        </div>
      </template>
    </section>

    <!-- Right Panel: Material Operations -->
    <aside class="asset-actions w-[350px] bg-surface-container border-l border-outline-variant flex flex-col shrink-0">
      <div class="actions-title"><span class="material-symbols-outlined">settings_applications</span><h2>Actions Matériel</h2></div>
      <div v-if="selectedAsset" class="actions-body">
        <div class="action-section">
          <h3>NOUVEAU MOUVEMENT</h3>
          <label>Type d'opération</label>
          <select v-model="operation" class="form-input">
            <option value="affecter" :disabled="!!raw(selectedAsset, 'employeMat', 'EmployeMat')">Affecter à un employé</option>
            <option value="transfert">Transférer (localisation)</option>
            <option value="retour" :disabled="!raw(selectedAsset, 'employeMat', 'EmployeMat')">Enregistrer un retour</option>
          </select>
          <template v-if="operation === 'affecter'">
            <label>Employé concerné</label>
            <select v-model="employeeMat" class="form-input"><option value="">Sélectionner un employé...</option><option v-for="emp in employees" :key="raw(emp, 'matricule', 'Matricule')" :value="raw(emp, 'matricule', 'Matricule')">{{ employeeName(raw(emp, 'matricule', 'Matricule')) }}</option></select>
          </template>
          <template v-if="operation === 'transfert'">
            <label>Destination</label>
            <select v-model="locationId" class="form-input"><option value="">Sélectionner un site...</option><option v-for="loc in locations" :key="raw(loc, 'id', 'Id')" :value="raw(loc, 'id', 'Id')">{{ raw(loc, 'nom', 'Nom') }}</option></select>
          </template>
          <label>Motif / commentaire</label>
          <textarea v-model="movementComment" class="form-input" rows="3" placeholder="Note interne (optionnelle)..." />
        </div>
        <div class="action-section quick-actions">
          <h3>RACCOURCIS MÉTIER</h3>
          <button :disabled="!!raw(selectedAsset, 'employeMat', 'EmployeMat')" @click="operation = 'affecter'"><span class="material-symbols-outlined">person_add</span>Affecter à un employé</button>
          <button :disabled="!raw(selectedAsset, 'employeMat', 'EmployeMat')" @click="operation = 'retour'"><span class="material-symbols-outlined">assignment_return</span>Enregistrer retour</button>
          <button @click="operation = 'transfert'"><span class="material-symbols-outlined">swap_horiz</span>Transférer</button>
        </div>
      </div>
      <div v-if="selectedAsset" class="actions-footer">
        <button class="validate-action" :disabled="saving" @click="submitMovement">{{ saving ? 'Validation...' : 'Valider le mouvement' }}</button>
        <button @click="employeeMat = ''; locationId = ''; movementComment = ''">Annuler l'opération</button>
      </div>
    </aside>
  </div>

  <!-- ADD Modal -->
  <Modal v-if="showAdd" title="Ajouter un matériel" confirm-text="Enregistrer" :loading="saving" @close="showAdd = false" @confirm="submitAdd">
    <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
      <div class="form-group">
        <label class="form-label">Libellé</label>
        <input v-model="form.libelle" class="form-input" placeholder="ex: PC Portable Dell" />
      </div>
      <div class="form-group">
        <label class="form-label">Marque</label>
        <input v-model="form.marque" class="form-input" placeholder="ex: Dell" />
      </div>
      <div class="form-group">
        <label class="form-label">Modèle</label>
        <input v-model="form.modele" class="form-input" placeholder="ex: Latitude 5520" />
      </div>
      <div class="form-group">
        <label class="form-label">Numéro de série</label>
        <input v-model="form.numSerie" class="form-input" placeholder="ex: SN-00123" />
      </div>
      <div class="form-group">
        <label class="form-label">Date d'acquisition</label>
        <input v-model="form.dateAcquisition" type="date" class="form-input" />
      </div>
      <div class="form-group">
        <label class="form-label">Type de matériel</label>
        <select v-model="form.typeMaterielId" class="form-input">
          <option v-for="t in matypes" :key="t.id ?? t.Id" :value="t.id ?? t.Id">{{ t.libelle ?? t.Libelle }}</option>
        </select>
      </div>
      <div class="form-group col-span-2">
        <label class="form-label">Commentaires</label>
        <input v-model="form.commentaires" class="form-input" placeholder="Remarques optionnelles…" />
      </div>
    </div>
  </Modal>

  <!-- EDIT Modal -->
  <Modal v-if="showEdit" title="Modifier le matériel" confirm-text="Enregistrer" :loading="saving" @close="showEdit = false" @confirm="submitEdit">
    <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
      <div class="form-group"><label class="form-label">Libellé</label><input v-model="form.libelle" class="form-input" /></div>
      <div class="form-group"><label class="form-label">Marque</label><input v-model="form.marque" class="form-input" /></div>
      <div class="form-group"><label class="form-label">Modèle</label><input v-model="form.modele" class="form-input" /></div>
      <div class="form-group"><label class="form-label">N° Série</label><input v-model="form.numSerie" class="form-input" /></div>
      <div class="form-group"><label class="form-label">Date d'acquisition</label><input v-model="form.dateAcquisition" type="date" class="form-input" /></div>
      <div class="form-group"><label class="form-label">Type</label><select v-model="form.typeMaterielId" class="form-input">
        <option v-for="t in matypes" :key="t.id ?? t.Id" :value="t.id ?? t.Id">{{ t.libelle ?? t.Libelle }}</option>
      </select></div>
      <div class="form-group col-span-2"><label class="form-label">Commentaires</label><input v-model="form.commentaires" class="form-input" /></div>
    </div>
  </Modal>

  <!-- DELETE Modal -->
  <Modal v-if="showDel" title="Supprimer le matériel ?" confirm-text="Supprimer" confirm-class="btn btn-danger" :loading="saving" size="sm" @close="showDel = false" @confirm="submitDel">
    <p class="font-body-sm text-body-sm text-on-surface-variant leading-relaxed">
      Vous êtes sur le point de supprimer <strong class="text-on-surface">{{ selected?.libelle }}</strong>. Cette action est irréversible.
    </p>
  </Modal>
</template>

<style scoped>
.asset-state,.asset-location{
  display:inline-flex;
  align-items:center;
  gap:4px;
  padding:3px 8px;
  border-radius:999px;
  font:700 10px 'JetBrains Mono',monospace;
  text-transform:uppercase
}

.asset-state.assigned{
  color:#4edea3;
  background:rgba(78,222,163,.1);
  border:1px solid rgba(78,222,163,.3)
}

.asset-state.stock{color:#adc6ff;background:rgba(173,198,255,.1);border:1px solid rgba(173,198,255,.3)}.asset-state.inactive{color:#ffb4ab;background:rgba(255,180,171,.1);border:1px solid rgba(255,180,171,.3)}.asset-location{color:#c2c6d6;background:#272a31;border:1px solid #424754;text-transform:none}.asset-location .material-symbols-outlined{font-size:13px}.acquired-date{text-align:right;color:#8c909f;font:600 9px 'JetBrains Mono',monospace}.acquired-date strong{display:block;margin-top:4px;color:#c2c6d6;font-size:11px}.history-heading,.movement-topline{display:flex;align-items:center;justify-content:space-between;gap:12px}.history-heading{margin-bottom:18px}.history-heading h3{display:flex;align-items:center;gap:8px;font-size:20px;font-weight:600}.history-heading h3 span{color:#adc6ff}.history-heading>span{padding:3px 7px;border:1px solid #424754;border-radius:4px;color:#8c909f;font:600 9px 'JetBrains Mono',monospace}.empty-history{padding:36px;text-align:center;color:#8c909f}.empty-history>span{font-size:36px}.movement-timeline{position:relative;display:flex;flex-direction:column;gap:12px}.movement-timeline:before{content:"";position:absolute;left:17px;top:18px;bottom:18px;width:2px;background:#424754}.movement-entry{position:relative;display:flex;gap:12px}.movement-marker{z-index:1;width:36px;height:36px;flex:none;display:grid;place-items:center;border:2px solid #adc6ff;border-radius:50%;background:#10131a;color:#adc6ff}.movement-marker span{font-size:17px}.movement-card{flex:1;padding:12px;border:1px solid #424754;border-radius:8px;background:#191b23}.movement-topline time{color:#8c909f;font:500 9px 'JetBrains Mono',monospace}.movement-type{color:#adc6ff;font:700 10px 'JetBrains Mono',monospace;text-transform:uppercase}.movement-data{display:grid;grid-template-columns:repeat(auto-fit,minmax(140px,1fr));gap:12px;margin-top:12px}.movement-data small{display:block;color:#8c909f;font:600 8px 'JetBrains Mono',monospace}.movement-data strong{display:block;margin-top:3px;color:#e1e2ec;font-size:12px}.actions-title{height:68px;padding:0 20px;display:flex;align-items:center;gap:8px;border-bottom:1px solid #424754}.actions-title span{color:#adc6ff}.actions-title h2{font-size:18px;font-weight:700}.actions-body{flex:1;padding:20px;overflow-y:auto}.action-section{display:flex;flex-direction:column;gap:8px}.action-section+ .action-section{margin-top:24px}.action-section h3{padding-bottom:6px;border-bottom:1px solid #424754;color:#8c909f;font:700 10px 'JetBrains Mono',monospace}.action-section label{margin-top:4px;color:#c2c6d6;font:600 10px 'JetBrains Mono',monospace}.quick-actions button{display:flex;align-items:center;justify-content:center;gap:8px;padding:9px;border:1px solid #424754;border-radius:5px;background:#10131a;color:#e1e2ec;font:600 11px 'JetBrains Mono',monospace}.quick-actions button:hover:not(:disabled){border-color:#adc6ff;color:#adc6ff}.quick-actions button:disabled{opacity:.35}.quick-actions span{font-size:17px}.actions-footer{padding:18px 20px;display:flex;flex-direction:column;gap:8px;border-top:1px solid #424754}.actions-footer button{padding:10px;border:1px solid #424754;border-radius:6px;color:#c2c6d6;font-size:12px}.actions-footer .validate-action{border-color:#adc6ff;background:#adc6ff;color:#002e6a;font-weight:700}.actions-footer button:disabled{opacity:.5}
@media(max-width:1250px){.asset-actions{width:300px}.workspace-layout>aside:first-child{width:250px}}
@media(max-width:980px){.workspace-layout{display:block}.workspace-layout>aside,.asset-center{width:100%!important;max-height:none!important}.workspace-layout>aside:first-child{height:auto}.workspace-layout>aside:first-child>div:nth-child(2){max-height:310px}.asset-actions{border-left:0;border-top:1px solid #424754}.asset-header>div{flex-wrap:wrap}.acquired-date{text-align:left}.asset-content{padding:16px}}
</style>
