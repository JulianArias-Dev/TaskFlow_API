import React, { useState, useEffect } from 'react';
import { Search, Filter, Save, X, Trash2 } from 'lucide-react';
import { Button } from '../../components/ui/Button';
import { Input } from '../../components/ui/Input';
import { SavedFilter } from '../../types/models';
import { dbService } from '../../services/databaseService';
import { motion, AnimatePresence } from 'framer-motion';

export interface TaskFilterCriteria {
  searchTerm: string;
  assignees: string[];
  priorities: string[];
  types: string[];
  labels: string[];
  dateFrom: string;
  dateTo: string;
}

interface TaskFilterBarProps {
  projectId: string;
  criteria: TaskFilterCriteria;
  onFilterChange: (criteria: TaskFilterCriteria) => void;
  uniqueAssignees: { id: string; name: string }[];
  uniqueLabels: { name: string; color: string }[];
}

export function TaskFilterBar({ projectId, criteria, onFilterChange, uniqueAssignees, uniqueLabels }: TaskFilterBarProps) {
  const [showFilters, setShowFilters] = useState(false);
  const [savedFilters, setSavedFilters] = useState<SavedFilter[]>([]);
  const [filterName, setFilterName] = useState('');
  const [showSaveOverlay, setShowSaveOverlay] = useState(false);

  useEffect(() => {
    dbService.getSavedFilters(projectId).then(setSavedFilters);
  }, [projectId]);

  const updateCriteria = (updates: Partial<TaskFilterCriteria>) => {
    onFilterChange({ ...criteria, ...updates });
  };

  const handleSaveFilter = async () => {
    if (!filterName.trim()) return;
    await dbService.saveFilter({
      projectId,
      name: filterName,
      criteria
    });
    setFilterName('');
    setShowSaveOverlay(false);
    dbService.getSavedFilters(projectId).then(setSavedFilters);
  };

  const handleDeleteFilter = async (id: string) => {
    await dbService.deleteSavedFilter(id);
    setSavedFilters(prev => prev.filter(f => f.id !== id));
  };
  
  const loadSavedFilter = (filter: SavedFilter) => {
    onFilterChange(filter.criteria as TaskFilterCriteria);
  };

  const activeFiltersCount = 
    (criteria.searchTerm ? 1 : 0) + 
    criteria.assignees.length + 
    criteria.priorities.length + 
    criteria.types.length + 
    criteria.labels.length + 
    (criteria.dateFrom || criteria.dateTo ? 1 : 0);

  return (
    <div className="bg-white dark:bg-gray-800 dark:text-gray-100 rounded-xl shadow-sm border border-gray-200 dark:border-gray-700 p-4 mb-6 flex flex-col gap-4">
      <div className="flex flex-col sm:flex-row gap-4 items-center justify-between">
        <div className="relative flex-1 w-full max-w-md">
          <Search className="absolute left-3 top-2.5 w-5 h-5 text-gray-400" />
          <Input 
            value={criteria.searchTerm}
            onChange={e => updateCriteria({ searchTerm: e.target.value })}
            placeholder="Buscar por título o descripción..." 
            className="pl-10" 
          />
        </div>
        
        <div className="flex gap-2 w-full sm:w-auto">
          <Button 
            variant={showFilters ? 'primary' : 'outline'} 
            onClick={() => setShowFilters(!showFilters)}
            className="flex-1 sm:flex-none relative"
          >
            <Filter className="w-4 h-4 mr-2" />
            Filtros
            {activeFiltersCount > 0 && (
              <span className="absolute -top-2 -right-2 bg-blue-600 text-white text-xs w-5 h-5 rounded-full flex items-center justify-center">
                {activeFiltersCount}
              </span>
            )}
          </Button>
          
          {savedFilters.length > 0 && (
            <div className="relative group">
              <Button variant="outline">Favoritos</Button>
              <div className="absolute top-10 right-0 w-64 bg-white dark:bg-gray-800 dark:text-gray-100 border border-gray-100 dark:border-gray-700 rounded-lg shadow-xl opacity-0 invisible group-hover:opacity-100 group-hover:visible transition-all z-20">
                <div className="p-2 space-y-1">
                  <div className="text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase px-2 mb-2">Filtros Guardados</div>
                  {savedFilters.map(f => (
                    <div key={f.id} className="flex items-center justify-between hover:bg-gray-50 dark:bg-gray-800 dark:bg-gray-900 rounded-md p-1">
                      <button onClick={() => loadSavedFilter(f)} className="text-sm text-left px-2 py-1 flex-1 text-gray-700 dark:text-gray-200 truncate">
                        {f.name}
                      </button>
                      <button onClick={() => handleDeleteFilter(f.id)} className="p-1 hover:text-red-500 text-gray-400">
                        <Trash2 className="w-3.5 h-3.5" />
                      </button>
                    </div>
                  ))}
                </div>
              </div>
            </div>
          )}
        </div>
      </div>

      <AnimatePresence>
        {showFilters && (
          <motion.div 
            initial={{ height: 0, opacity: 0 }}
            animate={{ height: 'auto', opacity: 1 }}
            exit={{ height: 0, opacity: 0 }}
            className="overflow-hidden"
          >
            <div className="pt-4 border-t border-gray-100 dark:border-gray-700 grid grid-cols-1 sm:grid-cols-2 md:grid-cols-4 gap-4">
              <div>
                <label className="text-xs font-medium text-gray-500 dark:text-gray-400 uppercase mb-1 block">Responsables</label>
                <div className="flex flex-wrap gap-1">
                   {uniqueAssignees.map(a => {
                     const isActive = criteria.assignees.includes(a.id);
                     return (
                       <button 
                         key={a.id} 
                         onClick={() => updateCriteria({ 
                           assignees: isActive ? criteria.assignees.filter(id => id !== a.id) : [...criteria.assignees, a.id] 
                         })}
                         className={`text-sm px-2 py-1 rounded-md border ${isActive ? 'bg-blue-50 border-blue-200 text-blue-700' : 'bg-gray-50 dark:bg-gray-800 dark:bg-gray-900 border-gray-200 dark:border-gray-700 text-gray-600 dark:text-gray-300 hover:bg-gray-100 dark:bg-gray-700'}`}
                         title={a.name}
                       >
                         {a.name.slice(0, 15)}{a.name.length > 15 && '...'}
                       </button>
                     )
                   })}
                  {uniqueAssignees.length === 0 && <span className="text-sm text-gray-400">Sin responsables</span>}
                </div>
              </div>

              <div>
                <label className="text-xs font-medium text-gray-500 dark:text-gray-400 uppercase mb-1 block">Prioridad</label>
                <div className="flex flex-wrap gap-1">
                  {['BAJA', 'MEDIA', 'ALTA', 'URGENTE'].map(p => {
                    const isActive = criteria.priorities.includes(p);
                    return (
                      <button 
                        key={p} 
                        onClick={() => updateCriteria({ 
                          priorities: isActive ? criteria.priorities.filter(x => x !== p) : [...criteria.priorities, p] 
                        })}
                        className={`text-sm px-2 py-1 rounded-md border ${isActive ? 'bg-blue-50 border-blue-200 text-blue-700' : 'bg-gray-50 dark:bg-gray-800 dark:bg-gray-900 border-gray-200 dark:border-gray-700 text-gray-600 dark:text-gray-300 hover:bg-gray-100 dark:bg-gray-700'}`}
                      >
                        {p}
                      </button>
                    )
                  })}
                </div>
              </div>

              <div>
                <label className="text-xs font-medium text-gray-500 dark:text-gray-400 uppercase mb-1 block">Etiquetas</label>
                <div className="flex flex-wrap gap-1">
                  {uniqueLabels.map(l => {
                    const isActive = criteria.labels.includes(l.name);
                    return (
                      <button 
                        key={l.name} 
                        onClick={() => updateCriteria({ 
                          labels: isActive ? criteria.labels.filter(x => x !== l.name) : [...criteria.labels, l.name] 
                        })}
                        className={`text-xs px-2 py-1 rounded-full border ${isActive ? 'ring-2 ring-offset-1 ring-blue-400' : ''}`}
                        style={{ backgroundColor: l.color + '20', color: l.color, borderColor: l.color + '40' }}
                      >
                        {l.name}
                      </button>
                    )
                  })}
                  {uniqueLabels.length === 0 && <span className="text-sm text-gray-400">Sin etiquetas</span>}
                </div>
              </div>

              <div>
                <label className="text-xs font-medium text-gray-500 dark:text-gray-400 uppercase mb-1 block">Tipo</label>
                <div className="flex flex-wrap gap-1">
                  {['BUG', 'FEATURE', 'TASK', 'IMPROVEMENT'].map(t => {
                    const isActive = criteria.types.includes(t);
                    return (
                      <button 
                        key={t} 
                        onClick={() => updateCriteria({ 
                          types: isActive ? criteria.types.filter(x => x !== t) : [...criteria.types, t] 
                        })}
                        className={`text-sm px-2 py-1 rounded-md border ${isActive ? 'bg-blue-50 border-blue-200 text-blue-700' : 'bg-gray-50 dark:bg-gray-800 dark:bg-gray-900 border-gray-200 dark:border-gray-700 text-gray-600 dark:text-gray-300 hover:bg-gray-100 dark:bg-gray-700'}`}
                      >
                        {t}
                      </button>
                    )
                  })}
                </div>
              </div>
              
              <div>
                <label className="text-xs font-medium text-gray-500 dark:text-gray-400 uppercase mb-1 block">Fechas (Vencimiento)</label>
                <div className="flex flex-col gap-2 relative">
                  <div className="flex items-center gap-2">
                    <Input 
                      type="date" 
                      value={criteria.dateFrom} 
                      onChange={e => updateCriteria({ dateFrom: e.target.value })} 
                      className="text-sm h-8"
                    />
                    <span className="text-gray-400">-</span>
                    <Input 
                        type="date" 
                        value={criteria.dateTo} 
                        onChange={e => updateCriteria({ dateTo: e.target.value })} 
                        className="text-sm h-8"
                      />
                  </div>
                </div>
              </div>
            </div>
            
            <div className="pt-4 mt-4 border-t border-gray-100 dark:border-gray-700 flex items-center justify-between">
              <button 
                onClick={() => onFilterChange({ searchTerm: '', assignees: [], priorities: [], types: [], labels: [], dateFrom: '', dateTo: '' })}
                className="text-sm text-gray-500 dark:text-gray-400 hover:text-gray-800 dark:text-gray-100 underline"
              >
                Limpiar Filtros
              </button>
              
              {showSaveOverlay ? (
                <div className="flex items-center gap-2">
                  <Input 
                    value={filterName}
                    onChange={e => setFilterName(e.target.value)}
                    placeholder="Nombre del filtro..."
                    className="h-8 text-sm"
                  />
                  <Button variant="primary" onClick={handleSaveFilter} className="h-8 px-3">Guardar</Button>
                  <Button variant="outline" onClick={() => setShowSaveOverlay(false)} className="h-8 px-2"><X className="w-4 h-4" /></Button>
                </div>
              ) : (
                <Button variant="outline" onClick={() => setShowSaveOverlay(true)} disabled={activeFiltersCount === 0} className="h-8 text-sm">
                  <Save className="w-4 h-4 mr-2" />
                  Guardar Filtro
                </Button>
              )}
            </div>
          </motion.div>
        )}
      </AnimatePresence>
    </div>
  );
}
