<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { Monitor, PlusCircle, Search, Pencil, Trash2, MoreHorizontal } from 'lucide-vue-next'
import PageHeader from '../components/PageHeader.vue'
import DataTable  from '../components/DataTable.vue'
import Modal      from '../components/Modal.vue'
import { apiGet, apiPost, apiPut, apiDelete } from '../services/api'
import { useToast } from '../composables/useToast'
import { useAuth } from '../services/auth'

const { success, error: toastError } = useToast()
const { isAdmin } = useAuth()
const router = useRouter()

// ─── State ───
const items   = ref([])
const loading = ref(true)
const query   = ref('')

const showAdd   = ref(false)
const showEdit  = ref(false)
const showDel   = ref(false)
const saving    = ref(false)
const selected  = ref(null)

const emptyForm = () => ({
  libelle: '',
  marque: '',
  modele: '',
  numSerie: '',
  dateAcquisition: '',
  actif: true,
  typeMaterielId: 1,
  commentaires: '',
})

const form = ref(emptyForm())

// ─── Load ───
async function loadData() {
  loading.value = true
  try {
    const mats = await apiGet('/Materials')
    items.value = Array.isArray(mats) ? mats : []
  } catch (e) {
    toastError(e.message || 'Erreur de chargement.')
  } finally {
    loading.value = false
  }
}

onMounted(loadData)

// ─── Filter ───
const filtered = computed(() => {
  if (!query.value) return items.value
  const q = query.value.toLowerCase()
  return items.value.filter(i =>
    JSON.stringify(i).toLowerCase().includes(q)
  )
})

// ─── Columns ───
const columns = [
  { key: 'libelle',          label: 'Libellé',   sortable: true  },
  { key: 'marque',           label: 'Marque',    sortable: true  },
  { key: 'modele',           label: 'Modèle',    sortable: true  },
  { key: 'numSerie',         label: 'N° Série',  sortable: false },
  { key: 'dateAcquisition',  label: 'Acquisition',sortable: true },
  { key: 'actif',            label: 'Statut',    sortable: true  },
]

// ─── Add ───
function openAdd() {
  form.value = emptyForm()
  showAdd.value = true
}
async function submitAdd() {
  saving.value = true
  try {
    await apiPost('/Materials', {
      libelle:         form.value.libelle,
      marque:          form.value.marque,
      modele:          form.value.modele,
      numSerie:        form.value.numSerie,
      dateAcquisition: form.value.dateAcquisition,
      actif:           form.value.actif,
      typeMaterielId:  Number(form.value.typeMaterielId),
      commentaires:    form.value.commentaires,
    })
    success('Matériel ajouté avec succès.')
    showAdd.value = false
    loadData()
  } catch (e) {
    toastError(e.message || 'Erreur lors de l\'ajout.')
  } finally {
    saving.value = false
  }
}

// ─── Edit ───
function openEdit(row) {
  selected.value = row
  form.value = {
    libelle:         row.libelle         ?? '',
    marque:          row.marque          ?? '',
    modele:          row.modele          ?? '',
    numSerie:        row.numSerie        ?? '',
    dateAcquisition: row.dateAcquisition ?? '',
    actif:           row.actif           ?? true,
    typeMaterielId:  row.typeMaterielId  ?? 1,
    commentaires:    row.commentaires    ?? '',
  }
  showEdit.value = true
}
async function submitEdit() {
  if (!selected.value) return
  saving.value = true
  try {
    await apiPut(`/Materials?id=${selected.value.id}`, {
      libelle:         form.value.libelle,
      marque:          form.value.marque,
      modele:          form.value.modele,
      numSerie:        form.value.numSerie,
      dateAcquisition: form.value.dateAcquisition,
      actif:           form.value.actif,
      typeMaterielId:  Number(form.value.typeMaterielId),
      commentaires:    form.value.commentaires,
    })
    success('Matériel mis à jour.')
    showEdit.value = false
    loadData()
  } catch (e) {
    toastError(e.message || 'Erreur lors de la modification.')
  } finally {
    saving.value = false
  }
}

