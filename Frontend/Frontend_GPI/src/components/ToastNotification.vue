<script setup>
import { CheckCircle, XCircle, Info, X } from 'lucide-vue-next'
import { useToast } from '../composables/useToast'

const { toasts, dismiss } = useToast()

const iconMap = { success: CheckCircle, error: XCircle, info: Info }
const colorMap = { success: 'text-emerald-400', error: 'text-rose-400', info: 'text-indigo-400' }
</script>

<template>
  <teleport to="body">
    <div class="toast-container">
      <transition-group name="toast-list" tag="div" class="flex flex-col gap-2.5">
        <div
          v-for="t in toasts"
          :key="t.id"
          :class="`toast toast-${t.type}`"
        >
          <component :is="iconMap[t.type]" :size="18" :class="colorMap[t.type]" class="flex-shrink-0" />
          <span class="flex-1 text-sm text-slate-200 leading-snug">{{ t.message }}</span>
          <button class="btn btn-ghost btn-icon flex-shrink-0 p-1" @click="dismiss(t.id)">
            <X :size="14" class="text-slate-500" />
          </button>
        </div>
      </transition-group>
    </div>
  </teleport>
</template>

<style scoped>
.toast-list-enter-active { transition: all 0.3s ease; }
.toast-list-leave-active { transition: all 0.25s ease; position: absolute; }
.toast-list-enter-from   { opacity: 0; transform: translateX(30px); }
.toast-list-leave-to     { opacity: 0; transform: translateX(30px); }
.toast-list-move         { transition: transform 0.3s ease; }
</style>
