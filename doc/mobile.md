# Mobile & Responsive Design

## 📋 Tổng quan

Widget Data hỗ trợ **mobile-first** approach:

1. **Responsive Design** - MudBlazor responsive components
2. **Progressive Web App (PWA)** - Offline support
3. **Mobile-Optimized UI** - Touch-friendly interface
4. **Performance** - Lazy loading, virtual scrolling

---

## 📱 1. Responsive Layout

### MudBlazor Breakpoints

```razor
@using MudBlazor

<MudGrid>
    <!-- Desktop: 4 columns, Tablet: 2 columns, Mobile: 1 column -->
    @foreach (var widget in widgets)
    {
        <MudItem xs="12" sm="6" md="4" lg="3">
            <WidgetCard Widget="@widget" />
        </MudItem>
    }
</MudGrid>
```

### Breakpoint Reference

| Breakpoint | Size | Usage |
|------------|------|-------|
| `xs` | < 600px | Mobile phones |
| `sm` | 600-960px | Tablets (portrait) |
| `md` | 960-1280px | Tablets (landscape), small laptops |
| `lg` | 1280-1920px | Desktops |
| `xl` | > 1920px | Large desktops |

### Responsive Navigation

```razor
<!-- AppBar.razor -->
<MudAppBar Elevation="1">
    <!-- Mobile: Hamburger menu -->
    <MudHidden Breakpoint="Breakpoint.SmAndDown" Invert="true">
        <MudIconButton Icon="@Icons.Material.Filled.Menu" 
                       Color="Color.Inherit" 
                       Edge="Edge.Start" 
                       OnClick="@ToggleDrawer" />
    </MudHidden>
    
    <MudText Typo="Typo.h6">Widget Data</MudText>
    
    <MudSpacer />
    
    <!-- Desktop: Full menu -->
    <MudHidden Breakpoint="Breakpoint.MdAndUp" Invert="true">
        <MudButton Href="/dashboard" Color="Color.Inherit">Dashboard</MudButton>
        <MudButton Href="/widgets" Color="Color.Inherit">Widgets</MudButton>
        <MudButton Href="/data-sources" Color="Color.Inherit">Data Sources</MudButton>
    </MudHidden>
    
    <MudIconButton Icon="@Icons.Material.Filled.AccountCircle" Color="Color.Inherit" />
</MudAppBar>

<!-- Drawer for mobile -->
<MudDrawer @bind-Open="@_drawerOpen" Elevation="1">
    <MudNavMenu>
        <MudNavLink Href="/dashboard" Icon="@Icons.Material.Filled.Dashboard">Dashboard</MudNavLink>
        <MudNavLink Href="/widgets" Icon="@Icons.Material.Filled.Widgets">Widgets</MudNavLink>
        <MudNavLink Href="/data-sources" Icon="@Icons.Material.Filled.Storage">Data Sources</MudNavLink>
    </MudNavMenu>
</MudDrawer>

@code {
    private bool _drawerOpen = false;
    
    private void ToggleDrawer()
    {
        _drawerOpen = !_drawerOpen;
    }
}
```

### Responsive Table

```razor
<MudTable Items="@widgets" 
          Hover="true" 
          Breakpoint="Breakpoint.Sm"
          Dense="@(_isMobile)">
    <HeaderContent>
        <MudTh>Name</MudTh>
        <MudTh><MudHidden Breakpoint="Breakpoint.Xs">Type</MudHidden></MudTh>
        <MudTh><MudHidden Breakpoint="Breakpoint.SmAndDown">Created</MudHidden></MudTh>
        <MudTh>Actions</MudTh>
    </HeaderContent>
    <RowTemplate>
        <MudTd DataLabel="Name">@context.Name</MudTd>
        <MudTd DataLabel="Type">
            <MudHidden Breakpoint="Breakpoint.Xs">@context.WidgetType</MudHidden>
        </MudTd>
        <MudTd DataLabel="Created">
            <MudHidden Breakpoint="Breakpoint.SmAndDown">
                @context.CreatedAt.ToShortDateString()
            </MudHidden>
        </MudTd>
        <MudTd>
            <MudIconButton Icon="@Icons.Material.Filled.Edit" Size="Size.Small" />
            <MudIconButton Icon="@Icons.Material.Filled.Delete" Size="Size.Small" />
        </MudTd>
    </RowTemplate>
</MudTable>

@code {
    private bool _isMobile => false; // Detect from browser
}
```

---

