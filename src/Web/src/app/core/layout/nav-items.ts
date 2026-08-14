/**
 * The navigation tree. Groups are presentation only — the leaves are the module
 * folders under `app/modules/`, named the same as the API modules (docs/04 §1).
 *
 * `capability` is the capability the route will require once identity is wired
 * (§2: never a role name). It is declared here so adding a screen means adding
 * its capability, not remembering to.
 */
export interface NavItem {
  readonly id: string;
  readonly text: string;
  readonly icon?: string;
  readonly path?: string;
  readonly capability?: string;
  readonly expanded?: boolean;
  /* Mutable array: DevExtreme's `items` binding takes `any[]` and rejects a
     readonly one. The contents are still treated as constant. */
  readonly items?: NavItem[];
}

/**
 * What the tree actually binds to. `id` is unique per row because a pinned leaf
 * appears twice — once under Pinned, once in its group — and a tree with two
 * rows sharing a key selects and expands the wrong one. `key` is the logical
 * item, which is what a pin and a badge are keyed by.
 */
export interface NavNode {
  id: string;
  key: string;
  text: string;
  icon?: string;
  path?: string;
  badge?: number;
  pinned?: boolean;
  pinnable?: boolean;
  expanded?: boolean;
  selected?: boolean;
  items?: NavNode[];
}

/** Counts shown as badges, keyed by `NavItem.id`. */
export type NavBadges = Readonly<Record<string, number>>;

export const NAV_ITEMS: NavItem[] = [
  { id: 'dashboard', text: 'Dashboard', icon: 'home', path: '/' },
  {
    id: 'field-assets',
    text: 'Field Assets',
    icon: 'map',
    path: '/field-assets',
    capability: 'field-asset.view',
  },
  { id: 'auditors', text: 'Auditors', icon: 'group', path: '/auditors' },
  { id: 'reports', text: 'Reports', icon: 'chart', path: '/reports' },
  { id: 'audit-reports', text: 'Audit Reports', icon: 'doc', path: '/audit-reports' },
];

/** The groups open on a first visit, before the user has expanded anything. */
export const DEFAULT_EXPANDED: readonly string[] = NAV_ITEMS.filter((i) => i.expanded).map(
  (i) => i.id,
);

/** The group a route lives in, so opening a screen opens its section. */
export function groupIdForPath(path: string): string | undefined {
  return NAV_ITEMS.find((group) => group.items?.some((child) => child.path === path))?.id;
}

export interface NavTreeState {
  readonly pinned: ReadonlySet<string>;
  readonly badges: NavBadges;
  /** Which groups are open. Held by the caller so a rebuild does not shut them. */
  readonly expanded: ReadonlySet<string>;
  /** The route showing now — what "selected" means. */
  readonly activePath: string;
}

/**
 * Decorates the static tree with the pin, badge, expansion and selection state.
 *
 * A group carries the SUM of its children's badges. Without that, collapsing a
 * group hides the fact that anything is waiting in it — which is exactly when
 * the count matters most.
 *
 * Expansion and selection are passed IN rather than left to the tree widget.
 * This function returns a brand new array every time a pin or a badge changes,
 * and DevExtreme rebuilds the tree from it — so any state the widget was
 * holding for itself is discarded on the next count update.
 */
export function buildNavTree(state: NavTreeState): NavNode[] {
  const { pinned, badges, expanded, activePath } = state;

  const decorate = (item: NavItem): NavNode => {
    const items = item.items?.map(decorate);
    const own = badges[item.id] ?? 0;
    const fromChildren = items?.reduce((sum, child) => sum + (child.badge ?? 0), 0) ?? 0;
    const badge = own + fromChildren;

    return {
      id: item.id,
      key: item.id,
      text: item.text,
      icon: item.icon,
      path: item.path,
      expanded: expanded.has(item.id),
      selected: item.path !== undefined && item.path === activePath,
      badge: badge > 0 ? badge : undefined,
      pinned: pinned.has(item.id),
      pinnable: item.path !== undefined,
      items,
    };
  };

  const tree = NAV_ITEMS.map(decorate);

  const leaves: NavNode[] = [];
  const collect = (nodes: NavNode[]): void => {
    for (const node of nodes) {
      if (node.items) {
        collect(node.items);
      } else if (node.pinnable) {
        leaves.push(node);
      }
    }
  };
  collect(tree);

  const pins = leaves
    .filter((leaf) => leaf.pinned)
    // `id` is prefixed so the pinned copy and the original can coexist.
    .map((leaf) => ({ ...leaf, id: `pinned:${leaf.key}` }));

  if (pins.length === 0) {
    return tree;
  }

  return [
    {
      id: 'pinned-group',
      key: 'pinned-group',
      text: 'Pinned',
      icon: 'pin',
      expanded: true,
      items: pins,
    },
    ...tree,
  ];
}
