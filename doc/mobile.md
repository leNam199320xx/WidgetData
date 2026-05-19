# Mobile & Thiết kế Responsive

## 📋 Tổng quan

Widget Data hỗ trợ **mobile-first** approach:

1. **Responsive Design** - MudBlazor responsive components
2. **Progressive Web App (PWA)** - Offline support
3. **Mobile-Optimized UI** - Touch-friendly interface
4. **Performance** - Lazy loading, virtual scrolling

---

## 📱 1. Bố cục Đáp ứng

### MudBlazor Breakpoints

```razor
@using MudBlazor

<MudGrid>
    <!-- Desktop: 4 cột, Tablet: 2 cột, Mobile: 1 cột -->
    @foreach (var widget in widgets)
    {
        <MudItem xs="12" sm="6" md="4" lg="3">
            <WidgetCard Widget="@widget" />
        </MudItem>
    }
</MudGrid>
```

### Bảng tham khảo Breakpoint

| Ngưỡng | Kích thước | Dùng cho |
|------------|------|-------|
| `xs` | < 600px | Điện thoại di động |
| `sm` | 600-960px | Máy tính bảng (dọc) |
| `md` | 960-1280px | Máy tính bảng (ngang), laptop nhỏ |
| `lg` | 1280-1920px | Máy tính để bàn |
| `xl` | > 1920px | Màn hình lớn |

### Điều hướng Đáp ứng

```razor
<!-- AppBar.razor -->
<MudAppBar Elevation="1">
    <!-- Mobile: Menu hamburger -->
    <MudHidden Breakpoint="Breakpoint.SmAndDown" Invert="true">
        <MudIconButton Icon="@Icons.Material.Filled.Menu" 
                       Color="Color.Inherit" 
                       Edge="Edge.Start" 
                       OnClick="@ToggleDrawer" />
    </MudHidden>
    
    <MudText Typo="Typo.h6">Widget Data</MudText>
    
    <MudSpacer />
    
    <!-- Desktop: Menu đầy đủ -->
    <MudHidden Breakpoint="Breakpoint.MdAndUp" Invert="true">
        <MudButton Href="/dashboard" Color="Color.Inherit">Dashboard</MudButton>
        <MudButton Href="/widgets" Color="Color.Inherit">Widgets</MudButton>
        <MudButton Href="/data-sources" Color="Color.Inherit">Data Sources</MudButton>
    </MudHidden>
    
    <MudIconButton Icon="@Icons.Material.Filled.AccountCircle" Color="Color.Inherit" />
</MudAppBar>

<!-- Drawer cho mobile -->
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

### Bảng Đáp ứng

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
    private bool _isMobile => false; // Phát hiện từ trình duyệt
}
```

---

## 🔌 2. Ứng dụng Web Tiến bộ (PWA)

### Bật PWA trong Blazor

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

// Cài đặt
self.addEventListener('install', event => {
  event.waitUntil(
    caches.open(CACHE_NAME)
      .then(cache => cache.addAll(urlsToCache))
  );
});

// Fetch (Network trước, rồi Cache)
self.addEventListener('fetch', event => {
  event.respondWith(
    fetch(event.request)
      .then(response => {
        // Sao chép response và lưu vào cache
        const responseClone = response.clone();
        caches.open(CACHE_NAME)
          .then(cache => cache.put(event.request, responseClone));
        return response;
      })
      .catch(() => {
        // Mạng thất bại, thử cache
        return caches.match(event.request);
      })
  );
});

// Kích hoạt
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

### Đăng ký Service Worker

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

### Chỉ báo Ngoại tuyến

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

## 📊 3. Thành phần Tối ưu cho Mobile

### Nút thân thiện với cảm ứng

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

### Cử chỉ Vuốt

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
        // Điều hướng đến widget tiếp theo
        NavigateNext();
    }
    
    [JSInvokable]
    public void OnSwipeRight()
    {
        // Điều hướng đến widget trước
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

### Kéo để làm mới

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

## 🎨 4. Mẫu UI Mobile

### Tấm đáy (Bottom Sheet)

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

### Bố cục Thẻ xếp chồng

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

## 📈 5. Tối ưu hóa Hiệu năng

### Cuộn ảo

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

### Tải ảnh Lazy

```razor
<img src="@widget.ThumbnailUrl" 
     loading="lazy" 
     alt="@widget.Name"
     class="widget-thumbnail" />
```

### Tối ưu hóa Ảnh

```csharp
// Thay đổi kích thước ảnh cho mobile
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

## 📱 6. Tính năng Thiết bị cụ thể

### Truy cập Camera

```razor
<InputFile OnChange="@HandleFileSelected" accept="image/*" capture="camera" />

@code {
    private async Task HandleFileSelected(InputFileChangeEventArgs e)
    {
        var file = e.File;
        var buffer = new byte[file.Size];
        await file.OpenReadStream().ReadAsync(buffer);
        
        // Xử lý ảnh
        await ProcessImageAsync(buffer);
    }
}
```

### Định vị

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

### Thông báo Đẩy

```javascript
// Yêu cầu quyền
async function requestNotificationPermission() {
  const permission = await Notification.requestPermission();
  return permission === 'granted';
}

// Hiển thị thông báo
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

## 📊 7. Phân tích Mobile

### Theo dõi Sử dụng Mobile

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
        // Phát hiện từ User-Agent hoặc JS
        return "mobile"; // hoặc "tablet"
    }
    
    private string GetOrientation()
    {
        return "portrait"; // hoặc "landscape"
    }
}
```

---

## ✅ Danh sách kiểm tra Mobile

### Thiết kế
- [ ] Layout responsive cho tất cả kích thước màn hình
- [ ] UI thân thiện với cảm ứng (vùng chạm tối thiểu 44x44px)
- [ ] Cỡ chữ dễ đọc (tối thiểu 16px)
- [ ] Khoảng cách phù hợp giữa các phần tử tương tác

### Hiệu năng
- [ ] Lazy loading cho ảnh
- [ ] Virtual scrolling cho danh sách dài
- [ ] Minified & nén assets
- [ ] Service worker caching

### PWA
- [ ] manifest.json đã cấu hình
- [ ] Service worker đã đăng ký
- [ ] Hỗ trợ offline
- [ ] Prompt cài đặt

### Tính năng
- [ ] Pull to refresh
- [ ] Swipe gestures
- [ ] Bottom sheets cho tuỳ chọn
- [ ] Camera/tải file

### Kiểm thử
- [ ] Test trên iOS Safari
- [ ] Test trên Android Chrome
- [ ] Test trên máy tính bảng
- [ ] Test chế độ offline
- [ ] Test cài đặt PWA

---

← [Quay lại INDEX](INDEX.md)
