import {
  ChangeDetectionStrategy,
  Component,
  HostListener,
  input,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { DxButtonModule } from 'devextreme-angular/ui/button';
import { DxDropDownButtonModule } from 'devextreme-angular/ui/drop-down-button';
import { DxTextBoxComponent, DxTextBoxModule } from 'devextreme-angular/ui/text-box';
import { DxToolbarModule } from 'devextreme-angular/ui/toolbar';
import type { ItemClickEvent } from 'devextreme/ui/drop_down_button';

export interface UserMenuItem {
  readonly id: string;
  readonly text: string;
  readonly icon: string;
}

/* Mutable array: DevExtreme's `items` binding rejects a readonly one. */
const USER_MENU: UserMenuItem[] = [
  { id: 'profile', text: 'My profile', icon: 'user' },
  { id: 'preferences', text: 'Preferences', icon: 'preferences' },
  { id: 'theme', text: 'Switch theme', icon: 'sun' },
  { id: 'signout', text: 'Sign out', icon: 'runner' },
];

/**
 * The application bar: drawer toggle, global search, notifications and the user
 * menu. It holds no state of its own beyond the search text — everything it
 * does is an output the shell decides what to do with.
 */
@Component({
  selector: 'ams-app-header',
  imports: [DxToolbarModule, DxButtonModule, DxTextBoxModule, DxDropDownButtonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './app-header.component.html',
  styleUrl: './app-header.component.scss',
})
export class AppHeaderComponent {
  readonly userName = input('Siddeswaran S');
  readonly unreadCount = input(0);

  readonly menuToggled = output<void>();
  readonly notificationsToggled = output<void>();
  readonly searched = output<string>();
  readonly userMenuSelected = output<UserMenuItem>();

  protected readonly userMenu = USER_MENU;
  protected readonly searchText = signal('');

  private readonly searchBox = viewChild<DxTextBoxComponent>('searchBox');

  /**
   * Ctrl+K / Cmd+K focuses search. A global search nobody can reach without the
   * mouse is a search for people who were not in a hurry.
   */
  @HostListener('document:keydown', ['$event'])
  protected onDocumentKeydown(event: KeyboardEvent): void {
    if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 'k') {
      event.preventDefault();
      this.searchBox()?.instance.focus();
    }
  }

  protected onSearchEnter(): void {
    const term = this.searchText().trim();
    if (term.length > 0) {
      this.searched.emit(term);
    }
  }

  protected onUserMenuClick(e: ItemClickEvent): void {
    this.userMenuSelected.emit(e.itemData as UserMenuItem);
  }
}
