<template>
    <div>
        <h2>Доступные экзамены</h2>
        <div v-if="loading" class="text-center">
            <div class="spinner-border" role="status"></div>
        </div>
        <div v-else-if="exams.length === 0" class="alert alert-info">
            Нет доступных экзаменов.
        </div>
        <div v-else class="list-group">
            <div v-for="exam in exams" :key="exam.id" class="list-group-item">
                <div class="d-flex justify-content-between align-items-start">
                    <div class="flex-grow-1">
                        <h5 class="mb-1">{{ exam.title }}</h5>
                        <p class="mb-1 text-muted">{{ exam.description || 'Без описания' }}</p>
                        <div class="small">
                            <div>📚 Всего вопросов: {{ exam.questionsCount }}
                                <span class="ms-2">🔘 {{ exam.singleChoiceCount }}</span>
                                <span class="ms-2">☑️ {{ exam.multipleChoiceCount }}</span>
                                <span class="ms-2">✏️ {{ exam.textInputCount }}</span>
                            </div>
                            <div class="mt-1 text-primary">
                                🎯 Вам будет показано:
                                <span class="ms-1">🔘 {{ formatToShow(exam.singleChoiceToShow, exam.singleChoiceCount) }}</span>
                                <span class="ms-2">☑️ {{ formatToShow(exam.multipleChoiceToShow, exam.multipleChoiceCount) }}</span>
                                <span class="ms-2">✏️ {{ formatToShow(exam.textInputToShow, exam.textInputCount) }}</span>
                            </div>
                        </div>
                    </div>
                    <button class="btn btn-primary ms-3" @click="startExam(exam.id)" :disabled="startingExam === exam.id">
                        {{ startingExam === exam.id ? 'Загрузка...' : 'Начать' }}
                    </button>
                </div>
            </div>
        </div>
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
    return `${showValue} из ${totalCount}`
}

async function loadExams() {
    try {
        const response = await api.getAvailableExams()
        exams.value = response.data
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
