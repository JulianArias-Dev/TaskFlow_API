import { jsPDF } from 'jspdf';
import 'jspdf-autotable';
import { Task, Project, BoardColumn } from '../types/models';
import { ProjectSummaryReport, CSVReportFormat } from './designPatterns';

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
  const doc = new jsPDF() as any;

  // Header
  doc.setFontSize(20);
  doc.text(`Reporte de Proyecto: ${project.name}`, 14, 22);
  
  doc.setFontSize(12);
  doc.setTextColor(100);
  doc.text(`Estado: ${project.status}`, 14, 30);
  doc.text(`Generado el: ${new Date().toLocaleString()}`, 14, 37);

  // Stats
  const statusCounts = tasks.reduce((acc, task) => {
    const statusName = columns.find(c => c.id === task.status)?.name || task.status;
    acc[statusName] = (acc[statusName] || 0) + 1;
    return acc;
  }, {} as Record<string, number>);

  let yPos = 50;
  doc.setFontSize(14);
  doc.setTextColor(0);
  doc.text('Resumen de Estados:', 14, yPos);
  yPos += 10;
  
  doc.setFontSize(11);
  Object.entries(statusCounts).forEach(([status, count]) => {
    doc.text(`- ${status}: ${count} tareas`, 20, yPos);
    yPos += 7;
  });

  // Table
  const tableHeaders = [['Título', 'Estado', 'Prioridad', 'Vencimiento']];
  const tableData = tasks.map(task => [
    task.title,
    columns.find(c => c.id === task.status)?.name || task.status,
    task.priority,
    task.dueDate || '-'
  ]);

  doc.autoTable({
    startY: yPos + 10,
    head: tableHeaders,
    body: tableData,
    theme: 'striped',
    headStyles: { fillGray: [41, 128, 185] },
  });

  const slug = project.name.toLowerCase().replace(/[^a-z0-9]+/g, '_').replace(/^_+|_+$/g, '') || 'proyecto';
  doc.save(`reporte_${slug}_${Date.now()}.pdf`);
};
