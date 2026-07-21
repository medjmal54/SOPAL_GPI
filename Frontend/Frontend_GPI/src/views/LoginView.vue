<script setup>
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuth } from '../services/auth'
import { useToast } from '../composables/useToast'
import { Cpu, LogIn } from 'lucide-vue-next'

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
  <div class="min-h-screen flex items-center justify-center dot-grid" style="background:#080c1a;">
    <div class="glass p-8 w-full" style="max-width:500px;">
      <!-- Brand -->
      <div class="flex items-center gap-3 mb-8 justify-center">
        <div
          class="flex items-center justify-center rounded-xl"
          style="width:46px;height:46px;background:rgba(99,102,241,0.15);border:1px solid rgba(99,102,241,0.3);box-shadow:0 0 20px rgba(99,102,241,0.2);"
        >
          <Cpu :size="24" class="text-indigo-400" />
        </div>
        <div>
          <div class="font-bold text-lg tracking-widest text-slate-100 uppercase">SOPAL GPI</div>
          <div class="text-[10px] text-indigo-400/70 font-mono tracking-[0.2em] uppercase">Asset Manager</div>
        </div>
      </div>

      <!-- Login Form -->
      <h2 class="text-center text-sm text-slate-400 mb-6">Connectez-vous à votre compte</h2>
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
          class="btn btn-primary w-full justify-center"
          :disabled="loading"
        >
          <span v-if="loading" class="spinner" style="width:14px;height:14px;border-width:2px;" />
          <LogIn v-else :size="15" />
          Se connecter
        </button>
      </form>
    </div>
  </div>
</template>
