/* global showMessage, clearMessage, showLoading, showEmpty, apiRequest, escapeHtml */

console.log('[groups.js] Скрипт загружен');

let currentGroupIdForModal = null;

document.addEventListener('DOMContentLoaded', async () => {
    console.log('[groups.js] DOMContentLoaded, инициализация...');
    await loadGroups();

    const createGroupBtn = document.getElementById('createGroupBtn');
    console.log('[groups.js] createGroupBtn найден:', !!createGroupBtn);
    createGroupBtn.addEventListener('click', () => {
        console.log('[groups.js] Нажата кнопка "Новая группа"');
        document.getElementById('createGroupForm').reset();
        const modal = new bootstrap.Modal(document.getElementById('createGroupModal'));
        modal.show();
    });

    document.getElementById('createGroupForm').addEventListener('submit', async (e) => {
        e.preventDefault();
        console.log('[groups.js] Отправка формы создания группы');
        const name = document.getElementById('groupName').value.trim();
        if (!name) {
            console.warn('[groups.js] Название группы пустое');
            showMessage(
                document.getElementById('groupsList'),
                '❌ Название группы обязательно',
                'error'
            );
            return;
        }
        try {
            console.log('[groups.js] Вызов apiRequest POST /api/v1/groups с именем:', name);
            await apiRequest('/api/v1/groups', {
                method: 'POST',
                headers: {'Content-Type': 'application/json'},
                body: JSON.stringify({name}),
            });
            const modalElement = document.getElementById('createGroupModal');
            const modal = bootstrap.Modal.getInstance(modalElement);
            if (modal) modal.hide();
            console.log('[groups.js] Группа создана, перезагружаем список');
            await loadGroups();
            showMessage(document.getElementById('groupsList'), '✅ Группа создана', 'success');
        } catch (err) {
            console.error('[groups.js] Ошибка при создании группы:', err);
            showMessage(
                document.getElementById('groupsList'),
                `❌ Ошибка: ${err.message}`,
                'error'
            );
        }
    });

    const importGroupBtn = document.getElementById('importGroupBtn');
    console.log('[groups.js] importGroupBtn найден:', !!importGroupBtn);
    importGroupBtn.addEventListener('click', () => {
        console.log('[groups.js] Нажата кнопка "Импорт группы (JSON)"');
        document.getElementById('importGroupForm').reset();
        const modal = new bootstrap.Modal(document.getElementById('importGroupModal'));
        modal.show();
    });

    const fileInput = document.getElementById('groupFile');
    fileInput.addEventListener('change', () => {
        console.log('[groups.js] Файл выбран:', fileInput.files.length > 0 ? fileInput.files[0].name : 'нет файла');
    });

    document.getElementById('importGroupForm').addEventListener('submit', async (e) => {
        e.preventDefault();
        console.log('[groups.js] Отправка формы импорта группы');
        const fileInput = document.getElementById('groupFile');
        console.log('[groups.js] fileInput.files:', fileInput.files);
        if (!fileInput.files || fileInput.files.length === 0) {
            console.warn('[groups.js] Файл не выбран');
            showMessage(document.getElementById('groupsList'), '❌ Выберите JSON-файл', 'error');
            return;
        }
        const file = fileInput.files[0];
        console.log('[groups.js] Выбран файл:', file.name, 'размер:', file.size);
        if (!file.name.toLowerCase().endsWith('.json')) {
            console.warn('[groups.js] Файл не JSON');
            showMessage(document.getElementById('groupsList'), '❌ Файл должен быть JSON', 'error');
            return;
        }
        const formData = new FormData();
        formData.append('file', file);
        console.log('[groups.js] FormData создан, содержимое:', [...formData.entries()]);
        try {
            console.log('[groups.js] Вызов apiRequest POST /api/v1/groups/import');
            await apiRequest('/api/v1/groups/import', {
                method: 'POST',
                body: formData,
            });
            const modalElement = document.getElementById('importGroupModal');
            const modal = bootstrap.Modal.getInstance(modalElement);
            if (modal) modal.hide();
            console.log('[groups.js] Импорт успешен, перезагружаем список групп');
            await loadGroups();
            showMessage(
                document.getElementById('groupsList'),
                '✅ Группа импортирована',
                'success'
            );
        } catch (err) {
            console.error('[groups.js] Ошибка при импорте группы:', err);
            showMessage(
                document.getElementById('groupsList'),
                `❌ Ошибка импорта: ${err.message}`,
                'error'
            );
        }
    });

    document.getElementById('addStudentForm').addEventListener('submit', async (e) => {
        e.preventDefault();
        const groupId = document.getElementById('currentGroupId').value;
        const lastName = document.getElementById('studentLastName').value.trim();
        const firstName = document.getElementById('studentFirstName').value.trim();
        console.log('[groups.js] Добавление студента в группу', groupId, 'ФИО:', lastName, firstName);
        if (!lastName || !firstName) {
            console.warn('[groups.js] Фамилия или имя пустые');
            showMessage(
                document.getElementById('studentsContent'),
                '❌ Фамилия и имя обязательны',
                'error'
            );
            return;
        }
        const patronymic = document.getElementById('studentPatronymic').value.trim() || null;
        try {
            console.log('[groups.js] Вызов apiRequest POST /api/v1/groups/${groupId}/students');
            await apiRequest(`/api/v1/groups/${groupId}/students`, {
                method: 'POST',
                headers: {'Content-Type': 'application/json'},
                body: JSON.stringify({lastName, firstName, patronymic}),
            });
            document.getElementById('addStudentForm').reset();
            console.log('[groups.js] Студент добавлен, обновляем список студентов');
            await showGroupStudents(groupId);
            showMessage(
                document.getElementById('studentsContent'),
                '✅ Студент добавлен',
                'success'
            );
        } catch (err) {
            console.error('[groups.js] Ошибка при добавлении студента:', err);
            showMessage(
                document.getElementById('studentsContent'),
                `❌ Ошибка: ${err.message}`,
                'error'
            );
        }
    });
});

