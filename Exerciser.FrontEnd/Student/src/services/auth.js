import api from './api'

export default {
    async login(groupId, studentId) {
        const response = await api.startSession(groupId, studentId)
        const sessionId = response.data.sessionId
        localStorage.setItem('sessionId', sessionId)
        localStorage.setItem('groupId', groupId)
        localStorage.setItem('studentId', studentId)
        return sessionId
    },

    logout() {
        localStorage.removeItem('sessionId')
        localStorage.removeItem('groupId')
        localStorage.removeItem('studentId')
    },

    isAuthenticated() {
        return !!localStorage.getItem('sessionId')
    },

    getSessionId() {
        return localStorage.getItem('sessionId')
    }
}
