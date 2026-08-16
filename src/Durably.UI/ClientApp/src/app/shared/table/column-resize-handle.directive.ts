import { Directive, ElementRef, EventEmitter, HostListener, Input, Output, inject } from '@angular/core';

const RESIZING_BODY_CLASS = 'is-col-resizing';

@Directive({
  selector: '[appColumnResizeHandle]',
  standalone: true,
  host: {
    class: 'column-resize-handle'
  }
})
export class ColumnResizeHandleDirective {
  private readonly elementRef = inject(ElementRef<HTMLElement>);

  @Input() width = 0;
  @Output() widthChange = new EventEmitter<number>();

  @Input() min = 96;
  @Input() max = 1200;

  @Output() resizeEnd = new EventEmitter<number>();

  private startX = 0;
  private startWidth = 0;
  private activePointerId: number | null = null;

  @HostListener('pointerdown', ['$event'])
  onPointerDown(event: PointerEvent): void {
    event.preventDefault();
    event.stopPropagation();

    this.startX = event.clientX;
    this.startWidth = this.width;
    this.activePointerId = event.pointerId;

    this.elementRef.nativeElement.setPointerCapture(event.pointerId);
    document.body.classList.add(RESIZING_BODY_CLASS);
  }

  @HostListener('pointermove', ['$event'])
  onPointerMove(event: PointerEvent): void {
    if (this.activePointerId === null) {
      return;
    }

    const delta = event.clientX - this.startX;
    const nextWidth = Math.min(this.max, Math.max(this.min, this.startWidth + delta));

    if (nextWidth !== this.width) {
      this.width = nextWidth;
      this.widthChange.emit(nextWidth);
    }
  }

  @HostListener('pointerup')
  @HostListener('pointercancel')
  onPointerUp(): void {
    if (this.activePointerId === null) {
      return;
    }

    try {
      this.elementRef.nativeElement.releasePointerCapture(this.activePointerId);
    } catch {
      // Capture may already be released.
    }

    this.activePointerId = null;
    document.body.classList.remove(RESIZING_BODY_CLASS);
    this.resizeEnd.emit(this.width);
  }
}
