<script setup>
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import Sidebar from './Layout/Sidebar.vue'
import ToastNotification from './components/ToastNotification.vue'

const route = useRoute()
const isLoginPage = computed(() => route.path === '/login')
</script>

<template>
  <template v-if="isLoginPage">
    <router-view />
  </template>
  <template v-else>
    <Sidebar />
  </template>
  <ToastNotification />
</template>
