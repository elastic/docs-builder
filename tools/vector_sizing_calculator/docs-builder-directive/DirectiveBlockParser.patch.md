# Patches to register the directive in docs-builder

These are the exact changes needed in the existing docs-builder C# files.

## 1. `DirectiveBlockParser.cs`

### Add import at the top:

```csharp
using Elastic.Markdown.Myst.Directives.VectorSizing;
```

### Add this block inside `CreateFencedBlock()`, before the `return new UnknownDirectiveBlock(...)` fallback:

```csharp
if (info.IndexOf("{vector-sizing-calculator}") > 0)
    return new VectorSizingBlock(this, context);
```

## 2. `DirectiveHtmlRenderer.cs`

### Add import at the top:

```csharp
using Elastic.Markdown.Myst.Directives.VectorSizing;
```

### Add this case inside the `Write()` switch, before the `default:` case:

```csharp
case VectorSizingBlock vectorSizingBlock:
    WriteVectorSizing(renderer, vectorSizingBlock);
    return;
```

### Add this method to the class:

```csharp
private static void WriteVectorSizing(HtmlRenderer renderer, VectorSizingBlock block)
{
    var slice = VectorSizingView.Create(new VectorSizingViewModel
    {
        DirectiveBlock = block
    });
    RenderRazorSlice(slice, renderer);
}
```

## 3. Load the JS bundle

The docs site frontend needs to load the built web component JS bundle.
Add to the site's base HTML template (or asset pipeline):

```html
<script src="/_static/vector-sizing-calculator.iife.js" defer></script>
```

The bundle file is produced by running `npm run build` in the `web-component/` directory
and is output as `dist/vector-sizing-calculator.iife.js`.
