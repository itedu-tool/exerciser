/* global showMessage, clearMessage, showLoading, showEmpty, apiRequest, escapeHtml */

console.log('[teacher.js] Скрипт загружен');

const API_BASE = window.API_BASE;
const IMPORT_URL = '/api/v1/exams/import';
const EXAMS_URL = '/api/v1/exams';

const fileInput = document.getElementById('examFile');
const uploadBtn = document.getElementById('uploadBtn');
const importResultDiv = document.getElementById('importResult');
const examsListDiv = document.getElementById('examsList');
const modalEl = document.getElementById('examDetailsModal');
const modalContent = document.getElementById('examDetailsContent');
let modal = null;

if (modalEl) {
    modal = new bootstrap.Modal(modalEl);
}

async function importExam(file) {
    console.log('[teacher.js] importExam() начат, файл:', file.name, file.size);
    const formData = new FormData();
    formData.append('file', file);
    try {
        console.log('[teacher.js] Вызов apiRequest POST', IMPORT_URL);
        const data = await apiRequest(IMPORT_URL, { method: 'POST', body: formData });
        console.log('[teacher.js] Импорт успешен, получены данные:', data);
        showMessage(
            importResultDiv,
            `✅ Экзамен "${data.title}" импортирован!\nID: ${data.id}\nВопросов: ${data.questionsCount}`,
            'success'
        );
        await loadExamsList();
    } catch (err) {
        console.error('[teacher.js] Ошибка импорта:', err);
        showMessage(importResultDiv, `❌ Ошибка импорта: ${err.message}`, 'error');
    }
}

uploadBtn.addEventListener('click', async () => {
    console.log('[teacher.js] Нажата кнопка "Загрузить"');
    if (!fileInput.files || fileInput.files.length === 0) {
        console.warn('[teacher.js] Файл не выбран');
        showMessage(importResultDiv, '❌ Выберите JSON-файл', 'error');
        return;
    }
    const file = fileInput.files[0];
    console.log('[teacher.js] Выбран файл:', file.name, file.size);
    if (!file.name.toLowerCase().endsWith('.json')) {
        console.warn('[teacher.js] Файл не JSON');
        showMessage(importResultDiv, '❌ Файл должен быть в формате JSON', 'error');
        return;
    }
    if (file.size > 10 * 1024 * 1024) {
        console.warn('[teacher.js] Файл превышает 10 MB');
        showMessage(importResultDiv, '❌ Файл превышает 10 MB', 'error');
        return;
    }
    clearMessage(importResultDiv);
    await importExam(file);
});

async function loadExamsList() {
    console.log('[teacher.js] loadExamsList() начат');
    showLoading(examsListDiv);
    try {
        console.log('[teacher.js] Вызов apiRequest GET', EXAMS_URL);
        const exams = await apiRequest(EXAMS_URL);
        console.log('[teacher.js] Получены экзамены:', exams);
        if (!Array.isArray(exams) || exams.length === 0) {
            console.log('[teacher.js] Экзаменов нет, показываем пустое состояние');
            showEmpty(
                examsListDiv,
                'Нет доступных экзаменов. Загрузите первый экзамен через импорт.'
            );
            return;
        }
        renderExamsList(exams);
    } catch (err) {
        console.error('[teacher.js] Ошибка загрузки списка экзаменов:', err);
        examsListDiv.innerHTML = `<div class="alert alert-danger">❌ Ошибка загрузки: ${err.message}</div>`;
    }
}

function renderExamsList(exams) {
    console.log('[teacher.js] renderExamsList(), количество экзаменов:', exams.length);
    if (!exams.length) {
        showEmpty(examsListDiv, 'Нет доступных экзаменов. Загрузите первый экзамен через импорт.');
        return;
    }
    examsListDiv.innerHTML = exams
        .map(
            (exam) => `
        <div class="exam-item mb-2 p-2 border rounded">
            <div class="d-flex justify-content-between align-items-center flex-wrap">
                <div>
                    <strong>${escapeHtml(exam.title)}</strong>
                    <div class="small text-muted">${escapeHtml(exam.description || 'Без описания')}</div>
                    <div class="small">
                        Вопросов в базе: ${exam.questionsCount}
                        (🔘 ${exam.singleChoiceCount} | ☑️ ${exam.multipleChoiceCount} | ✏️ ${exam.textInputCount})
                    </div>
                    <div class="small">Показывать студенту: 🔘 ${exam.singleChoiceToShow === 0 ? 'все' : exam.singleChoiceToShow} / ☑️ ${exam.multipleChoiceToShow === 0 ? 'все' : exam.multipleChoiceToShow} / ✏️ ${exam.textInputToShow === 0 ? 'все' : exam.textInputToShow}</div>
                    <div class="small">Создан: ${new Date(exam.createdAt).toLocaleString()}</div>
                </div>
                <div>
                    <button class="btn btn-sm btn-info view-btn" data-id="${exam.id}">
                        <i class="bi bi-eye"></i> Просмотр
                    </button>
                    <button class="btn btn-sm btn-warning edit-btn" data-id="${exam.id}">
                        <i class="bi bi-pencil"></i> Редактировать
                    </button>
                    <button class="btn btn-sm btn-danger delete-btn" data-id="${exam.id}">
                        <i class="bi bi-trash3"></i> Удалить
                    </button>
                </div>
            </div>
        </div>
    `
        )
        .join('');

    document.querySelectorAll('.view-btn').forEach((btn) => {
        btn.addEventListener('click', () => {
            console.log('[teacher.js] Нажата кнопка "Просмотр" для экзамена', btn.dataset.id);
            showExamDetails(btn.dataset.id);
        });
    });
    document.querySelectorAll('.edit-btn').forEach((btn) => {
        btn.addEventListener('click', () => {
            console.log('[teacher.js] Нажата кнопка "Редактировать" для экзамена', btn.dataset.id);
            window.location.href = `edit.html?id=${btn.dataset.id}`;
        });
    });
    document.querySelectorAll('.delete-btn').forEach((btn) => {
        btn.addEventListener('click', async () => {
            console.log('[teacher.js] Нажата кнопка "Удалить" для экзамена', btn.dataset.id);
            if (confirm('Удалить экзамен? Это действие необратимо.')) {
                await deleteExam(btn.dataset.id);
            }
        });
    });
}

