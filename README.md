# LiveX
Cross-platform light development web server to serve static content with live reloading support

![.NET Core](https://github.com/ababik/LiveX/workflows/.NET%20Core/badge.svg)

```
Usage:
  LiveX [options]

Options:
  --path <path>      Content directory path (skip to use current directory)
  --http <http>      HTTP port (skip to use random port)
  --https <https>    HTTPS port (skip to use random port)
```

## Features
- Web browser auto opening
- Directory browsing
- Live reloading
- HTTPS support
- Cache friendly (don't let broswer caching content)
- CORS friendly (allow any header/method/origin)