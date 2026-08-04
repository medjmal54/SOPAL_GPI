<script setup>
import { computed, onMounted, ref, watch } from 'vue'
import { Bar, Doughnut } from 'vue-chartjs'
import { ArcElement, BarElement, CategoryScale, Chart as ChartJS, LinearScale, Tooltip } from 'chart.js'
import { apiGet } from '../services/api'
import { useExport } from '../composables/useExport'

ChartJS.register(ArcElement, BarElement, CategoryScale, LinearScale, Tooltip)

const loading = ref(true)
const error = ref('')
const exporting = ref(false)
const dashboard = ref(null)
const dateRange = ref('30')
const department = ref('all')
const selectedTypes = ref([])
const typesInitialized = ref(false)
let loadTimer = null

const { exportData } = useExport()

const pick = (object, ...keys) => keys.map(key => object?.[key]).find(value => value !== undefined && value !== null)

const types = computed(() => dashboard.value?.types ?? [])
const departments = computed(() => dashboard.value?.departments ?? [])
const metrics = computed(() => dashboard.value?.metrics ?? [])
const movementChart = computed(() => dashboard.value?.movementChart ?? { labels: [], datasets: [] })
const distribution = computed(() => dashboard.value?.distribution ?? [])
const categoryCount = computed(() => dashboard.value?.categoryCount ?? 0)
const filteredMaterialsTotal = computed(() => dashboard.value?.filteredMaterialsTotal ?? 0)

async function loadData() {
  loading.value = true
  error.value = ''
  try {
    const params = new URLSearchParams({
      period: dateRange.value === 'all' ? '0' : dateRange.value,
      department: department.value,
    })
    if (selectedTypes.value.length) params.set('types', selectedTypes.value.join(','))
    else if (typesInitialized.value) params.set('types', '-1')

    const data = await apiGet(`/dashboard?${params.toString()}`)
    dashboard.value = data

    if (!typesInitialized.value && (data?.types ?? []).length) {
      selectedTypes.value = data.types.map(type => Number(pick(type, 'id', 'Id')))
      typesInitialized.value = true
    }
  } catch (exception) {
    error.value = exception.message || 'Impossible de charger le tableau de bord.'
  } finally {
    loading.value = false
  }
}

onMounted(loadData)

watch([dateRange, department], loadData)
watch(selectedTypes, () => {
  clearTimeout(loadTimer)
  loadTimer = setTimeout(loadData, 250)
}, { deep: true })

function toggleType(id) {
  id = Number(id)
  selectedTypes.value = selectedTypes.value.includes(id)
    ? selectedTypes.value.filter(item => item !== id)
    : [...selectedTypes.value, id]
}

function resetFilters() {
  dateRange.value = '30'
  department.value = 'all'
  selectedTypes.value = types.value.map(type => Number(pick(type, 'id', 'Id')))
}

const palette = ['#9bbcff', '#45d6a0', '#ffaaa6', '#c4c6d0', '#f5b942', '#b5a7ff', '#4ac6e8']
const doughnutData = computed(() => ({
  labels: distribution.value.map(entry => entry.label),
  datasets: [{ data: distribution.value.map(entry => entry.value), backgroundColor: distribution.value.map((_, i) => palette[i % palette.length]), borderColor: '#1c1f26', borderWidth: 2 }],
}))
const tooltip = { backgroundColor: '#171a21', borderColor: '#414652', borderWidth: 1, titleColor: '#fff', bodyColor: '#c5c8d2', padding: 11 }
const barOptions = {
  responsive: true, maintainAspectRatio: false,
  plugins: { legend: { display: false }, tooltip },
  scales: {
    x: { grid: { display: false }, border: { color: '#343943' }, ticks: { color: '#9da1ae', font: { family: 'JetBrains Mono', size: 10 } } },
    y: { beginAtZero: true, border: { display: false }, grid: { color: 'rgba(65,70,82,.35)' }, ticks: { display: false, precision: 0 } },
  },
}
const doughnutOptions = { responsive: true, maintainAspectRatio: false, cutout: '72%', plugins: { legend: { display: false }, tooltip } }

