<script setup>
import { ref, computed, onMounted, watch } from 'vue'
import { apiGet } from '../services/api'
import { useToast } from '../composables/useToast'

const { error: toastError } = useToast()

const items = ref([])
const materials = ref([])
const localisations = ref([])
const employees = ref([])
const loading = ref(true)
const query = ref('')
const filterType = ref(null)
const filterLocation = ref('')
const dateFrom = ref('')
const dateTo = ref('')
const statusFilter = ref('all')
const page = ref(1)
const pageSize = 12

async function loadData() {
  loading.value = true
  try {
    const [mouvs, mats, locs, emps] = await Promise.all([
      apiGet('/v1/mouvements'), apiGet('/Materials'), apiGet('/Localistations'), apiGet('/Employes'),
    ])
    items.value = Array.isArray(mouvs) ? mouvs : []
    materials.value = Array.isArray(mats) ? mats : []
    localisations.value = Array.isArray(locs) ? locs : []
    employees.value = Array.isArray(emps) ? emps : []
  } catch (e) {
    toastError(e.message || 'Erreur de chargement.')
  } finally {
    loading.value = false
  }
}

onMounted(loadData)

function materialName(id) {
  const found = materials.value.find(m => (m.id ?? m.Id) === id)
  return found ? (found.libelle ?? found.Libelle ?? `#${id}`) : `#${id}`
}

function localisationName(id) {
  const found = localisations.value.find(l => (l.id ?? l.Id) === id)
  return found ? (found.nom ?? found.Nom ?? `#${id}`) : `#${id}`
}

function employeeName(matricule) {
  if (!matricule) return '—'
  const found = employees.value.find(e => (e.matricule ?? e.Matricule) === matricule)
  return found ? `${found.nom ?? found.Nom ?? ''} ${found.prenom ?? found.Prenom ?? ''}`.trim() || `#${matricule}` : `#${matricule}`
}

const typmovLabel = (t) => ({ 1: 'Affectation', 2: 'Retour', 3: 'Transfert', 4: 'Intervention' })[t] ?? String(t ?? '—')
const typmovBadge = (t) => ({ 1: 'status-chip affecte', 2: 'status-chip maintenance', 3: 'status-chip en-stock-blue', 4: 'status-chip hors-service' })[t] ?? 'status-chip'

const filtered = computed(() => {
  let list = items.value || []
  if (query.value) {
    const q = query.value.toLowerCase()
    list = list.filter(i => JSON.stringify(i).toLowerCase().includes(q))
  }
  if (filterType.value) {
    list = list.filter(i => (i.typmov ?? i.Typmov) === filterType.value)
  }
  if (filterLocation.value) {
    list = list.filter(i => String(i.localisationId ?? i.LocalisationId) === filterLocation.value)
  }
  if (dateFrom.value) {
    list = list.filter(i => new Date(i.dateMouvement ?? i.DateMouvement) >= new Date(`${dateFrom.value}T00:00:00`))
  }
  if (dateTo.value) {
    list = list.filter(i => new Date(i.dateMouvement ?? i.DateMouvement) <= new Date(`${dateTo.value}T23:59:59`))
  }
  if (statusFilter.value !== 'all') {
    list = list.filter(i => {
      const done = ['Terminé', 'Termine'].includes(i.statut ?? i.Statut) || !(i.statut ?? i.Statut)
      return statusFilter.value === 'completed' ? done : !done
    })
  }
  return list
})

const pageCount = computed(() => Math.max(1, Math.ceil(filtered.value.length / pageSize)))
const paginated = computed(() => filtered.value.slice((page.value - 1) * pageSize, page.value * pageSize))
watch([query, filterType, filterLocation, dateFrom, dateTo, statusFilter], () => { page.value = 1 })

function resetFilters() {
  query.value = ''
  filterType.value = null
  filterLocation.value = ''
  dateFrom.value = ''
  dateTo.value = ''
  statusFilter.value = 'all'
}

