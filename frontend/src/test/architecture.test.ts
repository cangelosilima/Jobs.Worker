import { describe, it, expect } from 'vitest';
import { readdir, readFile } from 'fs/promises';
import { join } from 'path';

describe('Architecture Tests', () => {
  const srcPath = join(__dirname, '..');

  it('should not import from parent directories using ../..', async () => {
    const files = await getAllTypeScriptFiles(srcPath);
    const violations: string[] = [];

    for (const file of files) {
      const content = await readFile(file, 'utf-8');
      const lines = content.split('\n');

      lines.forEach((line, index) => {
        if (line.includes('from ') && line.includes('../..')) {
          violations.push(`${file}:${index + 1} - ${line.trim()}`);
        }
      });
    }

    expect(violations).toHaveLength(0);
  });

  it('should use @ alias for absolute imports', async () => {
    const files = await getAllTypeScriptFiles(srcPath);
    let hasAliasImports = false;

    for (const file of files) {
      const content = await readFile(file, 'utf-8');
      if (content.includes("from '@/")) {
        hasAliasImports = true;
        break;
      }
    }

    expect(hasAliasImports).toBe(true);
  });

  it('should have test files in __tests__ directories or *.test.ts(x)', async () => {
    const files = await getAllTypeScriptFiles(srcPath);
    const testFiles = files.filter(
      (f) => f.includes('.test.ts') || f.includes('.test.tsx') || f.includes('__tests__')
    );

    expect(testFiles.length).toBeGreaterThan(0);
  });

  it('modules should not import from other modules directly', async () => {
    const modulesPath = join(srcPath, 'modules');
    const files = await getAllTypeScriptFiles(modulesPath);
    const violations: string[] = [];

    for (const file of files) {
      const content = await readFile(file, 'utf-8');
      const lines = content.split('\n');

      // Get current module name (e.g., dashboard, jobs, schedules)
      const currentModule = file.split('/modules/')[1]?.split('/')[0];

      lines.forEach((line, index) => {
        // Check if importing from another module
        const moduleImportRegex = /from ['"]@\/modules\/([^'"\/]+)/;
        const match = line.match(moduleImportRegex);

        if (match && match[1] !== currentModule) {
          // Modules should not import from each other, only from shared layers
          violations.push(`${file}:${index + 1} - Cross-module import: ${line.trim()}`);
        }
      });
    }

    expect(violations).toHaveLength(0);
  });

  it('API layer should not import from UI components', async () => {
    const apiPath = join(srcPath, 'api');
    const files = await getAllTypeScriptFiles(apiPath);
    const violations: string[] = [];

    for (const file of files) {
      const content = await readFile(file, 'utf-8');
      const lines = content.split('\n');

      lines.forEach((line, index) => {
        if (
          (line.includes("from '@/components") ||
            line.includes("from '@/modules") ||
            line.includes("from '@/layouts")) &&
          line.includes('import')
        ) {
          violations.push(`${file}:${index + 1} - ${line.trim()}`);
        }
      });
    }

    expect(violations).toHaveLength(0);
  });

  it('Services should not import from UI components', async () => {
    const servicesPath = join(srcPath, 'services');
    const files = await getAllTypeScriptFiles(servicesPath);
    const violations: string[] = [];

    for (const file of files) {
      const content = await readFile(file, 'utf-8');
      const lines = content.split('\n');

      lines.forEach((line, index) => {
        if (
          (line.includes("from '@/components") ||
            line.includes("from '@/modules") ||
            line.includes("from '@/layouts")) &&
          line.includes('import')
        ) {
          violations.push(`${file}:${index + 1} - ${line.trim()}`);
        }
      });
    }

    expect(violations).toHaveLength(0);
  });

  it('State stores should not import from UI components', async () => {
    const statePath = join(srcPath, 'state');
    const files = await getAllTypeScriptFiles(statePath);
    const violations: string[] = [];

    for (const file of files) {
      const content = await readFile(file, 'utf-8');
      const lines = content.split('\n');

      lines.forEach((line, index) => {
        if (
          (line.includes("from '@/components") ||
            line.includes("from '@/modules") ||
            line.includes("from '@/layouts")) &&
          line.includes('import')
        ) {
          violations.push(`${file}:${index + 1} - ${line.trim()}`);
        }
      });
    }

    expect(violations).toHaveLength(0);
  });
});

async function getAllTypeScriptFiles(dir: string): Promise<string[]> {
  const files: string[] = [];

  async function walk(directory: string) {
    try {
      const entries = await readdir(directory, { withFileTypes: true });

      for (const entry of entries) {
        const fullPath = join(directory, entry.name);

        if (entry.isDirectory() && entry.name !== 'node_modules' && entry.name !== '.git') {
          await walk(fullPath);
        } else if (entry.isFile() && (entry.name.endsWith('.ts') || entry.name.endsWith('.tsx'))) {
          files.push(fullPath);
        }
      }
    } catch (error) {
      // Skip directories we can't access
    }
  }

  await walk(dir);
  return files;
}
