/* global showMessage, apiRequest, escapeHtml */

console.log('[analytics.js] Скрипт загружен');

const ANALYTICS_URL = '/api/v1/analytics/attempts/last';
const GROUPS_URL = '/api/v1/groups';

let allData = [];
let filteredData = [];
let currentSort = { key: 'finishedAt', direction: 'desc' };

const groupFilter = document.getElementById('groupFilter');
const examFilter = document.getElementById('examFilter');
const clearFiltersBtn = document.getElementById('clearFiltersBtn');
const container = document.getElementById('analyticsResult');
const table = document.getElementById('analyticsTable');
const tbody = document.getElementById('analyticsBody');

// Загрузка групп для фильтра
async function loadGroupFilter() {
    try {
        const groups = await apiRequest(GROUPS_URL);
        groupFilter.innerHTML = '<option value="">Все группы</option>';
        groups.forEach(g => {
            const option = document.createElement('option');
            option.value = g.name;
            option.textContent = g.name;
            groupFilter.appendChild(option);
        });
    } catch (err) {
        console.warn('Не удалось загрузить группы для фильтра', err);
    }
}

// Загрузка аналитики
async function loadAnalytics() {
    try {
        container.innerHTML = '<div class="text-muted">Загрузка данных...</div>';
        allData = await apiRequest(ANALYTICS_URL);
        container.classList.add('d-none');
        table.classList.remove('d-none');

        // Заполняем фильтр экзаменов
        const examSet = new Set(allData.map(item => item.examTitle));
        examFilter.innerHTML = '<option value="">Все экзамены</option>';
        examSet.forEach(title => {
            const option = document.createElement('option');
            option.value = title;
            option.textContent = title;
            examFilter.appendChild(option);
        });

        // Обновляем список
        applyFiltersAndSort();
    } catch (err) {
        container.classList.remove('d-none');
        container.innerHTML = `<div class="alert alert-danger">❌ Ошибка загрузки: ${err.message}</div>`;
        table.classList.add('d-none');
        console.error(err);
    }
}

// Применение фильтров и сортировки
function applyFiltersAndSort() {
    const group = groupFilter.value;
    const exam = examFilter.value;

    // Фильтрация
    filteredData = allData.filter(item => {
        let match = true;
        if (group) match = match && item.groupName === group;
        if (exam) match = match && item.examTitle === exam;
        return match;
    });

    // Сортировка
    const { key, direction } = currentSort;
    filteredData.sort((a, b) => {
        let aVal, bVal;
        switch (key) {
            case 'studentFullName':
            case 'groupName':
            case 'examTitle':
                aVal = a[key] || '';
                bVal = b[key] || '';
                return direction === 'asc' ? aVal.localeCompare(bVal) : bVal.localeCompare(aVal);
            case 'score':
                aVal = a.totalScore / a.maxPossibleScore || 0;
                bVal = b.totalScore / b.maxPossibleScore || 0;
                break;
            case 'percent':
                aVal = a.percent || 0;
                bVal = b.percent || 0;
                break;
            case 'finishedAt':
                aVal = new Date(a.finishedAt).getTime();
                bVal = new Date(b.finishedAt).getTime();
                break;
            case 'duration':
                aVal = a.durationMinutes || 0;
                bVal = b.durationMinutes || 0;
                break;
            default:
                aVal = 0;
                bVal = 0;
        }
        if (aVal < bVal) return direction === 'asc' ? -1 : 1;
        if (aVal > bVal) return direction === 'asc' ? 1 : -1;
        return 0;
    });

    renderTable();
}

// Рендеринг таблицы
function renderTable() {
    if (filteredData.length === 0) {
        tbody.innerHTML = `<tr><td colspan="7" class="text-center text-muted">Нет данных для отображения</td></tr>`;
        return;
    }

    tbody.innerHTML = filteredData.map(item => `
        <tr>
            <td>${escapeHtml(item.studentFullName)}</td>
            <td>${escapeHtml(item.groupName)}</td>
            <td>${escapeHtml(item.examTitle)}</td>
            <td>${item.totalScore} / ${item.maxPossibleScore}</td>
            <td>
                <div class="progress" style="height: 20px; width: 80px; display: inline-block; vertical-align: middle;">
                    <div class="progress-bar ${item.percent >= 70 ? 'bg-success' : item.percent >= 40 ? 'bg-warning' : 'bg-danger'}"
                         role="progressbar"
                         style="width: ${item.percent}%;"
                         aria-valuenow="${item.percent}"
                         aria-valuemin="0"
                         aria-valuemax="100">
                        ${item.percent}%
                    </div>
                </div>
            </td>
            <td>${new Date(item.finishedAt).toLocaleString()}</td>
            <td>${item.durationMinutes}</td>
        </tr>
    `).join('');

    // Обновляем индикаторы сортировки в заголовках
    document.querySelectorAll('.sortable').forEach(th => {
        const key = th.dataset.sort;
        const arrow = th.querySelector('.sort-arrow');
        if (arrow) arrow.remove();
        if (key === currentSort.key) {
            const span = document.createElement('span');
            span.className = 'sort-arrow ms-1';
            span.textContent = currentSort.direction === 'asc' ? '▲' : '▼';
            th.appendChild(span);
        }
    });
}

// Обработчики фильтров
groupFilter.addEventListener('change', applyFiltersAndSort);
examFilter.addEventListener('change', applyFiltersAndSort);

// Сброс фильтров
clearFiltersBtn.addEventListener('click', () => {
    groupFilter.value = '';
    examFilter.value = '';
    applyFiltersAndSort();
});

// Сортировка по клику на заголовок
document.addEventListener('click', (e) => {
    const th = e.target.closest('.sortable');
    if (!th) return;
    const key = th.dataset.sort;
    if (currentSort.key === key) {
        currentSort.direction = currentSort.direction === 'asc' ? 'desc' : 'asc';
    } else {
        currentSort.key = key;
        currentSort.direction = 'asc';
    }
    applyFiltersAndSort();
});

// Загружаем всё при готовности
document.addEventListener('DOMContentLoaded', async () => {
    await loadGroupFilter();
    await loadAnalytics();
});