## 🔌 2. Progressive Web App (PWA)

### Enable PWA in Blazor

```csharp
// Program.cs
builder.Services.AddBlazorWebAssembly().AddPWA();
```

### manifest.json

```json
{
  "name": "Widget Data",
  "short_name": "WidgetData",
  "start_url": "/",
  "display": "standalone",
  "background_color": "#ffffff",
  "theme_color": "#512BD4",
  "icons": [
    {
      "src": "icon-192.png",
      "sizes": "192x192",
      "type": "image/png"
    },
    {
      "src": "icon-512.png",
      "sizes": "512x512",
      "type": "image/png"
    }
  ],
  "description": "Data pipeline and widget platform",
  "categories": ["business", "productivity"],
  "screenshots": [
    {
      "src": "screenshot-desktop.png",
      "sizes": "1920x1080",
      "type": "image/png",
      "form_factor": "wide"
    },
    {
      "src": "screenshot-mobile.png",
      "sizes": "750x1334",
      "type": "image/png",
      "form_factor": "narrow"
    }
  ]
}
```

### Service Worker (service-worker.js)

```javascript
const CACHE_NAME = 'widgetdata-v1';
const urlsToCache = [
  '/',
  '/index.html',
  '/css/app.css',
  '/js/app.js',
  '/_framework/blazor.webassembly.js'
];

// Install
self.addEventListener('install', event => {
  event.waitUntil(
    caches.open(CACHE_NAME)
      .then(cache => cache.addAll(urlsToCache))
  );
});

// Fetch (Network First, then Cache)
self.addEventListener('fetch', event => {
  event.respondWith(
    fetch(event.request)
      .then(response => {
        // Clone response and cache it
        const responseClone = response.clone();
        caches.open(CACHE_NAME)
          .then(cache => cache.put(event.request, responseClone));
        return response;
      })
      .catch(() => {
        // Network failed, try cache
        return caches.match(event.request);
      })
  );
});

// Activate
self.addEventListener('activate', event => {
  event.waitUntil(
    caches.keys().then(cacheNames => {
      return Promise.all(
        cacheNames.map(cacheName => {
          if (cacheName !== CACHE_NAME) {
            return caches.delete(cacheName);
          }
        })
      );
    })
  );
});
```

### Register Service Worker

```html
<!-- index.html -->
<script>
if ('serviceWorker' in navigator) {
  navigator.serviceWorker.register('/service-worker.js')
    .then(registration => {
      console.log('Service Worker registered:', registration);
    })
    .catch(error => {
      console.log('Service Worker registration failed:', error);
    });
}
</script>
```

### Offline Indicator

```razor
@inject IJSRuntime JS

<MudSnackbarProvider />

@if (!_isOnline)
{
    <MudAlert Severity="Severity.Warning" Class="fixed-top">
        <MudIcon Icon="@Icons.Material.Filled.CloudOff" />
        You are offline. Some features may be limited.
    </MudAlert>
}

@code {
    private bool _isOnline = true;
    
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await JS.InvokeVoidAsync("addOnlineOfflineListeners", 
                DotNetObjectReference.Create(this));
        }
    }
    
    [JSInvokable]
    public void UpdateOnlineStatus(bool isOnline)
    {
        _isOnline = isOnline;
        StateHasChanged();
    }
}
```

```javascript
// wwwroot/js/app.js
window.addOnlineOfflineListeners = (dotNetHelper) => {
  window.addEventListener('online', () => {
    dotNetHelper.invokeMethodAsync('UpdateOnlineStatus', true);
  });
  
  window.addEventListener('offline', () => {
    dotNetHelper.invokeMethodAsync('UpdateOnlineStatus', false);
  });
};
```

---

## 📊 3. Mobile-Optimized Components

### Touch-Friendly Buttons

```razor
<MudFab Color="Color.Primary" 
        StartIcon="@Icons.Material.Filled.Add" 
        Size="Size.Large"
        OnClick="@CreateWidget"
        Class="fab-bottom-right" />

<style>
.fab-bottom-right {
    position: fixed;
    bottom: 16px;
    right: 16px;
    z-index: 1000;
}
</style>
```

### Swipe Gestures

