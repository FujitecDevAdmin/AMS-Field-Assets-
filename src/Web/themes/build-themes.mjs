// Compiles the Fujitec DevExtreme theme bundles and rewrites the compiled CSS
// from px to rem, so a single root-font-size lever rescales DevExtreme control
// internals — grid rows, inputs, buttons — along with the rest of the app,
// not just the chrome around them.
//
// Two steps, kept separate on purpose:
//   1. The `sass` compile, via its JS API (equivalent to the CLI flags
//      `--load-path=node_modules --no-source-map --style=compressed`). The JS
//      API is used instead of shelling to the CLI binary so the script needs no
//      platform-specific spawn handling — the `.cmd` shim npm installs on
//      Windows cannot be exec'd directly without a shell.
//   2. A PostCSS pass (`postcss-pxtorem`) over each compiled CSS file. This
//      runs on the OUTPUT only — the theme SCSS sources and their `@use` lists
//      are never touched, which is the invariant the bundle comments call out
//      as load-bearing (a missing `@use` entry silently drops those
//      components' styles).
//
// `rootValue: 16` means the transform is scale-1-identity: at the default 16px
// root font-size every converted `Nrem` computes back to exactly the original
// `Npx`, so this step is visually inert until something changes the root away
// from 16px. `minPixelValue: 2` leaves 1px hairline borders as crisp px rather
// than a fractional rem that could round to a blurry sub-pixel line.
// `mediaQuery: false` leaves px inside `@media` conditions alone — those are
// viewport breakpoints, not sizes, and must not scale with density.

import { copyFileSync, mkdirSync, writeFileSync } from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

import postcss from 'postcss';
import pxtorem from 'postcss-pxtorem';
import * as sass from 'sass';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const webRoot = path.resolve(__dirname, '..');

const bundles = [
  {
    src: 'themes/dx.material.fujitec.light.scss',
    out: 'public/assets/themes/dx.material.fujitec.light.css',
  },
  {
    src: 'themes/dx.material.fujitec.dark.scss',
    out: 'public/assets/themes/dx.material.fujitec.dark.css',
  },
];

const pxToRem = postcss([
  pxtorem({
    rootValue: 16,
    propList: ['*'],
    minPixelValue: 2,
    mediaQuery: false,
  }),
]);

for (const { src, out } of bundles) {
  const srcPath = path.join(webRoot, src);
  const outPath = path.join(webRoot, out);
  mkdirSync(path.dirname(outPath), { recursive: true });

  const compiled = sass.compile(srcPath, {
    loadPaths: [path.join(webRoot, 'node_modules')],
    style: 'compressed',
    sourceMap: false,
  });

  const result = await pxToRem.process(compiled.css, { from: srcPath, to: outPath });
  writeFileSync(outPath, result.css, 'utf8');
}

// The compiled CSS references the icon font at icons/dxiconsmaterial.* relative
// to itself; copy it next to the bundles so the glyphs resolve.
const iconsSrc = path.join(webRoot, 'node_modules/devextreme/dist/css/icons');
const iconsOut = path.join(webRoot, 'public/assets/themes/icons');
mkdirSync(iconsOut, { recursive: true });
for (const font of ['dxiconsmaterial.woff2', 'dxiconsmaterial.woff', 'dxiconsmaterial.ttf']) {
  copyFileSync(path.join(iconsSrc, font), path.join(iconsOut, font));
}
