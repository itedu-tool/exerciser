<template>
    <div class="row justify-content-center">
        <div class="col-md-6">
            <div class="card">
                <div class="card-header">
                    <h4 class="mb-0">
                        <i class="bi bi-box-arrow-in-right me-2"></i> Вход в систему тестирования
                    </h4>
                </div>
                <div class="card-body">
                    <form @submit.prevent="handleLogin">
                        <div class="mb-3">
                            <label class="form-label">Группа</label>
                            <select class="form-select" v-model="selectedGroupId" required>
                                <option value="">Выберите группу</option>
                                <option v-for="group in groups" :key="group.id" :value="group.id">
                                    {{ group.name }}
                                </option>
                            </select>
                        </div>
                        <div class="mb-3">
                            <label class="form-label">Студент</label>
                            <select class="form-select" v-model="selectedStudentId" :disabled="!selectedGroupId" required>
                                <option value="">Выберите студента</option>
                                <option v-for="student in students" :key="student.id" :value="student.id">
                                    {{ student.fullName }}
                                </option>
                            </select>
                        </div>
                        <button type="submit" class="btn btn-primary w-100" :disabled="!selectedGroupId || !selectedStudentId || loading">
                            <i v-if="loading" class="bi bi-hourglass-split me-1"></i>
                            <i v-else class="bi bi-box-arrow-in-right me-1"></i>
                            {{ loading ? 'Вход...' : 'Войти' }}
                        </button>
                    </form>
                    <div v-if="error" class="alert alert-danger mt-3">{{ error }}</div>
                </div>
            </div>
        </div>
    </div>
</template>

<script setup>
import { ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import api from '../services/api'
import auth from '../services/auth'

const router = useRouter()
const groups = ref([])
const students = ref([])
const selectedGroupId = ref('')
const selectedStudentId = ref('')
const loading = ref(false)
const error = ref('')

async function loadGroups() {
    try {
        const response = await api.getGroups()
        groups.value = response.data
    } catch (err) {
        error.value = 'Ошибка загрузки групп: ' + err.message
    }
}

watch(selectedGroupId, async (groupId) => {
    if (groupId) {
        const group = groups.value.find(g => g.id === groupId)
        students.value = group?.students || []
        selectedStudentId.value = ''
    }
})

async function handleLogin() {
    loading.value = true
    error.value = ''
    try {
        await auth.login(selectedGroupId.value, selectedStudentId.value)
        router.push('/exams')
    } catch (err) {
        error.value = 'Ошибка входа: ' + err.message
    } finally {
        loading.value = false
    }
}

loadGroups()
</script>
