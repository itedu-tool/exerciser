<template>
    <div class="question-card card mb-3">
        <div class="card-header">
            <strong>Вопрос {{ index + 1 }}</strong>
            <span class="badge bg-secondary ms-2">☑️ Несколько вариантов</span>
        </div>
        <div class="card-body">
            <p class="card-text">{{ question.text }}</p>
            <div v-for="option in question.options" :key="option" class="form-check">
                <input
                    class="form-check-input"
                    type="checkbox"
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

const selected = ref(Array.isArray(props.savedAnswer) ? [...props.savedAnswer] : [])

function updateAnswer() {
    emit('answer', { questionId: props.question.id, answer: selected.value })
}

watch(() => props.savedAnswer, (newVal) => {
    if (Array.isArray(newVal)) selected.value = [...newVal]
}, { deep: true })
</script>
