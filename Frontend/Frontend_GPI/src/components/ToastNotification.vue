<script setup>
import { useToast } from '../composables/useToast'

const { toasts, dismiss } = useToast()

const iconMap = { success: 'check_circle', error: 'error', info: 'info' }
</script>

<template>
  <teleport to="body">
    <div class="toast-container">
      <transition-group name="toast-list" tag="div" class="flex flex-col gap-2.5">
        <div v-for="t in toasts" :key="t.id" :class="`toast toast-${t.type}`">
          <span class="material-symbols-outlined text-[18px] flex-shrink-0" :class="t.type === 'success' ? 'text-tertiary' : t.type === 'error' ? 'text-secondary' : 'text-primary'">{{ iconMap[t.type] }}</span>
          <span class="flex-1 font-body-sm text-body-sm text-on-surface leading-snug">{{ t.message }}</span>
          <button class="btn btn-ghost btn-icon flex-shrink-0 p-1" @click="dismiss(t.id)">
            <span class="material-symbols-outlined text-[14px] text-on-surface-variant">close</span>
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
