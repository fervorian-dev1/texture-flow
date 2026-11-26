from pathlib import Path
import sys,base64
root=Path(__file__).resolve().parent.parent
sys.path.insert(0,str(root/'tools/manual-python'))
import markdown
body=markdown.markdown((root/'Skybox Void Guide.md').read_text(encoding='utf-8'),extensions=['tables','fenced_code'])
image=base64.b64encode((root/'docs/Skybox-void-demo.png').read_bytes()).decode('ascii')
body=body.replace('src="docs/Skybox-void-demo.png"','src="data:image/png;base64,'+image+'"')
style='body{font:16px/1.6 Segoe UI,Arial;color:#182033;max-width:960px;margin:36px auto;padding:24px}h1,h2,h3{color:#086d67}h2{margin-top:2em}img{max-width:100%}table{border-collapse:collapse;width:100%;font-size:14px}td,th{padding:10px;border:1px solid #bacbd6;text-align:left;vertical-align:top}th{background:#e9f4f2}code{background:#eef2f4}a{color:#006b73}@media print{body{font-size:10pt;margin:0;padding:0}h2,h3{break-after:avoid}tr{break-inside:avoid}table{font-size:9pt}}@page{size:A4;margin:18mm}'
(root/'Skybox Void Guide.html').write_text('<!doctype html><html lang="en"><meta charset="utf-8"><title>Texture Flow Skybox / Void Guide</title><style>'+style+'</style><body>'+body+'</body></html>',encoding='utf-8')
print('Self-contained Skybox Void Guide.html generated')
