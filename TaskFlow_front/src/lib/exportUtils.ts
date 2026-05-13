import { jsPDF } from 'jspdf';
import autoTable from 'jspdf-autotable';
import { Task, Project, BoardColumn } from '../types/models';
import { ProjectSummaryReport, CSVReportFormat, PDFReportFormat } from './designPatterns';

export const exportToCSV = (tasks: Task[], columns: BoardColumn[]) => {
  const rows = tasks.map(task => { // Format basic data for bridge
    const statusName = columns.find(c => c.id === task.status)?.name || task.status;
    return {
      ID: task.id,
      Titulo: task.title,
      Estado: statusName,
      Prioridad: task.priority,
      Tipo: task.type
    };
  });

  const report = new ProjectSummaryReport(new CSVReportFormat(), "Exportación CSV");
  report.save(rows, `reporte_tareas_${Date.now()}`);
};

export const exportToPDF = (project: Project, tasks: Task[], columns: BoardColumn[]) => {
  const rows = tasks.map(task => ({
    Titulo: task.title,
    Estado: columns.find(c => c.id === task.status)?.name || task.status,
    Prioridad: task.priority,
    Tipo: task.type,
    Vencimiento: task.dueDate || '-',
    'Horas estimadas': task.estimatedHours?.toString() || '0',
    'Horas trabajadas': task.loggedHours?.toString() || '0',
  }));

  const report = new ProjectSummaryReport(new PDFReportFormat(), project.name);
  report.generate(rows); // Prepara el contenido del reporte, aunque en este caso no lo usamos directamente

  // jsPDF renderiza el resultado
  const doc = new jsPDF();
  doc.setFontSize(20);
  doc.text(`Reporte: ${project.name}`, 14, 22);
  doc.setFontSize(10);
  doc.text(`Generado: ${new Date().toLocaleString()}`, 14, 30);

  const tableData = tasks.map(task => [
    task.title,
    columns.find(c => c.id === task.status)?.name || task.status,
    task.priority,
    task.type,
    task.dueDate || '-',
  ]);

  autoTable(doc, {
    startY: 40,
    head: [['Título', 'Estado', 'Prioridad', 'Tipo', 'Vencimiento']],
    body: tableData,
    theme: 'striped',
  });

  const slug = project.name.replace(/\s+/g, '_');
  doc.save(`reporte_${slug}_${Date.now()}.pdf`);
};
