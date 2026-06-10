import globals from 'globals';
import js from '@eslint/js';
import eslintConfigPrettier from 'eslint-config-prettier';
import eslintPluginPrettier from 'eslint-plugin-prettier';

export default [
    js.configs.recommended,
    eslintConfigPrettier,
    {
        files: ['scripts/**/*.js'],
        plugins: {
            prettier: eslintPluginPrettier,
        },
        languageOptions: {
            globals: {
                ...globals.browser,
                ...globals.es2021,
                bootstrap: 'readonly',
            },
            ecmaVersion: 'latest',
            sourceType: 'script',
        },
        rules: {
            'prettier/prettier': 'error',
            'no-console': 'off',
            'no-unused-vars': ['warn', { argsIgnorePattern: '^_' }],
        },
    },
];
