<template>
    <div class="question-card card mb-3">
        <div class="card-header">
            <i class="bi bi-pencil me-2 text-warning"></i>
            <strong>Вопрос {{ index + 1 }}</strong>
            <span class="badge bg-secondary ms-2">Ввод текста</span>
        </div>
        <div class="card-body">
            <p class="card-text">{{ question.text }}</p>
            <input type="text" class="form-control" v-model="answer" @input="updateAnswer" />
        </div>
    </div>
</template>

<script setup>
import { ref, watch } from 'vue'

const props = defineProps({
    question: Object,
    index: Number,
    savedAnswer: [String, Array]
})

const emit = defineEmits(['answer'])

const answer = ref(props.savedAnswer || '')

function updateAnswer() {
    emit('answer', { questionId: props.question.id, answer: answer.value })
}

watch(() => props.savedAnswer, (newVal) => {
    if (newVal !== undefined) answer.value = newVal
})
</script>
