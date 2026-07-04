/* global showMessage, clearMessage, apiRequest, escapeHtml, getTypeIcon, getTypeLabel */

console.log('[edit.js] Скрипт загружен');

let examId = null;

async function loadExam() {
    console.log('[edit.js] loadExam() начат');
    const urlParams = new URLSearchParams(window.location.search);
    examId = urlParams.get('id');
    console.log('[edit.js] examId из URL:', examId);
    if (!examId) {
        console.error('[edit.js] ID экзамена не указан');
        showMessage(
            document.getElementById('questionsContainer'),
            '❌ ID экзамена не указан',
            'error'
        );
        return;
    }
    try {
        console.log('[edit.js] Вызов apiRequest GET /api/v1/exams/${examId}');
        const exam = await apiRequest(`/api/v1/exams/${examId}`);
        console.log('[edit.js] Получен экзамен:', exam);
        document.getElementById('title').value = exam.title;
        document.getElementById('description').value = exam.description || '';
        document.getElementById('singleChoiceToShow').value = exam.singleChoiceToShow || 0;
        document.getElementById('multipleChoiceToShow').value = exam.multipleChoiceToShow || 0;
        document.getElementById('textInputToShow').value = exam.textInputToShow || 0;
        renderQuestions(exam.questions || []);
    } catch (err) {
        console.error('[edit.js] Ошибка загрузки экзамена:', err);
        showMessage(
            document.getElementById('questionsContainer'),
            `Ошибка загрузки: ${err.message}`,
            'error'
        );
    }
}

function renderQuestions(questions) {
    console.log('[edit.js] renderQuestions(), количество вопросов:', questions.length);
    const container = document.getElementById('questionsContainer');
    container.innerHTML = '';
    if (questions.length === 0) {
        container.innerHTML =
            '<div class="alert alert-info">Нет вопросов. Добавьте первый вопрос.</div>';
        return;
    }
    questions.forEach((q, idx) => {
        const fieldset = document.createElement('fieldset');
        fieldset.className = 'card mb-3';
        fieldset.dataset.index = idx;

        const legend = document.createElement('legend');
        legend.className = 'card-header d-flex justify-content-between align-items-center';
        legend.innerHTML = `
            <strong>Вопрос ${idx + 1}</strong>
            <div>
                <button type="button" class="btn btn-sm btn-secondary move-up-btn" data-index="${idx}" ${idx === 0 ? 'disabled' : ''}>
                    <i class="bi bi-arrow-up"></i> Вверх
                </button>
                <button type="button" class="btn btn-sm btn-secondary move-down-btn" data-index="${idx}" ${idx === questions.length - 1 ? 'disabled' : ''}>
                    <i class="bi bi-arrow-down"></i> Вниз
                </button>
                <button type="button" class="btn btn-sm btn-info copy-question-btn" data-index="${idx}">
                    <i class="bi bi-copy"></i> Копировать
                </button>
                <button type="button" class="btn btn-sm btn-primary preview-exam-btn" data-index="${idx}">
                    <i class="bi bi-eye"></i> Предпросмотр
                </button>
                <button type="button" class="btn btn-sm btn-danger delete-question-btn" data-index="${idx}">
                    <i class="bi bi-trash3"></i> Удалить
                </button>
            </div>
        `;
        fieldset.appendChild(legend);

        const body = document.createElement('div');
        body.className = 'card-body';
        body.innerHTML = `
            <div class="mb-2">
                <label class="form-label" for="qtext_${idx}">Текст вопроса</label>
                <input type="text" id="qtext_${idx}" class="form-control question-text" value="${escapeHtml(q.text)}">
            </div>
            <div class="mb-2">
                <label class="form-label" for="qtype_${idx}">Тип вопроса</label>
                <select id="qtype_${idx}" class="form-select question-type" data-index="${idx}">
                    <option value="SingleChoice" ${q.type === 'SingleChoice' ? 'selected' : ''}>Один вариант</option>
                    <option value="MultipleChoice" ${q.type === 'MultipleChoice' ? 'selected' : ''}>Несколько вариантов</option>
                    <option value="TextInput" ${q.type === 'TextInput' ? 'selected' : ''}>Ввод текста</option>
                </select>
            </div>
            <div class="options-container" data-index="${idx}">
                ${renderOptions(q, idx)}
            </div>
        `;
        fieldset.appendChild(body);
        container.appendChild(fieldset);
    });

    attachEventListeners();
}

