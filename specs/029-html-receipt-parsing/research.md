# Research: HTML Receipt Parsing

**Feature**: 029-html-receipt-parsing
**Date**: 2026-01-08

## Research Topics

### 1. HTML-to-Image Rendering for Thumbnails

**Decision**: Use PuppeteerSharp (headless Chromium)

**Rationale**:
- PuppeteerSharp is the .NET port of Puppeteer, providing full Chromium rendering
- Accurate rendering of CSS, fonts, and layout compared to simpler solutions
- Well-maintained with active community support
- Can capture screenshots at specific viewport sizes (200x200 for thumbnails)
- Supports running in Docker/Kubernetes with headless Chrome

**Alternatives Considered**:
| Alternative | Why Rejected |
|-------------|--------------|
| Playwright for .NET | More complex setup, overkill for simple screenshot task |
| wkhtmltoimage | Outdated WebKit engine, poor CSS3 support, rendering inconsistencies |
| Server-side HTML-to-canvas | Limited CSS support, no external fonts |
| Client-side thumbnail generation | Security risk - requires executing untrusted HTML on client |

**Implementation Notes**:
- Install via NuGet: `PuppeteerSharp`
- Download Chromium during first run or include in Docker image
- Set viewport to 800x600, then crop/resize to 200x200 thumbnail
- Use `--no-sandbox` flag for container environments

---

### 2. HTML Sanitization for XSS Prevention

**Decision**: Use HtmlSanitizer library

**Rationale**:
- Purpose-built for XSS prevention in .NET
- Configurable allowlist of tags, attributes, and CSS properties
- Actively maintained with regular security updates
- OWASP-recommended approach (allowlist vs blocklist)
- Performance-optimized for server-side use

**Alternatives Considered**:
| Alternative | Why Rejected |
|-------------|--------------|
| HtmlAgilityPack alone | HTML parser only, no built-in sanitization |
| AngleSharp | Parser focused, requires custom sanitization logic |
| Regex-based stripping | Fragile, easily bypassed, not security-best-practice |
| iframe sandbox | Client-side approach, doesn't protect backend |

**Configuration for Receipt Display**:
```csharp
var sanitizer = new HtmlSanitizer();
// Allow common formatting tags
sanitizer.AllowedTags.Clear();
sanitizer.AllowedTags.UnionWith(new[] {
    "div", "span", "p", "br", "table", "tr", "td", "th", "thead", "tbody",
    "img", "a", "b", "strong", "i", "em", "h1", "h2", "h3", "h4", "ul", "ol", "li"
});
// Block all scripts, forms, iframes
sanitizer.AllowedTags.Remove("script");
sanitizer.AllowedTags.Remove("form");
sanitizer.AllowedTags.Remove("iframe");
// Allow safe attributes only
sanitizer.AllowedAttributes.Clear();
sanitizer.AllowedAttributes.UnionWith(new[] {
    "class", "style", "src", "alt", "href", "width", "height"
});
// Strip event handlers (onclick, onerror, etc.)
sanitizer.RemovingAttribute += (s, e) => { /* log for monitoring */ };
```

---

### 3. AI-Based Receipt Data Extraction

**Decision**: Use existing Azure OpenAI integration with Semantic Kernel

**Rationale**:
- Leverages existing `gpt-4o-mini` deployment already configured in ExpenseFlow
- Semantic Kernel provides structured output parsing
- Consistent with constitution's technology stack
- No additional service costs beyond existing Azure OpenAI subscription
- Can handle diverse receipt formats without template maintenance

**Extraction Prompt Strategy**:
```text
Extract receipt information from the following HTML email content.
Return JSON with these fields:
- vendor_name: string (merchant/company name)
- transaction_date: string (ISO 8601 format, e.g., "2025-12-15")
- total_amount: number (final amount charged)
- currency: string (3-letter code, e.g., "USD")
- line_items: array of { description: string, quantity: number, unit_price: number }
- confidence: object with field-level confidence scores (0.0-1.0)

If a field cannot be determined, set it to null.
HTML Content:
{html_content}
```

**Alternatives Considered**:
| Alternative | Why Rejected |
|-------------|--------------|
| DOM pattern matching | Requires per-vendor templates, high maintenance |
| Document Intelligence | Designed for images/PDFs, not structured HTML |
| Custom ML model | Training data requirements, deployment complexity |
| Regex extraction | Fragile, poor accuracy across vendor variations |

---

### 4. HTML Content Type Detection

**Decision**: Add `text/html` to allowed content types with magic byte validation

**Rationale**:
- HTML files begin with `<!DOCTYPE` or `<html` tags (after optional BOM)
- Consistent with existing content type validation pattern in ReceiptService
- Browser uploads will provide correct MIME type
- Files saved from email clients use standard .html/.htm extensions

**Magic Byte Validation**:
```csharp
// Check for HTML signature (after skipping optional BOM)
private bool IsHtmlContent(byte[] buffer)
{
    var content = Encoding.UTF8.GetString(buffer).TrimStart();
    return content.StartsWith("<!DOCTYPE", StringComparison.OrdinalIgnoreCase)
        || content.StartsWith("<html", StringComparison.OrdinalIgnoreCase)
        || content.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase); // XHTML
}
```

---

### 5. Failed Extraction Storage for Re-processing

**Decision**: Store raw HTML in Azure Blob Storage alongside extraction metadata

**Rationale**:
- FR-014 requires storing raw HTML for failed/low-confidence extractions
- Enables "flywheel" improvement: review failures → improve prompts → re-process
- Blob storage is already used for receipt images/PDFs
- Separate from receipt blob to avoid polluting successful receipts

**Storage Pattern**:
```text
receipts/{userId}/{year}/{month}/{receiptId}.html          # Original HTML
receipts/{userId}/{year}/{month}/{receiptId}_thumb.jpg     # Generated thumbnail
extraction-failures/{userId}/{receiptId}/                   # Failed extraction folder
  ├── raw.html                                              # Raw HTML
  ├── prompt.txt                                            # Extraction prompt used
  ├── response.json                                         # AI response
  └── metrics.json                                          # Timing, confidence, errors
```

---

## Dependencies Summary

| Package | Version | Purpose |
|---------|---------|---------|
| PuppeteerSharp | 19.x | HTML-to-image thumbnail generation |
| HtmlSanitizer | 8.x | XSS prevention for HTML display |
| Microsoft.SemanticKernel | 1.25+ | AI extraction (already installed) |
| HtmlAgilityPack | 1.11.x | HTML parsing utilities |

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| AI extraction accuracy below 80% | Medium | High | Include confidence scores, flag low-confidence for review |
| Headless Chrome resource usage | Low | Medium | Configure resource limits, use browser pool |
| HTML sanitizer bypass | Low | Critical | Regular security updates, allowlist-only approach |
| Large HTML files timeout | Low | Low | 5MB limit already in spec, chunk processing if needed |
