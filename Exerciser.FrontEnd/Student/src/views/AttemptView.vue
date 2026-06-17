<template>
    <div v-if="loading" class="text-center">
        <div class="spinner-border" role="status"></div>
    </div>
    <div v-else-if="error" class="alert alert-danger">
        {{ error }}
        <button class="btn btn-primary mt-2" @click="$router.push('/exams')">Вернуться к списку</button>
    </div>
    <div v-else>
        <!-- Заголовок и таймер -->
        <div class="d-flex justify-content-between align-items-center mb-3">
            <h2>{{ exam?.title }}</h2>
            <Timer :seconds="timeLeft" @timeout="autoSubmit" />
        </div>
        <p class="text-muted">{{ exam?.description }}</p>

        <!-- Панель прогресса -->
        <div class="progress-panel card mb-3">
            <div class="card-body">
                <div class="d-flex justify-content-between align-items-center">
          <span>
            <strong>Прогресс:</strong> {{ answeredCount }} из {{ exam?.questions?.length || 0 }} вопросов отвечено
          </span>
                    <span class="text-primary">{{ Math.round((answeredCount / (exam?.questions?.length || 1)) * 100) }}%</span>
                </div>
                <div class="progress mt-2" style="height: 8px">
                    <div class="progress-bar bg-success" role="progressbar" :style="{ width: progressPercent + '%' }"></div>
                </div>
            </div>
        </div>

        <!-- Навигация по вопросам (сетка) -->
        <div class="question-nav mb-4">
            <div class="d-flex flex-wrap gap-2">
                <button
                    v-for="(q, idx) in exam?.questions"
                    :key="q.id"
                    type="button"
                    class="btn btn-sm"
                    :class="{
            'btn-success': isAnswered(q.id),
            'btn-outline-secondary': !isAnswered(q.id),
            'btn-primary': currentQuestionIndex === idx
          }"
                    @click="scrollToQuestion(idx)"
                >
                    {{ idx + 1 }}
                </button>
            </div>
        </div>

        <!-- Форма с вопросами -->
        <form @submit.prevent="submitAttempt" @keydown.enter.prevent>
            <div ref="questionsContainer">
                <component
                    v-for="(q, idx) in exam?.questions"
                    :key="q.id"
                    :is="questionComponent(q.type)"
                    :ref="el => setQuestionRef(el, idx)"
                    :question="q"
                    :index="idx"
                    :savedAnswer="answers[q.id]"
                    @answer="saveAnswer"
                />
            </div>
            <div class="mt-4 d-flex justify-content-between">
                <button type="button" class="btn btn-secondary" @click="$router.push('/exams')">Отмена</button>
                <button type="submit" class="btn btn-success" :disabled="submitting">
                    {{ submitting ? 'Отправка...' : 'Завершить тест' }}
                </button>
            </div>
        </form>
    </div>
</template>

<script setup>
import { ref, computed, onMounted, nextTick } from 'vue'
import { useRoute, useRouter } from 'vue-router'
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
const timeLeft = ref(3600) // 1 час в секундах

const questionsContainer = ref(null)
const questionRefs = ref([])

// Количество отвеченных вопросов
const answeredCount = computed(() => {
    if (!exam.value?.questions) return 0
    return exam.value.questions.filter(q => {
        const answer = answers.value[q.id]
        if (!answer) return false
        if (Array.isArray(answer)) return answer.length > 0
        return answer !== '' && answer !== null && answer !== undefined
    }).length
})

// Процент для прогресс-бара
const progressPercent = computed(() => {
    const total = exam.value?.questions?.length || 1
    return Math.round((answeredCount.value / total) * 100)
})

// Текущий индекс активного вопроса (для подсветки в навигации)
let currentQuestionIndex = ref(0)

const questionComponent = (type) => {
    if (type === 'SingleChoice') return QuestionSingleChoice
    if (type === 'MultipleChoice') return QuestionMultipleChoice
    return QuestionTextInput
}

function setQuestionRef(el, idx) {
    if (el) {
        questionRefs.value[idx] = el
    }
}

function isAnswered(questionId) {
    const answer = answers.value[questionId]
    if (!answer) return false
    if (Array.isArray(answer)) return answer.length > 0
    return answer !== '' && answer !== null && answer !== undefined
}

function saveAnswer({ questionId, answer }) {
    answers.value[questionId] = answer
}

/**
 * Расчёт балла за один вопрос по правилам.
 * @param {Object} question - объект вопроса (с полями type, correctAnswers)
 * @param {string|string[]|null} answer - ответ студента
 * @returns {number} балл за вопрос (0 или положительное число)
 */
function calculateScoreForQuestion(question, answer) {
    const { type, correctAnswers } = question

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
    submitting.value = true
    try {
        // Формируем массив ответов с баллами
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

        // Удаляем сохранённый экзамен из sessionStorage
        sessionStorage.removeItem(`attempt_${attemptId}`)
        router.push(`/result/${attemptId}`)
    } catch (err) {
        alert('Ошибка при завершении: ' + err.message)
    } finally {
        submitting.value = false
    }
}

function scrollToQuestion(idx) {
    currentQuestionIndex.value = idx
    const element = questionRefs.value[idx]?.$el || questionRefs.value[idx]
    if (element) {
        element.scrollIntoView({ behavior: 'smooth', block: 'center' })
        element.classList.add('highlight')
        setTimeout(() => element.classList.remove('highlight'), 1500)
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

// Intersection Observer для отслеживания видимого вопроса
const observer = new IntersectionObserver((entries) => {
    entries.forEach(entry => {
        if (entry.isIntersecting) {
            const idx = questionRefs.value.findIndex(ref => ref?.$el === entry.target || ref === entry.target)
            if (idx !== -1) currentQuestionIndex.value = idx
        }
    })
}, { threshold: 0.5 })

onMounted(() => {
    loadAttempt()
    nextTick(() => {
        if (questionsContainer.value) {
            const elements = questionRefs.value.map(ref => ref?.$el || ref).filter(Boolean)
            elements.forEach(el => observer.observe(el))
        }
    })
})
</script>

<style scoped>
.question-nav {
    background: #f8f9fa;
    padding: 0.75rem;
    border-radius: 0.5rem;
    position: sticky;
    top: 10px;
    z-index: 100;
}

.progress-panel {
    background: #f8f9fa;
}

.highlight {
    transition: background-color 0.3s;
    background-color: #fff3cd !important;
    border-left: 4px solid #ffc107;
}
</style>
