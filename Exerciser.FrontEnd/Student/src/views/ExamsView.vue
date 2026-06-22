<template>
    <div>
        <h1 class="h2"><i class="bi bi-list-ul me-2"></i>Доступные экзамены</h1>
        <div v-if="loading" class="text-center" aria-live="polite">
            <div class="spinner-border" role="status">
                <span class="visually-hidden">Загрузка...</span>
            </div>
        </div>
        <div v-else-if="exams.length === 0" class="alert alert-info" role="status">
            Нет доступных экзаменов.
        </div>
        <ul v-else class="list-group">
            <li v-for="exam in exams" :key="exam.id" class="list-group-item d-flex justify-content-between align-items-start flex-wrap">
                <div class="flex-grow-1">
                    <h2 class="h5 mb-1">{{ exam.title }}</h2>
                    <p class="mb-1 text-muted">{{ exam.description || 'Без описания' }}</p>
                    <div class="small">
                        <div class="mt-1 text-primary">
                            <i class="bi bi-bullseye me-1"></i> Вам будет показано:
                            <span class="ms-1"><i class="bi bi-circle me-1"></i>{{ formatToShow(exam.singleChoiceToShow, exam.singleChoiceCount) }}</span>
                            <span class="ms-2"><i class="bi bi-check2-square me-1"></i>{{ formatToShow(exam.multipleChoiceToShow, exam.multipleChoiceCount) }}</span>
                            <span class="ms-2"><i class="bi bi-pencil me-1"></i>{{ formatToShow(exam.textInputToShow, exam.textInputCount) }}</span>
                        </div>
                    </div>
                </div>
                <button class="btn btn-primary ms-3" @click="startExam(exam.id)" :disabled="startingExam === exam.id">
                    <i v-if="startingExam === exam.id" class="bi bi-hourglass-split me-1"></i>
                    <i v-else class="bi bi-play-circle me-1"></i>
                    {{ startingExam === exam.id ? 'Загрузка...' : 'Начать' }}
                </button>
            </li>
        </ul>
    </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import api from '../services/api'

const router = useRouter()
const exams = ref([])
const loading = ref(true)
const startingExam = ref(null)

function formatToShow(showValue, totalCount) {
    if (showValue === 0 || showValue >= totalCount) {
        return `все (${totalCount})`
    }
    return `${showValue}`
}

async function loadExams() {
    try {
        const response = await api.getAvailableExams()
        exams.value = response.data || []
    } catch (err) {
        console.error('Ошибка загрузки экзаменов:', err)
    } finally {
        loading.value = false
    }
}

async function startExam(examId) {
    startingExam.value = examId
    try {
        const response = await api.startAttempt(examId)
        const { attemptId, exam } = response.data
        sessionStorage.setItem(`attempt_${attemptId}`, JSON.stringify(exam))
        router.push(`/attempt/${attemptId}`)
    } catch (err) {
        alert('Ошибка начала экзамена: ' + err.message)
    } finally {
        startingExam.value = null
    }
}

onMounted(loadExams)
</script>
