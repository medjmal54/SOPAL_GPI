<script setup>
import { ref, computed, onMounted } from 'vue'
import Modal from '../components/Modal.vue'
import { apiGet, apiPost, apiPut } from '../services/api'
import { useToast } from '../composables/useToast'

const { success, error: toastError } = useToast()

const items = ref([])
const materials = ref([])
const loading = ref(true)
const query = ref('')
const selectedEmp = ref(null)

const showAdd = ref(false)
const showEdit = ref(false)
const saving = ref(false)
const selected = ref(null)

const emptyForm = () => ({ nom: '', prenom: '', email: '', service: '', poste: '' })
const form = ref(emptyForm())

async function loadData() {
  loading.value = true
  try {
    const [emps, mats] = await Promise.all([apiGet('/Employes'), apiGet('/Materials')])
    items.value = Array.isArray(emps) ? emps : []
    materials.value = Array.isArray(mats) ? mats : []
    if (items.value.length > 0) selectedEmp.value = items.value[0]
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

function selectEmp(emp) { selectedEmp.value = emp }

function getInitials(nom, prenom) {
  return ((prenom?.[0] ?? '') + (nom?.[0] ?? '')).toUpperCase() || '?'
}

function empMaterials(emp) {
  if (!emp) return []
  const mat = emp.matricule ?? emp.Matricule
  return materials.value.filter(m => (m.employeMat ?? m.EmployeMat) === mat)
}

function raw(obj, k1, k2) { return obj[k1] ?? obj[k2] ?? null }

const EmployeeFormFields = [
  { key: 'nom', label: 'Nom', placeholder: 'ex: Benali' },
  { key: 'prenom', label: 'Prénom', placeholder: 'ex: Karim' },
  { key: 'email', label: 'Email', placeholder: 'ex: k.benali@sopal.dz', type: 'email' },
  { key: 'service', label: 'Service', placeholder: 'ex: Informatique' },
  { key: 'poste', label: 'Poste', placeholder: 'ex: Responsable IT' },
]

function openAdd() { form.value = emptyForm(); showAdd.value = true }
async function submitAdd() {
  saving.value = true
  try {
    await apiPost('/Employes', form.value)
    success('Employé ajouté.'); showAdd.value = false; loadData()
  } catch (e) { toastError(e.message || 'Erreur.')
  } finally { saving.value = false }
}

function openEdit(row) {
  selected.value = row
  form.value = { nom: row.nom ?? '', prenom: row.prenom ?? '', email: row.email ?? '', service: row.service ?? '', poste: row.poste ?? '' }
  showEdit.value = true
}
async function submitEdit() {
  if (!selected.value) return
  saving.value = true
  try {
    await apiPut(`/Employes/${selected.value.matricule}`, { matricule: selected.value.matricule, ...form.value })
    success('Employé mis à jour.'); showEdit.value = false; loadData()
  } catch (e) { toastError(e.message || 'Erreur.')
  } finally { saving.value = false }
}
</script>

<template>
  <div class="workspace-layout flex h-[calc(100vh-64px)] overflow-hidden -m-container-padding">
    <!-- Left Panel: Employee List -->
    <aside class="w-80 flex flex-col border-r border-outline-variant bg-surface-container-low shrink-0">
      <div class="p-4 border-b border-outline-variant">
        <div class="flex items-center justify-between mb-3">
          <h2 class="font-headline-sm text-headline-sm font-bold text-on-surface">Employés</h2>
          <span class="font-label-sm bg-surface-container-high px-2 py-0.5 rounded border border-outline-variant text-on-surface-variant">{{ items.length }}</span>
        </div>
        <div class="relative">
          <span class="material-symbols-outlined absolute left-3 top-1/2 -translate-y-1/2 text-on-surface-variant text-[18px]">search</span>
          <input v-model="query" class="w-full bg-surface border border-outline-variant rounded-lg pl-9 pr-3 py-2 font-label-md text-on-surface focus:border-primary focus:ring-1 focus:ring-primary outline-none" placeholder="Rechercher..." type="text">
        </div>
      </div>
      <div class="flex-1 overflow-y-auto p-2 space-y-1">
        <div v-if="loading" class="flex items-center justify-center py-12"><div class="spinner" /></div>
        <button
          v-for="emp in filtered"
          :key="emp.matricule ?? emp.Matricule"
          class="w-full flex items-center gap-3 p-3 rounded-lg cursor-pointer transition-all text-left"
          :class="selectedEmp === emp ? 'bg-secondary-container text-on-secondary-container' : 'hover:bg-surface-container-highest'"
          @click="selectEmp(emp)"
        >
          <div class="w-10 h-10 rounded-full bg-surface-container-highest flex items-center justify-center font-bold text-primary shrink-0 border border-outline-variant/30">
            {{ getInitials(emp.nom, emp.prenom) }}
          </div>
          <div class="flex-1 min-w-0">
            <div class="font-label-md text-label-md font-bold truncate" :class="selectedEmp === emp ? 'text-on-secondary-container' : 'text-on-surface'">{{ emp.prenom }} {{ emp.nom }}</div>
            <div class="font-label-sm text-label-sm" :class="selectedEmp === emp ? 'text-on-secondary-container/70' : 'text-on-surface-variant'">{{ emp.service || emp.poste || '—' }}</div>
          </div>
          <div v-if="empMaterials(emp).length" class="px-2 py-0.5 rounded border font-label-sm text-[10px] font-bold" :class="selectedEmp === emp ? 'border-on-secondary-container/20 bg-on-secondary-container/10' : 'border-outline-variant/20 bg-surface-container-high text-on-surface-variant'">
            {{ empMaterials(emp).length }} BIENS
          </div>
        </button>
        <div v-if="!loading && filtered.length === 0" class="flex flex-col items-center py-12 text-on-surface-variant">
          <span class="material-symbols-outlined text-[36px] mb-2">badge</span>
          <span class="font-body-sm text-body-sm">Aucun employé trouvé</span>
        </div>
      </div>
      <div class="p-3 border-t border-outline-variant">
        <button @click="openAdd" class="w-full flex items-center justify-center gap-2 py-2 border border-outline-variant rounded-lg text-primary hover:bg-surface-container-high transition-colors font-label-md text-label-md">
          <span class="material-symbols-outlined text-[18px]">person_add</span> Ajouter un employé
        </button>
      </div>
    </aside>

    <!-- Center Panel: Employee Details -->
    <main class="flex-1 min-w-0 overflow-y-auto bg-background p-container-padding">
      <div v-if="!selectedEmp" class="flex items-center justify-center h-full text-on-surface-variant">
        <div class="text-center">
          <span class="material-symbols-outlined text-[48px]">badge</span>
          <p class="font-body-md text-body-md mt-2">Sélectionnez un employé</p>
        </div>
      </div>

      <template v-else>
        <div class="flex items-start gap-6 mb-8">
          <div class="w-20 h-20 rounded-xl overflow-hidden border-2 border-outline-variant/50 bg-surface-container-higher flex items-center justify-center shrink-0">
            <span class="font-display-lg text-display-lg font-bold text-primary">{{ getInitials(selectedEmp.nom, selectedEmp.prenom) }}</span>
          </div>
          <div class="flex-1">
            <div class="flex items-center justify-between">
              <h1 class="font-headline-md text-headline-md text-on-surface">{{ selectedEmp.prenom }} {{ selectedEmp.nom }}</h1>
              <span class="status-chip affecte">
                <span class="w-1.5 h-1.5 rounded-full bg-tertiary mr-1.5 inline-block"></span>
                Actif
              </span>
            </div>
            <div class="grid grid-cols-1 sm:grid-cols-2 gap-y-4 mt-4">
              <div>
                <p class="font-label-sm text-label-sm text-on-surface-variant uppercase tracking-widest">Matricule</p>
                <p class="font-label-md text-label-md text-primary">{{ selectedEmp.matricule ?? selectedEmp.Matricule }}</p>
              </div>
              <div>
                <p class="font-label-sm text-label-sm text-on-surface-variant uppercase tracking-widest">Email</p>
                <p class="font-body-md text-body-md text-on-surface">{{ selectedEmp.email || '—' }}</p>
              </div>
              <div>
                <p class="font-label-sm text-label-sm text-on-surface-variant uppercase tracking-widest">Service</p>
                <p class="font-body-md text-body-md text-on-surface">{{ selectedEmp.service || '—' }}</p>
              </div>
              <div>
                <p class="font-label-sm text-label-sm text-on-surface-variant uppercase tracking-widest">Poste</p>
                <p class="font-body-md text-body-md text-on-surface">{{ selectedEmp.poste || '—' }}</p>
              </div>
            </div>
          </div>
          <button @click="openEdit(selectedEmp)" class="btn btn-ghost">
            <span class="material-symbols-outlined text-[16px]">edit</span> Modifier
          </button>
        </div>

        <hr class="border-outline-variant/30 mb-6">

        <div>
          <h2 class="font-headline-md text-headline-md text-on-surface flex items-center gap-2 mb-4">
            <span class="material-symbols-outlined text-primary">devices</span>
            Matériels assignés
          </h2>
          <div v-if="empMaterials(selectedEmp).length === 0" class="font-body-md text-body-md text-on-surface-variant py-8 text-center">
            Aucun matériel assigné.
          </div>
          <div v-else class="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div v-for="mat in empMaterials(selectedEmp)" :key="mat.id ?? mat.Id" class="bg-surface-container border border-outline-variant rounded-xl p-4 flex gap-4 hover:border-primary/50 transition-all group">
              <div class="w-14 h-14 rounded-lg bg-surface-container-highest flex items-center justify-center shrink-0">
                <span class="material-symbols-outlined text-[28px] text-on-surface-variant group-hover:text-primary transition-colors">devices</span>
              </div>
              <div class="flex-1 min-w-0">
                <div class="flex justify-between items-start">
                  <h3 class="font-body-md text-body-md font-bold text-on-surface">{{ raw(mat, 'libelle', 'Libelle') }}</h3>
                  <span class="status-chip en-stock-blue">{{ raw(mat, 'marque', 'Marque') }}</span>
                </div>
                <div class="mt-2 space-y-1">
                  <p class="font-label-sm text-label-sm text-on-surface-variant flex justify-between">ID: <span class="text-on-surface">#{{ mat.id ?? mat.Id }}</span></p>
                  <p class="font-label-sm text-label-sm text-on-surface-variant flex justify-between">Série: <span class="text-on-surface font-label-md">{{ raw(mat, 'numSerie', 'NumSerie') || '—' }}</span></p>
                </div>
              </div>
            </div>
          </div>
        </div>
      </template>
    </main>

    <!-- Right Panel: Actions -->
    <aside class="w-72 border-l border-outline-variant bg-surface-container-low p-6 flex flex-col gap-6 shrink-0">
      <h2 class="font-label-md text-label-md font-bold text-on-surface uppercase tracking-widest border-b border-outline-variant pb-4">Actions</h2>
      <div class="space-y-3">
        <button @click="openAdd" class="w-full flex items-center justify-center gap-2 bg-primary text-on-primary py-3 rounded-lg font-bold hover:opacity-90 transition-opacity">
          <span class="material-symbols-outlined text-[20px]">person_add</span>
          Nouvel Employé
        </button>
        <button @click="openEdit(selectedEmp)" v-if="selectedEmp" class="w-full flex items-center justify-center gap-2 bg-surface-container-highest text-on-surface py-3 rounded-lg font-bold border border-outline-variant hover:bg-surface-bright transition-colors">
          <span class="material-symbols-outlined text-[20px]">edit</span>
          Modifier
        </button>
      </div>
      <div class="mt-auto pt-6 border-t border-outline-variant">
        <div class="bg-surface-container p-4 rounded-xl border border-outline-variant">
          <div class="flex items-center gap-2 mb-3">
            <span class="material-symbols-outlined text-primary text-[18px]">verified_user</span>
            <span class="font-label-sm text-label-sm text-on-surface-variant uppercase">Conformité</span>
          </div>
          <div class="flex items-end gap-2">
            <div class="font-headline-md text-headline-md text-tertiary">98.2%</div>
            <div class="font-label-sm text-label-sm text-on-surface-variant mb-1">Score</div>
          </div>
          <div class="w-full bg-outline-variant h-1.5 rounded-full mt-3">
            <div class="bg-tertiary h-full rounded-full" style="width:98%"></div>
          </div>
        </div>
      </div>
    </aside>
  </div>

  <Modal v-if="showAdd" title="Ajouter un employé" confirm-text="Enregistrer" :loading="saving" @close="showAdd = false" @confirm="submitAdd">
    <div class="space-y-3">
      <div v-for="f in EmployeeFormFields" :key="f.key" class="form-group">
        <label class="form-label">{{ f.label }}</label>
        <input v-model="form[f.key]" :type="f.type ?? 'text'" :placeholder="f.placeholder" class="form-input" />
      </div>
    </div>
  </Modal>

  <Modal v-if="showEdit" :title="`Modifier — ${selected?.nom} ${selected?.prenom}`" confirm-text="Enregistrer" :loading="saving" @close="showEdit = false" @confirm="submitEdit">
    <div class="space-y-3">
      <div v-for="f in EmployeeFormFields" :key="f.key" class="form-group">
        <label class="form-label">{{ f.label }}</label>
        <input v-model="form[f.key]" :type="f.type ?? 'text'" class="form-input" />
      </div>
    </div>
  </Modal>
</template>