async function exportMouvements(format) {
  exporting.value = true
  try {
    const params = {
      period: dateRange.value === 'all' ? '0' : dateRange.value,
      department: department.value,
      types: selectedTypes.value.length ? selectedTypes.value.join(',') : '-1',
    }
    await exportData('mouvements', params, format)
  } catch (exception) {
    error.value = exception.message || "Erreur lors de l'export."
  } finally {
    exporting.value = false
  }
}
</script>

<template>
  <div class="dash -m-container-padding">
    <div v-if="loading" class="dash-state"><div class="spinner" /><p>Synchronisation avec le parc…</p></div>
    <div v-else-if="error" class="dash-state error-state">
      <span class="material-symbols-outlined">error</span><h2>Tableau de bord indisponible</h2><p>{{ error }}</p>
      <button class="action secondary" @click="loadData">Réessayer</button>
    </div>
    <template v-else>
      <main class="dash-main">
        <section class="metrics">
          <article v-for="metric in metrics" :key="metric.label" class="metric" :class="metric.tone">
            <div><span class="material-symbols-outlined">{{ metric.icon }}</span>{{ metric.label }}</div>
            <strong>{{ metric.value.toLocaleString('fr-FR') }}</strong><p>{{ metric.note }}</p>
          </article>
        </section>

        <section class="charts">
          <article class="panel movement-panel">
            <div class="panel-head">
              <h2>Mouvements récents</h2>
              <div class="legend">
                <span><i class="in" />Affectations</span>
                <span><i class="out" />Retours</span>
              </div>
            </div>
            <div class="bar-chart"><Bar :data="movementChart" :options="barOptions" /></div>
          </article>
          <article class="panel distribution-panel">
            <div class="panel-head">
              <h2>Répartition du parc</h2>
            </div>
            <div v-if="distribution.length" class="donut">
              <Doughnut :data="doughnutData" :options="doughnutOptions" />
              <div>
                <strong>{{ categoryCount }}</strong>
                <span>Catégories</span>
              </div>
            </div>
            <div v-else class="no-data">Aucune donnée pour ces filtres.</div>
            <ul>
              <li v-for="(entry, index) in distribution.slice(0, 5)" :key="entry.id"><span><i :style="{ background: palette[index % palette.length] }" />{{ entry.label }}</span><strong>{{ filteredMaterialsTotal ? Math.round(entry.value / filteredMaterialsTotal * 100) : 0 }}%</strong></li>
            </ul>
          </article>
        </section>
      </main>

      <aside class="filters">
        <div class="filter-title"><span class="material-symbols-outlined">filter_list</span><h2>Dashboard Filters</h2></div>
        <div class="filter-group"><label for="period">Période</label><select id="period" v-model="dateRange"><option value="7">7 derniers jours</option><option value="30">30 derniers jours</option><option value="90">90 derniers jours</option><option value="365">12 derniers mois</option><option value="all">Tout l'historique</option></select></div>
        <div class="filter-group"><label>Service</label><div class="departments"><button :class="{ active: department === 'all' }" @click="department = 'all'">Tous</button><button v-for="item in departments" :key="item" :class="{ active: department === item }" @click="department = item">{{ item }}</button></div></div>
        <div class="filter-group categories"><label>Catégorie de matériel</label><label v-for="type in types" :key="pick(type, 'id', 'Id')" class="check"><input type="checkbox" :checked="selectedTypes.includes(Number(pick(type, 'id', 'Id')))" @change="toggleType(pick(type, 'id', 'Id'))"><span>{{ pick(type, 'libelle', 'Libelle') }}</span></label></div>
        <button class="reset" @click="resetFilters"><span class="material-symbols-outlined">restart_alt</span>Réinitialiser les filtres</button>
      </aside>
    </template>
  </div>
</template>

<style scoped>
.dash{
  height:calc(100vh - 64px);
  min-height:0;
  display:grid;
  grid-template-columns: minmax(0,1fr) 344px;
  background:#101319;
  color:#e8e9f0;
  overflow-y:auto;
}

