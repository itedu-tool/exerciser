<template>
    <div id="app">
        <nav class="navbar navbar-expand-lg bg-dark" data-bs-theme="dark">
            <div class="container">
                <a class="navbar-brand" href="#">
                    <i class="bi bi-book me-2"></i> Exerciser – Студент
                </a>
                <button class="navbar-toggler" type="button" data-bs-toggle="collapse" data-bs-target="#navbarNav">
                    <span class="navbar-toggler-icon"></span>
                </button>
                <div class="collapse navbar-collapse" id="navbarNav">
                    <ul class="navbar-nav ms-auto">
                        <li v-if="isAuthenticated" class="nav-item">
                            <a class="nav-link" href="#" @click.prevent="logout">
                                <i class="bi bi-box-arrow-right me-1"></i> Выйти
                            </a>
                        </li>
                        <ThemeSelector @theme-changed="applyTheme" />
                    </ul>
                </div>
            </div>
        </nav>
        <main class="container mt-4">
            <router-view />
        </main>
    </div>
</template>

<script setup>
import { computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import auth from './services/auth'
import ThemeSelector from './components/ThemeSelector.vue'
import { THEME_STORAGE_KEY, AVAILABLE_THEMES } from './config/themes'

const router = useRouter()
const isAuthenticated = computed(() => auth.isAuthenticated())

function logout() {
    auth.logout()
    router.push('/login')
}

function applyTheme(themeUrl) {
    let link = document.getElementById('theme-stylesheet')
    if (!link) {
        link = document.createElement('link')
        link.id = 'theme-stylesheet'
        link.rel = 'stylesheet'
        document.head.appendChild(link)
    }
    link.href = themeUrl
}

onMounted(() => {
    const savedThemeId = localStorage.getItem(THEME_STORAGE_KEY)
    if (savedThemeId) {
        const theme = AVAILABLE_THEMES.find(t => t.id === savedThemeId)
        if (theme) {
            applyTheme(theme.url)
            return
        }
    }
    const defaultTheme = AVAILABLE_THEMES.find(t => t.id === 'flatly') || AVAILABLE_THEMES[0]
    applyTheme(defaultTheme.url)
})
</script>

<style>
/* CSS загружается через main.js */
</style>
