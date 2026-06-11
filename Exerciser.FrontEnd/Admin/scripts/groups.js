/* global showMessage, clearMessage, showLoading, showEmpty, apiRequest, escapeHtml */

let currentGroupIdForModal = null;

document.addEventListener('DOMContentLoaded', async () => {
    await loadGroups();

    document.getElementById('createGroupBtn').addEventListener('click', () => {
        document.getElementById('createGroupForm').reset();
        const modal = new bootstrap.Modal(document.getElementById('createGroupModal'));
        modal.show();
    });

    document.getElementById('createGroupForm').addEventListener('submit', async (e) => {
        e.preventDefault();
        const name = document.getElementById('groupName').value.trim();
        if (!name) {
            showMessage(
                document.getElementById('groupsList'),
                '❌ Название группы обязательно',
                'error'
            );
            return;
        }
        try {
            await apiRequest('/api/v1/groups', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ name }),
            });
            bootstrap.Modal.getInstance(document.getElementById('createGroupModal')).hide();
            await loadGroups();
            showMessage(document.getElementById('groupsList'), '✅ Группа создана', 'success');
        } catch (err) {
            showMessage(
                document.getElementById('groupsList'),
                `❌ Ошибка: ${err.message}`,
                'error'
            );
        }
    });

    document.getElementById('importGroupBtn').addEventListener('click', () => {
        document.getElementById('importGroupForm').reset();
        const modal = new bootstrap.Modal(document.getElementById('importGroupModal'));
        modal.show();
    });

    document.getElementById('importGroupForm').addEventListener('submit', async (e) => {
        e.preventDefault();
        const fileInput = document.getElementById('groupFile');
        if (!fileInput.files || fileInput.files.length === 0) {
            showMessage(document.getElementById('groupsList'), '❌ Выберите JSON-файл', 'error');
            return;
        }
        const file = fileInput.files[0];
        if (!file.name.toLowerCase().endsWith('.json')) {
            showMessage(document.getElementById('groupsList'), '❌ Файл должен быть JSON', 'error');
            return;
        }
        const formData = new FormData();
        formData.append('file', file);
        try {
            await apiRequest('/api/v1/groups/import', {
                method: 'POST',
                body: formData,
            });
            bootstrap.Modal.getInstance(document.getElementById('importGroupModal')).hide();
            await loadGroups();
            showMessage(
                document.getElementById('groupsList'),
                '✅ Группа импортирована',
                'success'
            );
        } catch (err) {
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
        if (!lastName || !firstName) {
            showMessage(
                document.getElementById('studentsContent'),
                '❌ Фамилия и имя обязательны',
                'error'
            );
            return;
        }
        const patronymic = document.getElementById('studentPatronymic').value.trim() || null;
        try {
            await apiRequest(`/api/v1/groups/${groupId}/students`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ lastName, firstName, patronymic }),
            });
            document.getElementById('addStudentForm').reset();
            await showGroupStudents(groupId);
            showMessage(
                document.getElementById('studentsContent'),
                '✅ Студент добавлен',
                'success'
            );
        } catch (err) {
            showMessage(
                document.getElementById('studentsContent'),
                `❌ Ошибка: ${err.message}`,
                'error'
            );
        }
    });
});

async function loadGroups() {
    const container = document.getElementById('groupsList');
    showLoading(container);
    try {
        const groups = await apiRequest('/api/v1/groups');
        if (!Array.isArray(groups) || groups.length === 0) {
            showEmpty(container, 'Нет групп. Создайте первую группу.');
            return;
        }
        renderGroupsList(groups);
    } catch (err) {
        container.innerHTML = `<div class="alert alert-danger">❌ Ошибка загрузки: ${err.message}</div>`;
        console.error(err);
    }
}

function renderGroupsList(groups) {
    const container = document.getElementById('groupsList');
    container.innerHTML = groups
        .map(
            (group) => `
        <div class="card mb-2">
            <div class="card-body d-flex justify-content-between align-items-center">
                <div>
                    <strong>${escapeHtml(group.name)}</strong>
                    <div class="small text-muted">Студентов: ${group.students.length}</div>
                </div>
                <div>
                    <button class="btn btn-sm btn-info view-students-btn" data-id="${group.id}" data-name="${escapeHtml(group.name)}">👥 Студенты</button>
                </div>
            </div>
        </div>
    `
        )
        .join('');

    document.querySelectorAll('.view-students-btn').forEach((btn) => {
        btn.addEventListener('click', async () => {
            const groupId = btn.dataset.id;
            const groupName = btn.dataset.name;
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
    const container = document.getElementById('studentsContent');
    showLoading(container);
    try {
        const groups = await apiRequest('/api/v1/groups');
        const group = groups.find((g) => g.id === groupId);
        if (!group) throw new Error('Группа не найдена');
        if (!group.students || group.students.length === 0) {
            container.innerHTML = '<div class="text-muted">Нет студентов в этой группе.</div>';
            return;
        }
        container.innerHTML = `
            <ul class="list-group">
                ${group.students.map((s) => `<li class="list-group-item">${escapeHtml(s.fullName)}</li>`).join('')}
            </ul>
        `;
    } catch (err) {
        container.innerHTML = `<div class="alert alert-danger">❌ Ошибка: ${err.message}</div>`;
    }
}