function exportCsv() {
  const quote = value => `"${String(value ?? '').replaceAll('"', '""')}"`
  const rows = filtered.value.map(m => [
    formatDate(m.dateMouvement ?? m.DateMouvement),
    materialName(m.materielId ?? m.MaterielId),
    typmovLabel(m.typmov ?? m.Typmov),
    employeeName(m.employeMat ?? m.EmployeMat),
    localisationName(m.localisationId ?? m.LocalisationId),
    (m.statut ?? m.Statut) || 'Terminé',
  ])
  const csv = ['Date,Matériel,Type,Employé,Localisation,Statut', ...rows.map(row => row.map(quote).join(','))].join('\n')
  const url = URL.createObjectURL(new Blob(['\ufeff' + csv], { type: 'text/csv;charset=utf-8' }))
  const link = document.createElement('a')
  link.href = url
  link.download = `historique-mouvements-${new Date().toISOString().slice(0, 10)}.csv`
  link.click()
  URL.revokeObjectURL(url)
}

const statusCounts = computed(() => {
  const total = items.value.length
  const completed = items.value.filter(i => (i.statut ?? i.Statut) === 'Terminé' || !(i.statut ?? i.Statut)).length
  return { total, completed, pending: total - completed }
})

function formatDate(d) {
  if (!d) return '—'
  const date = new Date(d)
  if (Number.isNaN(date.getTime())) return '—'
  return date.toLocaleDateString('fr-FR', { day: '2-digit', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit' })
}

function statutIcon(statut) {
  if (statut === 'Terminé') return { icon: 'check_circle', color: 'text-tertiary' }
  if (statut === 'En cours') return { icon: 'sync', color: 'text-primary' }
  return { icon: 'schedule', color: 'text-on-surface-variant' }
}
</script>

<template>
  <div class="movement-history workspace-layout flex -m-container-padding">
    <!-- Left Panel: Filters -->
    <section class="w-72 border-r border-outline-variant bg-surface-container-low p-container-padding flex flex-col gap-gutter overflow-y-auto shrink-0">
      <div class="flex items-center gap-2 text-primary">
        <span class="material-symbols-outlined">filter_list</span>
        <h3 class="font-headline-sm text-headline-sm">Filtres</h3>
      </div>

      <div class="space-y-gutter">
        <div>
          <label class="font-label-md text-label-md text-on-surface-variant block mb-3">Type de mouvement</label>
          <div class="space-y-2">
            <label class="flex items-center gap-3 p-2 rounded-lg hover:bg-surface-container-high cursor-pointer transition-colors border border-transparent hover:border-outline-variant/30 group">
              <input type="checkbox" :checked="filterType === null" @change="filterType = null" class="form-checkbox bg-surface-container border-outline-variant text-primary rounded focus:ring-0">
              <span class="font-body-md text-body-md group-hover:text-primary transition-colors">Tous</span>
            </label>
            <label class="flex items-center gap-3 p-2 rounded-lg hover:bg-surface-container-high cursor-pointer transition-colors border border-transparent hover:border-outline-variant/30 group">
              <input type="checkbox" :checked="filterType === 1" @change="filterType = 1" class="form-checkbox bg-surface-container border-outline-variant text-primary rounded focus:ring-0">
              <span class="font-body-md text-body-md group-hover:text-primary transition-colors">Affectation</span>
            </label>
            <label class="flex items-center gap-3 p-2 rounded-lg hover:bg-surface-container-high cursor-pointer transition-colors border border-transparent hover:border-outline-variant/30 group">
              <input type="checkbox" :checked="filterType === 3" @change="filterType = 3" class="form-checkbox bg-surface-container border-outline-variant text-primary rounded focus:ring-0">
              <span class="font-body-md text-body-md group-hover:text-primary transition-colors">Transfert</span>
            </label>
            <label class="flex items-center gap-3 p-2 rounded-lg hover:bg-surface-container-high cursor-pointer transition-colors border border-transparent hover:border-outline-variant/30 group">
              <input type="checkbox" :checked="filterType === 2" @change="filterType = 2" class="form-checkbox bg-surface-container border-outline-variant text-primary rounded focus:ring-0">
              <span class="font-body-md text-body-md group-hover:text-primary transition-colors">Retour</span>
            </label>
            <label class="flex items-center gap-3 p-2 rounded-lg hover:bg-surface-container-high cursor-pointer transition-colors border border-transparent hover:border-outline-variant/30 group">
              <input type="checkbox" :checked="filterType === 4" @change="filterType = 4" class="form-checkbox bg-surface-container border-outline-variant text-primary rounded focus:ring-0">
              <span class="font-body-md text-body-md group-hover:text-primary transition-colors">Intervention</span>
            </label>
          </div>
        </div>
        <div class="h-px bg-outline-variant/30"></div>
        <div>
          <label class="font-label-md text-label-md text-on-surface-variant block mb-3">Période</label>
          <span class="filter-caption">Du</span>
          <input v-model="dateFrom" type="date" class="filter-input">
          <span class="filter-caption">Au</span>
          <input v-model="dateTo" type="date" class="filter-input">
        </div>
        <div class="h-px bg-outline-variant/30"></div>
        <div>
          <label class="font-label-md text-label-md text-on-surface-variant block mb-3">Localisation</label>
          <select v-model="filterLocation" class="filter-input">
            <option value="">Toutes les localisations</option>
            <option v-for="loc in localisations" :key="loc.id ?? loc.Id" :value="String(loc.id ?? loc.Id)">{{ loc.nom ?? loc.Nom }}</option>
          </select>
        </div>
      </div>

      <div class="mt-auto">
        <button @click="resetFilters" class="w-full border border-primary text-primary hover:bg-primary/10 font-bold py-2 rounded transition-colors font-label-md text-label-md">
          Réinitialiser
        </button>
      </div>
    </section>

    <!-- Center Panel: Data Table -->
    <section class="flex-1 flex flex-col min-w-0 bg-background overflow-auto">
      <div class="p-container-padding pb-0 flex justify-between items-end flex-wrap gap-3">
          <div>
            <span class="audit-kicker">JOURNAL D'AUDIT</span>
          <h1 class="font-display-lg text-display-lg text-on-surface">Historique des Mouvements</h1>
          <p class="text-on-surface-variant font-body-md mt-1">Suivi des mouvements du parc informatique.</p>
        </div>
        <div class="flex items-center gap-3 flex-wrap">
          <div class="relative w-full sm:w-auto">
            <span class="material-symbols-outlined absolute left-3 top-1/2 -translate-y-1/2 text-on-surface-variant text-[16px]">search</span>
            <input v-model="query" class="w-full sm:w-52 bg-surface border border-outline-variant rounded-lg pl-9 pr-3 py-1.5 font-label-sm text-on-surface focus:border-primary focus:ring-1 focus:ring-primary outline-none" placeholder="Rechercher..." type="text">
          </div>
          <div class="flex items-center gap-compact-gap bg-surface-container rounded-lg p-1 border border-outline-variant/30">
            <button v-for="tab in [{ value: 'all', label: 'Tout' }, { value: 'pending', label: 'En cours' }, { value: 'completed', label: 'Terminé' }]" :key="tab.value" @click="statusFilter = tab.value" class="px-3 py-1.5 rounded font-label-md text-label-md" :class="statusFilter === tab.value ? 'bg-primary text-on-primary shadow-sm' : 'text-on-surface-variant'">{{ tab.label }}</button>
          </div>
        </div>
      </div>

      <div class="flex-1 overflow-auto p-container-padding pt-6">
        <div v-if="loading" class="flex items-center justify-center py-20"><div class="spinner" /></div>
        <div v-else class="bg-surface-container border border-outline-variant rounded-xl overflow-x-auto">
          <table class="w-full text-left border-collapse">
            <thead class="sticky top-0 bg-surface-container-high border-b border-outline-variant z-10">
              <tr>
                <th class="px-6 py-4 font-label-md text-label-md text-on-surface-variant uppercase tracking-wider">Date</th>
                <th class="px-6 py-4 font-label-md text-label-md text-on-surface-variant uppercase tracking-wider">Matériel</th>
                <th class="px-6 py-4 font-label-md text-label-md text-on-surface-variant uppercase tracking-wider">Type</th>
                <th class="px-6 py-4 font-label-md text-label-md text-on-surface-variant uppercase tracking-wider">Employé</th>
                <th class="px-6 py-4 font-label-md text-label-md text-on-surface-variant uppercase tracking-wider">Localisation</th>
                <th class="px-6 py-4 font-label-md text-label-md text-on-surface-variant uppercase tracking-wider">Statut</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-outline-variant/30">
              <tr v-for="(m, i) in paginated" :key="i" class="hover:bg-surface-container-highest transition-colors group" :class="i % 2 === 1 ? 'bg-surface-container-low' : ''">
                <td class="px-6 py-4 font-label-md text-label-md text-on-surface-variant">{{ formatDate(m.dateMouvement ?? m.DateMouvement) }}</td>
                <td class="px-6 py-4">
                  <div class="flex items-center gap-2">
                    <span class="w-2 h-2 rounded-full" :class="{'bg-tertiary': !(m.typmov ?? m.Typmov) || (m.typmov ?? m.Typmov) === 1, 'bg-secondary': (m.typmov ?? m.Typmov) === 4, 'bg-primary': (m.typmov ?? m.Typmov) === 3}"></span>
                    <span class="font-label-md text-label-md font-bold text-primary">{{ materialName(m.materielId ?? m.MaterielId) }}</span>
                  </div>
                </td>
                <td class="px-6 py-4">
                  <span :class="typmovBadge(m.typmov ?? m.Typmov)">{{ typmovLabel(m.typmov ?? m.Typmov) }}</span>
                </td>
                <td class="px-6 py-4 font-body-md text-body-md text-on-surface">{{ employeeName(m.employeMat ?? m.EmployeMat) }}</td>
                <td class="px-6 py-4 font-body-md text-body-md text-on-surface-variant">{{ localisationName(m.localisationId ?? m.LocalisationId) }}</td>
                <td class="px-6 py-4">
                  <div class="flex items-center gap-1.5" :class="statutIcon(m.statut ?? m.Statut).color">
                    <span class="material-symbols-outlined text-[18px]">{{ statutIcon(m.statut ?? m.Statut).icon }}</span>
                    <span class="font-label-md text-label-md">{{ (m.statut ?? m.Statut) || 'Terminé' }}</span>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
          <div v-if="paginated.length === 0" class="flex items-center justify-center py-16 text-on-surface-variant">
            <div class="text-center">
              <span class="material-symbols-outlined text-[40px]">swap_horiz</span>
              <p class="font-body-md text-body-md mt-2">Aucun mouvement trouvé</p>
            </div>
          </div>
        </div>
      </div>

      <!-- Pagination -->
      <div class="px-container-padding py-4 flex justify-between items-center border-t border-outline-variant/30 bg-surface-container-lowest">
        <span class="font-label-md text-label-md text-on-surface-variant">{{ filtered.length }} résultat{{ filtered.length !== 1 ? 's' : '' }} · Page {{ page }} / {{ pageCount }}</span>
        <div class="flex gap-2">
          <button class="page-button" :disabled="page === 1" @click="page--"><span class="material-symbols-outlined">chevron_left</span></button>
          <button class="page-button" :disabled="page === pageCount" @click="page++"><span class="material-symbols-outlined">chevron_right</span></button>
        </div>
      </div>
    </section>

    <!-- Right Panel: Export & Audit -->
    <section class="w-80 border-l border-outline-variant bg-surface-container p-container-padding flex flex-col gap-gutter overflow-y-auto shrink-0">
      <div class="flex items-center gap-2 text-primary">
        <span class="material-symbols-outlined">ios_share</span>
        <h3 class="font-headline-sm text-headline-sm">Export & Audit</h3>
      </div>

      <div class="bg-surface-container-highest/30 rounded-xl p-4 border border-outline-variant/30">
        <div class="relative z-10">
          <h4 class="font-headline-sm text-headline-sm text-on-surface mb-2">Générer rapport</h4>
          <p class="font-body-sm text-body-sm text-on-surface-variant mb-6">
            Exportez les mouvements filtrés au format Excel pour la conformité.
          </p>
          <button @click="exportCsv" class="w-full bg-[#107C41] hover:bg-[#0D6234] text-white font-bold py-3 px-4 rounded-lg flex items-center justify-between transition-all group/btn">
            <span class="flex items-center gap-2 font-label-md text-label-md">
              <span class="material-symbols-outlined text-[20px]">description</span>
              Export CSV
            </span>
            <span class="material-symbols-outlined text-[20px] opacity-0 group-hover/btn:opacity-100 transition-opacity">download</span>
          </button>
          <button @click="window.print()" class="w-full mt-3 bg-surface-container-highest hover:bg-surface-bright text-on-surface font-bold py-3 px-4 rounded-lg flex items-center justify-center gap-2 border border-outline-variant transition-all">
            <span class="material-symbols-outlined text-[20px]">print</span>
            <span class="font-label-md">Imprimer le journal</span>
          </button>
        </div>
      </div>

      <div class="flex flex-col gap-4">
        <h5 class="font-label-md text-label-md uppercase tracking-widest text-outline">Statistiques</h5>
        <div class="grid grid-cols-2 gap-3">
          <div class="bg-surface-container-low p-3 rounded-lg border border-outline-variant/30">
            <p class="font-label-sm text-label-sm text-on-surface-variant">TOTAL</p>
            <p class="font-display-lg text-display-lg text-primary mt-1">{{ statusCounts.total }}</p>
          </div>
          <div class="bg-surface-container-low p-3 rounded-lg border border-outline-variant/30">
            <p class="font-label-sm text-label-sm text-on-surface-variant">EN COURS</p>
            <p class="font-display-lg text-display-lg text-secondary mt-1">{{ statusCounts.pending }}</p>
          </div>
        </div>
      </div>

      <div class="mt-auto p-4 bg-primary-container/10 border border-primary/20 rounded-xl">
        <div class="flex gap-3">
          <span class="material-symbols-outlined text-primary">info</span>
          <div>
            <p class="font-label-md text-label-md text-primary font-bold">Audit</p>
            <p class="font-body-sm text-body-sm text-on-surface-variant mt-1">Tous les mouvements sont horodatés et tracés.</p>
          </div>
        </div>
      </div>
    </section>
  </div>
</template>

<style scoped>
.movement-history{min-height:calc(100vh - 64px);background:#10131a}.audit-kicker{display:block;margin-bottom:4px;color:#7ea5ef;font:700 9px 'JetBrains Mono',monospace;letter-spacing:.14em}.filter-caption{display:block;margin:10px 0 5px;color:#8c909f;font:600 9px 'JetBrains Mono',monospace;text-transform:uppercase}.filter-input{width:100%;height:40px;padding:0 10px;border:1px solid #424754;border-radius:5px;background:#10131a;color:#e1e2ec;font:500 11px 'JetBrains Mono',monospace}.page-button{width:32px;height:32px;border:1px solid #424754;border-radius:5px;background:#1d2027;display:grid;place-items:center}.page-button:disabled{opacity:.35}.movement-history table{min-width:930px}.movement-history tbody tr{height:62px}
@media(max-width:1250px){.movement-history{flex-wrap:wrap}.movement-history>section:first-child{width:250px}.movement-history>section:nth-child(2){min-width:600px}.movement-history>section:last-child{width:100%;border-left:0;border-top:1px solid #424754}}
@media(max-width:900px){.movement-history{display:block}.movement-history>section{width:100%!important;min-width:0!important;max-height:none!important}.movement-history>section:first-child,.movement-history>section:last-child{border-left:0;border-right:0}.movement-history>section:nth-child(2){min-height:0}.movement-history>section:first-child{display:block}}
@media print{.app-topbar,.movement-history>section:first-child,.movement-history>section:last-child,.movement-history input,.movement-history .page-button{display:none!important}.movement-history{display:block}.movement-history>section:nth-child(2){display:block}.movement-history table{min-width:0}*{background:white!important;color:#111!important}}
</style>
