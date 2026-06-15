<template>
    <div class="timer" :class="{ 'text-danger': seconds < 60 }">
        ⏱️ {{ formatTime }}
    </div>
</template>

<script setup>
import { ref, computed, onMounted, onUnmounted } from 'vue'

const props = defineProps({
    seconds: {
        type: Number,
        required: true
    }
})

const emit = defineEmits(['timeout'])

const currentSeconds = ref(props.seconds)
let interval = null

const formatTime = computed(() => {
    const mins = Math.floor(currentSeconds.value / 60)
    const secs = currentSeconds.value % 60
    return `${mins.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`
})

function startTimer() {
    interval = setInterval(() => {
        if (currentSeconds.value > 0) {
            currentSeconds.value--
        } else {
            clearInterval(interval)
            emit('timeout')
        }
    }, 1000)
}

onMounted(startTimer)
onUnmounted(() => {
    if (interval) clearInterval(interval)
})
</script>

<style scoped>
.timer {
    font-size: 1.5rem;
    font-weight: bold;
    font-family: monospace;
}
</style>
