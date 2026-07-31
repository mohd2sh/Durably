import { Component, Input, OnChanges, inject } from '@angular/core';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { MatButtonModule } from '@angular/material/button';
import { MatTooltipModule } from '@angular/material/tooltip';

type JsonToken =
  | { kind: 'plain'; text: string }
  | { kind: 'key' | 'string' | 'number' | 'boolean' | 'null'; text: string };

@Component({
  selector: 'app-json-viewer',
  standalone: true,
  imports: [MatButtonModule, MatTooltipModule],
  templateUrl: './json-viewer.component.html',
  styleUrl: './json-viewer.component.scss'
})
export class JsonViewerComponent implements OnChanges {
  private readonly sanitizer = inject(DomSanitizer);

  @Input() value?: string | null;
  @Input() emptyLabel = 'No data';
  @Input() maxHeight = '420px';

  formatted = '';
  highlightedHtml: SafeHtml = '';
  copyLabel = 'Copy';

  ngOnChanges(): void {
    this.formatted = this.formatJson(this.value);
    this.highlightedHtml = this.sanitizer.bypassSecurityTrustHtml(
      this.toHighlightedHtml(this.tokenize(this.formatted))
    );
  }

  async copy(): Promise<void> {
    if (!this.formatted) {
      return;
    }

    try {
      await navigator.clipboard.writeText(this.formatted);
      this.copyLabel = 'Copied';
      setTimeout(() => (this.copyLabel = 'Copy'), 1200);
    } catch {
      this.copyLabel = 'Failed';
      setTimeout(() => (this.copyLabel = 'Copy'), 1200);
    }
  }

  private formatJson(value?: string | null): string {
    if (!value) {
      return '';
    }

    try {
      return JSON.stringify(JSON.parse(value), null, 2);
    } catch {
      return value;
    }
  }

  private tokenize(json: string): JsonToken[] {
    if (!json) {
      return [];
    }

    // Match "key": as a single key token including the colon (no separate ":" token).
    const pattern =
      /("(?:\\.|[^"\\])*")\s*:|("(?:\\.|[^"\\])*")|(-?\d+(?:\.\d+)?(?:[eE][+-]?\d+)?)|\b(true|false)\b|\b(null)\b/g;
    const tokens: JsonToken[] = [];
    let lastIndex = 0;
    let match: RegExpExecArray | null;

    while ((match = pattern.exec(json)) !== null) {
      if (match.index > lastIndex) {
        tokens.push({ kind: 'plain', text: json.slice(lastIndex, match.index) });
      }

      if (match[1] !== undefined) {
        tokens.push({ kind: 'key', text: `${match[1]}:` });
      } else if (match[2] !== undefined) {
        tokens.push({ kind: 'string', text: match[2] });
      } else if (match[3] !== undefined) {
        tokens.push({ kind: 'number', text: match[3] });
      } else if (match[4] !== undefined) {
        tokens.push({ kind: 'boolean', text: match[4] });
      } else if (match[5] !== undefined) {
        tokens.push({ kind: 'null', text: match[5] });
      }

      lastIndex = pattern.lastIndex;
    }

    if (lastIndex < json.length) {
      tokens.push({ kind: 'plain', text: json.slice(lastIndex) });
    }

    return tokens;
  }

  private toHighlightedHtml(tokens: JsonToken[]): string {
    return tokens
      .map(token => {
        const text = this.escapeHtml(token.text);
        if (token.kind === 'plain') {
          return text;
        }
        return `<span class="tok-${token.kind}">${text}</span>`;
      })
      .join('');
  }

  private escapeHtml(value: string): string {
    return value
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;')
      .replace(/'/g, '&#39;');
  }
}
