import { AfterViewInit, Directive, ElementRef, OnDestroy, inject, input } from '@angular/core';

@Directive({ selector: '[appLandingReveal]' })
export class LandingRevealDirective implements AfterViewInit, OnDestroy {
  readonly revealDelay = input(0);

  private readonly element = inject<ElementRef<HTMLElement>>(ElementRef).nativeElement;
  private observer?: IntersectionObserver;
  private animation?: Animation;

  ngAfterViewInit(): void {
    const view = this.element.ownerDocument.defaultView;
    const reducedMotion = view?.matchMedia?.('(prefers-reduced-motion: reduce)').matches ?? false;
    if (reducedMotion || !view?.IntersectionObserver || typeof this.element.animate !== 'function')
      return;

    this.animation = this.element.animate(
      [
        { opacity: 0, transform: 'translateY(1.5rem)' },
        { opacity: 1, transform: 'translateY(0)' },
      ],
      {
        delay: this.revealDelay(),
        duration: 620,
        easing: 'cubic-bezier(.2,.72,.2,1)',
        fill: 'both',
      },
    );
    this.animation.pause();

    this.observer = new view.IntersectionObserver(
      (entries) => {
        if (!entries.some((entry) => entry.isIntersecting)) return;
        this.animation?.play();
        this.observer?.disconnect();
        this.observer = undefined;
      },
      { rootMargin: '0px 0px -8% 0px', threshold: 0.08 },
    );
    this.observer.observe(this.element);
  }

  ngOnDestroy(): void {
    this.observer?.disconnect();
    this.animation?.cancel();
  }
}
