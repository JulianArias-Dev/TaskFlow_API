/**
 * Abstract Factory Pattern
 * Proporciona una interfaz para crear familias de objetos relacionados 
 * o dependientes sin especificar sus clases concretas.
 */

export interface IThemeFactory {
  createButtonStyles(): string;
  createBackgroundStyles(): string;
  createTextStyles(): string;
  createCardStyles(): string;
}

export class LightThemeFactory implements IThemeFactory {
  createButtonStyles(): string {
    return 'bg-blue-600 text-white hover:bg-blue-700 transition-colors';
  }
  createBackgroundStyles(): string {
    return 'bg-gray-50';
  }
  createTextStyles(): string {
    return 'text-gray-900';
  }
  createCardStyles(): string {
    return 'bg-white border-gray-200 shadow-sm';
  }
}

export class DarkThemeFactory implements IThemeFactory {
  createButtonStyles(): string {
    return 'bg-blue-500 text-white hover:bg-blue-600 transition-colors';
  }
  createBackgroundStyles(): string {
    return 'bg-gray-900';
  }
  createTextStyles(): string {
    return 'text-gray-100';
  }
  createCardStyles(): string {
    return 'bg-gray-800 border-gray-700 shadow-md';
  }
}

// Selector simple
export class ThemeProvider {
  static getFactory(theme: 'light' | 'dark'): IThemeFactory {
    if (theme === 'dark') {
      return new DarkThemeFactory();
    }
    return new LightThemeFactory();
  }
}
