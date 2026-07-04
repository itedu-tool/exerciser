import axios from 'axios'

const api = axios.create({
    baseURL: import.meta.env.VITE_API_BASE_URL || '/api/v1',
    headers: {
        'Content-Type': 'application/json'
    }
})

// Интерсептор для добавления Session-ID в заголовки
api.interceptors.request.use(config => {
    const sessionId = localStorage.getItem('sessionId')
    if (sessionId) {
        config.headers['X-Session-Id'] = sessionId
    }
    return config
})

export default {
    // Сессии
    startSession(groupId, studentId) {
        return api.post('/sessions/start', {groupId, studentId})
    },

    // Экзамены (доступные для студента)
    getAvailableExams() {
        return api.get('/exams')
    },

    // Попытки
    startAttempt(examId) {
        return api.post('/attempts/start', {examId})
    },

    finishAttempt(attemptId, answers, finishedAt) {
        return api.post(`/attempts/${attemptId}/finish`, {
            finishedAt,
            answers
        })
    },

    getAttemptResult(attemptId) {
        return api.get(`/attempts/${attemptId}/result`)
    },

    getAttemptExam(attemptId) {
        return api.get(`/attempts/${attemptId}`)
    },

    // Группы и студенты (для входа)
    getGroups() {
        return api.get('/groups')
    }
}
