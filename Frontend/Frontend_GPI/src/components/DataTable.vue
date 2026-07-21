<script setup>
import { ref, computed } from 'vue'
import { ChevronUp, ChevronDown } from 'lucide-vue-next'

const props = defineProps({
  columns: { type: Array, required: true }, // [{ key, label, sortable?, render? }]
  rows:    { type: Array, required: true },
  loading: { type: Boolean, default: false },
  emptyText: { type: String, default: 'Aucune donnée disponible.' },
  showActions: { type: Boolean, default: false },
})

const emit = defineEmits(['edit', 'delete', 'row-click'])

const sortKey = ref('')
const sortAsc = ref(true)

function toggleSort(col) {
  if (!col.sortable) return
  if (sortKey.value === col.key) {
    sortAsc.value = !sortAsc.value
  } else {
    sortKey.value = col.key
    sortAsc.value = true
  }
}

const sortedRows = computed(() => {
  if (!sortKey.value) return props.rows
  return [...props.rows].sort((a, b) => {
    const av = a[sortKey.value] ?? ''
    const bv = b[sortKey.value] ?? ''
    if (av < bv) return sortAsc.value ? -1 : 1
    if (av > bv) return sortAsc.value ? 1 : -1
    return 0
  })
})

function cellValue(row, col) {
  if (col.render) return col.render(row[col.key], row)
  const v = row[col.key]
  if (v === null || v === undefined || v === '') return null
  if (typeof v === 'boolean') return v ? 'Oui' : 'Non'
  return v
}
</script>

<template>
  <div class="glass overflow-hidden">
    <!-- Loading overlay -->
    <div v-if="loading" class="flex items-center justify-center py-20">
      <div class="spinner" />
    </div>

    <template v-else>
      <div class="overflow-x-auto">
        <table class="data-table">
          <thead>
            <tr>
              <th
                v-for="col in columns"
                :key="col.key"
                @click="toggleSort(col)"
                :class="['select-none', col.sortable ? 'cursor-pointer' : '', sortKey === col.key ? 'sorted' : '']"
              >
                <div class="flex items-center gap-1">
                  {{ col.label }}
                  <span v-if="col.sortable" class="opacity-50">
                    <ChevronUp v-if="sortKey === col.key && sortAsc" :size="12" class="text-indigo-400 opacity-100" />
                    <ChevronDown v-else-if="sortKey === col.key && !sortAsc" :size="12" class="text-indigo-400 opacity-100" />
                    <ChevronUp v-else :size="12" />
                  </span>
                </div>
              </th>
              <th v-if="showActions" class="text-right">Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr
              v-for="(row, i) in sortedRows"
              :key="i"
              class="cursor-pointer"
              @click="emit('row-click', row)"
            >
              <td v-for="col in columns" :key="col.key">
                <!-- Custom slot per column key -->
                <slot :name="`cell-${col.key}`" :row="row" :value="row[col.key]">
                  <span v-if="cellValue(row, col) !== null">{{ cellValue(row, col) }}</span>
                  <span v-else class="muted">—</span>
                </slot>
              </td>
              <td v-if="showActions" class="text-right" @click.stop>
                <div class="flex items-center justify-end gap-1.5">
                  <slot name="actions" :row="row">
                    <button class="btn btn-ghost btn-sm" @click="emit('edit', row)">Modifier</button>
                    <button class="btn btn-danger btn-sm" @click="emit('delete', row)">Supprimer</button>
                  </slot>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- Empty state -->
      <div v-if="!sortedRows.length" class="empty-state">
        <svg width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1">
          <path d="M3 6h18M8 6V4h8v2M19 6v14a2 2 0 01-2 2H7a2 2 0 01-2-2V6"/>
        </svg>
        <p class="text-sm">{{ emptyText }}</p>
      </div>

      <!-- Row count -->
      <div
        v-if="sortedRows.length"
        class="px-4 py-2.5 text-xs text-slate-600 flex items-center justify-between"
        style="border-top:1px solid rgba(255,255,255,0.04);"
      >
        <span>{{ sortedRows.length }} enregistrement{{ sortedRows.length > 1 ? 's' : '' }}</span>
        <slot name="footer" />
      </div>
    </template>
  </div>
</template>
