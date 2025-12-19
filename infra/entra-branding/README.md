# Microsoft Entra External ID - Branding Configuration

## Overview

This document provides the branding configuration for RAJ Financial's Microsoft Entra External ID login experience.

## Brand Colors

| Element | Color | Hex |
|---------|-------|-----|
| **Primary (Buttons, Links)** | Spanish Yellow | `#ebbb10` |
| **Primary Hover** | Rich Gold | `#d4a80e` |
| **Background** | White | `#ffffff` |
| **Text** | Dark Gray | `#1a1a1a` |
| **Secondary Text** | Medium Gray | `#666666` |
| **Accent** | UC Gold | `#c3922e` |
| **Error** | Red | `#dc3545` |
| **Success** | Green | `#28a745` |

## Required Assets

### Banner Logo (Sign-in page header)
- **Size**: 245 x 36 pixels (max)
- **Max file size**: 10 KB
- **Format**: Transparent PNG, JPG, or JPEG
- **File**: `banner-logo.png` (6.4 KB) ✅

### Square Logo (Loading screens, tiles)
- **Size**: 240 x 240 pixels
- **Max file size**: 50 KB
- **Format**: PNG or JPG
- **File**: `square-logo.png` (16.9 KB) ✅

### Background Image (Optional)
- **Size**: 1920 x 1080 pixels (recommended)
- **Format**: PNG or JPG
- **File**: `background.png` or `background.jpg`
- **Status**: ⚠️ **NEEDS TO BE CREATED**
- **Notes**: Create a subtle background using brand colors. Suggested options:
  1. Solid white (`#ffffff`) - cleanest look
  2. Subtle gold gradient: `linear-gradient(135deg, #ffffff 0%, #fffbcc 50%, #fff7b3 100%)`
  3. Abstract geometric pattern with gold accents
  4. Professional photo with gold overlay

**Design Requirements:**
- Keep it subtle - don't distract from the login form
- Ensure text remains readable (use lighter colors)
- Test on both desktop and mobile
- Consider dark mode compatibility

### Favicon
- **Size**: 32 x 32 pixels
- **Format**: PNG (required by Entra)
- **File**: `favicon.png`

## Azure Portal Configuration Steps

### 1. Navigate to Branding

1. Go to [Azure Portal](https://portal.azure.com)
2. Switch to your Entra External ID tenant:
   - **Dev**: `rajfinancialdev.onmicrosoft.com` (496527a2-41f8-4297-a979-c916e7255a22)
   - **Prod**: `rajfinancialprod.onmicrosoft.com` (cc4d96fb-ebb5-4aef-8ac3-1d4f947dd2b6)
3. Go to **Microsoft Entra ID** → **Company branding**
4. Click **Configure** or **Edit**

### 2. Configure Default Branding

| Setting | Value |
|---------|-------|
| **Sign-in page background color** | `#ffffff` |
| **Banner logo** | Upload `banner-logo.png` |
| **Square logo** | Upload `square-logo.png` |
| **Square logo (dark theme)** | Upload `square-logo.png` (or light version) |
| **Favicon** | Upload `favicon.png` |
| **Show option to remain signed in** | Yes |

### 3. Configure Sign-in Page Text

| Setting | Value |
|---------|-------|
| **Username hint text** | `Email address` |
| **Sign-in page text** | `Welcome to RAJ Financial. Sign in to manage your financial future.` |

### 4. Custom CSS (Advanced)

Entra External ID supports custom CSS. Create a CSS file with:

```css
/* RAJ Financial - Entra External ID Custom Styles */

/* Primary button styling */
.ext-button-primary,
.ext-sign-in-button {
    background-color: #ebbb10 !important;
    border-color: #ebbb10 !important;
    color: #1a1a1a !important;
}

.ext-button-primary:hover,
.ext-sign-in-button:hover {
    background-color: #d4a80e !important;
    border-color: #d4a80e !important;
}

/* Link styling */
a,
.ext-link {
    color: #c3922e !important;
}

a:hover,
.ext-link:hover {
    color: #a67c26 !important;
}

/* Focus states for accessibility */
.ext-button-primary:focus,
.ext-sign-in-button:focus,
input:focus {
    outline: 2px solid #ebbb10 !important;
    outline-offset: 2px;
}

/* Header/title styling */
.ext-title,
.ext-header {
    color: #1a1a1a !important;
    font-family: 'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif !important;
}
```

## User Flow Configuration

### Sign-up Flow

1. Go to **User flows** → **Sign up and sign in**
2. Configure attributes to collect:
   - Email (required)
   - Display name (required)
   - Given name (optional)
   - Surname (optional)

### Self-service Password Reset

1. Enable **Self-service password reset**
2. Configure reset methods:
   - Email
   - Phone (optional)

## Testing

After configuring branding:

1. Open an incognito/private browser window
2. Navigate to your SWA login URL:
   - Dev: `https://gray-cliff-072f3b510.azurestaticapps.net/login`
3. Verify:
   - [ ] Banner logo appears correctly
   - [ ] Colors match brand guidelines
   - [ ] Sign-in text is correct
   - [ ] Buttons use gold color
   - [ ] Mobile view is responsive

## Asset Preparation Commands

```powershell
# Navigate to assets folder
cd "D:\Code\rajfinancial\infra\entra-branding"

# The following assets need to be created/resized from source logos:
# 1. banner-logo.png (280x60) - from logo-horizontal.svg
# 2. square-logo.png (240x240) - from logo-icon.png
# 3. background.png (1920x1080) - optional, create gold gradient

# Source files are in:
# D:\OneDrive - RAJ Financial\RAJ Financial\Assets\All files\logo\
```

## Checklist

### Development Tenant
- [ ] Banner logo uploaded
- [ ] Square logo uploaded
- [ ] Favicon uploaded
- [ ] Sign-in page text configured
- [ ] Custom CSS applied (if supported)
- [ ] User flow configured
- [ ] Tested on desktop
- [ ] Tested on mobile

### Production Tenant
- [ ] Banner logo uploaded
- [ ] Square logo uploaded
- [ ] Favicon uploaded
- [ ] Sign-in page text configured
- [ ] Custom CSS applied (if supported)
- [ ] User flow configured
- [ ] Tested on desktop
- [ ] Tested on mobile

