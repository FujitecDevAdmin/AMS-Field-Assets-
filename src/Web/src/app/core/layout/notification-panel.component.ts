import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { DxButtonModule } from 'devextreme-angular/ui/button';
import { DxListModule } from 'devextreme-angular/ui/list';

export interface AppNotification {
  readonly id: string;
  readonly title: string;
  readonly detail: string;
  readonly when: string;
  readonly kind: 'info' | 'warning' | 'success';
  readonly read: boolean;
}

/**
 * The notification panel. Presentation only — it renders what it is given and
 * emits what the user did, so the list can come from the notifications module's
 * endpoint later without this component changing.
 */
@Component({
  selector: 'ams-notification-panel',
  imports: [DxListModule, DxButtonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './notification-panel.component.html',
  styleUrl: './notification-panel.component.scss',
})
export class NotificationPanelComponent {
  /* Mutable array: DevExtreme's `dataSource` binding rejects a readonly one. */
  readonly notifications = input<AppNotification[]>([]);

  readonly closed = output<void>();
  readonly allRead = output<void>();

  protected iconFor(kind: AppNotification['kind']): string {
    switch (kind) {
      case 'warning':
        return 'warning';
      case 'success':
        return 'check';
      default:
        return 'info';
    }
  }
}
