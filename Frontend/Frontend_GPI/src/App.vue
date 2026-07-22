<script setup>
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import Sidebar from './Layout/Sidebar.vue'
import ToastNotification from './components/ToastNotification.vue'

const route = useRoute()
const isLoginPage = computed(() => route.path === '/login')
</script>

<template>
  <Sidebar v-if="!isLoginPage" />
  <router-view v-slot="{ Component }">
    <transition name="page" mode="out-in">
      <component :is="Component" />
    </transition>
  </router-view>
  <ToastNotification />
</template>
