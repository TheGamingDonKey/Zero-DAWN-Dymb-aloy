from pathlib import Path
import pypdfium2 as pdfium
from PIL import Image
from pypdf import PdfReader
root=Path(__file__).resolve().parents[2]
source=root/'docs/reference/Aloy Focus Reference.pdf'
out=root/'.artifacts/research/booklet'
out.mkdir(parents=True,exist_ok=True)
document=pdfium.PdfDocument(source)
pages=[page.render(scale=1.4).to_pil().convert('RGB') for page in document]
for i in range(0,len(pages),2):
    pair=pages[i:i+2]
    result=Image.new('RGB',(pair[0].width*2+16,pair[0].height),'#081120')
    result.paste(pair[0],(0,0))
    if len(pair)>1: result.paste(pair[1],(pair[0].width+16,0))
    result.save(out/f'pages-{i+1:02d}-{min(i+2,len(pages)):02d}.png')
reader=PdfReader(source)
assert all(len(page.extract_text())>250 for page in reader.pages)
assert len(reader.pages)==len(pages)
print({'pages':len(pages),'bytes':source.stat().st_size,'rendered_directory':str(out)})
