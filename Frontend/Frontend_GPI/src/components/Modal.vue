<script setup>
import { X } from 'lucide-vue-next'

const props = defineProps({
  title:       { type: String,  required: true },
  size:        { type: String,  default: 'md' }, // 'sm' | 'md' | 'lg'
  hideFooter:  { type: Boolean, default: false },
  confirmText: { type: String,  default: 'Confirmer' },
  cancelText:  { type: String,  default: 'Annuler' },
  confirmClass:{ type: String,  default: 'btn btn-primary' },
  loading:     { type: Boolean, default: false },
})

const emit = defineEmits(['close', 'confirm'])

const maxWidths = { sm: '400px', md: '520px', lg: '700px', xl: '780px' }

function onBackdropClick(e) {
  if (e.target === e.currentTarget) emit('close')
}
</script>

<template>
  <teleport to="body">
    <div class="modal-backdrop" @click="onBackdropClick">
      <div class="modal-box" :style="{ maxWidth: maxWidths[size] }">
        <!-- Header -->
        <div class="modal-header">
          <h2 class="modal-title">{{ title }}</h2>
          <button class="btn btn-ghost btn-icon" @click="emit('close')">
            <X :size="16" class="text-slate-400" />
          </button>
        </div>

        <!-- Body -->
        <div class="modal-body">
          <slot />
        </div>

        <!-- Footer -->
        <div v-if="!hideFooter" class="modal-footer">
          <button class="btn btn-ghost" @click="emit('close')">{{ cancelText }}</button>
          <button
            :class="confirmClass"
            :disabled="loading"
            @click="emit('confirm')"
          >
            <span v-if="loading" class="spinner" style="width:14px;height:14px;border-width:2px;" />
            {{ confirmText }}
          </button>
        </div>
      </div>
    </div>
  </teleport>
</template>
