<template>
    <fieldset class="question-card card mb-3">
        <legend class="card-header">
            <i class="bi bi-pencil me-2 text-warning"></i>
            <strong>Вопрос {{ index + 1 }}</strong>
            <span class="badge bg-secondary ms-2">Ввод текста</span>
        </legend>
        <div class="card-body">
            <p class="card-text">{{ question.text }}</p>
            <label :for="`q${question.id}_input`" class="visually-hidden">Ваш ответ</label>
            <input type="text" class="form-control" :id="`q${question.id}_input`" v-model="answer"
                   @input="updateAnswer"/>
        </div>
    </fieldset>
</template>

<script setup>
import {ref, watch} from 'vue'

const props = defineProps({
    question: Object,
    index: Number,
    savedAnswer: [String, Array]
})
const emit = defineEmits(['answer'])
const answer = ref(props.savedAnswer || '')

function updateAnswer() {
    emit('answer', {questionId: props.question.id, answer: answer.value})
}

watch(() => props.savedAnswer, (newVal) => {
    if (newVal !== undefined) answer.value = newVal
})
</script>
