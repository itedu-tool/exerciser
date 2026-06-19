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

            <!-- Блок с баллами и итоговой оценкой -->
            <div class="row">
                <div class="col-md-6">
                    <div class="alert alert-info" role="status">
                        <i class="bi bi-star-fill me-2"></i>
                        <strong>Первичные баллы:</strong> {{ result?.totalScore }} из {{ result?.maxPossibleScore }}
                    </div>
                </div>
                <div class="col-md-6">
                    <!-- Динамический класс alert на основе оценки -->
                    <div class="alert" :class="gradeAlertClass" role="status">
                        <i class="bi bi-check-circle-fill me-2"></i>
                        <strong>Итоговая оценка:</strong> {{ finalGrade }}
                        <button
                            type="button"
                            class="btn btn-sm btn-outline-secondary ms-2"
                            data-bs-toggle="modal"
                            data-bs-target="#formulaModal"
                            aria-label="Подробнее о расчёте оценки"
                        >
                            <i class="bi bi-question-circle"></i>
                        </button>
                    </div>
                </div>
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
                <p><strong>Ваш ответ:</strong> {{ formatAnswer(q.userAnswer) }}</p>
                <p><strong>Правильные ответы:</strong> {{ q.correctAnswers.join(', ') }}</p>
                <p><strong>Баллы:</strong> {{ q.score }} / {{ q.maxScore }}</p>
            </div>
            <button class="btn btn-primary" @click="$router.push('/exams')">
                <i class="bi bi-arrow-left me-1"></i> К списку экзаменов
            </button>
        </div>
    </div>

    <!-- Модальное окно с пояснением формулы -->
    <div class="modal fade" id="formulaModal" tabindex="-1" aria-labelledby="formulaModalLabel" aria-hidden="true">
        <div class="modal-dialog">
            <div class="modal-content">
                <div class="modal-header">
                    <h5 class="modal-title" id="formulaModalLabel">
                        <i class="bi bi-calculator me-2"></i> Как рассчитывается итоговая оценка?
                    </h5>
                    <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Закрыть"></button>
                </div>
                <div class="modal-body">
                    <p><strong>Итоговая оценка</strong> вычисляется по формуле:</p>
                    <div class="p-3 bg-light rounded text-center mb-3">
                        <code class="fs-5">
                            T = 2 + 3 × (S / K)
                        </code>
                    </div>
                    <p>где:</p>
                    <ul>
                        <li><strong>S</strong> — сумма набранных первичных баллов (ваш результат);</li>
                        <li><strong>K</strong> — максимально возможная сумма первичных баллов за весь тест;</li>
                        <li><strong>T</strong> — итоговая оценка по пятибалльной шкале (от 2 до 5).</li>
                    </ul>
                    <p class="text-muted small">
                        Результат округляется <strong>вниз</strong> (например, 4.18 → 4).
                        Минимальная оценка — 2, максимальная — 5.
                    </p>
                    <div class="alert alert-secondary" role="alert">
                        <i class="bi bi-info-circle me-1"></i>
                        <strong>Пример:</strong> если максимальный балл K = 11, а вы набрали S = 8,
                        то T = 2 + 3 × (8/11) ≈ 4.18 → <strong>4</strong>.
                    </div>
                </div>
                <div class="modal-footer">
                    <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Закрыть</button>
                </div>
            </div>
        </div>
    </div>
</template>

<script setup>
import { ref, onMounted, computed } from 'vue'
import { useRoute } from 'vue-router'
import api from '../services/api'

const route = useRoute()
const attemptId = route.params.id
const result = ref(null)
const loading = ref(true)

// Расчёт итоговой оценки по формуле T = 2 + 3 * (S / K), округление вниз
const finalGrade = computed(() => {
    if (!result.value) return '—'
    const total = result.value.totalScore || 0
    const max = result.value.maxPossibleScore || 1
    const grade = 2 + 3 * (total / max)
    return Math.floor(grade)
})

// Определение класса alert в зависимости от оценки
const gradeAlertClass = computed(() => {
    const grade = finalGrade.value
    if (grade === '—') return 'alert-secondary'
    if (grade === 5) return 'alert-success'      // превосходно
    if (grade === 4) return 'alert-info'         // хорошо
    if (grade === 3) return 'alert-warning'      // удовлетворительно
    if (grade <= 2) return 'alert-danger'        // очень плохо
    return 'alert-secondary'
})

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
