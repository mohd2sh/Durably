declare global {
  interface Window {
    __DURABLY_API__?: string;
  }
}

function resolveApiBase(): string {
  const configured = window.__DURABLY_API__;
  if (configured && !configured.includes('#apiPath#')) {
    return configured.replace(/\/$/, '');
  }

  const baseHref = document.querySelector('base')?.getAttribute('href') ?? '/durable/';
  const normalized = baseHref.replace(/\/$/, '').replace('#uiPath#', '/durable');
  return `${normalized}/api`;
}

export const API_BASE_PATH = resolveApiBase();

export const EXECUTIONS_API_PATH = `${API_BASE_PATH}/executions`;

export const DEFAULT_PAGE_SIZE = 50;
