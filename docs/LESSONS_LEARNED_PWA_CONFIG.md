# Lessons Learned: PWA Configuration & Service Worker Caching

## Critical Issue: Service Worker Caching Configuration Files

### Problem
Production app was connecting to `http://127.0.0.1:54321` instead of production Supabase URL, despite GitHub Actions correctly injecting production secrets during deployment.

### Root Cause
**Service Worker cached `appsettings.json`**, serving stale localhost configuration instead of fresh production config deployed by CI/CD.

### Key Learnings

#### 1. **Never Cache Configuration Files**
Configuration files that change between environments (dev/prod) must NEVER be cached:
```javascript
// Separate domain-based and path-based patterns for correct matching
const CACHE_BLACKLIST = {
  // Domain-based patterns (tested against full URL)
  domains: [
    /supabase\.co/
  ],
  // Path-based patterns (tested against pathname only)
  paths: [
    /appsettings.*\.json$/  // Block ALL environment configs
  ]
};

// In shouldCache function:
// Test domain patterns against url.href
for (const pattern of CACHE_BLACKLIST.domains) {
  if (pattern.test(url.href)) return false;
}

// Test path patterns against url.pathname (CRITICAL for $ anchor to work)
for (const pattern of CACHE_BLACKLIST.paths) {
  if (pattern.test(url.pathname)) return false;
}
```

**Critical Lesson:** Regex patterns with `$` anchor must test against `url.pathname`, not `url.href`, because `url.href` includes protocol and domain (e.g., `https://example.com/appsettings.json`), causing the `$` anchor to fail.

#### 2. **Service Worker Cache Persists Across Deployments**
- Service worker cache survives deployments
- Cached files are served even when fresh versions exist on server
- Cache invalidation requires version bump or explicit cache clearing

#### 3. **Dual Defense Strategy**
Prevent configuration caching at TWO levels:

**Level 1: Service Worker**
```javascript
// service-worker.js
const CACHE_BLACKLIST = {
  domains: [/supabase\.co/],
  paths: [/appsettings.*\.json$/]  // Tested against url.pathname
};
```

**Level 2: HTTP Headers**
```json
// staticwebapp.config.json
{
  "route": "/appsettings*.json",
  "headers": {
    "cache-control": "no-cache, no-store, must-revalidate, max-age=0"
  }
}
```

#### 4. **Environment-Specific Secrets in CI/CD**
GitHub Actions correctly overwrites `appsettings.json` during build:
```yaml
- name: 🔧 Update appsettings for production
  run: |
    cat > ./publish/wwwroot/appsettings.json <<EOF
    {
      "Supabase": {
        "Url": "${{ secrets.PROD_SUPABASE_URL }}",
        "AnonKey": "${{ secrets.PROD_SUPABASE_ANON_KEY }}"
      }
    }
    EOF
```

**But** this is useless if service worker serves cached version!

#### 5. **Debugging PWA Configuration Issues**

**Check what config production is using:**
```javascript
// Browser console on deployed app
fetch('/appsettings.json')
  .then(r => r.json())
  .then(config => console.log('Config:', config));
```

**Check service worker cache:**
```javascript
// Browser console
caches.keys().then(keys => console.log('Cache keys:', keys));
caches.open('10xjournal-runtime-v1.0.0').then(cache => 
  cache.keys().then(keys => console.log('Cached URLs:', keys.map(r => r.url)))
);
```

#### 6. **Chrome Extension Scheme Filtering**
Service workers cannot cache non-HTTP(S) schemes:
```javascript
function shouldCache(request) {
  const url = new URL(request.url);
  
  // Only cache HTTP/HTTPS - exclude chrome-extension://, about:, etc.
  if (url.protocol !== 'http:' && url.protocol !== 'https:') {
    return false;
  }
}
```

#### 7. **Regex Pattern Matching: URL.href vs URL.pathname**
When using regex patterns with service workers, understand the difference between testing against full URLs vs paths:

```javascript
const url = new URL('https://example.com/appsettings.json');
console.log(url.href);     // "https://example.com/appsettings.json"
console.log(url.pathname); // "/appsettings.json"

// ❌ WRONG: Testing path pattern against full URL
/appsettings.*\.json$/.test(url.href);     // FALSE - $ expects string to end with .json, but href continues

// ✅ CORRECT: Testing path pattern against pathname
/appsettings.*\.json$/.test(url.pathname); // TRUE - pathname ends with .json
```

**Rule of Thumb:**
- Path-based patterns (with `/` or `$` anchors): Test against `url.pathname`
- Domain-based patterns (like `/supabase\.co/`): Test against `url.href` or `url.host`

### Best Practices

1. **Configuration Files**: ALWAYS blacklist from service worker cache
2. **Cache Versioning**: Bump version when changing cache strategy
3. **Testing**: Test PWA in incognito mode to avoid stale cache
4. **Monitoring**: Log service worker cache hits/misses in production
5. **Documentation**: Document which files should never be cached

### Impact

**Before Fix:**
- Production app → cached localhost config → 422 errors
- No user registration possible
- Confusing behavior (correct CI/CD, wrong runtime)

**After Fix:**
- Service worker ignores appsettings.json
- Fresh config loaded on every deployment
- Production uses correct Supabase URL

### Related Files
- `10xJournal.Client/wwwroot/service-worker.js`
- `10xJournal.Client/wwwroot/staticwebapp.config.json`
- `.github/workflows/deploy-production.yml`

---

**Date:** November 2, 2025  
**Issue:** Service worker caching environment configuration  
**Fix:** Blacklist appsettings.json + cache version bump to v1.0.2
