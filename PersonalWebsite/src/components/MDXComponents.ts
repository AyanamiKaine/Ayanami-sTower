/**
 * Global MDX Components
 * 
 * Components exported here are automatically available in all MDX files
 * without needing to import them.
 */

import Aside from './Aside.astro';
import SourceReference from './SourceReference.svelte';
import SourceQuote from './SourceQuote.svelte';

export const components = {
  Aside,
  SourceReference,
  SourceQuote,
};
