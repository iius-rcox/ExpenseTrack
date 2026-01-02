import js from '@eslint/js';
import globals from 'globals';
import reactHooks from 'eslint-plugin-react-hooks';
import reactRefresh from 'eslint-plugin-react-refresh';
import tseslint from 'typescript-eslint';

export default tseslint.config(
  { ignores: ['dist', 'node_modules', 'build', 'coverage', '*.config.js', '*.config.ts'] },
  {
    extends: [js.configs.recommended, ...tseslint.configs.recommended],
    files: ['**/*.{ts,tsx}'],
    languageOptions: {
      ecmaVersion: 2020,
      globals: globals.browser,
    },
    plugins: {
      'react-hooks': reactHooks,
      'react-refresh': reactRefresh,
    },
    rules: {
      ...reactHooks.configs.recommended.rules,
      'react-refresh/only-export-components': [
        'warn',
        {
          allowConstantExport: true,
          // Allow shadcn/ui patterns: variant configs and co-located hooks
          allowExportNames: [
            // shadcn/ui variant configs
            'badgeVariants',
            'buttonVariants',
            'sidebarMenuButtonVariants',
            // Custom hooks co-located with components
            'useIsMobile',
            'useSwipeAction',
            'useSwipeActionList',
            'useSidebar',
            // Utility functions co-located with components
            'getConfidenceLevel',
            'buildMatchingFactors',
          ],
        },
      ],
      '@typescript-eslint/no-unused-vars': ['warn', { argsIgnorePattern: '^_' }],
      '@typescript-eslint/no-explicit-any': 'warn',
    },
  }
);
