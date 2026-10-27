# Cabeceras de seguridad comunes a prod y desarrollo. Van solo a nivel de server: un add_header dentro de un
# location anularía todas estas (así se perdió la CSP del documento antes del rediseño)
add_header X-Frame-Options        "DENY"                            always;
add_header X-Content-Type-Options "nosniff"                         always;
add_header Referrer-Policy        "strict-origin-when-cross-origin" always;
add_header Permissions-Policy     "camera=(), microphone=(), geolocation=(), payment=(), usb=()" always;
# Blazor .NET 9 solo necesita 'wasm-unsafe-eval' para compilar el WebAssembly; no hay scripts ni estilos en línea.
# style-src-attr permite los style="" dinámicos que Blazor escribe en el DOM (anchos, colores calculados)
add_header Content-Security-Policy "default-src 'self'; script-src 'self' 'wasm-unsafe-eval'; style-src 'self'; style-src-attr 'unsafe-inline'; font-src 'self'; img-src 'self' data:; connect-src 'self'; object-src 'none'; base-uri 'self'; form-action 'self'; frame-ancestors 'none'" always;
add_header Cache-Control          $cache_control                    always;
