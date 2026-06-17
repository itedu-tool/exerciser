import { createRouter, createWebHistory } from 'vue-router'
import LoginView from '../views/LoginView.vue'
import ExamsView from '../views/ExamsView.vue'
import AttemptView from '../views/AttemptView.vue'
import ResultView from '../views/ResultView.vue'
import auth from '../services/auth'

const routes = [
    {
        path: '/',
        redirect: '/login'
    },
    {
        path: '/login',
        name: 'Login',
        component: LoginView,
        meta: { requiresGuest: true }
    },
    {
        path: '/exams',
        name: 'Exams',
        component: ExamsView,
        meta: { requiresAuth: true }
    },
    {
        path: '/attempt/:id',
        name: 'Attempt',
        component: AttemptView,
        props: true,
        meta: { requiresAuth: true }
    },
    {
        path: '/result/:id',
        name: 'Result',
        component: ResultView,
        props: true,
        meta: { requiresAuth: true }
    }
]

const router = createRouter({
    history: createWebHistory(),
    routes
})

// Исправленный navigation guard – без вызова next(), возвращаем значение напрямую
router.beforeEach((to, from) => {
    const isAuthenticated = auth.isAuthenticated()
    if (to.meta.requiresAuth && !isAuthenticated) {
        return '/login'
    }
    if (to.meta.requiresGuest && isAuthenticated) {
        return '/exams'
    }
    return true
})

export default router
