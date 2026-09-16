import { Component, inject } from '@angular/core';
import { ThemeId, themeOptions } from '../../../core/theme/theme.models';
import { ThemeService } from '../../../core/theme/theme.service';
import { AppIcon } from '../app-icon/app-icon';

@Component({
  selector: 'fit-theme-selector',
  imports: [AppIcon],
  templateUrl: './theme-selector.html',
  styleUrl: './theme-selector.scss',
})
export class ThemeSelector {
  protected readonly themes = inject(ThemeService);
  protected readonly options = themeOptions;

  protected select(theme: ThemeId): void {
    this.themes.select(theme);
  }
}
