<template>
    <fieldset class="question-card card mb-3">
        <legend class="card-header">
            <strong>Вопрос {{ index + 1 }}</strong>
        </legend>
        <div class="card-body">
            <p class="card-text">{{ question.text }}</p>
            <label :for="`q${question.id}_input`" class="visually-hidden">Ваш ответ</label>
            <input type="text" class="form-control" :id="`q${question.id}_input`" v-model="answer" @input="updateAnswer" />
        </div>
    </fieldset>
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
