/**
 * Bridge Pattern
 * Desacopla una abstracción de su implementación,
 * permitiendo que ambas varíen independientemente.
 */

// Implementador (La implementación técnica de los formatos)
export interface IReportFormat {
  formatHeader(title: string): string;
  formatBody(data: any[]): string;
  formatFooter(): string;
  getFileExtension(): string;
}

export class PDFReportFormat implements IReportFormat {
  formatHeader(title: string) { return `[ENCABEZADO PDF] Título: ${title}\n======================\n`; }
  formatBody(data: any[]) { return `  -> [CUERPO PDF] Mostrando ${data.length} elementos renderizados gráficamente.\n`; }
  formatFooter() { return `======================\n[PIE DE PDF] Fin del documento.`; }
  getFileExtension() { return '.pdf'; }
}

export class CSVReportFormat implements IReportFormat {
  formatHeader(title: string) { return `"Titulo","${title}"\n`; }
  formatBody(data: any[]) { 
    return data.map(row => Object.values(row).map(v => `"${v}"`).join(',')).join('\n') + '\n';
  }
  formatFooter() { return `"Fin del reporte"\n`; }
  getFileExtension() { return '.csv'; }
}

export class HTMLReportFormat implements IReportFormat {
  formatHeader(title: string) { return `<h1>${title}</h1>\n<ul>\n`; }
  formatBody(data: any[]) { 
    return data.map(item => `  <li>${JSON.stringify(item)}</li>`).join('\n') + '\n'; 
  }
  formatFooter() { return `</ul>`; }
  getFileExtension() { return '.html'; }
}


// Abstracción (La interfaz de alto nivel que usa el cliente)
export abstract class Report {
  // El bridge (puente) hacia la implementación
  constructor(protected format: IReportFormat) {}

  abstract generate(data: any[]): string;
  
  save(data: any[], filename: string) {
    const content = this.generate(data);
    console.log(`Guardando archivo: ${filename}${this.format.getFileExtension()}`);
    
    const blob = new Blob([content], { type: this.format.getFileExtension() === '.csv' ? 'text/csv;charset=utf-8;' : 'text/plain;charset=utf-8;' });
    const link = document.createElement('a');
    const url = URL.createObjectURL(blob);
    link.setAttribute('href', url);
    link.setAttribute('download', `${filename}${this.format.getFileExtension()}`);
    link.style.visibility = 'hidden';
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    
    return content;
  }
}

export class ProjectSummaryReport extends Report {
  constructor(format: IReportFormat, private projectName: string) {
    super(format);
  }
  
  generate(data: any[]): string {
    let report = this.format.formatHeader(`Resumen de Proyecto: ${this.projectName}`);
    // Lógica específica de cómo este reporte pre-procesa o formatea sus datos
    report += this.format.formatBody(data);
    report += this.format.formatFooter();
    return report;
  }
}

export class UserPerformanceReport extends Report {
  constructor(format: IReportFormat, private username: string) {
    super(format);
  }

  generate(data: any[]): string {
    let report = this.format.formatHeader(`Rendimiento de Usuario: ${this.username}`);
    // Procesamiento específico de rendimiento (estadísticas, agrupaciones)
    const aggregatedData = [{ totalCompleted: data.length, score: 95 }]; // Mock
    report += this.format.formatBody(aggregatedData);
    report += this.format.formatFooter();
    return report;
  }
}
