<script setup>
defineProps({
  title:       { type: String,  required: true },
  description: { type: String,  default: '' },
  badgeColor:  { type: String,  default: 'indigo' },
  icon:        { type: Object,  default: null },
})
</script>

<template>
  <div class="mb-6">
    <div class="flex flex-wrap items-start justify-between gap-4">
      <div class="flex items-center gap-3">
        <div
          v-if="icon"
          class="flex items-center justify-center rounded-xl flex-shrink-0"
          style="width:42px;height:42px;background:rgba(99,102,241,0.12);border:1px solid rgba(99,102,241,0.22);box-shadow:0 0 20px rgba(99,102,241,0.15);"
        >
          <component :is="icon" :size="20" class="text-indigo-400" />
        </div>
        <div>
          <div class="flex items-center gap-2.5 flex-wrap">
            <h1 class="text-xl font-bold text-slate-100 tracking-tight">{{ title }}</h1>
          </div>
          <p v-if="description" class="mt-0.5 text-sm text-slate-500">{{ description }}</p>
        </div>
      </div>
      <div v-if="$slots.actions" class="flex items-center justify-end">
        <slot name="actions" />
      </div>
    </div>
    <hr class="divider mt-5" />
  </div>
</template>
