# Browser Cache Clearing Guide

## For Chrome / Edge / Chromium
1. Press **Ctrl + Shift + Delete** to open Clear Browsing Data
2. Select **All time** from the dropdown
3. Check **Cookies and other site data** and **Cached images and files**
4. Click **Clear data**

## Alternative: Hard Refresh
- **Windows/Linux:** `Ctrl + F5` or `Ctrl + Shift + R`
- **Mac:** `Cmd + Shift + R` or `Cmd + Option + E`

## For Firefox
1. Press **Ctrl + Shift + Delete** to open Clear Recent History
2. Select **Everything** from the dropdown
3. Check **Cache**
4. Click **Clear Now**

## For Safari
1. Go to **Safari > Settings > Privacy**
2. Click **Manage Website Data**
3. Select the site and click **Remove**
4. Or press **Cmd + Option + E** to empty cache

## Browser DevTools Method
1. Open DevTools (**F12**)
2. Right-click the refresh button
3. Select **Empty cache and hard refresh**

## Automatic: Browser Cache Headers
The Nginx server is configured with cache-busting headers:
- JavaScript & CSS files: `no-cache, must-revalidate` (checked every time)
- Images & Fonts: `30 days` (immutable)
- Config changes: Reload browser to see updates

## Testing API Connection
Open browser console and run:
```javascript
fetch('http://localhost:5000/api/health')
  .then(r => r.ok ? console.log('✅ Backend OK') : console.log('❌ Backend Error'))
  .catch(e => console.log('❌ Connection Failed:', e.message))
```

Should print: **✅ Backend OK**
