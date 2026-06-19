<template>
    <li class="nav-item dropdown">
        <a class="nav-link dropdown-toggle" href="#" role="button" data-bs-toggle="dropdown" aria-expanded="false">
            <i class="bi bi-palette me-1"></i> Тема
        </a>
        <ul class="dropdown-menu dropdown-menu-end">
            <li v-for="theme in themes" :key="theme.id">
                <a class="dropdown-item" href="#" @click.prevent="selectTheme(theme)">
                    {{ theme.name }}
                    <span v-if="currentThemeId === theme.id" class="float-end">✓</span>
                </a>
            </li>
        </ul>
    </li>
</template>

<script setup>
import {ref, onMounted} from 'vue'
import {AVAILABLE_THEMES, THEME_STORAGE_KEY} from '../config/themes'

const themes = AVAILABLE_THEMES
const currentThemeId = ref(null)

const emit = defineEmits(['theme-changed'])

function selectTheme(theme) {
    currentThemeId.value = theme.id
    localStorage.setItem(THEME_STORAGE_KEY, theme.id)
    emit('theme-changed', theme.url)
}

function loadSavedTheme() {
    const saved = localStorage.getItem(THEME_STORAGE_KEY)
    if (saved) {
        const found = themes.find(t => t.id === saved)
        if (found) {
            currentThemeId.value = found.id
            return found.url
        }
    }
    const defaultTheme = themes.find(t => t.id === 'flatly') || themes[0]
    currentThemeId.value = defaultTheme.id
    return defaultTheme.url
}

onMounted(() => {
    const url = loadSavedTheme()
    emit('theme-changed', url)
})
</script>
