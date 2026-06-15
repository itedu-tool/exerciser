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

router.beforeEach((to, from, next) => {
    const isAuthenticated = auth.isAuthenticated()
    if (to.meta.requiresAuth && !isAuthenticated) {
        next('/login')
    } else if (to.meta.requiresGuest && isAuthenticated) {
        next('/exams')
    } else {
        next()
    }
})

export default router
