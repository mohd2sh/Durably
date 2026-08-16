export function loadColumnWidths<T extends string>(
  key: string,
  defaults: Record<T, number>
): Record<T, number> {
  const result: Record<T, number> = { ...defaults };

  try {
    const raw = localStorage.getItem(key);
    if (!raw) {
      return result;
    }

    const stored = JSON.parse(raw) as Partial<Record<T, unknown>>;
    for (const column of Object.keys(defaults) as T[]) {
      const value = stored[column];
      if (typeof value === 'number' && Number.isFinite(value)) {
        result[column] = value;
      }
    }
  } catch {
    // Corrupt or inaccessible storage: fall back to defaults.
  }

  return result;
}

export function saveColumnWidths<T extends string>(key: string, widths: Record<T, number>): void {
  try {
    localStorage.setItem(key, JSON.stringify(widths));
  } catch {
    // Storage may be full or unavailable (e.g. private browsing): ignore.
  }
}