async function loadGroups() {
    console.log('[groups.js] loadGroups() начат');
    const container = document.getElementById('groupsList');
    showLoading(container);
    try {
        console.log('[groups.js] Вызов apiRequest GET /api/v1/groups');
        const groups = await apiRequest('/api/v1/groups');
        console.log('[groups.js] Получены группы:', groups);
        if (!Array.isArray(groups) || groups.length === 0) {
            console.log('[groups.js] Групп нет, показываем пустое состояние');
            showEmpty(container, 'Нет групп. Создайте первую группу.');
            return;
        }
        renderGroupsList(groups);
    } catch (err) {
        console.error('[groups.js] Ошибка загрузки групп:', err);
        container.innerHTML = `<div class="alert alert-danger">❌ Ошибка загрузки: ${err.message}</div>`;
    }
}

function renderGroupsList(groups) {
    console.log('[groups.js] renderGroupsList(), количество групп:', groups.length);
    const container = document.getElementById('groupsList');
    const list = document.createElement('ul');
    list.className = 'list-group';
    groups.forEach(group => {
        const li = document.createElement('li');
        li.className = 'list-group-item d-flex justify-content-between align-items-center';
        li.innerHTML = `
            <div>
                <strong>${escapeHtml(group.name)}</strong>
                <div class="small text-muted">Студентов: ${group.students.length}</div>
            </div>
            <button class="btn btn-sm btn-info view-students-btn" data-id="${group.id}" data-name="${escapeHtml(group.name)}">
                <i class="bi bi-people"></i> Студенты
            </button>
        `;
        list.appendChild(li);
    });
    container.innerHTML = '';
    container.appendChild(list);

    document.querySelectorAll('.view-students-btn').forEach((btn) => {
        btn.addEventListener('click', async () => {
            const groupId = btn.dataset.id;
            const groupName = btn.dataset.name;
            console.log('[groups.js] Нажата кнопка "Студенты" для группы', groupName, groupId);
            document.getElementById('studentsModalTitle').innerText =
                `Студенты группы: ${groupName}`;
            document.getElementById('currentGroupId').value = groupId;
            await showGroupStudents(groupId);
            const modal = new bootstrap.Modal(document.getElementById('studentsModal'));
            modal.show();
        });
    });
}

async function showGroupStudents(groupId) {
    console.log('[groups.js] showGroupStudents(), groupId:', groupId);
    const container = document.getElementById('studentsContent');
    showLoading(container);
    try {
        console.log('[groups.js] Вызов apiRequest GET /api/v1/groups для поиска группы');
        const groups = await apiRequest('/api/v1/groups');
        const group = groups.find((g) => g.id === groupId);
        if (!group) throw new Error('Группа не найдена');
        console.log('[groups.js] Группа найдена, студентов:', group.students.length);
        if (!group.students || group.students.length === 0) {
            container.innerHTML = '<div class="text-muted">Нет студентов в этой группе.</div>';
            return;
        }
        const list = document.createElement('ul');
        list.className = 'list-group';
        group.students.forEach(s => {
            const li = document.createElement('li');
            li.className = 'list-group-item';
            li.innerHTML = `<i class="bi bi-person me-2"></i>${escapeHtml(s.fullName)}`;
            list.appendChild(li);
        });
        container.innerHTML = '';
        container.appendChild(list);
    } catch (err) {
        console.error('[groups.js] Ошибка загрузки студентов:', err);
        container.innerHTML = `<div class="alert alert-danger">❌ Ошибка: ${err.message}</div>`;
    }
}