function renderOptions(q, qIdx) {
    if (q.type === 'TextInput') {
        const correct = q.correctAnswers && q.correctAnswers[0] ? q.correctAnswers[0] : '';
        return `
            <div class="mb-2">
                <label class="form-label" for="correct_${qIdx}">Правильный ответ (текст)</label>
                <input type="text" id="correct_${qIdx}" class="form-control correct-text-input" value="${escapeHtml(correct)}" data-qidx="${qIdx}">
            </div>
        `;
    }

    const options = q.options || [];
    const correctSet = new Set(q.correctAnswers || []);
    const inputType = q.type === 'SingleChoice' ? 'radio' : 'checkbox';

    let html = `<label class="form-label">Варианты ответов</label><div class="options-list">`;
    options.forEach((opt, optIdx) => {
        const inputId = `opt_${qIdx}_${optIdx}`;
        html += `
            <div class="input-group mb-1">
                <div class="input-group-text">
                    <input class="form-check-input mt-0 correct-checkbox"
                           type="${inputType}"
                           name="correct_${qIdx}"
                           value="${escapeHtml(opt)}"
                           ${correctSet.has(opt) ? 'checked' : ''}
                           data-qidx="${qIdx}" data-opt="${escapeHtml(opt)}"
                           id="${inputId}">
                </div>
                <input type="text" class="form-control option-text" value="${escapeHtml(opt)}" data-qidx="${qIdx}" data-oidx="${optIdx}" aria-label="Вариант ${optIdx + 1}">
                <button class="btn btn-outline-danger remove-option-btn" type="button" data-qidx="${qIdx}" data-oidx="${optIdx}" title="Удалить вариант">
                    <i class="bi bi-trash3"></i>
                </button>
            </div>
        `;
    });
    html += `</div>
        <button type="button" class="btn btn-sm btn-secondary add-option-btn mt-1" data-qidx="${qIdx}">
            <i class="bi bi-plus-circle"></i> Добавить вариант
        </button>`;
    return html;
}