async function deleteExam(id) {
    console.log('[teacher.js] deleteExam() для id:', id);
    try {
        console.log('[teacher.js] Вызов apiRequest DELETE', `${EXAMS_URL}/${id}`);
        await apiRequest(`${EXAMS_URL}/${id}`, { method: 'DELETE' });
        console.log('[teacher.js] Экзамен удалён');
        showMessage(importResultDiv, '✅ Экзамен удалён', 'success');
        await loadExamsList();
        if (modal) modal.hide();
    } catch (err) {
        console.error('[teacher.js] Ошибка удаления:', err);
        showMessage(importResultDiv, `❌ Ошибка удаления: ${err.message}`, 'error');
    }
}

async function showExamDetails(id) {
    console.log('[teacher.js] showExamDetails() для id:', id);
    try {
        console.log('[teacher.js] Вызов apiRequest GET', `${EXAMS_URL}/${id}`);
        const exam = await apiRequest(`${EXAMS_URL}/${id}`);
        console.log('[teacher.js] Получен экзамен:', exam);
        renderExamDetails(exam);
        if (modal) modal.show();
    } catch (err) {
        console.error('[teacher.js] Ошибка загрузки деталей:', err);
        showMessage(importResultDiv, `❌ Ошибка загрузки деталей: ${err.message}`, 'error');
    }
}

function renderExamDetails(exam) {
    console.log('[teacher.js] renderExamDetails()');
    modalContent.innerHTML = `
        <h3>${escapeHtml(exam.title)}</h3>
        <p><strong>Описание:</strong> ${escapeHtml(exam.description || '—')}</p>
        <p><strong>Дата создания:</strong> ${new Date(exam.createdAt).toLocaleString()}</p>
        <p><strong>Показывать студенту:</strong> 🔘 ${exam.singleChoiceToShow === 0 ? 'все' : exam.singleChoiceToShow} / ☑️ ${exam.multipleChoiceToShow === 0 ? 'все' : exam.multipleChoiceToShow} / ✏️ ${exam.textInputToShow === 0 ? 'все' : exam.textInputToShow}</p>
        <hr>
        <h4>Вопросы (${exam.questions.length})</h4>
        <div class="accordion" id="questionsAccordion">
            ${exam.questions
        .map((q, idx) => {
            const typeIcon = getTypeIcon(q.type);
            const typeLabel = getTypeLabel(q.type);
            let optionsHtml = '';
            if (q.options && q.options.length) {
                optionsHtml = `
                        <div class="mt-2"><strong>Варианты ответов:</strong></div>
                        <div class="list-group mt-1">
                            ${q.options
                    .map((opt) => {
                        const isCorrect =
                            q.correctAnswers && q.correctAnswers.includes(opt);
                        const checkMark = isCorrect ? '✅ ' : '';
                        return `
                                    <div class="list-group-item d-flex align-items-center">
                                        <span class="me-2">${checkMark}</span>
                                        <span>${escapeHtml(opt)}</span>
                                    </div>
                                `;
                    })
                    .join('')}
                        </div>
                    `;
            }
            let textInputHtml = '';
            if (q.type === 'TextInput' && q.correctAnswers && q.correctAnswers.length) {
                textInputHtml = `
                        <div class="mt-2 text-success">
                            <strong>✓ Правильный ответ:</strong>
                            <span class="badge bg-success">${escapeHtml(q.correctAnswers[0])}</span>
                        </div>
                    `;
            }
            return `
                    <div class="accordion-item">
                        <h2 class="accordion-header" id="heading${idx}">
                            <button class="accordion-button ${idx !== 0 ? 'collapsed' : ''}" type="button" data-bs-toggle="collapse" data-bs-target="#collapse${idx}" aria-expanded="${idx === 0 ? 'true' : 'false'}" aria-controls="collapse${idx}">
                                <div class="d-flex justify-content-between w-100 me-3">
                                    <span><strong>Вопрос ${idx + 1}:</strong> ${escapeHtml(q.text)}</span>
                                    <span class="badge bg-secondary ms-2">${typeIcon} ${typeLabel}</span>
                                </div>
                            </button>
                        </h2>
                        <div id="collapse${idx}" class="accordion-collapse collapse ${idx === 0 ? 'show' : ''}" data-bs-parent="#questionsAccordion">
                            <div class="accordion-body">
                                ${optionsHtml}
                                ${textInputHtml}
                            </div>
                        </div>
                    </div>
                `;
        })
        .join('')}
        </div>
    `;
}

function getTypeIcon(type) {
    switch (type) {
        case 'SingleChoice': return '🔘';
        case 'MultipleChoice': return '☑️';
        case 'TextInput': return '✏️';
        default: return '❓';
    }
}

function getTypeLabel(type) {
    switch (type) {
        case 'SingleChoice': return 'Один вариант';
        case 'MultipleChoice': return 'Несколько вариантов';
        case 'TextInput': return 'Ввод текста';
        default: return 'Неизвестный тип';
    }
}

loadExamsList();
