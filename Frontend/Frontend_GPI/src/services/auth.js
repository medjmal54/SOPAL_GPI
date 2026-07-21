import { reactive } from 'vue'
import { apiPost } from './api'

const state = reactive({
  matricule: Number(localStorage.getItem('matricule')) || null,
  nom: localStorage.getItem('nom') || '',
  prenom: localStorage.getItem('prenom') || '',
  role: localStorage.getItem('role') || '',
})

export function useAuth() {
  async function login(email, matricule) {
    const data = await apiPost('/Auth/login', { email, matricule })
    state.matricule = data.matricule
    state.nom = data.nom
    state.prenom = data.prenom
    state.role = data.role

    localStorage.setItem('matricule', data.matricule)
    localStorage.setItem('nom', data.nom)
    localStorage.setItem('prenom', data.prenom)
    localStorage.setItem('role', data.role)
    return data
  }

  async function logout() {
    try { await apiPost('/Auth/logout') } catch {}
    state.matricule = null
    state.nom = ''
    state.prenom = ''
    state.role = ''
    localStorage.removeItem('matricule')
    localStorage.removeItem('nom')
    localStorage.removeItem('prenom')
    localStorage.removeItem('role')
  }

  function isAuthenticated() {
    return !!state.matricule
  }

  function isAdmin() {
    return state.role === 'Admin'
  }

  return { state, login, logout, isAuthenticated, isAdmin }
}