```razor
@inject IJSRuntime JS

<div @ref="_swipeContainer" class="swipe-container">
    <WidgetCard Widget="@currentWidget" />
</div>

@code {
    private ElementReference _swipeContainer;
    private Widget currentWidget;
    
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await JS.InvokeVoidAsync("initSwipe", _swipeContainer, 
                DotNetObjectReference.Create(this));
        }
    }
    
    [JSInvokable]
    public void OnSwipeLeft()
    {
        // Navigate to next widget
        NavigateNext();
    }
    
    [JSInvokable]
    public void OnSwipeRight()
    {
        // Navigate to previous widget
        NavigatePrevious();
    }
}
```

```javascript
// wwwroot/js/swipe.js
window.initSwipe = (element, dotNetHelper) => {
  let touchStartX = 0;
  let touchEndX = 0;
  
  element.addEventListener('touchstart', e => {
    touchStartX = e.changedTouches[0].screenX;
  });
  
  element.addEventListener('touchend', e => {
    touchEndX = e.changedTouches[0].screenX;
    handleSwipe();
  });
  
  function handleSwipe() {
    if (touchEndX < touchStartX - 50) {
      dotNetHelper.invokeMethodAsync('OnSwipeLeft');
    }
    if (touchEndX > touchStartX + 50) {
      dotNetHelper.invokeMethodAsync('OnSwipeRight');
    }
  }
};
```

### Pull to Refresh

```razor
<div class="pull-to-refresh" @ref="_refreshContainer">
    @if (_isRefreshing)
    {
        <MudProgressCircular Indeterminate="true" Size="Size.Small" />
    }
    
    <MudContainer>
        @foreach (var widget in widgets)
        {
            <WidgetCard Widget="@widget" />
        }
    </MudContainer>
</div>

@code {
    private ElementReference _refreshContainer;
    private bool _isRefreshing = false;
    
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await JS.InvokeVoidAsync("initPullToRefresh", _refreshContainer,
                DotNetObjectReference.Create(this));
        }
    }
    
    [JSInvokable]
    public async Task OnPullRefresh()
    {
        _isRefreshing = true;
        StateHasChanged();
        
        await Task.Delay(1000); // Simulate refresh
        widgets = await WidgetService.GetAllAsync();
        
        _isRefreshing = false;
        StateHasChanged();
    }
}
```

---

## 🎨 4. Mobile UI Patterns

### Bottom Sheet

```razor
<MudDrawer @bind-Open="@_bottomSheetOpen" 
           Anchor="Anchor.Bottom" 
           Elevation="1" 
           Variant="@DrawerVariant.Temporary">
    <MudContainer>
        <MudText Typo="Typo.h6">Widget Options</MudText>
        <MudDivider Class="my-2" />
        
        <MudList>
            <MudListItem Icon="@Icons.Material.Filled.Edit" OnClick="@EditWidget">
                Edit Widget
            </MudListItem>
            <MudListItem Icon="@Icons.Material.Filled.Refresh" OnClick="@RefreshWidget">
                Refresh Data
            </MudListItem>
            <MudListItem Icon="@Icons.Material.Filled.Share" OnClick="@ShareWidget">
                Share
            </MudListItem>
            <MudListItem Icon="@Icons.Material.Filled.Delete" OnClick="@DeleteWidget">
                Delete
            </MudListItem>
        </MudList>
    </MudContainer>
</MudDrawer>

@code {
    private bool _bottomSheetOpen = false;
    
    private void OpenBottomSheet()
    {
        _bottomSheetOpen = true;
    }
}
```

### Card Stack Layout

```razor
<MudContainer MaxWidth="MaxWidth.Small">
    @foreach (var widget in widgets)
    {
        <MudCard Class="mb-4">
            <MudCardHeader>
                <CardHeaderContent>
                    <MudText Typo="Typo.h6">@widget.Name</MudText>
                    <MudText Typo="Typo.body2">@widget.WidgetType</MudText>
                </CardHeaderContent>
                <CardHeaderActions>
                    <MudIconButton Icon="@Icons.Material.Filled.MoreVert" 
                                   OnClick="@(() => OpenOptions(widget))" />
                </CardHeaderActions>
            </MudCardHeader>
            
            <MudCardContent>
                <WidgetPreview Widget="@widget" />
            </MudCardContent>
            
            <MudCardActions>
                <MudButton Variant="Variant.Text" 
                           Color="Color.Primary" 
                           FullWidth="true"
                           OnClick="@(() => ViewDetails(widget))">
                    View Details
                </MudButton>
            </MudCardActions>
        </MudCard>
    }
</MudContainer>
```

---

## 📈 5. Performance Optimization

### Virtual Scrolling

