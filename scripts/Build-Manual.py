"""Render the repository manual as self-contained, printable HTML."""
from pathlib import Path
import base64
import sys
import re

root = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(root / "tools" / "manual-python"))
try:
    import markdown
except ImportError as exc:
    raise SystemExit("Install Markdown 3.10 for this documentation tool: python -m pip install --target tools/manual-python Markdown==3.10") from exc

source = (root / "T.A.R.D. Manual.md").read_text(encoding="utf-8-sig")
headings=re.findall(r"^## (\d+\. .+)$",source,re.M)
items=[]
for heading in headings:
    anchor=re.sub(r"[^\w\s-]","",heading.lower())
    anchor=re.sub(r"\s+","-",anchor)
    items.append(f"- [{heading}](#{anchor})")
source=re.sub(r"## Contents\n.*?(?=\n## 1\.)","## Contents\n\n"+"\n".join(items)+"\n",source,flags=re.S)
(root / "T.A.R.D. Manual.md").write_text(source,encoding="utf-8-sig")
body = markdown.markdown(source, extensions=["toc", "tables", "fenced_code"], extension_configs={"toc": {"toc_depth": "2-3"}})
for path in (root / "docs").glob("*.png"):
    data=base64.b64encode(path.read_bytes()).decode("ascii")
    body=body.replace(f'src="docs/{path.name}"',f'src="data:image/png;base64,{data}"')
icon = base64.b64encode((root / "src/TextureFlow/Assets/AppIcon.png").read_bytes()).decode("ascii")
style = """
:root{color-scheme:light;font-family:Segoe UI,Arial,sans-serif;color:#182033;background:#eef2f6}
body{max-width:1050px;margin:32px auto;padding:36px 44px;background:white;line-height:1.6}
header{display:flex;align-items:center;gap:18px;border-bottom:3px solid #248978;padding-bottom:18px}
header img{width:72px;height:72px;image-rendering:pixelated}header strong{font-size:22px}
img{max-width:100%;height:auto}h1{font-size:36px;line-height:1.2}h2{font-size:25px;margin-top:2em;border-bottom:1px solid #cbd5e1;padding-bottom:7px}
h3{font-size:19px;margin-top:1.5em}a{color:#006c71}p,li{overflow-wrap:anywhere}
table{border-collapse:collapse;width:100%;margin:18px 0;font-size:14px;table-layout:auto}
th,td{border:1px solid #cbd5e1;padding:9px 11px;vertical-align:top;overflow-wrap:anywhere}th{background:#e8f2f1;text-align:left}
tr:nth-child(even){background:#f7f9fb}td:first-child{min-width:120px;width:28%}
pre{background:#f0f4f7;border:1px solid #d8e1e9;padding:14px;white-space:pre-wrap;overflow-wrap:anywhere;font-size:12px}
code{font-family:Consolas,monospace;font-size:.9em}p code,li code,td code{background:#f0f4f7;padding:1px 4px}
.toc{background:#f5f8fb;border:1px solid #d8e1e9;padding:12px 22px}.toc a{text-decoration:none}
footer{border-top:1px solid #cbd5e1;margin-top:40px;padding-top:15px;color:#526078}
@page{size:A4;margin:17mm 16mm}
@media print{:root{background:white}body{margin:0;padding:0;max-width:none;font-size:10pt;line-height:1.4}
h1{font-size:26pt}h2{font-size:17pt;break-before:page}h3{font-size:12pt;break-after:avoid}
h1,h2,h3{break-after:avoid}table{font-size:8.5pt}th,td{padding:6px 7px}tr{break-inside:avoid}
pre{font-size:8pt}a{color:#185260;text-decoration:none}header{break-after:avoid}.toc{font-size:9pt}}
"""
html = f'''<!doctype html><html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>T.A.R.D. Manual</title><style>{style}</style></head><body><header><img src="data:image/png;base64,{icon}" alt="Texture Flow icon"><div><strong>Texture Flow for VRChat</strong><br>Owner's guide and recovery handbook · version 0.3.0</div></header>{body}<footer>T.A.R.D. Manual · November 24, 2026 · app 0.3.0 preview. Preserve this handbook with the project source and release downloads.</footer></body></html>'''
(root / "T.A.R.D. Manual.html").write_text(html, encoding="utf-8")
print(f"Manual: {len(source.split()):,} words; {html.count('<h2'):,} main sections; self-contained HTML generated.")