.dash-main{
  min-width:0;
  padding:30px;
}

.filters{
  padding:30px;
  border-left:1px solid #3b404a;
  background:#181b22;
  display:flex;
  flex-direction:column;
}

.dash-heading{
  display:flex;
  justify-content:space-between;
  align-items:center;
  gap:20px;
  margin-bottom:28px
}

.dash-heading h1{
  margin:0;
  font-size: clamp(27px,2vw,34px);
  font-weight:760;
  letter-spacing:-.04em
}

.dash-heading p{
  margin:7px 0 0;
  color:#aeb2bf;
  font:600 10px 'JetBrains Mono',monospace;
  letter-spacing:.12em
}

.action{
  min-height:44px;
  padding:0 18px;
  border:1px solid transparent;
  border-radius:8px;display:inline-flex;
  align-items:center;
  justify-content:center;
  gap:9px;
  font:650 12px 'JetBrains Mono',monospace;cursor:pointer
}

.action.primary{
  background:#9bbcff;
  color:#10284d
}

.action.secondary{
  background:#22262e;
  border-color:#444a56;
  color:#eee
}

.action:hover{
  filter:brightness(1.08)
}

.action .material-symbols-outlined{
  font-size:20px;
}

.metrics{
  display:grid;
  grid-template-columns:repeat(5,minmax(0,1fr));
  gap:16px;
  margin-bottom:15px
}

.metric{
  position:relative;
  min-height:152px;
  overflow:hidden;
  padding:20px;
  background:#1c1f26;
  border:1px solid #414650;
  border-radius:9px;
}

.metric:after{
  content:'';
  position:absolute;
  width:100px;height:100px;
  right:-35px;
  top:-42px;
  border-radius:50%;
  background:currentColor;
  opacity:.045
}

.metric>div{
  display:flex;
  align-items:center;
  gap:9px;
  color:#bec1cb;
  font:650 12px 'JetBrains Mono',monospace;
}

.metric>div span{
  font-size:21px
}

.metric strong{
  display:block;
  margin-top:18px;
  color:#eef0f6;
  font-size:clamp(30px,2.4vw,42px);
  line-height:.95
}

.metric p{
  margin:12px 0 0;
  color:#b6bac5;
  font:500 11px 'JetBrains Mono',monospace
}

.metric.blue>div{
  color:#a9c3ff
}

.metric.green>div,.metric.green p{
  color:#4ad7a3
}

.metric.amber>div,.metric.amber p{
  color:#efb33f
}

.metric.red>div,.metric.red p{
  color:#ffaaa6;
}

.charts{
  display:grid;
  grid-template-columns:minmax(0,2fr) minmax(280px,1fr);
  gap:15px;
  margin-bottom:35px
}

.panel{
  background:#1c1f26;
  border:1px solid #414650;
  border-radius:9px;
  overflow:hidden
}

.movement-panel,.distribution-panel{
  min-height:420px;
  padding:28px
}

.panel-head{
  display:flex;
  justify-content:space-between;
  align-items:center;
  gap:15px
}

.panel-head h2{
  margin:0;
  font-size:21px;
  letter-spacing:-.025em
}

.legend{
  display:flex;
  gap:14px;
  color:#b7bac4;
  font:550 10px 'JetBrains Mono',monospace
}

.legend span{
  display:flex;
  align-items:center;
  gap:6px;
}
.legend i,.distribution-panel li i{
  width:10px;
  height:10px;
  border-radius:50%;
  display:inline-block;
  flex:none
}

.legend .in{
  background:#9bbcff
}

.legend .out{
  background:#ffaaa6;
}

.bar-chart{
  height:310px;
  margin-top:26px
}

.donut{
  position:relative;
  height:220px;max-width:230px;
  margin:22px auto 12px;
}

.donut>div{
  position:absolute;
  top:50%;
  left:50%;
  transform:translate(-50%,-50%);
  display:flex;
  flex-direction:column;
  align-items:center;
  pointer-events:none;
}

