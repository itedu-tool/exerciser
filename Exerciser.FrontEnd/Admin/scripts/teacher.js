/* global showMessage, clearMessage, showLoading, showEmpty, apiRequest, escapeHtml */

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
    const formData = new FormData();
    formData.append('file', file);
    try {
        const data = await apiRequest(IMPORT_URL, { method: 'POST', body: formData });
        showMessage(
            importResultDiv,
            `✅ Экзамен "${data.title}" импортирован!\nID: ${data.id}\nВопросов: ${data.questionsCount}`,
            'success'
        );
        await loadExamsList();
    } catch (err) {
        showMessage(importResultDiv, `❌ Ошибка импорта: ${err.message}`, 'error');
        console.error(err);
    }
}

uploadBtn.addEventListener('click', async () => {
    if (!fileInput.files || fileInput.files.length === 0) {
        showMessage(importResultDiv, '❌ Выберите JSON-файл', 'error');
        return;
    }
    const file = fileInput.files[0];
    if (!file.name.toLowerCase().endsWith('.json')) {
        showMessage(importResultDiv, '❌ Файл должен быть в формате JSON', 'error');
        return;
    }
    if (file.size > 10 * 1024 * 1024) {
        showMessage(importResultDiv, '❌ Файл превышает 10 MB', 'error');
        return;
    }
    clearMessage(importResultDiv);
    await importExam(file);
});

async function loadExamsList() {
    showLoading(examsListDiv);
    try {
        const exams = await apiRequest(EXAMS_URL);
        if (!Array.isArray(exams) || exams.length === 0) {
            showEmpty(
                examsListDiv,
                'Нет доступных экзаменов. Загрузите первый экзамен через импорт.'
            );
            return;
        }
        renderExamsList(exams);
    } catch (err) {
        examsListDiv.innerHTML = `<div class="alert alert-danger">❌ Ошибка загрузки: ${err.message}</div>`;
        console.error(err);
    }
}

function renderExamsList(exams) {
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
                    <div class="small">Вопросов: ${exam.questionsCount} | Создан: ${new Date(exam.createdAt).toLocaleString()}</div>
                </div>
                <div>
                    <button class="btn btn-sm btn-info view-btn" data-id="${exam.id}">👁️ Просмотр</button>
                    <button class="btn btn-sm btn-warning edit-btn" data-id="${exam.id}">✏️ Редактировать</button>
                    <button class="btn btn-sm btn-danger delete-btn" data-id="${exam.id}">🗑️ Удалить</button>
                </div>
            </div>
        </div>
    `
        )
        .join('');
    document.querySelectorAll('.view-btn').forEach((btn) => {
        btn.addEventListener('click', async (e) => {
            e.stopPropagation();
            const id = btn.dataset.id;
            await showExamDetails(id);
        });
    });
    document.querySelectorAll('.edit-btn').forEach((btn) => {
        btn.addEventListener('click', (e) => {
            const id = btn.dataset.id;
            window.location.href = `edit.html?id=${id}`;
        });
    });
    document.querySelectorAll('.delete-btn').forEach((btn) => {
        btn.addEventListener('click', async (e) => {
            e.stopPropagation();
            const id = btn.dataset.id;
            if (confirm('Удалить экзамен? Это действие необратимо.')) {
                await deleteExam(id);
            }
        });
    });
}

async function deleteExam(id) {
    try {
        await apiRequest(`${EXAMS_URL}/${id}`, { method: 'DELETE' });
        showMessage(importResultDiv, '✅ Экзамен удалён', 'success');
        await loadExamsList();
        if (modal) modal.hide();
    } catch (err) {
        showMessage(importResultDiv, `❌ Ошибка удаления: ${err.message}`, 'error');
    }
}

async function showExamDetails(id) {
    try {
        const exam = await apiRequest(`${EXAMS_URL}/${id}`);
        renderExamDetails(exam);
        if (modal) modal.show();
    } catch (err) {
        showMessage(importResultDiv, `❌ Ошибка загрузки деталей: ${err.message}`, 'error');
    }
}

function renderExamDetails(exam) {
    modalContent.innerHTML = `
        <h3>${escapeHtml(exam.title)}</h3>
        <p><strong>Описание:</strong> ${escapeHtml(exam.description || '—')}</p>
        <p><strong>Дата создания:</strong> ${new Date(exam.createdAt).toLocaleString()}</p>
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
        case 'SingleChoice':
            return '🔘';
        case 'MultipleChoice':
            return '☑️';
        case 'TextInput':
            return '✏️';
        default:
            return '❓';
    }
}

function getTypeLabel(type) {
    switch (type) {
        case 'SingleChoice':
            return 'Один вариант';
        case 'MultipleChoice':
            return 'Несколько вариантов';
        case 'TextInput':
            return 'Ввод текста';
        default:
            return 'Неизвестный тип';
    }
}

loadExamsList();
