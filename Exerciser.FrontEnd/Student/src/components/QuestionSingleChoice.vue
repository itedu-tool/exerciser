<template>
    <fieldset class="question-card card mb-3">
        <legend class="card-header">
            <i class="bi bi-circle me-2 text-primary"></i>
            <strong>Вопрос {{ index + 1 }}</strong>
            <span class="badge bg-secondary ms-2">Один вариант</span>
        </legend>
        <div class="card-body">
            <p class="card-text">{{ question.text }}</p>
            <div v-for="option in question.options" :key="option" class="form-check">
                <input
                    class="form-check-input"
                    type="radio"
                    :name="`q${question.id}`"
                    :value="option"
                    v-model="selected"
                    @change="updateAnswer"
                    :id="`q${question.id}_${option}`"
                />
                <label class="form-check-label" :for="`q${question.id}_${option}`">
                    {{ option }}
                </label>
            </div>
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
const selected = ref(props.savedAnswer || null)
function updateAnswer() {
    emit('answer', { questionId: props.question.id, answer: selected.value })
}
watch(() => props.savedAnswer, (newVal) => {
    if (newVal !== undefined) selected.value = newVal
})
</script>