.donut strong{
  font-size:35px;
}

.donut span{
  color:#b9bdc8;
  font:500 10px 'JetBrains Mono',monospace;
}

.distribution-panel ul{
  list-style:none;
  margin:6px 0 0;
  padding:0;
  display:grid;
  gap:10px
}

.distribution-panel li{
  display:flex;
  justify-content:space-between;
  gap:10px;
  color:#c3c6d0;
  font:500 10px 'JetBrains Mono',monospace
}

.distribution-panel li span{
  display:flex;
  align-items:center;
  gap:9px;
  min-width:0;
}

.no-data{
  height:220px;
  display:grid;
  place-items:center;
  color:#9297a3;
  font-size:13px
}

.activity-head{
  min-height:72px;
  padding:0 28px;
  border-bottom:1px solid #3c414b
}

.activity-head button{
  border:0;
  background:none;
  color:#a9c3ff;
  font:600 11px 'JetBrains Mono',monospace;
  cursor:pointer;
}

.table-wrap{
  overflow-x:auto;
}

.activity table{
  width:100%;
  min-width:900px;
  border-collapse:collapse;
  text-align:left;
}

.activity th{
  height:48px;
  padding:0 28px;
  background:#282b32;
  color:#b9bdc8;
  font:650 10px 'JetBrains Mono',monospace;
  letter-spacing:.07em;
  text-transform:uppercase;
}

.activity td{
  height:66px;
  padding:0 28px;
  border-top:1px solid #383d47;
  color:#d8dae1;
  font-size:13px;
  white-space:nowrap;
}

.activity tbody tr:hover{
  background:#22262e;
}

.activity .asset-id{
  color:#abc4ff;
  font:600 11px 'JetBrains Mono',monospace
}

.asset-name{
  font-weight:650;
}

.activity .date{
  color:#b3b7c2;
  font:500 10px 'JetBrains Mono',monospace;
}

.status{
  display:inline-flex;
  justify-content:center;
  min-width:88px;
  padding:7px 10px;
  border:1px solid;
  border-radius:999px;
  font:650 9px 'JetBrains Mono',monospace;
  text-transform:uppercase;
}

.status.complete{
  color:#4bdca5;
  border-color:#4bdca544;
  background:#4bdca517;
}

.status.review{
  color:#a8c1ff;
  border-color:#a8c1ff44;
  background:#a8c1ff15;
}

.status.pending{
  color:#efb33f;
  border-color:#efb33f44;
  background:#efb33f15;
}

.empty{
  height:150px;
  display:flex;
  align-items:center;
  justify-content:center;
  gap:10px;
  color:#999daa;
}

.filter-title
{
  display:flex;
  align-items:center;
  gap:10px;
  margin-bottom:34px;
}

.filter-title h2{
  margin:0;
  font-size:20px;
}

.filter-group{
  margin-bottom:30px;
}

.filter-group>label:first-child{
  display:block;
  margin-bottom:12px;
  color:#bec1cb;
  font:650 11px 'JetBrains Mono',monospace;
}

.filter-group select{
  width:100%;
  height:44px;
  padding:0 14px;
  border:1px solid #444a55;
  border-radius:8px;
  background:#20232a;
  color:#e1e3e9;
  outline:0;
}

.departments{
  display:flex;
  flex-wrap:wrap;
  gap:8px;
}

.departments button{
  max-width:100%;
  height:34px;
  padding:0 14px;
  overflow:hidden;
  border:1px solid #414651;
  border-radius:999px;
  background:transparent;
  color:#c3c6cf;
  text-overflow:ellipsis;
  cursor:pointer;
}

.departments button.active{
  border-color:#5d98ff;
  background:#5d98ff;
  color:#081d3d;
  font-weight:700;
}

.categories{
  display:grid;
  gap:11px;
}

.check{
  display:flex!important;
  align-items:center;
  gap:10px;
  margin:0!important;
  color:#d8dae1!important;
  font:500 13px Geist,sans-serif!important;
  cursor:pointer;
}

