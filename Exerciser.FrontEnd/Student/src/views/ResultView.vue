<template>
    <div v-if="loading" class="text-center" aria-live="polite">
        <div class="spinner-border" role="status">
            <span class="visually-hidden">Загрузка результатов...</span>
        </div>
    </div>
    <div v-else class="card">
        <div class="card-header bg-success text-white">
            <h1 class="h4 mb-0">
                <i class="bi bi-trophy me-2"></i> Результаты тестирования
            </h1>
        </div>
        <div class="card-body">
            <h2 class="h5">{{ result?.examTitle }}</h2>
            <dl class="row">
                <dt class="col-sm-3">Студент</dt>
                <dd class="col-sm-9"><i class="bi bi-person me-1"></i> {{ result?.studentFullName }}</dd>

                <dt class="col-sm-3">Группа</dt>
                <dd class="col-sm-9"><i class="bi bi-people me-1"></i> {{ result?.groupName }}</dd>

                <dt class="col-sm-3">Начато</dt>
                <dd class="col-sm-9"><i class="bi bi-clock-history me-1"></i> <time :datetime="result?.startedAt">{{ new Date(result?.startedAt).toLocaleString() }}</time></dd>

                <dt class="col-sm-3">Завершено</dt>
                <dd class="col-sm-9"><i class="bi bi-clock me-1"></i> <time :datetime="result?.finishedAt">{{ new Date(result?.finishedAt).toLocaleString() }}</time></dd>
            </dl>
            <div class="alert alert-info" role="status">
                <i class="bi bi-star-fill me-2"></i>
                <strong>Итоговый балл:</strong> {{ result?.totalScore }} из {{ result?.maxPossibleScore }}
            </div>
            <hr />
            <h2 class="h5"><i class="bi bi-question-circle me-2"></i>Детали по вопросам</h2>
            <div
                v-for="(q, idx) in result?.questions"
                :key="idx"
                class="mb-3 border rounded p-2 border-start border-4"
                :class="questionBorderClass(q.score, q.maxScore)"
            >
                <p><strong>Вопрос {{ idx + 1 }}:</strong> {{ q.text }}</p>
                <!-- Удалено: <p><strong>Тип:</strong> {{ q.type }}</p> -->
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