```razor
@using Microsoft.AspNetCore.Components.Web.Virtualization

<Virtualize Items="@widgets" Context="widget">
    <ItemContent>
        <WidgetCard Widget="@widget" Class="mb-2" />
    </ItemContent>
    <Placeholder>
        <MudSkeleton SkeletonType="SkeletonType.Rectangle" Height="200px" Class="mb-2" />
    </Placeholder>
</Virtualize>
```

### Lazy Load Images

```razor
<img src="@widget.ThumbnailUrl" 
     loading="lazy" 
     alt="@widget.Name"
     class="widget-thumbnail" />
```

### Image Optimization

```csharp
// Resize images for mobile
public async Task<byte[]> ResizeImageAsync(byte[] imageBytes, int maxWidth, int maxHeight)
{
    using var image = Image.Load(imageBytes);
    
    image.Mutate(x => x.Resize(new ResizeOptions
    {
        Size = new Size(maxWidth, maxHeight),
        Mode = ResizeMode.Max
    }));
    
    using var ms = new MemoryStream();
    await image.SaveAsync(ms, new JpegEncoder { Quality = 80 });
    
    return ms.ToArray();
}
```

---

## 📱 6. Device-Specific Features

### Camera Access

```razor
<InputFile OnChange="@HandleFileSelected" accept="image/*" capture="camera" />

@code {
    private async Task HandleFileSelected(InputFileChangeEventArgs e)
    {
        var file = e.File;
        var buffer = new byte[file.Size];
        await file.OpenReadStream().ReadAsync(buffer);
        
        // Process image
        await ProcessImageAsync(buffer);
    }
}
```

### Geolocation

```javascript
// wwwroot/js/geolocation.js
window.getCurrentPosition = () => {
  return new Promise((resolve, reject) => {
    if (!navigator.geolocation) {
      reject('Geolocation not supported');
      return;
    }
    
    navigator.geolocation.getCurrentPosition(
      position => {
        resolve({
          latitude: position.coords.latitude,
          longitude: position.coords.longitude
        });
      },
      error => reject(error.message)
    );
  });
};
```

```razor
@inject IJSRuntime JS

@code {
    private async Task GetLocationAsync()
    {
        try
        {
            var location = await JS.InvokeAsync<LocationData>("getCurrentPosition");
            // Use location.Latitude, location.Longitude
        }
        catch (Exception ex)
        {
            // Handle error
        }
    }
    
    public class LocationData
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}
```

### Push Notifications

```javascript
// Request permission
async function requestNotificationPermission() {
  const permission = await Notification.requestPermission();
  return permission === 'granted';
}

// Show notification
function showNotification(title, body) {
  if (Notification.permission === 'granted') {
    new Notification(title, {
      body: body,
      icon: '/icon-192.png',
      badge: '/badge-72.png'
    });
  }
}
```

---

## 📊 7. Mobile Analytics

### Track Mobile Usage

```csharp
public class MobileAnalyticsService
{
    private readonly TelemetryClient _telemetry;
    
    public void TrackMobileEvent(string eventName, Dictionary<string, string> properties = null)
    {
        properties ??= new Dictionary<string, string>();
        
        properties["Platform"] = "Mobile";
        properties["ScreenSize"] = GetScreenSize();
        properties["Orientation"] = GetOrientation();
        
        _telemetry.TrackEvent(eventName, properties);
    }
    
    private string GetScreenSize()
    {
        // Detect from User-Agent or JS
        return "mobile"; // or "tablet"
    }
    
    private string GetOrientation()
    {
        return "portrait"; // or "landscape"
    }
}
```

---

## ✅ Mobile Checklist

### Design
- [ ] Responsive layout for all screen sizes
- [ ] Touch-friendly UI (min 44x44px tap targets)
- [ ] Readable font sizes (min 16px)
- [ ] Proper spacing between interactive elements

### Performance
- [ ] Lazy loading for images
- [ ] Virtual scrolling for long lists
- [ ] Minified & compressed assets
- [ ] Service worker caching

### PWA
- [ ] manifest.json configured
- [ ] Service worker registered
- [ ] Offline support
- [ ] Install prompt

### Features
- [ ] Pull to refresh
- [ ] Swipe gestures
- [ ] Bottom sheets for options
- [ ] Camera/file upload

### Testing
- [ ] Test on iOS Safari
- [ ] Test on Android Chrome
- [ ] Test on tablets
- [ ] Test offline mode
- [ ] Test PWA installation

---

← [Quay lại INDEX](INDEX.md)
