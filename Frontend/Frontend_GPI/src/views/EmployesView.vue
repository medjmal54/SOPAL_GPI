<script setup>
import { ref, computed, onMounted } from 'vue'
import { Users, PlusCircle, Search, Pencil } from 'lucide-vue-next'
import PageHeader from '../components/PageHeader.vue'
import DataTable  from '../components/DataTable.vue'
import Modal      from '../components/Modal.vue'
import { apiGet, apiPost, apiPut } from '../services/api'
import { useToast } from '../composables/useToast'

const { success, error: toastError } = useToast()

const items    = ref([])
const loading  = ref(true)
const query    = ref('')
const showAdd  = ref(false)
const showEdit = ref(false)
const saving   = ref(false)
const selected = ref(null)

const emptyForm = () => ({
  nom: '', prenom: '', email: '', service: '', poste: ''
})
const form = ref(emptyForm())

async function loadData() {
  loading.value = true
  try {
    const data = await apiGet('/Employes')
    items.value = Array.isArray(data) ? data : []
  } catch (e) {
    toastError(e.message || 'Erreur de chargement.')
  } finally {
    loading.value = false
  }
}

onMounted(loadData)

const filtered = computed(() => {
  const list = items.value || []
  if (!query.value) return list
  const q = query.value.toLowerCase()
  return list.filter(i => JSON.stringify(i).toLowerCase().includes(q))
})

const columns = [
  { key: 'matricule', label: 'Matricule', sortable: true  },
  { key: 'nom',       label: 'Nom',       sortable: true  },
  { key: 'prenom',    label: 'Prénom',    sortable: true  },
  { key: 'email',     label: 'Email',     sortable: false },
  { key: 'service',   label: 'Service',   sortable: true  },
  { key: 'poste',     label: 'Poste',     sortable: true  },
]

// ─── Add ───
function openAdd() {
  form.value = emptyForm()
  showAdd.value = true
}
async function submitAdd() {
  saving.value = true
  try {
    await apiPost('/Employes', form.value)
    success('Employé ajouté avec succès.')
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
    nom:     row.nom     ?? '',
    prenom:  row.prenom  ?? '',
    email:   row.email   ?? '',
    service: row.service ?? '',
    poste:   row.poste   ?? '',
  }
  showEdit.value = true
}
async function submitEdit() {
  if (!selected.value) return
  saving.value = true
  try {
    await apiPut(`/Employes/${selected.value.matricule}`, {
      matricule: selected.value.matricule,
      ...form.value,
    })
    success('Employé mis à jour.')
    showEdit.value = false
    loadData()
  } catch (e) {
    toastError(e.message || 'Erreur lors de la modification.')
  } finally {
    saving.value = false
  }
}

const EmployeeFormFields = [
  { key: 'nom',     label: 'Nom',     placeholder: 'ex: Benali' },
  { key: 'prenom',  label: 'Prénom',  placeholder: 'ex: Karim'  },
  { key: 'email',   label: 'Email',   placeholder: 'ex: k.benali@sopal.dz', type: 'email' },
  { key: 'service', label: 'Service', placeholder: 'ex: Informatique' },
  { key: 'poste',   label: 'Poste',   placeholder: 'ex: Responsable IT' },
]
</script>

<template>
  <div>
    <PageHeader
      title="Personnel"
      description="Gestion des collaborateurs et de leurs affectations matérielles."
      badge="Employes API"
      :icon="Users"
    >
      <template #actions>
        <button class="btn btn-primary" @click="openAdd">
          <PlusCircle :size="15" />
          Ajouter un employé
        </button>
      </template>
    </PageHeader>

    <div class="flex items-center gap-2 mb-4">
      <div class="flex items-center gap-2 flex-1 max-w-xs rounded-xl px-3 py-2.5 text-sm"
        style="background:rgba(8,12,30,0.8);border:1px solid rgba(255,255,255,0.08);">
        <Search :size="15" class="text-slate-500 flex-shrink-0" />
        <input v-model="query" placeholder="Rechercher un employé…" class="bg-transparent outline-none text-slate-200 placeholder-slate-600 w-full" />
      </div>
      <div class="text-xs text-slate-600 ml-2">{{ filtered.length }} résultat{{ filtered.length !== 1 ? 's' : '' }}</div>
    </div>

    <DataTable
      :columns="columns"
      :rows="filtered"
      :loading="loading"
      :show-actions="true"
      empty-text="Aucun employé trouvé."
      @edit="openEdit"
    >
      <template #cell-matricule="{ value }">
        <span class="font-mono text-xs text-indigo-300 badge badge-indigo">{{ value }}</span>
      </template>
      <template #cell-email="{ value }">
        <span class="text-slate-400 text-xs">{{ value}}</span>
      </template>
      <template #actions="{ row }">
        <button class="btn btn-ghost btn-icon" title="Modifier" @click="openEdit(row)">
          <Pencil :size="14" class="text-indigo-400" />
        </button>
      </template>
    </DataTable>

    <!-- ADD Modal -->
    <Modal
      v-if="showAdd"
      title="Ajouter un employé"
      confirm-text="Enregistrer"
      :loading="saving"
      @close="showAdd = false"
      @confirm="submitAdd"
    >
      <div class="space-y-3">
        <div v-for="f in EmployeeFormFields" :key="f.key" class="form-group">
          <label class="form-label">{{ f.label }}</label>
          <input
            v-model="form[f.key]"
            :type="f.type ?? 'text'"
            :placeholder="f.placeholder"
            class="form-input"
          />
        </div>
      </div>
    </Modal>

    <!-- EDIT Modal -->
    <Modal
      v-if="showEdit"
      :title="`Modifier — ${selected?.nom} ${selected?.prenom}`"
      confirm-text="Enregistrer"
      :loading="saving"
      @close="showEdit = false"
      @confirm="submitEdit"
    >
      <div class="space-y-3">
        <div v-for="f in EmployeeFormFields" :key="f.key" class="form-group">
          <label class="form-label">{{ f.label }}</label>
          <input
            v-model="form[f.key]"
            :type="f.type ?? 'text'"
            class="form-input"
          />
        </div>
      </div>
    </Modal>
  </div>
</template>
