<template>
    <div v-if="loading" class="text-center" aria-live="polite">
        <div class="spinner-border" role="status">
            <span class="visually-hidden">Загрузка экзамена...</span>
        </div>
    </div>
    <div v-else-if="error" class="alert alert-danger" role="alert">
        <i class="bi bi-exclamation-triangle-fill me-2"></i> {{ error }}
        <button class="btn btn-primary mt-2" @click="$router.push('/exams')">
            <i class="bi bi-arrow-left me-1"></i> Вернуться к списку
        </button>
    </div>
    <div v-else>
        <div class="sticky-header">
            <div class="d-flex justify-content-between align-items-center">
                <h1 class="h2 mb-0">{{ exam?.title }}</h1>
                <Timer :seconds="timeLeft" @timeout="autoSubmit"/>
            </div>
            <div class="progress mt-2" style="height: 6px;" role="progressbar" aria-valuenow="0" aria-valuemin="0"
                 aria-valuemax="100">
                <div
                    class="progress-bar progress-bar-striped progress-bar-animated"
                    :style="{ width: progressPercent + '%' }"
                    :aria-valuenow="progressPercent"
                ></div>
            </div>
        </div>

        <p class="text-muted mt-3">{{ exam?.description }}</p>

        <form @submit.prevent="submitAttempt" @keydown.enter.prevent>
            <div ref="questionsContainer">
                <component
                    v-for="(q, idx) in exam?.questions"
                    :key="q.id"
                    :is="questionComponent(q.type)"
                    :question="q"
                    :index="idx"
                    :savedAnswer="answers[q.id]"
                    @answer="saveAnswer"
                />
            </div>

            <div class="mt-4 d-flex justify-content-between">
                <button type="button" class="btn btn-secondary" @click="$router.push('/exams')">
                    <i class="bi bi-x-circle me-1"></i> Отмена
                </button>
                <button type="submit" class="btn btn-success" :disabled="submitting">
                    <i v-if="submitting" class="bi bi-hourglass-split me-1"></i>
                    <i v-else class="bi bi-check2-circle me-1"></i>
                    {{ submitting ? 'Отправка...' : 'Завершить тест' }}
                </button>
            </div>
        </form>
    </div>
</template>

<script setup>
import {ref, computed, onMounted} from 'vue'
import {useRoute, useRouter} from 'vue-router'
import api from '../services/api'
import Timer from '../components/Timer.vue'
import QuestionSingleChoice from '../components/QuestionSingleChoice.vue'
import QuestionMultipleChoice from '../components/QuestionMultipleChoice.vue'
import QuestionTextInput from '../components/QuestionTextInput.vue'

const route = useRoute()
const router = useRouter()
const attemptId = route.params.id

const exam = ref(null)
const loading = ref(true)
const error = ref(null)
const answers = ref({})
const submitting = ref(false)
const timeLeft = ref(3600)

const progressPercent = computed(() => {
    if (!exam.value?.questions) return 0
    const total = exam.value.questions.length
    if (total === 0) return 0
    const answered = exam.value.questions.filter(q => {
        const answer = answers.value[q.id]
        if (!answer) return false
        if (Array.isArray(answer)) return answer.length > 0
        return answer !== '' && answer !== null && answer !== undefined
    }).length
    return Math.round((answered / total) * 100)
})

const questionComponent = (type) => {
    if (type === 'SingleChoice') return QuestionSingleChoice
    if (type === 'MultipleChoice') return QuestionMultipleChoice
    return QuestionTextInput
}

function saveAnswer({questionId, answer}) {
    answers.value[questionId] = answer
}

function calculateScoreForQuestion(question, answer) {
    const {type, correctAnswers} = question

    if (!answer || (Array.isArray(answer) && answer.length === 0) || (typeof answer === 'string' && answer.trim() === '')) {
        return 0
    }

    switch (type) {
        case 'SingleChoice': {
            return answer === correctAnswers[0] ? 1 : 0
        }
        case 'MultipleChoice': {
            if (!Array.isArray(answer)) return 0
            const selected = answer.filter(item => item && item.trim() !== '')
            if (selected.length === 0) return 0
            const correctSet = new Set(correctAnswers)
            let correctSelected = 0
            let incorrectSelected = 0
            for (const val of selected) {
                if (correctSet.has(val)) {
                    correctSelected++
                } else {
                    incorrectSelected++
                }
            }
            const score = correctSelected - incorrectSelected
            return Math.max(0, score)
        }
        case 'TextInput': {
            const userAnswer = typeof answer === 'string' ? answer.trim() : ''
            if (userAnswer === '') return 0
            const expected = correctAnswers[0]?.trim() || ''
            return userAnswer === expected ? 3 : 0
        }
        default:
            return 0
    }
}

async function submitAttempt() {
    if (submitting.value) return
    submitting.value = true
    try {
        const answerList = []
        let totalScore = 0
        for (const question of exam.value.questions) {
            const userAnswer = answers.value[question.id] ?? null
            const score = calculateScoreForQuestion(question, userAnswer)
            totalScore += score
            answerList.push({
                questionId: question.id,
                answer: userAnswer,
                score: score
            })
        }
        const finishedAt = new Date().toISOString()
        await api.finishAttempt(attemptId, totalScore, answerList, finishedAt)
        sessionStorage.removeItem(`attempt_${attemptId}`)
        router.push(`/result/${attemptId}`)
    } catch (err) {
        alert('Ошибка при завершении: ' + err.message)
    } finally {
        submitting.value = false
    }
}

async function loadAttempt() {
    loading.value = true
    error.value = null
    const stored = sessionStorage.getItem(`attempt_${attemptId}`)
    if (stored) {
        try {
            exam.value = JSON.parse(stored)
            loading.value = false
            return
        } catch (e) {
            console.warn('Ошибка парсинга sessionStorage', e)
        }
    }
    try {
        const response = await api.getAttemptExam?.(attemptId)
        exam.value = response?.data
        sessionStorage.setItem(`attempt_${attemptId}`, JSON.stringify(exam.value))
    } catch (err) {
        if (err.response?.status === 404) {
            router.push(`/result/${attemptId}`)
        } else {
            error.value = 'Не удалось загрузить экзамен. Возможно, вы не начинали эту попытку или она уже завершена.'
            console.error(err)
        }
    } finally {
        loading.value = false
    }
}

function autoSubmit() {
    if (!submitting.value && exam.value) {
        if (confirm('Время вышло! Завершить тест?')) {
            submitAttempt()
        }
    }
}

onMounted(loadAttempt)
</script>

<style scoped>
.sticky-header {
    position: sticky;
    top: 0;
    z-index: 100;
    background: white;
    padding: 12px 0 8px 0;
    box-shadow: 0 2px 8px rgba(0, 0, 0, 0.08);
    margin-bottom: 16px;
}

@media (prefers-color-scheme: dark) {
    .sticky-header {
        background: #212529;
    }
}
</style>
