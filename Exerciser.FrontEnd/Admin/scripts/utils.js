/* exported showMessage, clearMessage, showLoading, showEmpty, apiRequest, escapeHtml */

/**
 * Отображает сообщение в указанном контейнере.
 * @param {HTMLElement} container - DOM-элемент для вывода сообщения.
 * @param {string} message - Текст сообщения.
 * @param {string} type - Тип сообщения: 'success', 'error', 'info'.
 */
function showMessage(container, message, type = 'success') {
    container.textContent = message;
    container.className = 'message-box show';
    if (type === 'success') container.classList.add('success');
    else if (type === 'error') container.classList.add('error');
    else container.classList.add('info');
}

/**
 * Очищает контейнер с сообщением.
 * @param {HTMLElement} container - DOM-элемент.
 */
function clearMessage(container) {
    container.classList.remove('show', 'success', 'error', 'info');
    container.textContent = '';
}

/**
 * Показывает индикатор загрузки в контейнере.
 * @param {HTMLElement} container - DOM-элемент.
 */
function showLoading(container) {
    container.innerHTML = '<div class="text-muted">Загрузка...</div>';
}

/**
 * Показывает сообщение об отсутствии данных.
 * @param {HTMLElement} container - DOM-элемент.
 * @param {string} [message] - Текст сообщения (по умолчанию "Нет данных").
 */
function showEmpty(container, message = 'Нет данных') {
    container.innerHTML = `<div class="text-muted">${escapeHtml(message)}</div>`;
}

/**
 * Выполняет HTTP-запрос к API и возвращает распарсенный JSON или null при статусе 204.
 * @param {string} endpoint - Относительный путь (начинается с "/").
 * @param {Object} [options] - Опции fetch (метод, заголовки, тело и т.д.).
 * @returns {Promise<Object|null>} - Результат запроса.
 * @throws {Error} - Если ответ не OK, выбрасывает ошибку с текстом из поля error.
 */
async function apiRequest(endpoint, options = {}) {
    const url = `${window.API_BASE}${endpoint}`;
    const response = await fetch(url, options);
    if (!response.ok) {
        let errorMsg = `Ошибка ${response.status}`;
        try {
            const data = await response.json();
            errorMsg = data.error || errorMsg;
        } catch {
            // игнорируем, если ответ не JSON
        }
        throw new Error(errorMsg);
    }
    if (response.status === 204) return null;
    return await response.json();
}

/**
 * Экранирует HTML-спецсимволы для предотвращения XSS.
 * @param {string} str - Входная строка.
 * @returns {string} - Экранированная строка.
 */
function escapeHtml(str) {
    if (!str) return '';
    return str.replace(/[&<>]/g, (m) => {
        if (m === '&') return '&amp;';
        if (m === '<') return '&lt;';
        if (m === '>') return '&gt;';
        return m;
    });
}