// ─── Delete ───
function openDel(row) {
  selected.value = row
  showDel.value = true
}
async function submitDel() {
  if (!selected.value) return
  saving.value = true
  try {
    await apiDelete(`/Materials/${selected.value.id}`)
    success('Matériel supprimé.')
    showDel.value = false
    loadData()
  } catch (e) {
    toastError(e.message || 'Action impossible (matériel en cours d\'utilisation ?).')
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <div>
    <PageHeader
      title="Parc Matériel"
      description="Consultez et gérez les actifs du parc informatique."
      badge="Materials API"
      :icon="Monitor"
    >
      <template #actions>
        <button v-if="isAdmin()" class="btn btn-primary" @click="openAdd">
          <PlusCircle :size="15" />
          Ajouter un matériel
        </button>
      </template>
    </PageHeader>

    <!-- Search bar -->
    <div class="flex items-center gap-2 mb-4">
      <div class="flex items-center gap-2 flex-1 max-w-xs rounded-xl px-3 py-2.5 text-sm"
        style="background:rgba(8,12,30,0.8);border:1px solid rgba(255,255,255,0.08);">
        <Search :size="15" class="text-slate-500 flex-shrink-0" />
        <input v-model="query" placeholder="Rechercher…" class="bg-transparent outline-none text-slate-200 placeholder-slate-600 w-full" />
      </div>
      <div class="text-xs text-slate-600 ml-2">{{ filtered.length }} résultat{{ filtered.length !== 1 ? 's' : '' }}</div>
    </div>

    <!-- Table -->
    <DataTable
      :columns="columns"
      :rows="filtered"
      :loading="loading"
      :show-actions="isAdmin()"
      empty-text="Aucun matériel trouvé. Commencez par en ajouter un."
      @edit="openEdit"
      @delete="openDel"
    >
      <template #cell-actif="{ value }">
        <span :class="value ? 'badge badge-green' : 'badge badge-red'">
          {{ value ? 'Actif' : 'Inactif' }}
        </span>
      </template>
      <template #cell-dateAcquisition="{ value }">
        <span class="text-slate-400">{{ value ?? '—' }}</span>
      </template>
      <template #actions="{ row }">
        <button class="btn btn-ghost btn-icon" title="Détails" @click="router.push('/materiels/' + row.id)">
          <MoreHorizontal :size="14" class="text-slate-300" />
        </button>
        <button class="btn btn-ghost btn-icon" title="Modifier" @click="openEdit(row)">
          <Pencil :size="14" class="text-indigo-400" />
        </button>
        <button class="btn btn-icon" title="Supprimer" @click="openDel(row)"
          style="background:rgba(239,68,68,0.08);border:1px solid rgba(239,68,68,0.15);">
          <Trash2 :size="14" class="text-rose-400" />
        </button>
      </template>
    </DataTable>

    <!-- ADD Modal -->
    <Modal
      v-if="showAdd"
      title="Ajouter un matériel"
      confirm-text="Enregistrer"
      :loading="saving"
      @close="showAdd = false"
      @confirm="submitAdd"
    >
      <div class="grid grid-cols-2 gap-4">
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
          <label class="form-label">Statut</label>
          <select v-model="form.actif" class="form-input">
            <option :value="true">Actif</option>
            <option :value="false">Inactif</option>
          </select>
        </div>
        <div class="form-group col-span-2">
          <label class="form-label">Commentaires</label>
          <input v-model="form.commentaires" class="form-input" placeholder="Remarques optionnelles…" />
        </div>
      </div>
    </Modal>

    <!-- EDIT Modal -->
    <Modal
      v-if="showEdit"
      title="Modifier le matériel"
      confirm-text="Enregistrer"
      :loading="saving"
      @close="showEdit = false"
      @confirm="submitEdit"
    >
      <div class="grid grid-cols-2 gap-4">
        <div class="form-group">
          <label class="form-label">Libellé</label>
          <input v-model="form.libelle" class="form-input" />
        </div>
        <div class="form-group">
          <label class="form-label">Marque</label>
          <input v-model="form.marque" class="form-input" />
        </div>
        <div class="form-group">
          <label class="form-label">Modèle</label>
          <input v-model="form.modele" class="form-input" />
        </div>
        <div class="form-group">
          <label class="form-label">N° Série</label>
          <input v-model="form.numSerie" class="form-input" />
        </div>
        <div class="form-group">
          <label class="form-label">Date d'acquisition</label>
          <input v-model="form.dateAcquisition" type="date" class="form-input" />
        </div>
        <div class="form-group">
          <label class="form-label">Statut</label>
          <select v-model="form.actif" class="form-input">
            <option :value="true">Actif</option>
            <option :value="false">Inactif</option>
          </select>
        </div>
        <div class="form-group col-span-2">
          <label class="form-label">Commentaires</label>
          <input v-model="form.commentaires" class="form-input" />
        </div>
      </div>
    </Modal>

    <!-- DELETE Confirm Modal -->
    <Modal
      v-if="showDel"
      title="Supprimer le matériel ?"
      confirm-text="Supprimer"
      confirm-class="btn btn-danger"
      :loading="saving"
      size="sm"
      @close="showDel = false"
      @confirm="submitDel"
    >
      <p class="text-sm text-slate-400 leading-relaxed">
        Vous êtes sur le point de supprimer
        <strong class="text-slate-200">{{ selected?.libelle }} ({{ selected?.marque }})</strong>.
        Cette action est irréversible si le matériel n'a pas d'historique.
      </p>
    </Modal>
  </div>
</template>
