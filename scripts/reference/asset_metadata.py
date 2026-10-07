"""Stable fresh GUIDs for newly authored project files; existing metadata stays unchanged."""
from pathlib import Path
import uuid
root=Path(__file__).resolve().parents[2]/'FocusCore/Assets/FocusCore'
created=[]
for path in [root/'Art',root/'Tests/PlayMode']+list(root.rglob('*')):
    if path.suffix=='.meta' or not path.exists(): continue
    meta=Path(str(path)+'.meta')
    if meta.exists(): continue
    content='fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\n'
    if path.is_dir(): content+='folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n'
    meta.write_text(content,encoding='utf-8');created.append(str(path.relative_to(root)))
print({'new_metadata':created})
