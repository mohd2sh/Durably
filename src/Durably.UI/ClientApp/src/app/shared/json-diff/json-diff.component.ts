import { Component, Input, OnChanges } from '@angular/core';
import { diffLines, type Change } from 'diff';

export interface DiffLineView {
  type: 'added' | 'removed' | 'context';
  text: string;
  oldLine?: number;
  newLine?: number;
}

@Component({
  selector: 'app-json-diff',
  standalone: true,
  templateUrl: './json-diff.component.html',
  styleUrl: './json-diff.component.scss'
})
export class JsonDiffComponent implements OnChanges {
  @Input() before?: string | null;
  @Input() after?: string | null;
  @Input() maxHeight = '480px';

  lines: DiffLineView[] = [];
  hasChanges = false;
  isEmpty = true;

  ngOnChanges(): void {
    const left = this.pretty(this.before);
    const right = this.pretty(this.after);
    this.isEmpty = !left && !right;

    if (this.isEmpty) {
      this.lines = [];
      this.hasChanges = false;
      return;
    }

    this.lines = this.buildUnifiedLines(left, right);
    this.hasChanges = this.lines.some(line => line.type !== 'context');
  }

  private pretty(value?: string | null): string {
    if (!value) {
      return '';
    }

    try {
      return JSON.stringify(JSON.parse(value), null, 2);
    } catch {
      return value;
    }
  }

  private buildUnifiedLines(before: string, after: string): DiffLineView[] {
    const changes: Change[] = diffLines(before, after);
    const result: DiffLineView[] = [];
    let oldLine = 1;
    let newLine = 1;

    for (const change of changes) {
      const chunks = change.value.replace(/\n$/, '').split('\n');
      // diffLines keeps trailing newline semantics; empty split when value is just "\n"
      const rows = change.value === '' ? [] : chunks;

      for (const row of rows) {
        if (change.added) {
          result.push({ type: 'added', text: row, newLine: newLine++ });
        } else if (change.removed) {
          result.push({ type: 'removed', text: row, oldLine: oldLine++ });
        } else {
          result.push({
            type: 'context',
            text: row,
            oldLine: oldLine++,
            newLine: newLine++
          });
        }
      }
    }

    return result;
  }
}
