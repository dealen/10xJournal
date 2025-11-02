// Production-grade service worker for 10xJournal PWA
// Uses stale-while-revalidate strategy for optimal performance
const CACHE_VERSION = '1.0.2';
const CACHE_NAME = `10xjournal-v${CACHE_VERSION}`;
const RUNTIME_CACHE = `10xjournal-runtime-v${CACHE_VERSION}`;

// Critical assets to pre-cache during installation
const PRECACHE_URLS = [
  '/',
  '/index.html',
  '/manifest.json',
  '/favicon.png',
  '/icon-192.png',
  '/icon-512.png',
  '/css/app.css'
];

// Assets that should NOT be cached (API calls, authentication, configuration)
const CACHE_BLACKLIST = [
  /supabase\.co/,
  /auth\//,
  /realtime\//,
  /storage\//,
  /appsettings.*\.json$/  // CRITICAL: Never cache configuration files - they change between environments
];

/**
 * Installation event - pre-cache critical assets
 */
self.addEventListener('install', event => {
  console.log('[ServiceWorker] Installing version:', CACHE_VERSION);
  
  event.waitUntil(
    caches.open(CACHE_NAME)
      .then(cache => {
        console.log('[ServiceWorker] Pre-caching app shell');
        return cache.addAll(PRECACHE_URLS);
      })
      .then(() => {
        // Force the waiting service worker to become the active service worker
        return self.skipWaiting();
      })
      .catch(err => {
        console.error('[ServiceWorker] Pre-cache failed:', err);
      })
  );
});

/**
 * Activation event - clean up old caches
 */
self.addEventListener('activate', event => {
  console.log('[ServiceWorker] Activating version:', CACHE_VERSION);
  
  event.waitUntil(
    caches.keys().then(cacheNames => {
      return Promise.all(
        cacheNames
          .filter(cacheName => {
            // Delete caches that don't match current version
            return cacheName.startsWith('10xjournal-') && cacheName !== CACHE_NAME && cacheName !== RUNTIME_CACHE;
          })
          .map(cacheName => {
            console.log('[ServiceWorker] Deleting old cache:', cacheName);
            return caches.delete(cacheName);
          })
      );
    })
    .then(() => {
      // Take control of all pages immediately
      return self.clients.claim();
    })
  );
});

/**
 * Check if a request should be cached
 */
function shouldCache(request) {
  // Only cache GET requests
  if (request.method !== 'GET') {
    return false;
  }
  
  const url = new URL(request.url);
  
  // Only cache HTTP and HTTPS requests (exclude chrome-extension://, about:, data:, etc.)
  if (url.protocol !== 'http:' && url.protocol !== 'https:') {
    return false;
  }
  
  // Don't cache blacklisted URLs
  for (const pattern of CACHE_BLACKLIST) {
    if (pattern.test(url.href)) {
      return false;
    }
  }
  
  return true;
}

/**
 * Stale-While-Revalidate fetch strategy
 * Returns cached content immediately while fetching fresh content in background
 */
self.addEventListener('fetch', event => {
  // Skip non-GET requests and blacklisted URLs
  if (!shouldCache(event.request)) {
    return;
  }
  
  event.respondWith(
    caches.open(RUNTIME_CACHE).then(cache => {
      return cache.match(event.request).then(cachedResponse => {
        // Fetch from network and update cache in background
        const fetchPromise = fetch(event.request)
          .then(networkResponse => {
            // Only cache valid responses
            if (networkResponse && networkResponse.status === 200 && networkResponse.type === 'basic') {
              cache.put(event.request, networkResponse.clone());
            }
            return networkResponse;
          })
          .catch(error => {
            console.error('[ServiceWorker] Fetch failed:', error);
            
            // If we have a cached response, return it as fallback
            if (cachedResponse) {
              return cachedResponse;
            }
            
            // Return a custom offline response
            return new Response(
              JSON.stringify({
                error: 'Offline',
                message: 'You are currently offline. Please check your connection.'
              }),
              {
                status: 503,
                statusText: 'Service Unavailable',
                headers: { 'Content-Type': 'application/json' }
              }
            );
          });
        
        // Return cached response immediately, or wait for network
        return cachedResponse || fetchPromise;
      });
    })
  );
});

/**
 * Message event handler for cache management
 */
self.addEventListener('message', event => {
  if (event.data && event.data.type === 'SKIP_WAITING') {
    self.skipWaiting();
  }
  
  if (event.data && event.data.type === 'CLEAR_CACHE') {
    event.waitUntil(
      caches.keys().then(cacheNames => {
        return Promise.all(
          cacheNames.map(cacheName => caches.delete(cacheName))
        );
      })
    );
  }
});
