/* global showMessage, clearMessage */

console.log('[health.js] Скрипт загружен');

const API_BASE = window.API_BASE;
const healthResultDiv = document.getElementById('healthResult');

async function testEndpoint(endpoint) {
    console.log('[health.js] testEndpoint() для', endpoint);
    const url = `${API_BASE}${endpoint}`;
    const start = performance.now();
    try {
        console.log('[health.js] Fetch:', url);
        const response = await fetch(url);
        const end = performance.now();
        const duration = (end - start).toFixed(0);
        console.log('[health.js] Ответ получен, статус:', response.status, 'время:', duration, 'ms');
        let responsePreview = '';
        const contentType = response.headers.get('content-type') || '';
        if (contentType.includes('application/json')) {
            const json = await response.json();
            responsePreview = JSON.stringify(json, null, 2);
            console.log('[health.js] JSON ответ:', json);
        } else {
            const text = await response.text();
            responsePreview = text.length > 500 ? text.substring(0, 500) + '…' : text;
            console.log('[health.js] Текстовый ответ (первые 500 символов):', responsePreview);
        }
        const statusIcon = response.ok ? '✅' : '⚠️';
        const message =
            `${statusIcon} [${response.status}] ${response.statusText}\n` +
            `⏱️ ${duration} ms\n` +
            `📄 Content-Type: ${contentType}\n` +
            `📦 Ответ:\n${responsePreview}`;
        showMessage(healthResultDiv, message, response.ok ? 'success' : 'info');
    } catch (err) {
        console.error('[health.js] Ошибка подключения:', err);
        showMessage(healthResultDiv, `❌ Ошибка подключения: ${err.message}`, 'error');
    }
}

document.querySelectorAll('.test-btn').forEach((btn) => {
    btn.addEventListener('click', async () => {
        const endpoint = btn.dataset.endpoint;
        console.log('[health.js] Нажата кнопка проверки для эндпоинта:', endpoint);
        clearMessage(healthResultDiv);
        await testEndpoint(endpoint);
    });
});
