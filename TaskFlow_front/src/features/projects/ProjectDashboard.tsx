import { useMemo } from 'react';
import { 
  PieChart, Pie, Cell, ResponsiveContainer, Tooltip, Legend,
  BarChart, Bar, XAxis, YAxis, CartesianGrid, LineChart, Line
} from 'recharts';
import { Task, Project, BoardColumn } from '../../types/models';
import { Button } from '../../components/ui/Button';
import { FileText, Download, TrendingUp, AlertCircle, CheckCircle2, Clock } from 'lucide-react';
import { exportToCSV, exportToPDF } from '../../lib/exportUtils';

interface ProjectDashboardProps {
  project: Project;
  tasks: Task[];
  columns: BoardColumn[];
}

const COLORS = ['#3b82f6', '#10b981', '#f59e0b', '#ef4444', '#8b5cf6', '#6366f1'];

export function ProjectDashboard({ project, tasks, columns }: ProjectDashboardProps) {
  const stats = useMemo(() => {
    // 1. Tasks by Status
    const statusData = columns.map(col => ({
      name: col.name,
      value: tasks.filter(t => t.status === col.id).length
    })).filter(d => d.value > 0);

    // 2. Tasks by Priority
    const priorities = ['BAJA', 'MEDIA', 'ALTA', 'URGENTE'];
    const priorityData = priorities.map(p => ({
      name: p,
      value: tasks.filter(t => t.priority === p).length
    }));

    // 3. Tasks by Assignee
    const assigneeCounts: Record<string, number> = {};
    tasks.forEach(task => {
      task.assigneeIds?.forEach(id => {
        assigneeCounts[id] = (assigneeCounts[id] || 0) + 1;
      });
    });
    const assigneeData = Object.entries(assigneeCounts).map(([name, value]) => ({
      name: name.substring(0, 8), // Just a slice since ID is long
      value
    }));

    // 4. Overdue Tasks
    const now = new Date().setHours(0,0,0,0);
    const doneColIds = columns
      .filter(c => c.name.toLowerCase().includes('hecho') || c.name.toLowerCase().includes('completad') || c.name.toLowerCase().includes('done'))
      .map(c => c.id);
    
    const overdueTasks = tasks.filter(t => 
      t.dueDate && 
      new Date(t.dueDate).getTime() < now && 
      !doneColIds.includes(t.status)
    );

    // 4. Progress
    const totalTasks = tasks.length;
    const completedTasks = tasks.filter(t => doneColIds.includes(t.status)).length;
    const progress = totalTasks > 0 ? Math.round((completedTasks / totalTasks) * 100) : 0;

    // 5. Velocity (Completed tasks per week for the last 4 weeks)
    const weeksData: Record<string, number> = {};
    const today = new Date();
    
    for (let i = 3; i >= 0; i--) {
      const d = new Date();
      d.setDate(today.getDate() - (i * 7));
      const weekLabel = `Sem ${i === 0 ? 'Actual' : '-' + i}`;
      weeksData[weekLabel] = 0;
    }

    tasks.forEach(task => {
      if (doneColIds.includes(task.status)) {
        // Try to find when it was completed from history
        const completionEvent = task.history?.filter(h => h.type === 'STATUS_CHANGE' && doneColIds.includes(h.to)).sort((a,b) => new Date(b.timestamp).getTime() - new Date(a.timestamp).getTime())[0];
        const completionDate = completionEvent ? new Date(completionEvent.timestamp) : (task.updatedAt?.toDate ? task.updatedAt.toDate() : new Date(task.updatedAt));
        
        const diffDays = Math.floor((today.getTime() - completionDate.getTime()) / (1000 * 60 * 60 * 24));
        const weekIndex = Math.floor(diffDays / 7);
        
        if (weekIndex >= 0 && weekIndex <= 3) {
          const label = `Sem ${weekIndex === 0 ? 'Actual' : '-' + weekIndex}`;
          weeksData[label] = (weeksData[label] || 0) + 1;
        }
      }
    });

    const velocityData = Object.entries(weeksData).map(([name, value]) => ({ name, value })).reverse();

    return { statusData, priorityData, assigneeData, overdueCount: overdueTasks.length, progress, velocityData, completedTasks, totalTasks };
  }, [tasks, columns]);

  return (
    <div className="space-y-6 animate-in fade-in duration-500">
      {/* Overview Cards */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
        <div className="bg-white dark:bg-gray-800 dark:text-gray-100 p-5 rounded-xl shadow-sm border border-gray-100 dark:border-gray-700 flex items-center gap-4">
          <div className="p-3 bg-blue-50 rounded-lg text-blue-600">
            <TrendingUp className="w-6 h-6" />
          </div>
          <div>
            <p className="text-sm font-medium text-gray-500 dark:text-gray-400">Progreso General</p>
            <p className="text-2xl font-bold text-gray-900 dark:text-gray-50">{stats.progress}%</p>
          </div>
        </div>
        
        <div className="bg-white dark:bg-gray-800 dark:text-gray-100 p-5 rounded-xl shadow-sm border border-gray-100 dark:border-gray-700 flex items-center gap-4">
          <div className="p-3 bg-red-50 rounded-lg text-red-600">
            <AlertCircle className="w-6 h-6" />
          </div>
          <div>
            <p className="text-sm font-medium text-gray-500 dark:text-gray-400">Tareas Vencidas</p>
            <p className="text-2xl font-bold text-gray-900 dark:text-gray-50">{stats.overdueCount}</p>
          </div>
        </div>

        <div className="bg-white dark:bg-gray-800 dark:text-gray-100 p-5 rounded-xl shadow-sm border border-gray-100 dark:border-gray-700 flex items-center gap-4">
          <div className="p-3 bg-green-50 rounded-lg text-green-600">
            <CheckCircle2 className="w-6 h-6" />
          </div>
          <div>
            <p className="text-sm font-medium text-gray-500 dark:text-gray-400">Completadas</p>
            <p className="text-2xl font-bold text-gray-900 dark:text-gray-50">{stats.completedTasks} / {stats.totalTasks}</p>
          </div>
        </div>

        <div className="bg-white dark:bg-gray-800 dark:text-gray-100 p-5 rounded-xl shadow-sm border border-gray-100 dark:border-gray-700 flex items-center gap-4">
          <div className="p-3 bg-purple-50 rounded-lg text-purple-600">
            <Clock className="w-6 h-6" />
          </div>
          <div>
            <p className="text-sm font-medium text-gray-500 dark:text-gray-400">Total Tareas</p>
            <p className="text-2xl font-bold text-gray-900 dark:text-gray-50">{stats.totalTasks}</p>
          </div>
        </div>
      </div>

      <div className="flex justify-end gap-3">
        <Button variant="outline" size="sm" onClick={() => exportToCSV(tasks, columns)}>
          <Download className="w-4 h-4 mr-2" />
          Exportar CSV
        </Button>
        <Button variant="outline" size="sm" onClick={() => exportToPDF(project, tasks, columns)}>
          <FileText className="w-4 h-4 mr-2" />
          Exportar PDF
        </Button>
      </div>

      {/* Charts Grid */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Status Chart */}
        <div className="bg-white dark:bg-gray-800 dark:text-gray-100 p-6 rounded-xl shadow-sm border border-gray-100 dark:border-gray-700">
          <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-50 mb-6 font-sans">Distribución por Estado</h3>
          <div className="h-[300px]">
            <ResponsiveContainer width="100%" height="100%">
              <PieChart>
                <Pie
                  data={stats.statusData}
                  cx="50%"
                  cy="50%"
                  innerRadius={60}
                  outerRadius={80}
                  paddingAngle={5}
                  dataKey="value"
                >
                  {stats.statusData.map((entry, index) => (
                    <Cell key={`cell-${index}`} fill={COLORS[index % COLORS.length]} />
                  ))}
                </Pie>
                <Tooltip />
                <Legend />
              </PieChart>
            </ResponsiveContainer>
          </div>
        </div>

        {/* Priority Chart */}
        <div className="bg-white dark:bg-gray-800 dark:text-gray-100 p-6 rounded-xl shadow-sm border border-gray-100 dark:border-gray-700">
          <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-50 mb-6 font-sans">Tareas por Prioridad</h3>
          <div className="h-[300px]">
            <ResponsiveContainer width="100%" height="100%">
              <BarChart data={stats.priorityData}>
                <CartesianGrid strokeDasharray="3 3" vertical={false} />
                <XAxis dataKey="name" />
                <YAxis allowDecimals={false} />
                <Tooltip />
                <Bar dataKey="value" fill="#3b82f6" radius={[4, 4, 0, 0]} />
              </BarChart>
            </ResponsiveContainer>
          </div>
        </div>

        {/* Assignee Chart */}
        <div className="bg-white dark:bg-gray-800 dark:text-gray-100 p-6 rounded-xl shadow-sm border border-gray-100 dark:border-gray-700">
          <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-50 mb-6 font-sans">Tareas por Usuario</h3>
          <div className="h-[300px]">
            <ResponsiveContainer width="100%" height="100%">
              <BarChart data={stats.assigneeData} layout="vertical">
                <CartesianGrid strokeDasharray="3 3" horizontal={false} />
                <XAxis type="number" allowDecimals={false} />
                <YAxis dataKey="name" type="category" width={80} />
                <Tooltip />
                <Bar dataKey="value" fill="#10b981" radius={[0, 4, 4, 0]} />
              </BarChart>
            </ResponsiveContainer>
          </div>
        </div>

        {/* Velocity Chart */}
        <div className="bg-white dark:bg-gray-800 dark:text-gray-100 p-6 rounded-xl shadow-sm border border-gray-100 dark:border-gray-700">
          <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-50 mb-4 font-sans">Velocidad del Equipo</h3>
          <p className="text-sm text-gray-500 dark:text-gray-400 mb-6">Tareas completadas por semana</p>
          <div className="h-[300px]">
            <ResponsiveContainer width="100%" height="100%">
              <LineChart data={stats.velocityData}>
                <CartesianGrid strokeDasharray="3 3" vertical={false} />
                <XAxis dataKey="name" />
                <YAxis allowDecimals={false} />
                <Tooltip />
                <Line type="monotone" dataKey="value" stroke="#8b5cf6" strokeWidth={2} dot={{ r: 4 }} activeDot={{ r: 6 }} />
              </LineChart>
            </ResponsiveContainer>
          </div>
        </div>

        {/* Summary Card */}
        <div className="bg-white dark:bg-gray-800 dark:text-gray-100 p-6 rounded-xl shadow-sm border border-gray-100 dark:border-gray-700 flex flex-col justify-center text-center space-y-4">
          <div className="inline-flex items-center justify-center w-16 h-16 bg-blue-50 rounded-full mx-auto">
            <TrendingUp className="w-8 h-8 text-blue-600" />
          </div>
          <h3 className="text-xl font-bold text-gray-900 dark:text-gray-50 font-sans">Análisis de Progreso</h3>
          <p className="text-gray-500 dark:text-gray-400 max-w-sm mx-auto">
            El proyecto se encuentra al {stats.progress}% de su finalización. 
            {stats.overdueCount > 0 ? ` Hay ${stats.overdueCount} tareas pendientes que requieren atención inmediata.` : ' Todas las tareas están dentro de los plazos establecidos.'}
          </p>
          <div className="w-full bg-gray-100 dark:bg-gray-700 rounded-full h-2.5 max-w-xs mx-auto">
            <div className="bg-blue-600 h-2.5 rounded-full" style={{ width: `${stats.progress}%` }}></div>
          </div>
        </div>
      </div>
    </div>
  );
}