function attachEventListeners() {
    document.querySelectorAll('.move-up-btn').forEach((btn) => {
        btn.addEventListener('click', () => {
            const idx = parseInt(btn.dataset.index);
            console.log('[edit.js] Переместить вопрос вверх, индекс:', idx);
            moveQuestionUp(idx);
        });
    });
    document.querySelectorAll('.move-down-btn').forEach((btn) => {
        btn.addEventListener('click', () => {
            const idx = parseInt(btn.dataset.index);
            console.log('[edit.js] Переместить вопрос вниз, индекс:', idx);
            moveQuestionDown(idx);
        });
    });
    document.querySelectorAll('.copy-question-btn').forEach((btn) => {
        btn.addEventListener('click', () => {
            const idx = parseInt(btn.dataset.index);
            console.log('[edit.js] Копировать вопрос, индекс:', idx);
            copyQuestion(idx);
        });
    });
    document.querySelectorAll('.preview-exam-btn').forEach((btn) => {
        btn.addEventListener('click', () => {
            console.log('[edit.js] Предпросмотр экзамена');
            previewExam();
        });
    });
    document.querySelectorAll('.delete-question-btn').forEach((btn) => {
        btn.addEventListener('click', () => {
            const idx = parseInt(btn.dataset.index);
            console.log('[edit.js] Удалить вопрос, индекс:', idx);
            const questions = getCurrentQuestionsFromDOM();
            questions.splice(idx, 1);
            renderQuestions(questions);
        });
    });
    document.querySelectorAll('.question-type').forEach((select) => {
        select.addEventListener('change', () => {
            const idx = parseInt(select.dataset.index);
            console.log('[edit.js] Изменён тип вопроса, индекс:', idx, 'новый тип:', select.value);
            const questions = getCurrentQuestionsFromDOM();
            const oldQ = questions[idx];
            questions[idx] = {
                ...oldQ,
                type: select.value,
                options: select.value === 'TextInput' ? [] : oldQ.options || ['', ''],
                correctAnswers: [],
            };
            renderQuestions(questions);
        });
    });
    document.querySelectorAll('.remove-option-btn').forEach((btn) => {
        btn.addEventListener('click', (e) => {
            const qIdx = parseInt(btn.dataset.qidx);
            const oIdx = parseInt(btn.dataset.oidx);
            console.log('[edit.js] Удалить вариант, вопрос:', qIdx, 'вариант:', oIdx);
            const questions = getCurrentQuestionsFromDOM();
            if (questions[qIdx] && questions[qIdx].options) {
                questions[qIdx].options.splice(oIdx, 1);
                const correctAnswers = questions[qIdx].correctAnswers;
                const removedOption = questions[qIdx].options[oIdx];
                if (removedOption && correctAnswers.includes(removedOption)) {
                    questions[qIdx].correctAnswers = correctAnswers.filter(
                        (a) => a !== removedOption
                    );
                }
                renderQuestions(questions);
            }
        });
    });
    document.querySelectorAll('.add-option-btn').forEach((btn) => {
        btn.addEventListener('click', (e) => {
            const qIdx = parseInt(btn.dataset.qidx);
            console.log('[edit.js] Добавить вариант, вопрос:', qIdx);
            const questions = getCurrentQuestionsFromDOM();
            if (!questions[qIdx].options) questions[qIdx].options = [];
            questions[qIdx].options.push('');
            renderQuestions(questions);
        });
    });
}

function getCurrentQuestionsFromDOM() {
    console.log('[edit.js] getCurrentQuestionsFromDOM()');
    const questions = [];
    const questionFields = document.querySelectorAll('#questionsContainer fieldset');
    for (let fieldset of questionFields) {
        const text = fieldset.querySelector('.question-text').value.trim();
        const type = fieldset.querySelector('.question-type').value;
        let options = [];
        let correctAnswers = [];

        if (type !== 'TextInput') {
            const optionRows = fieldset.querySelectorAll('.options-list .input-group');
            for (let row of optionRows) {
                const optionTextInput = row.querySelector('.option-text');
                const optionText = optionTextInput ? optionTextInput.value.trim() : '';
                if (optionText) {
                    options.push(optionText);
                    const isCorrect = row.querySelector('.correct-checkbox').checked;
                    if (isCorrect) correctAnswers.push(optionText);
                }
            }
        } else {
            const correctInput = fieldset.querySelector('.correct-text-input');
            if (correctInput && correctInput.value.trim()) {
                correctAnswers = [correctInput.value.trim()];
            }
        }

        questions.push({
            text,
            type,
            options: type === 'TextInput' ? [] : options,
            correctAnswers,
        });
    }
    console.log('[edit.js] Собрано вопросов:', questions.length);
    return questions;
}

function addEmptyQuestion() {
    console.log('[edit.js] Добавить пустой вопрос');
    const questions = getCurrentQuestionsFromDOM();
    questions.push({
        text: '',
        type: 'SingleChoice',
        options: ['', ''],
        correctAnswers: [],
    });
    renderQuestions(questions);
}

function moveQuestionUp(idx) {
    if (idx === 0) return;
    console.log('[edit.js] moveQuestionUp', idx);
    const questions = getCurrentQuestionsFromDOM();
    [questions[idx - 1], questions[idx]] = [questions[idx], questions[idx - 1]];
    renderQuestions(questions);
}

function moveQuestionDown(idx) {
    const questions = getCurrentQuestionsFromDOM();
    if (idx === questions.length - 1) return;
    console.log('[edit.js] moveQuestionDown', idx);
    [questions[idx + 1], questions[idx]] = [questions[idx], questions[idx + 1]];
    renderQuestions(questions);
}

