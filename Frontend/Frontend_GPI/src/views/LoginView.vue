<script setup>
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuth } from '../services/auth'
import { useToast } from '../composables/useToast'

const router = useRouter()
const { login, isAdmin } = useAuth()
const { success, error: toastError } = useToast()

const email = ref('')
const matricule = ref('')
const loading = ref(false)

async function handleLogin() {
  if (!email.value || !matricule.value) {
    toastError('Veuillez remplir tous les champs.')
    return
  }
  loading.value = true
  try {
    await login(email.value, Number(matricule.value))
    success('Connexion réussie.')
    router.push(isAdmin() ? '/dashboard' : '/mes-materiels')
  } catch (e) {
    toastError(e.message || 'Identifiants incorrects.')
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <div class="min-h-screen flex items-center justify-center bg-background" style="background:#0b0e15;">
    <div class="bg-surface-container border border-outline-variant rounded-xl p-8 w-full" style="max-width:440px;">
      <div class="flex items-center gap-3 mb-8 justify-center">
        <div class="flex items-center justify-center rounded-lg bg-primary-container/20 p-3 border border-primary/30">
          <span class="material-symbols-outlined text-primary text-[28px]">settings_input_component</span>
        </div>
        <div>
          <div class="font-display-lg text-display-lg font-bold text-primary" style="font-size:22px;">SOPAL GPI</div>
          <div class="font-label-sm text-label-sm text-on-surface-variant uppercase tracking-wider">Asset Manager</div>
        </div>
      </div>

      <h2 class="text-center font-body-md text-body-md text-on-surface-variant mb-6">Connectez-vous à votre compte</h2>
      <form @submit.prevent="handleLogin" class="space-y-4">
        <div class="form-group">
          <label class="form-label">Email</label>
          <input
            v-model="email"
            type="email"
            class="form-input"
            placeholder="ex: k.benali@sopal.dz"
            autocomplete="email"
          />
        </div>
        <div class="form-group">
          <label class="form-label">Matricule</label>
          <input
            v-model="matricule"
            type="number"
            class="form-input"
            placeholder="ex: 25001"
            autocomplete="off"
          />
        </div>
        <button
          type="submit"
          class="btn btn-primary w-full justify-center py-3"
          :disabled="loading"
        >
          <span v-if="loading" class="spinner" style="width:14px;height:14px;border-width:2px;" />
          <span v-else class="material-symbols-outlined text-[18px]">login</span>
          Se connecter
        </button>
      </form>
    </div>
  </div>
</template>
