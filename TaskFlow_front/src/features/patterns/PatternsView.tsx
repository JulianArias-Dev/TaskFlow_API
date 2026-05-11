import { useState, useEffect } from 'react';
import { BookOpen, Code, ChevronRight } from 'lucide-react';
import { Prism as SyntaxHighlighter } from 'react-syntax-highlighter';
import { vscDarkPlus } from 'react-syntax-highlighter/dist/esm/styles/prism';

// @ts-expect-error - Vite specific import.meta.glob
const patternFilesRaw = import.meta.glob('../../lib/designPatterns/*.ts', { as: 'raw' });

export function PatternsView() {
  const [patterns, setPatterns] = useState<{ path: string, content: string, name: string }[]>([]);
  const [selectedPattern, setSelectedPattern] = useState<{ path: string, content: string, name: string } | null>(null);

  useEffect(() => {
    const loadFiles = async () => {
      const files = [];
      for (const path in patternFilesRaw) {
        if (!path.endsWith('index.ts')) {
          const content = await patternFilesRaw[path]();
          const name = path.split('/').pop()?.replace('.ts', '') || 'Unknown';
          files.push({ path, content, name });
        }
      }
      setPatterns(files);
      if (files.length > 0) {
        setSelectedPattern(files[0]);
      }
    };
    loadFiles();
  }, []);

  return (
    <div className="flex h-full bg-white dark:bg-gray-800 dark:bg-gray-900 rounded-tl-xl overflow-hidden shadow-xl border-l border-t border-gray-100 dark:border-gray-700">
      {/* List */}
      <div className="w-1/3 min-w-[250px] border-r border-gray-100 dark:border-gray-700 flex flex-col bg-gray-50 dark:bg-gray-800">
        <div className="p-4 border-b border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800">
          <h2 className="text-lg font-bold text-gray-900 dark:text-gray-50 flex items-center gap-2">
            <BookOpen className="w-5 h-5 text-blue-600" />
            Patrones de Diseño
          </h2>
          <p className="text-sm text-gray-500 mt-1">Implementaciones en el sistema</p>
        </div>
        <div className="flex-1 overflow-y-auto p-2 space-y-1">
          {patterns.map(pattern => (
            <button
              key={pattern.path}
              onClick={() => setSelectedPattern(pattern)}
              className={`w-full flex items-center justify-between px-3 py-3 rounded-lg text-sm transition-all ${selectedPattern?.path === pattern.path ? 'bg-blue-600 text-white font-medium shadow-md' : 'text-gray-700 dark:text-gray-300 hover:bg-gray-200 dark:hover:bg-gray-700'}`}
            >
              <div className="flex items-center gap-2">
                <Code className="w-4 h-4 opacity-70" />
                {pattern.name}
              </div>
              <ChevronRight className={`w-4 h-4 transition-transform ${selectedPattern?.path === pattern.path ? 'opacity-100' : 'opacity-0 -translate-x-2'}`} />
            </button>
          ))}
        </div>
      </div>

      {/* Code Viewer */}
      <div className="flex-1 flex flex-col bg-[#1e1e1e] overflow-hidden">
        {selectedPattern ? (
          <>
            <div className="px-4 py-3 bg-[#1e1e1e] flex items-center justify-between border-b border-gray-800">
              <div className="flex items-center gap-2">
                <Code className="w-4 h-4 text-blue-400" />
                <span className="text-gray-300 font-mono text-sm">{selectedPattern.name}.ts</span>
              </div>
            </div>
            <div className="flex-1 overflow-auto bg-[#1e1e1e] custom-scrollbar">
              <SyntaxHighlighter
                language="typescript"
                style={vscDarkPlus}
                customStyle={{
                  margin: 0,
                  padding: '1.5rem',
                  background: 'transparent',
                  fontSize: '0.875rem',
                  lineHeight: '1.5',
                }}
                showLineNumbers={true}
              >
                {selectedPattern.content}
              </SyntaxHighlighter>
            </div>
          </>
        ) : (
          <div className="flex-1 flex items-center justify-center text-gray-500">
            <p>Selecciona un patrón para ver su código</p>
          </div>
        )}
      </div>
    </div>
  );
}