function copyQuestion(idx) {
    console.log('[edit.js] copyQuestion', idx);
    const questions = getCurrentQuestionsFromDOM();
    const original = questions[idx];
    const copy = JSON.parse(JSON.stringify(original));
    copy.text = copy.text + ' (копия)';
    questions.splice(idx + 1, 0, copy);
    renderQuestions(questions);
}

function previewExam() {
    console.log('[edit.js] previewExam()');
    const title = document.getElementById('title').value.trim() || '(без названия)';
    const description = document.getElementById('description').value;
    const singleChoiceToShow = parseInt(document.getElementById('singleChoiceToShow').value, 10) || 0;
    const multipleChoiceToShow = parseInt(document.getElementById('multipleChoiceToShow').value, 10) || 0;
    const textInputToShow = parseInt(document.getElementById('textInputToShow').value, 10) || 0;
    const questions = getCurrentQuestionsFromDOM();

    const previewHtml = `
        <h3>${escapeHtml(title)}</h3>
        <p><strong>Описание:</strong> ${escapeHtml(description || '—')}</p>
        <p><strong>Показывать студенту:</strong> 🔘 ${singleChoiceToShow === 0 ? 'все' : singleChoiceToShow} / ☑️ ${multipleChoiceToShow === 0 ? 'все' : multipleChoiceToShow} / ✏️ ${textInputToShow === 0 ? 'все' : textInputToShow}</p>
        <hr>
        <h4>Вопросы (${questions.length})</h4>
        <div class="accordion" id="previewAccordion">
            ${questions.map((q, idx) => {
        const typeLabel = getTypeLabel(q.type);
        let optionsHtml = '';
        if (q.type !== 'TextInput' && q.options && q.options.length) {
            optionsHtml = `
                        <div class="mt-2"><strong>Варианты ответов:</strong></div>
                        <ul class="list-group mt-1">
                            ${q.options.map((opt) => {
                const isCorrect = q.correctAnswers.includes(opt);
                return `<li class="list-group-item">${isCorrect ? '✅ ' : ''}${escapeHtml(opt)}</li>`;
            }).join('')}
                        </ul>
                    `;
        }
        let correctHtml = '';
        if (q.correctAnswers && q.correctAnswers.length) {
            if (q.type === 'TextInput') {
                correctHtml = `<div class="mt-2 text-success"><strong>✓ Правильный ответ:</strong> ${escapeHtml(q.correctAnswers[0])}</div>`;
            } else {
                correctHtml = `<div class="mt-2 text-success"><strong>✓ Правильные ответы:</strong> ${q.correctAnswers.map((c) => escapeHtml(c)).join(', ')}</div>`;
            }
        }
        return `
                    <div class="accordion-item">
                        <h2 class="accordion-header" id="previewHeading${idx}">
                            <button class="accordion-button ${idx !== 0 ? 'collapsed' : ''}" type="button" data-bs-toggle="collapse" data-bs-target="#previewCollapse${idx}" aria-expanded="${idx === 0 ? 'true' : 'false'}">
                                <strong>Вопрос ${idx + 1}:</strong> ${escapeHtml(q.text)} <span class="badge bg-secondary ms-2">${getTypeIcon(q.type)} ${typeLabel}</span>
                            </button>
                        </h2>
                        <div id="previewCollapse${idx}" class="accordion-collapse collapse ${idx === 0 ? 'show' : ''}" data-bs-parent="#previewAccordion">
                            <div class="accordion-body">
                                ${optionsHtml}
                                ${correctHtml}
                            </div>
                        </div>
                    </div>
                `;
    }).join('')}
        </div>
    `;
    document.getElementById('previewContent').innerHTML = previewHtml;
    const modal = new bootstrap.Modal(document.getElementById('previewModal'));
    modal.show();
}


