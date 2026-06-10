/* global showMessage, clearMessage */

const API_BASE = window.API_BASE;
const healthResultDiv = document.getElementById('healthResult');

async function testEndpoint(endpoint) {
    const url = `${API_BASE}${endpoint}`;
    const start = performance.now();
    try {
        const response = await fetch(url);
        const end = performance.now();
        const duration = (end - start).toFixed(0);
        let responsePreview = '';
        const contentType = response.headers.get('content-type') || '';
        if (contentType.includes('application/json')) {
            const json = await response.json();
            responsePreview = JSON.stringify(json, null, 2);
        } else {
            const text = await response.text();
            responsePreview = text.length > 500 ? text.substring(0, 500) + '…' : text;
        }
        const statusIcon = response.ok ? '✅' : '⚠️';
        const message =
            `${statusIcon} [${response.status}] ${response.statusText}\n` +
            `⏱️ ${duration} ms\n` +
            `📄 Content-Type: ${contentType}\n` +
            `📦 Ответ:\n${responsePreview}`;
        showMessage(healthResultDiv, message, response.ok ? 'success' : 'info');
    } catch (err) {
        showMessage(healthResultDiv, `❌ Ошибка подключения: ${err.message}`, 'error');
    }
}

document.querySelectorAll('.test-btn').forEach((btn) => {
    btn.addEventListener('click', async () => {
        const endpoint = btn.dataset.endpoint;
        clearMessage(healthResultDiv);
        await testEndpoint(endpoint);
    });
});
