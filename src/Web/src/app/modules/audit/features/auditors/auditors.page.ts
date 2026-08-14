import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { DxButtonModule } from 'devextreme-angular/ui/button';

@Component({
  selector: 'ams-auditors-page',
  imports: [DxButtonModule],
  templateUrl: './auditors.page.html',
  styleUrl: './auditors.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AuditorsPage {
  readonly isAddPanelVisible = signal(false);

  showAddAuditor(): void {
    this.isAddPanelVisible.set(true);
  }
}
