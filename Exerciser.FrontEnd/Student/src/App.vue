<template>
    <div id="app">
        <nav class="navbar navbar-dark bg-dark" v-if="isAuthenticated">
            <div class="container">
                <span class="navbar-brand">📚 Exerciser – Студент</span>
                <button class="btn btn-outline-light" @click="logout">Выйти</button>
            </div>
        </nav>
        <main class="container mt-4">
            <router-view />
        </main>
    </div>
</template>

<script setup>
import { computed } from 'vue'
import { useRouter } from 'vue-router'
import auth from './services/auth'

const router = useRouter()
const isAuthenticated = computed(() => auth.isAuthenticated())

function logout() {
    auth.logout()
    router.push('/login')
}
</script>

<style>
@import 'https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/css/bootstrap.min.css';
</style>
