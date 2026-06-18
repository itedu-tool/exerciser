<template>
    <div v-if="loading" class="text-center">
        <div class="spinner-border" role="status"></div>
    </div>
    <div v-else class="card">
        <div class="card-header bg-success text-white">
            <h4 class="mb-0">
                <i class="bi bi-trophy me-2"></i> Результаты тестирования
            </h4>
        </div>
        <div class="card-body">
            <h5>{{ result?.examTitle }}</h5>
            <p><i class="bi bi-person me-1"></i> <strong>Студент:</strong> {{ result?.studentFullName }}</p>
            <p><i class="bi bi-people me-1"></i> <strong>Группа:</strong> {{ result?.groupName }}</p>
            <p><i class="bi bi-clock-history me-1"></i> <strong>Начато:</strong> {{ new Date(result?.startedAt).toLocaleString() }}</p>
            <p><i class="bi bi-clock me-1"></i> <strong>Завершено:</strong> {{ new Date(result?.finishedAt).toLocaleString() }}</p>
            <div class="alert alert-info">
                <i class="bi bi-star-fill me-2"></i>
                <strong>Итоговый балл:</strong> {{ result?.totalScore }} из {{ result?.maxPossibleScore }}
            </div>
            <hr />
            <h5><i class="bi bi-question-circle me-2"></i>Детали по вопросам</h5>
            <div
                v-for="(q, idx) in result?.questions"
                :key="idx"
                class="mb-3 border rounded p-2 border-start border-4"
                :class="questionBorderClass(q.score, q.maxScore)"
            >
                <p><strong>Вопрос {{ idx + 1 }}:</strong> {{ q.text }}</p>
                <p><strong>Тип:</strong> {{ q.type }}</p>
                <p><strong>Ваш ответ:</strong> {{ formatAnswer(q.userAnswer) }}</p>
                <p><strong>Правильные ответы:</strong> {{ q.correctAnswers.join(', ') }}</p>
                <p><strong>Баллы:</strong> {{ q.score }} / {{ q.maxScore }}</p>
            </div>
            <button class="btn btn-primary" @click="$router.push('/exams')">
                <i class="bi bi-arrow-left me-1"></i> К списку экзаменов
            </button>
        </div>
    </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import api from '../services/api'

const route = useRoute()
const attemptId = route.params.id
const result = ref(null)
const loading = ref(true)

function formatAnswer(answer) {
    if (Array.isArray(answer)) return answer.join(', ') || '(не выбрано)'
    return answer || '(не введено)'
}

function questionBorderClass(score, maxScore) {
    if (score === maxScore) return 'border-success'
    if (score === 0) return 'border-danger'
    return 'border-warning'
}

async function loadResult() {
    try {
        const response = await api.getAttemptResult(attemptId)
        result.value = response.data
    } catch (err) {
        console.error(err)
    } finally {
        loading.value = false
    }
}

onMounted(loadResult)
</script>