.check input{
  width:18px;
  height:18px;
  margin:0;
  accent-color:#9bbcff;
}

.reset{
  display:flex;
  align-items:center;
  gap:7px;
  padding:0;
  border:0;
  background:none;
  color:#9ebaff;
  font:550 10px 'JetBrains Mono',monospace;
  cursor:pointer;
}

.reset span{
  font-size:17px;
}

.reports{
  margin-top:auto;
  padding-top:30px;
  border-top:1px solid #3b404a;
}

.report-card{
  padding:18px;
  border:1px solid #404653;
  border-radius:9px;
  background:#20232d;
}

.report-card p{
  margin:0 0 16px;
  color:#c2c5ce;
  font-size:12px;
  line-height:1.45;
}

.report-card>div{
  display:grid;
  grid-template-columns:1fr 1fr;
  gap:8px;
}

.report-card button{
  height:38px;
  border:1px solid #444a55;
  border-radius:5px;
  background:#1b1e25;
  color:#d4d6dd;
  display:flex;
  align-items:center;
  justify-content:center;
  gap:7px;
  cursor:pointer;
}

.report-card button:hover{
  border-color:#9bbcff;
}

.report-card span{
  font-size:17px;
}

.full{
  width:100%;
  margin-top:16px
}

.dash-state{
  grid-column:1/-1;
  min-height:calc(100vh - 64px);
  display:flex;
  flex-direction:column;
  align-items:center;
  justify-content:center;
  gap:14px;
  color:#aeb2bf
}

.error-state>span{
  color:#ffaaa6;
  font-size:40px
}

@media(max-width:1250px){
  .dash{
    grid-template-columns:1fr
  }

  .filters
  {
    border-left:0;
    border-top:1px solid #3b404a;
    display:grid;
    grid-template-columns:repeat(3,1fr);
    gap:24px
  }

  .filter-title{
    grid-column:1/-1;
    margin:0
  }
  
  .filter-group{
    margin:0;
  }
  
  .reports{
    grid-column:1/-1;
    margin:0;
    display:grid;
    grid-template-columns:1fr 1fr;
    gap:16px;
    align-items:end;
  }
  
  .full{
    margin:0;
  }
}

@media(max-width:1000px){
  .metrics{
    grid-template-columns:repeat(3,1fr)
  }
  
  .charts{
    grid-template-columns:1fr
  }

  .distribution-panel{
    min-height:360px
  }
}

@media(max-width:720px){
  .dash-main,.filters{
    padding:20px 16px
  }
  
  .dash-heading{
    align-items:flex-start;
    flex-direction:column;
  }
  
  .metrics{
    grid-template-columns:repeat(2,1fr);
    gap:10px;
  }
  
  .metric{
    min-height:136px;
    padding:16px;
  }
  
  .metric:last-child{
    grid-column:1/-1
  }
  
  .movement-panel,.distribution-panel{
    padding:20px 16px
  }
  
  .bar-chart{
    height:260px
  }
  
  .legend{
    flex-direction:column;
    gap:7px;
  }

  .filters{
    display:flex;
  }
  
  .filter-title{
    margin-bottom:8px
  }
  
  .filter-group{
    margin-top:20px
  }

  .reset{
    margin-top:22px
  }
  
  .reports{
    display:block;
    margin-top:30px;
  }

  .full{
    margin-top:14px;
  }
}

@media print{
  .dash{
    display:block;
    height:auto;
    overflow:visible;
    background:#fff;
    color:#000
    }
    
  .dash-main{
    padding:0;
  }

  .filters,.dash-heading .action{
    display:none!important
  }
  .metrics{
    grid-template-columns:repeat(5,1fr)
    }
    
  .metric,.panel{
      break-inside:avoid;
      background:#fff;
      border-color:#aaa;
  }
    
  .metric strong,.panel h2,.activity td{
      color:#000
  }
    
  .charts{
      grid-template-columns:2fr 1fr;
  }
    
  .activity th{
      background:#eee;
      color:#333;
  }
}
</style>