async function saveExam() {
    console.log('[edit.js] saveExam() начат');
    const title = document.getElementById('title').value.trim();
    const description = document.getElementById('description').value;
    const singleChoiceToShow = parseInt(document.getElementById('singleChoiceToShow').value, 10) || 0;
    const multipleChoiceToShow = parseInt(document.getElementById('multipleChoiceToShow').value, 10) || 0;
    const textInputToShow = parseInt(document.getElementById('textInputToShow').value, 10) || 0;

    if (!title) {
        console.warn('[edit.js] Название экзамена пустое');
        showMessage(
            document.getElementById('questionsContainer'),
            '❌ Название экзамена обязательно',
            'error'
        );
        return;
    }
    const questions = getCurrentQuestionsFromDOM();
    if (questions.length === 0) {
        console.warn('[edit.js] Нет вопросов');
        showMessage(
            document.getElementById('questionsContainer'),
            '❌ Экзамен должен содержать хотя бы один вопрос',
            'error'
        );
        return;
    }

    const singleAvailable = questions.filter(q => q.type === 'SingleChoice').length;
    const multipleAvailable = questions.filter(q => q.type === 'MultipleChoice').length;
    const textAvailable = questions.filter(q => q.type === 'TextInput').length;
    if (singleChoiceToShow > 0 && singleChoiceToShow > singleAvailable) {
        showMessage(
            document.getElementById('questionsContainer'),
            `❌ Количество SingleChoice для показа (${singleChoiceToShow}) превышает доступное (${singleAvailable})`,
            'error'
        );
        return;
    }
    if (multipleChoiceToShow > 0 && multipleChoiceToShow > multipleAvailable) {
        showMessage(
            document.getElementById('questionsContainer'),
            `❌ Количество MultipleChoice для показа (${multipleChoiceToShow}) превышает доступное (${multipleAvailable})`,
            'error'
        );
        return;
    }
    if (textInputToShow > 0 && textInputToShow > textAvailable) {
        showMessage(
            document.getElementById('questionsContainer'),
            `❌ Количество TextInput для показа (${textInputToShow}) превышает доступное (${textAvailable})`,
            'error'
        );
        return;
    }

    for (let i = 0; i < questions.length; i++) {
        const q = questions[i];
        if (!q.text) {
            console.warn('[edit.js] Пустой текст вопроса', i);
            showMessage(
                document.getElementById('questionsContainer'),
                `❌ Вопрос ${i + 1}: текст не может быть пустым`,
                'error'
            );
            return;
        }
        if (q.type !== 'TextInput' && q.options.length < 2) {
            console.warn('[edit.js] Недостаточно вариантов', i, q.options);
            showMessage(
                document.getElementById('questionsContainer'),
                `❌ Вопрос ${i + 1}: для типа ${q.type} нужно минимум 2 варианта`,
                'error'
            );
            return;
        }
        if (q.correctAnswers.length === 0) {
            console.warn('[edit.js] Нет правильных ответов', i);
            showMessage(
                document.getElementById('questionsContainer'),
                `❌ Вопрос ${i + 1}: укажите хотя бы один правильный ответ`,
                'error'
            );
            return;
        }
    }

    try {
        console.log('[edit.js] Вызов apiRequest PUT /api/v1/exams/${examId}');
        await apiRequest(`/api/v1/exams/${examId}`, {
            method: 'PUT',
            headers: {'Content-Type': 'application/json'},
            body: JSON.stringify({
                title,
                description,
                questions,
                singleChoiceToShow,
                multipleChoiceToShow,
                textInputToShow
            }),
        });
        console.log('[edit.js] Экзамен успешно сохранён');
        showMessage(
            document.getElementById('questionsContainer'),
            '✅ Экзамен успешно обновлён',
            'success'
        );
        setTimeout(() => {
            window.location.href = 'teacher.html';
        }, 1500);
    } catch (err) {
        console.error('[edit.js] Ошибка сохранения:', err);
        showMessage(
            document.getElementById('questionsContainer'),
            `❌ Ошибка: ${err.message}`,
            'error'
        );
    }
}

document.getElementById('addQuestionBtn').addEventListener('click', () => {
    console.log('[edit.js] Нажата кнопка "Добавить вопрос"');
    addEmptyQuestion();
});
document.getElementById('examForm').addEventListener('submit', async (e) => {
    e.preventDefault();
    console.log('[edit.js] Отправка формы редактирования');
    await saveExam();
});

loadExam();
