<script setup>
import { computed, onBeforeMount } from 'vue'
import { useRoute } from 'vue-router'
import TopNavBar from './Layout/TopNavBar.vue'
import ToastNotification from './components/ToastNotification.vue'

const route = useRoute()
const isLoginPage = computed(() => route.path === '/login')

onBeforeMount(() => {
  const savedTheme = localStorage.getItem('sopal-theme')
  document.documentElement.dataset.theme = savedTheme === 'light' ? 'light' : 'dark'
})
</script>

<template>
  <div class="min-h-screen bg-surface">
    <TopNavBar />
    <main :class="isLoginPage ? '' : 'pt-16'">
      <div :class="isLoginPage ? '' : 'app-content p-container-padding'">
        <router-view v-slot="{ Component }">
          <transition name="page" mode="out-in">
            <div :key="route.fullPath" class="route-view">
              <component :is="Component" />
            </div>
          </transition>
        </router-view>
      </div>
    </main>
  </div>
  <ToastNotification />
</template>
